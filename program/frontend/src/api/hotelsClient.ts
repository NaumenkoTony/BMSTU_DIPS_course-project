export interface HotelResponse {
  id: string;
  hotelUid: string;
  name: string;
  country: string;
  city: string;
  address: string;
  stars?: number;
  price: number;
}

export interface PaginationResponse<T> {
  page: number;
  pageSize: number;
  totalElements: number;
  items: T[];
}

export interface HotelsPaginationResponse {
  items: HotelResponse[];
  totalElements: number;
  totalPages: number;
}

const API_URL = import.meta.env.API_URL || window.appConfig?.API_URL || "http://localhost:8080";

export async function getHotels(page: number = 1, pageSize: number = 10): Promise<PaginationResponse<HotelResponse>> {
  const res = await fetch(`${API_URL}/api/v1/hotels?page=${page}&size=${pageSize}`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to load hotels: ${res.status}`);
  }

  return await res.json();
}