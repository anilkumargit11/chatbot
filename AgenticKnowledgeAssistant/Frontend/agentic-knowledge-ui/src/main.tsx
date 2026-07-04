import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { ToastContainer } from 'react-toastify';
import App from './App';
import { AuthProvider } from './contexts/AuthContext';
import { ThemeModeProvider } from './contexts/ThemeModeContext';
import 'react-toastify/dist/ReactToastify.css';
import 'highlight.js/styles/github-dark.css';
import './index.css';

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <BrowserRouter>
      <ThemeModeProvider>
        <AuthProvider>
          <App />
        </AuthProvider>
        <ToastContainer position="top-right" newestOnTop closeOnClick theme="colored" />
      </ThemeModeProvider>
    </BrowserRouter>
  </React.StrictMode>
);
