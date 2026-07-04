// ============================================================
// Profiles Module DTOs (matching backend)
// ============================================================

export interface InterestResponse {
  id: string;
  name: string;
}

export interface TravelerProfileResponse {
  profileId: string;
  userId: string;
  profileType: "Traveler";
  displayName: string;
  bio?: string;
  nationality?: string;
  preferredBudgetLevel?: string;
  travelStyle?: string;
  preferredInterests?: string;
  preferredVibes?: string;
  comfortLevel?: string;
  safetyPriority?: string;
  profileImageUrl?: string;
  interests: InterestResponse[];
  followersCount: number;
  followingCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface LocalBuddyListItemResponse {
  profileId: string;
  userId: string;
  displayName: string;
  bio?: string;
  city: string;
  languages?: string;
  profileImageUrl?: string;
  rating: number;
  reviewsCount: number;
  verificationStatus: string;
  interests: InterestResponse[];
  followersCount: number;
  followingCount: number;
  isFollowing: boolean;
}

export interface FollowStatusResponse {
  followedUserId: string;
  isFollowing: boolean;
  followersCount: number;
}

export interface BuddyAvailabilityDto {
  id: string;
  localBuddyUserId: string;
  startTimeUtc: string;
  endTimeUtc: string;
  price: number;
  isActive: boolean;
  isBooked: boolean;
}

export interface BuddyAvailabilityRequest {
  startTimeUtc: string;
  endTimeUtc: string;
  price: number;
}

export type BuddyBookingStatus =
  | "Pending"
  | "Accepted"
  | "Rejected"
  | "Completed"
  | "Cancelled";

export interface BuddyBookingDto {
  id: string;
  availabilityId: string;
  localBuddyUserId: string;
  buddyName: string;
  localBuddyDisplayName?: string;
  travelerUserId: string;
  travelerName: string;
  travelerDisplayName?: string;
  startTimeUtc: string;
  endTimeUtc: string;
  totalPrice: number;
  notes?: string;
  status: BuddyBookingStatus;
  createdAtUtc: string;
  updatedAtUtc?: string;
  respondedAtUtc?: string;
  canCancel: boolean;
  canReview: boolean;
  hasReview: boolean;
}

export interface BuddyReviewDto {
  id: string;
  bookingId: string;
  localBuddyUserId: string;
  travelerUserId: string;
  reviewerName: string;
  travelerDisplayName?: string;
  rating: number;
  reviewText: string;
  createdAtUtc: string;
}

export interface LocalBuddyProfileResponse {
  profileId: string;
  userId: string;
  profileType: "LocalBuddy";
  displayName: string;
  bio?: string;
  city: string;
  languages?: string;
  profileImageUrl?: string;
  rating: number;
  reviewsCount: number;
  verificationStatus: string;
  interests: InterestResponse[];
  createdAtUtc: string;
  updatedAtUtc?: string;
  followersCount: number;
  followingCount: number;
  isFollowing: boolean;
}

export interface TravelerProfileRequest {
  displayName: string;
  bio?: string;
  nationality?: string;
  preferredBudgetLevel?: string;
  travelStyle?: string;
  preferredInterests?: string;
  preferredVibes?: string;
  comfortLevel?: string;
  safetyPriority?: string;
  interestIds: string[];
}

export interface BuddyReviewSummaryDto {
  averageRating: number;
  reviewsCount: number;
}

// ============================================================
// Experiences Module DTOs (matching backend)
// ============================================================

export type ExperienceCategory = "Historical" | "Nature" | "Shopping" | "Nightlife" | "Dining";
export type ExperienceSourceType = "ThirdParty" | "Provider";
export type ExperienceBookingStatus = "Pending" | "Confirmed" | "Completed" | "Cancelled";
export type ExperienceModerationStatus = "Pending" | "Approved" | "Rejected";
export type ExperienceOpenStatus = "open" | "closed" | "unknown";

export interface ExperienceImageDto {
  id: number;
  link: string;
}

export interface ExperienceHourDto {
  id: number;
  dayOfWeek: string;
  opensAt: string;
  closesAt: string;
}

export interface ExperienceReviewDto {
  id: number;
  externalReviewId?: string;
  reviewerName?: string;
  rating?: number;
  reviewText?: string;
  publishedAtDate?: string;
  sourceList: string;
}

export interface ExperienceVisitInsightDto {
  requestedAt: string;
  dayOfWeek: string;
  hourOfDay: number;
  isOpen?: boolean;
  openStatus: ExperienceOpenStatus;
  popularityPercentage?: number;
  crowdLevel: string;
  bestKnownOpenWindow?: string;
}

export interface ExperienceMapItemDto {
  id: number;
  category: ExperienceCategory;
  sourceType: ExperienceSourceType;
  name: string;
  address?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  latitude?: number;
  longitude?: number;
  rating?: number;
  reviews?: number;
  startingPricePerPerson?: number;
  primaryImage?: string;
  isOpenNow?: boolean;
  popularityPercentageNow?: number;
}

export interface ExperienceImageUploadDto {
  link: string;
  fileName: string;
  sizeBytes: number;
}

export interface ImportExperiencesResult {
  created: number;
  updated: number;
  skipped: number;
}

export interface ExperienceReviewsForLlmDto {
  experienceId: number;
  experienceName: string;
  reviewsCount: number;
  reviewsText: string;
}

export interface ExperienceResponseDto {
  id: number;
  category: ExperienceCategory;
  sourceType: ExperienceSourceType;
  createdByUserId?: string;
  providerProfileId?: string;
  name: string;
  description?: string;
  address?: string;
  cid?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  latitude?: number;
  longitude?: number;
  featuredImages: ExperienceImageDto[];
  hours: ExperienceHourDto[];
  googleMapsLink?: string;
  popularTimes: Array<{
    id: number;
    dayOfWeek: string;
    hourOfDay: number;
    popularityPercentage: number;
  }>;
  phoneInternational?: string;
  priceRange?: string;
  startingPricePerPerson?: number;
  reviews?: number;
  rating?: number;
  reviewsPerRating: Array<{ rating: number; reviewsCount: number }>;
  website?: string;
  amenities: string[];
  featuredReviews: ExperienceReviewDto[];
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
  currentInsight?: ExperienceVisitInsightDto;
  moderationStatus: ExperienceModerationStatus;
  moderationNotes?: string;
  moderatedByUserId?: string;
  moderatedAtUtc?: string;
}

export type ExperienceSummaryDto = ExperienceResponseDto;

export interface CreateExperienceRequest {
  category: ExperienceCategory;
  name: string;
  description?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  featuredImageLinks: string[];
  hours: Array<{ dayOfWeek: string; opensAt: string; closesAt: string }>;
  googleMapsLink?: string;
  popularTimes: Array<{ dayOfWeek: string; hourOfDay: number; popularityPercentage: number }>;
  phoneInternational?: string;
  priceRange?: string;
  website?: string;
  amenities: string[];
}

export type UpdateExperienceRequest = CreateExperienceRequest;

export interface ExperienceAvailabilityDto {
  id: string;
  experienceId: number;
  startTimeUtc: string;
  endTimeUtc: string;
  capacity: number;
  remainingCapacity: number;
  pricePerPerson: number;
  isActive: boolean;
}

export interface CreateExperienceAvailabilityRequest {
  startTimeUtc: string;
  endTimeUtc: string;
  capacity: number;
  pricePerPerson: number;
}

export interface ExperienceBookingResponseDto {
  id: string;
  experienceId: number;
  experienceName: string;
  availabilityId: string;
  travelerProfileId: string;
  travelerName: string;
  startTimeUtc: string;
  endTimeUtc: string;
  guestsCount: number;
  totalPrice: number;
  status: ExperienceBookingStatus;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface CreateExperienceBookingRequest {
  availabilityId: string;
  guestsCount: number;
}

export type ExperienceSortBy =
  | "Recommended"
  | "Price"
  | "Rating"
  | "Reviews"
  | "Name"
  | "Newest"
  | "Popularity"
  | "OpenNow"
  | "Distance";
export type SortDirection = "Asc" | "Desc";

export interface GetExperiencesRequest {
  category?: ExperienceCategory;
  sourceType?: ExperienceSourceType;
  search?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  minRating?: number;
  isFree?: boolean;
  sortBy?: ExperienceSortBy;
  sortDirection?: SortDirection;
  currentLatitude?: number;
  currentLongitude?: number;
  page?: number;
  pageSize?: number;
}

export interface CreateExperienceReviewRequest {
  rating: number;
  reviewText: string;
}

// ============================================================
// Stays Module DTOs (matching backend)
// ============================================================

export type StaySourceType = "ThirdParty" | "HotelOwner";
export type StayBookingStatus = "Pending" | "Confirmed" | "Completed" | "Cancelled";
export type StaySortBy = "Recommended" | "Price" | "Rating" | "Reviews" | "Name" | "Newest";

export interface StayImageDto {
  id: number;
  link: string;
}

export interface StayBookingPlatformDto {
  id: number;
  name: string;
  priceWithTax?: number;
  link?: string;
}

export interface StayReviewDto {
  id: number;
  externalReviewId?: string;
  reviewerName?: string;
  rating?: number;
  reviewText?: string;
  platform: string;
  publishedAtDate?: string;
}

export interface StayResponseDto {
  id: number;
  sourceType: StaySourceType;
  createdByUserId?: string;
  hotelOwnerProfileId?: string;
  name: string;
  price?: number;
  description?: string;
  googleMapsLink?: string;
  reviews?: number;
  rating?: number;
  website?: string;
  phoneInternational?: string;
  locationSummaryDescription?: string;
  cid?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  latitude?: number;
  longitude?: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
  amenities: string[];
  images: StayImageDto[];
  reviewsPerRating: { rating: number; reviewsCount: number }[];
  bookingPlatforms: StayBookingPlatformDto[];
  featuredReviews: StayReviewDto[];
}

export interface CreateStayRequest {
  name: string;
  price: number;
  description?: string;
  googleMapsLink?: string;
  website?: string;
  phoneInternational?: string;
  locationSummaryDescription?: string;
  latitude?: number;
  longitude?: number;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  imageLinks: string[];
  amenities: string[];
  bookingPlatforms: Array<{
    name: string;
    priceWithTax?: number;
    link?: string;
  }>;
}

export type UpdateStayRequest = CreateStayRequest;

export interface GetStaysRequest {
  sourceType?: StaySourceType;
  search?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  sortBy?: StaySortBy;
  sortDirection?: SortDirection;
  page?: number;
  pageSize?: number;
}

export type StayRegionGroupBy = "Adm0" | "Adm1" | "Adm2" | "Adm3";

export interface StayRegionStatsRequest {
  groupBy: StayRegionGroupBy;
  sourceType?: StaySourceType;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
}

export interface StayRegionStatsDto {
  groupBy: StayRegionGroupBy;
  regionGid: number;
  hotelsCount: number;
  averagePrice?: number;
  pricePercentage?: number;
}

export interface StayImageUploadDto {
  link: string;
  fileName: string;
  sizeBytes: number;
}

export interface ImportStaysResult {
  created: number;
  updated: number;
  skipped: number;
}

export interface StayReviewsForLlmDto {
  stayId: number;
  stayName: string;
  reviewsCount: number;
  reviewsText: string;
}

export interface PagedResponse<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: T[];
}

export interface StayBookingResponseDto {
  id: string;
  stayId: number;
  stayName: string;
  travelerProfileId: string;
  guestName: string;
  checkInDate: string;
  checkOutDate: string;
  guestCount: number;
  totalPrice: number;
  status: StayBookingStatus;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface CreateStayBookingRequest {
  checkInDate: string;
  checkOutDate: string;
  guestCount: number;
}

export interface CreateStayReviewRequest {
  rating: number;
  reviewText: string;
}

// ============================================================
// Admin Module DTOs (matching backend)
// ============================================================

export interface AdminUserListItem {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  role: string;
}

export interface AdminUserResponse {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  createdAtUtc: string;
  roles: string[];
}

export interface AdminRoleResponse {
  name: string;
}

export interface AdminAssignRoleRequest {
  role: string;
}

export interface AdminChangeUserStatusRequest {
  isActive: boolean;
}

export interface AdminGetExperiencesRequest {
  moderationStatus?: ExperienceModerationStatus;
  isActive?: boolean;
}

export interface AdminModerateExperienceRequest {
  moderationStatus: ExperienceModerationStatus;
  moderationNotes?: string;
}

export interface AdminUpdateBuddyVerificationRequest {
  verificationStatus: string;
  moderationNotes?: string;
}

export interface AdminLocalBuddy {
  userId: string;
  profileId: string;
  displayName: string;
  city: string;
  languages?: string;
  profileImageUrl?: string;
  rating: number;
  reviewsCount: number;
  verificationStatus: "Pending" | "Approved" | "Rejected";
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface BuddyVerificationEvent {
  id: string;
  localBuddyUserId: string;
  actorUserId: string;
  previousStatus: "Pending" | "Approved" | "Rejected";
  newStatus: "Pending" | "Approved" | "Rejected";
  notes?: string;
  createdAtUtc: string;
}

export interface AdminDashboard {
  totalUsers: number;
  activeUsers: number;
  pendingBuddyVerifications: number;
  approvedBuddies: number;
  pendingExperiences: number;
  approvedExperiences: number;
  activeStays: number;
  totalBookings: number;
  totalBookingValue: number;
  auditEventsLast24Hours: number;
}

export interface AdminAnalytics {
  usersByRole: Record<string, number>;
  bookingsByStatus: Record<string, number>;
  listingsByType: Record<string, number>;
  stayBookingValue: number;
  experienceBookingValue: number;
}

export interface AdminAuditEvent {
  id: string;
  actorUserId: string;
  action: string;
  httpMethod: string;
  path: string;
  target?: string;
  statusCode: number;
  succeeded: boolean;
  correlationId: string;
  createdAtUtc: string;
  completedAtUtc?: string;
  changes?: unknown;
}

export interface AdminAuditPage {
  items: AdminAuditEvent[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// ============================================================
// Communication Module DTOs
// ============================================================

export interface ChatThreadDto {
  id: string;
  type: "Direct" | "Group";
  title?: string;
  participantUserIds: string[];
  participantDisplayNames: Record<string, string>;
  lastMessageBody?: string;
  lastMessageSenderUserId?: string;
  lastMessageAtUtc?: string;
  unreadCount: number;
  createdAtUtc: string;
}

export interface ChatMessageDto {
  id: string;
  threadId: string;
  senderUserId: string;
  body: string;
  sentAtUtc: string;
  isMine: boolean;
}

export interface ChatMessageEventDto {
  id: string;
  threadId: string;
  senderUserId: string;
  body: string;
  sentAtUtc: string;
}

export interface ChatThreadReadEventDto {
  threadId: string;
  userId: string;
  readAtUtc: string;
}

export interface NotificationDto {
  id: string;
  type: "System" | "ChatMessage" | "Booking" | "Moderation" | "Support";
  title: string;
  body: string;
  linkUrl?: string;
  sourceModule?: string;
  sourceEntityType?: string;
  sourceEntityId?: string;
  createdAtUtc: string;
  readAtUtc?: string;
  isRead: boolean;
}

export interface NotificationsPageDto {
  items: NotificationDto[];
  unreadCount: number;
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface NotificationPreferencesDto {
  inAppEnabled: boolean;
  emailEnabled: boolean;
  pushEnabled: boolean;
  chatMessageNotificationsEnabled: boolean;
  systemNotificationsEnabled: boolean;
  updatedAtUtc?: string;
}

export type UpdateNotificationPreferencesRequest = Omit<
  NotificationPreferencesDto,
  "updatedAtUtc"
>;

export interface HotelRecommendationCategoryPreference {
  category: string;
  weight?: number | null;
}

export interface HotelRecommendationRequest {
  budgetLevel?: number | null;
  experienceCategories: HotelRecommendationCategoryPreference[];
  requestedAmenities: string[];
  adm0Gid?: number | null;
  adm1Gid?: number | null;
  adm2Gid?: number | null;
  adm3Gid?: number | null;
  limit?: number | null;
  preferredLanguage?: string | null;
}

export interface NaturalLanguageHotelRecommendationRequest {
  text: string;
  limit?: number | null;
  adm0Gid?: number | null;
  adm1Gid?: number | null;
  adm2Gid?: number | null;
  adm3Gid?: number | null;
  preferredLanguage?: string | null;
}

export interface WeightedExperienceCategoryPreference {
  category: string;
  weight: number;
}

export interface HotelRecommendationPreferences {
  budgetLevel?: number | null;
  budgetLabel?: string | null;
  experienceCategories: WeightedExperienceCategoryPreference[];
  requestedAmenities: string[];
  adm0Gid?: number | null;
  adm1Gid?: number | null;
  adm2Gid?: number | null;
  adm3Gid?: number | null;
  limit: number;
  preferredLanguage: string;
  classificationConfidence?: number | null;
  notes?: string | null;
  regionName?: string | null;
  resolvedRegionName?: string | null;
}

export interface HotelRecommendationRegionResponse {
  countryNameEn?: string | null;
  countryNameAr?: string | null;
  governorateNameEn?: string | null;
  governorateNameAr?: string | null;
  districtNameEn?: string | null;
  districtNameAr?: string | null;
  neighbourhoodNameEn?: string | null;
  neighbourhoodNameAr?: string | null;
  displayName?: string | null;
}

export interface HotelRecommendationScoreBreakdown {
  interestProximityScore?: number | null;
  budgetMatchScore?: number | null;
  hotelQualityScore?: number | null;
  amenityMatchScore?: number | null;
}

export interface NearbyExperienceSummaryResponse {
  experienceId: number;
  name: string;
  category: string;
  distanceKm: number;
  rating?: number | null;
  reviews?: number | null;
}

export interface HotelRecommendationExplanationResponse {
  shortExplanation: string;
  reasons: string[];
  bestFor: string[];
  isAiGenerated: boolean;
}

export interface HotelRecommendationItemResponse {
  ranking: number;
  hotelId: number;
  name: string;
  price?: number | null;
  description?: string | null;
  locationSummaryDescription?: string | null;
  budgetLevel?: number | null;
  budgetLabel?: string | null;
  rating?: number | null;
  reviews?: number | null;
  latitude?: number | null;
  longitude?: number | null;
  adm0Gid?: number | null;
  adm1Gid?: number | null;
  adm2Gid?: number | null;
  adm3Gid?: number | null;
  region?: HotelRecommendationRegionResponse | null;
  googleMapsLink?: string | null;
  website?: string | null;
  phoneInternational?: string | null;
  finalScore: number;
  scores: HotelRecommendationScoreBreakdown;
  amenities: string[];
  matchedAmenities: string[];
  nearbyExperiences: NearbyExperienceSummaryResponse[];
  explanation: HotelRecommendationExplanationResponse;
}

export interface HotelRecommendationResponse {
  preferences: HotelRecommendationPreferences;
  totalCandidates: number;
  totalMatchingCandidates: number;
  evaluatedCandidates: number;
  returnedCount: number;
  items: HotelRecommendationItemResponse[];
}

export interface NaturalLanguageHotelRecommendationResponse extends HotelRecommendationResponse {
  inputText: string;
  classificationNotes?: string | null;
}
