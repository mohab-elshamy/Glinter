import { useSyncExternalStore } from "react";
import { apiActivity } from "@/shared/lib/api-activity";

const GlobalApiLoadingIndicator = () => {
  const activeRequests = useSyncExternalStore(
    apiActivity.subscribe,
    apiActivity.getSnapshot,
    apiActivity.getSnapshot,
  );

  if (activeRequests === 0) return null;

  return (
    <div
      role="progressbar"
      aria-label="Loading data"
      className="layer-toast fixed inset-x-0 top-0 h-0.5 overflow-hidden bg-primary/20"
    >
      <div className="h-full w-1/3 animate-pulse bg-primary" />
    </div>
  );
};

export default GlobalApiLoadingIndicator;
