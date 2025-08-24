import { Container } from "@mantine/core";
import { Routes, Route } from "react-router-dom";
import { useEffect, useState } from "react";
import { jwtDecode } from "jwt-decode";

import NavBar from "./components/NavBar";
import HotelsPage from "./pages/HotelsPage";
import ReservationsPage from "./pages/ReservationsPage";
import ProfilePage from "./pages/ProfilePage";
import LoyaltyPage from "./pages/LoyaltyPage";
import LoginPage from "./pages/LoginPage";

interface DecodedToken {
  exp: number;
  [key: string]: any;
}

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isAdmin, setIsAdmin] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (token) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        const now = Date.now() / 1000;

        if (decoded.exp && decoded.exp > now) {
          setIsAuthenticated(true);
          const role =
            decoded[
              "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            ];
          if (role && (role === "Admin" || (Array.isArray(role) && role.includes("Admin")))) {
            setIsAdmin(true);
          }
        } else {
          localStorage.removeItem("token");
        }
      } catch {
        localStorage.removeItem("token");
      }
    }
  }, []);

  const handleLogout = () => {
    setIsAuthenticated(false);
    setIsAdmin(false);
    localStorage.removeItem("token");
  };

  if (!isAuthenticated) {
    return <LoginPage />;
  }

  return (
    <>
      <NavBar
        isAuthenticated={isAuthenticated}
        isAdmin={isAdmin}
        onLogout={handleLogout}
      />
      <Container mt="lg">
        <Routes>
          <Route path="/" element={<HotelsPage />} />
          <Route path="/reservations" element={<ReservationsPage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/loyalty" element={<LoyaltyPage />} />
        </Routes>
      </Container>
    </>
  );
}

export default App;
