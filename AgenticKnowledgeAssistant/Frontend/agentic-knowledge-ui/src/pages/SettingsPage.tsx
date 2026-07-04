import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined';
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined';
import ShieldOutlinedIcon from '@mui/icons-material/ShieldOutlined';
import FileDownloadOutlinedIcon from '@mui/icons-material/FileDownloadOutlined';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import { 
  Box, 
  Card, 
  CardContent, 
  Divider, 
  FormControlLabel, 
  Stack, 
  Switch, 
  TextField, 
  Typography, 
  Button, 
  Alert,
  Grid, 
  Paper,
  IconButton,
  Chip,
  CircularProgress
} from '@mui/material';
import { useEffect, useState } from 'react';
import { env } from '../config/env';
import { useThemeMode } from '../contexts/ThemeModeContext';
import { PageHeader } from '../components/common/PageHeader';
import { httpClient } from '../services/httpClient';
import { toast } from 'react-toastify';

export function SettingsPage() {
  const { mode, toggleMode } = useThemeMode();
  const [aiSettings, setAiSettings] = useState<any>(null);
  const [providerStatuses, setProviderStatuses] = useState<any[]>([]);
  const [testingProviders, setTestingProviders] = useState(false);
  const [savingProviders, setSavingProviders] = useState(false);

  // MFA Setup/Status State
  const [isConfigured, setIsConfigured] = useState(false);
  const [setupMode, setSetupMode] = useState(false);
  const [secret, setSecret] = useState('');
  const [qrCodeUri, setQrCodeUri] = useState('');
  const [verificationCode, setVerificationCode] = useState('');
  const [backupCodes, setBackupCodes] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetchMfaStatus();
    fetchAiProviders();
  }, []);

  async function fetchAiProviders() {
    try {
      const response = await httpClient.get('/ai-providers');
      const data = response.data?.data ?? response.data?.Data;
      setAiSettings(normalizeAiSettings(data?.settings ?? data?.Settings ?? null));
      setProviderStatuses(data?.providers ?? data?.Providers ?? []);
    } catch {
      toast.error('Unable to load AI provider settings');
    }
  }

  async function saveAiProviders() {
    if (!aiSettings) {
      return;
    }

    setSavingProviders(true);
    try {
      await httpClient.post('/ai-providers', aiSettings);
      toast.success('AI provider settings saved');
      await fetchAiProviders();
    } catch {
      toast.error('Unable to save AI provider settings');
    } finally {
      setSavingProviders(false);
    }
  }

  async function testAiProviders() {
    setTestingProviders(true);
    try {
      const response = await httpClient.post('/ai-providers/test');
      const data = response.data?.data ?? response.data?.Data ?? [];
      setProviderStatuses(data);
      toast.success('AI provider test completed');
    } catch {
      toast.error('Unable to test AI providers');
    } finally {
      setTestingProviders(false);
    }
  }

  function updateProvider(provider: string, field: string, value: string | boolean | number) {
    setAiSettings((current: any) => ({
      ...current,
      [provider]: {
        ...current?.[provider],
        [field]: value
      }
    }));
  }

  function updateAiRoot(field: string, value: boolean | number) {
    setAiSettings((current: any) => ({
      ...current,
      [field]: value
    }));
  }

  function normalizeAiSettings(settings: any) {
    if (!settings) {
      return null;
    }

    const provider = (name: string) => {
      const value = settings[name] ?? settings[name[0].toUpperCase() + name.slice(1)] ?? {};
      return {
        enabled: value.enabled ?? value.Enabled ?? false,
        endpoint: value.endpoint ?? value.Endpoint ?? '',
        deploymentName: value.deploymentName ?? value.DeploymentName ?? '',
        apiVersion: value.apiVersion ?? value.ApiVersion ?? '',
        apiKey: value.apiKey ?? value.ApiKey ?? '',
        model: value.model ?? value.Model ?? ''
      };
    };

    return {
      timeoutSeconds: settings.timeoutSeconds ?? settings.TimeoutSeconds ?? 8,
      autoDetectLocalProviders: settings.autoDetectLocalProviders ?? settings.AutoDetectLocalProviders ?? true,
      azureOpenAI: provider('azureOpenAI'),
      openAI: provider('openAI'),
      ollama: provider('ollama'),
      lmStudio: provider('lmStudio'),
      localLlama: provider('localLlama')
    };
  }

  function statusColor(status: string) {
    if (status === 'Connected' || status === 'Configured') {
      return 'success';
    }

    if (status === 'Disconnected') {
      return 'warning';
    }

    return 'error';
  }

  function getProviderValue(provider: any, pascalKey: string, camelKey: string) {
    return provider?.[camelKey] ?? provider?.[pascalKey];
  }

  function providerLabel(provider: any) {
    const endpoint = getProviderValue(provider, 'Endpoint', 'endpoint');
    const model = getProviderValue(provider, 'Model', 'model');
    const deployment = getProviderValue(provider, 'DeploymentName', 'deploymentName');
    const latency = getProviderValue(provider, 'LatencyMs', 'latencyMs');
    const details = [
      endpoint,
      model || deployment,
      latency ? `${latency}ms` : ''
    ].filter(Boolean);

    return details.join(' | ');
  }

  async function fetchMfaStatus() {
    try {
      const response = await httpClient.get('/mfa/status');
      if (response.data?.data) {
        setIsConfigured(response.data.data.IsConfigured || response.data.data.isConfigured || false);
      }
    } catch {
      // Ignored if user not authorized yet
    }
  }

  async function handleInitiateSetup() {
    setLoading(true);
    try {
      const response = await httpClient.post('/mfa/setup-authenticator');
      const data = response.data?.data;
      if (data) {
        setSecret(data.Secret || data.secret || '');
        setQrCodeUri(data.QrCodeUri || data.qrCodeUri || '');
        setSetupMode(true);
      }
    } catch (err) {
      toast.error('Failed to initiate MFA setup');
    } finally {
      setLoading(false);
    }
  }

  async function handleVerifySetup() {
    if (!verificationCode.trim()) {
      toast.error('Enter the verification code');
      return;
    }

    setLoading(true);
    try {
      const response = await httpClient.post('/mfa/verify-setup', {
        Code: verificationCode
      });
      const data = response.data?.data;
      if (data && data.BackupCodes) {
        setBackupCodes(data.BackupCodes || data.backupCodes || []);
        setIsConfigured(true);
        setSetupMode(false);
        toast.success('MFA activated successfully');
      } else {
        setIsConfigured(true);
        setSetupMode(false);
        toast.success('MFA activated successfully');
      }
      fetchMfaStatus();
    } catch (err) {
      toast.error('Invalid code. Activation failed.');
    } finally {
      setLoading(false);
    }
  }

  async function handleDisableMfa() {
    if (!window.confirm('Are you sure you want to disable Multi-Factor Authentication? This reduces your account security.')) {
      return;
    }

    setLoading(true);
    try {
      await httpClient.post('/mfa/disable');
      setIsConfigured(false);
      setBackupCodes([]);
      toast.info('MFA has been disabled');
      fetchMfaStatus();
    } catch (err) {
      toast.error('Failed to disable MFA');
    } finally {
      setLoading(false);
    }
  }

  const copyBackupCodes = () => {
    navigator.clipboard.writeText(backupCodes.join('\n'));
    toast.success('Backup codes copied');
  };

  const downloadBackupCodes = () => {
    const text = `Backup Verification Codes (Enterprise AI Assistant):\n\n${backupCodes.join('\n')}\n\nStore these safely. Each code is single-use.`;
    const blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `backup-codes-${Date.now()}.txt`;
    anchor.click();
    URL.revokeObjectURL(url);
    toast.success('Backup codes downloaded');
  };

  return (
    <Box sx={{ animation: 'fadeIn 0.4s ease' }}>
      <PageHeader
        eyebrow="Settings"
        title="Workspace Configuration"
        description="Manage theme preferences, API credentials, and account two-factor settings."
      />
      
      <Grid container spacing={3}>
        <Grid size={{ xs: 12 }}>
          <Card>
            <CardContent sx={{ p: 3 }}>
              <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" spacing={2} sx={{ mb: 3 }}>
                <Box>
                  <Typography variant="h6" fontWeight={700}>AI Providers</Typography>
                  <Typography color="text.secondary" variant="body2">
                    Smart Auto uses the first available provider in priority order.
                  </Typography>
                </Box>
                <Stack direction="row" spacing={1}>
                  <Button variant="outlined" onClick={testAiProviders} disabled={testingProviders}>
                    {testingProviders ? <CircularProgress size={18} /> : 'Test Connection'}
                  </Button>
                  <Button variant="contained" onClick={saveAiProviders} disabled={savingProviders || !aiSettings}>
                    Save Configuration
                  </Button>
                </Stack>
              </Stack>

              <Stack direction="row" flexWrap="wrap" gap={1} sx={{ mb: 3 }}>
                {providerStatuses.map((provider: any) => (
                  <Chip
                    key={getProviderValue(provider, 'Name', 'name')}
                    label={`${getProviderValue(provider, 'Name', 'name')}: ${getProviderValue(provider, 'Status', 'status')}`}
                    color={statusColor(getProviderValue(provider, 'Status', 'status')) as any}
                    variant="outlined"
                  />
                ))}
              </Stack>

              {providerStatuses.length > 0 && (
                <Grid container spacing={1.5} sx={{ mb: 3 }}>
                  {providerStatuses.map((provider: any) => {
                    const name = getProviderValue(provider, 'Name', 'name');
                    const status = getProviderValue(provider, 'Status', 'status');
                    const active = Boolean(getProviderValue(provider, 'IsActiveProvider', 'isActiveProvider'));
                    const failure = getProviderValue(provider, 'FailureReason', 'failureReason');

                    return (
                      <Grid size={{ xs: 12, md: 6, xl: 4 }} key={`${name}-health`}>
                        <Paper variant="outlined" sx={{ p: 1.75, borderRadius: 2, height: '100%' }}>
                          <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1}>
                            <Typography fontWeight={700}>{name}</Typography>
                            <Chip
                              size="small"
                              label={active ? 'Active' : status}
                              color={active ? 'success' : statusColor(status) as any}
                              variant={active ? 'filled' : 'outlined'}
                            />
                          </Stack>
                          <Typography color="text.secondary" variant="caption" sx={{ display: 'block', mt: 1, wordBreak: 'break-word' }}>
                            {providerLabel(provider) || 'No endpoint/model metadata'}
                          </Typography>
                          {failure && (
                            <Typography color="error.main" variant="caption" sx={{ display: 'block', mt: 1, wordBreak: 'break-word' }}>
                              {failure}
                            </Typography>
                          )}
                        </Paper>
                      </Grid>
                    );
                  })}
                </Grid>
              )}

              {aiSettings && (
                <Stack spacing={2.5}>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <TextField
                      label="Timeout Seconds"
                      type="number"
                      value={aiSettings.timeoutSeconds ?? aiSettings.TimeoutSeconds ?? 8}
                      onChange={(event) => updateAiRoot('timeoutSeconds', Number(event.target.value))}
                      sx={{ maxWidth: 180 }}
                    />
                    <FormControlLabel
                      control={
                        <Switch
                          checked={Boolean(aiSettings.autoDetectLocalProviders ?? aiSettings.AutoDetectLocalProviders)}
                          onChange={(event) => updateAiRoot('autoDetectLocalProviders', event.target.checked)}
                        />
                      }
                      label="Auto-detect local providers"
                    />
                  </Stack>

                  <Grid container spacing={2}>
                    {[
                      ['azureOpenAI', 'Azure OpenAI', ['endpoint', 'deploymentName', 'apiVersion', 'apiKey']],
                      ['openAI', 'OpenAI', ['endpoint', 'model', 'apiKey']],
                      ['ollama', 'Ollama', ['endpoint', 'model']],
                      ['lmStudio', 'LM Studio', ['endpoint', 'model']],
                      ['localLlama', 'Local Llama', ['endpoint', 'model']]
                    ].map(([key, label, fields]) => {
                      const provider = aiSettings[key as string] ?? {};
                      return (
                        <Grid size={{ xs: 12, md: 6, xl: 4 }} key={key as string}>
                          <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2, height: '100%' }}>
                            <FormControlLabel
                              control={
                                <Switch
                                  checked={Boolean(provider.enabled)}
                                  onChange={(event) => updateProvider(key as string, 'enabled', event.target.checked)}
                                />
                              }
                              label={<Typography fontWeight={700}>{label as string}</Typography>}
                            />
                            <Stack spacing={1.5} sx={{ mt: 1 }}>
                              {(fields as string[]).map((field) => (
                                <TextField
                                  key={field}
                                  fullWidth
                                  size="small"
                                  type={field === 'apiKey' ? 'password' : 'text'}
                                  label={field.replace(/([A-Z])/g, ' $1').replace(/^./, (value) => value.toUpperCase())}
                                  value={provider[field] ?? ''}
                                  onChange={(event) => updateProvider(key as string, field, event.target.value)}
                                />
                              ))}
                            </Stack>
                          </Paper>
                        </Grid>
                      );
                    })}
                  </Grid>
                </Stack>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Left Side: General preferences */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Stack spacing={3}>
            <Card>
              <CardContent sx={{ p: 3 }}>
                <Typography variant="h6" fontWeight={700} sx={{ mb: 2 }}>Appearance</Typography>
                <FormControlLabel
                  control={<Switch checked={mode === 'dark'} onChange={toggleMode} />}
                  label={
                    <Stack alignItems="center" direction="row" spacing={1}>
                      {mode === 'dark' ? <DarkModeOutlinedIcon /> : <LightModeOutlinedIcon />}
                      <Typography fontWeight={500}>{mode === 'dark' ? 'Dark Mode' : 'Light Mode'}</Typography>
                    </Stack>
                  }
                />
              </CardContent>
            </Card>

            <Card>
              <CardContent sx={{ p: 3 }}>
                <Typography variant="h6" fontWeight={700} sx={{ mb: 2 }}>API Configuration</Typography>
                <TextField fullWidth label="Gateway Service Endpoint" value={env.apiBaseUrl} slotProps={{ input: { readOnly: true } }} />
                <Typography color="text.secondary" variant="body2" sx={{ mt: 1.5 }}>
                  Managed by system orchestrator environment variables.
                </Typography>
              </CardContent>
            </Card>
          </Stack>
        </Grid>

        {/* Right Side: Security & MFA Settings */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent sx={{ p: 3 }}>
              <Stack direction="row" alignItems="center" spacing={1.5} sx={{ mb: 2.5 }}>
                <ShieldOutlinedIcon color="primary" />
                <Typography variant="h6" fontWeight={700}>Security & Authentication</Typography>
              </Stack>
              
              {!isConfigured && !setupMode && (
                <Stack spacing={2}>
                  <Alert severity="warning" sx={{ borderRadius: 3 }}>
                    Multi-factor authentication (MFA) is not enabled on your account. Turn on MFA to secure your database access.
                  </Alert>
                  <Button 
                    variant="contained" 
                    color="primary" 
                    disabled={loading} 
                    onClick={handleInitiateSetup}
                    sx={{ borderRadius: 3, py: 1.2 }}
                  >
                    Enable Authenticator MFA
                  </Button>
                </Stack>
              )}

              {setupMode && (
                <Stack spacing={2.5}>
                  <Typography variant="body2" fontWeight={600}>
                    1. Scan this QR Code with Google Authenticator or Microsoft Authenticator app:
                  </Typography>
                  
                  {qrCodeUri && (
                    <Box 
                      sx={{ 
                        p: 2, 
                        bgcolor: '#ffffff', 
                        width: 'fit-content', 
                        borderRadius: 3, 
                        border: '1px solid', 
                        borderColor: 'divider',
                        mx: 'auto'
                      }}
                    >
                      {/* Simple mock QR representation since we're offline, and secret code text */}
                      <Box sx={{ width: 140, height: 140, display: 'grid', placeItems: 'center', bgcolor: '#0b0f19', color: '#ffffff', borderRadius: 2, p: 2, textAlign: 'center' }}>
                        <Typography variant="caption" fontWeight={700}>
                          QR CODE<br />SCAN SECRET
                        </Typography>
                      </Box>
                    </Box>
                  )}

                  <Typography variant="body2" fontWeight={600}>
                    2. If you cannot scan the QR, input this secret key manually:
                  </Typography>
                  <TextField 
                    fullWidth 
                    value={secret} 
                    slotProps={{ input: { readOnly: true } }} 
                    sx={{ fontFamily: 'monospace' }}
                  />

                  <Typography variant="body2" fontWeight={600}>
                    3. Enter the 6-digit code from the authenticator app:
                  </Typography>
                  <Stack direction="row" spacing={1.5}>
                    <TextField 
                      fullWidth 
                      placeholder="Code (e.g. 123456)" 
                      value={verificationCode}
                      onChange={(e) => setVerificationCode(e.target.value)}
                    />
                    <Button 
                      variant="contained" 
                      onClick={handleVerifySetup}
                      disabled={loading}
                      sx={{ borderRadius: 3, px: 3 }}
                    >
                      Verify
                    </Button>
                  </Stack>
                  <Button variant="text" size="small" onClick={() => setSetupMode(false)}>
                    Cancel setup
                  </Button>
                </Stack>
              )}

              {isConfigured && !setupMode && (
                <Stack spacing={2.5}>
                  <Alert severity="success" sx={{ borderRadius: 3 }}>
                    Multi-factor Authenticator MFA is active and securing your account.
                  </Alert>
                  
                  {backupCodes.length > 0 && (
                    <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 3, bgcolor: 'action.hover' }}>
                      <Typography variant="subtitle2" fontWeight={700} sx={{ mb: 1.5 }}>
                        Backup Recovery Keys:
                      </Typography>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        Keep these keys offline. Each code can be entered once in place of your TOTP code during logins.
                      </Typography>
                      <Grid container spacing={1} sx={{ mb: 2 }}>
                        {backupCodes.map((code, idx) => (
                          <Grid size={{ xs: 6 }} key={idx}>
                            <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 700 }}>
                              • {code}
                            </Typography>
                          </Grid>
                        ))}
                      </Grid>
                      <Stack direction="row" spacing={1.5}>
                        <Button 
                          size="small" 
                          variant="outlined" 
                          startIcon={<ContentCopyIcon fontSize="small" />} 
                          onClick={copyBackupCodes}
                        >
                          Copy
                        </Button>
                        <Button 
                          size="small" 
                          variant="outlined" 
                          startIcon={<FileDownloadOutlinedIcon fontSize="small" />} 
                          onClick={downloadBackupCodes}
                        >
                          Download
                        </Button>
                      </Stack>
                    </Paper>
                  )}

                  <Button 
                    variant="outlined" 
                    color="error" 
                    disabled={loading} 
                    onClick={handleDisableMfa}
                    sx={{ borderRadius: 3 }}
                  >
                    Disable Multi-Factor Authentication
                  </Button>
                </Stack>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
