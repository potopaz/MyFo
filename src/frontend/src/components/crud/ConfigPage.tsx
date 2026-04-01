import { useCallback, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import { DataTable } from './DataTable'
import { SearchBar } from './SearchBar'
import { SideDrawer } from './SideDrawer'
import { ConfirmDialog } from './ConfirmDialog'
import type { ConfigPageProps } from './types'
import axios from 'axios'

function extractErrorMessage(err: unknown, t: (key: string) => string): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
    if (data?.title) return data.title
    if (err.response?.status === 404) return t('errors.notFound')
    if (err.response?.status === 403) return t('errors.forbidden')
    if (err.response?.status === 500) return t('errors.serverError')
    if (!err.response) return t('errors.noConnection')
  }
  return t('errors.unexpected')
}

export function ConfigPage<T>({
  title,
  columns,
  fields,
  rowKey,
  fetchItems,
  onCreate,
  onUpdate,
  onDelete,
  defaultValues,
  newItemLabel,
  createTitle,
  editTitle,
  mapItemToForm,
  extraRowActions,
  hideEdit,
  readOnly,
}: ConfigPageProps<T>) {
  const { t } = useTranslation()
  const [items, setItems] = useState<T[]>([])
  const [search, setSearch] = useState('')
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [editingItem, setEditingItem] = useState<T | null>(null)
  const [formValues, setFormValues] = useState<Record<string, unknown>>(defaultValues)
  const [saving, setSaving] = useState(false)
  const [deleteItem, setDeleteItem] = useState<T | null>(null)

  const load = useCallback(() => {
    fetchItems().then(setItems)
  }, [fetchItems])

  useEffect(() => { load() }, [load])

  // Filter items by search (searches all string column values)
  const filtered = useMemo(() => {
    if (!search.trim()) return items
    const q = search.toLowerCase()
    return items.filter((item) =>
      columns.some((col) => {
        const val = (item as Record<string, unknown>)[col.key]
        return typeof val === 'string' && val.toLowerCase().includes(q)
      })
    )
  }, [items, search, columns])

  const openCreate = () => {
    setEditingItem(null)
    setFormValues({ ...defaultValues })
    setDrawerOpen(true)
  }

  const openEdit = (item: T) => {
    setEditingItem(item)
    if (mapItemToForm) {
      setFormValues(mapItemToForm(item))
    } else {
      const vals: Record<string, unknown> = {}
      for (const f of fields) {
        vals[f.key] = (item as Record<string, unknown>)[f.key] ?? defaultValues[f.key]
      }
      setFormValues(vals)
    }
    setDrawerOpen(true)
  }

  // Fields visible in the current mode (create vs edit)
  const visibleFields = useMemo(() => {
    if (editingItem) return fields.filter((f) => !f.createOnly)
    return fields
  }, [fields, editingItem])

  const handleSave = async () => {
    // Validate required fields (only visible ones)
    for (const field of visibleFields) {
      if (field.required && field.type !== 'checkbox' && field.type !== 'switch') {
        const val = formValues[field.key]
        if (val === undefined || val === null || String(val).trim() === '') {
          toast.error(t('errors.fieldRequired', { field: field.label }))
          return
        }
      }
    }
    setSaving(true)
    try {
      if (editingItem) {
        await onUpdate(String((editingItem as Record<string, unknown>)[rowKey]), formValues)
        toast.success(t('crud.updated'))
      } else {
        await onCreate(formValues)
        toast.success(t('crud.created'))
      }
      setDrawerOpen(false)
      load()
    } catch (err) {
      const message = extractErrorMessage(err, t)
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteItem) return
    try {
      await onDelete(String((deleteItem as Record<string, unknown>)[rowKey]))
      toast.success(t('crud.deleted'))
      setDeleteItem(null)
      load()
    } catch (err) {
      const message = extractErrorMessage(err, t)
      toast.error(message)
      setDeleteItem(null)
    }
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">{title}</h1>
        {!readOnly && (
          <Button onClick={openCreate}>
            <Plus className="mr-2 h-4 w-4" />
            {newItemLabel}
          </Button>
        )}
      </div>

      {/* Search */}
      <SearchBar value={search} onChange={setSearch} />

      {/* Table */}
      <Card>
        <CardContent className="p-0 sm:p-0">
          <DataTable
            data={filtered}
            columns={columns}
            rowKey={rowKey}
            onEdit={openEdit}
            onDelete={setDeleteItem}
            extraRowActions={extraRowActions}
            hideEdit={hideEdit}
            readOnly={readOnly}
          />
        </CardContent>
      </Card>

      {/* Side drawer */}
      <SideDrawer
        key={editingItem ? String((editingItem as Record<string, unknown>)[rowKey]) : '__new__'}
        open={drawerOpen}
        onOpenChange={setDrawerOpen}
        title={editingItem ? editTitle : createTitle}
        fields={visibleFields}
        values={formValues}
        onChange={(key, value) => setFormValues((prev) => ({ ...prev, [key]: value }))}
        onSubmit={handleSave}
        loading={saving}
      />

      {/* Delete confirmation */}
      <ConfirmDialog
        open={deleteItem !== null}
        onOpenChange={(open) => { if (!open) setDeleteItem(null) }}
        onConfirm={handleDelete}
      />
    </div>
  )
}
