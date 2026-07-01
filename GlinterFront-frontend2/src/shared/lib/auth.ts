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

const readSessionValue = (key: string) => {
  const current = sessionStorage.getItem(key);
  if (current != null) return current;

  // One-time migration from the older persistent storage. Tokens should not
  // survive a complete browser session or remain available to other tabs.
  const legacy = localStorage.getItem(key);
  if (legacy != null) {
    sessionStorage.setItem(key, legacy);
    localStorage.removeItem(key);
  }
  return legacy;
};

const writeSessionValue = (key: string, value: string) => {
  sessionStorage.setItem(key, value);
  localStorage.removeItem(key);
};

const removeSessionValue = (key: string) => {
  sessionStorage.removeItem(key);
  localStorage.removeItem(key);
};

export const authStorage = {
  getToken: (): string | null => {
    return readSessionValue(TOKEN_KEY);
  },

  setToken: (token: string): void => {
    writeSessionValue(TOKEN_KEY, token);
  },

  getRefreshToken: (): string | null => {
    return readSessionValue(REFRESH_TOKEN_KEY);
  },

  setTokens: (
    token: string,
    refreshToken: string,
    refreshTokenExpiresAtUtc: string,
  ): void => {
    writeSessionValue(TOKEN_KEY, token);
    writeSessionValue(REFRESH_TOKEN_KEY, refreshToken);
    writeSessionValue(REFRESH_TOKEN_EXPIRES_AT_KEY, refreshTokenExpiresAtUtc);
  },

  removeToken: (): void => {
    removeSessionValue(TOKEN_KEY);
  },

  isAuthenticated: (): boolean => {
    return !!readSessionValue(TOKEN_KEY);
  },

  setUser: (user: CurrentUserResponse): void => {
    writeSessionValue(USER_KEY, JSON.stringify(user));
  },

  getUser: (): CurrentUserResponse | null => {
    const user = readSessionValue(USER_KEY);
    if (!user) return null;

    try {
      return JSON.parse(user) as CurrentUserResponse;
    } catch {
      return null;
    }
  },

  clearUser: (): void => {
    removeSessionValue(USER_KEY);
  },

  isAdmin: (): boolean => {
    return authStorage.hasAnyRole(["Admin"]);
  },

  hasAnyRole: (allowedRoles: readonly AppRole[]): boolean => {
    const roles = authStorage.getUser()?.roles ?? [];
    return allowedRoles.some((role) => roles.includes(role));
  },

  clearAll: (): void => {
    removeSessionValue(TOKEN_KEY);
    removeSessionValue(REFRESH_TOKEN_KEY);
    removeSessionValue(REFRESH_TOKEN_EXPIRES_AT_KEY);
    removeSessionValue(USER_KEY);
  },
};
