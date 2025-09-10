import { useEffect, useState, useRef } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { Container, Title, Loader, Text, Paper, Alert, Button } from "@mantine/core";
import { IconAlertCircle, IconCheck } from "@tabler/icons-react";
import "./CallBackPage.css";

const AUTH_URL = window.appConfig?.IDP_API_URL || "http://localhost:8000/idp";
const CLIENT_ID = window.appConfig?.CLIENT_ID || "locus-frontend-client";
const REDIRECT_URI = window.appConfig?.REDIRECT_URI || "http://localhost:80/callback";

interface CallbackPageProps {
  onLogin: (token: string) => void;
}

export function CallbackPage({ onLogin }: CallbackPageProps) {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [error, setError] = useState<string | null>(null);
  const hasExecutedRef = useRef(false);

  useEffect(() => {
    if (hasExecutedRef.current) {
      return;
    }
    
    hasExecutedRef.current = true;
    
    const exchangeCodeForToken = async () => {
      try {
        const code = searchParams.get("code");
        const state = searchParams.get("state");
        const error = searchParams.get("error");

        if (error) {
          throw new Error(`Authorization error: ${error}`);
        }

        if (!code || !state) {
          throw new Error("Missing required parameters");
        }

        const savedState = sessionStorage.getItem("auth_state");
        if (state !== savedState) {
          throw new Error("Invalid state parameter");
        }

        const codeVerifier = sessionStorage.getItem("pkce_verifier");
        if (!codeVerifier) {
          throw new Error("Session expired");
        }

        const formData = new URLSearchParams();
        formData.append("grant_type", "authorization_code");
        formData.append("code", code);
        formData.append("redirect_uri", REDIRECT_URI);
        formData.append("client_id", CLIENT_ID);
        formData.append("code_verifier", codeVerifier);

        const headers: Record<string, string> = {
          "Content-Type": "application/x-www-form-urlencoded"
        };
        const tokenResponse = await fetch(AUTH_URL + '/token', {
          method: "POST",
          headers: headers,
          body: formData,
        });

        if (!tokenResponse.ok) {
          throw new Error(`Token request failed: ${tokenResponse.status}`);
        }

        const tokenData = await tokenResponse.json();
        localStorage.setItem("access_token", tokenData.access_token);
        localStorage.setItem("token_type", tokenData.token_type);
        localStorage.setItem("expires_in", tokenData.expires_in.toString());
        onLogin(tokenData.access_token);
        setStatus("success");

        setTimeout(() => {
          sessionStorage.removeItem("pkce_verifier");
          sessionStorage.removeItem("auth_state");
          navigate("/");
        }, 1500);

      } catch (err) {
        console.error("Callback error:", err);
        setError(err instanceof Error ? err.message : "Unknown error");
        setStatus("error");
        sessionStorage.removeItem("pkce_verifier");
        sessionStorage.removeItem("auth_state");
      }
    };

    exchangeCodeForToken();
  }, [searchParams, navigate, onLogin]);

  if (status === "loading") {
    return (
      <Container className="callback-container">
        <Paper shadow="md" p="xl" radius="lg" withBorder className="callback-card">
          <Loader size="xl" variant="dots" className="callback-loader" />
          <Title order={4} mt="md" className="callback-title">
            Обмен кода на токен...
          </Title>
          <Text size="sm" mt="sm" className="callback-text">
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
          <Alert 
            icon={<IconAlertCircle size="1rem" />} 
            title="Ошибка авторизации" 
            color="red" 
            mb="md"
            className="callback-alert"
          >
            {error}
          </Alert>
          <Button 
            fullWidth 
            onClick={() => navigate("/login")}
            className="callback-button"
            styles={{
              root: {
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                border: 'none',
                borderRadius: '8px',
                fontWeight: '500',
              }
            }}
          >
            Вернуться к входу
          </Button>
        </Paper>
      </Container>
    );
  }

  return (
    <Container className="callback-container">
      <Paper shadow="md" p="xl" radius="lg" withBorder className="callback-card">
        <IconCheck size={48} color="#28a745" className="callback-success-icon" />
        <Title order={3} mb="md" className="callback-title">
          Вход выполнен успешно!
        </Title>
        <Text className="callback-text">Перенаправление на главную страницу...</Text>
        <Loader size="sm" variant="dots" className="callback-loader" />
      </Paper>
    </Container>
  );
}