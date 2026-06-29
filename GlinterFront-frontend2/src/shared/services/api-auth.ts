import { request } from "@/shared/lib/api-client";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  role: "Traveler" | "LocalBuddy" | "HotelOwner" | "ExperienceProvider";
}

export interface AuthResponse {
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
  token: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

export interface RegisterResponse {
  userId: string;
  email: string;
  message: string;
  developmentConfirmationToken?: string | null;
}

export interface MessageResponse {
  message: string;
  developmentToken?: string | null;
}

export interface MfaChallengeResponse {
  requiresMfa: true;
  requiresSetup: boolean;
  mfaTicket: string;
  expiresAtUtc: string;
}

export interface MfaSetupResponse {
  sharedKey: string;
  authenticatorUri: string;
  mfaTicket: string;
  expiresAtUtc: string;
}

export interface EnableMfaResponse {
  authentication: AuthResponse;
  recoveryCodes: string[];
}

export interface CurrentUserResponse {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
}

export type LoginResponse = AuthResponse | MfaChallengeResponse;

export const isMfaChallenge = (
  response: LoginResponse,
): response is MfaChallengeResponse => "requiresMfa" in response;

export const authApi = {
  register: (data: RegisterRequest) =>
    request<RegisterResponse>("/auth/register", { method: "POST", body: data }),

  login: (data: LoginRequest) =>
    request<LoginResponse>("/auth/login", { method: "POST", body: data }),

  confirmEmail: (userId: string, token: string) =>
    request<MessageResponse>("/auth/confirm-email", {
      method: "POST",
      body: { userId, token },
    }),

  resendConfirmation: (email: string) =>
    request<MessageResponse>("/auth/resend-confirmation", {
      method: "POST",
      body: { email },
    }),

  forgotPassword: (email: string) =>
    request<MessageResponse>("/auth/forgot-password", {
      method: "POST",
      body: { email },
    }),

  resetPassword: (email: string, token: string, newPassword: string) =>
    request<MessageResponse>("/auth/reset-password", {
      method: "POST",
      body: { email, token, newPassword },
    }),

  refresh: (refreshToken: string) =>
    request<AuthResponse>("/auth/refresh", {
      method: "POST",
      body: { refreshToken },
    }),

  setupMfa: (mfaTicket: string) =>
    request<MfaSetupResponse>("/auth/mfa/setup", {
      method: "POST",
      body: { mfaTicket },
    }),

  enableMfa: (mfaTicket: string, code: string) =>
    request<EnableMfaResponse>("/auth/mfa/enable", {
      method: "POST",
      body: { mfaTicket, code },
    }),

  verifyMfa: (
    mfaTicket: string,
    verification: { code?: string; recoveryCode?: string },
  ) =>
    request<AuthResponse>("/auth/mfa/verify", {
      method: "POST",
      body: { mfaTicket, ...verification },
    }),

  me: () =>
    request<CurrentUserResponse>("/auth/me"),

  logout: () =>
    request<{ message: string }>("/auth/logout", { method: "POST" }),
};
