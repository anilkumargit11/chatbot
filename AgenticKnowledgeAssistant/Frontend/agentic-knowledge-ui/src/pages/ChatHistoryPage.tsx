import ContentCopyOutlinedIcon from '@mui/icons-material/ContentCopyOutlined';
import DeleteSweepOutlinedIcon from '@mui/icons-material/DeleteSweepOutlined';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import StarBorderOutlinedIcon from '@mui/icons-material/StarBorderOutlined';
import { Box, Button, Card, CardContent, Chip, IconButton, InputAdornment, Stack, TextField, Tooltip, Typography } from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import ReactMarkdown from 'react-markdown';
import { toast } from 'react-toastify';
import rehypeHighlight from 'rehype-highlight';
import remarkGfm from 'remark-gfm';
import { EmptyState } from '../components/common/EmptyState';
import { PageHeader } from '../components/common/PageHeader';
import { ChatHistoryItem } from '../models/api';
import { historyApi } from '../services/historyApi';

export function ChatHistoryPage() {
  const [items, setItems] = useState<ChatHistoryItem[]>([]);
  const [query, setQuery] = useState('');

  async function loadHistory() {
    setItems(await historyApi.list());
  }

  useEffect(() => {
    void loadHistory();
  }, []);

  const filteredItems = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) {
      return items;
    }

    return items.filter((item) =>
      item.question.toLowerCase().includes(normalizedQuery) ||
      item.answer.toLowerCase().includes(normalizedQuery) ||
      (item.toolUsed ?? '').toLowerCase().includes(normalizedQuery)
    );
  }, [items, query]);

  async function clearHistory() {
    await historyApi.clear();
    setItems([]);
    toast.success('Chat history cleared');
  }

  async function copyAnswer(answer: string) {
    await navigator.clipboard.writeText(answer);
    toast.success('Answer copied');
  }

  return (
    <Box>
      <PageHeader
        actions={
          <Button color="error" disabled={!items.length} onClick={() => void clearHistory()} startIcon={<DeleteSweepOutlinedIcon />} variant="outlined">
            Clear history
          </Button>
        }
        eyebrow="History"
        title="Chat history"
        description="Search, review, copy, and favorite locally stored assistant conversations."
      />
      <Card>
        <CardContent>
          <TextField
            fullWidth
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchOutlinedIcon />
                </InputAdornment>
              )
            }}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search questions, answers, or tools"
            value={query}
          />
          <Stack spacing={2} sx={{ mt: 3 }}>
            {filteredItems.map((item) => (
              <Box key={item.id} sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 2 }}>
                <Stack direction="row" flexWrap="wrap" gap={1} justifyContent="space-between" sx={{ mb: 1 }}>
                  <Stack direction="row" flexWrap="wrap" gap={1}>
                    {item.toolUsed && <Chip label={item.toolUsed} size="small" />}
                    <Chip label={new Date(item.createdAt).toLocaleString()} size="small" variant="outlined" />
                  </Stack>
                  <Stack direction="row" spacing={0.5}>
                    <Tooltip title="Favorite">
                      <IconButton size="small">
                        <StarBorderOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Copy answer">
                      <IconButton onClick={() => void copyAnswer(item.answer)} size="small">
                        <ContentCopyOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Stack>
                </Stack>
                <Typography fontWeight={800} sx={{ mb: 1 }}>
                  {item.question}
                </Typography>
                <Box
                  sx={{
                    color: 'text.secondary',
                    maxHeight: 260,
                    overflow: 'auto',
                    '& p': { m: 0 },
                    '& table': { width: '100%', borderCollapse: 'collapse', my: 1 },
                    '& th, & td': { border: 1, borderColor: 'divider', p: 1 },
                    '& pre': { bgcolor: '#111827', color: 'grey.100' }
                  }}
                >
                  <ReactMarkdown rehypePlugins={[rehypeHighlight]} remarkPlugins={[remarkGfm]}>
                    {item.answer}
                  </ReactMarkdown>
                </Box>
              </Box>
            ))}
            {!filteredItems.length && <EmptyState title="No chat history" description="Try a different search or ask the assistant a question." />}
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
