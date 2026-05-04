# 🧪 Testing de Payment Intents API

## Configuración Inicial

### 1. Asegúrate de tener la API corriendo
```powershell
cd C:\Users\pemil\OneDrive\Documents\EccomerceDevOps\ECommerceAPI\ECommerceAPI
dotnet run
```

### 2. URL Base
```
https://localhost:7XXX/api/paymentintents
```
*(Reemplaza 7XXX con tu puerto - revisa launchSettings.json)*

---

## 🎯 Escenarios de Testing

### Escenario 1: Pago Exitoso ✅

**Prerequisitos:**
1. Customer creado (cus_xxx)
2. Payment Method registrado (pm_xxx)
3. Customer activo

**Request:**
```bash
curl -X POST "https://localhost:7XXX/api/paymentintents" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
    "amount": 100.00,
    "currency": "mxn",
    "order_id": "ORDER-001",
    "description": "Compra de prueba",
    "auto_confirm": true,
    "auto_capture": true
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
  "status": "succeeded",
  "amount_decimal": 100.00,
  "currency": "mxn",
  "charge": {
    "charge_id": "ch_xxx",
    "status": "succeeded",
    "captured": true,
    "receipt_url": "https://pay.stripe.com/receipts/..."
  }
}
```

---

### Escenario 2: Pago con Tarjeta Rechazada ❌

**Request:**
```bash
curl -X POST "https://localhost:7XXX/api/paymentintents" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "customer_id": "cus_xxx",
    "payment_method_id": "pm_card_declined",
    "amount": 50.00,
    "currency": "mxn",
    "order_id": "ORDER-002"
  }'
```

**Expected Response:**
```json
{
  "success": false,
  "status": "failed",
  "error_message": "Your card was declined.",
  "error_code": "card_declined"
}
```

**Nota:** Para probar este escenario, primero registra un Payment Method con la tarjeta `4000 0000 0000 0002`.

---

### Escenario 3: Customer Inactivo ❌

**Paso 1: Desactivar el Customer**
```bash
curl -X PUT "https://localhost:7XXX/api/paymentmethods/customer/status" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "customer_id": "cus_xxx",
    "active": false,
    "detach_payment_methods": false
  }'
```

**Paso 2: Intentar Procesar Pago**
```bash
curl -X POST "https://localhost:7XXX/api/paymentintents" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "customer_id": "cus_xxx",
    "payment_method_id": "pm_xxx",
    "amount": 100.00,
    "currency": "mxn",
    "order_id": "ORDER-003"
  }'
```

**Expected Response:**
```json
{
  "success": false,
  "error_message": "El Customer está inactivo. No se pueden procesar pagos."
}
```

**Paso 3: Reactivar el Customer**
```bash
curl -X PUT "https://localhost:7XXX/api/paymentmethods/customer/status" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "customer_id": "cus_xxx",
    "active": true
  }'
```

---

### Escenario 4: Autorización sin Captura 💰

**Request:**
```bash
curl -X POST "https://localhost:7XXX/api/paymentintents" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "customer_id": "cus_xxx",
    "payment_method_id": "pm_xxx",
    "amount": 200.00,
    "currency": "mxn",
    "order_id": "ORDER-004",
    "auto_confirm": true,
    "auto_capture": false
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "payment_intent_id": "pi_xxx",
  "status": "requires_capture",
  "amount_decimal": 200.00
}
```

**Capturar después:**
```bash
curl -X POST "https://localhost:7XXX/api/paymentintents/capture" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "payment_intent_id": "pi_xxx"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "payment_intent_id": "pi_xxx",
  "status": "succeeded",
  "amount_captured": 200.00,
  "message": "Payment Intent capturado exitosamente"
}
```

---

### Escenario 5: Cancelar Pago ❌

**Paso 1: Crear Payment Intent sin confirmar**
```bash
curl -X POST "https://localhost:7XXX/api/paymentintents" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "customer_id": "cus_xxx",
    "payment_method_id": "pm_xxx",
    "amount": 75.00,
    "currency": "mxn",
    "order_id": "ORDER-005",
    "auto_confirm": false
  }'
```

