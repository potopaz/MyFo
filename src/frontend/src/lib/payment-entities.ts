import api from '@/lib/api'
import type { CashBoxDto, BankAccountDto, CreditCardDto } from '@/types/api'

export interface PaymentEntityOption {
  value: string
  label: string
  currencyCode: string
  isActive?: boolean
  canOperate?: boolean
}

export interface CreditCardOption extends PaymentEntityOption {
  members: { value: string; label: string; isCurrentUser: boolean }[]
}

export async function loadCashBoxOptions(): Promise<PaymentEntityOption[]> {
  const { data } = await api.get<CashBoxDto[]>('/cashboxes')
  return data
    .map((c) => ({ value: c.cashBoxId, label: c.name, currencyCode: c.currencyCode, isActive: c.isActive, canOperate: c.canOperate }))
}

export async function loadBankAccountOptions(): Promise<PaymentEntityOption[]> {
  const { data } = await api.get<BankAccountDto[]>('/bankaccounts')
  return data
    .map((b) => ({ value: b.bankAccountId, label: b.name, currencyCode: b.currencyCode, isActive: b.isActive }))
}

export async function loadCreditCardOptions(): Promise<CreditCardOption[]> {
  const { data } = await api.get<CreditCardDto[]>('/creditcards')
  return data
    .map((c) => ({
      value: c.creditCardId,
      label: c.name,
      currencyCode: c.currencyCode,
      isActive: c.isActive,
      members: c.members
        .filter((m) => m.isActive)
        .map((m) => ({
          value: m.creditCardMemberId,
          label: m.holderName + (m.lastFourDigits ? ` (${m.lastFourDigits})` : ''),
          isCurrentUser: m.isCurrentUser,
        })),
    }))
}
