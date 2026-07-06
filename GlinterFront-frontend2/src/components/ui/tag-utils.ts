const normalize = (value: string) => value.trim().replace(/\s+/g, " ");

export const normalizeTags = (
  values: string[],
  maxItems = 10,
  maxItemLength = 40,
) => {
  const seen = new Set<string>();
  return values
    .map(normalize)
    .filter((item) => {
      const key = item.toLocaleLowerCase();
      if (!item || item.length > maxItemLength || seen.has(key)) return false;
      seen.add(key);
      return true;
    })
    .slice(0, maxItems);
};

export const parseTags = (value?: string) =>
  normalizeTags((value ?? "").split(/[,;\n]/));
