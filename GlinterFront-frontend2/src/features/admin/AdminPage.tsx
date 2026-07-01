import { useCallback, useEffect, useState } from "react";
import type { ReactNode } from "react";
import {
  Activity,
  BarChart3,
  CheckCircle,
  ClipboardList,
  Shield,
  Sparkles,
  UserCheck,
  Users,
  XCircle,
} from "lucide-react";
import Navbar from "@/components/Navbar";
import { adminApi } from "@/shared/services/api-admin";
import type {
  AdminAnalytics,
  AdminAuditEvent,
  AdminDashboard,
  AdminLocalBuddy,
  AdminRoleResponse,
  AdminUserListItem,
  ExperienceModerationStatus,
  ExperienceResponseDto,
  BuddyVerificationEvent,
} from "@/shared/types/api";
import { toast } from "sonner";
import type { LoadState } from "@/shared/types/async-state";
import RegionsAdminPanel from "./RegionsAdminPanel";
import StayImportAdminPanel from "./StayImportAdminPanel";
import ExperienceImportAdminPanel from "./ExperienceImportAdminPanel";

const sections = [
  "Overview",
  "Users & Roles",
  "Buddy Verification",
  "Experience Moderation",
  "Analytics",
  "Audit Events",
  "Regions",
  "Stay Import",
  "Experience Import",
] as const;

type Section = (typeof sections)[number];

const errorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "The administration request failed.";

