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
            
            {isAdmin && (
              <Group gap="xs">
                <Button variant="subtle" component={Link} to="/admin/statistics" className="nav-btn">
                  Статистика
                </Button>
                <Button variant="subtle" component={Link} to="/admin/create-user" className="nav-btn">
                  Создать пользователя
                </Button>
              </Group>
            )}

            <Button variant="subtle" component={Link} to="/profile" className="nav-btn">
              Профиль
            </Button>
            
            <Button color="red" onClick={onLogout} className="nav-btn">
              Выйти
            </Button>
          </Group>
        )}
      </div>
    </Box>
  );
}