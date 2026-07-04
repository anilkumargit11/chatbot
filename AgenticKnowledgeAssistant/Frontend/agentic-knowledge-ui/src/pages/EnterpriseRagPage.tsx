import CloudUploadOutlinedIcon from '@mui/icons-material/CloudUploadOutlined';
import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import SendOutlinedIcon from '@mui/icons-material/SendOutlined';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  IconButton,
  InputBase,
  LinearProgress,
  Paper,
  Stack,
  Tooltip,
  Typography
} from '@mui/material';
import { FormEvent, useState } from 'react';
import { toast } from 'react-toastify';
import { PageHeader } from '../components/common/PageHeader';
import { RagDocument, RagSearchResultDto } from '../models/api';
import { ragApi } from '../services/ragApi';

export function EnterpriseRagPage() {
  const [uploading, setUploading] = useState(false);
  const [activeDocument, setActiveDocument] = useState<RagDocument | null>(null);
  const [query, setQuery] = useState('');
  const [answer, setAnswer] = useState('');
  const [results, setResults] = useState<RagSearchResultDto[]>([]);
  const [error, setError] = useState('');

  async function upload(file?: File) {
    if (!file) return;
    setUploading(true);
    setError('');
    try {
      const uploaded = await ragApi.upload(file);
      const document = await ragApi.getDocument(uploaded.documentId);
      setActiveDocument(document);
      toast.success('Document indexed for Enterprise RAG');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  }

  async function search(event: FormEvent) {
    event.preventDefault();
    if (!query.trim()) return;
    setError('');
    try {
      const [searchResults, chat] = await Promise.all([
        ragApi.search(query),
        ragApi.chat(query)
      ]);
      setResults(searchResults);
      setAnswer(chat.answer);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'RAG search failed');
    }
  }

  async function deleteDocument() {
    if (!activeDocument || !window.confirm(`Delete ${activeDocument.fileName}?`)) return;
    await ragApi.deleteDocument(activeDocument.documentId);
    setActiveDocument(null);
    setResults([]);
    setAnswer('');
    toast.success('RAG document deleted');
  }

  return (
    <Box>
      <PageHeader
        eyebrow="Enterprise RAG"
        title="Knowledge Retrieval"
        description="Upload, index, search, and chat with grounded enterprise knowledge."
      />

      {error && <Alert severity="error" sx={{ mb: 2, borderRadius: 2 }}>{error}</Alert>}

      <Stack spacing={2}>
        <Card variant="outlined" sx={{ borderRadius: 2 }}>
          <CardContent>
            <Stack direction={{ xs: 'column', md: 'row' }} alignItems={{ xs: 'stretch', md: 'center' }} spacing={2}>
              <Button component="label" variant="contained" startIcon={<CloudUploadOutlinedIcon />} sx={{ borderRadius: 2 }}>
                Upload Enterprise Document
                <input
                  hidden
                  type="file"
                  accept=".pdf,.docx,.txt,.md,.csv,.xlsx,.xls,.pptx,.json,.xml,.html,.zip,.sql,.cs,.js,.ts,.tsx,.py,.java,.yml,.yaml"
                  onChange={(event) => void upload(event.target.files?.[0])}
                />
              </Button>
              <Stack direction="row" spacing={1} flexWrap="wrap">
                <Chip label="Virus scan hook" variant="outlined" />
                <Chip label="Text extraction" variant="outlined" />
                <Chip label="Chunking" variant="outlined" />
                <Chip label="Embeddings" variant="outlined" />
                <Chip label="Hybrid search" variant="outlined" />
              </Stack>
            </Stack>
            {uploading && <LinearProgress sx={{ mt: 2, borderRadius: 999 }} />}
          </CardContent>
        </Card>

        {activeDocument && (
          <Card variant="outlined" sx={{ borderRadius: 2 }}>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1}>
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Typography fontWeight={800} noWrap>{activeDocument.fileName}</Typography>
                  <Typography color="text.secondary" variant="body2">{activeDocument.summary || activeDocument.title}</Typography>
                </Box>
                <Chip color="success" label={activeDocument.processingStatus} />
                <Chip label={`${activeDocument.chunkCount} chunks`} />
                <Chip label={`${activeDocument.embeddingCount} embeddings`} />
                <Tooltip title="Delete document">
                  <IconButton color="error" onClick={() => void deleteDocument()}>
                    <DeleteOutlineOutlinedIcon />
                  </IconButton>
                </Tooltip>
              </Stack>
            </CardContent>
          </Card>
        )}

        <Paper component="form" onSubmit={search} variant="outlined" sx={{ p: 1, display: 'flex', alignItems: 'center', gap: 1, borderRadius: 2 }}>
          <SearchOutlinedIcon sx={{ color: 'text.secondary' }} />
          <InputBase fullWidth value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Ask across indexed enterprise knowledge" />
          <IconButton color="primary" type="submit" disabled={!query.trim()}>
            <SendOutlinedIcon />
          </IconButton>
        </Paper>

        {answer && (
          <Card variant="outlined" sx={{ borderRadius: 2 }}>
            <CardContent>
              <Typography fontWeight={800} sx={{ mb: 1 }}>Grounded Answer</Typography>
              <Typography whiteSpace="pre-wrap">{answer}</Typography>
            </CardContent>
          </Card>
        )}

        <Stack spacing={1}>
          {results.map((result) => (
            <Card key={result.ChunkId} variant="outlined" sx={{ borderRadius: 2 }}>
              <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.75 }}>
                  <Typography fontWeight={800} variant="body2">{result.FileName}</Typography>
                  <Chip size="small" label={`Chunk ${result.ChunkIndex}`} />
                  <Chip size="small" label={`Score ${result.HybridScore.toFixed(3)}`} />
                </Stack>
                <Typography color="text.secondary" variant="body2">{result.Content}</Typography>
              </CardContent>
            </Card>
          ))}
        </Stack>
      </Stack>
    </Box>
  );
}
