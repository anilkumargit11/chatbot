import AddOutlinedIcon from '@mui/icons-material/AddOutlined';
import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import PushPinOutlinedIcon from '@mui/icons-material/PushPinOutlined';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import StarBorderOutlinedIcon from '@mui/icons-material/StarBorderOutlined';
import StarOutlinedIcon from '@mui/icons-material/StarOutlined';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputBase,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography
} from '@mui/material';
import { FormEvent, useEffect, useMemo, useState } from 'react';
import { toast } from 'react-toastify';
import { PageHeader } from '../components/common/PageHeader';
import { MemoryCategory, UserMemory } from '../models/api';
import { memoryApi } from '../services/memoryApi';

const emptyForm = {
  category: 'Reusable Context',
  key: '',
  value: '',
  isPinned: false,
  isFavorite: false,
  isActive: true
};

export function AiMemoryPage() {
  const [memories, setMemories] = useState<UserMemory[]>([]);
  const [categories, setCategories] = useState<MemoryCategory[]>([]);
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [error, setError] = useState('');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<UserMemory | null>(null);
  const [form, setForm] = useState(emptyForm);

  useEffect(() => {
    void loadCategories();
  }, []);

  useEffect(() => {
    const timeout = window.setTimeout(() => void loadMemories(), 250);
    return () => window.clearTimeout(timeout);
  }, [search, category]);

  const pinned = useMemo(() => memories.filter((memory) => memory.isPinned), [memories]);
  const favorites = useMemo(() => memories.filter((memory) => memory.isFavorite), [memories]);

  async function loadCategories() {
    try {
      const rows = await memoryApi.categories();
      setCategories(rows);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to load memory categories.');
    }
  }

  async function loadMemories() {
    try {
      const rows = await memoryApi.list({
        search: search || undefined,
        category: category || undefined,
        pageSize: 100
      });
      setMemories(rows);
      setError('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to load memories.');
    }
  }

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setDialogOpen(true);
  }

  function openEdit(memory: UserMemory) {
    setEditing(memory);
    setForm({
      category: memory.category,
      key: memory.key,
      value: memory.value,
      isPinned: memory.isPinned,
      isFavorite: memory.isFavorite,
      isActive: memory.isActive
    });
    setDialogOpen(true);
  }

  async function saveMemory(event: FormEvent) {
    event.preventDefault();
    try {
      if (editing) {
        await memoryApi.update(editing.memoryId, form);
        toast.success('Memory updated');
      } else {
        await memoryApi.save(form);
        toast.success('Memory saved');
      }
      setDialogOpen(false);
      await loadMemories();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Unable to save memory');
    }
  }

  async function updateMemory(memory: UserMemory, patch: Partial<UserMemory>) {
    await memoryApi.update(memory.memoryId, {
      isActive: patch.isActive,
      isPinned: patch.isPinned,
      isFavorite: patch.isFavorite
    });
    await loadMemories();
  }

  async function deleteMemory(memory: UserMemory) {
    if (!window.confirm(`Delete memory "${memory.key}"?`)) return;
    await memoryApi.remove(memory.memoryId);
    await loadMemories();
    toast.success('Memory deleted');
  }

  return (
    <Box>
      <PageHeader
        eyebrow="AI Memory"
        title="Long-Term Memory"
        description="Manage approved preferences and reusable context used across conversations."
        actions={
          <Button startIcon={<AddOutlinedIcon />} variant="contained" onClick={openCreate} sx={{ borderRadius: 2 }}>
            Add Memory
          </Button>
        }
      />

      {error && <Alert severity="error" sx={{ mb: 2, borderRadius: 2 }}>{error}</Alert>}

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <Paper variant="outlined" sx={{ px: 1.25, py: 0.5, display: 'flex', alignItems: 'center', gap: 1, borderRadius: 2, flex: 1 }}>
          <SearchOutlinedIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
          <InputBase fullWidth value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search memories" />
        </Paper>
        <FormControl size="small" sx={{ minWidth: 240 }}>
          <InputLabel>Category</InputLabel>
          <Select label="Category" value={category} onChange={(event) => setCategory(event.target.value)}>
            <MenuItem value="">All Categories</MenuItem>
            {categories.map((item) => (
              <MenuItem key={item.categoryId} value={item.categoryName}>{item.categoryName}</MenuItem>
            ))}
          </Select>
        </FormControl>
      </Stack>

      <Stack direction="row" flexWrap="wrap" gap={1} sx={{ mb: 2 }}>
        <Chip label={`${memories.length} Saved`} />
        <Chip label={`${pinned.length} Pinned`} color="primary" variant="outlined" />
        <Chip label={`${favorites.length} Favorites`} color="warning" variant="outlined" />
      </Stack>

      <Stack spacing={1.25}>
        {memories.map((memory) => (
          <Card key={memory.memoryId} variant="outlined" sx={{ borderRadius: 2 }}>
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Stack direction={{ xs: 'column', md: 'row' }} alignItems={{ xs: 'stretch', md: 'center' }} spacing={1.5}>
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                    <Typography variant="body2" fontWeight={800} noWrap>{memory.key}</Typography>
                    <Chip size="small" label={memory.category} />
                    {!memory.isActive && <Chip size="small" color="default" label="Disabled" />}
                  </Stack>
                  <Typography color="text.secondary" variant="body2">{memory.value}</Typography>
                  <Typography color="text.secondary" variant="caption" sx={{ display: 'block', mt: 0.75 }}>
                    Updated {new Date(memory.updatedDate).toLocaleString()}
                  </Typography>
                </Box>

                <Stack direction="row" spacing={0.5} alignItems="center">
                  <Tooltip title={memory.isPinned ? 'Unpin' : 'Pin'}>
                    <IconButton onClick={() => void updateMemory(memory, { isPinned: !memory.isPinned })}>
                      <PushPinOutlinedIcon color={memory.isPinned ? 'primary' : 'inherit'} />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title={memory.isFavorite ? 'Remove favorite' : 'Favorite'}>
                    <IconButton onClick={() => void updateMemory(memory, { isFavorite: !memory.isFavorite })}>
                      {memory.isFavorite ? <StarOutlinedIcon color="warning" /> : <StarBorderOutlinedIcon />}
                    </IconButton>
                  </Tooltip>
                  <Tooltip title={memory.isActive ? 'Disable' : 'Enable'}>
                    <Switch checked={memory.isActive} onChange={() => void updateMemory(memory, { isActive: !memory.isActive })} />
                  </Tooltip>
                  <Tooltip title="Edit">
                    <IconButton onClick={() => openEdit(memory)}><EditOutlinedIcon /></IconButton>
                  </Tooltip>
                  <Tooltip title="Delete">
                    <IconButton color="error" onClick={() => void deleteMemory(memory)}><DeleteOutlineOutlinedIcon /></IconButton>
                  </Tooltip>
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} fullWidth maxWidth="sm">
        <Box component="form" onSubmit={saveMemory}>
          <DialogTitle>{editing ? 'Edit Memory' : 'Add Memory'}</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <FormControl fullWidth>
                <InputLabel>Category</InputLabel>
                <Select label="Category" value={form.category} onChange={(event) => setForm((current) => ({ ...current, category: event.target.value }))}>
                  {categories.map((item) => (
                    <MenuItem key={item.categoryId} value={item.categoryName}>{item.categoryName}</MenuItem>
                  ))}
                </Select>
              </FormControl>
              <TextField required label="Key" value={form.key} onChange={(event) => setForm((current) => ({ ...current, key: event.target.value }))} />
              <TextField required multiline minRows={4} label="Value" value={form.value} onChange={(event) => setForm((current) => ({ ...current, value: event.target.value }))} />
              <Stack direction="row" spacing={2}>
                <Chip clickable color={form.isPinned ? 'primary' : 'default'} label="Pinned" onClick={() => setForm((current) => ({ ...current, isPinned: !current.isPinned }))} />
                <Chip clickable color={form.isFavorite ? 'warning' : 'default'} label="Favorite" onClick={() => setForm((current) => ({ ...current, isFavorite: !current.isFavorite }))} />
              </Stack>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained">Save</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  );
}
