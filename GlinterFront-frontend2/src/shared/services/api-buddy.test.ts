import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { buddyApi } from "./api-buddy";

vi.mock("@/shared/lib/api-client", () => ({ request: vi.fn() }));

describe("buddyApi", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("creates a request with the authenticated traveler implied", async () => {
    vi.mocked(request).mockResolvedValue({});
    await buddyApi.createRequest("buddy-id", "slot-id", "Museum");
    expect(request).toHaveBeenCalledWith("/buddy/requests", {
      method: "POST",
      body: {
        localBuddyUserId: "buddy-id",
        availabilityId: "slot-id",
        notes: "Museum",
      },
    });
  });

  it("uses separate accept and reject routes", async () => {
    vi.mocked(request).mockResolvedValue({});
    await buddyApi.accept("request-id");
    expect(request).toHaveBeenLastCalledWith(
      "/buddy/requests/request-id/accept",
      { method: "PATCH" },
    );
    await buddyApi.reject("request-id");
    expect(request).toHaveBeenLastCalledWith(
      "/buddy/requests/request-id/reject",
      { method: "PATCH" },
    );
  });

  it("submits reviews to the request route without a user ID", async () => {
    vi.mocked(request).mockResolvedValue({});
    await buddyApi.createReview("request-id", {
      rating: 5,
      reviewText: "Excellent guide",
    });
    expect(request).toHaveBeenCalledWith(
      "/buddy/requests/request-id/review",
      {
        method: "POST",
        body: { rating: 5, reviewText: "Excellent guide" },
      },
    );
  });

  it("loads mine and incoming through canonical routes", async () => {
    vi.mocked(request).mockResolvedValue([]);
    await buddyApi.getMine();
    expect(request).toHaveBeenLastCalledWith("/buddy/requests/mine");
    await buddyApi.getIncoming();
    expect(request).toHaveBeenLastCalledWith("/buddy/requests/incoming");
  });

  it("propagates ProblemDetails errors from the shared client", async () => {
    const problem = new Error("This buddy request is already final.");
    let rejectRequest!: (reason: unknown) => void;
    vi.mocked(request).mockImplementation(() => new Promise((_, reject) => {
      rejectRequest = reject;
    }));
    const assertion = expect(
      buddyApi.accept("request-id"),
    ).rejects.toThrow("This buddy request is already final.");
    rejectRequest(problem);
    await assertion;
  });
});
