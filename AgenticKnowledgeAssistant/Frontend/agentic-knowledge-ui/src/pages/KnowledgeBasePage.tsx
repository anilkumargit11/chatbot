import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import { Box, Card, CardContent, Chip, InputAdornment, Stack, TextField, Typography } from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { toast } from 'react-toastify';
import { EmptyState } from '../components/common/EmptyState';
import { PageHeader } from '../components/common/PageHeader';
import { KnowledgeItem } from '../models/api';
import { knowledgeBaseApi } from '../services/knowledgeBaseApi';

export function KnowledgeBasePage() {
  const [items, setItems] = useState<KnowledgeItem[]>([]);
  const [query, setQuery] = useState('');

  useEffect(() => {
    async function loadItems() {
      try {
        setItems(await knowledgeBaseApi.list());
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Unable to load knowledge base.');
      }
    }

    void loadItems();
  }, []);

  const filteredItems = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) {
      return items;
    }

    return items.filter(
      (item) =>
        item.title.toLowerCase().includes(normalizedQuery) ||
        item.preview.toLowerCase().includes(normalizedQuery) ||
        item.sourceType.toLowerCase().includes(normalizedQuery)
    );
  }, [items, query]);

  return (
    <Box>
      <PageHeader
        eyebrow="Knowledge Base"
        title="Browse indexed knowledge"
        description="Search documents and source snippets available to the assistant."
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
            placeholder="Search knowledge base"
            value={query}
          />
          <Stack spacing={1.5} sx={{ mt: 3 }}>
            {filteredItems.map((item) => (
              <Box key={item.id} sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 2 }}>
                <Stack alignItems="center" direction="row" justifyContent="space-between" spacing={2}>
                  <Typography fontWeight={800}>{item.title}</Typography>
                  <Chip color="primary" label={item.sourceType} size="small" variant="outlined" />
                </Stack>
                <Typography color="text.secondary" sx={{ mt: 1 }}>
                  {item.preview || 'No preview available'}
                </Typography>
                <Typography color="text.secondary" sx={{ mt: 1 }} variant="caption">
                  Added {new Date(item.createdDate).toLocaleString()}
                </Typography>
              </Box>
            ))}
            {!filteredItems.length && <EmptyState title="No knowledge found" description="Try a different search or upload more documents." />}
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
