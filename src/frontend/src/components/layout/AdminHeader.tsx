import { useTranslation } from 'react-i18next'
import { useAuth } from '@/contexts/AuthContext'
import { useTheme } from '@/contexts/ThemeContext'
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
import { LogOut, Sun, Moon, ChevronDown, ArrowLeft } from 'lucide-react'

export function AdminHeader() {
  const { t } = useTranslation()
  const { fullName, email, hasFamilySelected, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const navigate = useNavigate()

  const initials = fullName
    ?.split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2) ?? '?'

  const handleBack = () => {
    if (hasFamilySelected) navigate('/')
    else navigate('/auth/select-family')
  }

  return (
    <header className="flex h-14 items-center gap-3 border-b bg-card px-3 sm:px-4">
      <SidebarTrigger />

      <Button variant="ghost" size="sm" onClick={handleBack} className="gap-1.5 text-muted-foreground hover:text-foreground">
        <ArrowLeft className="h-4 w-4" />
        <span className="hidden sm:inline">{hasFamilySelected ? t('admin.backToApp') : t('admin.backToFamilies')}</span>
      </Button>

      <div className="flex-1" />

      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8 text-muted-foreground hover:text-foreground"
        onClick={toggleTheme}
        title={theme === 'light' ? t('header.themeDark') : t('header.themeLight')}
      >
        {theme === 'light' ? <Moon className="h-4.5 w-4.5" /> : <Sun className="h-4.5 w-4.5" />}
      </Button>

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
              </div>
            </DropdownMenuLabel>
          </DropdownMenuGroup>
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
