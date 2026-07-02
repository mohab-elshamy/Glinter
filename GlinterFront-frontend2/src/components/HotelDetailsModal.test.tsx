// @vitest-environment jsdom

import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import HotelDetailsModal from "./HotelDetailsModal";
import type { Hotel } from "@/features/where-to-stay/types";
import { staysApi } from "@/shared/services/api-stays";

vi.mock("@/shared/services/api-stays", () => ({
  staysApi: {
    getReviews: vi.fn(),
    createReview: vi.fn(),
  },
}));

const hotel: Hotel = {
  id: 7,
  name: "Portal Hotel",
  area: "Cairo",
  rating: 4.5,
  reviews: 12,
  price: 100,
  amenities: ["WiFi"],
  images: [],
  bookingPlatforms: [],
  reviewsPerRating: [],
  featuredReviews: [],
  sourceType: "ThirdParty",
  createdAtUtc: "2026-07-02T00:00:00Z",
};

const Harness = () => {
  const [selected, setSelected] = useState<Hotel | null>(null);
  return (
    <div data-testid="application-root">
      <button type="button" onClick={() => setSelected(hotel)}>Open hotel</button>
      <HotelDetailsModal
        hotel={selected}
        checkin="2026-08-01"
        checkout="2026-08-02"
        guests={1}
        onClose={() => setSelected(null)}
      />
    </div>
  );
};

describe("hotel details modal", () => {
  beforeEach(() => {
    vi.mocked(staysApi.getReviews).mockResolvedValue([]);
    vi.stubGlobal("requestAnimationFrame", (callback: FrameRequestCallback) => {
      callback(0);
      return 1;
    });
    vi.stubGlobal("cancelAnimationFrame", vi.fn());
  });

  afterEach(() => {
    cleanup();
    document.body.style.overflow = "";
    vi.unstubAllGlobals();
  });

  it("portals under document.body, locks scrolling, closes on Escape, and restores focus", async () => {
    render(<Harness />);
    const opener = screen.getByRole("button", { name: "Open hotel" });
    opener.focus();
    fireEvent.click(opener);

    const dialog = await screen.findByRole("dialog");
    expect(dialog.parentElement?.parentElement).toBe(document.body);
    expect(dialog).toHaveAttribute("aria-modal", "true");
    expect(document.body.style.overflow).toBe("hidden");
    await waitFor(() => expect(dialog.contains(document.activeElement)).toBe(true));

    fireEvent.keyDown(document, { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(document.body.style.overflow).toBe("");
    expect(opener).toHaveFocus();
  });

  it("traps forward and backward tab focus and ignores clicks inside the dialog", async () => {
    render(<Harness />);
    fireEvent.click(screen.getByRole("button", { name: "Open hotel" }));
    const dialog = await screen.findByRole("dialog");
    const focusable = Array.from(dialog.querySelectorAll<HTMLElement>(
      'button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled])',
    ));
    expect(focusable.length).toBeGreaterThan(1);

    focusable[focusable.length - 1].focus();
    fireEvent.keyDown(document, { key: "Tab" });
    expect(focusable[0]).toHaveFocus();

    focusable[0].focus();
    fireEvent.keyDown(document, { key: "Tab", shiftKey: true });
    expect(focusable[focusable.length - 1]).toHaveFocus();

    fireEvent.mouseDown(dialog);
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });
});