**Paso 2: Cancelar el Payment Intent**
```bash
curl -X POST "https://localhost:7XXX/api/paymentintents/cancel" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "payment_intent_id": "pi_xxx",
    "cancellation_reason": "requested_by_customer"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "payment_intent_id": "pi_xxx",
  "status": "canceled",
  "message": "Payment Intent cancelado exitosamente"
}
```

---

### Escenario 6: Consultar Payment Intent 🔍

**Request:**
```bash
curl -X GET "https://localhost:7XXX/api/paymentintents/pi_3PqRsT2aBcDeFgHi" \
  -H "Content-Type: application/json" \
  -k
```

**Expected Response:**
```json
{
  "success": true,
  "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
  "status": "succeeded",
  "amount_decimal": 100.00,
  "currency": "mxn",
  "order_id": "ORDER-001",
  "charge": { /* detalles */ }
}
```

---

## 🔄 Flujo de Testing Completo

### Paso 1: Crear Customer
```bash
curl -X POST "https://localhost:7XXX/api/customers" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "user_id": "1",
    "name": "Juan Pérez Testing",
    "email": "juan.test@example.com",
    "phone": "+525512345678"
  }'
```

**Guardar:** `customer_id` (cus_xxx)

---

### Paso 2: Registrar Payment Method

**Primero, generar token en Stripe Test Mode:**
Visita: https://dashboard.stripe.com/test/tokens/create

O usa tokens de prueba directamente:
- `tok_visa`
- `tok_mastercard`
- `tok_amex`

```bash
curl -X POST "https://localhost:7XXX/api/paymentmethods/attach" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "customer_id": "cus_xxx",
    "token": "tok_visa"
  }'
```

**Guardar:** `payment_method_id` (pm_xxx)

---

### Paso 3: Procesar Pago
```bash
curl -X POST "https://localhost:7XXX/api/paymentintents" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "customer_id": "cus_xxx",
    "payment_method_id": "pm_xxx",
    "amount": 100.00,
    "currency": "mxn",
    "order_id": "ORDER-TEST-001"
  }'
```

**Verificar:** `status: "succeeded"` y `charge` con detalles

---

### Paso 4: Consultar el Pago
```bash
curl -X GET "https://localhost:7XXX/api/paymentintents/pi_xxx" \
  -H "Content-Type: application/json" \
  -k
```

---

## 🎨 Testing con Swagger UI

### 1. Abre Swagger
```
https://localhost:7XXX/swagger
```

### 2. Busca "PaymentIntentsController"

### 3. Prueba los Endpoints
- **POST /api/paymentintents** - Crear pago
- **GET /api/paymentintents/{id}** - Consultar pago
- **POST /api/paymentintents/cancel** - Cancelar pago
- **POST /api/paymentintents/capture** - Capturar pago

---

## 📊 Validaciones a Probar

### ✅ Validaciones que Deben Pasar

1. **Customer Válido**
   ```json
   { "customer_id": "cus_PQx1yZ2aBcDeFgHi" }
   ```
   ✅ Debe aceptar

2. **Payment Method Válido**
   ```json
   { "payment_method_id": "pm_1PqRsT2aBcDeFgHi" }
   ```
   ✅ Debe aceptar

3. **Monto Positivo**
   ```json
   { "amount": 100.00 }
   ```
   ✅ Debe aceptar

4. **Moneda de 3 Letras**
   ```json
   { "currency": "mxn" }
   ```
   ✅ Debe aceptar

---

### ❌ Validaciones que Deben Fallar

1. **Customer ID Inválido**
   ```json
   { "customer_id": "invalid" }
   ```
   ❌ Debe rechazar: "El 'customer_id' debe tener el formato 'cus_xxx'"

2. **Payment Method ID Inválido**
   ```json
   { "payment_method_id": "invalid" }
   ```
   ❌ Debe rechazar: "El 'payment_method_id' debe tener el formato 'pm_xxx'"

3. **Monto Cero o Negativo**
   ```json
   { "amount": 0 }
   ```
   ❌ Debe rechazar: "El monto debe ser mayor a 0"

