import React from 'react';
import ReactDOM from 'react-dom/client';
import { MantineProvider, createTheme } from '@mantine/core';
import { BrowserRouter } from 'react-router-dom';
import { App } from './App';

import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';

const theme = createTheme({
  primaryColor: 'indigo',
  defaultRadius: '0',
  fontFamily: 'InterVariable, Inter, system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif',
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <MantineProvider theme={theme} defaultColorScheme="light">
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </MantineProvider>
  </React.StrictMode>
);