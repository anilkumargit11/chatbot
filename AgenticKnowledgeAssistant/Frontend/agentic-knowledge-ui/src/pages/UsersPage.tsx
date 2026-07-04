import AddCircleOutlineOutlinedIcon from '@mui/icons-material/AddCircleOutlineOutlined';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import FileDownloadOutlinedIcon from '@mui/icons-material/FileDownloadOutlined';
import LockResetOutlinedIcon from '@mui/icons-material/LockResetOutlined';
import MoreHorizOutlinedIcon from '@mui/icons-material/MoreHorizOutlined';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import ToggleOffOutlinedIcon from '@mui/icons-material/ToggleOffOutlined';
import ToggleOnOutlinedIcon from '@mui/icons-material/ToggleOnOutlined';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  FormControlLabel,
  Grid,
  IconButton,
  InputAdornment,
  InputLabel,
  Menu,
  MenuItem,
  Paper,
  Select,
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
import { AdminRole, AdminUser, SaveUserRequest } from '../models/api';
import { rolesApi, usersApi } from '../services/adminApi';

type SortKey = 'userName' | 'fullName' | 'email' | 'mobileNumber' | 'roleName' | 'isActive' | 'createdDate' | 'lastLoginDate';
type SortDirection = 'asc' | 'desc';
type UserFormErrors = Partial<Record<keyof SaveUserRequest | 'form', string>>;

