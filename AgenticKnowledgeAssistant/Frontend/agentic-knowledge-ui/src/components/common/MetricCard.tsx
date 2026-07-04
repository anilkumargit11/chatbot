import { Card, CardContent, Stack, SvgIconTypeMap, Typography, alpha } from '@mui/material';
import { OverridableComponent } from '@mui/material/OverridableComponent';

type MetricCardProps = {
  label: string;
  value: string;
  helper: string;
  icon: OverridableComponent<SvgIconTypeMap>;
  tone?: 'primary' | 'secondary' | 'success' | 'warning';
};

export function MetricCard({ label, value, helper, icon: Icon, tone = 'primary' }: MetricCardProps) {
  return (
    <Card>
      <CardContent>
        <Stack direction="row" justifyContent="space-between" spacing={2}>
          <Stack spacing={1}>
            <Typography color="text.secondary" variant="body2">
              {label}
            </Typography>
            <Typography variant="h4">{value}</Typography>
            <Typography color="text.secondary" variant="body2">
              {helper}
            </Typography>
          </Stack>
          <Stack
            alignItems="center"
            justifyContent="center"
            sx={(theme) => ({
              width: 44,
              height: 44,
              borderRadius: 2,
              color: theme.palette[tone].main,
              bgcolor: alpha(theme.palette[tone].main, 0.12)
            })}
          >
            <Icon />
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
}
