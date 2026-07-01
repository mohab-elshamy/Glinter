import type { AppRole } from "./auth";

export type ProfileRole = Exclude<AppRole, "Admin">;

export const PUBLIC_ACCOUNT_ROLES: readonly ProfileRole[] = [
  "Traveler",
  "LocalBuddy",
  "HotelOwner",
  "ExperienceProvider",
];

export const getPrimaryProfileRole = (roles: readonly string[]): ProfileRole =>
  (PUBLIC_ACCOUNT_ROLES.find((role) => roles.includes(role)) ?? "Traveler");

export const getRoleHome = (roles: readonly string[]): string => {
  if (roles.includes("Admin")) return "/admin";
  if (roles.includes("HotelOwner")) return "/profile/me?tab=hotels";
  if (roles.includes("ExperienceProvider")) return "/profile/me?tab=experiences";
  if (roles.includes("LocalBuddy")) return "/profile/me";
  return "/dashboard";
};
