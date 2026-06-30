import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { UserRoundCheck } from "lucide-react";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { authStorage } from "@/shared/lib/auth";
import {
  getPrimaryProfileRole,
  getRoleHome,
} from "@/shared/lib/auth-routing";
import { myProfileQueryKey } from "@/shared/hooks/use-my-profile";
import type { MyProfileResponse } from "@/shared/services/api-profiles";
import RoleProfileForm from "./RoleProfileForm";

const ProfileSetupPage = () => {
  const user = authStorage.getUser()!;
  const role = getPrimaryProfileRole(user.roles);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const handleSaved = (profile: MyProfileResponse) => {
    queryClient.setQueryData(myProfileQueryKey(user.userId), profile);
    navigate(getRoleHome(user.roles), { replace: true });
  };

  return (
    <div className="min-h-screen bg-background">
      <Navbar solid />
      <main className="mx-auto max-w-2xl px-4 py-10">
        <div className="card-glass p-6 sm:p-8">
          <UserRoundCheck className="mx-auto mb-3 h-10 w-10 text-primary" />
          <h1 className="text-center text-2xl font-bold">Complete your profile</h1>
          <p className="mb-6 mt-2 text-center text-sm text-muted-foreground">
            Add the information required for your{" "}
            {role.replace(/([A-Z])/g, " $1").trim()} account.
          </p>
          <RoleProfileForm
            role={role}
            defaultName={user.fullName}
            submitLabel="Save and continue"
            onSaved={handleSaved}
          />
        </div>
      </main>
      <Footer />
    </div>
  );
};

export default ProfileSetupPage;
