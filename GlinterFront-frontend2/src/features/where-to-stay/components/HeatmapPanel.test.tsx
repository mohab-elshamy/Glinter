// @vitest-environment jsdom

import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import HeatmapPanel from "./HeatmapPanel";
import HotelDetailsModal from "@/components/HotelDetailsModal";
import type { Hotel } from "../types";
import { staysApi } from "@/shared/services/api-stays";

vi.mock("@/components/LeafletMap", () => ({
  default: () => <div data-testid="leaflet-map" className="leaflet-pane">Map</div>,
}));
vi.mock("@/shared/services/api-stays", () => ({
  staysApi: {
    getReviews: vi.fn(),
    createReview: vi.fn(),
  },
}));

const hotel: Hotel = {
  id: 11,
  name: "Map Hotel",
  area: "Giza",
  rating: 4.2,
  reviews: 8,
  price: 90,
  amenities: [],
  image: "https://example.test/map-hotel.jpg",
  images: [],
  bookingPlatforms: [],
  reviewsPerRating: [],
  featuredReviews: [],
  sourceType: "ThirdParty",
  createdAtUtc: "2026-07-02T00:00:00Z",
  latitude: 30,
  longitude: 31,
};

const Harness = () => {
  const [details, setDetails] = useState<Hotel | null>(null);
  return (
    <div data-testid="map-host">
      <HeatmapPanel
        hotels={[hotel]}
        selectedHotel={hotel}
        mode="standard"
        safetyScores={new Map()}
        safetyAreas={[]}
        onSelectHotel={() => undefined}
        onClearHotel={() => undefined}
        onViewDetails={setDetails}
        onMapClick={() => undefined}
      />
      <HotelDetailsModal hotel={details} checkin="" checkout="" guests={1} onClose={() => setDetails(null)} />
    </div>
  );
};

describe("stay map overlays", () => {
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
    vi.unstubAllGlobals();
  });

  it("uses separate responsive mobile positions for legend and preview", () => {
    render(<Harness />);
    expect(screen.getByTestId("map-legend")).toHaveClass("top-3");
    expect(screen.getByTestId("map-legend")).toHaveClass("sm:bottom-4");
    expect(screen.getByTestId("selected-stay-preview")).toHaveClass("bottom-3");
    expect(screen.getByTestId("selected-stay-preview")).toHaveClass("left-3", "right-3");
  });

  it("opens the portal modal above the isolated map layer", async () => {
    render(<Harness />);
    fireEvent.click(screen.getByRole("button", { name: "View hotel details" }));
    const dialog = await screen.findByRole("dialog");
    expect(screen.getByTestId("map-host").firstElementChild).toHaveClass("layer-map");
    expect(dialog).toHaveClass("layer-modal-content");
    expect(dialog.parentElement?.parentElement).toBe(document.body);
  });
});
