import { useEffect, useMemo, useState } from "react";
import {
  Calendar,
  ChevronLeft,
  ChevronRight,
  Clock,
  ExternalLink,
  Globe2,
  Images,
  MapPin,
  Phone,
  Star,
  Users,
  X,
} from "lucide-react";
import LeafletMap from "@/components/LeafletMap";
import { authStorage } from "@/shared/lib/auth";
import { formatExperiencePrice, formatUsdPrice } from "@/shared/lib/price";
import { experiencesApi } from "@/shared/services/api-experiences";
import type {
  ExperienceAvailabilityDto,
  ExperienceResponseDto,
  ExperienceReviewDto,
  ExperienceVisitInsightDto,
} from "@/shared/types/api";
import { toast } from "sonner";

interface ExperienceDetailsModalProps {
  experience: ExperienceResponseDto;
  availability: ExperienceAvailabilityDto[];
  initialReviews: ExperienceReviewDto[];
  initialInsight?: ExperienceVisitInsightDto;
  fallbackImage: string;
  onClose: () => void;
}

const reviewsPageSize = 5;
const dayOrder = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

const safeExternalUrl = (value?: string) => {
  if (!value) return undefined;
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:" ? url.toString() : undefined;
  } catch {
    return undefined;
  }
};

const toLocalDateTimeInput = (value = new Date()) => {
  const offset = value.getTimezoneOffset() * 60_000;
  return new Date(value.getTime() - offset).toISOString().slice(0, 16);
};

const formatOpenStatus = (insight?: ExperienceVisitInsightDto) => {
  if (!insight || insight.openStatus === "unknown") return "Hours unknown";
  return insight.openStatus === "open" ? "Open now" : "Closed now";
};

const formatTime = (value: string) => {
  const [hours = "0", minutes = "0"] = value.split(":");
  const date = new Date(2000, 0, 1, Number(hours), Number(minutes));
  return date.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });
};

const formatDuration = (startTimeUtc: string, endTimeUtc: string) => {
  const minutes = Math.max(
    0,
    Math.round((new Date(endTimeUtc).getTime() - new Date(startTimeUtc).getTime()) / 60_000),
  );
  if (minutes < 60) return `${minutes} min`;
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  return remainingMinutes === 0 ? `${hours} hr` : `${hours} hr ${remainingMinutes} min`;
};

