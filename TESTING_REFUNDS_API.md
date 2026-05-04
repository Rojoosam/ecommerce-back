# 🧪 Testing de Refunds API - Guía Completa

## 🎯 Objetivo

Verificar que la API de reembolsos funciona correctamente y se integra perfectamente con Stripe.

---

## ⚙️ Prerrequisitos

1. ✅ Tener un **Payment Intent exitoso** (`pi_xxx`) o **Charge** (`ch_xxx`)
2. ✅ El pago debe estar en estado **succeeded**
3. ✅ API Key de Stripe configurada en `appsettings.Development.json`
4. ✅ Aplicación ejecutándose en `https://localhost:7001`

---

## 🔥 Paso 1: Iniciar la Aplicación

```bash
# Desde Visual Studio: Presiona F5
# O desde terminal:
cd ECommerceAPI
dotnet run
```

La aplicación debe iniciar en: `https://localhost:7001`

---

## 📝 Paso 2: Preparar un Payment Intent

### Opción A: Crear un Payment Intent Nuevo

```bash
# 1. Crear un Customer (opcional pero recomendado)
curl -X POST "https://localhost:7001/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "refund-test@example.com",
    "name": "Test Refund User"
  }'

# Respuesta: Guarda el customer_id (cus_xxx)

# 2. Crear un Payment Intent
curl -X POST "https://localhost:7001/api/payment-intents" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 100.00,
    "currency": "usd",
    "customerId": "cus_xxx",
    "description": "Test para reembolso",
    "captureMethod": "automatic"
  }'

# Respuesta: Guarda el payment_intent_id (pi_xxx) y client_secret
```

### Opción B: Usar un Payment Intent Existente

Si ya tienes un Payment Intent en estado `succeeded`, úsalo directamente.

---

## 🧪 Paso 3: Probar Endpoints de Refunds

### Test 1: Crear Reembolso Total ✅

```bash
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABCDEfghijk789012" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Cliente solicitó cancelación completa"
  }'
```

**Resultado Esperado:**
```json
{
  "refundId": "re_1PQRSTuvwxyz123456",
  "originalTransactionId": "pi_1ABCDEfghijk789012",
  "status": "Refunded",
  "amount": 100.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**✅ Verificación:**
- `refundId` debe comenzar con `re_`
- `status` debe ser `"Refunded"`
- `amount` debe ser el monto total del Payment Intent

---

### Test 2: Crear Reembolso Parcial ✅

```bash
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABCDEfghijk789012" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 25.00,
    "reason": "Reembolso parcial por artículo devuelto"
  }'
```

**Resultado Esperado:**
```json
{
  "refundId": "re_1XYZABCdefghi456789",
  "originalTransactionId": "pi_1ABCDEfghijk789012",
  "status": "Refunded",
  "amount": 25.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T11:15:00Z"
}
```

**✅ Verificación:**
- `amount` debe ser exactamente 25.00
- Puedes crear múltiples reembolsos parciales hasta el total

---

### Test 3: Reembolso desde Charge (Legacy) ✅

```bash
curl -X POST "https://localhost:7001/api/refunds/charge/ch_1MNOPQrstuvw345678" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 50.00,
    "reason": "fraudulent"
  }'
