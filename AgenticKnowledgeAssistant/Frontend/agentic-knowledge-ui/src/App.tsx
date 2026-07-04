import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { AppLayout } from './components/layout/AppLayout';
import { ChatAssistantPage } from './pages/ChatAssistantPage';
import { ChatHistoryPage } from './pages/ChatHistoryPage';
import { DashboardPage } from './pages/DashboardPage';
import { DocumentUploadPage } from './pages/DocumentUploadPage';
import { KnowledgeBasePage } from './pages/KnowledgeBasePage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import { RolesPage } from './pages/RolesPage';
import { SettingsPage } from './pages/SettingsPage';
import { UsersPage } from './pages/UsersPage';
import { AiMemoryPage } from './pages/AiMemoryPage';
import { EnterpriseRagPage } from './pages/EnterpriseRagPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/chat" element={<ChatAssistantPage />} />
          <Route path="/documents" element={<DocumentUploadPage />} />
          <Route path="/knowledge-base" element={<KnowledgeBasePage />} />
          <Route path="/history" element={<ChatHistoryPage />} />
          <Route path="/memory" element={<AiMemoryPage />} />
          <Route path="/enterprise-rag" element={<EnterpriseRagPage />} />
          <Route element={<ProtectedRoute permissions={['Users.View']} />}>
            <Route path="/users" element={<UsersPage />} />
          </Route>
          <Route element={<ProtectedRoute permissions={['Roles.View']} />}>
            <Route path="/roles" element={<RolesPage />} />
          </Route>
          <Route path="/settings" element={<SettingsPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
