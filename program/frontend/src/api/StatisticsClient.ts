export interface StatisticsSummary {
  totalActions: number;
  uniqueUsers: number;
  actionsByType: Record<string, number>;
  actionsByDay: Record<string, number>;
}

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:8080";

export async function fetchStatisticsSummary(): Promise<StatisticsSummary> {
  const token = localStorage.getItem("access_token");
  const response = await fetch(`${API_URL}/statistics/summary`, {
    headers: {
      "Authorization": `Bearer ${token ?? ""}`
    }
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch statistics: ${response.statusText}`);
  }
  return await response.json();
}
