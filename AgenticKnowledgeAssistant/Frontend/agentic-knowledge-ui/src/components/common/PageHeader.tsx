import { Box, Stack, Typography } from '@mui/material';
import { ReactNode } from 'react';

type PageHeaderProps = {
  eyebrow: string;
  title: string;
  description?: string;
  actions?: ReactNode;
};

export function PageHeader({ eyebrow, title, description, actions }: PageHeaderProps) {
  return (
    <Stack
      direction={{ xs: 'column', md: 'row' }}
      justifyContent="space-between"
      alignItems={{ xs: 'flex-start', md: 'center' }}
      spacing={2}
      sx={{ mb: 3 }}
    >
      <Box>
        <Typography color="primary" fontWeight={800} variant="overline">
          {eyebrow}
        </Typography>
        <Typography variant="h4">{title}</Typography>
        {description && (
          <Typography color="text.secondary" sx={{ mt: 0.75 }} variant="body1">
            {description}
          </Typography>
        )}
      </Box>
      {actions}
    </Stack>
  );
}
