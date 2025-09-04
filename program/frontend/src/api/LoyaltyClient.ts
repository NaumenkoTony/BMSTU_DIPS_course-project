export interface LoyaltyInfoResponse {
  status: string;
  discount: number;
  reservationCount: number;
}

const API_URL = import.meta.env.API_URL || window.appConfig?.API_URL || "http://localhost:8080";

export async function getLoyalty(): Promise<LoyaltyInfoResponse> {
  const res = await fetch(`${API_URL}/api/v1/loyalty`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to load loyalty info: ${res.status}`);
  }

  return await res.json();
}
