import ArticleOutlinedIcon from '@mui/icons-material/ArticleOutlined';
import ChatOutlinedIcon from '@mui/icons-material/ChatOutlined';
import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined';
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined';
import HistoryOutlinedIcon from '@mui/icons-material/HistoryOutlined';
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined';
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined';
import MenuIcon from '@mui/icons-material/Menu';
import PsychologyOutlinedIcon from '@mui/icons-material/PsychologyOutlined';
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined';
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined';
import AdminPanelSettingsOutlinedIcon from '@mui/icons-material/AdminPanelSettingsOutlined';
import GroupOutlinedIcon from '@mui/icons-material/GroupOutlined';
import ExpandLessOutlinedIcon from '@mui/icons-material/ExpandLessOutlined';
import ExpandMoreOutlinedIcon from '@mui/icons-material/ExpandMoreOutlined';
import PushPinOutlinedIcon from '@mui/icons-material/PushPinOutlined';
import FolderOpenOutlinedIcon from '@mui/icons-material/FolderOpenOutlined';
import SearchIcon from '@mui/icons-material/Search';
import KeyboardIcon from '@mui/icons-material/Keyboard';
import MemoryOutlinedIcon from '@mui/icons-material/MemoryOutlined';
import TravelExploreOutlinedIcon from '@mui/icons-material/TravelExploreOutlined';
import {
  AppBar,
  Avatar,
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
  Button
} from '@mui/material';
import { useState, useEffect } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { env } from '../../config/env';
import { useAuth } from '../../contexts/AuthContext';
import { useThemeMode } from '../../contexts/ThemeModeContext';
import { CommandPalette } from '../common/CommandPalette';

const drawerWidth = 290;

const navigation = [
  { label: 'Dashboard', path: '/', icon: DashboardOutlinedIcon, permission: 'Dashboard.View' },
  { label: 'Chat Assistant', path: '/chat', icon: ChatOutlinedIcon, permission: 'ChatAssistant.View' },
  { label: 'Document Upload', path: '/documents', icon: UploadFileOutlinedIcon, permission: 'Document.Upload' },
  { label: 'Knowledge Base', path: '/knowledge-base', icon: ArticleOutlinedIcon, permission: 'KnowledgeBase.View' },
  { label: 'AI Memory', path: '/memory', icon: MemoryOutlinedIcon, permission: 'ChatAssistant.View' },
  { label: 'Enterprise RAG', path: '/enterprise-rag', icon: TravelExploreOutlinedIcon, permission: 'KnowledgeBase.View' },
  { label: 'Chat History', path: '/history', icon: HistoryOutlinedIcon, permission: 'ChatHistory.View' }
];

const settingsNavigation = [
  { label: 'Users', path: '/users', icon: GroupOutlinedIcon, permission: 'Users.View' },
  { label: 'Roles', path: '/roles', icon: AdminPanelSettingsOutlinedIcon, permission: 'Roles.View' },
  { label: 'Settings', path: '/settings', icon: SettingsOutlinedIcon, permission: 'Settings.View' }
];

