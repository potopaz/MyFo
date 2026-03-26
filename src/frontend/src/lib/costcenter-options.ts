import api from '@/lib/api'
import type { CostCenterDto } from '@/types/api'

let cache: { value: string; label: string; isActive: boolean }[] | null = null

/** Cost centers for selects/comboboxes (includes inactive for edit display) */
export async function loadCostCenterOptions(): Promise<{ value: string; label: string; isActive: boolean }[]> {
  if (cache) return cache
  const { data } = await api.get<CostCenterDto[]>('/costcenters')
  cache = data
    .map((c) => ({ value: c.costCenterId, label: c.name, isActive: c.isActive }))
  return cache
}

export function clearCostCenterCache() {
  cache = null
}
