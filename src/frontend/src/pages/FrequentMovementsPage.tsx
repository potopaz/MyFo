import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { ConfirmDialog } from '@/components/crud/ConfirmDialog'
import { Plus, Pencil, Trash2, Play, HelpCircle } from 'lucide-react'
import api from '@/lib/api'
import axios from 'axios'
import type { FrequentMovementListItemDto } from '@/types/api'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrió un error inesperado'
}

function formatFrequency(t: ReturnType<typeof useTranslation>['t'], months: number): string {
  if (months === 1) return t('frequentMovements.frequencyLabel', { n: months })
  return t('frequentMovements.frequencyLabelPlural', { n: months })
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) return ''
  return new Date(dateStr).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function ItemCard({
  item,
  applying,
  deleting,
  onApply,
  onDelete,
  navigate,
  t,
}: {
  item: FrequentMovementListItemDto
  applying: string | null
  deleting: string | null
  onApply: (item: FrequentMovementListItemDto) => void
  onDelete: (id: string) => void
  navigate: ReturnType<typeof useNavigate>
  t: ReturnType<typeof useTranslation>['t']
}) {
  return (
    <div className="flex items-center gap-4 rounded-lg border bg-card px-4 py-3">
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="font-medium">{item.name}</span>
          {!item.isActive && <Badge variant="secondary">{t('common.inactive')}</Badge>}
          <Badge variant={item.movementType === 'Income' ? 'default' : 'outline'} className="text-xs">
            {item.movementType === 'Income' ? t('frequentMovements.form.income') : t('frequentMovements.form.expense')}
          </Badge>
        </div>
        <div className="text-sm text-muted-foreground mt-0.5 flex flex-wrap gap-x-3">
          <span>{item.categoryName} › {item.subcategoryName}</span>
          {item.paymentEntityName && <span>{item.paymentEntityName}</span>}
          <span>{formatFrequency(t, item.frequencyMonths)}</span>
          {item.nextDueDate && (
            <span>{t('frequentMovements.nextDue', { date: formatDate(item.nextDueDate) })}</span>
          )}
          {!item.lastAppliedAt && !item.nextDueDate && (
            <span>{t('frequentMovements.neverApplied')}</span>
          )}
        </div>
      </div>
      <div className="flex items-center gap-3 shrink-0">
        {item.amount > 0 && (
          <div className="text-right">
            <div className="font-semibold">
              {item.amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            </div>
            {item.currencyCode && <div className="text-xs text-muted-foreground">{item.currencyCode}</div>}
          </div>
        )}
        <div className="flex gap-1">
          <Button
            size="sm"
            disabled={applying === item.frequentMovementId}
            onClick={() => onApply(item)}
            title={t('frequentMovements.apply')}
          >
            <Play className="h-3.5 w-3.5 mr-1" />
            {t('frequentMovements.apply')}
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            onClick={() => navigate(`/frequent-movements/${item.frequentMovementId}/edit`)}
            title={t('common.edit')}
          >
            <Pencil className="h-3.5 w-3.5" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 text-destructive hover:text-destructive"
            disabled={deleting === item.frequentMovementId}
            onClick={() => onDelete(item.frequentMovementId)}
            title={t('common.delete')}
          >
            <Trash2 className="h-3.5 w-3.5" />
          </Button>
        </div>
      </div>
    </div>
  )
}

export default function FrequentMovementsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [items, setItems] = useState<FrequentMovementListItemDto[]>([])
  const [loading, setLoading] = useState(true)
  const [applying, setApplying] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<string | null>(null)
  const [deleteId, setDeleteId] = useState<string | null>(null)

  const load = async () => {
    setLoading(true)
    try {
      const { data } = await api.get<FrequentMovementListItemDto[]>('/frequent-movements')
      setItems(data)
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const handleApply = (item: FrequentMovementListItemDto) => {
    navigate(`/movements/new?from=${item.frequentMovementId}`)
  }

  const handleDelete = async () => {
    if (!deleteId) return
    setDeleting(deleteId)
    try {
      await api.delete(`/frequent-movements/${deleteId}`)
      toast.success(t('frequentMovements.deleteSuccess'))
      setItems((prev) => prev.filter((i) => i.frequentMovementId !== deleteId))
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setDeleting(null)
      setDeleteId(null)
    }
  }

  const today = new Date()
  today.setHours(0, 0, 0, 0)

  const isPending = (item: FrequentMovementListItemDto) => {
    if (!item.nextDueDate) return false
    return new Date(item.nextDueDate) <= today
  }

  const pending = items.filter((i) => i.isActive && isPending(i))
  const upToDate = items.filter((i) => i.isActive && !isPending(i))
  const inactive = items.filter((i) => !i.isActive)

  if (loading) {
    return <div className="flex items-center justify-center py-20 text-muted-foreground">{t('common.loading')}</div>
  }

  const cardProps = { applying, deleting, onApply: handleApply, onDelete: setDeleteId, navigate, t }

  return (
    <div className="mx-auto max-w-5xl space-y-6 pb-10">
      <div className="flex items-center justify-between">
        <div className="flex items-center">
          <h1 className="text-2xl font-bold">{t('frequentMovements.title')}
            <Tooltip>
              <TooltipTrigger>
                <HelpCircle className="inline h-3.5 w-3.5 ml-1.5 align-super text-muted-foreground cursor-help hover:text-foreground transition-colors" />
              </TooltipTrigger>
              <TooltipContent>
                {t('frequentMovements.tooltip')}
              </TooltipContent>
            </Tooltip>
          </h1>
        </div>
        <Button onClick={() => navigate('/frequent-movements/new')}>
          <Plus className="mr-2 h-4 w-4" />
          {t('frequentMovements.new')}
        </Button>
      </div>

      {items.length === 0 ? (
        <div className="flex items-center justify-center py-20 text-muted-foreground">
          {t('frequentMovements.empty')}
        </div>
      ) : (
        <>
          <div className="space-y-2">
            <h2 className="text-sm font-semibold text-destructive uppercase tracking-wide">
              {t('frequentMovements.sectionPending')}
            </h2>
            {pending.length === 0 ? (
              <p className="text-sm text-muted-foreground py-1 px-1">{t('frequentMovements.noPending')}</p>
            ) : (
              pending.map((item) => (
                <ItemCard key={item.frequentMovementId} item={item} {...cardProps} />
              ))
            )}
          </div>

          <div className="space-y-2">
            <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
              {t('frequentMovements.sectionUpToDate')}
            </h2>
            {upToDate.length === 0 ? (
              <p className="text-sm text-muted-foreground py-1 px-1">{t('frequentMovements.noUpToDate')}</p>
            ) : (
              upToDate.map((item) => (
                <ItemCard key={item.frequentMovementId} item={item} {...cardProps} />
              ))
            )}
          </div>

          {inactive.length > 0 && (
            <div className="space-y-2">
              <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
                {t('frequentMovements.sectionInactive')}
              </h2>
              {inactive.map((item) => (
                <ItemCard key={item.frequentMovementId} item={item} {...cardProps} />
              ))}
            </div>
          )}
        </>
      )}

      <ConfirmDialog
        open={deleteId !== null}
        onOpenChange={(open) => { if (!open) setDeleteId(null) }}
        onConfirm={handleDelete}
      />
    </div>
  )
}
