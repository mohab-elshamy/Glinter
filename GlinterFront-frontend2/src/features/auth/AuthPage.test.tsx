// @vitest-environment jsdom

import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import Auth from "./AuthPage";
import { authApi } from "@/shared/services/api-auth";

vi.mock("@/shared/services/api-auth", () => ({
  authApi: {
    register: vi.fn(),
    login: vi.fn(),
    confirmEmail: vi.fn(),
    resendConfirmation: vi.fn(),
    forgotPassword: vi.fn(),
    resetPassword: vi.fn(),
    setupMfa: vi.fn(),
    enableMfa: vi.fn(),
    verifyMfa: vi.fn(),
  },
  isMfaChallenge: (response: Record<string, unknown>) => "requiresMfa" in response,
}));

const renderAuth = () => render(
  <MemoryRouter initialEntries={["/auth"]}>
    <Routes>
      <Route path="/auth" element={<Auth />} />
      <Route path="/explore" element={<h1>Explore Complete</h1>} />
      <Route path="/start" element={<h1>Start Complete</h1>} />
      <Route path="/admin" element={<h1>Admin Complete</h1>} />
    </Routes>
  </MemoryRouter>,
);

describe("authentication page", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
    localStorage.clear();
  });

  it("registers without creating a session and asks for email confirmation", async () => {
    vi.mocked(authApi.register).mockResolvedValueOnce({
      userId: "user-1",
      email: "new@example.com",
      message: "Registration succeeded. Confirm your email before signing in.",
      developmentConfirmationToken: "confirmation-token",
    });
    vi.mocked(authApi.confirmEmail).mockResolvedValueOnce({
      message: "Email confirmed successfully.",
    });
    renderAuth();

    fireEvent.click(screen.getByRole("button", { name: "Sign Up" }));
    fireEvent.change(screen.getByLabelText("Full Name"), {
      target: { value: "New Traveler" },
    });
    fireEvent.change(screen.getByLabelText("Email"), {
      target: { value: "new@example.com" },
    });
    fireEvent.change(screen.getByLabelText("Password"), {
      target: { value: "ValidPassword!2026" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Create Account" }));

    await screen.findByRole("heading", { name: "Confirm your email" });
    expect(localStorage.getItem("token")).toBeNull();
    expect(screen.getByRole("button", {
      name: "Confirm email with development token",
    })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", {
      name: "Confirm email with development token",
    }));
    await waitFor(() => {
      expect(authApi.confirmEmail).toHaveBeenCalledWith(
        "user-1",
        "confirmation-token",
      );
    });
    expect(screen.queryByText("Sign in with Google")).not.toBeInTheDocument();
  });

  it("stores the complete session after a normal login", async () => {
    vi.mocked(authApi.login).mockResolvedValueOnce({
      userId: "user-1",
      fullName: "Existing Traveler",
      email: "traveler@example.com",
      roles: ["Traveler"],
      token: "access-token",
      refreshToken: "refresh-token",
      refreshTokenExpiresAtUtc: "2026-07-29T00:00:00Z",
    });
    renderAuth();

    fireEvent.change(screen.getByLabelText("Email"), {
      target: { value: "traveler@example.com" },
    });
    fireEvent.change(screen.getByLabelText("Password"), {
      target: { value: "ValidPassword!2026" },
    });
    const passwordForm = screen.getByLabelText("Password").closest("form");
    expect(passwordForm).not.toBeNull();
    fireEvent.submit(passwordForm!);

    await screen.findByRole("heading", { name: "Start Complete" });
    expect(localStorage.getItem("token")).toBe("access-token");
    expect(localStorage.getItem("refreshToken")).toBe("refresh-token");
  });

  it("moves an admin login into MFA setup without creating a session", async () => {
    vi.mocked(authApi.login).mockResolvedValueOnce({
      requiresMfa: true,
      requiresSetup: true,
      mfaTicket: "setup-ticket",
      expiresAtUtc: "2026-06-29T21:30:00Z",
    });
    vi.mocked(authApi.setupMfa).mockResolvedValueOnce({
      sharedKey: "AUTHENTICATORKEY",
      authenticatorUri: "otpauth://totp/Glinter:admin",
      mfaTicket: "enable-ticket",
      expiresAtUtc: "2026-06-29T21:30:00Z",
    });
    renderAuth();

    fireEvent.change(screen.getByLabelText("Email"), {
      target: { value: "admin@example.com" },
    });
    fireEvent.change(screen.getByLabelText("Password"), {
      target: { value: "AdminPassword!2026" },
    });
    fireEvent.submit(screen.getByLabelText("Password").closest("form")!);

    await waitFor(() => {
      expect(screen.getByRole("heading", {
        name: "Secure your admin account",
      })).toBeInTheDocument();
    });
    expect(screen.getByText("AUTHENTICATORKEY")).toBeInTheDocument();
    expect(localStorage.getItem("token")).toBeNull();
  });

  it("supports development password recovery and reset", async () => {
    vi.mocked(authApi.forgotPassword).mockResolvedValueOnce({
      message: "If the account exists, a password-reset email has been sent.",
      developmentToken: "reset-token",
    });
    vi.mocked(authApi.resetPassword).mockResolvedValueOnce({
      message: "Password reset successfully.",
    });
    renderAuth();

    fireEvent.click(screen.getByRole("button", { name: "Forgot password?" }));
    fireEvent.change(screen.getByLabelText("Email"), {
      target: { value: "traveler@example.com" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Send reset link" }));

    const continueButton = await screen.findByRole("button", {
      name: "Continue with development reset token",
    });
    fireEvent.click(continueButton);
    fireEvent.change(screen.getByLabelText("New password"), {
      target: { value: "ReplacementPassword!2026" },
    });
    fireEvent.change(screen.getByLabelText("Confirm password"), {
      target: { value: "ReplacementPassword!2026" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Reset password" }));

    await waitFor(() => {
      expect(authApi.resetPassword).toHaveBeenCalledWith(
        "traveler@example.com",
        "reset-token",
        "ReplacementPassword!2026",
      );
    });
  });
});
