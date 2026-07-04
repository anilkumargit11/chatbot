import AddCircleOutlineOutlinedIcon from '@mui/icons-material/AddCircleOutlineOutlined';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import FileDownloadOutlinedIcon from '@mui/icons-material/FileDownloadOutlined';
import MoreHorizOutlinedIcon from '@mui/icons-material/MoreHorizOutlined';
import RuleOutlinedIcon from '@mui/icons-material/RuleOutlined';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import {
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControlLabel,
  Grid,
  IconButton,
  InputAdornment,
  Menu,
  MenuItem,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TableSortLabel,
  TextField,
  Tooltip,
  Typography
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { toast } from 'react-toastify';
import { AdminRole, Permission, SaveRoleRequest } from '../models/api';
import { rolesApi } from '../services/adminApi';

type SortKey = 'roleName' | 'description' | 'isSystemRole' | 'isActive' | 'createdDate';
type SortDirection = 'asc' | 'desc';

const emptyRole: SaveRoleRequest = {
  roleName: '',
  description: '',
  isActive: true
};

function formatDate(value?: string) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}

function isInDateRange(value: string | undefined, fromDate: string, toDate: string) {
  if (!value || (!fromDate && !toDate)) {
    return true;
  }

  const date = new Date(value);
  if (fromDate && date < new Date(`${fromDate}T00:00:00`)) {
    return false;
  }

  if (toDate && date > new Date(`${toDate}T23:59:59`)) {
    return false;
  }

  return true;
}

