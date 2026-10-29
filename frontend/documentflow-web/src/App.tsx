import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { AuthProvider, useAuth } from './components/AuthProvider'
import { Layout } from './components/Layout'
import { DocumentDetailsPage } from './pages/DocumentDetailsPage'
import { CounterpartyPage } from './pages/CounterpartyPage'
import { DocumentsPage } from './pages/DocumentsPage'
import { UploadDocumentPage } from './pages/UploadDocumentPage'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { ChangePasswordPage } from './pages/ChangePasswordPage'
import { UsersPage } from './pages/UsersPage'
import { SearchPage } from './pages/SearchPage'
import { AdminCatalogsPage } from './pages/AdminCatalogsPage'

function Protected({ admin = false }: { admin?: boolean }) {
  const { user, loading } = useAuth()
  const location = useLocation()
  if (loading) return <div className="page-shell state-panel">Проверка сеанса...</div>
  if (!user) return <Navigate replace to="/login" state={{ from: location.pathname + location.search }} />
  if (admin && user.role !== 'Admin') return <div className="page-shell"><div className="alert alert-error" role="alert">Недостаточно прав</div></div>
  return <Layout />
}

export default function App() {
  return <AuthProvider><Routes>
    <Route path="login" element={<LoginPage />} />
    <Route element={<Protected />}>
      <Route index element={<Navigate replace to="/documents" />} />
      <Route path="documents" element={<DocumentsPage />} />
      <Route path="search" element={<SearchPage />} />
      <Route path="documents/:id" element={<DocumentDetailsPage />} />
      <Route path="counterparties/:inn" element={<CounterpartyPage />} />
      <Route path="dashboard" element={<DashboardPage />} />
      <Route path="upload" element={<UploadDocumentPage />} />
      <Route path="change-password" element={<ChangePasswordPage />} />
      <Route path="*" element={<Navigate replace to="/documents" />} />
    </Route>
    <Route element={<Protected admin />}><Route path="users" element={<UsersPage />} /><Route path="admin/catalogs" element={<AdminCatalogsPage />} /></Route>
  </Routes></AuthProvider>
}
