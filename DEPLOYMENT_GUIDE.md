# Guía de Deploy — MyFO

Este documento resume todo lo aprendido durante el primer deploy a producción.
Sirve como referencia para futuras subidas de funcionalidades y para proyectos nuevos.

---

## Infraestructura actual

| Componente   | Servicio   | URL                                          |
|--------------|------------|----------------------------------------------|
| Frontend     | Vercel     | https://my-fo-eta.vercel.app                 |
| Backend API  | Railway    | https://myfo-production.up.railway.app       |
| Base de datos| Railway PG | Interna (variables PG* auto-inyectadas)      |
| Email        | Resend     | sender: no-reply@nexpen.com.ar               |

---

## Checklist de deploy — nuevas funcionalidades

Antes de pushear cualquier cambio:

### Frontend
- [ ] Correr `cd src/frontend && npm run build` — debe terminar sin errores TypeScript
- [ ] Si hay componentes nuevos con Base UI: verificar que no usan `asChild` (usar `render` prop)
- [ ] Si hay archivos `.tsx` nuevos: confirmar que están importados en las rutas o eliminarlos
- [ ] Si hay `useRef` nuevos: siempre inicializar con valor (`undefined` o el tipo correspondiente)

### Backend
- [ ] Correr `cd src/backend && dotnet build MyFO.slnx` sin errores
- [ ] Si hay migraciones nuevas: aplicarlas en producción via TCP proxy de Railway antes o después del deploy
- [ ] Verificar que no hay secrets en archivos commiteados

### Git / CI
- [ ] Push a `master` dispara deploy automático en Railway y Vercel
- [ ] Railway redeploya automáticamente. Si no, hacerlo manualmente desde el dashboard.
- [ ] Vercel redeploya automáticamente en cada push.

---

## Lecciones aprendidas — primer deploy

### 1. Preparar el dominio ANTES de empezar
El dominio afecta múltiples cosas desde el inicio:
- CORS del backend (necesita la URL del frontend)
- OAuth (Google, Microsoft requieren redirect URIs con dominio real)
- Email (Resend/SendGrid requieren dominio verificado para enviar a cualquier destinatario)
- Branding / URL final de la app

**Recomendación**: definir y registrar el dominio antes de empezar el deploy.

### 2. TypeScript strict mode en build de producción
El build de Vercel es más estricto que el dev server de Vite. Errores que no se ven en desarrollo aparecen en producción.

**Solución definitiva**: siempre correr `npm run build` localmente antes de pushear. Nunca asumir que si funciona en dev va a pasar el build.

Errores frecuentes encontrados:
- `useRef<T>()` sin valor inicial → usar `useRef<T | undefined>(undefined)`
- Funciones sin tipo de retorno explícito cuando retornan `null`
- Archivos `.tsx` en `src/` que no están en rutas pero sí se compilan (eliminarlos)
- Props incorrectas: `decimalPlaces` vs `maxDecimals`, `asChild` vs `render`
- `Select onValueChange` puede recibir string vacío → proteger con `val && setState(val)`

### 3. Base UI ≠ Radix UI
Esta librería usa `render` prop en lugar de `asChild`:
```tsx
// ❌ Incorrecto (Radix)
<TooltipTrigger asChild><Button /></TooltipTrigger>

// ✅ Correcto (Base UI)
<TooltipTrigger render={<Button />}>...</TooltipTrigger>
```

### 4. CORS en producción
El backend necesita configurar CORS explícitamente cuando frontend y backend están en dominios distintos. Configurarlo desde el inicio con la URL del frontend en `App:FrontendUrl`.

En Railway usar `__` para config anidada: `App__FrontendUrl`.

### 5. SMTP bloqueado en cloud
Railway (y la mayoría de proveedores cloud) bloquea el puerto 587 saliente para evitar spam.

**Solución**: usar un servicio de email transaccional (Resend, SendGrid, Postmark).
- Resend: free tier 3000 emails/mes, API simple, buena deliverability.
- Sin dominio propio: solo se puede enviar al email del dueño de la cuenta.
- Con dominio verificado: se puede enviar a cualquiera.

### 6. Variables de entorno en Railway
- Formato para config anidada de .NET: `__` doble guión bajo
  - `Jwt__Secret`, `App__FrontendUrl`, `Email__ResendApiKey`
- Las variables de PostgreSQL (`PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`) se inyectan automáticamente cuando los servicios están en el mismo proyecto.
- Secrets (JWT, API keys) nunca van en el código — siempre en variables de entorno.

### 7. Google Translate rompe React en producción
En build de producción, Google Translate modifica el DOM y React pierde el rastro de los nodos → crash con `removeChild`.

**Solución**: agregar `translate="no"` al `<html>` en `index.html`.

### 8. appsettings y secrets
- `appsettings.json`: solo valores por defecto vacíos. Se commitea.
- `appsettings.Development.json`: valores reales para desarrollo local. Está en `.gitignore`.
- Si se pierden los valores de desarrollo, hay que regenerarlos (JWT secret, connection strings, etc.).

---

## Conectarse a la BD de producción desde DBeaver

Railway PostgreSQL no tiene el timezone `America/Buenos_Aires` instalado. DBeaver lo envía automáticamente tomándolo del sistema operativo Windows, lo que causa el error:
```
FATAL: invalid value for parameter "TimeZone": "America/Buenos_Aires"
```

**Solución**: en DBeaver → **Window** → **Preferences** → **User Interface** → **Timezone** → seleccionar **UTC**.

Esto aplica globalmente a todas las conexiones de DBeaver.

Datos de conexión:
- Host: `gondola.proxy.rlwy.net`
- Port: `18647`
- Database: `railway`
- Username: `postgres`
- Password: en Railway → servicio PostgreSQL → Variables → `POSTGRES_PASSWORD`

## Aplicar migraciones en producción

Railway PostgreSQL tiene un TCP Proxy para conexiones externas:
```
gondola.proxy.rlwy.net:18647
```

Para aplicar migraciones desde local:
```bash
cd src/backend
dotnet ef database update -s MyFO.API --connection "Host=gondola.proxy.rlwy.net;Port=18647;Database=railway;Username=postgres;Password=<PASSWORD>"
```

---

## Pendientes de producción

- [ ] Configurar RLS en la base de datos de Railway (script: `001_setup_rls.sql`)
- [ ] Configurar dominio personalizado en Vercel cuando se decida la URL final
- [ ] Configurar OAuth (Google) con el dominio de producción
- [ ] Evaluar licencia de MediatR

---

## Para proyectos nuevos — checklist pre-deploy

1. **Dominio**: registrarlo antes de empezar el deploy
2. **Email transaccional**: configurar Resend/SendGrid con el dominio verificado
3. **CORS**: configurar desde el inicio con la URL del frontend
4. **Variables de entorno**: documentar todas las variables necesarias antes de deployar
5. **Build local**: correr `npm run build` y el build del backend antes del primer push
6. **secrets**: nunca commitear — usar `.gitignore` para archivos de config locales
7. **Google Translate**: agregar `translate="no"` al `<html>` si la app es en español
