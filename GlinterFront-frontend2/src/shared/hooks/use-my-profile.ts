import { useQuery } from "@tanstack/react-query";
import { authStorage } from "@/shared/lib/auth";
import { profilesApi } from "@/shared/services/api-profiles";

export const myProfileQueryKey = (userId?: string) => [
  "my-profile",
  userId ?? "anonymous",
] as const;

export const useMyProfile = () => {
  const userId = authStorage.getUser()?.userId;

  return useQuery({
    queryKey: myProfileQueryKey(userId),
    queryFn: profilesApi.getMyProfile,
    enabled: Boolean(userId && authStorage.isAuthenticated()),
    retry: false,
    staleTime: 5 * 60 * 1000,
  });
};
