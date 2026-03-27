import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
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
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetFooter,
  SheetClose,
} from '@/components/ui/sheet'
import { Switch } from '@/components/ui/switch'
import { ConfirmDialog } from '@/components/crud/ConfirmDialog'
import { IconPicker, getIconComponent } from '@/components/crud/IconPicker'
import { ComboboxField } from '@/components/crud/ComboboxField'
import { SearchBar } from '@/components/crud/SearchBar'
import { Plus, ChevronDown, ChevronRight, Pencil, Trash2 } from 'lucide-react'
import api from '@/lib/api'
import axios from 'axios'
import { loadCostCenterOptions } from '@/lib/costcenter-options'
import type { CategoryDto, SubcategoryDto } from '@/types/api'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrió un error inesperado'
}

interface CategoryForm {
  name: string
  icon: string
}

interface SubcategoryForm {
  name: string
  subcategoryType: string
  isActive: boolean
  suggestedAccountingType: string
  suggestedCostCenterId: string
  isOrdinary: string
  newCategoryId: string
}

const defaultCatForm: CategoryForm = { name: '', icon: '' }
const defaultSubForm: SubcategoryForm = {
  name: '',
  subcategoryType: 'Expense',
  isActive: true,
  suggestedAccountingType: '',
  suggestedCostCenterId: '',
  isOrdinary: '',
  newCategoryId: '',
}

