import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from '@/contexts/AuthContext'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { TooltipProvider } from '@/components/ui/tooltip'
import { Toaster } from '@/components/ui/sonner'
import { AppLayout } from '@/components/layout/AppLayout'
import { AdminLayout } from '@/components/layout/AdminLayout'
import DashboardPage from '@/pages/DashboardPage'
import CurrenciesPage from '@/pages/CurrenciesPage'
import CategoriesPage from '@/pages/CategoriesPage'
import CostCentersPage from '@/pages/CostCentersPage'
import CashBoxesPage from '@/pages/CashBoxesPage'
import BankAccountsPage from '@/pages/BankAccountsPage'
import CreditCardsPage from '@/pages/CreditCardsPage'
import SettingsPage from '@/pages/SettingsPage'
import ProfilePage from '@/pages/ProfilePage'
import MovementsPage from '@/pages/MovementsPage'
import MovementFormPage from '@/pages/MovementFormPage'
import FrequentMovementsPage from '@/pages/FrequentMovementsPage'
import FrequentMovementFormPage from '@/pages/FrequentMovementFormPage'
import TransfersPage from '@/pages/TransfersPage'
import TransferFormPage from '@/pages/TransferFormPage'
import StatementPeriodsPage from '@/pages/StatementPeriodsPage'
import StatementPeriodFormPage from '@/pages/StatementPeriodFormPage'
import CreditCardPaymentsPage from '@/pages/CreditCardPaymentsPage'
import AnalysisPage from '@/pages/AnalysisPage'
import ChartsPrototypePage from '@/pages/ChartsPrototypePage'
import GastosIngresosPage from '@/pages/reports/GastosIngresosPage'
import FlujoCajaPage from '@/pages/reports/FlujoCajaPage'
import TarjetasCCPage from '@/pages/reports/TarjetasCCPage'
import PatrimonioPage from '@/pages/reports/PatrimonioPage'
import AdminFamiliesPage from '@/pages/AdminFamiliesPage'
import AdminFamilyDetailPage from '@/pages/AdminFamilyDetailPage'
import JoinPage from '@/pages/JoinPage'

// Auth pages
import AuthEntryPage from '@/pages/auth/AuthEntryPage'
import AuthRegisterPage from '@/pages/auth/AuthRegisterPage'
import AuthVerifyPendingPage from '@/pages/auth/AuthVerifyPendingPage'
import AuthVerifyPage from '@/pages/auth/AuthVerifyPage'
import AuthLoginPage from '@/pages/auth/AuthLoginPage'
import ForgotPasswordPage from '@/pages/auth/ForgotPasswordPage'
import ResetPasswordPage from '@/pages/auth/ResetPasswordPage'
import SelectFamilyPage from '@/pages/auth/SelectFamilyPage'
import CreateFamilyPage from '@/pages/auth/CreateFamilyPage'
import AuthExternalCallbackPage from '@/pages/auth/AuthExternalCallbackPage'

/** Public auth routes: redirect to select-family if already authenticated */
function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, hasFamilySelected } = useAuth()
  if (isAuthenticated && hasFamilySelected) return <Navigate to="/" replace />
  if (isAuthenticated && !hasFamilySelected) return <Navigate to="/auth/select-family" replace />
  return <>{children}</>
}

/** Routes that require authentication but NOT a family selected */
function AuthenticatedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth()
  if (!isAuthenticated) return <Navigate to="/auth" replace />
  return <>{children}</>
}

/** Routes that require authentication AND a family selected */
function FamilyRequiredRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, hasFamilySelected } = useAuth()
  if (!isAuthenticated) return <Navigate to="/auth" replace />
  if (!hasFamilySelected) return <Navigate to="/auth/select-family" replace />
  return <>{children}</>
}

function FamilyAdminRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, hasFamilySelected, isFamilyAdmin } = useAuth()
  if (!isAuthenticated) return <Navigate to="/auth" replace />
  if (!hasFamilySelected) return <Navigate to="/auth/select-family" replace />
  if (!isFamilyAdmin) return <Navigate to="/" replace />
  return <>{children}</>
}


