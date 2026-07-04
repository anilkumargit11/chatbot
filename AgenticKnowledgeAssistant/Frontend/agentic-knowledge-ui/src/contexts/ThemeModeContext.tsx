import { CssBaseline, ThemeProvider } from '@mui/material';
import { createContext, PropsWithChildren, useContext, useMemo, useState } from 'react';
import { createAppTheme } from '../theme/createAppTheme';

type ThemeModeContextValue = {
  mode: 'light' | 'dark';
  toggleMode: () => void;
};

const ThemeModeContext = createContext<ThemeModeContextValue | undefined>(undefined);

export function ThemeModeProvider({ children }: PropsWithChildren) {
  const preferredMode = (localStorage.getItem('theme-mode') as 'light' | 'dark' | null) ?? 'light';
  const [mode, setMode] = useState<'light' | 'dark'>(preferredMode);
  const theme = useMemo(() => createAppTheme(mode), [mode]);

  const value = useMemo(
    () => ({
      mode,
      toggleMode: () => {
        setMode((current) => {
          const nextMode = current === 'light' ? 'dark' : 'light';
          localStorage.setItem('theme-mode', nextMode);
          return nextMode;
        });
      }
    }),
    [mode]
  );

  return (
    <ThemeModeContext.Provider value={value}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </ThemeProvider>
    </ThemeModeContext.Provider>
  );
}

export function useThemeMode() {
  const context = useContext(ThemeModeContext);

  if (!context) {
    throw new Error('useThemeMode must be used within ThemeModeProvider');
  }

  return context;
}
