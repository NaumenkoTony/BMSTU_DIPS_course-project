export interface StatisticsSummary {
  totalActions: number;
  activeUsers: number;
  actionsByType: { [key: string]: number };
  actionsByDay: { [key: string]: number };
  topUsers: { username: string; count: number }[];
}

export interface UserAction {
  id: number;
  userId: string;
  username: string;
  service: string;
  action: string;
  status: string;
  timestamp: string;
  metadataJson?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

const API_URL = window.appConfig?.API_URL || "http://localhost:8080/api/v1";

async function fetchWithAuth(path: string) {
  const token = localStorage.getItem("access_token");
  const response = await fetch(`${API_URL}${path}`, {
    headers: { Authorization: `Bearer ${token ?? ""}` },
  });
  if (!response.ok) throw new Error(`Failed request: ${response.statusText}`);
  return await response.json();
}

export function fetchStatisticsSummary(): Promise<StatisticsSummary> {
  return fetchWithAuth("/api/v1/summary");
}

export function fetchRecentActions(
  page = 1,
  pageSize = 50,
  username?: string
): Promise<PagedResult<UserAction>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (username) params.append("username", username);

  return fetchWithAuth(`/api/v1/recent?${params.toString()}`);
}
