import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { ConfirmDialog } from '@/components/crud/ConfirmDialog'
import { Plus, Pencil, Trash2, HelpCircle, ArrowRight, Info, CheckCircle, XCircle, Eye } from 'lucide-react'
import api from '@/lib/api'
import axios from 'axios'
import { useAuth } from '@/contexts/AuthContext'
import type { TransferListItemDto } from '@/types/api'

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

function formatAmount(amount: number): string {
  return amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}


function getAccountName(item: TransferListItemDto, side: 'from' | 'to'): string {
  if (side === 'from') return item.fromCashBoxName ?? item.fromBankAccountName ?? '—'
  return item.toCashBoxName ?? item.toBankAccountName ?? '—'
}

function StatusBadge({ status, isAutoConfirmed }: { status: string; isAutoConfirmed: boolean }) {
  const { t } = useTranslation()
  if (status === 'Confirmed' && isAutoConfirmed) return <Badge variant="default">{t('transfers.status.autoConfirmed')}</Badge>
  if (status === 'Confirmed') return <Badge variant="default">{t('transfers.status.confirmed')}</Badge>
  if (status === 'PendingConfirmation') return <Badge variant="secondary">{t('transfers.status.pending')}</Badge>
  if (status === 'Rejected') return <Badge variant="destructive">{t('transfers.status.rejected')}</Badge>
  return null
}

