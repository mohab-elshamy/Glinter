export type HeatmapLayer = "safety" | "price" | "comfort";

export interface Neighborhood {
  name: string;
  x: number;
  y: number;
  lat: number;
  lng: number;
  safety: number;
  price: number;
  comfort: number;
}

export interface Hotel {
  name: string;
  area: string;
  rating: number;
  reviews: number;
  price: number;
  comfort: number;
  safety: number;
  amenities: string[];
}

export interface Recommendation {
  title: string;
  tags: string[];
}