export function AppLayout() {
  const theme = useTheme();
  const location = useLocation();
  const isDesktop = useMediaQuery(theme.breakpoints.up('lg'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(() => 
    location.pathname.startsWith('/users') || 
    location.pathname.startsWith('/roles') || 
    location.pathname.startsWith('/settings')
  );
  const [commandPaletteOpen, setCommandPaletteOpen] = useState(false);
  const { mode, toggleMode } = useThemeMode();
  const { auth, hasPermission, logout } = useAuth();
  
  const visibleNavigation = navigation.filter((item) => hasPermission(item.permission));
  const visibleSettingsNavigation = settingsNavigation.filter((item) => hasPermission(item.permission));

  // Listen for Ctrl + K shortcut
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        setCommandPaletteOpen(prev => !prev);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const drawer = (
    <Stack sx={{ height: '100%', bgcolor: mode === 'dark' ? '#080c14' : '#fcfdfe' }}>
      {/* Brand Header */}
      <Stack alignItems="center" direction="row" spacing={1.5} sx={{ px: 3, py: 3 }}>
        <Avatar 
          sx={{ 
            bgcolor: 'primary.main', 
            width: 42, 
            height: 42,
            boxShadow: '0 8px 16px rgba(99, 102, 241, 0.25)',
            background: 'linear-gradient(135deg, #6366f1 0%, #0ea5e9 100%)'
          }}
        >
          <PsychologyOutlinedIcon sx={{ fontSize: 24, color: '#ffffff' }} />
        </Avatar>
        <Box>
          <Typography fontWeight={900} variant="subtitle1" sx={{ letterSpacing: '-0.02em', lineHeight: 1.2 }}>
            Copilot Core
          </Typography>
          <Typography color="text.secondary" variant="caption" sx={{ fontWeight: 600, letterSpacing: '0.02em', textTransform: 'uppercase', opacity: 0.8 }}>
            Enterprise AI
          </Typography>
        </Box>
      </Stack>

      <Divider sx={{ opacity: 0.5 }} />

      {/* Main Navigation */}
      <List sx={{ px: 2, py: 2 }}>
        {visibleNavigation.map((item) => {
          const selected = item.path === '/' ? location.pathname === '/' : location.pathname.startsWith(item.path);
          const Icon = item.icon;

          return (
            <ListItemButton
              component={NavLink}
              key={item.path}
              onClick={() => setMobileOpen(false)}
              selected={selected}
              sx={{ 
                borderRadius: 3, 
                mb: 0.75,
                py: 1.2,
                px: 2,
                color: selected ? 'primary.main' : 'text.secondary',
                bgcolor: selected ? (mode === 'dark' ? 'rgba(99, 102, 241, 0.08)' : 'rgba(79, 70, 229, 0.04)') : 'transparent',
                '&:hover': {
                  bgcolor: mode === 'dark' ? 'rgba(255, 255, 255, 0.02)' : 'rgba(0, 0, 0, 0.01)'
                }
              }}
              to={item.path}
            >
              <ListItemIcon sx={{ minWidth: 38, color: selected ? 'primary.main' : 'text.secondary' }}>
                <Icon sx={{ fontSize: 22 }} />
              </ListItemIcon>
              <ListItemText 
                primary={item.label} 
                primaryTypographyProps={{ sx: { fontWeight: selected ? 700 : 500, fontSize: '0.925rem' } }} 
              />
            </ListItemButton>
          );
        })}

        {visibleSettingsNavigation.length > 0 && (
          <>
            <ListItemButton
              onClick={() => setSettingsOpen((current) => !current)}
              selected={location.pathname.startsWith('/settings') || location.pathname.startsWith('/users') || location.pathname.startsWith('/roles')}
              sx={{ 
                borderRadius: 3, 
                mb: 0.75,
                py: 1.2,
                px: 2,
                color: 'text.secondary'
              }}
            >
              <ListItemIcon sx={{ minWidth: 38, color: 'text.secondary' }}>
                <SettingsOutlinedIcon sx={{ fontSize: 22 }} />
              </ListItemIcon>
              <ListItemText 
                primary="Management" 
                primaryTypographyProps={{ sx: { fontWeight: 500, fontSize: '0.925rem' } }} 
              />
              {settingsOpen ? <ExpandLessOutlinedIcon /> : <ExpandMoreOutlinedIcon />}
            </ListItemButton>

            {settingsOpen && visibleSettingsNavigation.map((item) => {
              const selected = location.pathname.startsWith(item.path);
              const Icon = item.icon;

              return (
                <ListItemButton
                  component={NavLink}
                  key={item.path}
                  onClick={() => setMobileOpen(false)}
                  selected={selected}
                  sx={{ 
                    borderRadius: 3, 
                    mb: 0.5, 
                    pl: 5,
                    py: 1,
                    color: selected ? 'primary.main' : 'text.secondary',
                    bgcolor: selected ? (mode === 'dark' ? 'rgba(99, 102, 241, 0.08)' : 'rgba(79, 70, 229, 0.04)') : 'transparent'
                  }}
                  to={item.path}
                >
                  <ListItemIcon sx={{ minWidth: 32, color: selected ? 'primary.main' : 'text.secondary' }}>
                    <Icon sx={{ fontSize: 18 }} />
                  </ListItemIcon>
                  <ListItemText primary={item.label} primaryTypographyProps={{ sx: { fontSize: '0.85rem', fontWeight: selected ? 700 : 500 } }} />
                </ListItemButton>
              );
            })}
          </>
        )}
      </List>

      <Divider sx={{ opacity: 0.5 }} />

      {/* Pinned & Folders Mock Segment */}
      <Box sx={{ px: 3, py: 2, flexGrow: 1, overflowY: 'auto' }}>
        <Stack direction="row" alignItems="center" spacing={1} sx={{ color: 'text.secondary', mb: 1.5 }}>
          <PushPinOutlinedIcon sx={{ fontSize: 16 }} />
          <Typography variant="caption" sx={{ fontWeight: 700, letterSpacing: '0.05em', textTransform: 'uppercase' }}>
            Pinned Chats
          </Typography>
        </Stack>
        <List sx={{ p: 0, mb: 3 }}>
          <ListItemButton component={NavLink} to="/chat?session=clone-brd" sx={{ borderRadius: 2, py: 0.5, px: 1, mb: 0.5 }}>
            <ListItemText primary="BRD-9810 Enhancement Audit" primaryTypographyProps={{ variant: 'caption', noWrap: true, sx: { color: 'text.secondary', fontWeight: 500 } }} />
          </ListItemButton>
          <ListItemButton component={NavLink} to="/chat?session=medical-ocr" sx={{ borderRadius: 2, py: 0.5, px: 1, mb: 0.5 }}>
            <ListItemText primary="Amlodipine Medicine Review" primaryTypographyProps={{ variant: 'caption', noWrap: true, sx: { color: 'text.secondary', fontWeight: 500 } }} />
          </ListItemButton>
        </List>

        <Stack direction="row" alignItems="center" spacing={1} sx={{ color: 'text.secondary', mb: 1.5 }}>
          <FolderOpenOutlinedIcon sx={{ fontSize: 16 }} />
          <Typography variant="caption" sx={{ fontWeight: 700, letterSpacing: '0.05em', textTransform: 'uppercase' }}>
            Folders
          </Typography>
        </Stack>
        <List sx={{ p: 0 }}>
          <ListItemButton sx={{ borderRadius: 2, py: 0.5, px: 1, mb: 0.5 }}>
            <ListItemText primary="📁 Financial Analysis 2026" primaryTypographyProps={{ variant: 'caption', noWrap: true, sx: { color: 'text.secondary', fontWeight: 500 } }} />
          </ListItemButton>
          <ListItemButton sx={{ borderRadius: 2, py: 0.5, px: 1, mb: 0.5 }}>
            <ListItemText primary="📁 Product Specifications" primaryTypographyProps={{ variant: 'caption', noWrap: true, sx: { color: 'text.secondary', fontWeight: 500 } }} />
          </ListItemButton>
        </List>
      </Box>

      <Divider sx={{ opacity: 0.5 }} />

      {/* Profile & Logout Section */}
      <Box sx={{ p: 2 }}>
        <Stack direction="row" alignItems="center" spacing={2} sx={{ p: 1.5, borderRadius: 3, border: '1px solid', borderColor: 'divider', bgcolor: mode === 'dark' ? 'rgba(255,255,255,0.01)' : 'rgba(0,0,0,0.01)' }}>
          <Avatar sx={{ width: 36, height: 36, bgcolor: 'primary.main', fontWeight: 700 }}>
            {(auth?.user.fullName || auth?.user.email || 'AK').slice(0, 2).toUpperCase()}
          </Avatar>
          <Box sx={{ flexGrow: 1, overflow: 'hidden' }}>
            <Typography variant="body2" noWrap sx={{ fontWeight: 700 }}>
              {auth?.user.fullName || 'User Account'}
            </Typography>
            <Typography variant="caption" noWrap sx={{ color: 'text.secondary', display: 'block' }}>
              {auth?.user.email || 'offline@domain.com'}
            </Typography>
          </Box>
          <Tooltip title="Log out">
            <IconButton size="small" onClick={() => void logout()}>
              <LogoutOutlinedIcon sx={{ fontSize: 18 }} />
            </IconButton>
          </Tooltip>
        </Stack>
      </Box>
    </Stack>
  );

  return (
    <Box
      sx={{
        minHeight: '100vh',
        height: '100vh',
        display: 'flex',
        overflow: 'hidden',
        bgcolor: 'background.default'
      }}
    >
      {/* Desktop Sidebar */}
      {isDesktop && (
        <Box
          component="aside"
          sx={{
            width: drawerWidth,
            minWidth: drawerWidth,
            maxWidth: drawerWidth,
            height: '100vh',
            flex: `0 0 ${drawerWidth}px`,
            borderRight: '1px solid',
            borderColor: 'divider',
            overflow: 'hidden',
            zIndex: (theme) => theme.zIndex.appBar
          }}
        >
          {drawer}
        </Box>
      )}

      {/* Mobile Sidebar */}
      <Drawer
        ModalProps={{ keepMounted: true }}
        onClose={() => setMobileOpen(false)}
        open={mobileOpen}
        sx={{
          display: { xs: 'block', lg: 'none' },
          '& .MuiDrawer-paper': {
            width: { xs: 'min(86vw, 290px)', sm: drawerWidth },
            maxWidth: '100vw',
            borderRight: '1px solid',
            borderColor: 'divider',
            boxSizing: 'border-box'
          }
        }}
        variant="temporary"
      >
        {drawer}
      </Drawer>

      {/* Content Shell */}
      <Box
        sx={{
          minWidth: 0,
          flex: 1,
          height: '100vh',
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden'
        }}
      >
        {/* Top Header */}
        <AppBar
          color="inherit"
          elevation={0}
          position="static"
          sx={{
            flexShrink: 0,
            borderBottom: '1px solid',
            borderColor: 'divider',
            backgroundColor: mode === 'dark' ? 'rgba(7, 10, 19, 0.88)' : 'rgba(255, 255, 255, 0.92)',
            backdropFilter: 'blur(16px)',
            zIndex: (theme) => theme.zIndex.appBar
          }}
        >
          <Toolbar sx={{ justifyContent: 'space-between', gap: 2, px: { xs: 2, sm: 3 }, minWidth: 0 }}>
            <Stack direction="row" alignItems="center" spacing={1} sx={{ minWidth: 0 }}>
              {!isDesktop && (
                <IconButton aria-label="Open navigation" edge="start" onClick={() => setMobileOpen(true)} sx={{ mr: 1 }}>
                  <MenuIcon />
                </IconButton>
              )}
              <Typography variant="subtitle1" fontWeight={700} noWrap sx={{ display: { xs: 'none', sm: 'block' } }}>
                {env.appName}
              </Typography>
            </Stack>

            {/* Quick Search Shortcut & Controls */}
            <Stack direction="row" alignItems="center" spacing={{ xs: 1, sm: 2 }} sx={{ flexShrink: 0 }}>
              <Button
                variant="outlined"
                color="inherit"
                size="small"
                onClick={() => setCommandPaletteOpen(true)}
                startIcon={<SearchIcon sx={{ fontSize: 16 }} />}
                sx={{
                  borderRadius: 2,
                  borderColor: 'divider',
                  color: 'text.secondary',
                  fontSize: '0.8rem',
                  py: 0.5,
                  px: { xs: 1.25, sm: 2 },
                  minWidth: { xs: 42, sm: 132 },
                  '& .MuiButton-startIcon': { mr: { xs: 0, sm: 1 } },
                  '&:hover': {
                    borderColor: 'primary.main',
                    bgcolor: 'transparent'
                  }
                }}
              >
                <Box component="span" sx={{ display: { xs: 'none', sm: 'inline' } }}>
                  Search...
                </Box>
                <Box component="span" sx={{ ml: 2, px: 0.75, py: 0.25, border: '1px solid', borderColor: 'divider', borderRadius: 1, fontSize: '0.7rem', display: { xs: 'none', md: 'flex' }, alignItems: 'center' }}>
                  Ctrl + K
                </Box>
              </Button>

              <Tooltip title={`Toggle Theme`}>
                <IconButton aria-label="Toggle theme" onClick={toggleMode} size="small" sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
                  {mode === 'light' ? <DarkModeOutlinedIcon sx={{ fontSize: 20 }} /> : <LightModeOutlinedIcon sx={{ fontSize: 20 }} />}
                </IconButton>
              </Tooltip>
            </Stack>
          </Toolbar>
        </AppBar>

        {/* Main Viewport Container */}
        <Box
          component="main"
          sx={{
            minWidth: 0,
            flex: 1,
            overflow: 'auto',
            px: { xs: 2, sm: 3, xl: 4 },
            py: { xs: 2, sm: 3 },
            bgcolor: 'background.default'
          }}
        >
          <Outlet />
        </Box>
      </Box>

      {/* Global Command Palette */}
      <CommandPalette 
        open={commandPaletteOpen} 
        onClose={() => setCommandPaletteOpen(false)} 
      />
    </Box>
  );
}
