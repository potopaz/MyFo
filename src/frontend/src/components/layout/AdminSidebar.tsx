import { useLocation, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/components/ui/sidebar'
import { Users, Landmark } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

interface MenuItem {
  title: string
  url: string
  icon: LucideIcon
}

export function AdminSidebar() {
  const location = useLocation()
  const { t } = useTranslation()

  const items: MenuItem[] = [
    { title: t('nav.adminFamilies'), url: '/admin/families', icon: Users },
  ]

  const isActive = (url: string) => location.pathname.startsWith(url)

  return (
    <Sidebar>
      <SidebarHeader className="p-4 pb-2">
        <Link to="/admin/families" className="flex items-center gap-2.5">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-white/20">
            <Landmark className="h-4.5 w-4.5" />
          </div>
          <div className="flex flex-col">
            <span className="text-base font-bold leading-tight tracking-tight">MyFO</span>
            <span className="text-[10px] leading-tight opacity-70">{t('nav.admin')}</span>
          </div>
        </Link>
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>{t('nav.admin')}</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu className="gap-0.5">
              {items.map((item) => (
                <SidebarMenuItem key={item.url}>
                  <SidebarMenuButton render={<Link to={item.url} />} isActive={isActive(item.url)}>
                    <item.icon className="size-4" />
                    <span>{item.title}</span>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  )
}
