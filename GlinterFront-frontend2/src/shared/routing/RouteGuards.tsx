import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { Loader2 } from "lucide-react";
import { ApiError } from "@/shared/lib/api-client";
import { authStorage, type AppRole } from "@/shared/lib/auth";
import { getRoleHome } from "@/shared/lib/auth-routing";
import { useMyProfile } from "@/shared/hooks/use-my-profile";

const RouteLoading = () => (
  <div className="flex min-h-screen items-center justify-center bg-background">
    <Loader2 className="h-7 w-7 animate-spin text-primary" aria-label="Loading profile" />
  </div>
);

export const RequireAuth = ({ children }: { children: ReactNode }) => {
  const location = useLocation();

  if (!authStorage.isAuthenticated() || !authStorage.getUser()) {
    return (
      <Navigate
        to="/auth"
        replace
        state={{ from: `${location.pathname}${location.search}` }}
      />
    );
  }

  return <>{children}</>;
};

export const RedirectIfAuthenticated = ({ children }: { children: ReactNode }) => {
  if (authStorage.isAuthenticated() && authStorage.getUser()) {
    return <Navigate to="/start" replace />;
  }

  return <>{children}</>;
};

export const RequireRole = ({
  roles,
  children,
}: {
  roles: readonly AppRole[];
  children: ReactNode;
}) => {
  if (!authStorage.hasAnyRole(roles)) {
    return <Navigate to="/start" replace />;
  }

  return <>{children}</>;
};

export const RequireProfile = ({ children }: { children: ReactNode }) => {
  const profile = useMyProfile();

  if (profile.isPending) return <RouteLoading />;
  if (profile.error instanceof ApiError && profile.error.status === 404) {
    return <Navigate to="/profile/setup" replace />;
  }
  if (profile.isError) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-3 bg-background px-4 text-center">
        <p className="text-sm text-destructive">
          {profile.error instanceof Error
            ? profile.error.message
            : "Profile could not be loaded."}
        </p>
        <button type="button" onClick={() => profile.refetch()} className="rounded-lg bg-primary px-4 py-2 text-primary-foreground">
          Try again
        </button>
      </div>
    );
  }

  return <>{children}</>;
};

export const AuthenticatedLanding = () => {
  const user = authStorage.getUser();
  const profile = useMyProfile();

  if (!user) return <Navigate to="/auth" replace />;
  if (user.roles.includes("Admin")) return <Navigate to="/admin" replace />;
  if (profile.isPending) return <RouteLoading />;
  if (profile.error instanceof ApiError && profile.error.status === 404) {
    return <Navigate to="/profile/setup" replace />;
  }
  if (profile.isError) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-3 bg-background px-4 text-center">
        <p className="text-sm text-destructive">Unable to determine your account destination.</p>
        <button type="button" onClick={() => profile.refetch()} className="rounded-lg bg-primary px-4 py-2 text-primary-foreground">
          Try again
        </button>
      </div>
    );
  }

  return <Navigate to={getRoleHome(user.roles)} replace />;
};
