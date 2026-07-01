const usdFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
});

export function formatUsdPrice(
  value: number | null | undefined,
  unavailableLabel = "Price unavailable",
): string {
  if (value == null || !Number.isFinite(value)) return unavailableLabel;
  if (value === 0) return "Free";
  return usdFormatter.format(value);
}

export function formatExperiencePrice(
  startingPricePerPerson: number | null | undefined,
  priceRange: string | null | undefined,
): string {
  if (startingPricePerPerson != null && Number.isFinite(startingPricePerPerson)) {
    const formatted = formatUsdPrice(startingPricePerPerson);
    return startingPricePerPerson === 0 ? formatted : `From ${formatted}/person`;
  }

  const fallback = priceRange?.trim();
  return fallback || "Price unavailable";
}

export function isFreeExperience(
  startingPricePerPerson: number | null | undefined,
  priceRange: string | null | undefined,
): boolean {
  if (startingPricePerPerson != null) return startingPricePerPerson === 0;
  return /^(free|\$?\s*0(?:\.0+)?(?:\s*\/?\s*person)?)$/i.test(priceRange?.trim() ?? "");
}
