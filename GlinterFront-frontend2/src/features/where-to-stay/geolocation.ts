export type GeolocationFailure =
  | "denied"
  | "unavailable"
  | "timeout"
  | "insecure"
  | "unsupported";

export function geolocationFailureFromCode(code: number): GeolocationFailure {
  if (code === 1) return "denied";
  if (code === 3) return "timeout";
  return "unavailable";
}

export const geolocationFailureMessage: Record<GeolocationFailure, string> = {
  denied: "Location permission was denied. Enable it in your browser settings and try again.",
  unavailable: "Your current location is unavailable. Check your device location services and try again.",
  timeout: "Finding your location took too long. Move to an area with a stronger signal and retry.",
  insecure: "Current location requires HTTPS or a trusted localhost development address.",
  unsupported: "This browser does not support current-location lookup.",
};
