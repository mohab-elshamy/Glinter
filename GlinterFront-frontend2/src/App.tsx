import { lazy, Suspense, useEffect } from "react";
import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  BrowserRouter,
  Routes,
  Route,
  useLocation,
  useNavigate,
} from "react-router-dom";
import { systemApi } from "@/shared/services/api-system";
import GlobalApiLoadingIndicator from "@/components/GlobalApiLoadingIndicator";
import type { ApiFailureEventDetail } from "@/shared/lib/api-client";
import { toast } from "sonner";
import { PUBLIC_ACCOUNT_ROLES } from "@/shared/lib/auth-routing";
import {
  AuthenticatedLanding,
  RedirectIfAuthenticated,
  RequireAuth,
  RequireProfile,
  RequireRole,
} from "@/shared/routing/RouteGuards";

const ScrollToTop = () => {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  return null;
};

const BackendConnectionCheck = () => {
  useEffect(() => {
    if (!import.meta.env.DEV) return;

    systemApi.checkLiveness()
      .then((status) => console.info(`[Glinter] Backend health: ${status}`))
      .catch((error) => console.error("[Glinter] Backend health check failed", error));
  }, []);

  return null;
};

const SessionExpirationHandler = () => {
  const navigate = useNavigate();

  useEffect(() => {
    const handleSessionExpired = () => {
      window.dispatchEvent(new Event("auth-change"));
      navigate("/auth", { replace: true });
    };

    window.addEventListener("auth-session-expired", handleSessionExpired);
    return () => window.removeEventListener("auth-session-expired", handleSessionExpired);
  }, [navigate]);

  return null;
};

const ApiFailureHandler = () => {
  useEffect(() => {
    const handleApiFailure = (event: Event) => {
      const { detail } = event as CustomEvent<ApiFailureEventDetail>;
      toast.error(detail.message, {
        id: `api-error-${detail.errorCode}`,
        description: detail.traceId ? `Reference: ${detail.traceId}` : undefined,
      });
    };

    window.addEventListener("api-request-error", handleApiFailure);
    return () => window.removeEventListener("api-request-error", handleApiFailure);
  }, []);

  return null;
};
const Auth = lazy(() => import("./features/auth/AuthPage"));
const Explore = lazy(() => import("./pages/Explore"));
const LocalBuddies = lazy(() => import("./features/local-buddies/LocalBuddiesPage"));
const Messages = lazy(() => import("./features/messages/MessagesPage"));
const About = lazy(() => import("./pages/About"));
const WhereToGo = lazy(() => import("./features/where-to-go/WhereToGoPage"));
const WhereToStay = lazy(() => import("./features/where-to-stay/WhereToStayPage"));
const Dashboard = lazy(() => import("./pages/Dashboard"));
const Admin = lazy(() => import("./features/admin/AdminPage"));
const ProfilePage = lazy(() => import("./features/profile/ProfilePage"));
const EditProfilePage = lazy(() => import("./features/profile/EditProfilePage"));
const ProfileSetupPage = lazy(() => import("./features/profile/ProfileSetupPage"));
const NotFound = lazy(() => import("./pages/NotFound"));

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <GlobalApiLoadingIndicator />
      <BrowserRouter>
        <BackendConnectionCheck />
        <SessionExpirationHandler />
        <ApiFailureHandler />
        <ScrollToTop />
        <Suspense fallback={(
          <div role="status" aria-live="polite" className="flex min-h-screen items-center justify-center bg-background text-sm text-muted-foreground">
            Loading page…
          </div>
        )}>
        <Routes>
          <Route path="/" element={<Explore />} />
          <Route path="/auth" element={<RedirectIfAuthenticated><Auth /></RedirectIfAuthenticated>} />
          <Route path="/confirm-email" element={<Auth initialView="confirm-email" />} />
          <Route path="/reset-password" element={<Auth initialView="reset-password" />} />
          <Route path="/start" element={<RequireAuth><AuthenticatedLanding /></RequireAuth>} />
          <Route path="/explore" element={<Explore />} />
          <Route path="/local-buddies" element={<LocalBuddies />} />
          <Route path="/messages" element={<RequireAuth><Messages /></RequireAuth>} />
          <Route path="/about" element={<About />} />
          <Route path="/where-to-go" element={<WhereToGo />} />
          <Route path="/where-to-stay" element={<WhereToStay />} />
          <Route
            path="/dashboard"
            element={(
              <RequireAuth>
                <RequireRole roles={["Traveler"]}>
                  <RequireProfile><Dashboard /></RequireProfile>
                </RequireRole>
              </RequireAuth>
            )}
          />
          <Route
            path="/admin"
            element={<RequireAuth><RequireRole roles={["Admin"]}><Admin /></RequireRole></RequireAuth>}
          />
          <Route path="/profile/:id" element={<ProfilePage />} />
          <Route
            path="/profile/setup"
            element={(
              <RequireAuth>
                <RequireRole roles={PUBLIC_ACCOUNT_ROLES}>
                  <ProfileSetupPage />
                </RequireRole>
              </RequireAuth>
            )}
          />
          <Route
            path="/profile/me"
            element={(
              <RequireAuth>
                <RequireRole roles={["Traveler", "LocalBuddy", "HotelOwner", "ExperienceProvider"]}>
                  <RequireProfile><EditProfilePage /></RequireProfile>
                </RequireRole>
              </RequireAuth>
            )}
          />
          <Route path="*" element={<NotFound />} />
        </Routes>
        </Suspense>
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
