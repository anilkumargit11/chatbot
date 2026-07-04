import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';

type ProtectedRouteProps = {
  roles?: string[];
  permissions?: string[];
};

export function ProtectedRoute({ roles, permissions }: ProtectedRouteProps) {
  const { isAuthenticated, hasPermission, hasRole } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate replace state={{ from: location }} to="/login" />;
  }

  if (roles?.length && !hasRole(roles)) {
    return <Navigate replace to="/" />;
  }

  if (permissions?.length && !hasPermission(permissions)) {
    return <Navigate replace to="/" />;
  }

  return <Outlet />;
}
