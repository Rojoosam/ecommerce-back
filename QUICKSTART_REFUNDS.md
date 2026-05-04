# ⚡ Refunds API - Quick Start Guide

## 🚀 Inicio Rápido en 5 Minutos

Esta guía te permite probar la API de reembolsos en menos de 5 minutos.

---

## ✅ Prerrequisito

Necesitas un **Payment Intent exitoso** para reembolsar. Si no tienes uno, créalo primero:

```bash
# Crear Payment Intent de prueba
curl -X POST "https://localhost:7001/api/payment-intents" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 100.00,
    "currency": "usd",
    "description": "Test para reembolso",
    "captureMethod": "automatic"
  }'

# Guarda el "paymentIntentId" que comienza con "pi_"
```

---

## 🎯 Paso 1: Crear Reembolso Total (30 segundos)

```bash
curl -X POST "https://localhost:7001/api/refunds/payment-intent/PI_ID_AQUI" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Cliente solicitó cancelación"
  }'
```

**Respuesta:**
```json
{
  "refundId": "re_1PQRSTuvwxyz123456",
  "originalTransactionId": "pi_xxx",
  "status": "Refunded",
  "amount": 100.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

✅ **Listo!** Tu primer reembolso fue creado.

---

## 🎯 Paso 2: Crear Reembolso Parcial (30 segundos)

Primero crea otro Payment Intent para este test, luego:

```bash
curl -X POST "https://localhost:7001/api/refunds/payment-intent/PI_ID_AQUI" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 25.00,
    "reason": "Reembolso parcial de $25"
  }'
```

**Respuesta:**
```json
{
  "refundId": "re_1XYZABCdefghi456789",
  "originalTransactionId": "pi_xxx",
  "status": "Refunded",
  "amount": 25.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T11:00:00Z"
}
```

✅ **Perfecto!** Reembolso parcial creado.

---

## 🎯 Paso 3: Consultar Reembolso (30 segundos)

```bash
curl -X GET "https://localhost:7001/api/refunds/REFUND_ID_AQUI"
```

Reemplaza `REFUND_ID_AQUI` con el `refundId` del paso anterior (ej: `re_1PQRSTuvwxyz123456`).

---

## 🎯 Paso 4: Listar Reembolsos (30 segundos)

```bash
curl -X GET "https://localhost:7001/api/refunds?limit=5"
```

**Respuesta:**
```json
[
  {
    "refundId": "re_1PQRSTuvwxyz123456",
    "originalTransactionId": "pi_xxx",
    "status": "Refunded",
    "amount": 100.00,
    "currency": "USD",
    "message": "Reembolso procesado exitosamente",
    "timestamp": "2024-01-15T10:30:00Z"
  }
]
```

---

## 🎉 ¡Listo!

En 5 minutos has:
- ✅ Creado un reembolso total
- ✅ Creado un reembolso parcial
- ✅ Consultado un reembolso
- ✅ Listado todos los reembolsos

---

## 🌟 Prueba Avanzada: PowerShell

Si prefieres PowerShell, copia y pega este script:

```powershell
$base = "https://localhost:7001/api"

# 1. Crear Payment Intent
$pi = Invoke-RestMethod -Uri "$base/payment-intents" -Method Post -ContentType "application/json" -Body '{"amount":100,"currency":"usd","description":"Test"}'
Write-Host "Payment Intent: $($pi.paymentIntentId)" -ForegroundColor Green

# 2. Crear Reembolso
$refund = Invoke-RestMethod -Uri "$base/refunds/payment-intent/$($pi.paymentIntentId)" -Method Post -ContentType "application/json" -Body '{"reason":"Test"}'
Write-Host "Refund ID: $($refund.refundId)" -ForegroundColor Cyan
Write-Host "Amount: $$($refund.amount)" -ForegroundColor Yellow
Write-Host "Status: $($refund.status)" -ForegroundColor Green
```

---

## 📱 Prueba desde Swagger UI

1. Abre `https://localhost:7001/swagger`
2. Busca la sección **Refunds**
3. Expande **POST /api/refunds/payment-intent/{paymentIntentId}**
4. Click en **"Try it out"**
5. Ingresa:
   - `paymentIntentId`: tu Payment Intent ID
   - Request body: `{"reason": "Test"}`
6. Click en **"Execute"**
7. ¡Revisa la respuesta!

---

## 🔍 Verificar en Stripe Dashboard

1. Ve a https://dashboard.stripe.com/test/payments
2. Busca tu Payment Intent
3. Verás el reembolso en la sección **Refunds**
4. Confirma que el `refund_id` coincida

---

## 🆘 Problemas Comunes

### ❌ "Charge has already been refunded"
**Solución:** Ya reembolsaste el pago completamente. Usa un nuevo Payment Intent.

### ❌ "Cannot refund uncaptured charge"
**Solución:** El Payment Intent debe estar en estado `succeeded`. Confírmalo primero.

### ❌ "Amount exceeds available to refund"
**Solución:** Ya hay reembolsos previos. Verifica con GET /payment-intents/{id}.

---

## 📚 Siguiente Paso

👉 **Integrar con Laravel**: Sigue el checklist en `LARAVEL_REFUNDS_CHECKLIST.md`

---

## 🎊 ¡Felicidades!

Tu API de reembolsos está funcionando perfectamente. Ya puedes integrarla con Laravel y empezar a procesar reembolsos reales.

**Happy coding! 🚀**
