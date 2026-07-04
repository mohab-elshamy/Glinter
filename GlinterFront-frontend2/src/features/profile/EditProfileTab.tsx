import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { useQueryClient } from "@tanstack/react-query";
import { authStorage } from "@/shared/lib/auth";
import { getPrimaryProfileRole } from "@/shared/lib/auth-routing";
import {
  myProfileQueryKey,
  useMyProfile,
} from "@/shared/hooks/use-my-profile";
import type { MyProfileResponse } from "@/shared/services/api-profiles";
import RoleProfileForm from "./RoleProfileForm";

const EditProfileTab = () => {
  const user = authStorage.getUser()!;
  const role = getPrimaryProfileRole(user.roles);
  const profileQuery = useMyProfile();
  const queryClient = useQueryClient();

  if (profileQuery.isPending) {
    return (
      <div className="flex justify-center p-10">
        <Loader2 className="h-6 w-6 animate-spin text-primary" />
      </div>
    );
  }

  if (!profileQuery.data) {
    return (
      <div role="alert" className="rounded-lg border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive">
        Profile information could not be loaded.
      </div>
    );
  }

  const handleSaved = (profile: MyProfileResponse) => {
    queryClient.setQueryData(myProfileQueryKey(user.userId), profile);
    toast.success("Profile saved.");
  };

  return (
    <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
      <RoleProfileForm
        role={role}
        defaultName={user.fullName}
        initialProfile={profileQuery.data}
        submitLabel="Save profile"
        onSaved={handleSaved}
      />
    </div>
  );
};

export default EditProfileTab;
