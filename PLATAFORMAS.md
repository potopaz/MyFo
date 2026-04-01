# Plataformas de Infraestructura — MyFO

Referencia rápida de cada plataforma usada, para qué sirve, costos y limitaciones.

---

## Railway

**¿Qué es?**
Plataforma para alojar aplicaciones backend y bases de datos en la nube (PaaS — Platform as a Service). Se conecta a GitHub y redeploya automáticamente en cada push.

**¿Para qué lo usamos?**
- Backend API (.NET 10)
- Base de datos PostgreSQL

**¿Por qué Railway y no otra cosa?**
- Soporta .NET nativamente
- PostgreSQL integrado en el mismo proyecto (comunicación interna, sin exponer la BD a internet)
- Deploy automático desde GitHub sin configuración compleja
- Variables de entorno fáciles de manejar

**Costos**
| Plan | Precio | Incluye |
|------|--------|---------|
| Trial | Gratis | $5 de crédito único |
| Hobby | u$s5/mes | u$s5 de crédito mensual incluido |
| Pro | u$s20/mes | u$s20 de crédito mensual incluido |

El crédito se consume según uso (CPU, RAM, disco). Para MyFO en producción liviana, el plan Hobby ($5/mes) es suficiente y el crédito incluido prácticamente lo cubre.

**Limitación plan Hobby**
- Sin SLA de uptime garantizado
- Logs limitados a 7 días
- Sin soporte prioritario

---

## Vercel

**¿Qué es?**
Plataforma especializada en hosting de frontends (React, Next.js, Vue, etc.). CDN global, SSL automático, deploy desde GitHub.

**¿Para qué lo usamos?**
- Frontend de MyFO (React + Vite)
- Frontend de Nexpen (futuro)

**¿Por qué Vercel y no Railway para el frontend?**
Railway sirve para backends. Vercel está optimizado para frontends estáticos: CDN global, builds más rápidos, mejor experiencia para React.

**Costos**
| Plan | Precio | Incluye |
|------|--------|---------|
| Hobby | Gratis | Proyectos ilimitados, 100 GB bandwidth/mes |
| Pro | u$s20/mes | más bandwidth, analytics, soporte |

**Limitación plan Hobby**
- Solo para uso personal/no comercial (en la práctica no lo controlan para proyectos pequeños)
- Sin SLA de uptime
- Sin soporte

---

## Cloudflare

**¿Qué es?**
Proveedor de DNS, CDN y seguridad. Es la plataforma más usada en el mundo para gestionar DNS de dominios.

**¿Para qué lo usamos?**
- Gestión de DNS de `myfo.com.ar`
- Configurar los registros necesarios para Resend (SPF, DKIM, MX)

**¿Por qué DNS en Cloudflare y no en Vercel?**
Vercel tiene DNS básico, pensado solo para apuntar dominios a sus propios servidores. No está diseñado para gestionar registros complejos de email (SPF, DKIM, MX). Cloudflare es un gestor de DNS completo y profesional — podés configurar cualquier tipo de registro para cualquier servicio (email, subdominios, CDN, etc.).

**¿Cómo funciona la cadena de DNS?**
```
nic.ar (registrador del dominio)
  → delega a Cloudflare (nameservers)
    → Cloudflare gestiona todos los registros DNS
      → apunta a Vercel (frontend), Railway (backend), Resend (email)
```

**Costos**
| Plan | Precio | Incluye |
|------|--------|---------|
| Free | Gratis | DNS ilimitado, CDN básico, SSL |
| Pro | u$s20/mes | más analytics, reglas de firewall avanzadas |

Para nuestro uso, el plan gratuito es más que suficiente.

**Limitación plan Free**
- Sin soporte prioritario
- Analytics limitados
- Sin algunas reglas avanzadas de firewall

---

## Resend

**¿Qué es?**
Servicio de envío de emails transaccionales vía API. Alternativa moderna a SMTP.

**¿Para qué lo usamos?**
- Envío de emails desde `no-reply@mail.myfo.com.ar`
- Verificación de cuentas, notificaciones, liquidaciones

**¿Por qué no usar SMTP directamente?**
Railway (y la mayoría de proveedores cloud) bloquea el puerto 587 (SMTP) para evitar spam. Resend provee una API HTTP para enviar emails — el backend llama a su API y ellos se encargan del envío con buena reputación de entrega.

**¿Por qué un subdominio (`mail.myfo.com.ar`) y no el dominio raíz?**
Si configurás Resend en el dominio raíz (`myfo.com.ar`) y ya tenés registros MX (para recibir emails), hay conflicto. Un subdominio dedicado evita ese problema y es la práctica recomendada.

**Costos**
| Plan | Precio | Emails/mes | Dominios |
|------|--------|------------|----------|
| Free | Gratis | 3.000 | 1 |
| Pro | u$s20/mes | 50.000 | Ilimitados |

**Limitación plan Free**
- 1 solo dominio verificado
- 100 emails/día (aunque el límite mensual sea 3.000)
- Sin soporte prioritario

---

## Resumen visual

```
Usuario
  │
  ▼
Vercel (frontend React)
  │  /api/*
  ▼
Railway (backend .NET API)
  │
  ├── Railway PostgreSQL (base de datos)
  │
  └── Resend API (emails)
        │
        └── mail.myfo.com.ar (DNS en Cloudflare)

Dominio myfo.com.ar
  ├── registrado en nic.ar
  └── DNS gestionado en Cloudflare
        ├── apunta frontend → Vercel
        └── registros email → Resend
```

---

## nic.ar

**¿Qué es?**
Registro oficial de dominios `.com.ar` en Argentina. Dependencia del Estado (NIC Argentina).

**¿Para qué lo usamos?**
- Registrar y renovar el dominio `myfo.com.ar`
- Configurar a qué nameservers apunta el dominio (en nuestro caso, Cloudflare)

**Costos**
Muy bajo, en pesos argentinos. Renovación anual.