4. **Customer Inactivo**
   ```json
   { "customer_id": "cus_inactive" }
   ```
   ❌ Debe rechazar: "El Customer está inactivo"

---

## 📈 Métricas de Testing

### Casos de Prueba
- ✅ Pago exitoso
- ✅ Pago rechazado
- ✅ Customer inactivo
- ✅ Autorización sin captura
- ✅ Captura de pago autorizado
- ✅ Cancelación de pago
- ✅ Consulta de Payment Intent
- ✅ Multi-moneda (USD, MXN, EUR, JPY)

### Total de Pruebas: **8 escenarios principales**

---

## 🔍 Debugging

### Ver Logs en Visual Studio
1. Abre **Output** → **Debug**
2. Busca logs con emojis:
   - ✅ Pago exitoso
   - ❌ Error
   - 💰 Requiere captura
   - 🔐 Requiere autenticación

### Ver Logs en Stripe Dashboard
1. Visita: https://dashboard.stripe.com/test/logs
2. Busca por:
   - `payment_intent_id`
   - `order_id`
   - `customer_id`

---

## 🎯 Checklist de Testing

### Funcionalidad Básica
- [ ] Crear Payment Intent exitoso
- [ ] Consultar Payment Intent
- [ ] Verificar datos del charge
- [ ] Verificar conversion de montos

### Validaciones
- [ ] Validar formato de customer_id
- [ ] Validar formato de payment_method_id
- [ ] Validar monto positivo
- [ ] Validar moneda válida
- [ ] Rechazar Customer inactivo

### Flujos Avanzados
- [ ] Autorización sin captura
- [ ] Captura de pago autorizado
- [ ] Captura parcial
- [ ] Cancelación de pago
- [ ] Intentar cancelar pago exitoso (debe fallar)

### Multi-Moneda
- [ ] Pago en MXN
- [ ] Pago en USD
- [ ] Pago en EUR
- [ ] Pago en JPY (sin decimales)

### Errores
- [ ] Tarjeta rechazada
- [ ] Fondos insuficientes
- [ ] Tarjeta expirada
- [ ] Payment Method inválido

---

## 📊 Tarjetas de Prueba por Escenario

| Escenario | Número de Tarjeta | Expected Status |
|-----------|-------------------|-----------------|
| ✅ Éxito | 4242 4242 4242 4242 | succeeded |
| ❌ Rechazada | 4000 0000 0000 0002 | failed (card_declined) |
| ❌ Sin fondos | 4000 0000 0000 9995 | failed (insufficient_funds) |
| ❌ Expirada | 4000 0000 0000 0069 | failed (expired_card) |
| 🔐 3D Secure | 4000 0027 6000 3184 | requires_action |
| 💰 Sin captura | 4000 0000 0000 3063 | requires_capture |

---

## 🚀 Testing desde Swagger UI

### 1. Navega a Swagger
```
https://localhost:7XXX/swagger
```

### 2. Expande "PaymentIntentsController"

### 3. Prueba "POST /api/paymentintents"

**Request Body (Swagger):**
```json
{
  "customer_id": "cus_xxx",
  "payment_method_id": "pm_xxx",
  "amount": 100.00,
  "currency": "mxn",
  "order_id": "ORDER-SWAGGER-001",
  "description": "Prueba desde Swagger",
  "auto_confirm": true,
  "auto_capture": true
}
```

**Click "Execute"**

**Verifica Response:**
- Status: 200
- Body: `success: true`, `status: "succeeded"`

---

## 🧪 Testing desde Postman

### Colección de Requests

**1. Procesar Pago**
- Method: POST
- URL: `https://localhost:7XXX/api/paymentintents`
- Body:
  ```json
  {
    "customer_id": "{{customer_id}}",
    "payment_method_id": "{{payment_method_id}}",
    "amount": 150.00,
    "currency": "mxn",
    "order_id": "{{$randomInt}}"
  }
  ```

**2. Consultar Pago**
- Method: GET
- URL: `https://localhost:7XXX/api/paymentintents/{{payment_intent_id}}`