export default function App() {
  return (
    <BrowserRouter>
      <ThemeProvider>
        <AuthProvider>
          <TooltipProvider>
            <Routes>
            {/* Public auth routes */}
            <Route path="/auth" element={<PublicRoute><AuthEntryPage /></PublicRoute>} />
            <Route path="/auth/register" element={<PublicRoute><AuthRegisterPage /></PublicRoute>} />
            <Route path="/auth/verify-pending" element={<AuthVerifyPendingPage />} />
            <Route path="/auth/verify" element={<AuthVerifyPage />} />
            <Route path="/auth/login" element={<PublicRoute><AuthLoginPage /></PublicRoute>} />
            <Route path="/auth/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/auth/reset-password" element={<ResetPasswordPage />} />
            <Route path="/auth/external-callback" element={<AuthExternalCallbackPage />} />

            {/* Authenticated but no family required */}
            <Route path="/auth/select-family" element={<AuthenticatedRoute><SelectFamilyPage /></AuthenticatedRoute>} />
            <Route path="/auth/create-family" element={<AuthenticatedRoute><CreateFamilyPage /></AuthenticatedRoute>} />

            {/* Legacy redirects */}
            <Route path="/login" element={<Navigate to="/auth" replace />} />
            <Route path="/register" element={<Navigate to="/auth" replace />} />

            {/* Invitation flow (kept as-is) */}
            <Route path="/join" element={<JoinPage />} />

            {/* App routes - require family selected */}
            <Route element={<FamilyRequiredRoute><AppLayout /></FamilyRequiredRoute>}>
              <Route index element={<DashboardPage />} />
              <Route path="currencies" element={<CurrenciesPage />} />
              <Route path="categories" element={<CategoriesPage />} />
              <Route path="cost-centers" element={<CostCentersPage />} />
              <Route path="movements" element={<MovementsPage />} />
              <Route path="movements/new" element={<MovementFormPage />} />
              <Route path="movements/:id/edit" element={<MovementFormPage />} />
              <Route path="frequent-movements" element={<FrequentMovementsPage />} />
              <Route path="frequent-movements/new" element={<FrequentMovementFormPage />} />
              <Route path="frequent-movements/:id/edit" element={<FrequentMovementFormPage />} />
              <Route path="transfers" element={<TransfersPage />} />
              <Route path="transfers/new" element={<TransferFormPage />} />
              <Route path="transfers/:id/edit" element={<TransferFormPage />} />
              <Route path="analysis" element={<Navigate to="/reports/gastos" replace />} />
              <Route path="reports/gastos" element={<GastosIngresosPage />} />
              <Route path="reports/flujo" element={<FlujoCajaPage />} />
              <Route path="reports/tarjetas" element={<TarjetasCCPage />} />
              <Route path="reports/patrimonio" element={<PatrimonioPage />} />
              <Route path="charts-prototype" element={<ChartsPrototypePage />} />
              <Route path="cash-boxes" element={<CashBoxesPage />} />
              <Route path="bank-accounts" element={<BankAccountsPage />} />
              <Route path="credit-cards" element={<CreditCardsPage />} />
              <Route path="statements" element={<StatementPeriodsPage />} />
              <Route path="statements/:id/edit" element={<StatementPeriodFormPage />} />
              <Route path="cc-payments" element={<CreditCardPaymentsPage />} />
              <Route path="settings" element={<FamilyAdminRoute><SettingsPage /></FamilyAdminRoute>} />
              <Route path="members" element={<Navigate to="/settings?tab=members" replace />} />
              <Route path="profile" element={<ProfilePage />} />
            </Route>

            {/* Admin routes - require SuperAdmin, no family needed */}
            <Route element={<AdminLayout />}>
              <Route path="admin/families" element={<AdminFamiliesPage />} />
              <Route path="admin/families/:id" element={<AdminFamilyDetailPage />} />
            </Route>
          </Routes>
            </TooltipProvider>
            <Toaster position="top-right" richColors />
          </AuthProvider>
        </ThemeProvider>
      </BrowserRouter>
  )
}