const emptyForm: SaveUserRequest = {
  userName: '',
  fullName: '',
  email: '',
  mobileNumber: '',
  password: '',
  confirmPassword: '',
  roleId: 0,
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

function downloadCsv(fileName: string, rows: AdminUser[]) {
  const headers = ['User Name', 'Full Name', 'Email', 'Mobile Number', 'Role', 'Status', 'Created Date', 'Last Login'];
  const csvRows = rows.map((user) => [
    user.userName,
    user.fullName,
    user.email,
    user.mobileNumber,
    user.roleName,
    user.isActive ? 'Active' : 'Inactive',
    formatDate(user.createdDate),
    formatDate(user.lastLoginDate)
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

export function UsersPage() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [roleFilter, setRoleFilter] = useState<number | ''>('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [sortKey, setSortKey] = useState<SortKey>('createdDate');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<AdminUser | null>(null);
  const [viewUser, setViewUser] = useState<AdminUser | null>(null);
  const [form, setForm] = useState<SaveUserRequest>(emptyForm);
  const [formErrors, setFormErrors] = useState<UserFormErrors>({});
  const [resetUser, setResetUser] = useState<AdminUser | null>(null);
  const [resetPassword, setResetPassword] = useState({ password: '', confirmPassword: '' });
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const [selectedUser, setSelectedUser] = useState<AdminUser | null>(null);
  const [loading, setLoading] = useState(false);

  async function loadData() {
    setLoading(true);
    try {
      const [loadedUsers, loadedRoles] = await Promise.all([usersApi.list(), rolesApi.list({ isActive: true })]);
      setUsers(loadedUsers);
      setRoles(loadedRoles);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadData().catch((err) => toast.error(err instanceof Error ? err.message : 'Unable to load users.'));
  }, []);

  const filteredUsers = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    const result = users.filter((user) => {
      const matchesSearch = !normalizedSearch
        || user.userName.toLowerCase().includes(normalizedSearch)
        || user.fullName.toLowerCase().includes(normalizedSearch)
        || user.email.toLowerCase().includes(normalizedSearch)
        || user.mobileNumber.toLowerCase().includes(normalizedSearch);
      const matchesRole = roleFilter === '' || user.roleId === roleFilter;
      const matchesStatus = statusFilter === 'all'
        || (statusFilter === 'active' && user.isActive)
        || (statusFilter === 'inactive' && !user.isActive);
      return matchesSearch && matchesRole && matchesStatus && isInDateRange(user.createdDate, fromDate, toDate);
    });

    return [...result].sort((left, right) => {
      const leftValue = left[sortKey] ?? '';
      const rightValue = right[sortKey] ?? '';
      const comparison = String(leftValue).localeCompare(String(rightValue), undefined, { numeric: true, sensitivity: 'base' });
      return sortDirection === 'asc' ? comparison : -comparison;
    });
  }, [fromDate, roleFilter, search, sortDirection, sortKey, statusFilter, toDate, users]);

  const pagedUsers = filteredUsers.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);

  function requestSort(key: SortKey) {
    if (sortKey === key) {
      setSortDirection((current) => current === 'asc' ? 'desc' : 'asc');
    } else {
      setSortKey(key);
      setSortDirection('asc');
    }
  }

  function openCreate() {
    setEditingUser(null);
    setFormErrors({});
    setForm({ ...emptyForm, roleId: roles[0]?.id ?? 0 });
    setDialogOpen(true);
  }

  function openEdit(user: AdminUser) {
    setEditingUser(user);
    setFormErrors({});
    setForm({
      userName: user.userName,
      fullName: user.fullName,
      email: user.email,
      mobileNumber: user.mobileNumber,
      roleId: user.roleId ?? 0,
      isActive: user.isActive,
      password: '',
      confirmPassword: ''
    });
    setDialogOpen(true);
  }

  function openActionMenu(event: React.MouseEvent<HTMLElement>, user: AdminUser) {
    setAnchorEl(event.currentTarget);
    setSelectedUser(user);
  }

  function closeActionMenu() {
    setAnchorEl(null);
    setSelectedUser(null);
  }

  async function saveUser() {
    const errors: UserFormErrors = {};
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const mobilePattern = /^[0-9]{10,15}$/;

    if (!form.userName.trim()) errors.userName = 'Username is required';
    if (!form.fullName.trim()) errors.fullName = 'Full name is required';
    if (!form.email.trim()) {
      errors.email = 'Email is required';
    } else if (!emailPattern.test(form.email.trim())) {
      errors.email = 'Enter a valid email address';
    }

    if (form.mobileNumber.trim() && !mobilePattern.test(form.mobileNumber.trim())) {
      errors.mobileNumber = 'Enter 10 to 15 digits only';
    }

    if (form.roleId <= 0) errors.roleId = 'Role is required';

    if (!editingUser && (!form.password || form.password.length < 8)) {
      errors.password = 'Password must be at least 8 characters';
    }

    if (!editingUser && form.password !== form.confirmPassword) {
      errors.confirmPassword = 'Confirm password must match password';
    }

    if (Object.keys(errors).length > 0) {
      setFormErrors(errors);
      return;
    }

    try {
      const request = {
        ...form,
        userName: form.userName.trim(),
        fullName: form.fullName.trim(),
        email: form.email.trim().toLowerCase(),
        mobileNumber: form.mobileNumber.trim()
      };

      if (editingUser) {
        await usersApi.update(editingUser.id, request);
        toast.success('User updated');
      } else {
        await usersApi.create(request);
        toast.success('User created');
      }

      setDialogOpen(false);
      setFormErrors({});
      await loadData();
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unable to save user';
      setFormErrors({ form: message });
      toast.error(message);
    }
  }

  async function deleteUser(user: AdminUser) {
    closeActionMenu();
    if (!window.confirm(`Delete user ${user.userName}?`)) {
      return;
    }

    await usersApi.remove(user.id);
    toast.success('User deleted');
    await loadData();
  }

  async function toggleStatus(user: AdminUser) {
    closeActionMenu();
    if (user.isActive) {
      await usersApi.deactivate(user.id);
      toast.success('User deactivated');
    } else {
      await usersApi.activate(user.id);
      toast.success('User activated');
    }

    await loadData();
  }

  async function saveResetPassword() {
    if (!resetUser) {
      return;
    }

    await usersApi.resetPassword(resetUser.id, resetPassword.password, resetPassword.confirmPassword);
    toast.success('Password reset');
    setResetUser(null);
    setResetPassword({ password: '', confirmPassword: '' });
  }

  return (
    <Box>
      <Paper square elevation={0} sx={{ borderBottom: 1, borderColor: 'divider', mx: { xs: -2, sm: -3, xl: -5 }, px: { xs: 2, sm: 3, xl: 5 }, py: 2 }}>
        <Stack alignItems="center" direction="row" justifyContent="space-between" spacing={2}>
          <Stack alignItems="center" direction="row" spacing={1.5}>
            <IconButton aria-label="Back" onClick={() => window.history.back()}>
              <ArrowBackOutlinedIcon />
            </IconButton>
            <Typography fontWeight={800} variant="h5">Users</Typography>
          </Stack>
          <Button color="warning" onClick={openCreate} startIcon={<AddCircleOutlineOutlinedIcon />} variant="contained">
            Add User
          </Button>
        </Stack>
      </Paper>

      <Paper square elevation={0} sx={{ borderBottom: 1, borderColor: 'divider', mx: { xs: -2, sm: -3, xl: -5 }, px: { xs: 2, sm: 3, xl: 5 }, py: 2.5 }}>
        <Grid alignItems="center" container spacing={2}>
          <Grid size={{ xs: 12, md: 4 }}>
            <Typography variant="body2">Search</Typography>
            <TextField
              fullWidth
              InputProps={{ endAdornment: <InputAdornment position="end"><SearchOutlinedIcon /></InputAdornment> }}
              onChange={(event) => { setSearch(event.target.value); setPage(0); }}
              placeholder="Enter keywords"
              value={search}
            />
            <Typography color="text.secondary" sx={{ mt: 0.5 }} variant="caption">Search by name, email and mobile number.</Typography>
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 2 }}>
            <Typography variant="body2">From Date</Typography>
            <TextField fullWidth onChange={(event) => { setFromDate(event.target.value); setPage(0); }} type="date" value={fromDate} />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 2 }}>
            <Typography variant="body2">To Date</Typography>
            <TextField fullWidth onChange={(event) => { setToDate(event.target.value); setPage(0); }} type="date" value={toDate} />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 2 }}>
            <FormControl fullWidth>
              <InputLabel>Role</InputLabel>
              <Select label="Role" onChange={(event) => { setRoleFilter(event.target.value as number | ''); setPage(0); }} value={roleFilter}>
                <MenuItem value="">All roles</MenuItem>
                {roles.map((role) => <MenuItem key={role.id} value={role.id}>{role.roleName}</MenuItem>)}
              </Select>
            </FormControl>
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 2 }}>
            <Stack direction="row" spacing={1}>
              <Button fullWidth onClick={() => setPage(0)} sx={{ minHeight: 40 }} variant="contained">Submit</Button>
              <Tooltip title="Export Excel">
                <IconButton onClick={() => downloadCsv('users.csv', filteredUsers)}><FileDownloadOutlinedIcon /></IconButton>
              </Tooltip>
            </Stack>
          </Grid>
        </Grid>
      </Paper>

      <Paper square elevation={0} sx={{ mt: 2, overflow: 'hidden' }}>
        <TableContainer>
          <Table sx={{ minWidth: 1120 }}>
            <TableHead>
              <TableRow>
                {[
                  ['userName', 'User Name'],
                  ['fullName', 'Full Name'],
                  ['email', 'Email'],
                  ['mobileNumber', 'Mobile Number'],
                  ['roleName', 'Role'],
                  ['isActive', 'Status'],
                  ['createdDate', 'Created Date'],
                  ['lastLoginDate', 'Last Login']
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
              {pagedUsers.map((user) => (
                <TableRow hover key={user.id}>
                  <TableCell><Typography color="text.primary" fontWeight={800}>{user.userName}</Typography></TableCell>
                  <TableCell>{user.fullName}</TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>{user.mobileNumber || '-'}</TableCell>
                  <TableCell>{user.roleName || 'Unassigned'}</TableCell>
                  <TableCell>
                    <Chip color={user.isActive ? 'success' : 'default'} label={user.isActive ? 'Active' : 'Inactive'} size="small" variant="outlined" />
                  </TableCell>
                  <TableCell>{formatDate(user.createdDate)}</TableCell>
                  <TableCell>{formatDate(user.lastLoginDate)}</TableCell>
                  <TableCell align="center">
                    <IconButton aria-label="User actions" onClick={(event) => openActionMenu(event, user)} sx={{ bgcolor: 'background.paper', boxShadow: 1 }}>
                      <MoreHorizOutlinedIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
              {!loading && pagedUsers.length === 0 && (
                <TableRow>
                  <TableCell align="center" colSpan={9}>No users found.</TableCell>
                </TableRow>
              )}
              {loading && (
                <TableRow>
                  <TableCell align="center" colSpan={9}>Loading users...</TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          component="div"
          count={filteredUsers.length}
          onPageChange={(_, nextPage) => setPage(nextPage)}
          onRowsPerPageChange={(event) => { setRowsPerPage(Number(event.target.value)); setPage(0); }}
          page={page}
          rowsPerPage={rowsPerPage}
          rowsPerPageOptions={[5, 10, 25, 50]}
        />
      </Paper>

      <Menu anchorEl={anchorEl} onClose={closeActionMenu} open={!!anchorEl}>
        <MenuItem onClick={() => { if (selectedUser) setViewUser(selectedUser); closeActionMenu(); }}><VisibilityOutlinedIcon fontSize="small" sx={{ mr: 1 }} />View</MenuItem>
        <MenuItem onClick={() => { if (selectedUser) openEdit(selectedUser); closeActionMenu(); }}><EditOutlinedIcon fontSize="small" sx={{ mr: 1 }} />Edit</MenuItem>
        <MenuItem onClick={() => { if (selectedUser) setResetUser(selectedUser); closeActionMenu(); }}><LockResetOutlinedIcon fontSize="small" sx={{ mr: 1 }} />Reset Password</MenuItem>
        <MenuItem onClick={() => selectedUser && void toggleStatus(selectedUser)}>
          {selectedUser?.isActive ? <ToggleOffOutlinedIcon fontSize="small" sx={{ mr: 1 }} /> : <ToggleOnOutlinedIcon fontSize="small" sx={{ mr: 1 }} />}
          {selectedUser?.isActive ? 'Deactivate' : 'Activate'}
        </MenuItem>
        <Divider />
        <MenuItem onClick={() => selectedUser && void deleteUser(selectedUser)} sx={{ color: 'error.main' }}><DeleteOutlineOutlinedIcon fontSize="small" sx={{ mr: 1 }} />Delete</MenuItem>
      </Menu>

      <Dialog fullWidth maxWidth="md" onClose={() => setDialogOpen(false)} open={dialogOpen}>
        <DialogTitle>{editingUser ? 'Edit User' : 'Add User'}</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 0.5 }}>
            {formErrors.form && (
              <Grid size={{ xs: 12 }}>
                <Typography color="error" variant="body2">{formErrors.form}</Typography>
              </Grid>
            )}
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                autoComplete="off"
                error={!!formErrors.userName}
                fullWidth
                helperText={formErrors.userName}
                label="Username"
                name="aka-user-name"
                onChange={(event) => { setForm({ ...form, userName: event.target.value }); setFormErrors({ ...formErrors, userName: undefined, form: undefined }); }}
                required
                value={form.userName}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                autoComplete="name"
                error={!!formErrors.fullName}
                fullWidth
                helperText={formErrors.fullName}
                label="Full Name"
                name="aka-full-name"
                onChange={(event) => { setForm({ ...form, fullName: event.target.value }); setFormErrors({ ...formErrors, fullName: undefined, form: undefined }); }}
                required
                value={form.fullName}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                autoComplete="email"
                error={!!formErrors.email}
                fullWidth
                helperText={formErrors.email}
                label="Email"
                name="aka-email"
                onChange={(event) => { setForm({ ...form, email: event.target.value }); setFormErrors({ ...formErrors, email: undefined, form: undefined }); }}
                required
                type="email"
                value={form.email}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <TextField
                autoComplete="off"
                error={!!formErrors.mobileNumber}
                fullWidth
                helperText={formErrors.mobileNumber || 'Digits only, for example 9876543210'}
                inputProps={{ inputMode: 'numeric', maxLength: 15, pattern: '[0-9]*' }}
                label="Mobile Number"
                name="aka-mobile-number"
                onChange={(event) => {
                  setForm({ ...form, mobileNumber: event.target.value.replace(/\D/g, '') });
                  setFormErrors({ ...formErrors, mobileNumber: undefined, form: undefined });
                }}
                value={form.mobileNumber}
              />
            </Grid>
            {!editingUser && (
              <>
                <Grid size={{ xs: 12, md: 6 }}>
                  <TextField
                    autoComplete="new-password"
                    error={!!formErrors.password}
                    fullWidth
                    helperText={formErrors.password}
                    label="Password"
                    name="aka-new-password"
                    onChange={(event) => { setForm({ ...form, password: event.target.value }); setFormErrors({ ...formErrors, password: undefined, form: undefined }); }}
                    required
                    type="password"
                    value={form.password}
                  />
                </Grid>
                <Grid size={{ xs: 12, md: 6 }}>
                  <TextField
                    autoComplete="new-password"
                    error={!!formErrors.confirmPassword}
                    fullWidth
                    helperText={formErrors.confirmPassword}
                    label="Confirm Password"
                    name="aka-confirm-password"
                    onChange={(event) => { setForm({ ...form, confirmPassword: event.target.value }); setFormErrors({ ...formErrors, confirmPassword: undefined, form: undefined }); }}
                    required
                    type="password"
                    value={form.confirmPassword}
                  />
                </Grid>
              </>
            )}
            <Grid size={{ xs: 12, md: 6 }}>
              <FormControl error={!!formErrors.roleId} fullWidth>
                <InputLabel>Role</InputLabel>
                <Select label="Role" onChange={(event) => { setForm({ ...form, roleId: Number(event.target.value) }); setFormErrors({ ...formErrors, roleId: undefined, form: undefined }); }} value={form.roleId}>
                  {roles.map((role) => <MenuItem key={role.id} value={role.id}>{role.roleName}</MenuItem>)}
                </Select>
                {formErrors.roleId && <Typography color="error" sx={{ mt: 0.5, ml: 1.75 }} variant="caption">{formErrors.roleId}</Typography>}
              </FormControl>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}><FormControlLabel control={<Switch checked={form.isActive} onChange={(event) => setForm({ ...form, isActive: event.target.checked })} />} label="Active" /></Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button onClick={() => void saveUser()} variant="contained">Save</Button>
        </DialogActions>
      </Dialog>

      <Dialog fullWidth maxWidth="sm" onClose={() => setViewUser(null)} open={!!viewUser}>
        <DialogTitle>User Details</DialogTitle>
        <DialogContent>
          <Stack spacing={1.5} sx={{ mt: 1 }}>
            <Typography><strong>User Name:</strong> {viewUser?.userName}</Typography>
            <Typography><strong>Full Name:</strong> {viewUser?.fullName}</Typography>
            <Typography><strong>Email:</strong> {viewUser?.email}</Typography>
            <Typography><strong>Mobile:</strong> {viewUser?.mobileNumber || '-'}</Typography>
            <Typography><strong>Role:</strong> {viewUser?.roleName || 'Unassigned'}</Typography>
            <Typography><strong>Status:</strong> {viewUser?.isActive ? 'Active' : 'Inactive'}</Typography>
          </Stack>
        </DialogContent>
        <DialogActions><Button onClick={() => setViewUser(null)}>Close</Button></DialogActions>
      </Dialog>

      <Dialog fullWidth maxWidth="xs" onClose={() => setResetUser(null)} open={!!resetUser}>
        <DialogTitle>Reset Password</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Typography color="text.secondary">Reset password for {resetUser?.userName}</Typography>
            <TextField label="Password" onChange={(event) => setResetPassword({ ...resetPassword, password: event.target.value })} type="password" value={resetPassword.password} />
            <TextField label="Confirm Password" onChange={(event) => setResetPassword({ ...resetPassword, confirmPassword: event.target.value })} type="password" value={resetPassword.confirmPassword} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setResetUser(null)}>Cancel</Button>
          <Button onClick={() => void saveResetPassword()} variant="contained">Reset</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
