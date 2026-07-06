// @vitest-environment jsdom

import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { buddyApi } from "@/shared/services/api-buddy";
import MyBuddyScheduleTab from "./MyBuddyScheduleTab";

vi.mock("@/shared/services/api-buddy", () => ({
  buddyApi: {
    getManagedAvailability: vi.fn(),
    getIncoming: vi.fn(),
    createAvailability: vi.fn(),
    updateAvailability: vi.fn(),
    setAvailabilityActive: vi.fn(),
    accept: vi.fn(),
    reject: vi.fn(),
  },
}));
vi.mock("@/shared/lib/auth", () => ({
  authStorage: {
    getUser: () => ({ userId: "buddy-id" }),
  },
}));
vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

const pendingRequest = {
  id: "request-id",
  availabilityId: "slot-id",
  localBuddyUserId: "buddy-id",
  buddyName: "Local Buddy",
  travelerUserId: "traveler-id",
  travelerName: "Traveler Name",
  startTimeUtc: "2026-07-10T10:00:00Z",
  endTimeUtc: "2026-07-10T12:00:00Z",
  totalPrice: 50,
  notes: "Museum",
  status: "Pending" as const,
  createdAtUtc: "2026-07-04T10:00:00Z",
  canCancel: true,
  canReview: false,
  hasReview: false,
};

describe("MyBuddyScheduleTab", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(buddyApi.getManagedAvailability).mockResolvedValue([]);
    vi.mocked(buddyApi.getIncoming).mockResolvedValue([]);
  });

  afterEach(cleanup);

  it("renders loading then empty states", async () => {
    render(<MyBuddyScheduleTab />);
    expect(screen.getByText("Loading schedule and requests…")).toBeInTheDocument();
    expect(await screen.findByText("No buddy requests yet.")).toBeInTheDocument();
    expect(screen.getByText("No availability slots yet.")).toBeInTheDocument();
  });

  it.each([
    ["Accept", "accept", "Accepted"],
    ["Reject", "reject", "Rejected"],
  ] as const)("handles the %s action and updates status", async (
    buttonName,
    method,
    status,
  ) => {
    vi.mocked(buddyApi.getIncoming).mockResolvedValue([pendingRequest]);
    vi.mocked(buddyApi[method]).mockResolvedValue({
      ...pendingRequest,
      status,
    });
    render(<MyBuddyScheduleTab />);

    fireEvent.click(await screen.findByRole("button", { name: buttonName }));
    await waitFor(() => expect(buddyApi[method]).toHaveBeenCalledWith("request-id"));
    expect(await screen.findByText(status)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Accept" })).not.toBeInTheDocument();
  });
});
