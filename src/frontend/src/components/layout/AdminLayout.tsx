import { Outlet, Navigate } from 'react-router-dom'
import { SidebarProvider, SidebarInset } from '@/components/ui/sidebar'
import { useAuth } from '@/contexts/AuthContext'
import { AdminSidebar } from './AdminSidebar'
import { AdminHeader } from './AdminHeader'

export function AdminLayout() {
  const { isAuthenticated, isSuperAdmin } = useAuth()

  if (!isAuthenticated) return <Navigate to="/auth" replace />
  if (!isSuperAdmin) return <Navigate to="/" replace />

  return (
    <SidebarProvider>
      <AdminSidebar />
      <SidebarInset>
        <AdminHeader />
        <main className="flex-1 p-4 sm:p-6">
          <Outlet />
        </main>
      </SidebarInset>
    </SidebarProvider>
  )
}