export default function CategoriesPage() {
  const { t } = useTranslation()
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [costCenterMap, setCostCenterMap] = useState<Record<string, string>>({})
  const [search, setSearch] = useState('')
  const [expanded, setExpanded] = useState<Set<string>>(new Set())

  // Category drawer
  const [catDrawerOpen, setCatDrawerOpen] = useState(false)
  const [editingCat, setEditingCat] = useState<CategoryDto | null>(null)
  const [catForm, setCatForm] = useState<CategoryForm>(defaultCatForm)
  const [savingCat, setSavingCat] = useState(false)

  // Subcategory drawer
  const [subDrawerOpen, setSubDrawerOpen] = useState(false)
  const [editingSub, setEditingSub] = useState<SubcategoryDto | null>(null)
  const [subCategoryId, setSubCategoryId] = useState<string>('')
  const [subForm, setSubForm] = useState<SubcategoryForm>(defaultSubForm)
  const [savingSub, setSavingSub] = useState(false)

  // Delete
  const [deleteTarget, setDeleteTarget] = useState<{ type: 'category' | 'subcategory'; id: string; categoryId?: string } | null>(null)

  const load = useCallback(() => {
    api.get<CategoryDto[]>('/categories').then((r) => setCategories(r.data))
    loadCostCenterOptions().then((opts) => {
      const map: Record<string, string> = {}
      for (const o of opts) map[o.value] = o.label
      setCostCenterMap(map)
    })
  }, [])

  useEffect(() => { load() }, [load])

  const toggleExpand = (id: string) => {
    setExpanded((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const filtered = categories.filter((cat) => {
    if (!search.trim()) return true
    const q = search.toLowerCase()
    return (
      cat.name.toLowerCase().includes(q) ||
      cat.subcategories.some((s) => s.name.toLowerCase().includes(q))
    )
  })

  // Category CRUD
  const openCreateCat = () => {
    setEditingCat(null)
    setCatForm({ ...defaultCatForm })
    setCatDrawerOpen(true)
  }

  const openEditCat = (cat: CategoryDto) => {
    setEditingCat(cat)
    setCatForm({ name: cat.name, icon: cat.icon ?? '' })
    setCatDrawerOpen(true)
  }

  const saveCat = async () => {
    if (!catForm.name.trim()) {
      toast.error(t('categories.toast.nameRequired'))
      return
    }
    setSavingCat(true)
    try {
      if (editingCat) {
        await api.put(`/categories/${editingCat.categoryId}`, {
          name: catForm.name,
          icon: catForm.icon || null,
        })
        toast.success(t('categories.toast.categoryUpdated'))
      } else {
        await api.post('/categories', {
          name: catForm.name,
          icon: catForm.icon || null,
        })
        toast.success(t('categories.toast.categoryCreated'))
      }
      setCatDrawerOpen(false)
      load()
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSavingCat(false)
    }
  }

  // Subcategory CRUD
  const openCreateSub = (categoryId: string) => {
    setEditingSub(null)
    setSubCategoryId(categoryId)
    setSubForm({ ...defaultSubForm })
    setSubDrawerOpen(true)
  }

  const openEditSub = (categoryId: string, sub: SubcategoryDto) => {
    setEditingSub(sub)
    setSubCategoryId(categoryId)
    setSubForm({
      name: sub.name,
      subcategoryType: sub.subcategoryType,
      isActive: sub.isActive,
      suggestedAccountingType: sub.suggestedAccountingType ?? '',
      suggestedCostCenterId: sub.suggestedCostCenterId ?? '',
      isOrdinary: sub.isOrdinary === null ? '' : sub.isOrdinary ? 'true' : 'false',
      newCategoryId: categoryId,
    })
    setSubDrawerOpen(true)
  }

  const saveSub = async () => {
    if (!subForm.name.trim()) {
      toast.error(t('categories.toast.nameRequired'))
      return
    }
    setSavingSub(true)
    try {
      const payload = {
        name: subForm.name,
        subcategoryType: subForm.subcategoryType,
        isActive: subForm.isActive,
        suggestedAccountingType: subForm.suggestedAccountingType || null,
        suggestedCostCenterId: subForm.suggestedCostCenterId || null,
        isOrdinary: subForm.isOrdinary === '' ? null : subForm.isOrdinary === 'true',
        newCategoryId: editingSub && subForm.newCategoryId !== subCategoryId ? subForm.newCategoryId || null : null,
      }
      if (editingSub) {
        await api.put(`/categories/${subCategoryId}/subcategories/${editingSub.subcategoryId}`, payload)
        toast.success(t('categories.toast.subcategoryUpdated'))
      } else {
        await api.post(`/categories/${subCategoryId}/subcategories`, payload)
        toast.success(t('categories.toast.subcategoryCreated'))
      }
      setSubDrawerOpen(false)
      load()
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSavingSub(false)
    }
  }

  // Delete
  const handleDelete = async () => {
    if (!deleteTarget) return
    try {
      if (deleteTarget.type === 'category') {
        await api.delete(`/categories/${deleteTarget.id}`)
        toast.success(t('categories.toast.categoryDeleted'))
      } else {
        await api.delete(`/categories/${deleteTarget.categoryId}/subcategories/${deleteTarget.id}`)
        toast.success(t('categories.toast.subcategoryDeleted'))
      }
      setDeleteTarget(null)
      load()
    } catch (err) {
      toast.error(extractError(err))
      setDeleteTarget(null)
    }
  }

  const typeLabel = (type: string) => {
    if (type === 'Income') return t('categories.badges.income')
    if (type === 'Expense') return t('categories.badges.expense')
    return t('categories.badges.both')
  }

  const typeBadgeVariant = (type: string): 'default' | 'secondary' | 'outline' => {
    if (type === 'Income') return 'default'
    if (type === 'Expense') return 'secondary'
    return 'outline'
  }

  const accountingTypeLabel = (type: string | null) => {
    if (!type) return null
    const map: Record<string, string> = {
      Asset: t('categories.badges.asset'),
      Liability: t('categories.badges.liability'),
      Income: t('categories.badges.income'),
      Expense: t('categories.badges.expense'),
    }
    return map[type] ?? type
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">{t('categories.title')}</h1>
        <Button onClick={openCreateCat}>
          <Plus className="mr-2 h-4 w-4" />
          {t('categories.new')}
        </Button>
      </div>

      <SearchBar value={search} onChange={setSearch} />

      <div className="space-y-2">
        {filtered.map((cat) => {
          const isExpanded = expanded.has(cat.categoryId)
          const CatIcon = getIconComponent(cat.icon)

          return (
            <Card key={cat.categoryId}>
              <CardContent className="p-0">
                {/* Category row */}
                <div className="flex items-center gap-2 px-4 py-3">
                  <button
                    type="button"
                    onClick={() => toggleExpand(cat.categoryId)}
                    className="flex h-6 w-6 shrink-0 items-center justify-center rounded hover:bg-muted"
                  >
                    {isExpanded ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </button>

                  {CatIcon && <CatIcon className="h-4 w-4 text-muted-foreground" />}

                  <span className="font-medium">{cat.name}</span>

                  <Badge variant="outline" className="ml-2 text-xs">
                    {cat.subcategories.length} sub
                  </Badge>

                  <div className="flex-1" />

                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7"
                    onClick={() => openEditCat(cat)}
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7 text-destructive hover:text-destructive"
                    onClick={() => setDeleteTarget({ type: 'category', id: cat.categoryId })}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>

                {/* Subcategories */}
                {isExpanded && (
                  <div className="border-t bg-muted/30 px-4 py-2">
                    {cat.subcategories.length === 0 ? (
                      <p className="py-2 text-sm text-muted-foreground">{t('categories.noSubcategories')}</p>
                    ) : (
                      <div className="space-y-1">
                        {cat.subcategories.map((sub) => (
                          <div
                            key={sub.subcategoryId}
                            className="flex items-center gap-2 rounded px-2 py-1.5 text-sm hover:bg-muted/50"
                          >
                            <span className={!sub.isActive ? 'text-muted-foreground' : ''}>{sub.name}</span>
                            {!sub.isActive && (
                              <Badge variant="outline" className="text-xs text-muted-foreground">{t('common.inactive')}</Badge>
                            )}
                            <Badge variant={typeBadgeVariant(sub.subcategoryType)} className="text-xs">
                              {typeLabel(sub.subcategoryType)}
                            </Badge>
                            {sub.suggestedAccountingType && (
                              <span className="text-xs text-muted-foreground">
                                {accountingTypeLabel(sub.suggestedAccountingType)}
                              </span>
                            )}
                            {sub.suggestedCostCenterId && (
                              <Badge variant="outline" className="text-xs font-normal">
                                {costCenterMap[sub.suggestedCostCenterId] ?? 'Centro de costo'}
                              </Badge>
                            )}
                            {sub.isOrdinary !== null && (
                              <Badge variant={sub.isOrdinary ? 'default' : 'secondary'} className="text-xs">
                                {sub.isOrdinary ? t('categories.badges.ordinary') : t('categories.badges.extraordinary')}
                              </Badge>
                            )}
                            <div className="flex-1" />
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-6 w-6"
                              onClick={() => openEditSub(cat.categoryId, sub)}
                            >
                              <Pencil className="h-3 w-3" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-6 w-6 text-destructive hover:text-destructive"
                              onClick={() => setDeleteTarget({ type: 'subcategory', id: sub.subcategoryId, categoryId: cat.categoryId })}
                            >
                              <Trash2 className="h-3 w-3" />
                            </Button>
                          </div>
                        ))}
                      </div>
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      className="mt-1 h-7 text-xs"
                      onClick={() => openCreateSub(cat.categoryId)}
                    >
                      <Plus className="mr-1 h-3 w-3" />
                      {t('categories.addSubcategory')}
                    </Button>
                  </div>
                )}
              </CardContent>
            </Card>
          )
        })}

        {filtered.length === 0 && (
          <p className="py-8 text-center text-muted-foreground">
            {search ? t('categories.noResults') : t('categories.noCategories')}
          </p>
        )}
      </div>

      {/* Category drawer */}
      <Sheet open={catDrawerOpen} onOpenChange={setCatDrawerOpen}>
        <SheetContent side="right" className="flex flex-col sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{editingCat ? t('categories.editCategory') : t('categories.newCategory')}</SheetTitle>
            <SheetDescription className="sr-only">Formulario de categoría</SheetDescription>
          </SheetHeader>
          <div className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
            <div className="space-y-1.5">
              <Label>{t('categories.fields.name')}</Label>
              <Input
                value={catForm.name}
                onChange={(e) => setCatForm((p) => ({ ...p, name: e.target.value }))}
                placeholder={t('categories.fields.nameCategoryPlaceholder')}
                maxLength={100}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('categories.fields.icon')}</Label>
              <IconPicker
                value={catForm.icon}
                onChange={(icon) => setCatForm((p) => ({ ...p, icon }))}
              />
            </div>
          </div>
          <SheetFooter className="flex-row gap-2 border-t pt-4">
            <SheetClose render={<Button variant="outline" className="flex-1" />}>
              {t('common.cancel')}
            </SheetClose>
            <Button className="flex-1" disabled={savingCat} onClick={saveCat}>
              {savingCat ? t('common.saving') : t('common.save')}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      {/* Subcategory drawer */}
      <Sheet open={subDrawerOpen} onOpenChange={setSubDrawerOpen}>
        <SheetContent side="right" className="flex flex-col sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{editingSub ? t('categories.editSubcategory') : t('categories.newSubcategory')}</SheetTitle>
            <SheetDescription className="sr-only">Formulario de subcategoría</SheetDescription>
          </SheetHeader>
          <div className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
            {editingSub && (
              <div className="space-y-1.5">
                <Label>{t('categories.fields.parentCategory')}</Label>
                <Select
                  value={subForm.newCategoryId}
                  onValueChange={(v) => setSubForm((p) => ({ ...p, newCategoryId: v ?? p.newCategoryId }))}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue>
                      {categories.find((c) => c.categoryId === subForm.newCategoryId)?.name ?? t('categories.fields.selectCategory')}
                    </SelectValue>
                  </SelectTrigger>
                  <SelectContent>
                    {categories.map((c) => (
                      <SelectItem key={c.categoryId} value={c.categoryId}>{c.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
            <div className="space-y-1.5">
              <Label>{t('categories.fields.name')}</Label>
              <Input
                value={subForm.name}
                onChange={(e) => setSubForm((p) => ({ ...p, name: e.target.value }))}
                placeholder={t('categories.fields.nameSubcategoryPlaceholder')}
                maxLength={100}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('categories.fields.type')}</Label>
              <Select
                value={subForm.subcategoryType}
                onValueChange={(v) => setSubForm((p) => ({ ...p, subcategoryType: v ?? p.subcategoryType }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder={t('categories.fields.selectType')}>
                    {{
                      Income: t('categories.fields.income'),
                      Expense: t('categories.fields.expense'),
                      Both: t('categories.fields.both'),
                    }[subForm.subcategoryType] ?? subForm.subcategoryType}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Income">{t('categories.fields.income')}</SelectItem>
                  <SelectItem value="Expense">{t('categories.fields.expense')}</SelectItem>
                  <SelectItem value="Both">{t('categories.fields.both')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>{t('categories.fields.accountingType')}</Label>
              <Select
                value={subForm.suggestedAccountingType || '_none_'}
                onValueChange={(v) => setSubForm((p) => ({ ...p, suggestedAccountingType: v == null ? p.suggestedAccountingType : v === '_none_' ? '' : v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue className={!subForm.suggestedAccountingType ? 'text-muted-foreground' : ''} placeholder={t('categories.fields.noSuggestion')}>
                    {{
                      _none_: t('categories.fields.noSuggestion'),
                      Asset: t('categories.fields.asset'),
                      Liability: t('categories.fields.liability'),
                      Income: t('categories.fields.income'),
                      Expense: t('categories.fields.expense'),
                    }[subForm.suggestedAccountingType || '_none_'] ?? t('categories.fields.noSuggestion')}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="_none_">{t('categories.fields.noSuggestion')}</SelectItem>
                  <SelectItem value="Asset">{t('categories.fields.asset')}</SelectItem>
                  <SelectItem value="Liability">{t('categories.fields.liability')}</SelectItem>
                  <SelectItem value="Income">{t('categories.fields.income')}</SelectItem>
                  <SelectItem value="Expense">{t('categories.fields.expense')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>{t('categories.fields.costCenter')}</Label>
              <ComboboxField
                id="suggestedCostCenterId"
                value={subForm.suggestedCostCenterId}
                onChange={(v) => setSubForm((p) => ({ ...p, suggestedCostCenterId: v }))}
                loadOptions={loadCostCenterOptions}
                placeholder={t('categories.fields.noSuggestion')}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('categories.fields.character')}</Label>
              <Select
                value={subForm.isOrdinary || '_none_'}
                onValueChange={(v) => setSubForm((p) => ({ ...p, isOrdinary: v === '_none_' ? '' : v }))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue className={!subForm.isOrdinary ? 'text-muted-foreground' : ''} placeholder={t('categories.fields.noSuggestion')}>
                    {{
                      _none_: t('categories.fields.noSuggestion'),
                      true: t('categories.fields.ordinary'),
                      false: t('categories.fields.extraordinary'),
                    }[subForm.isOrdinary || '_none_'] ?? t('categories.fields.noSuggestion')}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="_none_">{t('categories.fields.noSuggestion')}</SelectItem>
                  <SelectItem value="true">{t('categories.fields.ordinary')}</SelectItem>
                  <SelectItem value="false">{t('categories.fields.extraordinary')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {editingSub && (
              <div className="flex items-center gap-3">
                <Switch
                  id="subIsActive"
                  checked={subForm.isActive}
                  onCheckedChange={(v) => setSubForm((p) => ({ ...p, isActive: v }))}
                />
                <Label htmlFor="subIsActive">{t('common.active')}</Label>
              </div>
            )}
          </div>
          <SheetFooter className="flex-row gap-2 border-t pt-4">
            <SheetClose render={<Button variant="outline" className="flex-1" />}>
              {t('common.cancel')}
            </SheetClose>
            <Button className="flex-1" disabled={savingSub} onClick={saveSub}>
              {savingSub ? t('common.saving') : t('common.save')}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      {/* Delete confirmation */}
      <ConfirmDialog
        open={deleteTarget !== null}
        onOpenChange={(open) => { if (!open) setDeleteTarget(null) }}
        onConfirm={handleDelete}
      />
    </div>
  )
}