const ExperienceDetailsModal = ({
  experience,
  availability,
  initialReviews,
  initialInsight,
  fallbackImage,
  onClose,
}: ExperienceDetailsModalProps) => {
  const images = experience.featuredImages.length > 0
    ? experience.featuredImages.map((image) => image.link)
    : [fallbackImage];
  const activeSlots = availability.filter((slot) => slot.isActive && slot.remainingCapacity > 0);
  const [activeImage, setActiveImage] = useState(0);
  const [selectedAvailabilityId, setSelectedAvailabilityId] = useState(activeSlots[0]?.id ?? "");
  const [guestsCount, setGuestsCount] = useState(1);
  const [reviews, setReviews] = useState(initialReviews);
  const [reviewPage, setReviewPage] = useState(1);
  const [hasMoreReviews, setHasMoreReviews] = useState(initialReviews.length === reviewsPageSize);
  const [loadingMore, setLoadingMore] = useState(false);
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewText, setReviewText] = useState("");
  const [visitAt, setVisitAt] = useState(
    toLocalDateTimeInput(initialInsight ? new Date(initialInsight.requestedAt) : new Date()),
  );
  const [visitInsight, setVisitInsight] = useState(initialInsight);
  const [insightLoading, setInsightLoading] = useState(false);

  const selectedSlot = activeSlots.find((slot) => slot.id === selectedAvailabilityId);
  const website = safeExternalUrl(experience.website);
  const googleMapsLink = safeExternalUrl(experience.googleMapsLink)
    ?? (experience.latitude != null && experience.longitude != null
      ? `https://www.google.com/maps/search/?api=1&query=${experience.latitude},${experience.longitude}`
      : undefined);
  const totalReviewCount = experience.reviews
    ?? experience.reviewsPerRating.reduce((sum, item) => sum + item.reviewsCount, 0);
  const maximumRatingCount = Math.max(1, ...experience.reviewsPerRating.map((item) => item.reviewsCount));

  const popularTimes = useMemo(() => {
    const requestedDay = visitInsight?.dayOfWeek;
    const matching = experience.popularTimes
      .filter((item) => !requestedDay || item.dayOfWeek.toLowerCase() === requestedDay.toLowerCase())
      .sort((left, right) => left.hourOfDay - right.hourOfDay);
    return matching.length > 0 ? matching : experience.popularTimes.slice(0, 24);
  }, [experience.popularTimes, visitInsight?.dayOfWeek]);

  const hours = useMemo(
    () => [...experience.hours].sort(
      (left, right) => dayOrder.indexOf(left.dayOfWeek) - dayOrder.indexOf(right.dayOfWeek),
    ),
    [experience.hours],
  );

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  const loadVisitInsight = async () => {
    if (!visitAt) return;
    setInsightLoading(true);
    try {
      setVisitInsight(await experiencesApi.getVisitInsights(
        experience.id,
        new Date(visitAt).toISOString(),
      ));
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load visit insights.");
    } finally {
      setInsightLoading(false);
    }
  };

  const loadMoreReviews = async () => {
    if (loadingMore || !hasMoreReviews) return;
    const nextPage = reviewPage + 1;
    setLoadingMore(true);
    try {
      const loaded = await experiencesApi.getReviews(experience.id, nextPage, reviewsPageSize);
      setReviews((current) => {
        const known = new Set(current.map((review) => review.id));
        return [...current, ...loaded.filter((review) => !known.has(review.id))];
      });
      setReviewPage(nextPage);
      setHasMoreReviews(loaded.length === reviewsPageSize);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load more reviews.");
    } finally {
      setLoadingMore(false);
    }
  };

  const submitReview = async () => {
    if (!reviewText.trim()) return;
    try {
      const created = await experiencesApi.createReview(experience.id, {
        rating: reviewRating,
        reviewText: reviewText.trim(),
      });
      setReviews((current) => [created, ...current.filter((review) => review.id !== created.id)]);
      setReviewText("");
      toast.success("Review submitted.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not submit review.");
    }
  };

  const bookExperience = async () => {
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in as a traveler to book this experience.");
      return;
    }
    if (!selectedSlot) {
      toast.error("Select an available date first.");
      return;
    }
    try {
      await experiencesApi.createBooking(experience.id, {
        availabilityId: selectedSlot.id,
        guestsCount,
      });
      toast.success(`"${experience.name}" booking requested.`);
      onClose();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not create booking.");
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-2 backdrop-blur-sm sm:p-4"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
      aria-labelledby="experience-detail-title"
    >
      <div
        className="max-h-[96vh] w-full max-w-6xl overflow-y-auto rounded-2xl border border-border bg-background shadow-2xl"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="relative min-h-64 overflow-hidden rounded-t-2xl bg-secondary sm:min-h-80">
          <img
            src={images[activeImage]}
            alt={`${experience.name} photo ${activeImage + 1}`}
            className="h-64 w-full object-cover sm:h-80"
            onError={(event) => {
              event.currentTarget.src = fallbackImage;
            }}
          />
          <div className="absolute inset-x-0 top-0 flex items-start justify-between bg-gradient-to-b from-black/70 to-transparent p-4">
            <div className="flex flex-wrap gap-2">
              <span className="rounded-full bg-black/60 px-3 py-1 text-xs font-medium text-white">
                {experience.category}
              </span>
              <span className="rounded-full bg-black/60 px-3 py-1 text-xs text-white">
                {experience.sourceType === "Provider" ? "Hosted experience" : "Listed experience"}
              </span>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="rounded-full bg-black/60 p-2 text-white transition hover:bg-black/80"
              aria-label="Close experience details"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
          {images.length > 1 && (
            <>
              <button
                type="button"
                onClick={() => setActiveImage((index) => (index - 1 + images.length) % images.length)}
                className="absolute left-3 top-1/2 rounded-full bg-black/55 p-2 text-white"
                aria-label="Previous image"
              >
                <ChevronLeft className="h-5 w-5" />
              </button>
              <button
                type="button"
                onClick={() => setActiveImage((index) => (index + 1) % images.length)}
                className="absolute right-3 top-1/2 rounded-full bg-black/55 p-2 text-white"
                aria-label="Next image"
              >
                <ChevronRight className="h-5 w-5" />
              </button>
              <span className="absolute bottom-3 right-3 flex items-center gap-1 rounded-full bg-black/60 px-3 py-1 text-xs text-white">
                <Images className="h-3.5 w-3.5" /> {activeImage + 1}/{images.length}
              </span>
            </>
          )}
        </div>

        {images.length > 1 && (
          <div className="flex gap-2 overflow-x-auto border-b border-border p-3">
            {images.map((image, index) => (
              <button
                type="button"
                key={`${image}-${index}`}
                onClick={() => setActiveImage(index)}
                className={`h-16 w-24 shrink-0 overflow-hidden rounded-lg border-2 ${
                  activeImage === index ? "border-accent" : "border-transparent"
                }`}
                aria-label={`Show image ${index + 1}`}
              >
                <img src={image} alt="" className="h-full w-full object-cover" />
              </button>
            ))}
          </div>
        )}

        <div className="grid gap-6 p-4 sm:p-6 lg:grid-cols-[minmax(0,1fr)_21rem]">
          <main className="min-w-0 space-y-7">
            <section>
              <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
                <div>
                  <h2 id="experience-detail-title" className="text-2xl font-bold sm:text-3xl">
                    {experience.name}
                  </h2>
                  <p className="mt-2 flex items-start gap-2 text-sm text-muted-foreground">
                    <MapPin className="mt-0.5 h-4 w-4 shrink-0" />
                    {experience.address || "Address unavailable"}
                  </p>
                </div>
                <div className="shrink-0 sm:text-right">
                  <p className="text-xs uppercase tracking-wide text-muted-foreground">Starting from</p>
                  <p className="text-xl font-bold text-accent">
                    {formatExperiencePrice(experience.startingPricePerPerson, experience.priceRange)}
                  </p>
                </div>
              </div>
              <div className="mt-4 flex flex-wrap items-center gap-3 text-sm">
                <span className="flex items-center gap-1 text-gold">
                  <Star className="h-4 w-4 fill-current" />
                  {experience.rating?.toFixed(1) ?? "Not rated"} ({totalReviewCount} reviews)
                </span>
                <span className={`rounded-full px-3 py-1 text-xs font-medium ${
                  visitInsight?.openStatus === "open"
                    ? "bg-green-500/15 text-green-400"
                    : visitInsight?.openStatus === "closed"
                      ? "bg-red-500/15 text-red-400"
                      : "bg-secondary text-muted-foreground"
                }`}>
                  {formatOpenStatus(visitInsight)}
                </span>
                {visitInsight?.popularityPercentage != null && (
                  <span className="flex items-center gap-1 text-muted-foreground">
                    <Users className="h-4 w-4" />
                    {visitInsight.crowdLevel} · {visitInsight.popularityPercentage}% busy
                  </span>
                )}
              </div>
              <p className="mt-5 whitespace-pre-line text-sm leading-7 text-foreground/90">
                {experience.description || "No description is available for this experience."}
              </p>
            </section>

            <section className="grid gap-4 md:grid-cols-2">
              <div className="rounded-xl border border-border p-4">
                <h3 className="flex items-center gap-2 font-semibold">
                  <Clock className="h-4 w-4 text-accent" /> Opening hours
                </h3>
                {hours.length === 0 ? (
                  <p className="mt-3 text-sm text-muted-foreground">Opening hours are unavailable.</p>
                ) : (
                  <dl className="mt-3 space-y-2 text-sm">
                    {hours.map((hour) => (
                      <div key={hour.id} className="flex justify-between gap-3">
                        <dt className="text-muted-foreground">{hour.dayOfWeek}</dt>
                        <dd>{formatTime(hour.opensAt)} – {formatTime(hour.closesAt)}</dd>
                      </div>
                    ))}
                  </dl>
                )}
              </div>
              <div className="rounded-xl border border-border p-4">
                <h3 className="font-semibold">Amenities</h3>
                {experience.amenities.length === 0 ? (
                  <p className="mt-3 text-sm text-muted-foreground">No amenities are listed.</p>
                ) : (
                  <div className="mt-3 flex flex-wrap gap-2">
                    {experience.amenities.map((amenity) => (
                      <span key={amenity} className="rounded-full bg-secondary px-3 py-1 text-xs">
                        {amenity}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </section>

            <section className="rounded-xl border border-border p-4">
              <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
                <div>
                  <h3 className="font-semibold">Plan the best time to visit</h3>
                  <p className="text-xs text-muted-foreground">Check opening status and expected crowd level.</p>
                </div>
                <div className="flex gap-2">
                  <input
                    type="datetime-local"
                    value={visitAt}
                    onChange={(event) => setVisitAt(event.target.value)}
                    className="min-w-0 rounded-lg border border-border bg-background px-2 py-2 text-xs"
                    aria-label="Visit date and time"
                  />
                  <button
                    type="button"
                    disabled={!visitAt || insightLoading}
                    onClick={() => void loadVisitInsight()}
                    className="rounded-lg border border-border px-3 py-2 text-xs disabled:opacity-40"
                  >
                    {insightLoading ? "Checking…" : "Check"}
                  </button>
                </div>
              </div>
              {visitInsight?.bestKnownOpenWindow && (
                <p className="mt-3 text-xs text-muted-foreground">
                  Best known open window: {visitInsight.bestKnownOpenWindow}
                </p>
              )}
              {popularTimes.length > 0 && (
                <div className="mt-5">
                  <p className="mb-2 text-xs font-medium">
                    Popular times{visitInsight?.dayOfWeek ? ` · ${visitInsight.dayOfWeek}` : ""}
                  </p>
                  <div className="flex h-28 items-end gap-1 overflow-x-auto pb-5">
                    {popularTimes.map((time) => (
                      <div
                        key={`${time.dayOfWeek}-${time.hourOfDay}`}
                        className="group relative flex h-full min-w-5 flex-1 items-end"
                        title={`${time.hourOfDay}:00 · ${time.popularityPercentage}% busy`}
                      >
                        <div
                          className={`w-full rounded-t transition ${
                            time.hourOfDay === visitInsight?.hourOfDay ? "bg-accent" : "bg-accent/35"
                          }`}
                          style={{ height: `${Math.max(5, time.popularityPercentage)}%` }}
                        />
                        {time.hourOfDay % 3 === 0 && (
                          <span className="absolute -bottom-5 left-1/2 -translate-x-1/2 text-[9px] text-muted-foreground">
                            {time.hourOfDay}
                          </span>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </section>

            <section className="grid gap-4 md:grid-cols-2">
              <div className="rounded-xl border border-border p-4">
                <h3 className="font-semibold">Rating breakdown</h3>
                {experience.reviewsPerRating.length === 0 ? (
                  <p className="mt-3 text-sm text-muted-foreground">No rating distribution yet.</p>
                ) : (
                  <div className="mt-3 space-y-2">
                    {[5, 4, 3, 2, 1].map((rating) => {
                      const count = experience.reviewsPerRating.find((item) => item.rating === rating)?.reviewsCount ?? 0;
                      return (
                        <div key={rating} className="grid grid-cols-[2rem_1fr_2.5rem] items-center gap-2 text-xs">
                          <span>{rating} ★</span>
                          <div className="h-2 overflow-hidden rounded-full bg-secondary">
                            <div
                              className="h-full rounded-full bg-gold"
                              style={{ width: `${(count / maximumRatingCount) * 100}%` }}
                            />
                          </div>
                          <span className="text-right text-muted-foreground">{count}</span>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
              <div className="rounded-xl border border-border p-4">
                <h3 className="font-semibold">Contact & links</h3>
                <div className="mt-3 space-y-3 text-sm">
                  {experience.phoneInternational && (
                    <a className="flex items-center gap-2 hover:text-accent" href={`tel:${experience.phoneInternational}`}>
                      <Phone className="h-4 w-4" /> {experience.phoneInternational}
                    </a>
                  )}
                  {website && (
                    <a className="flex items-center gap-2 hover:text-accent" href={website} target="_blank" rel="noreferrer">
                      <Globe2 className="h-4 w-4" /> Official website <ExternalLink className="h-3 w-3" />
                    </a>
                  )}
                  {googleMapsLink && (
                    <a className="flex items-center gap-2 hover:text-accent" href={googleMapsLink} target="_blank" rel="noreferrer">
                      <MapPin className="h-4 w-4" /> Open in Google Maps <ExternalLink className="h-3 w-3" />
                    </a>
                  )}
                  {!experience.phoneInternational && !website && !googleMapsLink && (
                    <p className="text-muted-foreground">Contact details are unavailable.</p>
                  )}
                </div>
              </div>
            </section>

            {experience.latitude != null && experience.longitude != null && (
              <section>
                <h3 className="mb-3 font-semibold">Location</h3>
                <div className="overflow-hidden rounded-xl border border-border">
                  <LeafletMap
                    center={[experience.latitude, experience.longitude]}
                    zoom={14}
                    height="260px"
                    showSearch={false}
                    showFullscreen={false}
                    showLegend={false}
                    markers={[{
                      id: experience.id,
                      lat: experience.latitude,
                      lng: experience.longitude,
                      name: experience.name,
                      cheapestPrice: experience.startingPricePerPerson,
                    }]}
                  />
                </div>
              </section>
            )}

            {experience.featuredReviews.length > 0 && (
              <section>
                <h3 className="mb-3 font-semibold">Featured traveler reviews</h3>
                <div className="grid gap-3 md:grid-cols-2">
                  {experience.featuredReviews.map((review) => (
                    <article key={review.id} className="rounded-xl bg-secondary/40 p-4 text-sm">
                      <p className="font-medium">
                        {review.reviewerName || "Traveler"} · {review.rating ?? "—"}/5
                      </p>
                      <p className="mt-2 text-muted-foreground">{review.reviewText || "No written review."}</p>
                    </article>
                  ))}
                </div>
              </section>
            )}

            <section className="border-t border-border pt-6">
              <h3 className="font-semibold">All traveler reviews</h3>
              <div className="mt-3 space-y-3">
                {reviews.length === 0 && <p className="text-sm text-muted-foreground">No reviews yet.</p>}
                {reviews.map((review) => (
                  <article key={review.id} className="rounded-xl border border-border p-4 text-sm">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <p className="font-medium">{review.reviewerName || "Traveler"} · {review.rating ?? "—"}/5</p>
                      {review.publishedAtDate && (
                        <time className="text-xs text-muted-foreground">
                          {new Date(review.publishedAtDate).toLocaleDateString()}
                        </time>
                      )}
                    </div>
                    <p className="mt-2 text-muted-foreground">{review.reviewText || "No written review."}</p>
                  </article>
                ))}
              </div>
              {hasMoreReviews && (
                <button
                  type="button"
                  disabled={loadingMore}
                  onClick={() => void loadMoreReviews()}
                  className="mt-3 rounded-lg border border-border px-4 py-2 text-xs disabled:opacity-40"
                >
                  {loadingMore ? "Loading…" : "Load more reviews"}
                </button>
              )}
              {authStorage.hasAnyRole(["Traveler"]) && (
                <div className="mt-4 grid gap-2 rounded-xl bg-secondary/30 p-3 sm:grid-cols-[5rem_1fr_auto]">
                  <select
                    value={reviewRating}
                    onChange={(event) => setReviewRating(Number(event.target.value))}
                    className="rounded-lg border border-border bg-background px-2 text-xs"
                    aria-label="Review rating"
                  >
                    {[5, 4, 3, 2, 1].map((value) => <option key={value} value={value}>{value}/5</option>)}
                  </select>
                  <input
                    value={reviewText}
                    onChange={(event) => setReviewText(event.target.value)}
                    className="input-glass min-w-0"
                    placeholder="Write a review"
                  />
                  <button
                    type="button"
                    disabled={!reviewText.trim()}
                    onClick={() => void submitReview()}
                    className="rounded-lg bg-secondary px-4 py-2 text-xs disabled:opacity-40"
                  >
                    Post
                  </button>
                </div>
              )}
            </section>
          </main>

          <aside className="lg:sticky lg:top-4 lg:self-start">
            <div className="rounded-2xl border border-border bg-card p-4 shadow-lg">
              <h3 className="font-semibold">Choose availability</h3>
              {activeSlots.length === 0 ? (
                <p className="mt-3 rounded-lg bg-secondary/40 p-3 text-sm text-muted-foreground">
                  No bookable dates are currently available.
                </p>
              ) : (
                <>
                  <select
                    value={selectedAvailabilityId}
                    onChange={(event) => {
                      setSelectedAvailabilityId(event.target.value);
                      setGuestsCount(1);
                    }}
                    className="mt-3 w-full rounded-lg border border-border bg-background px-3 py-2 text-xs"
                  >
                    {activeSlots.map((slot) => (
                      <option key={slot.id} value={slot.id}>
                        {new Date(slot.startTimeUtc).toLocaleString()} · {formatUsdPrice(slot.pricePerPerson)}/person · {slot.remainingCapacity} left
                      </option>
                    ))}
                  </select>
                  {selectedSlot && (
                    <div className="mt-4 space-y-3 text-sm">
                      <div className="flex justify-between gap-3">
                        <span className="text-muted-foreground">Duration</span>
                        <span>{formatDuration(selectedSlot.startTimeUtc, selectedSlot.endTimeUtc)}</span>
                      </div>
                      <div className="flex justify-between gap-3">
                        <span className="text-muted-foreground">Availability</span>
                        <span>{selectedSlot.remainingCapacity} of {selectedSlot.capacity} places left</span>
                      </div>
                      <div className="flex items-center justify-between gap-3">
                        <label htmlFor="experience-guests" className="text-muted-foreground">Guests</label>
                        <input
                          id="experience-guests"
                          type="number"
                          min="1"
                          max={selectedSlot.remainingCapacity}
                          value={guestsCount}
                          onChange={(event) => {
                            const value = Number(event.target.value);
                            setGuestsCount(Math.min(selectedSlot.remainingCapacity, Math.max(1, value || 1)));
                          }}
                          className="w-20 rounded-lg border border-border bg-background px-2 py-1 text-right"
                        />
                      </div>
                      <div className="border-t border-border pt-3">
                        <div className="flex justify-between gap-3">
                          <span>{formatUsdPrice(selectedSlot.pricePerPerson)} × {guestsCount}</span>
                          <span>{formatUsdPrice(selectedSlot.pricePerPerson * guestsCount)}</span>
                        </div>
                        <div className="mt-2 flex justify-between text-base font-bold">
                          <span>Total</span>
                          <span className="text-accent">
                            {formatUsdPrice(selectedSlot.pricePerPerson * guestsCount)}
                          </span>
                        </div>
                      </div>
                    </div>
                  )}
                </>
              )}
              <button
                type="button"
                disabled={!selectedSlot}
                onClick={() => void bookExperience()}
                className="btn-accent mt-4 flex w-full items-center justify-center gap-2 rounded-lg py-2.5 disabled:cursor-not-allowed disabled:opacity-50"
              >
                <Calendar className="h-4 w-4" /> Book experience
              </button>
              <p className="mt-3 text-center text-[11px] text-muted-foreground">
                The exact slot price returned by the provider is used for your total.
              </p>
            </div>
          </aside>
        </div>
      </div>
    </div>
  );
};

export default ExperienceDetailsModal;
