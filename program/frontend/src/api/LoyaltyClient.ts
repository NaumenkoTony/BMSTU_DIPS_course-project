export interface LoyaltyInfoResponse {
  status: string;
  discount: number;
  reservationCount: number;
}

const API_URL = window.appConfig?.API_URL || "http://localhost:8080/api/v1";

export async function getLoyalty(): Promise<LoyaltyInfoResponse> {
  const res = await fetch(`${API_URL}/loyalty`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to load loyalty info: ${res.status}`);
  }

  return await res.json();
}
