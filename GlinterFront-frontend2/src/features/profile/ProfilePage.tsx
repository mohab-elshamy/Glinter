import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  AlertTriangle,
  ArrowLeft,
  Calendar,
  CheckCircle,
  Heart,
  Languages,
  MapPin,
  MessageSquare,
  RefreshCw,
  Share2,
  Star,
  Users,
} from "lucide-react";
import { motion } from "framer-motion";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { toast } from "sonner";
import { profilesApi } from "@/shared/services/api-profiles";
import { authStorage } from "@/shared/lib/auth";
import type {
  BuddyAvailabilityDto,
  BuddyBookingDto,
  BuddyReviewDto,
  LocalBuddyProfileResponse,
} from "@/shared/types/api";
import type { LoadState } from "@/shared/types/async-state";
import { createInitialsAvatar } from "@/shared/lib/avatar";

const avatarFor = (profile: LocalBuddyProfileResponse) =>
  profile.profileImageUrl ||
  createInitialsAvatar(profile.displayName);

const ProfilePage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<LocalBuddyProfileResponse>();
  const [availability, setAvailability] = useState<BuddyAvailabilityDto[]>([]);
  const [reviews, setReviews] = useState<BuddyReviewDto[]>([]);
  const [myBookings, setMyBookings] = useState<BuddyBookingDto[]>([]);
  const [selectedAvailabilityId, setSelectedAvailabilityId] = useState("");
  const [bookingNotes, setBookingNotes] = useState("");
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewText, setReviewText] = useState("");
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });
  const [actionPending, setActionPending] = useState(false);
  const [retryVersion, setRetryVersion] = useState(0);

  useEffect(() => {
    if (!id) {
      setLoadState({ status: "error", message: "The profile address is invalid." });
      return;
    }

    let active = true;
    setLoadState({ status: "loading" });
    Promise.all([
      profilesApi.getLocalBuddy(id),
      profilesApi.getBuddyAvailability(id),
      profilesApi.getBuddyReviews(id),
      authStorage.hasAnyRole(["Traveler"])
        ? profilesApi.getMyBuddyBookings()
        : Promise.resolve([]),
    ])
      .then(([loadedProfile, loadedAvailability, loadedReviews, loadedBookings]) => {
        if (!active) return;
        setProfile(loadedProfile);
        setAvailability(loadedAvailability);
        setReviews(loadedReviews);
        setMyBookings(loadedBookings);
        setSelectedAvailabilityId(
          loadedAvailability.find((slot) => !slot.isBooked)?.id ?? "",
        );
        setLoadState({ status: "ready" });
      })
      .catch((error: unknown) => {
        if (!active) return;
        setProfile(undefined);
        setLoadState({
          status: "error",
          message: error instanceof Error ? error.message : "Could not load this buddy profile.",
        });
      });

    return () => {
      active = false;
    };
  }, [id, retryVersion]);

  const selectedSlot = availability.find((slot) => slot.id === selectedAvailabilityId);
  const reviewableBooking = useMemo(() => myBookings.find(
    (booking) =>
      booking.localBuddyUserId === id &&
      booking.status === "Completed" &&
      !reviews.some((review) => review.bookingId === booking.id),
  ), [id, myBookings, reviews]);
  const isOwnProfile = authStorage.getUser()?.userId === id;

  const toggleFollow = async () => {
    if (!profile) return;
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in to follow local buddies.");
      navigate("/auth");
      return;
    }
    setActionPending(true);
    try {
      const status = profile.isFollowing
        ? await profilesApi.unfollowUser(profile.userId)
        : await profilesApi.followUser(profile.userId);
      setProfile((current) => current
        ? {
            ...current,
            isFollowing: status.isFollowing,
            followersCount: status.followersCount,
          }
        : current);
      toast.success(status.isFollowing ? `Following ${profile.displayName}.` : `Unfollowed ${profile.displayName}.`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not update follow status.");
    } finally {
      setActionPending(false);
    }
  };

  const requestBuddy = async () => {
    if (!profile || !selectedAvailabilityId) return;
    if (!authStorage.hasAnyRole(["Traveler"])) {
      toast.error("Sign in with a traveler account to request a buddy.");
      return;
    }
    setActionPending(true);
    try {
      const booking = await profilesApi.createBuddyBooking(
        profile.userId,
        selectedAvailabilityId,
        bookingNotes || undefined,
      );
      setMyBookings((current) => [booking, ...current]);
      setAvailability((current) => current.map((slot) =>
        slot.id === booking.availabilityId ? { ...slot, isBooked: true } : slot));
      setSelectedAvailabilityId("");
      setBookingNotes("");
      toast.success("Buddy request sent.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not request this buddy.");
    } finally {
      setActionPending(false);
    }
  };

  const submitReview = async () => {
    if (!profile || !reviewableBooking || !reviewText.trim()) return;
    setActionPending(true);
    try {
      const review = await profilesApi.createBuddyReview(profile.userId, {
        bookingId: reviewableBooking.id,
        rating: reviewRating,
        reviewText,
      });
      setReviews((current) => [review, ...current]);
      setReviewText("");
      setProfile((current) => current
        ? {
            ...current,
            reviewsCount: current.reviewsCount + 1,
            rating: (
              (current.rating * current.reviewsCount + review.rating) /
              (current.reviewsCount + 1)
            ),
          }
        : current);
      toast.success("Buddy review submitted.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not submit your review.");
    } finally {
      setActionPending(false);
    }
  };

  const connect = () => {
    if (!profile) return;
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in to message this buddy.");
      navigate("/auth");
      return;
    }
    navigate("/messages", {
      state: {
        selectedBuddy: {
          userId: profile.userId,
          name: profile.displayName,
          photo: avatarFor(profile),
          status: "offline",
        },
      },
    });
  };

  if (loadState.status !== "ready" || !profile) {
    return (
      <div className="min-h-screen bg-background">
        <Navbar />
        <main className="container mx-auto max-w-3xl px-4 py-16">
          {loadState.status === "loading" ? (
            <div className="card-glass p-12 text-center text-sm text-muted-foreground">
              Loading buddy profile, availability, and reviews…
            </div>
          ) : (
            <div role="alert" className="rounded-2xl border border-destructive/30 bg-destructive/10 p-8 text-center">
              <AlertTriangle className="mx-auto mb-3 h-8 w-8 text-destructive" />
              <h1 className="font-semibold">Profile unavailable</h1>
              <p className="mt-2 text-sm text-muted-foreground">{loadState.message}</p>
              <button
                onClick={() => setRetryVersion((value) => value + 1)}
                className="mt-5 inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm"
              >
                <RefreshCw className="h-4 w-4" /> Retry
              </button>
            </div>
          )}
        </main>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main className="container mx-auto max-w-5xl px-4 py-8">
        <button onClick={() => navigate(-1)} className="mb-6 flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" /> Back
        </button>

        <motion.section initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} className="card-glass mb-6 p-6">
          <div className="flex flex-col gap-6 md:flex-row">
            <div className="relative shrink-0">
              <img src={avatarFor(profile)} alt={profile.displayName} className="h-36 w-36 rounded-full object-cover" />
              {profile.verificationStatus === "Approved" && (
                <CheckCircle className="absolute bottom-1 right-1 h-7 w-7 rounded-full bg-background text-green-400" />
              )}
            </div>
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <h1 className="text-3xl font-bold">{profile.displayName}</h1>
                  <div className="mt-2 flex flex-wrap gap-4 text-sm text-muted-foreground">
                    <span className="flex items-center gap-1"><MapPin className="h-4 w-4" />{profile.city}</span>
                    <span className="flex items-center gap-1 text-gold"><Star className="h-4 w-4 fill-current" />{profile.rating.toFixed(1)} ({profile.reviewsCount})</span>
                  </div>
                </div>
                <div className="flex gap-2">
                  {!isOwnProfile && (
                    <button
                      disabled={actionPending}
                      onClick={() => void toggleFollow()}
                      aria-pressed={profile.isFollowing}
                      className={`flex items-center gap-2 rounded-lg border px-3 py-2 text-sm ${profile.isFollowing ? "border-red-500/40 text-red-300" : "border-border"}`}
                    >
                      <Heart className={`h-4 w-4 ${profile.isFollowing ? "fill-current" : ""}`} />
                      {profile.isFollowing ? "Following" : "Follow"}
                    </button>
                  )}
                  <button
                    onClick={() => {
                      void navigator.clipboard.writeText(window.location.href);
                      toast.success("Profile link copied.");
                    }}
                    aria-label="Copy profile link"
                    className="rounded-lg border border-border p-2"
                  >
                    <Share2 className="h-5 w-5 text-muted-foreground" />
                  </button>
                </div>
              </div>
              <div className="mt-6 grid grid-cols-2 gap-3 border-t border-border pt-4 sm:grid-cols-4">
                <Stat value={profile.followersCount} label="Followers" />
                <Stat value={profile.followingCount} label="Following" />
                <Stat value={profile.reviewsCount} label="Reviews" />
                <Stat value={profile.verificationStatus} label="Verification" />
              </div>
            </div>
          </div>
        </motion.section>

        <div className="grid gap-6 lg:grid-cols-[1fr_22rem]">
          <div className="space-y-6">
            <section className="card-glass p-6">
              <h2 className="mb-3 font-bold">About {profile.displayName}</h2>
              <p className="text-sm leading-relaxed text-muted-foreground">{profile.bio || "This buddy has not added a biography yet."}</p>
              <div className="mt-4 flex flex-wrap gap-2">
                {profile.interests.length > 0
                  ? profile.interests.map((interest) => <span key={interest.id} className="rounded-full bg-secondary px-3 py-1 text-xs">{interest.name}</span>)
                  : <span className="text-xs text-muted-foreground">No interests listed.</span>}
              </div>
            </section>

            <section className="card-glass p-6">
              <h2 className="mb-3 flex items-center gap-2 font-bold"><Languages className="h-4 w-4" /> Languages</h2>
              <p className="text-sm text-muted-foreground">{profile.languages || "Not specified"}</p>
            </section>

            <section className="card-glass p-6">
              <h2 className="mb-4 font-bold">Traveler reviews</h2>
              {reviews.length === 0 ? (
                <p className="text-sm text-muted-foreground">No completed-trip reviews yet.</p>
              ) : (
                <div className="space-y-3">
                  {reviews.map((review) => (
                    <article key={review.id} className="rounded-xl bg-secondary/35 p-4">
                      <div className="flex justify-between gap-3 text-sm">
                        <strong>{review.reviewerName}</strong>
                        <span className="text-gold">{review.rating}/5 ★</span>
                      </div>
                      <p className="mt-2 text-sm text-muted-foreground">{review.reviewText}</p>
                    </article>
                  ))}
                </div>
              )}
              {reviewableBooking && (
                <div className="mt-5 border-t border-border pt-4">
                  <h3 className="mb-2 text-sm font-semibold">Review your completed buddy experience</h3>
                  <div className="flex gap-2">
                    <select value={reviewRating} onChange={(event) => setReviewRating(Number(event.target.value))} className="rounded-lg bg-secondary px-2 text-sm">
                      {[5, 4, 3, 2, 1].map((rating) => <option key={rating} value={rating}>{rating}/5</option>)}
                    </select>
                    <input value={reviewText} onChange={(event) => setReviewText(event.target.value)} placeholder="Share your experience" className="input-glass min-w-0 flex-1" />
                    <button disabled={actionPending || !reviewText.trim()} onClick={() => void submitReview()} className="rounded-lg bg-secondary px-4 text-sm disabled:opacity-50">Post</button>
                  </div>
                </div>
              )}
            </section>
          </div>

          <aside className="space-y-4">
            <section className="card-glass p-5">
              <h2 className="mb-3 flex items-center gap-2 font-bold"><Calendar className="h-4 w-4" /> Request this buddy</h2>
              {availability.length === 0 ? (
                <p className="text-sm text-muted-foreground">No future availability has been published.</p>
              ) : (
                <>
                  <select
                    value={selectedAvailabilityId}
                    onChange={(event) => setSelectedAvailabilityId(event.target.value)}
                    className="w-full rounded-lg border border-border bg-secondary px-3 py-2 text-xs"
                    aria-label="Buddy availability"
                  >
                    <option value="">Choose a time</option>
                    {availability.map((slot) => (
                      <option key={slot.id} value={slot.id} disabled={slot.isBooked}>
                        {new Date(slot.startTimeUtc).toLocaleString()} · ${slot.price}{slot.isBooked ? " · requested" : ""}
                      </option>
                    ))}
                  </select>
                  <textarea
                    value={bookingNotes}
                    onChange={(event) => setBookingNotes(event.target.value)}
                    className="input-glass mt-3 min-h-20 w-full"
                    placeholder="Optional trip or activity notes"
                    maxLength={1000}
                  />
                  <div className="mt-3 flex items-center justify-between">
                    <span className="text-lg font-bold">{selectedSlot ? `$${selectedSlot.price}` : "—"}</span>
                    <button
                      disabled={!selectedAvailabilityId || actionPending || isOwnProfile}
                      onClick={() => void requestBuddy()}
                      className="btn-accent rounded-lg px-4 py-2 text-sm disabled:opacity-50"
                    >
                      Send request
                    </button>
                  </div>
                </>
              )}
            </section>
            {!isOwnProfile && (
              <button onClick={connect} className="btn-accent flex w-full items-center justify-center gap-2 rounded-lg px-5 py-3">
                <MessageSquare className="h-4 w-4" /> Message {profile.displayName.split(" ")[0]}
              </button>
            )}
          </aside>
        </div>
      </main>
      <Footer />
    </div>
  );
};

const Stat = ({ value, label }: { value: string | number; label: string }) => (
  <div className="text-center">
    <p className="flex items-center justify-center gap-1 text-lg font-bold">
      {label === "Followers" && <Users className="h-4 w-4" />}{value}
    </p>
    <p className="text-[11px] text-muted-foreground">{label}</p>
  </div>
);

export default ProfilePage;
