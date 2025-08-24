import { Group, Button, Box } from "@mantine/core";
import { Link } from "react-router-dom";
import "./NavBar.css";

interface NavBarProps {
  isAuthenticated: boolean;
  isAdmin: boolean;
  onLogout: () => void;
}

export default function NavBar({ isAuthenticated, isAdmin, onLogout }: NavBarProps) {
  return (
    <Box className="navbar">
      <div className="navbar-content">
        <Button variant="subtle" component={Link} to="/" className="nav-btn logo">
          Локус
        </Button>
        
        {isAuthenticated && (
          <Group 
            gap="xs"
            className="nav-links"
            align="center"
            >
            <Button variant="subtle" component={Link} to="/" className="nav-btn">
              Отели
            </Button>
            <Button variant="subtle" component={Link} to="/reservations" className="nav-btn">
              Бронирования
            </Button>
            <Button variant="subtle" component={Link} to="/profile" className="nav-btn">
              Профиль
            </Button>
            <Button variant="subtle" component={Link} to="/loyalty" className="nav-btn">
              Лояльность
            </Button>
            
            {isAdmin && (
              <Group gap="xs">
                <Button variant="subtle" component={Link} to="/reports" className="nav-btn">
                  Отчёты
                </Button>
                <Button variant="subtle" component={Link} to="/admin/create-user" className="nav-btn">
                  Создать пользователя
                </Button>
              </Group>
            )}
            
            <Button color="red" onClick={onLogout} className="nav-btn">
              Выйти
            </Button>
          </Group>
        )}
      </div>
    </Box>
  );
}