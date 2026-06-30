export interface Hotel {
  id?: number;
  name: string;
  area: string;
  rating: number;
  reviews: number;
  price: number;
  amenities: string[];
  image?: string;
  description?: string;
  latitude?: number;
  longitude?: number;
}
