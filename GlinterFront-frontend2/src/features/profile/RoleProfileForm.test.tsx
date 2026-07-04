// @vitest-environment jsdom

import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { profilesApi, type MyProfileResponse } from "@/shared/services/api-profiles";
import type { LocalBuddyProfileResponse } from "@/shared/types/api";
import RoleProfileForm from "./RoleProfileForm";

vi.mock("@/shared/services/api-profiles", () => ({
  profilesApi: {
    getInterests: vi.fn(),
    updateTravelerProfile: vi.fn(),
    updateLocalBuddyProfile: vi.fn(),
    updateHotelOwnerProfile: vi.fn(),
    updateExperienceProviderProfile: vi.fn(),
    updateProfileImage: vi.fn(),
  },
}));

const renderForm = (
  role: "Traveler" | "LocalBuddy" | "HotelOwner" | "ExperienceProvider",
  initialProfile?: MyProfileResponse,
) => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const onSaved = vi.fn();
  render(
    <QueryClientProvider client={queryClient}>
      <RoleProfileForm
        role={role}
        defaultName="Test User"
        initialProfile={initialProfile}
        submitLabel="Save profile"
        onSaved={onSaved}
      />
    </QueryClientProvider>,
  );
  return onSaved;
};

describe("role profile form", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(profilesApi.getInterests).mockResolvedValue([
      { id: "interest-culture", name: "Culture" },
      { id: "interest-food", name: "Food" },
    ]);
  });

  afterEach(cleanup);

  it("loads backend interests and sends their IDs for a traveler", async () => {
    const savedProfile: MyProfileResponse = {
      profileId: "profile-1",
      userId: "user-1",
      profileType: "Traveler",
      displayName: "Test User",
      preferredBudgetLevel: "Mid-range",
      travelStyle: "Solo",
      interests: [{ id: "interest-culture", name: "Culture" }],
      followersCount: 0,
      followingCount: 0,
      createdAtUtc: "2026-06-30T00:00:00Z",
    };
    vi.mocked(profilesApi.updateTravelerProfile).mockResolvedValue(savedProfile);
    const onSaved = renderForm("Traveler");

    const culture = await screen.findByRole("button", { name: "Culture" });
    fireEvent.click(culture);
    fireEvent.click(screen.getByRole("button", { name: "Save profile" }));

    await waitFor(() => {
      expect(profilesApi.updateTravelerProfile).toHaveBeenCalledWith(
        expect.objectContaining({
          interestIds: ["interest-culture"],
          preferredInterests: "Culture",
          preferredBudgetLevel: "MidRange",
        }),
      );
    });
    expect(onSaved).toHaveBeenCalledWith(savedProfile);
  });

  it("renders and saves all five canonical traveler budget levels", async () => {
    const savedProfile: MyProfileResponse = {
      profileId: "profile-budget",
      userId: "user-budget",
      profileType: "Traveler",
      displayName: "Test User",
      preferredBudgetLevel: "Upscale",
      interests: [],
      followersCount: 0,
      followingCount: 0,
      createdAtUtc: "2026-07-02T00:00:00Z",
    };
    vi.mocked(profilesApi.updateTravelerProfile).mockResolvedValue(savedProfile);
    renderForm("Traveler");

    const select = screen.getByLabelText("Preferred budget");
    await screen.findByRole("button", { name: "Culture" });
    expect(Array.from((select as HTMLSelectElement).options).map((option) => option.text))
      .toEqual(["Budget", "Economy", "Mid-range", "Upscale", "Luxury"]);
    fireEvent.change(select, { target: { value: "4" } });
    fireEvent.click(screen.getByRole("button", { name: "Save profile" }));

    await waitFor(() => expect(profilesApi.updateTravelerProfile).toHaveBeenCalledWith(
      expect.objectContaining({ preferredBudgetLevel: "Upscale" }),
    ));
  });

  it("edits a local buddy with existing backend interests", async () => {
    const initialProfile: LocalBuddyProfileResponse = {
      profileId: "profile-2",
      userId: "user-2",
      profileType: "LocalBuddy",
      displayName: "Cairo Buddy",
      city: "Cairo",
      rating: 0,
      reviewsCount: 0,
      verificationStatus: "Pending",
      interests: [{ id: "interest-food", name: "Food" }],
      followersCount: 0,
      followingCount: 0,
      isFollowing: false,
      createdAtUtc: "2026-06-30T00:00:00Z",
    };
    vi.mocked(profilesApi.updateLocalBuddyProfile).mockResolvedValue(initialProfile);
    renderForm("LocalBuddy", initialProfile);

    expect(await screen.findByRole("button", { name: "Food" }))
      .toHaveAttribute("aria-pressed", "true");
    fireEvent.change(screen.getByLabelText("City"), {
      target: { value: "Giza" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Save profile" }));

    await waitFor(() => {
      expect(profilesApi.updateLocalBuddyProfile).toHaveBeenCalledWith(
        expect.objectContaining({
          displayName: "Cairo Buddy",
          city: "Giza",
          interestIds: ["interest-food"],
        }),
      );
    });
  });

  it("normalizes local buddy languages into chips and a stable API string", async () => {
    const savedProfile: LocalBuddyProfileResponse = {
      profileId: "profile-languages",
      userId: "user-languages",
      profileType: "LocalBuddy",
      displayName: "Cairo Buddy",
      city: "Cairo",
      languages: "Arabic, English",
      rating: 0,
      reviewsCount: 0,
      verificationStatus: "Approved",
      interests: [],
      followersCount: 0,
      followingCount: 0,
      isFollowing: false,
      createdAtUtc: "2026-07-04T00:00:00Z",
    };
    vi.mocked(profilesApi.updateLocalBuddyProfile).mockResolvedValue(savedProfile);
    renderForm("LocalBuddy", savedProfile);

    expect(screen.getByText("Arabic")).toBeInTheDocument();
    const input = screen.getByLabelText("Languages");
    fireEvent.change(input, { target: { value: "arabic" } });
    fireEvent.keyDown(input, { key: "Enter" });
    fireEvent.change(input, { target: { value: "French" } });
    fireEvent.keyDown(input, { key: "Enter" });
    await screen.findByRole("button", { name: "Culture" });
    fireEvent.click(screen.getByRole("button", { name: "Save profile" }));

    await waitFor(() => expect(profilesApi.updateLocalBuddyProfile)
      .toHaveBeenCalledWith(expect.objectContaining({
        languages: "Arabic, English, French",
      })));
  });

  it("synchronizes form state when an asynchronously loaded profile changes", () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const first: MyProfileResponse = {
      profileId: "profile-first",
      userId: "user-1",
      profileType: "Traveler",
      displayName: "First Name",
      interests: [],
      followersCount: 0,
      followingCount: 0,
      createdAtUtc: "2026-07-04T00:00:00Z",
    };
    const second = { ...first, displayName: "Loaded Name", nationality: "Egyptian" };
    const view = render(
      <QueryClientProvider client={queryClient}>
        <RoleProfileForm
          role="Traveler"
          defaultName="Account Name"
          initialProfile={first}
          submitLabel="Save profile"
          onSaved={() => undefined}
        />
      </QueryClientProvider>,
    );

    view.rerender(
      <QueryClientProvider client={queryClient}>
        <RoleProfileForm
          role="Traveler"
          defaultName="Account Name"
          initialProfile={second}
          submitLabel="Save profile"
          onSaved={() => undefined}
        />
      </QueryClientProvider>,
    );
    expect(screen.getByLabelText("Display name")).toHaveValue("Loaded Name");
    expect(screen.getByLabelText("Nationality")).toHaveValue("Egyptian");
  });

  it.each([
    ["HotelOwner", "updateHotelOwnerProfile"],
    ["ExperienceProvider", "updateExperienceProviderProfile"],
  ] as const)("uses the %s profile endpoint", async (role, method) => {
    const savedProfile: MyProfileResponse = {
      profileId: `profile-${role}`,
      userId: `user-${role}`,
      profileType: role,
      businessName: "Glinter Business",
      followersCount: 0,
      followingCount: 0,
      createdAtUtc: "2026-06-30T00:00:00Z",
    };
    vi.mocked(profilesApi[method]).mockResolvedValue(savedProfile);
    renderForm(role);

    fireEvent.change(screen.getByLabelText("Business name"), {
      target: { value: "Glinter Business" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Save profile" }));

    await waitFor(() => {
      expect(profilesApi[method]).toHaveBeenCalledWith(
        expect.objectContaining({ businessName: "Glinter Business" }),
      );
    });
  });
});
