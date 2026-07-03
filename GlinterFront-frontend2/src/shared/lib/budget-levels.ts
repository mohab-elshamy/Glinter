export type BudgetLevel = 1 | 2 | 3 | 4 | 5;

export const BUDGET_LEVEL_STORAGE_KEY = "glinterBudgetLevel";

export const budgetLevelOptions: ReadonlyArray<{
  level: BudgetLevel;
  canonical: string;
  label: string;
}> = [
  { level: 1, canonical: "Budget", label: "Budget" },
  { level: 2, canonical: "Economy", label: "Economy" },
  { level: 3, canonical: "MidRange", label: "Mid-range" },
  { level: 4, canonical: "Upscale", label: "Upscale" },
  { level: 5, canonical: "Luxury", label: "Luxury" },
];

export const parseBudgetLevel = (
  value: string | number | null | undefined,
): BudgetLevel | undefined => {
  if (typeof value === "number" && Number.isInteger(value) && value >= 1 && value <= 5) {
    return value as BudgetLevel;
  }
  if (typeof value !== "string") return undefined;

  const normalized = value.trim().toLowerCase().replaceAll(/[\s_-]/g, "");
  const aliases: Record<string, BudgetLevel> = {
    "1": 1,
    budget: 1,
    "$2030": 1,
    "2": 2,
    economy: 2,
    "3": 3,
    midrange: 3,
    "$3050": 3,
    "4": 4,
    upscale: 4,
    "$5080": 4,
    "5": 5,
    luxury: 5,
    "$80+": 5,
  };
  return aliases[normalized];
};

export const budgetLevelToProfileString = (level: BudgetLevel): string =>
  budgetLevelOptions.find((option) => option.level === level)?.canonical ?? "MidRange";

export const budgetLevelLabel = (level: BudgetLevel): string =>
  budgetLevelOptions.find((option) => option.level === level)?.label ?? "Mid-range";

export const readStoredBudgetLevel = (storage: Pick<Storage, "getItem" | "setItem" | "removeItem">): BudgetLevel => {
  const current = parseBudgetLevel(storage.getItem(BUDGET_LEVEL_STORAGE_KEY));
  if (current) return current;

  const migrated = parseBudgetLevel(storage.getItem("budgetRange")) ?? 3;
  storage.setItem(BUDGET_LEVEL_STORAGE_KEY, String(migrated));
  storage.removeItem("budgetRange");
  return migrated;
};
