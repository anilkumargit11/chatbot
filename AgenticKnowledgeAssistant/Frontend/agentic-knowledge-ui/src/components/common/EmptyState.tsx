import InboxOutlinedIcon from '@mui/icons-material/InboxOutlined';
import { Stack, Typography } from '@mui/material';

export function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <Stack alignItems="center" justifyContent="center" spacing={1.5} sx={{ minHeight: 220, p: 3 }}>
      <InboxOutlinedIcon color="disabled" sx={{ fontSize: 48 }} />
      <Typography fontWeight={800}>{title}</Typography>
      <Typography color="text.secondary" textAlign="center">
        {description}
      </Typography>
    </Stack>
  );
}