function RejectDialog({ transferId, onClose, onSuccess }: { transferId: string; onClose: () => void; onSuccess: () => void }) {
  const { t } = useTranslation()
  const [comment, setComment] = useState('')
  const [saving, setSaving] = useState(false)

  const handleReject = async () => {
    setSaving(true)
    try {
      await api.post(`/transfers/${transferId}/reject`, { comment: comment || null })
      toast.success(t('transfers.rejectSuccess'))
      onSuccess()
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(open) => { if (!open) onClose() }}>
      <DialogContent className="max-w-md" showCloseButton={false}>
        <DialogHeader>
          <DialogTitle>{t('transfers.rejectTitle')}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3 py-2">
          <div className="space-y-1">
            <Label className="text-sm">{t('transfers.rejectComment')}</Label>
            <Input
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder={t('common.optional')}
              maxLength={200}
              disabled={saving}
            />
          </div>
        </div>
        <div className="flex justify-end gap-2 pt-2 border-t">
          <Button variant="outline" onClick={onClose} disabled={saving}>{t('common.cancel')}</Button>
          <Button variant="destructive" onClick={handleReject} disabled={saving}>
            {saving ? t('common.saving') : t('transfers.rejectConfirm')}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}

export default function TransfersPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { userId: currentUserId } = useAuth()
  const [items, setItems] = useState<TransferListItemDto[]>([])
  const [loading, setLoading] = useState(true)

  const [filterDateFrom, setFilterDateFrom] = useState(() => daysAgo(30))
  const [filterDateTo, setFilterDateTo] = useState(() => formatDateISO(new Date()))
  const [filterStatus, setFilterStatus] = useState('all')

  const [deleteId, setDeleteId] = useState<string | null>(null)
  const [rejectId, setRejectId] = useState<string | null>(null)

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (filterDateFrom) params.set('dateFrom', filterDateFrom)
      if (filterDateTo) params.set('dateTo', filterDateTo)
      if (filterStatus !== 'all') params.set('status', filterStatus)
      const { data } = await api.get<TransferListItemDto[]>(`/transfers?${params}`)
      setItems(data)
    } catch {
      toast.error(t('transfers.loadError'))
    } finally {
      setLoading(false)
    }
  }, [filterDateFrom, filterDateTo, filterStatus, t])

  useEffect(() => { loadData() }, [loadData])

  const handleDelete = async () => {
    if (!deleteId) return
    try {
      await api.delete(`/transfers/${deleteId}`)
      toast.success(t('transfers.deleteSuccess'))
      setDeleteId(null)
      loadData()
    } catch (err) {
      toast.error(extractError(err))
      setDeleteId(null)
    }
  }

  const handleConfirm = async (id: string) => {
    try {
      await api.post(`/transfers/${id}/confirm`)
      toast.success(t('transfers.confirmSuccess'))
      loadData()
    } catch (err) {
      toast.error(extractError(err))
    }
  }

  const canEdit = (item: TransferListItemDto) =>
    item.status === 'PendingConfirmation' && item.creatorUserId === currentUserId

  const canDelete = (item: TransferListItemDto) =>
    (item.status === 'PendingConfirmation' && item.creatorUserId === currentUserId) ||
    (item.status === 'Confirmed' && item.isAutoConfirmed)

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center">
          <h1 className="text-2xl font-bold">{t('transfers.title')}
            <Tooltip>
              <TooltipTrigger>
                <HelpCircle className="inline h-3.5 w-3.5 ml-1.5 align-super text-muted-foreground cursor-help hover:text-foreground transition-colors" />
              </TooltipTrigger>
              <TooltipContent>
                {t('transfers.tooltip')}
              </TooltipContent>
            </Tooltip>
          </h1>
        </div>
        <Button onClick={() => navigate('/transfers/new')}>
          <Plus className="mr-2 h-4 w-4" />
          {t('transfers.new')}
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
            <Label className="text-xs">{t('transfers.filterStatus')}</Label>
            <Select value={filterStatus} onValueChange={(val) => val && setFilterStatus(val)}>
              <SelectTrigger className="h-8 w-44">
                <SelectValue>
                  {filterStatus === 'all' ? t('transfers.status.all')
                    : filterStatus === 'Confirmed' ? t('transfers.status.confirmed')
                    : filterStatus === 'PendingConfirmation' ? t('transfers.status.pending')
                    : t('transfers.status.rejected')}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">{t('transfers.status.all')}</SelectItem>
                <SelectItem value="Confirmed">{t('transfers.status.confirmed')}</SelectItem>
                <SelectItem value="PendingConfirmation">{t('transfers.status.pending')}</SelectItem>
                <SelectItem value="Rejected">{t('transfers.status.rejected')}</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Table */}
      {loading ? (
        <p className="py-8 text-center text-muted-foreground">{t('common.loading')}</p>
      ) : items.length === 0 ? (
        <p className="py-8 text-center text-muted-foreground">{t('transfers.empty')}</p>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('transfers.columns.date')}</TableHead>
                <TableHead>{t('transfers.columns.from')}</TableHead>
                <TableHead className="w-6" />
                <TableHead>{t('transfers.columns.to')}</TableHead>
                <TableHead>{t('transfers.columns.fromCurrency')}</TableHead>
                <TableHead className="text-right">{t('transfers.columns.fromAmount')}</TableHead>
                <TableHead>{t('transfers.columns.toCurrency')}</TableHead>
                <TableHead className="text-right">{t('transfers.columns.toAmount')}</TableHead>
                <TableHead>{t('transfers.columns.status')}</TableHead>
                <TableHead className="w-28" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.transferId}>
                  <TableCell>{formatDateDisplay(item.date)}</TableCell>
                  <TableCell className="font-medium">{getAccountName(item, 'from')}</TableCell>
                  <TableCell className="text-muted-foreground">
                    <ArrowRight className="h-3.5 w-3.5" />
                  </TableCell>
                  <TableCell className="font-medium">{getAccountName(item, 'to')}</TableCell>
                  <TableCell>{item.fromCurrencyCode}</TableCell>
                  <TableCell className="text-right font-medium">{formatAmount(item.amount)}</TableCell>
                  <TableCell>{item.toCurrencyCode}</TableCell>
                  <TableCell className="text-right font-medium">{formatAmount(item.amountTo)}</TableCell>
                  <TableCell>
                    <StatusBadge status={item.status} isAutoConfirmed={item.isAutoConfirmed} />
                    {item.status === 'Rejected' && item.rejectionComment && (
                      <Tooltip>
                        <TooltipTrigger>
                          <Info className="inline ml-1 h-3 w-3 text-muted-foreground cursor-help" />
                        </TooltipTrigger>
                        <TooltipContent>{item.rejectionComment}</TooltipContent>
                      </Tooltip>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-1 items-center">

                      {/* Confirm (only PendingConfirmation, not creator) */}
                      {item.status === 'PendingConfirmation' && item.creatorUserId !== currentUserId && (
                        <Tooltip>
                          <TooltipTrigger render={<Button variant="ghost" size="icon" className="h-7 w-7 text-green-600 hover:text-green-600" onClick={() => handleConfirm(item.transferId)}><CheckCircle className="h-3.5 w-3.5" /></Button>} />
                          <TooltipContent>{t('transfers.confirm')}</TooltipContent>
                        </Tooltip>
                      )}

                      {/* Reject (only PendingConfirmation, not creator) */}
                      {item.status === 'PendingConfirmation' && item.creatorUserId !== currentUserId && (
                        <Tooltip>
                          <TooltipTrigger render={<Button variant="ghost" size="icon" className="h-7 w-7 text-destructive hover:text-destructive" onClick={() => setRejectId(item.transferId)}><XCircle className="h-3.5 w-3.5" /></Button>} />
                          <TooltipContent>{t('transfers.reject')}</TooltipContent>
                        </Tooltip>
                      )}

                      {/* Edit (only PendingConfirmation, creator only) or View (all others) */}
                      {canEdit(item) ? (
                        <Button
                          variant="ghost" size="icon" className="h-7 w-7"
                          title={t('common.edit')}
                          onClick={() => navigate(`/transfers/${item.transferId}/edit`)}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                      ) : (
                        <Button
                          variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground"
                          title={t('common.view')}
                          onClick={() => navigate(`/transfers/${item.transferId}/edit`)}
                        >
                          <Eye className="h-3.5 w-3.5" />
                        </Button>
                      )}

                      {/* Delete */}
                      {canDelete(item) && (
                        <Button
                          variant="ghost" size="icon" className="h-7 w-7 text-destructive hover:text-destructive"
                          title={t('common.delete')}
                          onClick={() => setDeleteId(item.transferId)}
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

      {rejectId && (
        <RejectDialog
          transferId={rejectId}
          onClose={() => setRejectId(null)}
          onSuccess={() => { setRejectId(null); loadData() }}
        />
      )}
    </div>
  )
}
