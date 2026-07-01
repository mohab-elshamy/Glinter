import { useCallback, useRef, useState } from "react";
import { regionsApi } from "@/shared/services/api-regions";
import type { RegionSelection } from "@/shared/types/regions";
import {
  geolocationFailureFromCode,
  geolocationFailureMessage,
  type GeolocationFailure,
} from "./geolocation";

export interface CurrentPosition {
  latitude: number;
  longitude: number;
  accuracy: number;
}

type LocationStatus = "idle" | "locating" | "ready" | GeolocationFailure;

export interface CurrentLocationState {
  status: LocationStatus;
  position?: CurrentPosition;
  detectedRegion?: RegionSelection;
  regionMessage?: string;
  requestSequence: number;
}

const initialState: CurrentLocationState = {
  status: "idle",
  requestSequence: 0,
};

export function useCurrentLocation() {
  const [state, setState] = useState<CurrentLocationState>(initialState);
  const requestRef = useRef(0);

  const fail = useCallback((failure: GeolocationFailure, requestSequence: number) => {
    setState((current) => ({
      ...current,
      status: failure,
      regionMessage: geolocationFailureMessage[failure],
      requestSequence,
    }));
  }, []);

  const locate = useCallback(() => {
    const requestSequence = ++requestRef.current;

    if (!window.isSecureContext) {
      fail("insecure", requestSequence);
      return;
    }
    if (!navigator.geolocation) {
      fail("unsupported", requestSequence);
      return;
    }

    setState((current) => ({
      ...current,
      status: "locating",
      regionMessage: undefined,
      requestSequence,
    }));

    navigator.geolocation.getCurrentPosition(
      (result) => {
        if (requestSequence !== requestRef.current) return;
        const position = {
          latitude: result.coords.latitude,
          longitude: result.coords.longitude,
          accuracy: result.coords.accuracy,
        };
        setState({
          status: "ready",
          position,
          regionMessage: "Identifying the backend region…",
          requestSequence,
        });

        void regionsApi.getSelectionByPoint(position.latitude, position.longitude)
          .then((detectedRegion) => {
            if (requestSequence !== requestRef.current) return;
            setState({
              status: "ready",
              position,
              detectedRegion,
              requestSequence,
            });
          })
          .catch((error: unknown) => {
            if (requestSequence !== requestRef.current) return;
            setState({
              status: "ready",
              position,
              regionMessage: error instanceof Error
                ? `Location found, but its backend region could not be identified: ${error.message}`
                : "Location found, but its backend region could not be identified.",
              requestSequence,
            });
          });
      },
      (error) => {
        if (requestSequence !== requestRef.current) return;
        fail(geolocationFailureFromCode(error.code), requestSequence);
      },
      {
        enableHighAccuracy: true,
        timeout: 12_000,
        maximumAge: 60_000,
      },
    );
  }, [fail]);

  return { state, locate };
}

export function detectedRegionLabel(region?: RegionSelection): string | undefined {
  if (!region) return undefined;
  return [
    region.country?.nameEn,
    region.governorate?.nameEn,
    region.district?.nameEn,
    region.neighbourhood?.nameEn ?? region.neighbourhood?.nameAr,
  ].filter(Boolean).join(" › ") || undefined;
}
