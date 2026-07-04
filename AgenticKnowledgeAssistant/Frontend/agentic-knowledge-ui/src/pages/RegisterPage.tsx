import PersonAddAltOutlinedIcon from '@mui/icons-material/PersonAddAltOutlined';
import PsychologyOutlinedIcon from '@mui/icons-material/PsychologyOutlined';
import { Avatar, Box, Button, Card, CardContent, Container, Link, Stack, TextField, Typography } from '@mui/material';
import { FormEvent, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { toast } from 'react-toastify';
import { useAuth } from '../contexts/AuthContext';
import { isValidEmail, isValidMobileNumber, validatePassword } from '../utils/validation';

export function RegisterPage() {
  const { register } = useAuth();
  const [form, setForm] = useState({ fullName: '', email: '', mobileNumber: '', password: '', confirmPassword: '' });
  const [isSubmitting, setIsSubmitting] = useState(false);

  function updateField(field: keyof typeof form, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form.fullName.trim()) {
      toast.error('Full name is required');
      return;
    }

    if (!isValidEmail(form.email)) {
      toast.error('Enter a valid email address');
      return;
    }

    if (!isValidMobileNumber(form.mobileNumber)) {
      toast.error('Enter a valid mobile number');
      return;
    }

    const passwordError = validatePassword(form.password);
    if (passwordError) {
      toast.error(passwordError);
      return;
    }

    if (form.password !== form.confirmPassword) {
      toast.error('Password and confirm password must match');
      return;
    }

    setIsSubmitting(true);
    try {
      await register(form);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Registration failed');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', bgcolor: 'background.default', p: 2 }}>
      <Container maxWidth="sm">
        <Card>
          <CardContent sx={{ p: 4 }}>
            <Stack alignItems="center" spacing={2.5}>
              <Avatar sx={{ bgcolor: 'primary.main', width: 56, height: 56 }}>
                <PsychologyOutlinedIcon />
              </Avatar>
              <Box textAlign="center">
                <Typography variant="h5">Create Account</Typography>
                <Typography color="text.secondary">Register for Agentic Knowledge Assistant</Typography>
              </Box>
              <Stack component="form" onSubmit={handleSubmit} spacing={2} sx={{ width: '100%' }}>
                <TextField fullWidth label="Full Name" onChange={(event) => updateField('fullName', event.target.value)} required value={form.fullName} />
                <TextField autoComplete="email" fullWidth label="Email" onChange={(event) => updateField('email', event.target.value)} required type="email" value={form.email} />
                <TextField fullWidth label="Mobile Number" onChange={(event) => updateField('mobileNumber', event.target.value)} required value={form.mobileNumber} />
                <TextField fullWidth label="Password" onChange={(event) => updateField('password', event.target.value)} required type="password" value={form.password} />
                <TextField fullWidth label="Confirm Password" onChange={(event) => updateField('confirmPassword', event.target.value)} required type="password" value={form.confirmPassword} />
                <Button disabled={isSubmitting} fullWidth startIcon={<PersonAddAltOutlinedIcon />} type="submit" variant="contained">
                  {isSubmitting ? 'Creating account...' : 'Register'}
                </Button>
                <Link component={RouterLink} textAlign="center" to="/login" underline="hover">
                  Back to sign in
                </Link>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
}
