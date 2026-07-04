import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import PsychologyOutlinedIcon from '@mui/icons-material/PsychologyOutlined';
import ShieldOutlinedIcon from '@mui/icons-material/ShieldOutlined';
import KeyboardArrowLeftOutlinedIcon from '@mui/icons-material/KeyboardArrowLeftOutlined';
import {
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Container,
  FormControlLabel,
  Link,
  Stack,
  TextField,
  Typography
} from '@mui/material';
import { FormEvent, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { toast } from 'react-toastify';
import { useAuth } from '../contexts/AuthContext';
import { isValidEmail } from '../utils/validation';
import { motion } from 'framer-motion';

export function LoginPage() {
  const { login, verifyMfa } = useAuth();
  
  // Login Form State
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // MFA Challenge State
  const [isMfaRequired, setIsMfaRequired] = useState(false);
  const [mfaToken, setMfaToken] = useState('');
  const [mfaCode, setMfaCode] = useState('');

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!email || !password) {
      toast.error('Email and password are required');
      return;
    }

    if (!isValidEmail(email)) {
      toast.error('Enter a valid email address');
      return;
    }

    setIsSubmitting(true);
    try {
      const response = await login({ email, password, rememberMe });
      if (response.isMfaRequired && response.mfaToken) {
        setIsMfaRequired(true);
        setMfaToken(response.mfaToken);
        toast.info('Multi-factor authentication required. Please check your Authenticator app.');
      }
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Login failed');
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleMfaSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!mfaCode.trim()) {
      toast.error('Verification code is required');
      return;
    }

    setIsSubmitting(true);
    try {
      await verifyMfa(mfaToken, mfaCode, rememberMe);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'MFA Verification failed');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.98 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.35, ease: 'easeOut' }}
      style={{ minHeight: '100vh', display: 'grid', placeItems: 'center', backgroundColor: 'transparent', width: '100%' }}
    >
      <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', bgcolor: 'background.default', p: 2, width: '100%' }}>
      <Container maxWidth="xs">
        <Card>
          <CardContent sx={{ p: 4 }}>
            {!isMfaRequired ? (
              // Standard Email/Password Form
              <Stack alignItems="center" spacing={2.5}>
                <Avatar 
                  sx={{ 
                    bgcolor: 'primary.main', 
                    width: 56, 
                    height: 56, 
                    boxShadow: '0 8px 16px rgba(99, 102, 241, 0.25)',
                    background: 'linear-gradient(135deg, #6366f1 0%, #0ea5e9 100%)'
                  }}
                >
                  <PsychologyOutlinedIcon sx={{ fontSize: 32, color: '#ffffff' }} />
                </Avatar>
                <Box textAlign="center">
                  <Typography variant="h5" fontWeight={900}>Copilot Core</Typography>
                  <Typography color="text.secondary" variant="body2" sx={{ mt: 0.5 }}>
                    Sign in to your enterprise assistant
                  </Typography>
                </Box>
                <Stack component="form" onSubmit={handleSubmit} spacing={2.5} sx={{ width: '100%', mt: 1 }}>
                  <TextField 
                    autoComplete="email" 
                    fullWidth 
                    label="Email Address" 
                    onChange={(event) => setEmail(event.target.value)} 
                    required 
                    type="email" 
                    value={email} 
                  />
                  <TextField 
                    autoComplete="current-password" 
                    fullWidth 
                    label="Password" 
                    onChange={(event) => setPassword(event.target.value)} 
                    required 
                    type="password" 
                    value={password} 
                  />
                  <FormControlLabel 
                    control={<Checkbox checked={rememberMe} onChange={(event) => setRememberMe(event.target.checked)} />} 
                    label="Remember my session" 
                  />
                  <Button disabled={isSubmitting} fullWidth startIcon={<LockOutlinedIcon />} type="submit" variant="contained">
                    {isSubmitting ? 'Verifying...' : 'Sign In'}
                  </Button>
                  <Stack direction="row" justifyContent="space-between">
                    <Link component={RouterLink} to="/register" underline="hover" sx={{ fontWeight: 600 }}>
                      Create account
                    </Link>
                    <Link component={RouterLink} to="/forgot-password" underline="hover" sx={{ fontWeight: 600 }}>
                      Forgot password?
                    </Link>
                  </Stack>
                </Stack>
              </Stack>
            ) : (
              // MFA Verification Code Form
              <Stack alignItems="center" spacing={2.5}>
                <Avatar 
                  sx={{ 
                    bgcolor: 'secondary.main', 
                    width: 56, 
                    height: 56, 
                    boxShadow: '0 8px 16px rgba(14, 165, 233, 0.25)',
                    background: 'linear-gradient(135deg, #0ea5e9 0%, #2dd4bf 100%)'
                  }}
                >
                  <ShieldOutlinedIcon sx={{ fontSize: 32, color: '#ffffff' }} />
                </Avatar>
                <Box textAlign="center">
                  <Typography variant="h5" fontWeight={900}>Two-Step Verification</Typography>
                  <Typography color="text.secondary" variant="body2" sx={{ mt: 0.5 }}>
                    Enter the code from your authenticator app or one of your backup recovery codes.
                  </Typography>
                </Box>
                <Stack component="form" onSubmit={handleMfaSubmit} spacing={2.5} sx={{ width: '100%', mt: 1 }}>
                  <TextField 
                    fullWidth 
                    label="Authenticator Code / Backup Code" 
                    onChange={(event) => setMfaCode(event.target.value)} 
                    required 
                    autoFocus
                    placeholder="Enter 6-digit code or backup key"
                    inputProps={{ style: { textAlign: 'center', letterSpacing: '0.1em', fontSize: '1.1rem', fontWeight: 700 } }}
                    value={mfaCode} 
                  />
                  <Button disabled={isSubmitting} fullWidth startIcon={<LockOutlinedIcon />} type="submit" variant="contained">
                    {isSubmitting ? 'Verifying Code...' : 'Verify Code'}
                  </Button>
                  <Button 
                    startIcon={<KeyboardArrowLeftOutlinedIcon />} 
                    fullWidth 
                    variant="text" 
                    onClick={() => {
                      setIsMfaRequired(false);
                      setMfaCode('');
                    }}
                  >
                    Back to login
                  </Button>
                </Stack>
              </Stack>
            )}
          </CardContent>
        </Card>
      </Container>
    </Box>
    </motion.div>
  );
}
