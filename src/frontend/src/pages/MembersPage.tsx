import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetFooter,
  SheetClose,
} from '@/components/ui/sheet'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/crud/ConfirmDialog'
import { Pencil, Plus, Send } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import api from '@/lib/api'
import axios from 'axios'
import type { CreateInvitationResponse } from '@/types/api'

function extractError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data
    if (data?.message) return data.message
  }
  return 'Ocurrió un error inesperado'
}

function formatDate(dateISO: string): string {
  const d = new Date(dateISO)
  return d.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

interface MemberDto {
  memberId: string
  userId: string
  displayName: string
  role: string
  isActive: boolean
  createdAt: string
}

interface InvitationDto {
  invitationId: string
  invitedByDisplayName: string
  invitedEmail: string
  expiresAt: string
  createdAt: string
}

export default function MembersPage() {
  const { t } = useTranslation()
  const { userId } = useAuth()

  const [members, setMembers] = useState<MemberDto[]>([])
  const [invitations, setInvitations] = useState<InvitationDto[]>([])
  const [loading, setLoading] = useState(true)

  // Edit sheet
  const [editingMember, setEditingMember] = useState<MemberDto | null>(null)
  const [editRole, setEditRole] = useState('')
  const [editActive, setEditActive] = useState(true)
  const [saving, setSaving] = useState(false)

  // Invite dialog
  const [inviteOpen, setInviteOpen] = useState(false)
  const [inviteEmail, setInviteEmail] = useState('')
  const [sendingInvite, setSendingInvite] = useState(false)

  // Confirm dialog
  const [confirmAction, setConfirmAction] = useState<{
    message: string
    onConfirm: () => Promise<void>
  } | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [membersRes, invitationsRes] = await Promise.all([
        api.get<MemberDto[]>('/family-members'),
        api.get<InvitationDto[]>('/family-members/invitations'),
      ])
      setMembers(membersRes.data)
      setInvitations(invitationsRes.data)
    } catch {
      toast.error(t('members.toast.loadError'))
    } finally {
      setLoading(false)
    }
  }, [t])

  useEffect(() => { load() }, [load])

  const openEdit = (m: MemberDto) => {
    setEditingMember(m)
    setEditRole(m.role)
    setEditActive(m.isActive)
  }

  const closeEdit = () => {
    setEditingMember(null)
  }

  const executeSave = async () => {
    if (!editingMember) return
    setSaving(true)
    try {
      const roleChanged = editRole !== editingMember.role
      const activeChanged = editActive !== editingMember.isActive

      if (activeChanged) {
        await api.put(`/family-members/${editingMember.memberId}/toggle-active`)
      }
      if (roleChanged) {
        await api.put(`/family-members/${editingMember.memberId}/role`, { role: editRole })
      }
      toast.success(t('members.toast.memberUpdated'))
      closeEdit()
      load()
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSaving(false)
    }
  }

  const handleSaveEdit = () => {
    if (!editingMember) return

    const roleChanged = editRole !== editingMember.role
    const activeChanged = editActive !== editingMember.isActive

    if (!roleChanged && !activeChanged) {
      closeEdit()
      return
    }

    // Check if any change is destructive (deactivate or demote)
    const isDeactivating = activeChanged && !editActive
    const isDemoting = roleChanged && editRole === 'Member' && editingMember.role === 'FamilyAdmin'

    if (isDeactivating || isDemoting) {
      const messages: string[] = []
      if (isDeactivating) messages.push(t('members.confirmDeactivate'))
      if (isDemoting) messages.push(t('members.confirmDemote'))

      setConfirmAction({
        message: messages.join('\n\n'),
        onConfirm: executeSave,
      })
      return
    }

    executeSave()
  }

  const handleSendInvite = async () => {
    const email = inviteEmail.trim()
    if (!email) {
      toast.error(t('members.toast.emailRequired'))
      return
    }
    setSendingInvite(true)
    try {
      const { data } = await api.post<CreateInvitationResponse>('/invitations', { email, baseUrl: window.location.origin })
      const link = `${window.location.origin}/join?token=${data.token}`
      await navigator.clipboard.writeText(link)
      toast.success(t('members.toast.inviteSent'))
      setInviteOpen(false)
      setInviteEmail('')
      load()
    } catch (err) {
      toast.error(extractError(err))
    } finally {
      setSendingInvite(false)
    }
  }

  const hasEditChanges = editingMember
    ? editRole !== editingMember.role || editActive !== editingMember.isActive
    : false

  const visibleInvitations = invitations.filter(inv => new Date(inv.expiresAt) > new Date())

  if (loading) {
    return <div className="flex items-center justify-center py-20 text-muted-foreground">{t('common.loading')}</div>
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">{t('members.title')}</h1>
        <Button onClick={() => setInviteOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          {t('members.addMember')}
        </Button>
      </div>

      {/* Members table */}
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t('members.columns.name')}</TableHead>
              <TableHead>{t('members.columns.role')}</TableHead>
              <TableHead>{t('members.columns.status')}</TableHead>
              <TableHead>{t('members.columns.since')}</TableHead>
              <TableHead className="w-16">{t('members.columns.actions')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {members.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center text-muted-foreground py-8">
                  {t('members.noMembers')}
                </TableCell>
              </TableRow>
            ) : (
              members.map((m) => {
                const isMe = m.userId === userId
                return (
                  <TableRow key={m.memberId} className={!m.isActive ? 'opacity-60' : ''}>
                    <TableCell className="font-medium">
                      {m.displayName}
                      {isMe && <span className="ml-1 text-xs text-muted-foreground">{t('members.you')}</span>}
                    </TableCell>
                    <TableCell>
                      <Badge variant={m.role === 'FamilyAdmin' ? 'default' : 'secondary'}>
                        {t(`members.roles.${m.role}`)}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={m.isActive ? 'outline' : 'destructive'}>
                        {m.isActive ? t('members.active') : t('members.inactive')}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{formatDate(m.createdAt)}</TableCell>
                    <TableCell>
                      {!isMe && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7"
                          title={t('common.edit')}
                          onClick={() => openEdit(m)}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                )
              })
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pending invitations */}
      {visibleInvitations.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('members.pendingInvitations')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {visibleInvitations.map((inv) => (
                <div key={inv.invitationId} className="flex items-center justify-between text-sm rounded-md border px-3 py-2">
                  <div className="flex flex-col gap-0.5">
                    <span className="font-medium">{inv.invitedEmail}</span>
                    <span className="text-xs text-muted-foreground">
                      {t('members.invitedBy')}: {inv.invitedByDisplayName}
                    </span>
                  </div>
                  <div className="text-muted-foreground text-xs">
                    {t('members.expiresAt')} {formatDate(inv.expiresAt)}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Edit member sheet */}
      <Sheet open={editingMember !== null} onOpenChange={(open) => { if (!open) closeEdit() }}>
        <SheetContent side="right" className="flex flex-col sm:max-w-md">
          <SheetHeader>
            <SheetTitle>{t('members.editMember')}</SheetTitle>
            <SheetDescription className="sr-only">{editingMember?.displayName}</SheetDescription>
          </SheetHeader>

          <div className="flex flex-1 flex-col gap-5 overflow-y-auto px-4">
            <div className="space-y-1.5">
              <Label htmlFor="editRole">{t('members.columns.role')}</Label>
              <Select value={editRole} onValueChange={setEditRole}>
                <SelectTrigger id="editRole" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="FamilyAdmin">{t('members.roles.FamilyAdmin')}</SelectItem>
                  <SelectItem value="Member">{t('members.roles.Member')}</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center gap-3">
              <Switch
                id="editActive"
                checked={editActive}
                onCheckedChange={setEditActive}
              />
              <Label htmlFor="editActive">{t('members.activeLabel')}</Label>
            </div>
          </div>

          <SheetFooter className="flex-row gap-2 border-t pt-4">
            <SheetClose render={<Button variant="outline" className="flex-1" />}>
              {t('common.cancel')}
            </SheetClose>
            <Button className="flex-1" onClick={handleSaveEdit} disabled={saving || !hasEditChanges}>
              {saving ? t('common.saving') : t('common.save')}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      {/* Invite dialog */}
      <Dialog open={inviteOpen} onOpenChange={setInviteOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('members.inviteTitle')}</DialogTitle>
            <DialogDescription>{t('members.inviteDescription')}</DialogDescription>
          </DialogHeader>
          <div className="space-y-1.5">
            <Label htmlFor="inviteEmail">{t('members.inviteEmail')}</Label>
            <Input
              id="inviteEmail"
              type="email"
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
              placeholder={t('members.inviteEmailPlaceholder')}
              onKeyDown={(e) => { if (e.key === 'Enter') handleSendInvite() }}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => { setInviteOpen(false); setInviteEmail('') }}>
              {t('common.cancel')}
            </Button>
            <Button onClick={handleSendInvite} disabled={sendingInvite || !inviteEmail.trim()}>
              <Send className="mr-2 h-4 w-4" />
              {sendingInvite ? t('members.inviteSending') : t('members.inviteSend')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Confirm dialog */}
      <ConfirmDialog
        open={confirmAction !== null}
        onOpenChange={(open) => { if (!open) setConfirmAction(null) }}
        onConfirm={async () => {
          if (confirmAction) {
            await confirmAction.onConfirm()
            setConfirmAction(null)
          }
        }}
        title={t('common.confirm')}
        description={confirmAction?.message}
        confirmLabel={t('common.confirm')}
      />
    </div>
  )
}
