import { Clock } from 'lucide-react'
import { useTranslation } from 'react-i18next'

function formatDateTime(isoString: string): string {
  const d = new Date(isoString)
  const date = `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`
  const time = `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
  return `${date} ${time}`
}

interface AuditInfoProps {
  createdAt: string
  createdByName?: string | null
  modifiedAt?: string | null
  modifiedByName?: string | null
  className?: string
}

export function AuditInfo({ createdAt, createdByName, modifiedAt, modifiedByName, className }: AuditInfoProps) {
  const { t } = useTranslation()

  return (
    <div className={`flex items-start gap-1 text-xs text-muted-foreground justify-end ${className ?? ''}`}>
      <Clock className="h-3 w-3 shrink-0 mt-0.5" />
      <div className="flex flex-col">
        <span>
          {t('audit.loaded')}
          {createdByName ? ` ${t('audit.by')} ${createdByName}` : ''}
          {` ${t('audit.on')} ${formatDateTime(createdAt)}`}
        </span>
        {modifiedAt && (
          <span>
            {t('audit.modified')}
            {modifiedByName ? ` ${t('audit.by')} ${modifiedByName}` : ''}
            {` ${t('audit.on')} ${formatDateTime(modifiedAt)}`}
          </span>
        )}
      </div>
    </div>
  )
}
