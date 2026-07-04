import { Box, Button, Card, CardContent, Container, Link, Stack, TextField, Typography } from '@mui/material';
import { FormEvent, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { toast } from 'react-toastify';
import { isValidEmail } from '../utils/validation';

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!isValidEmail(email)) {
      toast.error('Enter a valid email address');
      return;
    }

    toast.info('Password reset workflow is ready for SMTP integration.');
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', bgcolor: 'background.default', p: 2 }}>
      <Container maxWidth="xs">
        <Card>
          <CardContent sx={{ p: 4 }}>
            <Stack component="form" onSubmit={handleSubmit} spacing={2.5}>
              <Box textAlign="center">
                <Typography variant="h5">Forgot Password</Typography>
                <Typography color="text.secondary">Enter your email to start recovery</Typography>
              </Box>
              <TextField autoComplete="email" fullWidth label="Email" onChange={(event) => setEmail(event.target.value)} required type="email" value={email} />
              <Button fullWidth type="submit" variant="contained">
                Continue
              </Button>
              <Link component={RouterLink} textAlign="center" to="/login" underline="hover">
                Back to sign in
              </Link>
            </Stack>
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
}
