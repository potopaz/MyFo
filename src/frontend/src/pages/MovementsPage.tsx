import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { ConfirmDialog } from '@/components/crud/ConfirmDialog'
import { Plus, Pencil, Trash2, Copy, HelpCircle, Lock } from 'lucide-react'
import api from '@/lib/api'
import axios from 'axios'
import type { MovementListItemDto } from '@/types/api'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrió un error inesperado'
}

function formatDateISO(d: Date): string {
  return d.toISOString().slice(0, 10)
}

function daysAgo(n: number): string {
  const d = new Date()
  d.setDate(d.getDate() - n)
  return formatDateISO(d)
}

function formatDateDisplay(dateISO: string): string {
  const [year, month, day] = dateISO.split('-')
  return `${day}/${month}/${year}`
}

export default function MovementsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [items, setItems] = useState<MovementListItemDto[]>([])
  const [loading, setLoading] = useState(true)

  const [filterDateFrom, setFilterDateFrom] = useState(() => daysAgo(7))
  const [filterDateTo, setFilterDateTo] = useState(() => formatDateISO(new Date()))
  const [filterType, setFilterType] = useState('_all_')
  const [filterDescription, setFilterDescription] = useState('')
  const [descriptionInput, setDescriptionInput] = useState('')
  const debounceRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  const [deleteId, setDeleteId] = useState<string | null>(null)

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (filterDateFrom) params.set('dateFrom', filterDateFrom)
      if (filterDateTo) params.set('dateTo', filterDateTo)
      if (filterType && filterType !== '_all_') params.set('movementType', filterType)
      if (filterDescription.trim()) params.set('description', filterDescription.trim())
      const { data } = await api.get<MovementListItemDto[]>(`/movements?${params}`)
      setItems(data)
    } catch {
      toast.error('Error al cargar movimientos')
    } finally {
      setLoading(false)
    }
  }, [filterDateFrom, filterDateTo, filterType, filterDescription])

  useEffect(() => { loadData() }, [loadData])

  const handleDelete = async () => {
    if (!deleteId) return
    try {
      await api.delete(`/movements/${deleteId}`)
      toast.success(t('movements.deleteSuccess'))
      setDeleteId(null)
      loadData()
    } catch (err) {
      toast.error(extractError(err))
      setDeleteId(null)
    }
  }

  const formatClassification = (accountingType: string | null, isOrdinary: boolean | null, costCenterName: string | null): string => {
    const parts: string[] = []

    if (accountingType) {
      const labels: Record<string, string> = {
        Asset: t('movements.classification.asset'),
        Liability: t('movements.classification.liability'),
        Income: t('movements.classification.income'),
        Expense: t('movements.classification.expense'),
      }
      parts.push(labels[accountingType] || accountingType)
    }

    if (isOrdinary !== null) {
      parts.push(isOrdinary ? t('movements.classification.ordinary') : t('movements.classification.extraordinary'))
    }

    if (costCenterName) {
      parts.push(costCenterName)
    }

    return parts.length > 0 ? parts.join(' • ') : '—'
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center">
          <h1 className="text-2xl font-bold">{t('movements.title')}
            <Tooltip>
              <TooltipTrigger>
                <HelpCircle className="inline h-3.5 w-3.5 ml-1.5 align-super text-muted-foreground cursor-help hover:text-foreground transition-colors" />
              </TooltipTrigger>
              <TooltipContent>
                {t('movements.tooltip')}
              </TooltipContent>
            </Tooltip>
          </h1>
        </div>
        <Button onClick={() => navigate('/movements/new')}>
          <Plus className="mr-2 h-4 w-4" />
          {t('movements.new')}
        </Button>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="flex flex-wrap items-end gap-3 p-3">
          <div className="flex flex-col gap-1">
            <Label className="text-xs">{t('common.from')}</Label>
            <Input
              type="date"
              value={filterDateFrom}
              onChange={(e) => setFilterDateFrom(e.target.value)}
              className="h-8 w-36"
            />
          </div>
          <div className="flex flex-col gap-1">
            <Label className="text-xs">{t('common.to')}</Label>
            <Input
              type="date"
              value={filterDateTo}
              onChange={(e) => setFilterDateTo(e.target.value)}
              className="h-8 w-36"
            />
          </div>
          <div className="flex flex-col gap-1">
            <Label className="text-xs">{t('movements.columns.type')}</Label>
            <Select value={filterType} onValueChange={(v) => setFilterType(v ?? '_all_')}>
              <SelectTrigger className="h-8 w-36">
                <SelectValue>
                  {{
                    _all_: t('movements.allTypes'),
                    Income: t('movements.incomeFilter'),
                    Expense: t('movements.expenseFilter'),
                  }[filterType] ?? t('movements.allTypes')}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="_all_">{t('movements.allTypes')}</SelectItem>
                <SelectItem value="Income">{t('movements.incomeFilter')}</SelectItem>
                <SelectItem value="Expense">{t('movements.expenseFilter')}</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1">
            <Label className="text-xs">{t('movements.columns.description')}</Label>
            <Input
              value={descriptionInput}
              onChange={(e) => {
                const v = e.target.value
                setDescriptionInput(v)
                clearTimeout(debounceRef.current)
                debounceRef.current = setTimeout(() => setFilterDescription(v), 400)
              }}
              placeholder={t('movements.searchDescription')}
              className="h-8 w-48"
              maxLength={100}
            />
          </div>
        </CardContent>
      </Card>

      {/* Table */}
      {loading ? (
        <p className="py-8 text-center text-muted-foreground">{t('common.loading')}</p>
      ) : items.length === 0 ? (
        <p className="py-8 text-center text-muted-foreground">{t('movements.empty')}</p>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('movements.columns.date')}</TableHead>
                <TableHead>{t('movements.columns.type')}</TableHead>
                <TableHead>{t('movements.columns.category')}</TableHead>
                <TableHead>{t('movements.columns.subcategory')}</TableHead>
                <TableHead>{t('movements.columns.description')}</TableHead>
                <TableHead>{t('movements.columns.classification')}</TableHead>
                <TableHead>{t('movements.columns.currency')}</TableHead>
                <TableHead className="text-right">{t('movements.columns.amount')}</TableHead>
                <TableHead className="w-28" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.movementId}>
                  <TableCell>{formatDateDisplay(item.date)}</TableCell>
                  <TableCell>
                    <Badge variant={item.movementType === 'Income' ? 'default' : 'secondary'}>
                      {item.movementType === 'Income' ? t('movements.incomeLabel') : t('movements.expenseLabel')}
                    </Badge>
                  </TableCell>
                  <TableCell>{item.categoryName}</TableCell>
                  <TableCell>{item.subcategoryName}</TableCell>
                  <TableCell className="max-w-[200px] truncate">{item.description ?? ''}</TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {formatClassification(item.accountingType, item.isOrdinary, item.costCenterName)}
                  </TableCell>
                  <TableCell>{item.currencyCode}</TableCell>
                  <TableCell className="text-right font-medium">
                    {item.amount.toLocaleString('es-AR', { minimumFractionDigits: 2 })}
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7"
                        title="Duplicar"
                        onClick={() => navigate(`/movements/new?duplicate=${item.movementId}`)}
                      >
                        <Copy className="h-3.5 w-3.5" />
                      </Button>
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7"
                        title="Editar"
                        onClick={() => navigate(`/movements/${item.movementId}/edit`)}
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                      {item.hasAssignedInstallments ? (
                        <Tooltip>
                          <TooltipTrigger>
                            <div className="inline-flex items-center justify-center h-7 w-7 text-muted-foreground">
                              <Lock className="h-3.5 w-3.5" />
                            </div>
                          </TooltipTrigger>
                          <TooltipContent>{t('movements.lockedByStatement')}</TooltipContent>
                        </Tooltip>
                      ) : (
                        <Button
                          variant="ghost" size="icon" className="h-7 w-7 text-destructive hover:text-destructive"
                          title="Eliminar"
                          onClick={() => setDeleteId(item.movementId)}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <ConfirmDialog
        open={deleteId !== null}
        onOpenChange={(open) => { if (!open) setDeleteId(null) }}
        onConfirm={handleDelete}
      />
    </div>
  )
}
