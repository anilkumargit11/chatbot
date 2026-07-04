import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Dialog,
  DialogContent,
  Box,
  InputBase,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Typography,
  Divider,
  Paper
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import ChatOutlinedIcon from '@mui/icons-material/ChatOutlined';
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined';
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined';
import ArticleOutlinedIcon from '@mui/icons-material/ArticleOutlined';
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined';
import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined';
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined';
import { useThemeMode } from '../../contexts/ThemeModeContext';

interface CommandPaletteProps {
  open: boolean;
  onClose: () => void;
  onSwitchMode?: (mode: string) => void;
}

export function CommandPalette({ open, onClose, onSwitchMode }: CommandPaletteProps) {
  const navigate = useNavigate();
  const { mode, toggleMode } = useThemeMode();
  const [search, setSearch] = useState('');

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  const items = [
    { label: 'Go to Dashboard', category: 'Navigation', icon: DashboardOutlinedIcon, action: () => navigate('/') },
    { label: 'Go to Chat Assistant', category: 'Navigation', icon: ChatOutlinedIcon, action: () => navigate('/chat') },
    { label: 'Upload Documents', category: 'Navigation', icon: UploadFileOutlinedIcon, action: () => navigate('/documents') },
    { label: 'Browse Knowledge Base', category: 'Navigation', icon: ArticleOutlinedIcon, action: () => navigate('/knowledge-base') },
    
    { label: 'Toggle Light/Dark Theme', category: 'Actions', icon: mode === 'dark' ? LightModeOutlinedIcon : DarkModeOutlinedIcon, action: () => { toggleMode(); onClose(); } },
    { label: 'System Settings', category: 'Actions', icon: SettingsOutlinedIcon, action: () => navigate('/settings') },

    { label: 'Switch to Normal Chat Mode', category: 'AI Modes', icon: ChatOutlinedIcon, action: () => { if (onSwitchMode) onSwitchMode('Normal'); onClose(); } },
    { label: 'Switch to Enterprise Search Mode', category: 'AI Modes', icon: SearchIcon, action: () => { if (onSwitchMode) onSwitchMode('Enterprise'); onClose(); } },
    { label: 'Switch to SQL Assistant Mode', category: 'AI Modes', icon: SearchIcon, action: () => { if (onSwitchMode) onSwitchMode('Database'); onClose(); } }
  ];

  const filtered = items.filter(item =>
    item.label.toLowerCase().includes(search.toLowerCase()) ||
    item.category.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      PaperProps={{
        sx: {
          borderRadius: 4,
          overflow: 'hidden',
          border: '1px solid rgba(255, 255, 255, 0.08)',
          boxShadow: '0 24px 60px rgba(0,0,0,0.4)',
          backgroundColor: mode === 'dark' ? 'rgba(15, 20, 32, 0.85)' : 'rgba(255, 255, 255, 0.9)',
          backdropFilter: 'blur(16px)'
        }
      }}
    >
      <DialogContent sx={{ p: 0 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', p: 2, borderBottom: '1px solid', borderColor: 'divider' }}>
          <SearchIcon sx={{ color: 'text.secondary', mr: 1.5 }} />
          <InputBase
            placeholder="Type a command or search..."
            fullWidth
            autoFocus
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            sx={{ fontSize: '1.05rem', color: 'text.primary' }}
          />
          <Typography variant="caption" sx={{ px: 1, py: 0.5, border: '1px solid', borderColor: 'divider', borderRadius: 1, color: 'text.secondary' }}>
            ESC
          </Typography>
        </Box>

        <Box sx={{ maxHeight: 350, overflowY: 'auto', py: 1 }}>
          {filtered.length === 0 ? (
            <Typography variant="body2" sx={{ p: 3, textAlign: 'center', color: 'text.secondary' }}>
              No commands matching search query.
            </Typography>
          ) : (
            ['Navigation', 'AI Modes', 'Actions'].map(cat => {
              const catItems = filtered.filter(i => i.category === cat);
              if (catItems.length === 0) return null;

              return (
                <Box key={cat}>
                  <Typography variant="caption" sx={{ px: 2.5, py: 1, display: 'block', fontWeight: 700, color: 'primary.main', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
                    {cat}
                  </Typography>
                  <List sx={{ px: 1, py: 0 }}>
                    {catItems.map((item, idx) => {
                      const Icon = item.icon;
                      return (
                        <ListItemButton
                          key={idx}
                          onClick={() => { item.action(); onClose(); }}
                          sx={{ borderRadius: 2, mb: 0.25, py: 1, px: 1.5 }}
                        >
                          <ListItemIcon sx={{ minWidth: 36, color: 'text.secondary' }}>
                            <Icon fontSize="small" />
                          </ListItemIcon>
                          <ListItemText
                            primary={item.label}
                            primaryTypographyProps={{ sx: { fontSize: '0.925rem', fontWeight: 500 } }}
                          />
                        </ListItemButton>
                      );
                    })}
                  </List>
                  <Divider sx={{ my: 1 }} />
                </Box>
              );
            })
          )}
        </Box>
      </DialogContent>
    </Dialog>
  );
}
