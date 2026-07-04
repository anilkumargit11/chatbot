import { createTheme, alpha } from '@mui/material/styles';

export const createAppTheme = (mode: 'light' | 'dark') =>
  createTheme({
    palette: {
      mode,
      primary: {
        main: mode === 'dark' ? '#6366f1' : '#4f46e5', // Indigo accent
        dark: mode === 'dark' ? '#4f46e5' : '#3730a3',
        light: mode === 'dark' ? '#818cf8' : '#6366f1'
      },
      secondary: {
        main: '#0ea5e9', // Sky blue for Copilot styling
        light: '#38bdf8',
        dark: '#0284c7'
      },
      background: {
        default: mode === 'dark' ? '#070a13' : '#f8fafc',
        paper: mode === 'dark' ? '#0f1422' : '#ffffff'
      },
      divider: mode === 'dark' ? 'rgba(99, 102, 241, 0.08)' : 'rgba(15, 23, 42, 0.06)',
      text: {
        primary: mode === 'dark' ? '#f1f5f9' : '#0f172a',
        secondary: mode === 'dark' ? '#94a3b8' : '#475569'
      }
    },
    typography: {
      fontFamily: '"Outfit", "Inter", "Roboto", sans-serif',
      h4: { fontWeight: 800, letterSpacing: '-0.025em' },
      h5: { fontWeight: 700, letterSpacing: '-0.02em' },
      h6: { fontWeight: 700, letterSpacing: '-0.01em' },
      body1: { fontSize: '0.975rem', lineHeight: 1.6 },
      body2: { fontSize: '0.875rem', lineHeight: 1.5 },
      button: { textTransform: 'none', fontWeight: 600 }
    },
    shape: {
      borderRadius: 16
    },
    components: {
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: 12,
            minHeight: 40,
            padding: '8px 16px',
            boxShadow: 'none',
            '&:hover': {
              boxShadow: '0 4px 12px rgba(99, 102, 241, 0.15)'
            }
          }
        }
      },
      MuiCard: {
        styleOverrides: {
          root: ({ theme }) => ({
            borderRadius: 20,
            border: `1px solid ${theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.04)' : 'rgba(15, 23, 42, 0.05)'}`,
            boxShadow: theme.palette.mode === 'dark'
              ? '0 20px 40px rgba(0, 0, 0, 0.3)'
              : '0 20px 40px rgba(15, 23, 42, 0.03)',
            backgroundColor: theme.palette.mode === 'dark' ? 'rgba(15, 20, 34, 0.6)' : 'rgba(255, 255, 255, 0.8)',
            backdropFilter: 'blur(20px)',
            transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)'
          })
        }
      },
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: 8,
            fontWeight: 600,
            fontSize: '0.775rem'
          }
        }
      },
      MuiTextField: {
        defaultProps: {
          size: 'medium'
        },
        styleOverrides: {
          root: ({ theme }) => ({
            '& .MuiOutlinedInput-root': {
              borderRadius: 12,
              backgroundColor: theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.02)' : 'rgba(0, 0, 0, 0.01)',
              '& fieldset': {
                borderColor: theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.08)' : 'rgba(0, 0, 0, 0.08)'
              },
              '&:hover fieldset': {
                borderColor: theme.palette.primary.main
              }
            }
          })
        }
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none'
          }
        }
      }
    }
  });
