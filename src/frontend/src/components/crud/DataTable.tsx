import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Button } from '@/components/ui/button'
import { Pencil, Trash2 } from 'lucide-react'
import type { ColumnDef } from './types'

interface DataTableProps<T> {
  data: T[]
  columns: ColumnDef<T>[]
  rowKey: keyof T & string
  onEdit: (item: T) => void
  onDelete: (item: T) => void
  emptyMessage?: string
  extraRowActions?: (item: T) => ReactNode
  hideEdit?: boolean
  readOnly?: boolean
}

export function DataTable<T>({
  data,
  columns,
  rowKey,
  onEdit,
  onDelete,
  emptyMessage,
  extraRowActions,
  hideEdit,
  readOnly,
}: DataTableProps<T>) {
  const { t } = useTranslation()

  return (
    <Table>
      <TableHeader>
        <TableRow>
          {columns.map((col) => (
            <TableHead key={col.key} className={col.className}>
              {col.header}
            </TableHead>
          ))}
          {!readOnly && <TableHead className="w-[80px] text-right">{t('common.actions')}</TableHead>}
        </TableRow>
      </TableHeader>
      <TableBody>
        {data.map((item) => (
          <TableRow key={String(item[rowKey])}>
            {columns.map((col) => (
              <TableCell key={col.key} className={col.className}>
                {col.render(item)}
              </TableCell>
            ))}
            {!readOnly && (
              <TableCell className="text-right">
                <div className="flex justify-end gap-1">
                  {extraRowActions?.(item)}
                  {!hideEdit && (
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => onEdit(item)}
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </Button>
                  )}
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    className="text-destructive hover:text-destructive"
                    onClick={() => onDelete(item)}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </TableCell>
            )}
          </TableRow>
        ))}
        {data.length === 0 && (
          <TableRow>
            <TableCell
              colSpan={readOnly ? columns.length : columns.length + 1}
              className="text-center text-muted-foreground"
            >
              {emptyMessage ?? t('common.noData')}
            </TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  )
}
