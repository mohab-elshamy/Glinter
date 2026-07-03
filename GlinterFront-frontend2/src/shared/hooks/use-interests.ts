import { useQuery } from "@tanstack/react-query";
import { profilesApi } from "@/shared/services/api-profiles";

export const interestsQueryKey = ["interests"] as const;

export const useInterests = (enabled = true) => useQuery({
  queryKey: interestsQueryKey,
  queryFn: profilesApi.getInterests,
  enabled,
  staleTime: 30 * 60 * 1000,
});
