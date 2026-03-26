import { Card, CardContent } from '@/components/ui/card'
import { Construction } from 'lucide-react'

export default function PlaceholderPage({ title }: { title: string }) {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">{title}</h1>
      <Card>
        <CardContent className="flex flex-col items-center justify-center gap-3 py-16 text-muted-foreground">
          <Construction className="h-10 w-10" />
          <p className="text-sm">Esta seccion esta en desarrollo</p>
        </CardContent>
      </Card>
    </div>
  )
}
