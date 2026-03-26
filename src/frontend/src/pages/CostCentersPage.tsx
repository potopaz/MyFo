import { useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { ConfigPage } from '@/components/crud'
import type { ColumnDef, FieldDef } from '@/components/crud'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import api from '@/lib/api'
import { HelpCircle } from 'lucide-react'
import type { CostCenterDto } from '@/types/api'

export default function CostCentersPage() {
  const { t } = useTranslation()
  const fetchItems = useCallback(() => api.get<CostCenterDto[]>('/costcenters').then((r) => r.data), [])

  const columns: ColumnDef<CostCenterDto>[] = useMemo(() => [
    { key: 'name', header: t('costCenters.columns.name'), render: (item) => <span className="font-medium">{item.name}</span> },
    {
      key: 'isActive',
      header: t('costCenters.columns.status'),
      render: (item) => (
        <Badge variant={item.isActive ? 'default' : 'secondary'}>
          {item.isActive ? t('costCenters.active') : t('costCenters.inactive')}
        </Badge>
      ),
    },
  ], [t])

  const fields: FieldDef<Record<string, unknown>>[] = useMemo(() => [
    { key: 'name', label: t('costCenters.fields.name'), type: 'text', required: true, placeholder: t('costCenters.fields.namePlaceholder'), maxLength: 100 },
    { key: 'isActive', label: t('costCenters.fields.isActive'), type: 'switch' },
  ], [t])

  const title = (
    <div className="flex items-center">
      <span>{t('costCenters.title')}
        <Tooltip>
          <TooltipTrigger>
            <HelpCircle className="inline h-3.5 w-3.5 ml-1.5 align-super text-muted-foreground cursor-help hover:text-foreground transition-colors" />
          </TooltipTrigger>
          <TooltipContent>
            {t('costCenters.tooltip')}
          </TooltipContent>
        </Tooltip>
      </span>
    </div>
  )

  return (
    <ConfigPage<CostCenterDto>
      title={title}
      columns={columns}
      fields={fields}
      rowKey="costCenterId"
      fetchItems={fetchItems}
      onCreate={(data) => api.post('/costcenters', { name: data.name })}
      onUpdate={(id, data) => api.put(`/costcenters/${id}`, data)}
      onDelete={(id) => api.delete(`/costcenters/${id}`)}
      defaultValues={{ name: '', isActive: true }}
      newItemLabel={t('costCenters.new')}
      createTitle={t('costCenters.createTitle')}
      editTitle={t('costCenters.editTitle')}
    />
  )
}
