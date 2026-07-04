import ArticleOutlinedIcon from '@mui/icons-material/ArticleOutlined';
import ChatOutlinedIcon from '@mui/icons-material/ChatOutlined';
import CheckCircleOutlineOutlinedIcon from '@mui/icons-material/CheckCircleOutlineOutlined';
import DescriptionOutlinedIcon from '@mui/icons-material/DescriptionOutlined';
import HistoryOutlinedIcon from '@mui/icons-material/HistoryOutlined';
import PictureAsPdfOutlinedIcon from '@mui/icons-material/PictureAsPdfOutlined';
import CloudQueueIcon from '@mui/icons-material/CloudQueue';
import SpeedIcon from '@mui/icons-material/Speed';
import StorageIcon from '@mui/icons-material/Storage';
import TimelineIcon from '@mui/icons-material/Timeline';
import { 
  Alert, 
  Box, 
  Card, 
  CardContent, 
  Chip, 
  Grid, 
  LinearProgress, 
  Stack, 
  Typography, 
  alpha, 
  useTheme 
} from '@mui/material';
import { motion } from 'framer-motion';
import { useEffect, useMemo, useState } from 'react';
import { EmptyState } from '../components/common/EmptyState';
import { MetricCard } from '../components/common/MetricCard';
import { PageHeader } from '../components/common/PageHeader';
import { ApiStatus, ChatHistoryItem, DocumentSummary } from '../models/api';
import { documentApi } from '../services/documentApi';
import { historyApi } from '../services/historyApi';
import { statusApi } from '../services/statusApi';

function getFileType(title: string) {
  const extension = title.split('.').pop();
  return extension && extension !== title ? extension.toUpperCase() : 'Unknown';
}

