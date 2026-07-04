// @vitest-environment jsdom

import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { staysApi } from "@/shared/services/api-stays";
import type {
  HotelRecommendationResponse,
  NaturalLanguageHotelRecommendationResponse,
} from "@/shared/types/api";
import HotelRecommendationPanel from "./HotelRecommendationPanel";
import {
  buildStructuredRecommendationRequest,
  NATURAL_LANGUAGE_MAX_CHARACTERS,
} from "../recommendation-form";

vi.mock("@/shared/services/api-stays", () => ({
  staysApi: {
    getRecommendations: vi.fn(),
    getNaturalLanguageRecommendations: vi.fn(),
  },
}));
vi.mock("@/shared/hooks/use-my-profile", () => ({
  useMyProfile: () => ({ data: undefined }),
}));

const successfulResponse: HotelRecommendationResponse = {
  preferences: {
    budgetLevel: 3,
    budgetLabel: "MidRange",
    experienceCategories: [{ category: "Historical", weight: 1 }],
    requestedAmenities: ["WiFi"],
    limit: 5,
    preferredLanguage: "en",
  },
  totalCandidates: 1,
  totalMatchingCandidates: 1,
  evaluatedCandidates: 1,
  returnedCount: 1,
  items: [{
    ranking: 1,
    hotelId: 42,
    name: "Matched Hotel",
    price: 120,
    budgetLevel: 3,
    budgetLabel: "MidRange",
    rating: 4.5,
    reviews: 20,
    finalScore: 88.5,
    scores: {
      interestProximityScore: 0.9,
      budgetMatchScore: 1,
      hotelQualityScore: 0.8,
      amenityMatchScore: 1,
    },
    amenities: ["WiFi"],
    matchedAmenities: ["WiFi"],
    nearbyExperiences: [{
      experienceId: 2,
      name: "Museum",
      category: "Historical",
      distanceKm: 1.2,
      rating: 4.7,
      reviews: 100,
    }],
    explanation: {
      shortExplanation: "A strong match.",
      reasons: ["Near museums"],
      bestFor: ["History"],
      isAiGenerated: false,
    },
    region: { displayName: "Cairo" },
  }],
};

const naturalSuccessfulResponse: NaturalLanguageHotelRecommendationResponse = {
  ...successfulResponse,
  inputText: "A quiet hotel near museums",
};

const renderPanel = (onViewDetails = vi.fn().mockResolvedValue(undefined)) => {
  render(
    <HotelRecommendationPanel
      region={{ adm0Gid: 1, adm1Gid: 2, adm2Gid: 3, adm3Gid: 4 }}
      onViewDetails={onViewDetails}
    />,
  );
  fireEvent.click(screen.getByRole("button", { name: "Open AI stay assistant" }));
  return onViewDetails;
};

describe("hotel recommendation panel", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });
  afterEach(cleanup);

  it("builds a structured request with active administrative filters", () => {
    expect(buildStructuredRecommendationRequest({
      budgetLevel: 4,
      selectedCategories: ["Historical"],
      categoryWeights: { Historical: 2 },
      selectedAmenities: ["WiFi"],
      limit: 5,
      language: "ar",
      region: { adm0Gid: 1, adm1Gid: 2, adm2Gid: 3, adm3Gid: 4 },
    })).toEqual({
      budgetLevel: 4,
      experienceCategories: [{ category: "Historical", weight: 2 }],
      requestedAmenities: ["WiFi"],
      adm0Gid: 1,
      adm1Gid: 2,
      adm2Gid: 3,
      adm3Gid: 4,
      limit: 5,
      preferredLanguage: "ar",
    });
  });

  it("shows loading then empty state and sends the selected region", async () => {
    let resolve!: (response: HotelRecommendationResponse) => void;
    vi.mocked(staysApi.getRecommendations).mockReturnValue(
      new Promise((complete) => { resolve = complete; }),
    );
    renderPanel();
    fireEvent.click(screen.getByRole("tab", { name: "Preferences" }));
    fireEvent.click(screen.getByLabelText("Historical"));
    fireEvent.click(screen.getByRole("button", { name: "Find my best stays" }));
    expect(screen.getByRole("status")).toHaveTextContent("Finding your best stays");

    resolve({ ...successfulResponse, returnedCount: 0, items: [] });
    expect(await screen.findByText(/couldn’t find an eligible active stay/)).toBeInTheDocument();
    expect(staysApi.getRecommendations).toHaveBeenCalledWith(
      expect.objectContaining({ adm0Gid: 1, adm1Gid: 2, adm2Gid: 3, adm3Gid: 4 }),
    );
  });

  it("shows an error with retry", async () => {
    vi.mocked(staysApi.getRecommendations).mockRejectedValue(new Error("Backend unavailable"));
    renderPanel();
    fireEvent.click(screen.getByRole("tab", { name: "Preferences" }));
    fireEvent.click(screen.getByRole("button", { name: "Find my best stays" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Backend unavailable");
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    await waitFor(() => expect(staysApi.getRecommendations).toHaveBeenCalledTimes(2));
  });

  it("renders successful scores and loads the full stay by ID", async () => {
    vi.mocked(staysApi.getRecommendations).mockResolvedValue(successfulResponse);
    const onViewDetails = renderPanel();
    fireEvent.click(screen.getByRole("tab", { name: "Preferences" }));
    fireEvent.click(screen.getByRole("button", { name: "Find my best stays" }));
    expect(await screen.findByText("Matched Hotel")).toBeInTheDocument();
    expect(screen.getByText("90%")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "View hotel details" }));
    await waitFor(() => expect(onViewDetails).toHaveBeenCalledWith(42));
  });

  it("enforces the natural-language character maximum", () => {
    renderPanel();
    const input = screen.getByLabelText("Describe the stay you want");
    fireEvent.change(input, { target: { value: "x".repeat(NATURAL_LANGUAGE_MAX_CHARACTERS + 50) } });
    expect(input).toHaveValue("x".repeat(NATURAL_LANGUAGE_MAX_CHARACTERS));
    expect(screen.getByText(`${NATURAL_LANGUAGE_MAX_CHARACTERS}/${NATURAL_LANGUAGE_MAX_CHARACTERS}`))
      .toBeInTheDocument();
  });

  it("uses a floating launcher, explains the match count, and exposes a clear send action", () => {
    renderPanel();
    expect(screen.getByRole("dialog", { name: "AI stay recommendation assistant" }))
      .toHaveClass("fixed", "sm:w-[430px]");
    expect(screen.getByText(/controls how many ranked hotel suggestions/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Send stay request" })).toBeDisabled();
  });

  it("sends a natural-language request from the chat composer", async () => {
    vi.mocked(staysApi.getNaturalLanguageRecommendations).mockResolvedValue(naturalSuccessfulResponse);
    renderPanel();
    fireEvent.change(screen.getByLabelText("Describe the stay you want"), {
      target: { value: "A quiet hotel near museums" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Send stay request" }));
    await waitFor(() => expect(staysApi.getNaturalLanguageRecommendations).toHaveBeenCalledWith(
      expect.objectContaining({
        text: "A quiet hotel near museums",
        limit: 5,
        adm0Gid: 1,
        adm1Gid: 2,
        adm2Gid: 3,
        adm3Gid: 4,
      }),
    ));
  });
});
