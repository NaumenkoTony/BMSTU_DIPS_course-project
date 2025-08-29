export interface PaymentInfo {
  status: string;
  price: number;
}

export interface CreateReservationResponse {
  reservationUid: string;
  hotelUid: string;
  startDate: string;
  endDate: string;
  discount: number;
  status: string;
  payment: PaymentInfo;
}

export interface HotelInfo {
  id: string;
  hotelUid: string;
  name: string;
  fullAddress: string;
  stars?: number;
}

export interface PaymentInfo {
  status: string;
  price: number;
}

export interface AggregatedReservationResponse {
  reservationUid: string;
  hotel: {
    hotelUid: string;
    name: string;
    fullAddress: string;
    stars?: number;
  };
  startDate: string;
  endDate: string;
  status: string;
  payment: PaymentInfo;
}


const API_URL = import.meta.env.VITE_API_URL || "http://localhost:8080/api/v1";

export async function bookHotel(req: {
  hotelUid: string;
  startDate: string;
  endDate: string;
}): Promise<CreateReservationResponse> {
  const token = localStorage.getItem("access_token") ?? "";

  const res = await fetch(`${API_URL}/reservations`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(req),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Booking failed: ${res.status} ${text}`);
  }

  return (await res.json()) as CreateReservationResponse;
}

export async function getReservations(): Promise<AggregatedReservationResponse[]> {
  const res = await fetch(`${API_URL}/reservations`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to load reservations: ${res.status}`);
  }

  return await res.json();
}

export async function getReservation(reservationUid: string): Promise<AggregatedReservationResponse> {
  const res = await fetch(`${API_URL}/reservations/${reservationUid}`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to load reservation: ${res.status}`);
  }

  return await res.json();
}

export async function unbookHotel(reservationUid: string): Promise<void> {
  const res = await fetch(`${API_URL}/reservations/${reservationUid}`, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to unbook hotel: ${res.status}`);
  }
}