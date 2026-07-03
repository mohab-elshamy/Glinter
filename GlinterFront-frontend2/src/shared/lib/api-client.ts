import { apiActivity } from "./api-activity";
import { authStorage } from "./auth";

const DEFAULT_API_ORIGIN = "http://localhost:5160";

export const API_ORIGIN = (
  import.meta.env.VITE_API_ORIGIN || DEFAULT_API_ORIGIN
).replace(/\/+$/, "");

const API_BASE_URL = `${API_ORIGIN}/api`;

type RequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  headers?: Record<string, string>;
};

export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
  message?: string;
}

export interface ApiFailureEventDetail {
  status: number;
  errorCode: string;
  message: string;
  traceId?: string;
}

interface RefreshResponse {
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
  token: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

export class ApiError extends Error {
  readonly status: number;
  readonly errorCode: string;
  readonly traceId?: string;
  readonly validationErrors?: Record<string, string[]>;

  constructor(
    message: string,
    status: number,
    errorCode = "request_failed",
    traceId?: string,
    validationErrors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errorCode = errorCode;
    this.traceId = traceId;
    this.validationErrors = validationErrors;
  }
}

const readProblemDetails = async (response: Response): Promise<ApiError> => {
  let problem: ApiProblemDetails = {};

  try {
    const body = await response.text();
    problem = body ? JSON.parse(body) as ApiProblemDetails : {};
  } catch {
    // Non-JSON error responses still receive a useful status-based message.
  }

  const defaultMessage = response.status === 403
    ? "You do not have permission to perform this action."
    : `Request failed with HTTP ${response.status}.`;

  return new ApiError(
    problem.detail || problem.title || problem.message || defaultMessage,
    response.status,
    problem.errorCode || (response.status === 403 ? "forbidden" : "request_failed"),
    problem.traceId,
    problem.errors,
  );
};

let refreshPromise: Promise<string> | null = null;

const refreshAccessToken = async (): Promise<string> => {
  if (refreshPromise) return refreshPromise;

  const refreshToken = authStorage.getRefreshToken();
  if (!refreshToken) {
    throw new ApiError(
      "Your session has expired. Please sign in again.",
      401,
      "session_expired",
    );
  }

  refreshPromise = (async () => {
    let response: Response;

    try {
      response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ refreshToken }),
      });
    } catch {
      throw new ApiError(
        "Unable to reach the server. Check your connection and try again.",
        0,
        "network_error",
      );
    }

    if (!response.ok) throw await readProblemDetails(response);

    const session = await response.json() as RefreshResponse;
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
    });

    return session.token;
  })().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
};

const refreshExcludedPaths = new Set([
  "/auth/register",
  "/auth/login",
  "/auth/confirm-email",
  "/auth/resend-confirmation",
  "/auth/forgot-password",
  "/auth/reset-password",
  "/auth/refresh",
  "/auth/mfa/setup",
  "/auth/mfa/enable",
  "/auth/mfa/verify",
]);

const expireSession = () => {
  authStorage.clearAll();
  window.dispatchEvent(new CustomEvent("auth-session-expired"));
};

const reportGlobalFailure = (error: ApiError) => {
  if (error.status !== 0 && error.status !== 403) return;

  const detail: ApiFailureEventDetail = {
    status: error.status,
    errorCode: error.errorCode,
    message: error.message,
    traceId: error.traceId,
  };
  window.dispatchEvent(new CustomEvent("api-request-error", { detail }));
};

const executeRequest = async <T>(
  url: string,
  options: RequestOptions,
  allowRefresh: boolean,
): Promise<T> => {
  const { method = "GET", body, headers = {} } = options;
  const requestAccessToken = authStorage.getToken();
  const requestHeaders: Record<string, string> = {
    Accept: "application/json",
    ...(requestAccessToken
      ? { Authorization: `Bearer ${requestAccessToken}` }
      : {}),
    ...headers,
  };

  const config: RequestInit = {
    method,
    headers: requestHeaders,
  };

  if (body !== undefined) {
    if (body instanceof FormData) {
      config.body = body;
    } else {
      requestHeaders["Content-Type"] = "application/json";
      config.body = JSON.stringify(body);
    }
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${url}`, config);
  } catch {
    throw new ApiError(
      "Unable to reach the server. Check your connection and try again.",
      0,
      "network_error",
    );
  }

  if (
    response.status === 401 &&
    allowRefresh &&
    !refreshExcludedPaths.has(url) &&
    requestAccessToken
  ) {
    if (authStorage.getToken() !== requestAccessToken) {
      return executeRequest<T>(url, options, false);
    }

    try {
      await refreshAccessToken();
      return executeRequest<T>(url, options, false);
    } catch (error) {
      if (error instanceof ApiError && error.errorCode === "network_error") {
        throw error;
      }

      expireSession();
      throw new ApiError(
        "Your session has expired. Please sign in again.",
        401,
        "session_expired",
      );
    }
  }

  if (!response.ok) throw await readProblemDetails(response);
  if (response.status === 204) return undefined as T;

  return response.json() as Promise<T>;
};

export const request = async <T>(
  url: string,
  options: RequestOptions = {},
): Promise<T> => {
  const endActivity = apiActivity.begin();

  try {
    return await executeRequest<T>(url, options, true);
  } catch (error) {
    if (error instanceof ApiError) reportGlobalFailure(error);
    throw error;
  } finally {
    endActivity();
  }
};

export default API_BASE_URL;
