import type { AppRole } from "./auth";

export const PUBLIC_ACCOUNT_ROLES: readonly AppRole[] = [
  "Traveler",
  "LocalBuddy",
  "HotelOwner",
  "ExperienceProvider",
];

export const getRoleHome = (roles: readonly string[]): string => {
  if (roles.includes("Admin")) return "/admin";
  if (roles.includes("HotelOwner")) return "/profile/me?tab=hotels";
  if (roles.includes("ExperienceProvider")) return "/profile/me?tab=experiences";
  if (roles.includes("LocalBuddy")) return "/local-buddies";
  return "/dashboard";
};
