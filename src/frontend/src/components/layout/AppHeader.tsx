import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts/AuthContext'
import { useTheme } from '@/contexts/ThemeContext'
import { useNotifications } from '@/contexts/NotificationsContext'
import { useNavigate } from 'react-router-dom'
import { SidebarTrigger } from '@/components/ui/sidebar'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Bell, LogOut, ChevronDown, User, Sun, Moon, Clock, ArrowLeft } from 'lucide-react'

export function AppHeader() {
  const { t } = useTranslation()
  const { fullName, email, currentFamily, isSuperAdmin, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const { notifications, unreadCount, isUnread, markAllAsRead } = useNotifications()
  const navigate = useNavigate()

  const initials = fullName
    ?.split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2) ?? '?'

  const handleNotificationClick = (url: string) => {
    markAllAsRead()
    navigate(url)
  }

  return (
    <header className="flex h-14 items-center gap-3 border-b bg-card px-3 sm:px-4">
      <SidebarTrigger />

      {isSuperAdmin && (
        <Button variant="ghost" size="sm" onClick={() => navigate('/auth/select-family')} className="gap-1.5 text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          <span className="hidden sm:inline">{t('admin.backToFamilies')}</span>
        </Button>
      )}

      <div className="flex-1" />

      {/* Notification bell */}
      <DropdownMenu>
        <DropdownMenuTrigger render={<Button variant="ghost" size="icon" className="relative h-8 w-8 text-muted-foreground hover:text-foreground"><Bell className="h-4.5 w-4.5" />{unreadCount > 0 && (<span className="absolute -right-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-500 px-1 text-[9px] font-bold text-white leading-none">{unreadCount > 9 ? '9+' : unreadCount}</span>)}</Button>} />
        <DropdownMenuContent align="end" className="w-80">
          <div className="flex items-center justify-between px-3 py-2">
            <span className="text-sm font-semibold">{t('notifications.title')}</span>
            {unreadCount > 0 && (
              <button
                className="text-xs text-muted-foreground hover:text-foreground transition-colors"
                onClick={markAllAsRead}
              >
                {t('notifications.markAllRead')}
              </button>
            )}
          </div>
          <DropdownMenuSeparator />
          {notifications.length === 0 ? (
            <div className="px-3 py-6 text-center text-sm text-muted-foreground">
              {t('notifications.empty')}
            </div>
          ) : (
            notifications.map((n) => (
              <DropdownMenuItem
                key={n.id}
                className="flex flex-col items-start gap-0.5 px-3 py-2.5 cursor-pointer"
                onClick={() => handleNotificationClick(n.url)}
              >
                <div className="flex items-center gap-2 w-full">
                  <Clock className="h-3.5 w-3.5 text-destructive shrink-0" />
                  <span className={`text-sm flex-1 ${isUnread(n.id) ? 'font-semibold' : ''}`}>
                    {n.title}
                  </span>
                  {isUnread(n.id) && (
                    <span className="h-2 w-2 rounded-full bg-red-500 shrink-0" />
                  )}
                </div>
                <span className="pl-5 text-xs text-muted-foreground">
                  {t('notifications.overdueFrequentMovement', { date: n.date })}
                </span>
              </DropdownMenuItem>
            ))
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Theme toggle */}
      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8 text-muted-foreground hover:text-foreground"
        onClick={toggleTheme}
        title={theme === 'light' ? t('header.themeDark') : t('header.themeLight')}
      >
        {theme === 'light' ? <Moon className="h-4.5 w-4.5" /> : <Sun className="h-4.5 w-4.5" />}
      </Button>

      {/* User menu */}
      <DropdownMenu>
        <DropdownMenuTrigger className="flex h-8 cursor-pointer items-center gap-2 rounded-lg px-2 outline-none hover:bg-muted">
          <Avatar className="h-7 w-7">
            <AvatarFallback className="bg-primary text-[11px] text-primary-foreground">
              {initials}
            </AvatarFallback>
          </Avatar>
          <span className="hidden text-sm font-medium sm:inline-block">
            {fullName?.split(' ')[0] ?? 'Usuario'}
          </span>
          <ChevronDown className="hidden h-3.5 w-3.5 text-muted-foreground sm:block" />
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-52">
          <DropdownMenuGroup>
            <DropdownMenuLabel>
              <div className="flex flex-col space-y-1">
                <p className="text-sm font-medium">{fullName}</p>
                <p className="text-xs text-muted-foreground">{email}</p>
                {currentFamily && (
                  <p className="text-xs text-muted-foreground">{currentFamily.familyName}</p>
                )}
              </div>
            </DropdownMenuLabel>
          </DropdownMenuGroup>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={() => navigate('/profile')}>
            <User className="mr-2 h-4 w-4" />
            {t('header.myProfile')}
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={logout}>
            <LogOut className="mr-2 h-4 w-4" />
            {t('header.logout')}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </header>
  )
}
