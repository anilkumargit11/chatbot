import { CircularProgress, Stack, Typography } from '@mui/material';

export function LoadingState({ label = 'Loading data' }: { label?: string }) {
  return (
    <Stack alignItems="center" justifyContent="center" spacing={2} sx={{ minHeight: 240 }}>
      <CircularProgress size={32} />
      <Typography color="text.secondary">{label}</Typography>
    </Stack>
  );
}
