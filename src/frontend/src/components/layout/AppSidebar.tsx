import { useLocation, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts/AuthContext'
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
import {
  CircleDollarSign,
  Tags,
  Target,
  Wallet,
  Landmark,
  CreditCard,
  ArrowLeftRight,
  Receipt,
  LineChart,
  RefreshCw,
  FileText,
  Banknote,
  Settings,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

interface MenuItem {
  title: string
  url: string
  icon: LucideIcon
}

export function AppSidebar() {
  const location = useLocation()
  const { t } = useTranslation()
  const { isFamilyAdmin } = useAuth()

  const operationsItems: MenuItem[] = [
    { title: t('nav.frequentMovements'), url: '/frequent-movements', icon: RefreshCw },
    { title: t('nav.movements'), url: '/movements', icon: Receipt },
    { title: t('nav.transfers'), url: '/transfers', icon: ArrowLeftRight },
    { title: t('nav.statements'), url: '/statements', icon: FileText },
    { title: t('nav.ccPayments'), url: '/cc-payments', icon: Banknote },
  ]

  const configItems: MenuItem[] = [
    { title: t('nav.currencies'), url: '/currencies', icon: CircleDollarSign },
    { title: t('nav.costCenters'), url: '/cost-centers', icon: Target },
    { title: t('nav.categories'), url: '/categories', icon: Tags },
    { title: t('nav.cashBoxes'), url: '/cash-boxes', icon: Wallet },
    { title: t('nav.bankAccounts'), url: '/bank-accounts', icon: Landmark },
    { title: t('nav.creditCards'), url: '/credit-cards', icon: CreditCard },
  ]

  const reportItems: MenuItem[] = [
    { title: t('nav.analysis'), url: '/analysis', icon: LineChart },
  ]

  const adminItems: MenuItem[] = [
    { title: t('nav.settings'), url: '/settings', icon: Settings },
  ]

  const isActive = (url: string) =>
    url === '/' ? location.pathname === '/' : location.pathname.startsWith(url)

  const renderMenu = (items: MenuItem[]) => (
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
  )

  return (
    <Sidebar>
      <SidebarHeader className="p-4 pb-2">
        <Link to="/" className="flex items-center gap-2.5">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-white/20">
            <Landmark className="h-4.5 w-4.5" />
          </div>
          <div className="flex flex-col">
            <span className="text-base font-bold leading-tight tracking-tight">MyFO</span>
            <span className="text-[10px] leading-tight opacity-70">Family Office</span>
          </div>
        </Link>
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>{t('nav.operations')}</SidebarGroupLabel>
          <SidebarGroupContent>
            {renderMenu(operationsItems)}
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarGroupLabel>{t('nav.configuration')}</SidebarGroupLabel>
          <SidebarGroupContent>
            {renderMenu(configItems)}
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarGroupLabel>{t('nav.reports')}</SidebarGroupLabel>
          <SidebarGroupContent>
            {renderMenu(reportItems)}
          </SidebarGroupContent>
        </SidebarGroup>

        {isFamilyAdmin && (
          <SidebarGroup>
            <SidebarGroupLabel>{t('nav.admin')}</SidebarGroupLabel>
            <SidebarGroupContent>
              {renderMenu(adminItems)}
            </SidebarGroupContent>
          </SidebarGroup>
        )}

      </SidebarContent>

    </Sidebar>
  )
}
