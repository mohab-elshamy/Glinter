const simplePaths = new Set([
  "/explore",
  "/local-buddies",
  "/where-to-stay",
  "/where-to-go",
]);

const profileTabs = new Set(["bookings", "hotels", "experiences", "buddy-schedule"]);
const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export const getSafeNotificationLink = (value?: string | null): string | undefined => {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.includes("\\") || value.includes("#")) {
    return undefined;
  }

  try {
    const applicationOrigin = "https://glinter.local";
    const url = new URL(value, applicationOrigin);
    if (url.origin !== applicationOrigin) return undefined;

    const keys = [...url.searchParams.keys()];
    if (simplePaths.has(url.pathname)) {
      return keys.length === 0 ? url.pathname : undefined;
    }

    if (url.pathname === "/messages") {
      if (keys.some((key) => key !== "thread") || url.searchParams.getAll("thread").length > 1) {
        return undefined;
      }
      const threadId = url.searchParams.get("thread");
      if (threadId && !guidPattern.test(threadId)) return undefined;
      return `${url.pathname}${url.search}`;
    }

    if (url.pathname === "/profile/me") {
      if (keys.some((key) => key !== "tab") || url.searchParams.getAll("tab").length > 1) {
        return undefined;
      }
      const tab = url.searchParams.get("tab");
      if (tab && !profileTabs.has(tab)) return undefined;
      return `${url.pathname}${url.search}`;
    }
  } catch {
    return undefined;
  }

  return undefined;
};
