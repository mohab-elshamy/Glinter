import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { profilesApi } from "./api-profiles";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("profilesApi", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("unwraps and continues paged buddy results", async () => {
    const firstPage = Array.from({ length: 50 }, (_, index) => ({
      userId: `buddy-${index}`,
    }));
    vi.mocked(request)
      .mockResolvedValueOnce({
        page: 1,
        pageSize: 50,
        totalCount: 51,
        items: firstPage,
      })
      .mockResolvedValueOnce({
        page: 2,
        pageSize: 50,
        totalCount: 51,
        items: [{ userId: "buddy-50" }],
      });

    const buddies = await profilesApi.getLocalBuddies();

    expect(buddies).toHaveLength(51);
    expect(request).toHaveBeenNthCalledWith(1, "/local-buddies?page=1&pageSize=50");
    expect(request).toHaveBeenNthCalledWith(2, "/local-buddies?page=2&pageSize=50");
  });

  it("uses backend follow state rather than local favorites", async () => {
    vi.mocked(request).mockResolvedValue({ isFollowing: true, followersCount: 4 });
    await profilesApi.followUser("buddy-id");
    expect(request).toHaveBeenCalledWith("/profiles/users/buddy-id/follow", {
      method: "POST",
    });

    await profilesApi.unfollowUser("buddy-id");
    expect(request).toHaveBeenLastCalledWith("/profiles/users/buddy-id/follow", {
      method: "DELETE",
    });
  });

  it("uploads profile images using multipart form data", async () => {
    const file = new File(["image"], "profile.png", { type: "image/png" });
    await profilesApi.uploadProfileImage(file);
    const call = vi.mocked(request).mock.calls.at(-1);
    expect(call?.[0]).toBe("/profiles/images");
    expect(call?.[1]).toMatchObject({ method: "POST" });
    expect(call?.[1]?.body).toBeInstanceOf(FormData);
  });

  it("persists experience favorites through Profiles", async () => {
    await profilesApi.getExperienceFavoriteIds();
    expect(request).toHaveBeenLastCalledWith("/profiles/experience-favorites");

    await profilesApi.addExperienceFavorite(14);
    expect(request).toHaveBeenLastCalledWith("/profiles/experience-favorites/14", {
      method: "PUT",
    });

    await profilesApi.removeExperienceFavorite(14);
    expect(request).toHaveBeenLastCalledWith("/profiles/experience-favorites/14", {
      method: "DELETE",
    });
  });
});
