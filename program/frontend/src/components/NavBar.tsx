import AppBar from '@mui/material/AppBar';
import Toolbar from '@mui/material/Toolbar';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import { Link as RouterLink } from 'react-router-dom';
import Box from '@mui/material/Box';

export default function NavBar() {
  return (
    <AppBar position="static">
      <Toolbar>
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Локус 
        </Typography>
        <Box>
          <Button color="inherit" component={RouterLink} to="/">Отели</Button>
          <Button color="inherit" component={RouterLink} to="/reservations">Бронирования</Button>
          <Button color="inherit" component={RouterLink} to="/profile">Профиль</Button>
        </Box>
      </Toolbar>
    </AppBar>
  );
}
