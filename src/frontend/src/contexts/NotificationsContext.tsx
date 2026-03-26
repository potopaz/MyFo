import { createContext, useContext, useEffect, useState, useCallback } from 'react'
import api from '@/lib/api'
import type { FrequentMovementListItemDto } from '@/types/api'

export interface AppNotification {
  id: string
  type: 'overdue_frequent_movement'
  title: string
  date: string
  url: string
}

interface NotificationsContextValue {
  notifications: AppNotification[]
  unreadCount: number
  isUnread: (id: string) => boolean
  markAllAsRead: () => void
  refresh: () => void
}

const NotificationsContext = createContext<NotificationsContextValue>({
  notifications: [],
  unreadCount: 0,
  isUnread: () => false,
  markAllAsRead: () => {},
  refresh: () => {},
})

const STORAGE_KEY = 'myfo_read_notification_ids'

function getReadIds(): Set<string> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) return new Set(JSON.parse(raw) as string[])
  } catch {}
  return new Set()
}

function saveReadIds(ids: Set<string>) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify([...ids]))
}

export function NotificationsProvider({ children }: { children: React.ReactNode }) {
  const [notifications, setNotifications] = useState<AppNotification[]>([])
  const [readIds, setReadIds] = useState<Set<string>>(getReadIds)

  const load = useCallback(async () => {
    try {
      const { data } = await api.get<FrequentMovementListItemDto[]>('/frequent-movements')
      const today = new Date()
      today.setHours(0, 0, 0, 0)

      const notifs: AppNotification[] = data
        .filter((i) => i.isActive && i.nextDueDate && new Date(i.nextDueDate) <= today)
        .map((i) => ({
          id: `frequent_${i.frequentMovementId}`,
          type: 'overdue_frequent_movement' as const,
          title: i.name,
          date: new Date(i.nextDueDate!).toLocaleDateString('es-AR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
          }),
          url: '/frequent-movements',
        }))

      setNotifications(notifs)
    } catch {
      // silent — no interrumpir el layout
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const unreadCount = notifications.filter((n) => !readIds.has(n.id)).length

  const isUnread = useCallback((id: string) => !readIds.has(id), [readIds])

  const markAllAsRead = useCallback(() => {
    setReadIds((prev) => {
      const next = new Set(prev)
      notifications.forEach((n) => next.add(n.id))
      saveReadIds(next)
      return next
    })
  }, [notifications])

  return (
    <NotificationsContext.Provider value={{ notifications, unreadCount, isUnread, markAllAsRead, refresh: load }}>
      {children}
    </NotificationsContext.Provider>
  )
}

export function useNotifications() {
  return useContext(NotificationsContext)
}
