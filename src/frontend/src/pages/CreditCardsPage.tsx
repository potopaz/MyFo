import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetFooter,
  SheetClose,
} from '@/components/ui/sheet'
import { ConfirmDialog } from '@/components/crud/ConfirmDialog'
import { ComboboxField } from '@/components/crud/ComboboxField'
import { SearchBar } from '@/components/crud/SearchBar'
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
import { Plus, ChevronDown, ChevronRight, Pencil, Trash2, HelpCircle } from 'lucide-react'
import api from '@/lib/api'
import axios from 'axios'
import { loadFamilyCurrencyOptions } from '@/lib/currency-options'
import type { CreditCardDto, CreditCardMemberDto } from '@/types/api'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrió un error inesperado'
}

interface CardForm {
  name: string
  currencyCode: string
  isActive: boolean
}

interface MemberForm {
  holderName: string
  lastFourDigits: string
  isPrimary: boolean
  isActive: boolean
  expirationMonth: string
  expirationYear: string
  memberId: string
}

const defaultCardForm: CardForm = { name: '', currencyCode: '', isActive: true }
const defaultMemberForm: MemberForm = {
  holderName: '', lastFourDigits: '', isPrimary: false, isActive: true,
  expirationMonth: '', expirationYear: '', memberId: '',
}

const MONTHS = [
  { value: '1', label: '01 - Enero' },
  { value: '2', label: '02 - Febrero' },
  { value: '3', label: '03 - Marzo' },
  { value: '4', label: '04 - Abril' },
  { value: '5', label: '05 - Mayo' },
  { value: '6', label: '06 - Junio' },
  { value: '7', label: '07 - Julio' },
  { value: '8', label: '08 - Agosto' },
  { value: '9', label: '09 - Septiembre' },
  { value: '10', label: '10 - Octubre' },
  { value: '11', label: '11 - Noviembre' },
  { value: '12', label: '12 - Diciembre' },
]

