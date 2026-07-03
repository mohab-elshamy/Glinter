// @vitest-environment jsdom

import "@testing-library/jest-dom/vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/shared/lib/api-client";
import { authStorage } from "@/shared/lib/auth";
import {
  AuthenticatedLanding,
  RequireAuth,
  RequireRole,
} from "./RouteGuards";
import { profilesApi } from "@/shared/services/api-profiles";

vi.mock("@/shared/services/api-profiles", () => ({
  profilesApi: {
    getMyProfile: vi.fn(),
  },
}));

const LocationResult = ({ label }: { label: string }) => {
  const location = useLocation();
  return <h1>{`${label}${location.search}`}</h1>;
};

const renderRouting = (initialEntry: string, protectedElement: React.ReactNode) => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/protected" element={protectedElement} />
          <Route path="/start" element={<RequireAuth><AuthenticatedLanding /></RequireAuth>} />
          <Route path="/auth" element={<h1>Authentication</h1>} />
          <Route path="/profile/setup" element={<h1>Profile Setup</h1>} />
          <Route path="/profile/me" element={<LocationResult label="Provider Account" />} />
          <Route path="/dashboard" element={<h1>Traveler Dashboard</h1>} />
          <Route path="/admin" element={<h1>Admin Dashboard</h1>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
};

const setSession = (role: string) => {
  authStorage.setTokens("token", "refresh", "2026-07-29T00:00:00Z");
  authStorage.setUser({
    userId: `user-${role}`,
    fullName: role,
    email: `${role.toLowerCase()}@example.com`,
    roles: [role],
    isActive: true,
  });
};

describe("route guards", () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
    localStorage.clear();
    sessionStorage.clear();
  });

  it("redirects unauthenticated visitors to login", async () => {
    renderRouting(
      "/protected",
      <RequireAuth><h1>Protected</h1></RequireAuth>,
    );

    expect(await screen.findByRole("heading", { name: "Authentication" }))
      .toBeInTheDocument();
  });

  it("redirects the wrong role through the account landing route", async () => {
    setSession("Traveler");
    vi.mocked(profilesApi.getMyProfile).mockResolvedValueOnce({
      profileId: "profile-1",
      userId: "user-Traveler",
      profileType: "Traveler",
      displayName: "Traveler",
      interests: [],
      followersCount: 0,
      followingCount: 0,
      createdAtUtc: "2026-06-29T00:00:00Z",
    });

    renderRouting(
      "/protected",
      <RequireAuth>
        <RequireRole roles={["Admin"]}><h1>Admin Only</h1></RequireRole>
      </RequireAuth>,
    );

    expect(await screen.findByRole("heading", { name: "Traveler Dashboard" }))
      .toBeInTheDocument();
  });

  it("sends an account without a profile to profile setup", async () => {
    setSession("Traveler");
    vi.mocked(profilesApi.getMyProfile).mockRejectedValueOnce(
      new ApiError("Profile not found.", 404, "not_found"),
    );

    renderRouting("/start", <></>);

    expect(await screen.findByRole("heading", { name: "Profile Setup" }))
      .toBeInTheDocument();
  });

  it("sends a provider with a profile to the correct management tab", async () => {
    setSession("HotelOwner");
    vi.mocked(profilesApi.getMyProfile).mockResolvedValueOnce({
      profileId: "profile-1",
      userId: "user-HotelOwner",
      profileType: "HotelOwner",
      businessName: "Glinter Hotel",
      followersCount: 0,
      followingCount: 0,
      createdAtUtc: "2026-06-29T00:00:00Z",
    });

    renderRouting("/start", <></>);

    expect(await screen.findByRole("heading", {
      name: "Provider Account?tab=hotels",
    })).toBeInTheDocument();
  });
});