**3. Cancelar Pago**
- Method: POST
- URL: `https://localhost:7XXX/api/paymentintents/cancel`
- Body:
  ```json
  {
    "payment_intent_id": "{{payment_intent_id}}"
  }
  ```

---

## 📝 Logs Esperados

### Pago Exitoso
```
[2024-01-15 10:30:00] Creando Payment Intent. Customer: cus_xxx, Monto: 100.00 mxn, Order: ORDER-001
[2024-01-15 10:30:01] Payment Intent creado. PaymentIntentId: pi_xxx, Estado: succeeded, Order: ORDER-001
[2024-01-15 10:30:01] ✅ Pago exitoso. PaymentIntent: pi_xxx, Monto: 100.00 MXN, Order: ORDER-001
```

### Pago Rechazado
```
[2024-01-15 10:35:00] Creando Payment Intent. Customer: cus_xxx, Monto: 50.00 mxn, Order: ORDER-002
[2024-01-15 10:35:01] ❌ Error de Stripe al crear Payment Intent. Customer: cus_xxx, Order: ORDER-002, Error: Your card was declined.
```

### Customer Inactivo
```
[2024-01-15 10:40:00] Creando Payment Intent. Customer: cus_xxx, Monto: 100.00 mxn, Order: ORDER-003
[2024-01-15 10:40:00] ⚠️ Intento de crear Payment Intent para Customer inactivo: cus_xxx
```

---

## ✅ Checklist de Testing Completo

### Pre-requisitos
- [ ] API corriendo en localhost
- [ ] Configuración de Stripe en appsettings.json
- [ ] Customer creado
- [ ] Payment Method registrado
- [ ] Customer activo

### Tests Funcionales
- [ ] ✅ Crear Payment Intent exitoso
- [ ] ❌ Payment Intent rechazado
- [ ] 💰 Payment Intent sin captura
- [ ] 🔐 Payment Intent con 3D Secure
- [ ] ❌ Cancelar Payment Intent
- [ ] 💰 Capturar Payment Intent
- [ ] 🔍 Consultar Payment Intent

### Tests de Validación
- [ ] Formato customer_id inválido
- [ ] Formato payment_method_id inválido
- [ ] Formato payment_intent_id inválido
- [ ] Monto = 0
- [ ] Monto negativo
- [ ] Moneda inválida
- [ ] Order ID vacío

### Tests de Seguridad
- [ ] Customer inactivo no puede pagar
- [ ] Payment Method desasociado no puede usarse
- [ ] Cancelar Payment Intent ya succeeded (debe fallar)

---

## 🎯 Resultados Esperados

### Build Status
```
✅ 0 errores
✅ 0 warnings
✅ Compilación exitosa
```

### Endpoints Status
```
✅ POST   /api/paymentintents          → 200 OK
✅ GET    /api/paymentintents/{id}     → 200 OK
✅ POST   /api/paymentintents/cancel   → 200 OK
✅ POST   /api/paymentintents/capture  → 200 OK
```

---

## 🚀 Testing Rápido (5 minutos)

### 1. Variables
```bash
# Reemplaza con tus IDs reales
$CUSTOMER_ID = "cus_xxx"
$PM_ID = "pm_xxx"
$API_URL = "https://localhost:7XXX"
```

### 2. Procesar Pago
```powershell
Invoke-RestMethod -Uri "$API_URL/api/paymentintents" `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    customer_id = $CUSTOMER_ID
    payment_method_id = $PM_ID
    amount = 100.00
    currency = "mxn"
    order_id = "ORDER-TEST-001"
  } | ConvertTo-Json) `
  -SkipCertificateCheck
```

### 3. Verificar Resultado
- ✅ `success: true`
- ✅ `status: "succeeded"`
- ✅ `payment_intent_id: "pi_xxx"`
- ✅ `charge` con datos completos

---

## 📚 Recursos

- **Stripe Dashboard (Test)**: https://dashboard.stripe.com/test/payments
- **Stripe Logs**: https://dashboard.stripe.com/test/logs
- **Testing Guide**: https://stripe.com/docs/testing
- **Payment Intents API**: https://stripe.com/docs/api/payment_intents

---

**¡Testing completado!** ✅
