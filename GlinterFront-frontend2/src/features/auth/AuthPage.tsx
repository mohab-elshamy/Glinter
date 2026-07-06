import { useEffect, useState } from "react";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { FileText, KeyRound, Loader2, ShieldCheck, Sparkles } from "lucide-react";
import { motion } from "framer-motion";
import {
  authApi,
  isMfaChallenge,
  type AuthResponse,
  type MfaSetupResponse,
} from "@/shared/services/api-auth";
import { authStorage } from "@/shared/lib/auth";

const ROLES = ["Traveler", "LocalBuddy", "HotelOwner", "ExperienceProvider"] as const;
const ROLES_REQUIRING_REVIEW = new Set<typeof ROLES[number]>([
  "LocalBuddy",
  "HotelOwner",
  "ExperienceProvider",
]);
const MAX_IDENTITY_DOCUMENT_BYTES = 5 * 1024 * 1024;

const roleLabel = (role: typeof ROLES[number]) => {
  if (role === "LocalBuddy") return "Local Buddy";
  if (role === "HotelOwner") return "Hotel Provider";
  if (role === "ExperienceProvider") return "Experience Provider";
  return role;
};

type AuthView =
  | "login"
  | "signup"
  | "confirm-email"
  | "forgot-password"
  | "reset-password"
  | "mfa-verify"
  | "mfa-setup"
  | "recovery-codes";

interface AuthProps {
  initialView?: Extract<AuthView, "confirm-email" | "reset-password">;
}

const inputClass =
  "w-full px-3 py-2.5 rounded-lg bg-secondary border border-border text-sm " +
  "text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-primary";

