import { useEffect, useId, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import {
  CalendarDays,
  Check,
  ChevronLeft,
  ChevronRight,
  ExternalLink,
  Globe2,
  Heart,
  Images,
  MapPin,
  Navigation,
  Phone,
  Star,
  Users,
  X,
} from "lucide-react";
import hotelImg from "@/assets/hotel-1.jpg";
import { computePrice } from "@/features/where-to-stay/pricing";
import type { Hotel } from "@/features/where-to-stay/types";
import type { StayReviewDto } from "@/shared/types/api";
import { staysApi } from "@/shared/services/api-stays";
import { authStorage } from "@/shared/lib/auth";
import { toast } from "sonner";
import type { LoadState } from "@/shared/types/async-state";
import { formatUsdPrice } from "@/shared/lib/price";
import { overlayLayers } from "@/shared/lib/overlay-layers";

interface HotelDetailsModalProps {
  hotel: Hotel | null;
  checkin: string;
  checkout: string;
  guests: number;
  onClose: () => void;
  onBook?: (hotel: NonNullable<HotelDetailsModalProps["hotel"]>) => void;
  onFavoriteChange?: (stayId: number, isFavorite: boolean) => void;
}

const reviewsPageSize = 5;

const safeExternalUrl = (value?: string) => {
  if (!value) return undefined;
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:" ? url.toString() : undefined;
  } catch {
    return undefined;
  }
};

