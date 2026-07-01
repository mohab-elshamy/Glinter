import type {
  StayReviewDto,
  StaySourceType,
} from "@/shared/types/api";

export interface Hotel {
  id?: number;
  name: string;
  area: string;
  rating: number;
  reviews: number;
  price?: number;
  amenities: string[];
  image?: string;
  images: Array<{ id: number; link: string }>;
  description?: string;
  bookingPlatforms: Array<{
    id?: number;
    name: string;
    priceWithTax?: number;
    link?: string;
  }>;
  reviewsPerRating: Array<{ rating: number; reviewsCount: number }>;
  featuredReviews: StayReviewDto[];
  sourceType: StaySourceType;
  website?: string;
  phoneInternational?: string;
  googleMapsLink?: string;
  cid?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
  regionNames?: string[];
  latitude?: number;
  longitude?: number;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
}