function downloadCsv(fileName: string, rows: AdminRole[]) {
  const headers = ['Role Name', 'Description', 'Is System Role', 'Status', 'Created Date'];
  const csvRows = rows.map((role) => [
    role.roleName,
    role.description,
    role.isSystemRole ? 'Yes' : 'No',
    role.isActive ? 'Active' : 'Inactive',
    formatDate(role.createdDate)
  ]);

  const csv = [headers, ...csvRows]
    .map((row) => row.map((cell) => `"${String(cell ?? '').replaceAll('"', '""')}"`).join(','))
    .join('\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(link.href);
}

export function RolesPage() {
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [sortKey, setSortKey] = useState<SortKey>('createdDate');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<AdminRole | null>(null);
  const [viewRole, setViewRole] = useState<AdminRole | null>(null);
  const [form, setForm] = useState<SaveRoleRequest>(emptyRole);
  const [permissionRole, setPermissionRole] = useState<AdminRole | null>(null);
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const [selectedRole, setSelectedRole] = useState<AdminRole | null>(null);
  const [loading, setLoading] = useState(false);

  async function loadRoles() {
    setLoading(true);
    try {
      setRoles(await rolesApi.list());
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadRoles().catch((err) => toast.error(err instanceof Error ? err.message : 'Unable to load roles.'));
  }, []);

  const filteredRoles = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    const result = roles.filter((role) => {
      const matchesSearch = !normalizedSearch
        || role.roleName.toLowerCase().includes(normalizedSearch)
        || role.description.toLowerCase().includes(normalizedSearch);
      return matchesSearch && isInDateRange(role.createdDate, fromDate, toDate);
    });

    return [...result].sort((left, right) => {
      const comparison = String(left[sortKey] ?? '').localeCompare(String(right[sortKey] ?? ''), undefined, { numeric: true, sensitivity: 'base' });
      return sortDirection === 'asc' ? comparison : -comparison;
    });
  }, [fromDate, roles, search, sortDirection, sortKey, toDate]);

  const pagedRoles = filteredRoles.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);

  function requestSort(key: SortKey) {
    if (sortKey === key) {
      setSortDirection((current) => current === 'asc' ? 'desc' : 'asc');
    } else {
      setSortKey(key);
      setSortDirection('asc');
    }
  }

  function openCreate() {
    setEditingRole(null);
    setForm(emptyRole);
    setDialogOpen(true);
  }

  function openEdit(role: AdminRole) {
    setEditingRole(role);
    setForm({
      roleName: role.roleName,
      description: role.description,
      isActive: role.isActive
    });
    setDialogOpen(true);
  }

  function openActionMenu(event: React.MouseEvent<HTMLElement>, role: AdminRole) {
    setAnchorEl(event.currentTarget);
    setSelectedRole(role);
  }

  function closeActionMenu() {
    setAnchorEl(null);
    setSelectedRole(null);
  }

  async function saveRole() {
    if (!form.roleName.trim()) {
      toast.error('Role name is required');
      return;
    }

    if (editingRole) {
      await rolesApi.update(editingRole.id, form);
      toast.success('Role updated');
    } else {
      await rolesApi.create(form);
      toast.success('Role created');
    }

    setDialogOpen(false);
    await loadRoles();
  }

  async function deleteRole(role: AdminRole) {
    closeActionMenu();
    if (role.isSystemRole) {
      toast.error('System roles cannot be deleted');
      return;
    }

    if (!window.confirm(`Delete role ${role.roleName}?`)) {
      return;
    }

    await rolesApi.remove(role.id);
    toast.success('Role deleted');
    await loadRoles();
  }

  async function openPermissions(role: AdminRole) {
    closeActionMenu();
    setPermissionRole(role);
    setPermissions(await rolesApi.permissions(role.id));
  }

  async function savePermissions() {
    if (!permissionRole) {
      return;
    }

    await rolesApi.assignPermissions(permissionRole.id, permissions.filter((permission) => permission.isAssigned).map((permission) => permission.id));
    toast.success('Permissions updated');
    setPermissionRole(null);
  }

  return (
    <Box>
      <Paper square elevation={0} sx={{ borderBottom: 1, borderColor: 'divider', mx: { xs: -2, sm: -3, xl: -5 }, px: { xs: 2, sm: 3, xl: 5 }, py: 2 }}>
        <Stack alignItems="center" direction="row" justifyContent="space-between" spacing={2}>
          <Stack alignItems="center" direction="row" spacing={1.5}>
            <IconButton aria-label="Back" onClick={() => window.history.back()}>
              <ArrowBackOutlinedIcon />
            </IconButton>
            <Typography fontWeight={800} variant="h5">Roles</Typography>
          </Stack>
          <Button color="warning" onClick={openCreate} startIcon={<AddCircleOutlineOutlinedIcon />} variant="contained">
            Add Role
          </Button>
        </Stack>
      </Paper>

      <Paper square elevation={0} sx={{ borderBottom: 1, borderColor: 'divider', mx: { xs: -2, sm: -3, xl: -5 }, px: { xs: 2, sm: 3, xl: 5 }, py: 2.5 }}>
        <Grid alignItems="center" container spacing={2}>
          <Grid size={{ xs: 12, md: 5 }}>
            <Typography variant="body2">Search</Typography>
            <TextField
              fullWidth
              InputProps={{ endAdornment: <InputAdornment position="end"><SearchOutlinedIcon /></InputAdornment> }}
              onChange={(event) => { setSearch(event.target.value); setPage(0); }}
              placeholder="Enter keywords"
              value={search}
            />
            <Typography color="text.secondary" sx={{ mt: 0.5 }} variant="caption">Search by role name.</Typography>
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 2 }}>
            <Typography variant="body2">From Date</Typography>
            <TextField fullWidth onChange={(event) => { setFromDate(event.target.value); setPage(0); }} type="date" value={fromDate} />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 2 }}>
            <Typography variant="body2">To Date</Typography>
            <TextField fullWidth onChange={(event) => { setToDate(event.target.value); setPage(0); }} type="date" value={toDate} />
          </Grid>
          <Grid size={{ xs: 12, md: 3 }}>
            <Stack direction="row" spacing={1}>
              <Button fullWidth onClick={() => setPage(0)} sx={{ minHeight: 40 }} variant="contained">Submit</Button>
              <Tooltip title="Export Excel">
                <IconButton onClick={() => downloadCsv('roles.csv', filteredRoles)}><FileDownloadOutlinedIcon /></IconButton>
              </Tooltip>
            </Stack>
          </Grid>
        </Grid>
      </Paper>

      <Paper square elevation={0} sx={{ mt: 2, overflow: 'hidden' }}>
        <TableContainer>
          <Table sx={{ minWidth: 960 }}>
            <TableHead>
              <TableRow>
                {[
                  ['roleName', 'Role Name'],
                  ['description', 'Description'],
                  ['isSystemRole', 'Is System Role'],
                  ['isActive', 'Status'],
                  ['createdDate', 'Created Date']
                ].map(([key, label]) => (
                  <TableCell key={key}>
                    <TableSortLabel active={sortKey === key} direction={sortKey === key ? sortDirection : 'asc'} onClick={() => requestSort(key as SortKey)}>
                      {label}
                    </TableSortLabel>
                  </TableCell>
                ))}
                <TableCell align="center">Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {pagedRoles.map((role) => (
                <TableRow hover key={role.id}>
                  <TableCell><Typography fontWeight={800}>{role.roleName}</Typography></TableCell>
                  <TableCell>{role.description || '-'}</TableCell>
                  <TableCell><Chip label={role.isSystemRole ? 'Yes' : 'No'} size="small" variant="outlined" /></TableCell>
                  <TableCell><Chip color={role.isActive ? 'success' : 'default'} label={role.isActive ? 'Active' : 'Inactive'} size="small" variant="outlined" /></TableCell>
                  <TableCell>{formatDate(role.createdDate)}</TableCell>
                  <TableCell align="center">
                    <IconButton aria-label="Role actions" onClick={(event) => openActionMenu(event, role)} sx={{ bgcolor: 'background.paper', boxShadow: 1 }}>
                      <MoreHorizOutlinedIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
              {!loading && pagedRoles.length === 0 && (
                <TableRow>
                  <TableCell align="center" colSpan={6}>No roles found.</TableCell>
                </TableRow>
              )}
              {loading && (
                <TableRow>
                  <TableCell align="center" colSpan={6}>Loading roles...</TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          component="div"
          count={filteredRoles.length}
          onPageChange={(_, nextPage) => setPage(nextPage)}
          onRowsPerPageChange={(event) => { setRowsPerPage(Number(event.target.value)); setPage(0); }}
          page={page}
          rowsPerPage={rowsPerPage}
          rowsPerPageOptions={[5, 10, 25, 50]}
        />
      </Paper>

      <Menu anchorEl={anchorEl} onClose={closeActionMenu} open={!!anchorEl}>
        <MenuItem onClick={() => { if (selectedRole) setViewRole(selectedRole); closeActionMenu(); }}><VisibilityOutlinedIcon fontSize="small" sx={{ mr: 1 }} />View</MenuItem>
        <MenuItem onClick={() => { if (selectedRole) openEdit(selectedRole); closeActionMenu(); }}><EditOutlinedIcon fontSize="small" sx={{ mr: 1 }} />Edit</MenuItem>
        <MenuItem onClick={() => selectedRole && void openPermissions(selectedRole)}><RuleOutlinedIcon fontSize="small" sx={{ mr: 1 }} />Assign Permissions</MenuItem>
        <Divider />
        <MenuItem disabled={selectedRole?.isSystemRole} onClick={() => selectedRole && void deleteRole(selectedRole)} sx={{ color: 'error.main' }}>
          <DeleteOutlineOutlinedIcon fontSize="small" sx={{ mr: 1 }} />Delete
        </MenuItem>
      </Menu>

      <Dialog fullWidth maxWidth="sm" onClose={() => setDialogOpen(false)} open={dialogOpen}>
        <DialogTitle>{editingRole ? 'Edit Role' : 'Add Role'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField label="Role Name" onChange={(event) => setForm({ ...form, roleName: event.target.value })} required value={form.roleName} />
            <TextField label="Description" multiline minRows={3} onChange={(event) => setForm({ ...form, description: event.target.value })} value={form.description} />
            <FormControlLabel control={<Switch checked={!!editingRole?.isSystemRole} disabled />} label="System Role" />
            <FormControlLabel control={<Switch checked={form.isActive} onChange={(event) => setForm({ ...form, isActive: event.target.checked })} />} label="Active" />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button onClick={() => void saveRole()} variant="contained">Save</Button>
        </DialogActions>
      </Dialog>

      <Dialog fullWidth maxWidth="sm" onClose={() => setViewRole(null)} open={!!viewRole}>
        <DialogTitle>Role Details</DialogTitle>
        <DialogContent>
          <Stack spacing={1.5} sx={{ mt: 1 }}>
            <Typography><strong>Role Name:</strong> {viewRole?.roleName}</Typography>
            <Typography><strong>Description:</strong> {viewRole?.description || '-'}</Typography>
            <Typography><strong>System Role:</strong> {viewRole?.isSystemRole ? 'Yes' : 'No'}</Typography>
            <Typography><strong>Status:</strong> {viewRole?.isActive ? 'Active' : 'Inactive'}</Typography>
            <Typography><strong>Created Date:</strong> {formatDate(viewRole?.createdDate)}</Typography>
          </Stack>
        </DialogContent>
        <DialogActions><Button onClick={() => setViewRole(null)}>Close</Button></DialogActions>
      </Dialog>

      <Dialog fullWidth maxWidth="sm" onClose={() => setPermissionRole(null)} open={!!permissionRole}>
        <DialogTitle>Assign Permissions - {permissionRole?.roleName}</DialogTitle>
        <DialogContent>
          <Stack spacing={1} sx={{ mt: 1 }}>
            {permissions.map((permission) => (
              <FormControlLabel
                control={
                  <Checkbox
                    checked={permission.isAssigned}
                    onChange={(event) =>
                      setPermissions((current) =>
                        current.map((item) => item.id === permission.id ? { ...item, isAssigned: event.target.checked } : item)
                      )
                    }
                  />
                }
                key={permission.id}
                label={
                  <Box>
                    <Typography fontWeight={700}>{permission.permissionName}</Typography>
                    <Typography color="text.secondary" variant="caption">{permission.description}</Typography>
                  </Box>
                }
              />
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPermissionRole(null)}>Cancel</Button>
          <Button onClick={() => void savePermissions()} variant="contained">Save Permissions</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
