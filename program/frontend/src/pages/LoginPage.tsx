import { useState } from "react";
import { Container, Title, Button, Text, Paper } from "@mantine/core";
import "./LoginPage.css";

const AUTH_URL = window.appConfig?.IDP_API_URL || "http://localhost:8000";
const CLIENT_ID = window.appConfig?.CLIENT_ID || "locus-frontend-client";
const REDIRECT_URI = window.appConfig?.REDIRECT_URI || "http://localhost:5173/callback";
const SCOPES = "openid profile email";

const generateRandomString = (length: number = 64): string => {
  const charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
  const randomValues = new Uint8Array(length);
  crypto.getRandomValues(randomValues);
  return Array.from(randomValues).map((value) => charset[value % charset.length]).join("");
};

const base64UrlEncode = (arrayBuffer: ArrayBuffer): string => {
  const bytes = new Uint8Array(arrayBuffer);
  return btoa(String.fromCharCode(...bytes))
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");
};

const pkceChallengeFromVerifier = async (verifier: string): Promise<string> => {
  const encoder = new TextEncoder();
  const data = encoder.encode(verifier);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return base64UrlEncode(digest);
};

export function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleLogin = async () => {
    setIsLoading(true);
    setError(null);

    try {     
      const state = crypto.randomUUID();
      const codeVerifier = generateRandomString();
      const codeChallenge = await pkceChallengeFromVerifier(codeVerifier);

      sessionStorage.setItem("pkce_verifier", codeVerifier);
      sessionStorage.setItem("auth_state", state);

      const authUrl = new URL(AUTH_URL + "/authorize");
      authUrl.searchParams.append("response_type", "code");
      authUrl.searchParams.append("client_id", CLIENT_ID);
      authUrl.searchParams.append("redirect_uri", REDIRECT_URI);
      authUrl.searchParams.append("scope", SCOPES);
      authUrl.searchParams.append("state", state);
      authUrl.searchParams.append("code_challenge", codeChallenge);
      authUrl.searchParams.append("code_challenge_method", "S256");

      console.log("Initiating authorization flow");
      window.location.href = authUrl.toString();
    } catch (err) {
      console.error("Authorization error:", err);
      setError("Ошибка входа. Попробуйте еще раз.");
      setIsLoading(false);
    }
  };

  return (
    <Container className="login-container">
      <Paper shadow="md" p="xl" radius="lg" withBorder className="login-card">
        <Title order={2} mb="lg" className="login-title">
          Добро пожаловать
        </Title>

        {error && (
          <Text color="red" size="sm" mb="md">
            {error}
          </Text>
        )}

        <Button
          fullWidth
          size="lg"
          onClick={handleLogin}
          loading={isLoading}
          loaderProps={{ type: "dots" }}
          className="login-button"
        >
          {isLoading ? "Перенаправление..." : "Войти в систему"}
        </Button>

        <Text size="sm" mt="md" className="login-hint">
          Вы будете перенаправлены на сервер авторизации
        </Text>
      </Paper>
    </Container>
  );
}

export default LoginPage;