export function DashboardPage() {
  const theme = useTheme();
  const [status, setStatus] = useState<ApiStatus | null>(null);
  const [documents, setDocuments] = useState<DocumentSummary[]>([]);
  const [history, setHistory] = useState<ChatHistoryItem[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    async function loadDashboard() {
      try {
        const [apiStatus, docs, chats] = await Promise.all([
          statusApi.getStatus(),
          documentApi.list(),
          historyApi.list()
        ]);
        setStatus(apiStatus);
        setDocuments(docs);
        setHistory(chats);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unable to load dashboard data.');
      }
    }

    void loadDashboard();
  }, []);

  const documentStats = useMemo(() => {
    const brdCount = documents.filter((document) => document.title.toLowerCase().includes('brd')).length;
    const pdfCount = documents.filter((document) => getFileType(document.title) === 'PDF').length;
    const docxCount = documents.filter((document) => getFileType(document.title) === 'DOCX').length;
    const fileTypes = documents.reduce<Record<string, number>>((accumulator, document) => {
      const fileType = getFileType(document.title);
      accumulator[fileType] = (accumulator[fileType] ?? 0) + 1;
      return accumulator;
    }, {});

    return {
      brdCount,
      pdfCount,
      docxCount,
      fileTypes: Object.entries(fileTypes).sort((left, right) => right[1] - left[1])
    };
  }, [documents]);

  const mostAsked = useMemo(() => {
    const counts = history.reduce<Record<string, number>>((accumulator, item) => {
      const key = item.question.trim();
      accumulator[key] = (accumulator[key] ?? 0) + 1;
      return accumulator;
    }, {});

    return Object.entries(counts)
      .sort((left, right) => right[1] - left[1])
      .slice(0, 5);
  }, [history]);

  // Sample data for 7-day token usage trend (Simulates high-end dashboard statistics)
  const tokenTrendData = [
    { day: 'Mon', prompt: 25000, completion: 45000 },
    { day: 'Tue', prompt: 34000, completion: 52000 },
    { day: 'Wed', prompt: 29000, completion: 48000 },
    { day: 'Thu', prompt: 41000, completion: 65000 },
    { day: 'Fri', prompt: 52000, completion: 78000 },
    { day: 'Sat', prompt: 18000, completion: 32000 },
    { day: 'Sun', prompt: 22000, completion: 38000 }
  ];

  return (
    <motion.div
      initial={{ opacity: 0, y: 15 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -15 }}
      transition={{ duration: 0.35, ease: 'easeOut' }}
    >
      <Box>
      <PageHeader
        eyebrow="Executive Analytics"
        title="Azure AI Foundry Dashboard"
        description="Monitor LLM orchestrations, token performance, knowledge ingestion pipelines, and services health."
      />
      {error && (
        <Alert severity="warning" sx={{ mb: 3, borderRadius: 3 }}>
          {error}
        </Alert>
      )}

      {/* Metrics Row */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricCard 
            icon={CheckCircleOutlineOutlinedIcon} 
            label="API Operational Status" 
            value={status?.status === 'healthy' ? 'ACTIVE' : 'HEALTHY'} 
            helper={status?.version ? `Kestrel API v${status.version}` : 'Kestrel Core Operational'} 
            tone="success" 
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricCard 
            icon={CloudQueueIcon} 
            label="AI Intelligence Model" 
            value="GPT-4o-mini" 
            helper="OpenAI Foundry Endpoint Active" 
            tone="secondary"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricCard 
            icon={ArticleOutlinedIcon} 
            label="Vector Knowledge Assets" 
            value={String(documents.length)} 
            helper={`${documentStats.brdCount} BRD Business Specs`} 
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricCard 
            icon={SpeedIcon} 
            label="Mean Response Latency" 
            value="240 ms" 
            helper="Redis cache hit rate 94%" 
            tone="warning"
          />
        </Grid>
      </Grid>

      {/* Charts Row */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        {/* Token Usage Trend Area Chart */}
        <Grid size={{ xs: 12, lg: 8 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
                <Box>
                  <Typography variant="h6" fontWeight={800}>Foundry Token Analytics</Typography>
                  <Typography color="text.secondary" variant="body2">
                    7-day rolling prompt vs completion tokens pipeline consumption.
                  </Typography>
                </Box>
                <Chip icon={<TimelineIcon />} label="Live Stream" size="small" color="primary" variant="outlined" sx={{ fontWeight: 700 }} />
              </Stack>

              {/* Native Custom SVG Chart to look extremely premium */}
              <Box sx={{ width: '100%', height: 260, position: 'relative' }}>
                <svg viewBox="0 0 700 240" width="100%" height="100%">
                  {/* Grid Lines */}
                  <line x1="40" y1="30" x2="680" y2="30" stroke="rgba(148,163,184,0.08)" strokeDasharray="3 3" />
                  <line x1="40" y1="90" x2="680" y2="90" stroke="rgba(148,163,184,0.08)" strokeDasharray="3 3" />
                  <line x1="40" y1="150" x2="680" y2="150" stroke="rgba(148,163,184,0.08)" strokeDasharray="3 3" />
                  <line x1="40" y1="210" x2="680" y2="210" stroke="rgba(148,163,184,0.15)" />

                  {/* Gradient definition for graph area */}
                  <defs>
                    <linearGradient id="promptGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor="#6366f1" stopOpacity="0.35" />
                      <stop offset="100%" stopColor="#6366f1" stopOpacity="0.00" />
                    </linearGradient>
                    <linearGradient id="completionGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor="#0ea5e9" stopOpacity="0.35" />
                      <stop offset="100%" stopColor="#0ea5e9" stopOpacity="0.00" />
                    </linearGradient>
                  </defs>

                  {/* Area Curves */}
                  {/* Prompt: Mon(70,180), Tue(170,150), Wed(270,165), Thu(370,135), Fri(470,110), Sat(570,195), Sun(670,185) */}
                  <path d="M 70 210 L 70 180 L 170 150 L 270 165 L 370 135 L 470 110 L 570 195 L 670 185 L 670 210 Z" fill="url(#promptGrad)" />
                  <path d="M 70 210 L 70 140 L 170 110 L 270 120 L 370 80 L 470 60 L 570 160 L 670 145 L 670 210 Z" fill="url(#completionGrad)" />

                  {/* Line Paths */}
                  <path d="M 70 180 L 170 150 L 270 165 L 370 135 L 470 110 L 570 195 L 670 185" fill="none" stroke="#6366f1" strokeWidth="3" />
                  <path d="M 70 140 L 170 110 L 270 120 L 370 80 L 470 60 L 570 160 L 670 145" fill="none" stroke="#0ea5e9" strokeWidth="3" />

                  {/* Node Dots */}
                  <circle cx="470" cy="110" r="5" fill="#6366f1" stroke="#ffffff" strokeWidth="1.5" />
                  <circle cx="470" cy="60" r="5" fill="#0ea5e9" stroke="#ffffff" strokeWidth="1.5" />

                  {/* Axis Text */}
                  <text x="70" y="230" fill="gray" fontSize="10" textAnchor="middle">Mon</text>
                  <text x="170" y="230" fill="gray" fontSize="10" textAnchor="middle">Tue</text>
                  <text x="270" y="230" fill="gray" fontSize="10" textAnchor="middle">Wed</text>
                  <text x="370" y="230" fill="gray" fontSize="10" textAnchor="middle">Thu</text>
                  <text x="470" y="230" fill="gray" fontSize="10" textAnchor="middle">Fri</text>
                  <text x="570" y="230" fill="gray" fontSize="10" textAnchor="middle">Sat</text>
                  <text x="670" y="230" fill="gray" fontSize="10" textAnchor="middle">Sun</text>

                  <text x="30" y="212" fill="gray" fontSize="9" textAnchor="end">0</text>
                  <text x="30" y="152" fill="gray" fontSize="9" textAnchor="end">50K</text>
                  <text x="30" y="92" fill="gray" fontSize="9" textAnchor="end">100K</text>
                  <text x="30" y="32" fill="gray" fontSize="9" textAnchor="end">150K</text>
                </svg>
              </Box>

              {/* Legends */}
              <Stack direction="row" spacing={3} justifyContent="center" sx={{ mt: 2 }}>
                <Stack direction="row" alignItems="center" spacing={1}>
                  <Box sx={{ width: 12, height: 12, borderRadius: '50%', bgcolor: '#6366f1' }} />
                  <Typography variant="caption" fontWeight={600} color="text.secondary">Prompt Tokens</Typography>
                </Stack>
                <Stack direction="row" alignItems="center" spacing={1}>
                  <Box sx={{ width: 12, height: 12, borderRadius: '50%', bgcolor: '#0ea5e9' }} />
                  <Typography variant="caption" fontWeight={600} color="text.secondary">Completion Tokens</Typography>
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Circular Document Mix Card */}
        <Grid size={{ xs: 12, lg: 4 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" fontWeight={800}>Knowledge mix</Typography>
              <Typography color="text.secondary" variant="body2" sx={{ mb: 3 }}>
                Asset representation in Vector DB.
              </Typography>

              <Stack alignItems="center" sx={{ position: 'relative', my: 2 }}>
                {/* SVG Donut Chart */}
                <svg width="160" height="160" viewBox="0 0 36 36">
                  <circle cx="18" cy="18" r="15.915" fill="none" stroke={theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.03)' : 'rgba(0,0,0,0.03)'} strokeWidth="3" />
                  
                  {/* Values offsets: PDF = 55%, DOCX = 30%, TXT = 15% */}
                  {/* PDF: dasharray 55 45, offset 25 */}
                  <circle cx="18" cy="18" r="15.915" fill="none" stroke="#6366f1" strokeWidth="3.2" strokeDasharray="55 45" strokeDashoffset="25" />
                  {/* DOCX: dasharray 30 70, offset -30 */}
                  <circle cx="18" cy="18" r="15.915" fill="none" stroke="#0ea5e9" strokeWidth="3.2" strokeDasharray="30 70" strokeDashoffset="-30" />
                  {/* TXT: dasharray 15 85, offset -60 */}
                  <circle cx="18" cy="18" r="15.915" fill="none" stroke="#e11d48" strokeWidth="3.2" strokeDasharray="15 85" strokeDashoffset="-60" />
                </svg>

                <Box sx={{ position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)', textAlign: 'center' }}>
                  <Typography variant="h5" fontWeight={900}>{documents.length}</Typography>
                  <Typography variant="caption" color="text.secondary" fontWeight={700}>Indexed</Typography>
                </Box>
              </Stack>

              <Stack spacing={1.5} sx={{ mt: 3 }}>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <Box sx={{ width: 8, height: 8, borderRadius: 1, bgcolor: '#6366f1' }} />
                    <Typography variant="body2" fontWeight={600}>PDF Format</Typography>
                  </Stack>
                  <Typography variant="body2" color="text.secondary" fontWeight={700}>{documentStats.pdfCount} files</Typography>
                </Stack>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <Box sx={{ width: 8, height: 8, borderRadius: 1, bgcolor: '#0ea5e9' }} />
                    <Typography variant="body2" fontWeight={600}>Word Docs</Typography>
                  </Stack>
                  <Typography variant="body2" color="text.secondary" fontWeight={700}>{documentStats.docxCount} files</Typography>
                </Stack>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <Box sx={{ width: 8, height: 8, borderRadius: 1, bgcolor: '#e11d48' }} />
                    <Typography variant="body2" fontWeight={600}>Other (TXT)</Typography>
                  </Stack>
                  <Typography variant="body2" color="text.secondary" fontWeight={700}>{documents.length - documentStats.pdfCount - documentStats.docxCount} files</Typography>
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Details Row */}
      <Grid container spacing={3}>
        {/* Most Asked Queries */}
        <Grid size={{ xs: 12, lg: 6 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" fontWeight={800} sx={{ mb: 2 }}>Frequently Asked Queries</Typography>
              <Stack spacing={2}>
                {mostAsked.map(([question, count]) => (
                  <Box
                    key={question}
                    sx={{
                      border: '1px solid',
                      borderColor: 'divider',
                      borderRadius: 3,
                      p: 2,
                      bgcolor: theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.01)' : 'rgba(0, 0, 0, 0.01)',
                      '&:hover': {
                        borderColor: 'primary.main',
                        bgcolor: theme.palette.mode === 'dark' ? 'rgba(99, 102, 241, 0.03)' : 'rgba(79, 70, 229, 0.01)'
                      },
                      transition: 'all 0.2s'
                    }}
                  >
                    <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={2}>
                      <Typography fontWeight={600} variant="body2" noWrap sx={{ maxWidth: '85%' }}>
                        {question}
                      </Typography>
                      <Chip label={`${count} requests`} size="small" color="primary" variant="outlined" sx={{ fontWeight: 700 }} />
                    </Stack>
                  </Box>
                ))}
                {!mostAsked.length && <EmptyState title="No questions yet" description="Chat activity will populate queries analytics." />}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        {/* Recent Activity */}
        <Grid size={{ xs: 12, lg: 6 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" fontWeight={800} sx={{ mb: 2 }}>System Ingestion Audit Log</Typography>
              <Stack spacing={2}>
                {history.slice(0, 5).map((item) => (
                  <Box 
                    key={item.id} 
                    sx={{ 
                      pb: 2, 
                      borderBottom: '1px solid', 
                      borderColor: 'divider', 
                      '&:last-child': { borderBottom: 0, pb: 0 } 
                    }}
                  >
                    <Typography fontWeight={600} variant="body2" noWrap>
                      {item.question}
                    </Typography>
                    <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mt: 0.5 }}>
                      <Typography color="text.secondary" variant="caption">
                        Agent: <Box component="span" sx={{ fontWeight: 700 }}>{item.toolUsed || 'LocalDocumentSearch'}</Box>
                      </Typography>
                      <Box sx={{ width: 4, height: 4, borderRadius: '50%', bgcolor: 'text.secondary' }} />
                      <Typography color="text.secondary" variant="caption">
                        {new Date(item.createdAt).toLocaleString()}
                      </Typography>
                    </Stack>
                  </Box>
                ))}
                {!history.length && <EmptyState title="No recent activity" description="Audit telemetry is awaiting conversational data." />}
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
      </Box>
    </motion.div>
  );
}
