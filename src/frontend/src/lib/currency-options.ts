import api from '@/lib/api'
import type { CurrencyDto, FamilyCurrencyDto } from '@/types/api'

let allCache: { value: string; label: string }[] | null = null
let familyCache: { value: string; label: string; isActive: boolean }[] | null = null

/** All ISO 4217 currencies (for adding new currencies to family, settings) */
export async function loadCurrencyOptions(): Promise<{ value: string; label: string }[]> {
  if (allCache) return allCache
  const { data } = await api.get<CurrencyDto[]>('/currencies')
  allCache = data.map((c) => ({ value: c.code, label: `${c.code} — ${c.name}` }))
  return allCache
}

/** Only currencies associated with the family (includes inactive for edit display) */
export async function loadFamilyCurrencyOptions(): Promise<{ value: string; label: string; isActive: boolean }[]> {
  if (familyCache) return familyCache
  const { data } = await api.get<FamilyCurrencyDto[]>('/familycurrencies')
  familyCache = data
    .map((c) => ({ value: c.code, label: `${c.code} — ${c.name}`, isActive: c.isActive }))
  return familyCache
}

/** Clear family currency cache (call after adding/removing family currencies) */
export function clearFamilyCurrencyCache() {
  familyCache = null
}
