import api from '@/lib/api'
import type { CategoryDto } from '@/types/api'

export interface SubcategoryOption {
  value: string
  label: string
  group: string
  subcategoryType: string
  suggestedAccountingType: string | null
  suggestedCostCenterId: string | null
  isOrdinary: boolean | null
  isActive: boolean
}

let cache: SubcategoryOption[] | null = null

export async function loadSubcategoryOptions(): Promise<SubcategoryOption[]> {
  if (cache) return cache
  const { data } = await api.get<CategoryDto[]>('/categories')
  cache = data.flatMap((cat) =>
    cat.subcategories
      .map((sub) => ({
        value: sub.subcategoryId,
        label: sub.name,
        group: cat.name,
        subcategoryType: sub.subcategoryType,
        suggestedAccountingType: sub.suggestedAccountingType,
        suggestedCostCenterId: sub.suggestedCostCenterId,
        isOrdinary: sub.isOrdinary,
        isActive: sub.isActive,
      }))
  )
  return cache
}

export function clearSubcategoryCache() {
  cache = null
}