const Auth = ({ initialView }: AuthProps) => {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const [view, setView] = useState<AuthView>(initialView ?? "login");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const [loginEmail, setLoginEmail] = useState("");
  const [loginPassword, setLoginPassword] = useState("");
  const [signupName, setSignupName] = useState("");
  const [signupEmail, setSignupEmail] = useState("");
  const [signupPassword, setSignupPassword] = useState("");
  const [signupRole, setSignupRole] = useState<typeof ROLES[number]>("Traveler");
  const [identityDocument, setIdentityDocument] = useState<{
    url: string;
    fileName: string;
    contentType: string;
  } | null>(null);
  const [identityDocumentLoading, setIdentityDocumentLoading] = useState(false);

  const [confirmationUserId, setConfirmationUserId] = useState(
    searchParams.get("userId") ?? "",
  );
  const [confirmationToken, setConfirmationToken] = useState(
    searchParams.get("token") ?? "",
  );
  const [recoveryEmail, setRecoveryEmail] = useState(
    searchParams.get("email") ?? "",
  );
  const [resetToken, setResetToken] = useState(searchParams.get("token") ?? "");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [mfaTicket, setMfaTicket] = useState("");
  const [mfaSetup, setMfaSetup] = useState<MfaSetupResponse | null>(null);
  const [mfaCode, setMfaCode] = useState("");
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [confirmationAttempted, setConfirmationAttempted] = useState(false);

  const clearFeedback = () => {
    setError("");
    setMessage("");
  };

  const changeView = (nextView: AuthView) => {
    clearFeedback();
    setView(nextView);
  };

  const storeSession = (session: AuthResponse) => {
    authStorage.setTokens(
      session.token,
      session.refreshToken,
      session.refreshTokenExpiresAtUtc,
    );
    authStorage.setUser({
      userId: session.userId,
      fullName: session.fullName,
      email: session.email,
      roles: session.roles,
      isActive: true,
      accountReviewStatus: session.accountReviewStatus,
    });
    window.dispatchEvent(new Event("auth-change"));
  };

  const finishLogin = (session: AuthResponse) => {
    storeSession(session);
    const requestedPath = (location.state as { from?: string } | null)?.from;
    navigate(requestedPath?.startsWith("/") ? requestedPath : "/start", {
      replace: true,
    });
  };

  useEffect(() => {
    if (
      initialView !== "confirm-email" ||
      !confirmationUserId ||
      !confirmationToken ||
      confirmationAttempted
    ) {
      return;
    }

    setConfirmationAttempted(true);
    setLoading(true);
    authApi.confirmEmail(confirmationUserId, confirmationToken)
      .then((response) => {
        setMessage(response.message);
        setLoginEmail(recoveryEmail);
        setConfirmationToken("");
      })
      .catch((requestError: unknown) => {
        setError(requestError instanceof Error
          ? requestError.message
          : "Email confirmation failed.");
      })
      .finally(() => setLoading(false));
  }, [
    confirmationAttempted,
    confirmationToken,
    confirmationUserId,
    initialView,
    recoveryEmail,
  ]);

  const handleLogin = async (event: React.FormEvent) => {
    event.preventDefault();
    clearFeedback();
    setLoading(true);

    try {
      const response = await authApi.login({
        email: loginEmail,
        password: loginPassword,
      });

      if (!isMfaChallenge(response)) {
        finishLogin(response);
        return;
      }

      setMfaTicket(response.mfaTicket);
      setMfaCode("");
      if (response.requiresSetup) {
        const setup = await authApi.setupMfa(response.mfaTicket);
        setMfaSetup(setup);
        setView("mfa-setup");
      } else {
        setView("mfa-verify");
      }
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Login failed.");
    } finally {
      setLoading(false);
    }
  };

  const handleSignup = async (event: React.FormEvent) => {
    event.preventDefault();
    clearFeedback();
    const requiresReview = ROLES_REQUIRING_REVIEW.has(signupRole);
    if (requiresReview && !identityDocument) {
      setError("Identity document is required for this account type.");
      return;
    }

    setLoading(true);

    try {
      const response = await authApi.register({
        fullName: signupName,
        email: signupEmail,
        password: signupPassword,
        role: signupRole,
        identityDocumentUrl: requiresReview ? identityDocument?.url : undefined,
        identityDocumentFileName: requiresReview ? identityDocument?.fileName : undefined,
        identityDocumentContentType: requiresReview ? identityDocument?.contentType : undefined,
      });
      setConfirmationUserId(response.userId);
      setConfirmationToken(response.developmentConfirmationToken ?? "");
      setRecoveryEmail(response.email);
      setLoginEmail(response.email);
      setMessage(response.message);
      setView("confirm-email");
    } catch (requestError) {
      setError(requestError instanceof Error
        ? requestError.message
        : "Registration failed.");
    } finally {
      setLoading(false);
    }
  };

  const handleIdentityDocument = (files: FileList | null) => {
    const file = files?.[0];
    setIdentityDocument(null);
    if (!file) return;

    clearFeedback();
    if (!["application/pdf", "image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      setError("Identity document must be a PDF, JPEG, PNG, or WebP file.");
      return;
    }

    if (file.size > MAX_IDENTITY_DOCUMENT_BYTES) {
      setError("Identity document must be 5 MB or smaller.");
      return;
    }

    setIdentityDocumentLoading(true);
    const reader = new FileReader();
    reader.onload = () => {
      setIdentityDocument({
        url: String(reader.result ?? ""),
        fileName: file.name,
        contentType: file.type,
      });
      setIdentityDocumentLoading(false);
    };
    reader.onerror = () => {
      setError("Identity document could not be read.");
      setIdentityDocumentLoading(false);
    };
    reader.readAsDataURL(file);
  };

  const handleConfirmation = async () => {
    clearFeedback();
    setLoading(true);
    try {
      const response = await authApi.confirmEmail(
        confirmationUserId,
        confirmationToken,
      );
      setMessage(response.message);
      setConfirmationToken("");
    } catch (requestError) {
      setError(requestError instanceof Error
        ? requestError.message
        : "Email confirmation failed.");
    } finally {
      setLoading(false);
    }
  };

  const handleResendConfirmation = async () => {
    clearFeedback();
    setLoading(true);
    try {
      const response = await authApi.resendConfirmation(recoveryEmail);
      setMessage(response.message);
      if (response.developmentToken) {
        setConfirmationToken(response.developmentToken);
      }
    } catch (requestError) {
      setError(requestError instanceof Error
        ? requestError.message
        : "Could not resend confirmation.");
    } finally {
      setLoading(false);
    }
  };

  const handleForgotPassword = async (event: React.FormEvent) => {
    event.preventDefault();
    clearFeedback();
    setLoading(true);
    try {
      const response = await authApi.forgotPassword(recoveryEmail);
      setMessage(response.message);
      if (response.developmentToken) {
        setResetToken(response.developmentToken);
      }
    } catch (requestError) {
      setError(requestError instanceof Error
        ? requestError.message
        : "Password recovery failed.");
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (event: React.FormEvent) => {
    event.preventDefault();
    clearFeedback();
    if (newPassword !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }

    setLoading(true);
    try {
      const response = await authApi.resetPassword(
        recoveryEmail,
        resetToken,
        newPassword,
      );
      setLoginEmail(recoveryEmail);
      setLoginPassword("");
      setMessage(response.message);
      setView("login");
    } catch (requestError) {
      setError(requestError instanceof Error
        ? requestError.message
        : "Password reset failed.");
    } finally {
      setLoading(false);
    }
  };

  const handleMfa = async (event: React.FormEvent) => {
    event.preventDefault();
    clearFeedback();
    setLoading(true);

    try {
      if (view === "mfa-setup" && mfaSetup) {
        const response = await authApi.enableMfa(mfaSetup.mfaTicket, mfaCode);
        storeSession(response.authentication);
        setRecoveryCodes(response.recoveryCodes);
        setView("recovery-codes");
        return;
      }

      const session = await authApi.verifyMfa(
        mfaTicket,
        useRecoveryCode ? { recoveryCode: mfaCode } : { code: mfaCode },
      );
      finishLogin(session);
    } catch (requestError) {
      setError(
        `${requestError instanceof Error ? requestError.message : "MFA failed."} ` +
        "Sign in again to request a new one-time challenge.",
      );
      setView("login");
      setMfaCode("");
    } finally {
      setLoading(false);
    }
  };

  const feedback = (
    <>
      {error && (
        <div role="alert" className="mb-4 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          {error}
        </div>
      )}
      {message && (
        <div role="status" className="mb-4 rounded-lg border border-primary/30 bg-primary/10 p-3 text-sm text-foreground">
          {message}
        </div>
      )}
    </>
  );

  const submitButton = (label: string, loadingLabel: string) => (
    <button
      type="submit"
      disabled={loading}
      className="flex w-full items-center justify-center gap-2 rounded-lg bg-primary py-3 font-semibold text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
    >
      {loading && <Loader2 className="h-4 w-4 animate-spin" />}
      {loading ? loadingLabel : label}
    </button>
  );

  const renderContent = () => {
    if (view === "login") {
      return (
        <>
          <div className="mb-6 flex rounded-lg bg-secondary p-1">
            <button type="button" onClick={() => changeView("login")} className="flex-1 rounded-md bg-card py-2 text-sm font-medium shadow-sm">
              Login
            </button>
            <button type="button" onClick={() => changeView("signup")} className="flex-1 rounded-md py-2 text-sm font-medium text-muted-foreground">
              Sign Up
            </button>
          </div>
          {feedback}
          <form className="space-y-4" onSubmit={handleLogin}>
            <label className="block text-sm font-medium">
              Email
              <input type="email" required autoComplete="email" value={loginEmail} onChange={(event) => setLoginEmail(event.target.value)} placeholder="Enter your email" className={`${inputClass} mt-1`} />
            </label>
            <label className="block text-sm font-medium">
              Password
              <input type="password" required autoComplete="current-password" value={loginPassword} onChange={(event) => setLoginPassword(event.target.value)} placeholder="Enter your password" className={`${inputClass} mt-1`} />
            </label>
            <button type="button" onClick={() => { setRecoveryEmail(loginEmail); changeView("forgot-password"); }} className="text-xs text-primary hover:underline">
              Forgot password?
            </button>
            {submitButton("Login", "Signing in...")}
          </form>
        </>
      );
    }

    if (view === "signup") {
      const requiresReview = ROLES_REQUIRING_REVIEW.has(signupRole);
      return (
        <>
          <div className="mb-6 flex rounded-lg bg-secondary p-1">
            <button type="button" onClick={() => changeView("login")} className="flex-1 rounded-md py-2 text-sm font-medium text-muted-foreground">
              Login
            </button>
            <button type="button" onClick={() => changeView("signup")} className="flex-1 rounded-md bg-card py-2 text-sm font-medium shadow-sm">
              Sign Up
            </button>
          </div>
          {feedback}
          <form className="space-y-3" onSubmit={handleSignup}>
            <label className="block text-sm font-medium">
              Full Name
              <input required autoComplete="name" value={signupName} onChange={(event) => setSignupName(event.target.value)} placeholder="Enter your full name" className={`${inputClass} mt-1`} />
            </label>
            <label className="block text-sm font-medium">
              Email
              <input type="email" required autoComplete="email" value={signupEmail} onChange={(event) => setSignupEmail(event.target.value)} placeholder="Enter your email" className={`${inputClass} mt-1`} />
            </label>
            <label className="block text-sm font-medium">
              Password
              <input type="password" required minLength={10} autoComplete="new-password" value={signupPassword} onChange={(event) => setSignupPassword(event.target.value)} placeholder="At least 10 characters" className={`${inputClass} mt-1`} />
            </label>
            <label className="block text-sm font-medium">
              I am a...
              <select value={signupRole} onChange={(event) => setSignupRole(event.target.value as typeof signupRole)} className={`${inputClass} mt-1`}>
                {ROLES.map((role) => (
                  <option key={role} value={role}>
                    {roleLabel(role)}
                  </option>
                ))}
              </select>
            </label>
            {requiresReview && (
              <div className="rounded-lg border border-border bg-secondary/40 p-3">
                <label className="block text-sm font-medium">
                  Identity document
                  <span className="mt-1 flex items-center gap-2 rounded-lg border border-border bg-background px-3 py-2 text-sm">
                    {identityDocumentLoading
                      ? <Loader2 className="h-4 w-4 animate-spin text-primary" />
                      : <FileText className="h-4 w-4 text-primary" />}
                    <span className="min-w-0 flex-1 truncate text-muted-foreground">
                      {identityDocument?.fileName ?? "Upload PDF or image"}
                    </span>
                    <span className="text-xs text-primary">Choose</span>
                    <input
                      type="file"
                      required={requiresReview}
                      accept="application/pdf,image/jpeg,image/png,image/webp"
                      disabled={identityDocumentLoading}
                      onChange={(event) => handleIdentityDocument(event.target.files)}
                      className="sr-only"
                    />
                  </span>
                </label>
                <p className="mt-2 text-[11px] text-muted-foreground">
                  Admins review this before activating Local Buddy, Hotel Provider, and Experience Provider accounts.
                </p>
              </div>
            )}
            <button
              type="submit"
              disabled={loading || identityDocumentLoading}
              className="flex w-full items-center justify-center gap-2 rounded-lg bg-primary py-3 font-semibold text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
            >
              {loading && <Loader2 className="h-4 w-4 animate-spin" />}
              {loading ? "Creating account..." : "Create Account"}
            </button>
          </form>
        </>
      );
    }

    if (view === "confirm-email") {
      return (
        <div className="space-y-4">
          <div className="text-center">
            <ShieldCheck className="mx-auto mb-2 h-9 w-9 text-primary" />
            <h2 className="text-lg font-bold">Confirm your email</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Open the confirmation link sent to {recoveryEmail || "your email address"}.
            </p>
          </div>
          {feedback}
          {confirmationToken && confirmationUserId && (
            <button type="button" disabled={loading} onClick={handleConfirmation} className="w-full rounded-lg bg-primary py-2.5 font-medium text-primary-foreground disabled:opacity-50">
              Confirm email with development token
            </button>
          )}
          <label className="block text-sm font-medium">
            Email for resend
            <input type="email" required value={recoveryEmail} onChange={(event) => setRecoveryEmail(event.target.value)} className={`${inputClass} mt-1`} />
          </label>
          <button type="button" disabled={loading || !recoveryEmail} onClick={handleResendConfirmation} className="w-full rounded-lg border border-border py-2.5 text-sm font-medium disabled:opacity-50">
            Resend confirmation email
          </button>
          <button type="button" onClick={() => changeView("login")} className="w-full text-sm text-primary hover:underline">
            Back to login
          </button>
        </div>
      );
    }

    if (view === "forgot-password") {
      return (
        <div>
          <h2 className="mb-1 text-center text-lg font-bold">Reset your password</h2>
          <p className="mb-5 text-center text-sm text-muted-foreground">We will send a reset link if the account exists.</p>
          {feedback}
          <form className="space-y-4" onSubmit={handleForgotPassword}>
            <label className="block text-sm font-medium">
              Email
              <input type="email" required value={recoveryEmail} onChange={(event) => setRecoveryEmail(event.target.value)} className={`${inputClass} mt-1`} />
            </label>
            {submitButton("Send reset link", "Sending...")}
          </form>
          {resetToken && (
            <button type="button" onClick={() => changeView("reset-password")} className="mt-3 w-full rounded-lg border border-primary/40 py-2.5 text-sm text-primary">
              Continue with development reset token
            </button>
          )}
          <button type="button" onClick={() => changeView("login")} className="mt-4 w-full text-sm text-primary hover:underline">
            Back to login
          </button>
        </div>
      );
    }

    if (view === "reset-password") {
      return (
        <div>
          <h2 className="mb-5 text-center text-lg font-bold">Choose a new password</h2>
          {feedback}
          <form className="space-y-4" onSubmit={handleResetPassword}>
            <label className="block text-sm font-medium">
              Email
              <input type="email" required value={recoveryEmail} onChange={(event) => setRecoveryEmail(event.target.value)} className={`${inputClass} mt-1`} />
            </label>
            <label className="block text-sm font-medium">
              New password
              <input type="password" required minLength={10} autoComplete="new-password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} className={`${inputClass} mt-1`} />
            </label>
            <label className="block text-sm font-medium">
              Confirm password
              <input type="password" required minLength={10} autoComplete="new-password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} className={`${inputClass} mt-1`} />
            </label>
            {submitButton("Reset password", "Resetting...")}
          </form>
        </div>
      );
    }

    if (view === "mfa-setup" && mfaSetup) {
      return (
        <div>
          <KeyRound className="mx-auto mb-2 h-9 w-9 text-primary" />
          <h2 className="text-center text-lg font-bold">Secure your admin account</h2>
          <p className="mt-1 text-center text-sm text-muted-foreground">Add this key to your authenticator app, then enter its six-digit code.</p>
          <div className="my-4 break-all rounded-lg bg-secondary p-3 text-center font-mono text-sm">{mfaSetup.sharedKey}</div>
          {feedback}
          <form className="space-y-4" onSubmit={handleMfa}>
            <label className="block text-sm font-medium">
              Authenticator code
              <input inputMode="numeric" required value={mfaCode} onChange={(event) => setMfaCode(event.target.value)} placeholder="123456" className={`${inputClass} mt-1 text-center tracking-[0.4em]`} />
            </label>
            {submitButton("Enable MFA", "Verifying...")}
          </form>
        </div>
      );
    }

    if (view === "mfa-verify") {
      return (
        <div>
          <ShieldCheck className="mx-auto mb-2 h-9 w-9 text-primary" />
          <h2 className="text-center text-lg font-bold">Admin verification</h2>
          <p className="mb-5 mt-1 text-center text-sm text-muted-foreground">
            Enter your {useRecoveryCode ? "recovery code" : "authenticator code"}.
          </p>
          {feedback}
          <form className="space-y-4" onSubmit={handleMfa}>
            <input required autoFocus value={mfaCode} onChange={(event) => setMfaCode(event.target.value)} placeholder={useRecoveryCode ? "Recovery code" : "123456"} className={`${inputClass} text-center`} />
            {submitButton("Verify", "Verifying...")}
          </form>
          <button type="button" onClick={() => { setUseRecoveryCode((current) => !current); setMfaCode(""); }} className="mt-4 w-full text-sm text-primary hover:underline">
            {useRecoveryCode ? "Use authenticator code" : "Use a recovery code"}
          </button>
        </div>
      );
    }

    return (
      <div>
        <ShieldCheck className="mx-auto mb-2 h-9 w-9 text-primary" />
        <h2 className="text-center text-lg font-bold">Save your recovery codes</h2>
        <p className="mb-4 mt-1 text-center text-sm text-muted-foreground">Each code can be used once if you lose access to your authenticator.</p>
        <div className="grid grid-cols-2 gap-2 rounded-lg bg-secondary p-4 font-mono text-sm">
          {recoveryCodes.map((code) => <span key={code}>{code}</span>)}
        </div>
        <button type="button" onClick={() => navigate("/start", { replace: true })} className="mt-5 w-full rounded-lg bg-primary py-3 font-semibold text-primary-foreground">
          I saved these codes
        </button>
      </div>
    );
  };

  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center overflow-hidden bg-background px-4 py-10">
      <div className="pointer-events-none absolute inset-0 bg-glow-purple" />
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} className="relative z-10 w-full max-w-md">
        <div className="mb-8 text-center">
          <div className="mb-4 inline-flex h-12 w-12 items-center justify-center rounded-xl border border-primary/30 bg-primary/20">
            <Sparkles className="h-6 w-6 text-primary" />
          </div>
          <h1 className="text-gradient-purple text-3xl font-extrabold">Glinter</h1>
          <p className="mt-2 text-sm text-muted-foreground">Travel with intelligence.</p>
        </div>
        <div className="card-glass p-6">{renderContent()}</div>
        <p className="mt-5 text-center text-xs text-muted-foreground">© 2026 Glinter</p>
      </motion.div>
    </div>
  );
};

export default Auth;
