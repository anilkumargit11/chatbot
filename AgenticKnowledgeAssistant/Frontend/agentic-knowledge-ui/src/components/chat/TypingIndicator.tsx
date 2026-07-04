import { Box, Stack } from '@mui/material';

export function TypingIndicator() {
  return (
    <Stack direction="row" spacing={0.75} sx={{ py: 0.75 }}>
      {[0, 1, 2].map((index) => (
        <Box
          key={index}
          sx={{
            width: 8,
            height: 8,
            borderRadius: '50%',
            bgcolor: 'text.secondary',
            animation: 'pulse 1s ease-in-out infinite',
            animationDelay: `${index * 0.15}s`,
            '@keyframes pulse': {
              '0%, 80%, 100%': { opacity: 0.25, transform: 'translateY(0)' },
              '40%': { opacity: 1, transform: 'translateY(-3px)' }
            }
          }}
        />
      ))}
    </Stack>
  );
}
