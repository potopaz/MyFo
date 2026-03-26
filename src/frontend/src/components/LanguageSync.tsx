import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import api from '@/lib/api'

export function LanguageSync() {
  const { i18n } = useTranslation()

  useEffect(() => {
    api.get<{ language: string }>('/family-settings')
      .then(({ data }) => {
        if (data.language && data.language !== i18n.language) {
          i18n.changeLanguage(data.language)
          localStorage.setItem('myfo_language', data.language)
        }
      })
      .catch(() => {/* silent */})
  }, [i18n])

  return null
}
