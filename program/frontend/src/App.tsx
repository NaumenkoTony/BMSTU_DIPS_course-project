import { ThemeProvider, CssBaseline, Container, createTheme } from '@mui/material';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import NavBar from './components/NavBar';
import HotelsPage from './pages/HotelsPage';
import ReservationsPage from './pages/ReservationsPage';
import ProfilePage from './pages/ProfilePage';
import LoginPage from './pages/LoginPage';
import LoyaltyPage from './pages/LoyaltyPage';

const mainTheme = createTheme({
  palette: {
    primary: {
      main: '#1a237e',
      light: '#534bae',
      dark: '#000051',
    },
    secondary: {
      main: '#ffd700',
      light: '#ffff54',
      dark: '#c8a600',
    },
    background: {
      default: '#f5f5f5',
      paper: '#ffffff',
    },
  },
  typography: {
    fontFamily: '"Playfair Display", "Georgia", serif',
  },
});

function App() {
  return (
    <ThemeProvider theme={mainTheme}>
      <CssBaseline />
      <BrowserRouter>
        <NavBar />
        <Container sx={{ mt: 4 }}>
          <Routes>
            <Route path="/" element={<HotelsPage />} />
            <Route path="/reservations" element={<ReservationsPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/loyalty" element={<LoyaltyPage />} />
            <Route path="/login" element={<LoginPage />} />
          </Routes>
        </Container>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