export default function CreditCardsPage() {
  const { t } = useTranslation()
  const [cards, setCards] = useState<CreditCardDto[]>([])
  const [search, setSearch] = useState('')
  const [expanded, setExpanded] = useState<Set<string>>(new Set())

  // Card drawer
  const [cardDrawerOpen, setCardDrawerOpen] = useState(false)
  const [editingCard, setEditingCard] = useState<CreditCardDto | null>(null)
  const [cardForm, setCardForm] = useState<CardForm>(defaultCardForm)
  const [savingCard, setSavingCard] = useState(false)

  // Member drawer
  const [memberDrawerOpen, setMemberDrawerOpen] = useState(false)
  const [editingMember, setEditingMember] = useState<CreditCardMemberDto | null>(null)
  const [memberCardId, setMemberCardId] = useState<string>('')
  const [memberForm, setMemberForm] = useState<MemberForm>(defaultMemberForm)
  const [savingMember, setSavingMember] = useState(false)
  const [familyMembers, setFamilyMembers] = useState<{ memberId: string; displayName: string }[]>([])

  // Delete
  const [deleteTarget, setDeleteTarget] = useState<
    | { type: 'card'; cardId: string }
    | { type: 'member'; cardId: string; memberId: string }
    | null
  >(null)

  const load = useCallback(() => {
    api.get<CreditCardDto[]>('/creditcards').then((r) => setCards(r.data))
  }, [])

  useEffect(() => { load() }, [load])
  useEffect(() => {
    api.get<{ memberId: string; displayName: string }[]>('/family-members').then((r) => setFamilyMembers(r.data))
  }, [])

  const toggleExpand = (id: string) => {
    setExpanded((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const filtered = cards.filter((card) => {
    if (!search.trim()) return true
    const q = search.toLowerCase()
    return (
      card.name.toLowerCase().includes(q) ||
      card.currencyCode.toLowerCase().includes(q) ||
      card.members.some((m) => m.holderName.toLowerCase().includes(q))
    )
  })

  // ── Card CRUD ────────────────────────────────────────────────────────────────

  const openCreateCard = () => {
    setEditingCard(null)
    setCardForm({ ...defaultCardForm })
    setCardDrawerOpen(true)
  }

  const openEditCard = (card: CreditCardDto) => {
    setEditingCard(card)
    setCardForm({ name: card.name, currencyCode: card.currencyCode, isActive: card.isActive })
    setCardDrawerOpen(true)
  }

  const saveCard = async () => {
    if (!cardForm.name.trim()) { toast.error(t('creditCards.toast.nameRequired')); return }
    if (!cardForm.currencyCode) { toast.error(t('creditCards.toast.currencyRequired')); return }
    setSavingCard(true)
    try {
      if (editingCard) {
        await api.put(`/creditcards/${editingCard.creditCardId}`, {
          name: cardForm.name,
          currencyCode: cardForm.currencyCode,
          isActive: cardForm.isActive,
        })
        toast.success(t('creditCards.toast.cardUpdated'))
      } else {
        const res = await api.post<CreditCardDto>('/creditcards', {
          name: cardForm.name,
          currencyCode: cardForm.currencyCode,
          members: [],
        })
        // Auto-expand newly created card
        setExpanded((prev) => new Set([...prev, res.data.creditCardId]))
        toast.success(t('creditCards.toast.cardCreated'))
      }
      setCardDrawerOpen(false)
      load()
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSavingCard(false)
    }
  }

  // ── Member CRUD ──────────────────────────────────────────────────────────────

  const openCreateMember = (cardId: string) => {
    setEditingMember(null)
    setMemberCardId(cardId)
    setMemberForm({ ...defaultMemberForm })
    setMemberDrawerOpen(true)
  }

  const openEditMember = (cardId: string, member: CreditCardMemberDto) => {
    setEditingMember(member)
    setMemberCardId(cardId)
    setMemberForm({
      holderName: member.holderName,
      lastFourDigits: member.lastFourDigits ?? '',
      isPrimary: member.isPrimary,
      isActive: member.isActive,
      expirationMonth: member.expirationMonth != null ? String(member.expirationMonth) : '',
      expirationYear: member.expirationYear != null ? String(member.expirationYear) : '',
      memberId: member.memberId ?? '',
    })
    setMemberDrawerOpen(true)
  }

  const saveMember = async () => {
    if (!memberForm.holderName.trim()) { toast.error(t('creditCards.toast.holderNameRequired')); return }
    if (memberForm.lastFourDigits && !/^\d{1,4}$/.test(memberForm.lastFourDigits)) {
      toast.error(t('creditCards.toast.lastFourDigitsInvalid'))
      return
    }
    if (memberForm.expirationYear && !/^\d{4}$/.test(memberForm.expirationYear)) {
      toast.error(t('creditCards.toast.expirationYearInvalid'))
      return
    }
    if ((memberForm.expirationMonth && !memberForm.expirationYear) ||
        (!memberForm.expirationMonth && memberForm.expirationYear)) {
      toast.error(t('creditCards.toast.expirationBothRequired'))
      return
    }
    // Frontend check: only one primary per card
    if (memberForm.isPrimary) {
      const card = cards.find((c) => c.creditCardId === memberCardId)
      const existingPrimary = card?.members.find(
        (m) => m.isPrimary && m.creditCardMemberId !== editingMember?.creditCardMemberId
      )
      if (existingPrimary) {
        toast.error(t('creditCards.toast.duplicatePrimary', { name: existingPrimary.holderName }))
        return
      }
    }
    setSavingMember(true)
    try {
      const expirationMonth = memberForm.expirationMonth ? parseInt(memberForm.expirationMonth) : null
      const expirationYear = memberForm.expirationYear ? parseInt(memberForm.expirationYear) : null
      const memberId = memberForm.memberId || null
      if (editingMember) {
        await api.put(`/creditcards/${memberCardId}/members/${editingMember.creditCardMemberId}`, {
          holderName: memberForm.holderName,
          lastFourDigits: memberForm.lastFourDigits || null,
          isPrimary: memberForm.isPrimary,
          isActive: memberForm.isActive,
          expirationMonth,
          expirationYear,
          memberId,
        })
        toast.success(t('creditCards.toast.memberUpdated'))
      } else {
        await api.post(`/creditcards/${memberCardId}/members`, {
          holderName: memberForm.holderName,
          lastFourDigits: memberForm.lastFourDigits || null,
          isPrimary: memberForm.isPrimary,
          expirationMonth,
          expirationYear,
          memberId,
        })
        toast.success(t('creditCards.toast.memberCreated'))
      }
      setMemberDrawerOpen(false)
      load()
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSavingMember(false)
    }
  }

  // ── Delete ───────────────────────────────────────────────────────────────────

  const handleDelete = async () => {
    if (!deleteTarget) return
    try {
      if (deleteTarget.type === 'card') {
        await api.delete(`/creditcards/${deleteTarget.cardId}`)
        toast.success(t('creditCards.toast.cardDeleted'))
      } else {
        await api.delete(`/creditcards/${deleteTarget.cardId}/members/${deleteTarget.memberId}`)
        toast.success(t('creditCards.toast.memberDeleted'))
      }
      setDeleteTarget(null)
      load()
    } catch (err) {
      toast.error(extractError(err))
      setDeleteTarget(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold flex items-center gap-1">
          {t('creditCards.title')}
          <Tooltip>
            <TooltipTrigger>
              <HelpCircle className="h-3.5 w-3.5 ml-0.5 text-muted-foreground cursor-help hover:text-foreground transition-colors" />
            </TooltipTrigger>
            <TooltipContent>{t('creditCards.tooltip')}</TooltipContent>
          </Tooltip>
        </h1>
        <Button onClick={openCreateCard}>
          <Plus className="mr-2 h-4 w-4" />
          {t('creditCards.new')}
        </Button>
      </div>

      <SearchBar value={search} onChange={setSearch} />

      <div className="space-y-2">
        {filtered.map((card) => {
          const isExpanded = expanded.has(card.creditCardId)
          return (
            <Card key={card.creditCardId}>
              <CardContent className="p-0">
                {/* Card row */}
                <div className="flex items-center gap-2 px-4 py-3">
                  <button
                    type="button"
                    onClick={() => toggleExpand(card.creditCardId)}
                    className="flex h-6 w-6 shrink-0 items-center justify-center rounded hover:bg-muted"
                  >
                    {isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                  </button>

                  <span className="font-medium">{card.name}</span>
                  <Badge variant="outline" className="text-xs">{card.currencyCode}</Badge>
                  <Badge variant="outline" className="text-xs">
                    {card.members.length} {card.members.length === 1 ? t('creditCards.memberSingular') : t('creditCards.memberPlural')}
                  </Badge>
                  <Badge variant={card.isActive ? 'default' : 'secondary'} className="text-xs">
                    {card.isActive ? t('creditCards.active') : t('creditCards.inactive')}
                  </Badge>

                  <div className="flex-1" />

                  <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => openEditCard(card)}>
                    <Pencil className="h-3.5 w-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7 text-destructive hover:text-destructive"
                    onClick={() => setDeleteTarget({ type: 'card', cardId: card.creditCardId })}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>

                {/* Members */}
                {isExpanded && (
                  <div className="border-t bg-muted/30 px-4 py-2">
                    {card.members.length === 0 ? (
                      <p className="py-2 text-sm text-muted-foreground">{t('creditCards.noMembers')}</p>
                    ) : (
                      <div className="space-y-1">
                        {card.members.map((member) => (
                          <div
                            key={member.creditCardMemberId}
                            className="flex items-center gap-2 rounded px-2 py-1.5 text-sm hover:bg-muted/50"
                          >
                            <span>{member.holderName}</span>
                            {member.lastFourDigits && (
                              <span className="text-xs text-muted-foreground">···· {member.lastFourDigits}</span>
                            )}
                            {member.expirationMonth != null && member.expirationYear != null && (
                              <span className="text-xs text-muted-foreground">
                                {String(member.expirationMonth).padStart(2, '0')}/{member.expirationYear}
                              </span>
                            )}
                            {member.isPrimary && (
                              <Badge variant="default" className="text-xs">{t('creditCards.primary')}</Badge>
                            )}
                            {!member.isActive && (
                              <Badge variant="secondary" className="text-xs">{t('creditCards.inactive')}</Badge>
                            )}
                            <div className="flex-1" />
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-6 w-6"
                              onClick={() => openEditMember(card.creditCardId, member)}
                            >
                              <Pencil className="h-3 w-3" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-6 w-6 text-destructive hover:text-destructive"
                              onClick={() => setDeleteTarget({ type: 'member', cardId: card.creditCardId, memberId: member.creditCardMemberId })}
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
                      onClick={() => openCreateMember(card.creditCardId)}
                    >
                      <Plus className="mr-1 h-3 w-3" />
                      {t('creditCards.addMember')}
                    </Button>
                  </div>
                )}
              </CardContent>
            </Card>
          )
        })}

        {filtered.length === 0 && (
          <p className="py-8 text-center text-muted-foreground">
            {search ? t('creditCards.noResults') : t('creditCards.noCards')}
          </p>
        )}
      </div>

      {/* Card drawer */}
      <Sheet open={cardDrawerOpen} onOpenChange={setCardDrawerOpen}>
        <SheetContent side="right" className="flex flex-col sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{editingCard ? t('creditCards.editTitle') : t('creditCards.createTitle')}</SheetTitle>
            <SheetDescription className="sr-only">Formulario de tarjeta de crédito</SheetDescription>
          </SheetHeader>
          <div className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
            <div className="space-y-1.5">
              <Label>{t('creditCards.fields.name')}</Label>
              <Input
                value={cardForm.name}
                onChange={(e) => setCardForm((p) => ({ ...p, name: e.target.value }))}
                placeholder={t('creditCards.fields.namePlaceholder')}
                maxLength={100}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('creditCards.fields.currency')}</Label>
              <ComboboxField
                id="currencyCode"
                value={cardForm.currencyCode}
                onChange={(v) => setCardForm((p) => ({ ...p, currencyCode: v }))}
                loadOptions={loadFamilyCurrencyOptions}
                placeholder={t('creditCards.fields.searchCurrency')}
              />
            </div>
            {editingCard && (
              <div className="flex items-center gap-3">
                <Switch
                  id="card-isActive"
                  checked={cardForm.isActive}
                  onCheckedChange={(v) => setCardForm((p) => ({ ...p, isActive: v }))}
                />
                <Label htmlFor="card-isActive">{t('creditCards.fields.isActive')}</Label>
              </div>
            )}
          </div>
          <SheetFooter className="flex-row gap-2 border-t pt-4">
            <SheetClose render={<Button variant="outline" className="flex-1" />}>
              {t('common.cancel')}
            </SheetClose>
            <Button className="flex-1" disabled={savingCard} onClick={saveCard}>
              {savingCard ? t('common.saving') : t('common.save')}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      {/* Member drawer */}
      <Sheet open={memberDrawerOpen} onOpenChange={setMemberDrawerOpen}>
        <SheetContent side="right" className="flex flex-col sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{editingMember ? t('creditCards.editMember') : t('creditCards.newMember')}</SheetTitle>
            <SheetDescription className="sr-only">Formulario de miembro de tarjeta</SheetDescription>
          </SheetHeader>
          <div className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
            <div className="space-y-1.5">
              <Label>{t('creditCards.fields.holderName')}</Label>
              <Input
                value={memberForm.holderName}
                onChange={(e) => setMemberForm((p) => ({ ...p, holderName: e.target.value }))}
                placeholder={t('creditCards.fields.holderNamePlaceholder')}
                maxLength={100}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('creditCards.fields.familyMember')}</Label>
              <Select
                value={memberForm.memberId || '_none_'}
                onValueChange={(v) => setMemberForm((p) => ({ ...p, memberId: v === '_none_' ? '' : v }))}
              >
                <SelectTrigger>
                  <SelectValue>
                    {memberForm.memberId
                      ? (familyMembers.find((m) => m.memberId === memberForm.memberId)?.displayName ?? '—')
                      : t('common.optional')}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="_none_">{t('common.optional')}</SelectItem>
                  {familyMembers.map((m) => (
                    <SelectItem key={m.memberId} value={m.memberId}>{m.displayName}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>{t('creditCards.fields.lastFourDigits')}</Label>
              <Input
                value={memberForm.lastFourDigits}
                onChange={(e) => {
                  if (/^\d{0,4}$/.test(e.target.value)) setMemberForm((p) => ({ ...p, lastFourDigits: e.target.value }))
                }}
                inputMode="numeric"
                placeholder={t('common.optional')}
                maxLength={4}
              />
            </div>
            <div className="space-y-1.5">
              <Label>{t('creditCards.fields.expiration')}</Label>
              <div className="grid grid-cols-2 gap-2">
                <Select
                  value={memberForm.expirationMonth}
                  onValueChange={(v) => setMemberForm((p) => ({ ...p, expirationMonth: v }))}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('creditCards.fields.expirationMonthPlaceholder')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="">{t('common.optional')}</SelectItem>
                    {MONTHS.map((m) => (
                      <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Input
                  value={memberForm.expirationYear}
                  onChange={(e) => {
                    if (/^\d{0,4}$/.test(e.target.value)) setMemberForm((p) => ({ ...p, expirationYear: e.target.value }))
                  }}
                  inputMode="numeric"
                  placeholder={t('creditCards.fields.expirationYearPlaceholder')}
                  maxLength={4}
                />
              </div>
            </div>
            <div className="flex items-center gap-3">
              <Switch
                id="member-isPrimary"
                checked={memberForm.isPrimary}
                onCheckedChange={(v) => setMemberForm((p) => ({ ...p, isPrimary: v }))}
              />
              <Label htmlFor="member-isPrimary">{t('creditCards.fields.isPrimary')}</Label>
            </div>
            {editingMember && (
              <div className="flex items-center gap-3">
                <Switch
                  id="member-isActive"
                  checked={memberForm.isActive}
                  onCheckedChange={(v) => setMemberForm((p) => ({ ...p, isActive: v }))}
                />
                <Label htmlFor="member-isActive">{t('creditCards.fields.isActive')}</Label>
              </div>
            )}
          </div>
          <SheetFooter className="flex-row gap-2 border-t pt-4">
            <SheetClose render={<Button variant="outline" className="flex-1" />}>
              {t('common.cancel')}
            </SheetClose>
            <Button className="flex-1" disabled={savingMember} onClick={saveMember}>
              {savingMember ? t('common.saving') : t('common.save')}
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
