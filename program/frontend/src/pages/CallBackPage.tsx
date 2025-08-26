import { useEffect, useState, useRef } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { Container, Title, Loader, Text, Paper, Alert, Button } from "@mantine/core";
import { IconAlertCircle } from "@tabler/icons-react";
import "./CallbackPage.css";

const TOKEN_URL = import.meta.env.VITE_TOKEN_URL || "http://localhost:8000/token";
const CLIENT_ID = import.meta.env.VITE_CLIENT_ID || "locus-frontend-client";

interface CallbackPageProps {
  onLogin: (token: string) => void;
}

export function CallbackPage({ onLogin }: CallbackPageProps) {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [error, setError] = useState<string | null>(null);
  const isMountedRef = useRef(true);
  const hasExecutedRef = useRef(false);

  useEffect(() => {
    const exchangeCodeForToken = async () => {
      if (hasExecutedRef.current) {
        console.log("Already executing, skipping...");
        return;
      }
      
      hasExecutedRef.current = true;
      console.log("Starting token exchange...");

      try {
        console.log("Callback received. Search params:", Object.fromEntries(searchParams.entries()));
        
        const code = searchParams.get("code");
        const state = searchParams.get("state");
        const error = searchParams.get("error");

        if (error) {
          throw new Error(`Authorization error: ${error}`);
        }

        if (!code || !state) {
          throw new Error("Missing required parameters: code and state");
        }

        const savedState = sessionStorage.getItem("auth_state");
        console.log("Saved state:", savedState, "Received state:", state);
        
        if (state !== savedState) {
          throw new Error("Invalid state parameter");
        }

        const codeVerifier = sessionStorage.getItem("pkce_verifier");
        console.log("Code verifier from session:", codeVerifier);
        
        if (!codeVerifier) {
          throw new Error("Session expired. Please login again.");
        }

        console.log("Exchanging code for token with PKCE...");
        
        const formData = new URLSearchParams();
        formData.append("grant_type", "authorization_code");
        formData.append("code", code);
        formData.append("redirect_uri", window.location.origin + "/callback");
        formData.append("client_id", CLIENT_ID);
        formData.append("code_verifier", codeVerifier);

        console.log("Token request data:", Object.fromEntries(formData.entries()));
        
        const tokenResponse = await fetch(TOKEN_URL, {
          method: "POST",
          headers: {
            "Content-Type": "application/x-www-form-urlencoded",
          },
          body: formData,
        }).catch(fetchError => {
          console.error("Fetch error:", fetchError);
          throw new Error(`Network error: ${fetchError.message}`);
        });

        if (!isMountedRef.current) {
          console.log('Component unmounted, skipping state updates');
          return;
        }

        console.log("Token response status:", tokenResponse.status);
        
        if (!tokenResponse.ok) {
          const errorData = await tokenResponse.text();
          console.error("Token request failed:", tokenResponse.status, errorData);
          throw new Error(`Token request failed: ${tokenResponse.status} ${errorData}`);
        }

        const tokenData = await tokenResponse.json();
        console.log("Token data received:", tokenData);

        localStorage.setItem("access_token", tokenData.access_token);
        localStorage.setItem("token_type", tokenData.token_type);
        localStorage.setItem("expires_in", tokenData.expires_in.toString());

        onLogin(tokenData.access_token);
        
        setStatus("success");
        console.log("Login successful, redirecting to home...");

        setTimeout(() => {
          sessionStorage.removeItem("pkce_verifier");
          sessionStorage.removeItem("auth_state");
          navigate("/");
        }, 2000);

      } catch (err) {
        console.error("Callback error:", err);
        setError(err instanceof Error ? err.message : "Unknown error");
        setStatus("error");

        sessionStorage.removeItem("pkce_verifier");
        sessionStorage.removeItem("auth_state");
      } finally {
        isMountedRef.current = false;
      }
    };

    exchangeCodeForToken();
  }, [searchParams, navigate, onLogin]);

  if (status === "loading") {
    return (
      <Container className="callback-container">
        <Paper shadow="md" p="xl" radius="lg" withBorder className="callback-card">
          <Loader size="xl" variant="dots" className="callback-loader" />
          <Title order={4} mt="md">
            Обмен кода на токен...
          </Title>
          <Text size="sm" mt="sm">
            Пожалуйста, подождите
          </Text>
        </Paper>
      </Container>
    );
  }

  if (status === "error") {
    return (
      <Container className="callback-container">
        <Paper shadow="md" p="xl" radius="lg" withBorder className="callback-card">
          <Alert icon={<IconAlertCircle size="1rem" />} title="Ошибка авторизации" color="red" mb="md">
            {error}
          </Alert>
          <Button fullWidth onClick={() => navigate("/login")}>
            Вернуться к входу
          </Button>
        </Paper>
      </Container>
    );
  }

  return (
    <Container className="callback-container">
      <Paper shadow="md" p="xl" radius="lg" withBorder className="callback-card">
        <Title order={3} mb="md">
          Вход выполнен успешно!
        </Title>
        <Text>Перенаправление на главную страницу...</Text>
        <Loader size="sm" variant="dots" className="callback-loader" />
      </Paper>
    </Container>
  );
}