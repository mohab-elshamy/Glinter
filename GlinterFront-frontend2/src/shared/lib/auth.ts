import type { CurrentUserResponse } from "@/shared/services/api-auth";

export type AppRole =
  | "Traveler"
  | "LocalBuddy"
  | "HotelOwner"
  | "ExperienceProvider"
  | "Admin";

const TOKEN_KEY = "token";
const REFRESH_TOKEN_KEY = "refreshToken";
const REFRESH_TOKEN_EXPIRES_AT_KEY = "refreshTokenExpiresAtUtc";
const USER_KEY = "user";

export const authStorage = {
  getToken: (): string | null => {
    return localStorage.getItem(TOKEN_KEY);
  },

  setToken: (token: string): void => {
    localStorage.setItem(TOKEN_KEY, token);
  },

  getRefreshToken: (): string | null => {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },

  setTokens: (
    token: string,
    refreshToken: string,
    refreshTokenExpiresAtUtc: string,
  ): void => {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
    localStorage.setItem(REFRESH_TOKEN_EXPIRES_AT_KEY, refreshTokenExpiresAtUtc);
  },

  removeToken: (): void => {
    localStorage.removeItem(TOKEN_KEY);
  },

  isAuthenticated: (): boolean => {
    return !!localStorage.getItem(TOKEN_KEY);
  },

  setUser: (user: CurrentUserResponse): void => {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  getUser: (): CurrentUserResponse | null => {
    const user = localStorage.getItem(USER_KEY);
    if (!user) return null;

    try {
      return JSON.parse(user) as CurrentUserResponse;
    } catch {
      return null;
    }
  },

  clearUser: (): void => {
    localStorage.removeItem(USER_KEY);
  },

  isAdmin: (): boolean => {
    return authStorage.hasAnyRole(["Admin"]);
  },

  hasAnyRole: (allowedRoles: readonly AppRole[]): boolean => {
    const roles = authStorage.getUser()?.roles ?? [];
    return allowedRoles.some((role) => roles.includes(role));
  },

  clearAll: (): void => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_EXPIRES_AT_KEY);
    localStorage.removeItem(USER_KEY);
  },
};
