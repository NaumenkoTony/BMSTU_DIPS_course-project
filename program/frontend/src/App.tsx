import { Container, Loader } from "@mantine/core";
import { Routes, Route } from "react-router-dom";
import { useEffect, useState } from "react";
import { jwtDecode } from "jwt-decode";

import NavBar from "./components/NavBar";
import HotelsPage from "./pages/HotelsPage";
import ReservationsPage from "./pages/ReservationsPage";
import ProfilePage from "./pages/ProfilePage";
import LoyaltyPage from "./pages/LoyaltyPage";
import LoginPage from "./pages/LoginPage";
import { CallbackPage } from "./pages/CallBackPage";

interface DecodedToken {
  exp: number;
  [key: string]: any;
}

export function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isAdmin, setIsAdmin] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  const handleLogin = (token: string) => {
    localStorage.setItem("access_token", token);
    setIsAuthenticated(true);

    try {
      const decoded = jwtDecode<DecodedToken>(token);
      const role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
      if (role && (role === "Admin" || (Array.isArray(role) && role.includes("Admin")))) {
        setIsAdmin(true);
      }
    } catch (error) {
      console.error("Token decoding error:", error);
    }
  };

  useEffect(() => {
    const token = localStorage.getItem("access_token");
    if (token) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        const now = Date.now() / 1000;
        if (decoded.exp && decoded.exp > now) {
          setIsAuthenticated(true);
          const role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
          if (role && (role === "Admin" || (Array.isArray(role) && role.includes("Admin")))) {
            setIsAdmin(true);
          }
        } else {
          localStorage.removeItem("access_token");
        }
      } catch {
        localStorage.removeItem("access_token");
      }
    }
    setIsLoading(false);
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("access_token");
    localStorage.removeItem("token_type");
    localStorage.removeItem("expires_in");
    setIsAuthenticated(false);
    setIsAdmin(false);
  };

  if (isLoading) {
    return (
      <Container style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <Loader size="xl" />
      </Container>
    );
  }

  return (
    <>
      <NavBar isAuthenticated={isAuthenticated} isAdmin={isAdmin} onLogout={handleLogout} />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/callback" element={<CallbackPage onLogin={handleLogin} />} />
        <Route path="/" element={isAuthenticated ? <HotelsPage /> : <LoginPage />} />
        <Route path="/reservations" element={isAuthenticated ? <ReservationsPage /> : <LoginPage />} />
        <Route path="/profile" element={isAuthenticated ? <ProfilePage /> : <LoginPage />} />
        <Route path="/loyalty" element={isAuthenticated ? <LoyaltyPage /> : <LoginPage />} />
      </Routes>
    </>
  );
}