const HotelDetailsModal = ({
  hotel,
  checkin,
  checkout,
  guests,
  onClose,
  onBook,
  onFavoriteChange,
}: HotelDetailsModalProps) => {
  const [activeImage, setActiveImage] = useState(0);
  const [reviews, setReviews] = useState<StayReviewDto[]>([]);
  const [reviewPage, setReviewPage] = useState(1);
  const [hasMoreReviews, setHasMoreReviews] = useState(false);
  const [reviewsState, setReviewsState] = useState<LoadState>({ status: "loading" });
  const [loadingMore, setLoadingMore] = useState(false);
  const [rating, setRating] = useState(5);
  const [reviewText, setReviewText] = useState("");
  const [isFavorite, setIsFavorite] = useState(false);
  const [favoritePending, setFavoritePending] = useState(false);
  const dialogRef = useRef<HTMLElement>(null);
  const onCloseRef = useRef(onClose);
  const titleId = useId();
  const activeHotelId = hotel?.id;

  useEffect(() => {
    setIsFavorite(false);
    if (!activeHotelId || !authStorage.isAuthenticated() ||
        !authStorage.hasAnyRole(["Traveler"])) return;
    staysApi.getFavoriteStatus(activeHotelId)
      .then((status) => setIsFavorite(status.isFavorite))
      .catch(() => undefined);
  }, [activeHotelId]);

  const toggleFavorite = async () => {
    if (!activeHotelId) return;
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in as a traveler to save hotels.");
      return;
    }
    const previous = isFavorite;
    setIsFavorite(!previous);
    setFavoritePending(true);
    try {
      if (previous) await staysApi.removeFavorite(activeHotelId);
      else await staysApi.addFavorite(activeHotelId);
      onFavoriteChange?.(activeHotelId, !previous);
      window.dispatchEvent(new CustomEvent("stay-favorites-change", {
        detail: { stayId: activeHotelId, isFavorite: !previous },
      }));
    } catch (error) {
      setIsFavorite(previous);
      toast.error(error instanceof Error ? error.message : "Could not update favorites.");
    } finally {
      setFavoritePending(false);
    }
  };

  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useEffect(() => {
    if (!activeHotelId) return;

    const previouslyFocused = document.activeElement as HTMLElement | null;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const focusDialog = window.requestAnimationFrame(() => {
      const firstFocusable = dialogRef.current?.querySelector<HTMLElement>(
        'button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      );
      (firstFocusable ?? dialogRef.current)?.focus();
    });

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onCloseRef.current();
        return;
      }
      if (event.key !== "Tab" || !dialogRef.current) return;

      const focusable = Array.from(dialogRef.current.querySelectorAll<HTMLElement>(
        'button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      )).filter((element) => element.getAttribute("aria-hidden") !== "true");

      if (focusable.length === 0) {
        event.preventDefault();
        dialogRef.current.focus();
        return;
      }

      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => {
      window.cancelAnimationFrame(focusDialog);
      document.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = previousOverflow;
      if (previouslyFocused?.isConnected) previouslyFocused.focus();
    };
  }, [activeHotelId]);

  useEffect(() => {
    setActiveImage(0);
    setReviews([]);
    setReviewPage(1);
    setHasMoreReviews(false);
    if (!hotel?.id) {
      setReviewsState({ status: "ready" });
      return;
    }

    let active = true;
    setReviewsState({ status: "loading" });
    staysApi.getReviews(hotel.id, 1, reviewsPageSize)
      .then((loadedReviews) => {
        if (!active) return;
        setReviews(loadedReviews);
        setHasMoreReviews(loadedReviews.length === reviewsPageSize);
        setReviewsState({ status: "ready" });
      })
      .catch((error: unknown) => {
        if (!active) return;
        setReviewsState({
          status: "error",
          message: error instanceof Error ? error.message : "Could not load guest reviews.",
        });
      });
    return () => {
      active = false;
    };
  }, [hotel?.id]);

  const loadMoreReviews = async () => {
    if (!hotel?.id || loadingMore || !hasMoreReviews) return;
    const nextPage = reviewPage + 1;
    setLoadingMore(true);
    try {
      const loaded = await staysApi.getReviews(hotel.id, nextPage, reviewsPageSize);
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
    if (!hotel?.id || !reviewText.trim()) return;
    try {
      const created = await staysApi.createReview(hotel.id, { rating, reviewText });
      setReviews((current) => [created, ...current.filter((review) => review.id !== created.id)]);
      setReviewText("");
      toast.success("Review submitted.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not submit review.");
    }
  };

  const gallery = useMemo(
    () => hotel?.images.length ? hotel.images.map((image) => image.link) : [hotelImg],
    [hotel],
  );
  const price = computePrice(hotel?.price, checkin, checkout);
  const reviewDistributionTotal = hotel?.reviewsPerRating.reduce(
    (total, item) => total + item.reviewsCount,
    0,
  ) ?? 0;

  if (!hotel) return null;
  const websiteHref = safeExternalUrl(hotel.website);
  const mapsHref = safeExternalUrl(hotel.googleMapsLink);

  const changeImage = (direction: -1 | 1) => {
    setActiveImage((current) => (current + direction + gallery.length) % gallery.length);
  };

  return createPortal(
    <div
      className={`${overlayLayers.modalBackdrop} fixed inset-0 flex items-center justify-center bg-black/75 p-2 backdrop-blur-md sm:p-4`}
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <section
        ref={dialogRef}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className={`${overlayLayers.modalContent} relative max-h-[96vh] w-full max-w-6xl overflow-y-auto rounded-2xl border border-white/10 bg-[#101116] shadow-2xl outline-none`}
      >
        <button
          type="button"
          onClick={onClose}
          aria-label="Close hotel details"
          className="absolute right-4 top-4 flex h-10 w-10 items-center justify-center rounded-full border border-white/15 bg-black/75 text-white backdrop-blur transition hover:bg-black"
        >
          <X className="h-5 w-5" />
        </button>
        {activeHotelId && (
          <button
            type="button"
            onClick={() => void toggleFavorite()}
            disabled={favoritePending}
            aria-label={`${isFavorite ? "Remove" : "Add"} ${hotel.name} ${isFavorite ? "from" : "to"} favorites`}
            className="absolute right-16 top-4 flex h-10 items-center gap-2 rounded-full border border-white/15 bg-black/75 px-3 text-xs font-semibold text-white backdrop-blur transition hover:bg-black disabled:opacity-50"
          >
            <Heart className={`h-4 w-4 ${isFavorite ? "fill-brand-gold text-brand-gold" : ""}`} />
            <span className="hidden sm:inline">{isFavorite ? "Saved" : "Save"}</span>
          </button>
        )}

        <div className="relative h-64 overflow-hidden rounded-t-2xl bg-white/5 sm:h-80">
          <img
            src={gallery[activeImage]}
            alt={`${hotel.name} — image ${activeImage + 1}`}
            className="h-full w-full object-cover"
            onError={(event) => {
              event.currentTarget.onerror = null;
              event.currentTarget.src = hotelImg;
            }}
          />
          <div className="absolute inset-0 bg-gradient-to-t from-[#101116] via-transparent to-black/20" />
          {gallery.length > 1 && (
            <>
              <button type="button" onClick={() => changeImage(-1)} aria-label="Previous hotel image" className="absolute left-3 top-1/2 rounded-full bg-black/65 p-2 text-white backdrop-blur">
                <ChevronLeft className="h-5 w-5" />
              </button>
              <button type="button" onClick={() => changeImage(1)} aria-label="Next hotel image" className="absolute right-3 top-1/2 rounded-full bg-black/65 p-2 text-white backdrop-blur">
                <ChevronRight className="h-5 w-5" />
              </button>
            </>
          )}
          <div className="absolute bottom-4 left-4 right-4 flex items-end justify-between gap-3">
            <div>
              <div className="mb-2 flex flex-wrap gap-2">
                <span className="rounded-full bg-black/65 px-2.5 py-1 text-[10px] uppercase tracking-wider text-gray-200 backdrop-blur">
                  {hotel.sourceType === "HotelOwner" ? "Glinter host" : "Third-party listing"}
                </span>
                <span className="flex items-center gap-1 rounded-full bg-black/65 px-2.5 py-1 text-[10px] text-gray-200 backdrop-blur">
                  <Images className="h-3 w-3" /> {gallery.length} image{gallery.length === 1 ? "" : "s"}
                </span>
              </div>
              <h2 id={titleId} className="max-w-3xl text-2xl font-bold text-white sm:text-4xl">{hotel.name}</h2>
              <p className="mt-2 flex items-center gap-1 text-sm text-gray-300">
                <MapPin className="h-4 w-4" /> {hotel.area}
              </p>
            </div>
            <div className="hidden rounded-xl bg-black/65 px-4 py-3 text-right backdrop-blur sm:block">
              <strong className="text-2xl text-brand-gold">{formatUsdPrice(hotel.price)}</strong>
              <p className="text-xs text-gray-300">{hotel.price == null ? "No backend price" : "per night"}</p>
            </div>
          </div>
        </div>

        {gallery.length > 1 && (
          <div className="flex gap-2 overflow-x-auto border-b border-white/10 p-3">
            {gallery.map((image, index) => (
              <button
                type="button"
                key={`${image}-${index}`}
                onClick={() => setActiveImage(index)}
                aria-label={`Show hotel image ${index + 1}`}
                aria-pressed={activeImage === index}
                className={`h-16 w-24 shrink-0 overflow-hidden rounded-lg border-2 ${activeImage === index ? "border-brand-gold" : "border-transparent opacity-65 hover:opacity-100"}`}
              >
                <img
                  src={image}
                  alt=""
                  className="h-full w-full object-cover"
                  onError={(event) => {
                    event.currentTarget.onerror = null;
                    event.currentTarget.src = hotelImg;
                  }}
                />
              </button>
            ))}
          </div>
        )}

        <div className="grid gap-8 p-5 sm:p-7 lg:grid-cols-[minmax(0,1fr)_22rem]">
          <div className="min-w-0 space-y-8">
            <div className="flex flex-wrap items-center gap-3">
              <span className="flex items-center gap-1.5 rounded-full bg-amber-500/10 px-3 py-1.5 text-sm font-semibold text-amber-300">
                <Star className="h-4 w-4 fill-current" /> {hotel.rating || "Not rated"}
              </span>
              <span className="text-sm text-gray-400">{hotel.reviews} reviews</span>
              {hotel.updatedAtUtc && (
                <span className="text-xs text-gray-500">Updated {new Date(hotel.updatedAtUtc).toLocaleDateString()}</span>
              )}
            </div>

            <section>
              <h3 className="mb-3 text-lg font-semibold text-white">About this stay</h3>
              <p className="whitespace-pre-line text-sm leading-7 text-gray-300">
                {hotel.description || "The provider has not supplied a description for this stay."}
              </p>
            </section>

            <section>
              <h3 className="mb-3 text-lg font-semibold text-white">Amenities</h3>
              {hotel.amenities.length === 0 ? (
                <p className="text-sm text-gray-500">No amenities were supplied.</p>
              ) : (
                <div className="grid gap-2 sm:grid-cols-2">
                  {hotel.amenities.map((amenity) => (
                    <span key={amenity} className="flex items-center gap-2 rounded-xl border border-white/8 bg-white/[0.035] px-3 py-2.5 text-sm text-gray-300">
                      <Check className="h-4 w-4 shrink-0 text-emerald-400" />
                      {amenity}
                    </span>
                  ))}
                </div>
              )}
            </section>

            <section>
              <h3 className="mb-3 text-lg font-semibold text-white">Location and contact</h3>
              <div className="grid gap-3 sm:grid-cols-2">
                <InfoCard icon={MapPin} label="Location" value={hotel.regionNames?.join(" › ") || hotel.area} />
                {hotel.latitude != null && hotel.longitude != null && (
                  <InfoCard icon={Navigation} label="Coordinates" value={`${hotel.latitude.toFixed(5)}, ${hotel.longitude.toFixed(5)}`} />
                )}
                {hotel.phoneInternational && (
                  <InfoLink icon={Phone} label="Phone" value={hotel.phoneInternational} href={`tel:${hotel.phoneInternational}`} />
                )}
                {websiteHref && (
                  <InfoLink icon={Globe2} label="Website" value="Visit official website" href={websiteHref} />
                )}
                {mapsHref && (
                  <InfoLink icon={Navigation} label="Google Maps" value="Open location" href={mapsHref} />
                )}
              </div>
            </section>

            {hotel.reviewsPerRating.length > 0 && (
              <section>
                <h3 className="mb-4 text-lg font-semibold text-white">Rating distribution</h3>
                <div className="space-y-2">
                  {[5, 4, 3, 2, 1].map((score) => {
                    const count = hotel.reviewsPerRating.find((item) => item.rating === score)?.reviewsCount ?? 0;
                    const percentage = reviewDistributionTotal > 0 ? (count / reviewDistributionTotal) * 100 : 0;
                    return (
                      <div key={score} className="grid grid-cols-[2.5rem_1fr_3rem] items-center gap-3 text-xs">
                        <span className="flex items-center gap-1 text-gray-300">{score}<Star className="h-3 w-3 fill-amber-400 text-amber-400" /></span>
                        <div className="h-2 overflow-hidden rounded-full bg-white/8">
                          <div className="h-full rounded-full bg-gradient-to-r from-amber-500 to-yellow-300" style={{ width: `${percentage}%` }} />
                        </div>
                        <span className="text-right text-gray-500">{count}</span>
                      </div>
                    );
                  })}
                </div>
              </section>
            )}

            {hotel.featuredReviews.length > 0 && (
              <section>
                <h3 className="mb-3 text-lg font-semibold text-white">Featured reviews</h3>
                <div className="grid gap-3 sm:grid-cols-2">
                  {hotel.featuredReviews.slice(0, 4).map((review) => (
                    <ReviewCard key={`featured-${review.id}`} review={review} featured />
                  ))}
                </div>
              </section>
            )}

            <section className="border-t border-white/10 pt-7">
              <div className="mb-4 flex items-center justify-between gap-3">
                <h3 className="text-lg font-semibold text-white">All guest reviews</h3>
                <span className="text-xs text-gray-500">Page {reviewPage}</span>
              </div>
              {reviewsState.status === "loading" ? (
                <p className="text-sm text-gray-400">Loading reviews…</p>
              ) : reviewsState.status === "error" ? (
                <p role="alert" className="text-sm text-red-300">{reviewsState.message}</p>
              ) : reviews.length === 0 ? (
                <p className="text-sm text-gray-400">No reviews yet.</p>
              ) : (
                <div className="space-y-3">
                  {reviews.map((review) => <ReviewCard key={`review-${review.id}`} review={review} />)}
                  {hasMoreReviews && (
                    <button
                      type="button"
                      disabled={loadingMore}
                      onClick={() => void loadMoreReviews()}
                      className="w-full rounded-xl border border-white/10 py-2.5 text-sm text-gray-300 hover:bg-white/5 disabled:opacity-50"
                    >
                      {loadingMore ? "Loading…" : "Load more reviews"}
                    </button>
                  )}
                </div>
              )}

              {hotel.id && authStorage.hasAnyRole(["Traveler"]) && (
                <div className="mt-5 grid gap-2 rounded-xl border border-white/10 bg-white/[0.035] p-3 sm:grid-cols-[5rem_1fr_auto]">
                  <select value={rating} onChange={(event) => setRating(Number(event.target.value))} aria-label="Review rating" className="rounded-lg bg-[#191a20] px-2 text-sm">
                    {[5, 4, 3, 2, 1].map((value) => <option key={value} value={value}>{value}/5</option>)}
                  </select>
                  <input value={reviewText} onChange={(event) => setReviewText(event.target.value)} className="input-glass min-w-0" placeholder="Share your experience" />
                  <button type="button" onClick={() => void submitReview()} className="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-primary-foreground">Post</button>
                </div>
              )}
            </section>
          </div>

          <aside className="space-y-4 lg:sticky lg:top-4 lg:self-start">
            <section className="rounded-2xl border border-white/10 bg-white/[0.045] p-5 shadow-xl">
              <div className="mb-5 flex items-end justify-between gap-3">
                <div>
                  <strong className="text-3xl text-brand-gold">{formatUsdPrice(hotel.price)}</strong>
                  <p className="text-xs text-gray-400">{hotel.price == null ? "Price unavailable" : "backend nightly rate"}</p>
                </div>
                <span className="rounded-full bg-white/5 px-2 py-1 text-[10px] text-gray-400">{guests} guest{guests === 1 ? "" : "s"}</span>
              </div>

              <div className="grid grid-cols-2 gap-2 text-xs">
                <SummaryDatum icon={CalendarDays} label="Check-in" value={checkin || "Not selected"} />
                <SummaryDatum icon={CalendarDays} label="Check-out" value={checkout || "Not selected"} />
                <SummaryDatum icon={Users} label="Guests" value={String(guests)} />
                <SummaryDatum icon={CalendarDays} label="Nights" value={price.nights > 0 ? String(price.nights) : "—"} />
              </div>

              {price.nights > 0 && price.total != null ? (
                <div className="mt-5 space-y-2 border-t border-white/10 pt-4 text-sm">
                  <div className="flex justify-between text-gray-400">
                    <span>{formatUsdPrice(price.nightlyPrice)} × {price.nights} nights</span>
                    <span>{formatUsdPrice(price.total)}</span>
                  </div>
                  <div className="flex justify-between text-xs text-gray-500">
                    <span>Selected party</span>
                    <span>{guests} guest{guests === 1 ? "" : "s"}</span>
                  </div>
                  <div className="flex justify-between border-t border-white/10 pt-3 font-bold text-white">
                    <span>Backend booking total</span>
                    <span className="text-brand-gold">{formatUsdPrice(price.total)}</span>
                  </div>
                  <p className="text-[10px] leading-4 text-gray-500">The backend nightly rate is not multiplied by guest count.</p>
                </div>
              ) : (
                <p className="mt-4 rounded-lg bg-white/5 p-3 text-xs text-gray-400">
                  {!checkin || !checkout ? "Select valid stay dates to calculate the backend booking total." : "A backend price is unavailable for these dates."}
                </p>
              )}

              <button
                type="button"
                onClick={() => onBook?.(hotel)}
                disabled={hotel.price == null || price.nights < 1}
                className="mt-5 w-full rounded-xl bg-gradient-to-r from-brand-gold to-yellow-600 py-3 font-semibold text-brand-dark transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-45"
              >
                {hotel.price == null ? "Price unavailable" : price.nights < 1 ? "Select valid dates" : "Request booking"}
              </button>
            </section>

            {hotel.bookingPlatforms.length > 0 && (
              <section className="rounded-2xl border border-white/10 bg-white/[0.035] p-4">
                <h3 className="mb-3 font-semibold text-white">Booking-platform offers</h3>
                <div className="space-y-2">
                  {hotel.bookingPlatforms.map((platform) => (
                    <div key={`${platform.name}-${platform.link ?? ""}`} className="rounded-xl border border-white/8 bg-black/15 p-3">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-medium text-white">{platform.name}</p>
                          <p className="mt-1 text-xs text-gray-400">
                            {platform.priceWithTax == null ? "Tax-inclusive price unavailable" : `${formatUsdPrice(platform.priceWithTax)} including tax`}
                          </p>
                        </div>
                        {safeExternalUrl(platform.link) && (
                          <a href={safeExternalUrl(platform.link)} target="_blank" rel="noreferrer" aria-label={`Open ${platform.name} booking offer`} className="rounded-lg border border-white/10 p-2 text-brand-gold hover:bg-white/5">
                            <ExternalLink className="h-4 w-4" />
                          </a>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
                <p className="mt-3 text-[10px] leading-4 text-gray-500">Platform prices are displayed exactly as supplied by the backend and may use a different pricing basis.</p>
              </section>
            )}
          </aside>
        </div>
      </section>
    </div>,
    document.body,
  );
};

const InfoCard = ({ icon: Icon, label, value }: { icon: typeof MapPin; label: string; value: string }) => (
  <div className="rounded-xl border border-white/8 bg-white/[0.035] p-3">
    <span className="flex items-center gap-2 text-[10px] uppercase tracking-wider text-gray-500"><Icon className="h-3.5 w-3.5" />{label}</span>
    <p className="mt-2 text-sm text-gray-300">{value}</p>
  </div>
);

const InfoLink = ({ icon: Icon, label, value, href }: { icon: typeof MapPin; label: string; value: string; href: string }) => (
  <a href={href} target={href.startsWith("http") ? "_blank" : undefined} rel="noreferrer" className="rounded-xl border border-white/8 bg-white/[0.035] p-3 transition hover:border-primary/40 hover:bg-primary/5">
    <span className="flex items-center gap-2 text-[10px] uppercase tracking-wider text-gray-500"><Icon className="h-3.5 w-3.5" />{label}</span>
    <p className="mt-2 flex items-center gap-1 text-sm text-primary">{value}<ExternalLink className="h-3 w-3" /></p>
  </a>
);

const ReviewCard = ({ review, featured = false }: { review: StayReviewDto; featured?: boolean }) => (
  <article className={`rounded-xl border p-3 ${featured ? "border-amber-500/15 bg-amber-500/[0.04]" : "border-white/8 bg-white/[0.035]"}`}>
    <div className="flex items-start justify-between gap-3">
      <div>
        <p className="text-sm font-medium text-white">{review.reviewerName || "Traveler"}</p>
        <p className="mt-0.5 text-[10px] uppercase text-gray-500">{review.platform}</p>
      </div>
      <span className="flex items-center gap-1 text-xs text-amber-300"><Star className="h-3 w-3 fill-current" />{review.rating ?? "—"}</span>
    </div>
    <p className="mt-3 text-sm leading-6 text-gray-300">{review.reviewText || "No written review."}</p>
    {review.publishedAtDate && <p className="mt-2 text-[10px] text-gray-500">{new Date(review.publishedAtDate).toLocaleDateString()}</p>}
  </article>
);

const SummaryDatum = ({ icon: Icon, label, value }: { icon: typeof CalendarDays; label: string; value: string }) => (
  <div className="rounded-lg bg-white/5 p-2.5">
    <span className="flex items-center gap-1 text-[10px] text-gray-500"><Icon className="h-3 w-3" />{label}</span>
    <p className="mt-1 truncate font-medium text-gray-200">{value}</p>
  </div>
);

export default HotelDetailsModal;
