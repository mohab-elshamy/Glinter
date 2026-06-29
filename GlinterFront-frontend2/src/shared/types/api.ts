export interface AuthResponse {
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
  token: string;
}

export interface CurrentUser {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
}

export interface RegisterData {
  fullName: string;
  email: string;
  password: string;
  role?: string;
}

export interface UserListItem {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
}

export interface Role {
  name: string;
  id: string;
}

export interface Neighborhood {
  id: string;
  name: string;
  city: string;
  lat: number;
  lng: number;
  safety_score: number;
  price_level: number;
  comfort_score: number;
  ai_insights?: string;
}

export interface NeighborhoodsResponse {
  neighborhoods: Neighborhood[];
}

export interface Hotel {
  id: string;
  name: string;
  neighborhood: string;
  price: number;
  rating: number;
  reviews_count: number;
  images: string[];
  amenities: string[];
}

export interface HotelsResponse {
  hotels: Hotel[];
}

export interface Experience {
  id: string;
  name: string;
  city: string;
  description: string;
  price: number;
  duration_hours: number;
  images: string[];
}

export interface ExperiencesResponse {
  experiences: Experience[];
}

export interface ItineraryGenerateData {
  city: string;
  start_date: string;
  end_date: string;
  vibes: string[];
  budget: string;
  preferences?: {
    start_time?: string;
    end_time?: string;
    num_travelers?: number;
  };
}

export interface ItineraryStop {
  time: string;
  name: string;
  duration: string;
  type: string;
  cost: number;
  location?: {
    lat: number;
    lng: number;
  };
}

export interface ItineraryDay {
  day: string;
  theme: string;
  stops: ItineraryStop[];
  transport_to_next?: string;
}

export interface ItineraryResponse {
  itinerary: ItineraryDay[];
  transport_estimate: {
    total: number;
    breakdown: { day: string; uber: number; metro: number; walking: number }[];
  };
  total_cost: number;
  weather?: {
    condition: string;
    temp_min: number;
    temp_max: number;
    advice: string;
  };
}

export interface SaveItineraryData {
  city: string;
  start_date: string;
  end_date: string;
  vibes: string[];
  plan_json: ItineraryDay[];
}

export interface ItineraryListResponse {
  itineraries: ItineraryResponse[];
}

export interface Buddy {
  id: string;
  name: string;
  avatar: string;
  city: string;
  languages: string[];
  rating: number;
  reviews_count: number;
  is_verified: boolean;
  verified_at?: string;
  specialties: string[];
  price?: string;
}

export interface BuddiesResponse {
  buddies: Buddy[];
}

export interface Message {
  id: string;
  sender_id: string;
  receiver_id: string;
  content: string;
  is_read: boolean;
  created_at: string;
}

export interface ConversationsResponse {
  conversations: {
    user_id: string;
    user_name: string;
    last_message: string;
    unread_count: number;
  }[];
}

export interface Rating {
  id: string;
  traveler_id: string;
  buddy_id: string;
  score: number;
  review_text?: string;
  created_at: string;
}

export interface RatingsResponse {
  ratings: Rating[];
}

export interface Favorite {
  neighborhood_id?: string;
  hotel_id?: string;
  created_at: string;
}

export interface FavoritesResponse {
  favorites: Favorite[];
}

export interface SafetyReportData {
  description: string;
  location_lat?: number;
  location_lng?: number;
}

export interface SafetyReport {
  id: string;
  status: string;
  created_at: string;
}

export interface SafetyReportsResponse {
  reports: SafetyReport[];
}

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
  profileType: string;
  displayName: string;
  bio?: string;
  nationality?: string;
  preferredBudgetLevel?: string;
  travelStyle?: string;
  preferredInterests?: string;
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
}

export interface LocalBuddyProfileResponse {
  profileId: string;
  userId: string;
  profileType: string;
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
}

export interface TravelerProfileRequest {
  displayName: string;
  bio?: string;
  nationality?: string;
  preferredBudgetLevel?: string;
  travelStyle?: string;
  preferredInterests?: string;
  interestIds: string[];
}