```

**Resultado Esperado:**
```json
{
  "refundId": "re_1ABCrefund123456",
  "originalTransactionId": "ch_1MNOPQrstuvw345678",
  "status": "Refunded",
  "amount": 50.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T12:00:00Z"
}
```

---

### Test 4: Obtener Información de Reembolso ✅

```bash
curl -X GET "https://localhost:7001/api/refunds/re_1PQRSTuvwxyz123456"
```

**Resultado Esperado:**
```json
{
  "refundId": "re_1PQRSTuvwxyz123456",
  "originalTransactionId": "pi_1ABCDEfghijk789012",
  "status": "Refunded",
  "amount": 100.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### Test 5: Listar Reembolsos ✅

```bash
# Listar últimos 10 reembolsos
curl -X GET "https://localhost:7001/api/refunds?limit=10"
```

**Resultado Esperado:**
```json
[
  {
    "refundId": "re_1Latest",
    "originalTransactionId": "pi_1ABC...",
    "status": "Refunded",
    "amount": 100.00,
    "currency": "USD",
    "message": "Reembolso procesado exitosamente",
    "timestamp": "2024-01-15T15:00:00Z"
  },
  {
    "refundId": "re_1Previous",
    "originalTransactionId": "pi_1DEF...",
    "status": "Refunded",
    "amount": 50.00,
    "currency": "USD",
    "message": "Reembolso procesado exitosamente",
    "timestamp": "2024-01-15T14:00:00Z"
  }
]
```

---

## 🚫 Tests de Validación (Deben Fallar)

### Test 6: ID de Payment Intent Inválido ❌

```bash
curl -X POST "https://localhost:7001/api/refunds/payment-intent/invalid_id" \
  -H "Content-Type: application/json" \
  -d '{}'
```

**Resultado Esperado:**
```json
{
  "error": "El Payment Intent ID debe comenzar con 'pi_'"
}
```

---

### Test 7: Monto Excede el Disponible ❌

```bash
# Intentar reembolsar más del monto original
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABCDEfghijk789012" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 999999.00
  }'
```

**Resultado Esperado:**
```json
{
  "error": "Error al crear reembolso en Stripe: Amount exceeds the amount available to refund."
}
```

---

### Test 8: Reembolso Duplicado ❌

```bash
# Intentar reembolsar el mismo Charge dos veces (total)
curl -X POST "https://localhost:7001/api/refunds/charge/ch_1MNOPQrstuvw345678" \
  -H "Content-Type: application/json" \
  -d '{}'

# Ejecutar el mismo comando nuevamente
```

**Resultado Esperado:**
```json
{
  "error": "Error al crear reembolso en Stripe: Charge ch_xxx has already been refunded."
}
```

---

### Test 9: Refund ID Inválido ❌

```bash
curl -X GET "https://localhost:7001/api/refunds/invalid_refund_id"
```

**Resultado Esperado:**
```json
{
  "error": "El Refund ID debe comenzar con 're_'"
}
```

---

## 🎯 Flujo de Prueba Completo (End-to-End)

### Escenario: Pedido con Reembolso Parcial

```bash
# 1. Crear Customer
curl -X POST "https://localhost:7001/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "e2e-test@example.com",
    "name": "End-to-End Test User",
    "phone": "+1234567890"
  }'
# Guarda: customer_id

# 2. Adjuntar Payment Method
curl -X POST "https://localhost:7001/api/payment-methods/attach" \
  -H "Content-Type: application/json" \
  -d '{
    "paymentMethodId": "pm_card_visa",
    "customerId": "<customer_id del paso 1>"
  }'

# 3. Crear Payment Intent
curl -X POST "https://localhost:7001/7001/api/payment-intents" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 100.00,
    "currency": "usd",
    "customerId": "<customer_id del paso 1>",
    "paymentMethodId": "pm_card_visa",
    "description": "Pedido #12345 - Reembolso E2E Test",
    "captureMethod": "automatic",
    "confirm": true
  }'
# Guarda: payment_intent_id

# 4. Verificar que el Payment Intent está succeeded
curl -X GET "https://localhost:7001/api/payment-intents/<payment_intent_id>"

# 5. Crear reembolso parcial de $30
curl -X POST "https://localhost:7001/api/refunds/payment-intent/<payment_intent_id>" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 30.00,
    "reason": "Artículo A devuelto"
  }'
# Guarda: refund_id_1

# 6. Crear segundo reembolso parcial de $20
curl -X POST "https://localhost:7001/api/refunds/payment-intent/<payment_intent_id>" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 20.00,
    "reason": "Artículo B devuelto"
  }'
# Guarda: refund_id_2

# 7. Consultar primer reembolso
curl -X GET "https://localhost:7001/api/refunds/<refund_id_1>"

# 8. Listar todos los reembolsos
curl -X GET "https://localhost:7001/api/refunds?limit=5"
```

**Resultado Final:**
- Payment Intent de $100
- Reembolso 1: $30
- Reembolso 2: $20
- **Total reembolsado:** $50
- **Aún capturado:** $50

---

## 📊 Verificación en Stripe Dashboard

1. Ve a [Stripe Dashboard](https://dashboard.stripe.com)
2. Ve a **Pagos** → **Todos los pagos**
3. Busca tu Payment Intent
4. En la sección **Timeline**, verás los reembolsos creados
5. Cada reembolso mostrará:
   - Refund ID (`re_xxx`)
   - Monto reembolsado
   - Razón del reembolso
   - Estado

---

## 🐞 Debugging

### Ver Logs en Visual Studio

Los logs incluyen:
```
info: Creando reembolso para Payment Intent: pi_xxx, Monto: 50.00
info: Reembolso creado exitosamente. RefundId: re_xxx, Estado: Refunded
```

### Verificar en Stripe Dashboard

1. **Payments** → Busca el Payment Intent
2. En el detalle verás la sección **Refunds**
3. Cada reembolso listará:
   - ID del reembolso
   - Monto
   - Estado
   - Razón

---

## ✅ Checklist de Verificación

### Funcionalidad Básica
- [ ] Crear reembolso total funciona
- [ ] Crear reembolso parcial funciona
- [ ] Obtener información de reembolso funciona
- [ ] Listar reembolsos funciona

### Validaciones
- [ ] Rechaza Payment Intent IDs inválidos
- [ ] Rechaza Charge IDs inválidos
- [ ] Rechaza Refund IDs inválidos
- [ ] Rechaza montos fuera de rango
- [ ] Rechaza límites fuera de rango (1-100)

### Casos de Error
- [ ] Maneja Payment Intent no encontrado
- [ ] Maneja Payment Intent no capturado
- [ ] Maneja monto excedido
- [ ] Maneja reembolso duplicado

### Integración con Stripe
- [ ] El reembolso aparece en Stripe Dashboard
- [ ] El refund_id tiene formato `re_xxx`
- [ ] El estado es correcto
- [ ] El monto es correcto (convertido de centavos)

---

## 🎭 Escenarios de Prueba

### Escenario 1: Reembolso Total por Cancelación

**Historia:** Cliente cancela pedido antes del envío.

```bash
# Crear reembolso total
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABC..." \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Cliente canceló pedido completo"
  }'
```

**Verificar:**
- ✅ Reembolso por el monto total
- ✅ Estado: `Refunded`
- ✅ Mensaje descriptivo

---

### Escenario 2: Reembolsos Parciales Múltiples

**Historia:** Cliente devuelve productos de forma individual.

```bash
# Primer reembolso: $40 (Producto A)
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABC..." \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 40.00,
    "reason": "Producto A devuelto"
  }'

# Segundo reembolso: $30 (Producto B)
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABC..." \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 30.00,
    "reason": "Producto B devuelto"
  }'

# Listar reembolsos para verificar ambos
curl -X GET "https://localhost:7001/api/refunds?limit=5"
```

**Verificar:**
- ✅ Dos reembolsos creados con IDs diferentes
- ✅ Suma de montos no excede el total
- ✅ Ambos tienen estado `Refunded`

---

### Escenario 3: Consultar Reembolso Existente

**Historia:** Verificar el estado de un reembolso anterior.

```bash
# Obtener información del reembolso
curl -X GET "https://localhost:7001/api/refunds/re_1PQRSTuvwxyz123456"
```

**Verificar:**
- ✅ Devuelve información correcta del reembolso
- ✅ Incluye ID del Payment Intent o Charge original
- ✅ Estado y monto coinciden con Stripe Dashboard

---

### Escenario 4: Intentar Reembolso Inválido

**Historia:** Intentar reembolsar más del monto disponible.

```bash
# Payment Intent de $100, intentar reembolsar $150
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABC..." \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 150.00,
    "reason": "Test de validación"
  }'
```

**Verificar:**
- ✅ Devuelve error 500
- ✅ Mensaje indica que el monto excede lo disponible

---

## 🔍 Verificación en Stripe Dashboard

### Pasos:
1. Ve a https://dashboard.stripe.com/test/payments
2. Busca tu Payment Intent por ID
3. Haz clic en el Payment Intent
4. Verás una sección **"Refunds"** con:
   - Lista de todos los reembolsos
   - Montos individuales
   - Razones
   - Estados
   - Total reembolsado

### Captura de Pantalla Esperada:
```
Payment Intent: pi_1ABCDEfghijk789012
Amount: $100.00 USD
Status: Succeeded (partially refunded)

Refunds:
├─ re_1PQRSTuvwxyz123456  |  $40.00  |  succeeded  |  "Producto A devuelto"
└─ re_1XYZABCdefghi456789  |  $30.00  |  succeeded  |  "Producto B devuelto"

Total refunded: $70.00
```

---

## 📊 Casos de Uso del Mundo Real

### 1. E-commerce con Devoluciones

```bash
# Pedido: 3 artículos ($100 total)
# Cliente devuelve 1 artículo ($35)

curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_order123" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 35.00,
    "reason": "Cliente devolvió artículo #2"
  }'
```

### 2. Servicio de Suscripción

```bash
# Suscripción cancelada a mitad de mes
# Reembolso proporcional

curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_subscription456" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 15.00,
    "reason": "Suscripción cancelada - reembolso proporcional"
  }'
```

### 3. Transacción Fraudulenta

```bash
# Detectada actividad fraudulenta

curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_fraud789" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "fraudulent"
  }'
```

---

## 🧪 PowerShell Testing Script

### Script Automatizado

```powershell
# test-refunds.ps1

$baseUrl = "https://localhost:7001/api"

Write-Host "🧪 Iniciando pruebas de Refunds API..." -ForegroundColor Cyan

# Test 1: Crear Payment Intent
Write-Host "`n📝 Test 1: Creando Payment Intent..." -ForegroundColor Yellow
$piResponse = Invoke-RestMethod -Uri "$baseUrl/payment-intents" `
  -Method Post `
  -ContentType "application/json" `
  -Body (@{
    amount = 100.00
    currency = "usd"
    description = "Test para reembolsos"
    captureMethod = "automatic"
  } | ConvertTo-Json)

$paymentIntentId = $piResponse.paymentIntentId
Write-Host "✅ Payment Intent creado: $paymentIntentId" -ForegroundColor Green

# Test 2: Crear Reembolso Parcial
Write-Host "`n📝 Test 2: Creando reembolso parcial..." -ForegroundColor Yellow
$refundResponse = Invoke-RestMethod -Uri "$baseUrl/refunds/payment-intent/$paymentIntentId" `
  -Method Post `
  -ContentType "application/json" `
  -Body (@{
    amount = 25.00
    reason = "Reembolso parcial de prueba"
  } | ConvertTo-Json)

$refundId = $refundResponse.refundId
Write-Host "✅ Reembolso creado: $refundId (Monto: `$$($refundResponse.amount))" -ForegroundColor Green

# Test 3: Consultar Reembolso
Write-Host "`n📝 Test 3: Consultando reembolso..." -ForegroundColor Yellow
$getResponse = Invoke-RestMethod -Uri "$baseUrl/refunds/$refundId" -Method Get
Write-Host "✅ Reembolso consultado: Estado = $($getResponse.status)" -ForegroundColor Green

# Test 4: Listar Reembolsos
Write-Host "`n📝 Test 4: Listando reembolsos..." -ForegroundColor Yellow
$listResponse = Invoke-RestMethod -Uri "$baseUrl/refunds?limit=5" -Method Get
Write-Host "✅ Total de reembolsos listados: $($listResponse.Count)" -ForegroundColor Green

Write-Host "`n🎉 Todas las pruebas completadas exitosamente!" -ForegroundColor Green
```

**Ejecutar:**
```powershell
.\test-refunds.ps1
```

---

## 📝 Checklist Final

### Antes de Integrar con Laravel

- [ ] Todos los tests pasan correctamente
- [ ] Los reembolsos aparecen en Stripe Dashboard
- [ ] Las validaciones funcionan correctamente
- [ ] Los logs muestran información útil
- [ ] Los errores devuelven mensajes claros
- [ ] La documentación está actualizada

### Puntos Clave para Laravel

1. **Guardar refund_id** en la base de datos
2. **Validar payment_intent_id** antes de solicitar reembolso
3. **Verificar monto disponible** para reembolsos parciales
4. **Notificar al cliente** cuando el reembolso es exitoso
5. **Manejar errores** de Stripe apropiadamente

---

## 🚀 Siguiente Paso

Una vez que todos los tests pasen, procede con:
1. Crear el controlador en Laravel
2. Definir las rutas API
3. Implementar la lógica de negocio
4. Actualizar la base de datos para trackear reembolsos

---

## 📞 Soporte

Si encuentras algún problema:
1. Verifica los logs de Visual Studio
2. Revisa Stripe Dashboard para el estado real
3. Consulta la documentación de Stripe: https://stripe.com/docs/refunds

---

**¡Happy Testing! 🎉**
