export interface CreateUserRequest {
  UserName: string;
  Email: string;
  FirstName: string;
  LastName: string;
  Password: string;
  Roles: string[];
}

export interface CreateUserResponse {
  message: string;
  userId: string;
}

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:8080/api/v1";

export async function createUser(userData: CreateUserRequest): Promise<CreateUserResponse> {
  const token = localStorage.getItem("access_token");
  const url = `${API_URL}/create-user`;
  
  console.log('Creating user with URL:', url);
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token ?? ""}`,
    },
    body: JSON.stringify(userData),
  });

  console.log('Response status:', response.status);

  const responseText = await response.text();
  console.log('Response text:', responseText);

  try {
    const data = JSON.parse(responseText);
    if (!response.ok) {
      throw new Error(data.error || data.message || `HTTP error ${response.status}`);
    }
    return data;
  } catch (e) {
    throw new Error(`Failed to parse JSON: ${responseText.substring(0, 100)}`);
  }
}

export async function getAvailableRoles(): Promise<string[]> {
  const token = localStorage.getItem("access_token");
  
  const response = await fetch(`${API_URL}/users/roles`, {
    headers: {
      'Authorization': `Bearer ${token ?? ""}`,
    },
  });

  if (!response.ok) {
    throw new Error(`Failed to load roles: ${response.status}`);
  }

  return await response.json();
}