// ============================================================
// Experiences Module DTOs (matching backend)
// ============================================================

export interface VibeResponseDto {
  id: string;
  name: string;
}

export interface ExperienceTagDto {
  id: string;
  experienceId: string;
  name: string;
}

export interface ExperienceResponseDto {
  id: string;
  providerProfileId: string;
  categoryId: string;
  categoryName?: string;
  areaId: string;
  title: string;
  description: string;
  locationName: string;
  pricePerPerson: number;
  currency: string;
  durationMinutes: number;
  maxGuests: number;
  latitude: number;
  longitude: number;
  isActive: boolean;
  approvalStatus: string;
  moderationNotes?: string;
  moderatedAtUtc?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
  vibes: VibeResponseDto[];
  tags: ExperienceTagDto[];
}

export interface ExperienceSummaryDto {
  id: string;
  providerProfileId: string;
  categoryId: string;
  categoryName?: string;
  areaId: string;
  title: string;
  locationName: string;
  pricePerPerson: number;
  currency: string;
  durationMinutes: number;
  maxGuests: number;
  isActive: boolean;
  approvalStatus: string;
  moderationNotes?: string;
  vibes: string[];
  tags: string[];
}

export interface ExperienceCategoryResponseDto {
  id: string;
  name: string;
  description?: string;
}

export interface ExperienceBookingResponseDto {
  id: string;
  experienceId: string;
  availabilityId: string;
  travelerProfileId: string;
  guestsCount: number;
  totalPrice: number;
  status: string;
  createdAtUtc: string;
  cancelledAtUtc?: string;
  completedAtUtc?: string;
}

export interface CreateExperienceBookingRequest {
  availabilityId: string;
  guestsCount: number;
}

export interface GetExperiencesRequest {
  areaId?: string;
  categoryId?: string;
  minPrice?: number;
  maxPrice?: number;
  guests?: number;
  vibeId?: string;
  tag?: string;
}

// ============================================================
// Stays Module DTOs (matching backend)
// ============================================================

export interface StayTagDto {
  id: string;
  stayId: string;
  name: string;
}

export interface StaySummaryDto {
  id: string;
  name: string;
  address: string;
  pricePerNight: number;
  currency: string;
  maxGuests: number;
  isActive: boolean;
  amenities: string[];
  images: string[];
}

export interface StayResponseDto {
  id: string;
  ownerProfileId: string;
  areaId: string;
  name: string;
  description: string;
  address: string;
  pricePerNight: number;
  currency: string;
  maxGuests: number;
  latitude: number;
  longitude: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
  tags: StayTagDto[];
  amenities: string[];
  images: string[];
}

export interface CreateStayRequest {
  areaId: string;
  name: string;
  description: string;
  address: string;
  pricePerNight: number;
  currency: string;
  maxGuests: number;
  latitude: number;
  longitude: number;
  tags: string[];
  amenities?: string[];
  images?: string[];
}

export interface UpdateStayRequest {
  name: string;
  description?: string;
  address?: string;
  pricePerNight: number;
  currency: string;
  maxGuests: number;
  latitude: number;
  longitude: number;
  tags?: string[];
  amenities?: string[];
  images?: string[];
}

export interface GetStaysRequest {
  areaId?: string;
  minPrice?: number;
  maxPrice?: number;
  guests?: number;
  tag?: string;
}

export interface StayBookingResponseDto {
  id: string;
  stayId: string;
  travelerProfileId: string;
  checkInDate: string;
  checkOutDate: string;
  guestCount: number;
  totalPrice: number;
  status: string;
  createdAtUtc: string;
}

export interface CreateStayBookingRequest {
  checkInDate: string;
  checkOutDate: string;
  guestCount: number;
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
  approvalStatus?: string;
  isActive?: boolean;
}

export interface AdminSetApprovalStatusRequest {
  approvalStatus: string;
  moderationNotes?: string;
}

export interface AdminUpdateBuddyVerificationRequest {
  verificationStatus: string;
}