const AdminPage = () => {
  const [section, setSection] = useState<Section>("Overview");
  const [users, setUsers] = useState<AdminUserListItem[]>([]);
  const [roles, setRoles] = useState<AdminRoleResponse[]>([]);
  const [buddies, setBuddies] = useState<AdminLocalBuddy[]>([]);
  const [experiences, setExperiences] = useState<ExperienceResponseDto[]>([]);
  const [dashboard, setDashboard] = useState<AdminDashboard>();
  const [analytics, setAnalytics] = useState<AdminAnalytics>();
  const [auditEvents, setAuditEvents] = useState<AdminAuditEvent[]>([]);
  const [auditTotal, setAuditTotal] = useState(0);
  const [buddyHistory, setBuddyHistory] = useState<Record<string, BuddyVerificationEvent[]>>({});
  const [historyLoadingUserId, setHistoryLoadingUserId] = useState("");
  const [search, setSearch] = useState("");
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });
  const [refreshing, setRefreshing] = useState(false);

  const loadAdministration = useCallback(async () => {
    setRefreshing(true);
    try {
      const [
        loadedUsers,
        loadedRoles,
        loadedBuddies,
        loadedExperiences,
        loadedDashboard,
        loadedAnalytics,
        auditPage,
      ] = await Promise.all([
        adminApi.getUsers(),
        adminApi.getRoles(),
        adminApi.getLocalBuddies(),
        adminApi.getExperiences(),
        adminApi.getDashboard(),
        adminApi.getAnalytics(),
        adminApi.getAuditEvents({ page: 1, pageSize: 100 }),
      ]);
      setUsers(loadedUsers);
      setRoles(loadedRoles);
      setBuddies(loadedBuddies);
      setExperiences(loadedExperiences);
      setDashboard(loadedDashboard);
      setAnalytics(loadedAnalytics);
      setAuditEvents(auditPage.items);
      setAuditTotal(auditPage.totalCount);
      setLoadState({ status: "ready" });
    } catch (error) {
      const message = errorMessage(error);
      setLoadState({ status: "error", message });
      toast.error(message);
    } finally {
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    void loadAdministration();
  }, [loadAdministration]);

  const changeUserStatus = async (user: AdminUserListItem) => {
    try {
      const updated = await adminApi.changeUserStatus(user.userId, {
        isActive: !user.isActive,
      });
      setUsers((current) =>
        current.map((item) => item.userId === updated.userId
          ? { ...item, isActive: updated.isActive, role: updated.roles[0] ?? item.role }
          : item),
      );
      toast.success(updated.isActive ? "User activated." : "User deactivated.");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const assignRole = async (user: AdminUserListItem, role: string) => {
    if (!role || role === user.role) return;
    try {
      const updated = await adminApi.assignRole(user.userId, { role });
      setUsers((current) =>
        current.map((item) => item.userId === updated.userId
          ? { ...item, role: updated.roles[0] ?? role, isActive: updated.isActive }
          : item),
      );
      toast.success(`${role} role assigned.`);
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const verifyBuddy = async (
    buddy: AdminLocalBuddy,
    verificationStatus: "Approved" | "Rejected",
  ) => {
    try {
      await adminApi.updateBuddyVerification(buddy.userId, {
        verificationStatus,
        moderationNotes: verificationStatus === "Rejected"
          ? "Rejected by an administrator."
          : undefined,
      });
      setBuddies((current) =>
        current.map((item) =>
          item.userId === buddy.userId ? { ...item, verificationStatus } : item),
      );
      if (buddyHistory[buddy.userId]) {
        const history = await adminApi.getBuddyVerificationHistory(buddy.userId);
        setBuddyHistory((current) => ({ ...current, [buddy.userId]: history }));
      }
      toast.success(`Buddy ${verificationStatus.toLowerCase()}.`);
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const toggleBuddyHistory = async (userId: string) => {
    if (buddyHistory[userId]) {
      setBuddyHistory((current) => {
        const next = { ...current };
        delete next[userId];
        return next;
      });
      return;
    }
    setHistoryLoadingUserId(userId);
    try {
      const history = await adminApi.getBuddyVerificationHistory(userId);
      setBuddyHistory((current) => ({ ...current, [userId]: history }));
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setHistoryLoadingUserId("");
    }
  };

  const moderateExperience = async (
    experience: ExperienceResponseDto,
    moderationStatus: Exclude<ExperienceModerationStatus, "Pending">,
  ) => {
    try {
      const updated = await adminApi.moderateExperience(experience.id, {
        moderationStatus,
        moderationNotes: moderationStatus === "Rejected"
          ? "Rejected by an administrator."
          : undefined,
      });
      setExperiences((current) =>
        current.map((item) => item.id === updated.id ? updated : item),
      );
      toast.success(`Experience ${moderationStatus.toLowerCase()}.`);
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const normalizedSearch = search.trim().toLowerCase();
  const filteredUsers = users.filter((user) =>
    !normalizedSearch ||
    user.fullName.toLowerCase().includes(normalizedSearch) ||
    user.email.toLowerCase().includes(normalizedSearch));
  const filteredBuddies = buddies.filter((buddy) =>
    !normalizedSearch ||
    buddy.displayName.toLowerCase().includes(normalizedSearch) ||
    buddy.city.toLowerCase().includes(normalizedSearch));
  const filteredExperiences = experiences.filter((experience) =>
    !normalizedSearch ||
    experience.name.toLowerCase().includes(normalizedSearch) ||
    (experience.address ?? "").toLowerCase().includes(normalizedSearch));

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <div className="mx-auto flex max-w-7xl">
        <aside className="hidden min-h-[calc(100vh-4rem)] w-64 border-r border-border/50 p-4 md:block">
          <div className="mb-6 flex items-center gap-2 px-3">
            <Shield className="h-5 w-5 text-accent" />
            <h2 className="font-bold">Administration</h2>
          </div>
          <nav className="space-y-1">
            {sections.map((item) => (
              <button
                key={item}
                onClick={() => {
                  setSection(item);
                  setSearch("");
                }}
                className={`w-full rounded-lg px-3 py-2 text-left text-sm ${
                  section === item
                    ? "bg-accent text-accent-foreground"
                    : "text-muted-foreground hover:bg-secondary"
                }`}
              >
                {item}
              </button>
            ))}
          </nav>
        </aside>

        <main className="min-w-0 flex-1 p-4 md:p-6">
          <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h1 className="text-2xl font-bold">{section}</h1>
              <p className="text-xs text-muted-foreground">Live administration data from the backend</p>
            </div>
            <div className="flex gap-2">
              {section !== "Overview" && section !== "Analytics" && (
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  className="input-glass"
                  placeholder="Search"
                />
              )}
              <button
                disabled={refreshing}
                onClick={() => void loadAdministration()}
                className="rounded-lg border border-border px-3 py-2 text-xs disabled:opacity-50"
              >
                {refreshing ? "Refreshing…" : "Refresh"}
              </button>
            </div>
          </div>

          {loadState.status === "loading" ? (
            <div className="card-glass p-12 text-center text-sm text-muted-foreground">Loading administration data…</div>
          ) : loadState.status === "error" ? (
            <div className="rounded-xl border border-destructive/30 bg-destructive/10 p-5 text-sm text-destructive">
              {loadState.message}
            </div>
          ) : (
            <>
              {section === "Overview" && dashboard && (
                <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                  {[
                    { label: "Total users", value: dashboard.totalUsers, icon: Users },
                    { label: "Active users", value: dashboard.activeUsers, icon: UserCheck },
                    { label: "Pending buddies", value: dashboard.pendingBuddyVerifications, icon: Shield },
                    { label: "Pending experiences", value: dashboard.pendingExperiences, icon: Sparkles },
                    { label: "Approved experiences", value: dashboard.approvedExperiences, icon: CheckCircle },
                    { label: "Active stays", value: dashboard.activeStays, icon: Activity },
                    { label: "Total bookings", value: dashboard.totalBookings, icon: ClipboardList },
                    { label: "Booking value", value: `$${dashboard.totalBookingValue}`, icon: BarChart3 },
                  ].map(({ label, value, icon: Icon }) => (
                    <div key={label} className="card-glass p-5">
                      <Icon className="mb-3 h-5 w-5 text-accent" />
                      <p className="text-2xl font-bold">{value}</p>
                      <p className="text-xs text-muted-foreground">{label}</p>
                    </div>
                  ))}
                  <div className="card-glass p-5 sm:col-span-2 lg:col-span-4">
                    <p className="text-sm font-medium">Administrative activity</p>
                    <p className="mt-1 text-2xl font-bold">{dashboard.auditEventsLast24Hours}</p>
                    <p className="text-xs text-muted-foreground">audit events during the last 24 hours</p>
                  </div>
                </div>
              )}

              {section === "Users & Roles" && (
                <TableShell empty={filteredUsers.length === 0}>
                  <thead><tr><th>User</th><th>Role</th><th>Status</th><th>Actions</th></tr></thead>
                  <tbody>
                    {filteredUsers.map((user) => (
                      <tr key={user.userId}>
                        <td><p className="font-medium">{user.fullName}</p><p className="text-xs text-muted-foreground">{user.email}</p></td>
                        <td>
                          <select value={user.role} onChange={(event) => void assignRole(user, event.target.value)} className="rounded bg-secondary px-2 py-1 text-xs">
                            {!roles.some((role) => role.name === user.role) && <option>{user.role}</option>}
                            {roles.map((role) => <option key={role.name} value={role.name}>{role.name}</option>)}
                          </select>
                        </td>
                        <td><StatusBadge value={user.isActive ? "Active" : "Inactive"} /></td>
                        <td><button onClick={() => void changeUserStatus(user)} className="text-xs text-accent">{user.isActive ? "Deactivate" : "Activate"}</button></td>
                      </tr>
                    ))}
                  </tbody>
                </TableShell>
              )}

              {section === "Buddy Verification" && (
                <TableShell empty={filteredBuddies.length === 0}>
                  <thead><tr><th>Buddy</th><th>Location</th><th>Rating</th><th>Status</th><th>Moderate</th></tr></thead>
                  <tbody>
                    {filteredBuddies.map((buddy) => (
                      <tr key={buddy.userId}>
                        <td>{buddy.displayName}</td>
                        <td>{buddy.city}</td>
                        <td>{buddy.rating} ({buddy.reviewsCount})</td>
                        <td><StatusBadge value={buddy.verificationStatus} /></td>
                        <td className="space-x-3">
                          <button onClick={() => void verifyBuddy(buddy, "Approved")} className="text-xs text-green-400">Approve</button>
                          <button onClick={() => void verifyBuddy(buddy, "Rejected")} className="text-xs text-red-400">Reject</button>
                          <button onClick={() => void toggleBuddyHistory(buddy.userId)} className="text-xs text-accent">
                            {historyLoadingUserId === buddy.userId
                              ? "Loading…"
                              : buddyHistory[buddy.userId]
                                ? "Hide history"
                                : "History"}
                          </button>
                          {buddyHistory[buddy.userId] && (
                            <div className="mt-2 min-w-72 space-y-2 rounded-lg bg-secondary/40 p-3">
                              {buddyHistory[buddy.userId].length === 0 && (
                                <p className="text-xs text-muted-foreground">No verification decisions recorded.</p>
                              )}
                              {buddyHistory[buddy.userId].map((event) => (
                                <div key={event.id} className="border-b border-border/50 pb-2 text-xs last:border-0 last:pb-0">
                                  <p><strong>{event.previousStatus}</strong> → <strong>{event.newStatus}</strong></p>
                                  <p className="text-muted-foreground">{new Date(event.createdAtUtc).toLocaleString()}</p>
                                  {event.notes && <p className="mt-1 text-muted-foreground">{event.notes}</p>}
                                </div>
                              ))}
                            </div>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </TableShell>
              )}

              {section === "Experience Moderation" && (
                <TableShell empty={filteredExperiences.length === 0}>
                  <thead><tr><th>Experience</th><th>Category</th><th>Status</th><th>Active</th><th>Moderate</th></tr></thead>
                  <tbody>
                    {filteredExperiences.map((experience) => (
                      <tr key={experience.id}>
                        <td><p className="font-medium">{experience.name}</p><p className="text-xs text-muted-foreground">{experience.address}</p></td>
                        <td>{experience.category}</td>
                        <td><StatusBadge value={experience.moderationStatus} /></td>
                        <td>{experience.isActive ? "Yes" : "No"}</td>
                        <td className="space-x-3">
                          <button onClick={() => void moderateExperience(experience, "Approved")} className="text-xs text-green-400">Approve</button>
                          <button onClick={() => void moderateExperience(experience, "Rejected")} className="text-xs text-red-400">Reject</button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </TableShell>
              )}

              {section === "Analytics" && analytics && (
                <div className="grid gap-4 md:grid-cols-3">
                  <MetricBreakdown title="Users by role" values={analytics.usersByRole} />
                  <MetricBreakdown title="Bookings by status" values={analytics.bookingsByStatus} />
                  <MetricBreakdown title="Listings" values={analytics.listingsByType} />
                  <div className="card-glass p-5 md:col-span-3">
                    <h2 className="mb-3 font-semibold">Booking value</h2>
                    <div className="grid grid-cols-2 gap-4">
                      <div><p className="text-2xl font-bold">${analytics.stayBookingValue}</p><p className="text-xs text-muted-foreground">Stays</p></div>
                      <div><p className="text-2xl font-bold">${analytics.experienceBookingValue}</p><p className="text-xs text-muted-foreground">Experiences</p></div>
                    </div>
                  </div>
                </div>
              )}

              {section === "Regions" && <RegionsAdminPanel />}
              {section === "Stay Import" && <StayImportAdminPanel />}
              {section === "Experience Import" && <ExperienceImportAdminPanel />}

              {section === "Audit Events" && (
                <div>
                  <p className="mb-3 text-xs text-muted-foreground">Showing {auditEvents.length} of {auditTotal} events</p>
                  <TableShell empty={auditEvents.length === 0}>
                    <thead><tr><th>Time</th><th>Action</th><th>Target</th><th>Result</th><th>Correlation</th></tr></thead>
                    <tbody>
                      {auditEvents
                        .filter((event) => !normalizedSearch || event.action.toLowerCase().includes(normalizedSearch))
                        .map((event) => (
                          <tr key={event.id}>
                            <td className="text-xs">{new Date(event.createdAtUtc).toLocaleString()}</td>
                            <td><p className="font-medium">{event.action}</p><p className="text-xs text-muted-foreground">{event.httpMethod} {event.path}</p></td>
                            <td className="text-xs">{event.target || "—"}</td>
                            <td>{event.succeeded ? <span className="text-green-400">Succeeded</span> : <span className="text-red-400">Failed ({event.statusCode})</span>}</td>
                            <td className="max-w-32 truncate text-xs" title={event.correlationId}>{event.correlationId}</td>
                          </tr>
                        ))}
                    </tbody>
                  </TableShell>
                </div>
              )}
            </>
          )}
        </main>
      </div>
    </div>
  );
};

const TableShell = ({
  children,
  empty,
}: {
  children: ReactNode;
  empty: boolean;
}) => (
  <div className="card-glass overflow-x-auto p-5">
    {empty ? (
      <div className="py-10 text-center text-sm text-muted-foreground">No matching records.</div>
    ) : (
      <table className="w-full text-left text-sm [&_td]:border-b [&_td]:border-border/60 [&_td]:py-3 [&_th]:border-b [&_th]:border-border [&_th]:pb-3">
        {children}
      </table>
    )}
  </div>
);

const StatusBadge = ({ value }: { value: string }) => {
  const good = value === "Active" || value === "Approved";
  const pending = value === "Pending";
  return (
    <span className={`rounded-full px-2 py-1 text-[10px] ${
      good
        ? "bg-green-500/20 text-green-400"
        : pending
          ? "bg-yellow-500/20 text-yellow-400"
          : "bg-red-500/20 text-red-400"
    }`}>
      {good ? <CheckCircle className="mr-1 inline h-3 w-3" /> : <XCircle className="mr-1 inline h-3 w-3" />}
      {value}
    </span>
  );
};

const MetricBreakdown = ({
  title,
  values,
}: {
  title: string;
  values: Record<string, number>;
}) => (
  <div className="card-glass p-5">
    <h2 className="mb-3 font-semibold">{title}</h2>
    <div className="space-y-2">
      {Object.entries(values).map(([label, value]) => (
        <div key={label} className="flex justify-between text-sm">
          <span className="text-muted-foreground">{label}</span>
          <span className="font-bold">{value}</span>
        </div>
      ))}
      {Object.keys(values).length === 0 && <p className="text-xs text-muted-foreground">No data yet.</p>}
    </div>
  </div>
);

export default AdminPage;
