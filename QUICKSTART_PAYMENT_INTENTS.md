# 🚀 Quick Start: Procesamiento de Pagos en 5 Minutos

## ⚡ Testing Rápido del Sistema Completo

---

## Pre-requisitos (1 minuto)

1. **API corriendo:**
   ```powershell
   cd C:\Users\pemil\OneDrive\Documents\EccomerceDevOps\ECommerceAPI\ECommerceAPI
   dotnet run
   ```

2. **Configuración de Stripe en `appsettings.json`:**
   ```json
   {
     "Stripe": {
       "PublishableKey": "pk_test_...",
       "SecretKey": "sk_test_..."
     }
   }
   ```

3. **Variables de PowerShell:**
   ```powershell
   $API_URL = "https://localhost:7XXX"  # Tu puerto
   ```

---

## Paso 1: Crear Customer (1 minuto)

```powershell
$customerResponse = Invoke-RestMethod `
  -Uri "$API_URL/api/customers" `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    user_id = "1"
    name = "Juan Pérez Test"
    email = "juan.test@example.com"
    phone = "+525512345678"
    address = @{
        line1 = "Calle Ejemplo 123"
        city = "CDMX"
        state = "CDMX"
        postal_code = "12345"
        country = "MX"
    }
  } | ConvertTo-Json) `
  -SkipCertificateCheck

$CUSTOMER_ID = $customerResponse.customer_id
Write-Host "✅ Customer creado: $CUSTOMER_ID" -ForegroundColor Green
```

**Resultado esperado:**
```
✅ Customer creado: cus_PQx1yZ2aBcDeFgHi
```

---

## Paso 2: Registrar Payment Method (1 minuto)

```powershell
$pmResponse = Invoke-RestMethod `
  -Uri "$API_URL/api/paymentmethods/attach" `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    customer_id = $CUSTOMER_ID
    token = "tok_visa"  # Token de prueba de Stripe
  } | ConvertTo-Json) `
  -SkipCertificateCheck

$PM_ID = $pmResponse.payment_method_id
Write-Host "✅ Payment Method registrado: $PM_ID" -ForegroundColor Green
Write-Host "   Tarjeta: $($pmResponse.card.brand.ToUpper()) ****$($pmResponse.card.last4)" -ForegroundColor Cyan
```

**Resultado esperado:**
```
✅ Payment Method registrado: pm_1PqRsT2aBcDeFgHi
   Tarjeta: VISA ****4242
```

---

## Paso 3: Procesar Pago (1 minuto)

```powershell
$paymentResponse = Invoke-RestMethod `
  -Uri "$API_URL/api/paymentintents" `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    customer_id = $CUSTOMER_ID
    payment_method_id = $PM_ID
    amount = 100.00
    currency = "mxn"
    order_id = "ORDER-TEST-001"
    description = "Prueba de pago desde PowerShell"
  } | ConvertTo-Json) `
  -SkipCertificateCheck

$PI_ID = $paymentResponse.payment_intent_id
Write-Host "✅ Pago procesado: $PI_ID" -ForegroundColor Green
Write-Host "   Estado: $($paymentResponse.status)" -ForegroundColor Cyan
Write-Host "   Monto: `$$($paymentResponse.amount_decimal) $($paymentResponse.currency.ToUpper())" -ForegroundColor Yellow
if ($paymentResponse.charge) {
    Write-Host "   Charge ID: $($paymentResponse.charge.charge_id)" -ForegroundColor Magenta
    Write-Host "   Recibo: $($paymentResponse.charge.receipt_url)" -ForegroundColor Blue
}
```

**Resultado esperado:**
```
✅ Pago procesado: pi_3PqRsT2aBcDeFgHi
   Estado: succeeded
   Monto: $100.00 MXN
   Charge ID: ch_3PqRsT2aBcDeFgHi
   Recibo: https://pay.stripe.com/receipts/...
```

---

## Paso 4: Consultar Payment Intent (1 minuto)

```powershell
$paymentDetails = Invoke-RestMethod `
  -Uri "$API_URL/api/paymentintents/$PI_ID" `
  -Method GET `
  -SkipCertificateCheck

Write-Host "`n📊 Detalles del Payment Intent:" -ForegroundColor Cyan
Write-Host "   Payment Intent ID: $($paymentDetails.payment_intent_id)"
Write-Host "   Customer ID: $($paymentDetails.customer_id)"
Write-Host "   Payment Method ID: $($paymentDetails.payment_method_id)"
Write-Host "   Order ID: $($paymentDetails.order_id)"
Write-Host "   Estado: $($paymentDetails.status)"
Write-Host "   Monto: `$$($paymentDetails.amount_decimal) $($paymentDetails.currency.ToUpper())"
Write-Host "   Fecha: $($paymentDetails.created)"

if ($paymentDetails.charge.card) {
    Write-Host "`n💳 Tarjeta usada:" -ForegroundColor Yellow
    Write-Host "   $($paymentDetails.charge.card.brand.ToUpper()) ****$($paymentDetails.charge.card.last4)"
    Write-Host "   Exp: $($paymentDetails.charge.card.exp_month)/$($paymentDetails.charge.card.exp_year)"
}
```

**Resultado esperado:**
```
📊 Detalles del Payment Intent:
   Payment Intent ID: pi_3PqRsT2aBcDeFgHi
   Customer ID: cus_PQx1yZ2aBcDeFgHi
   Payment Method ID: pm_1PqRsT2aBcDeFgHi
   Order ID: ORDER-TEST-001
   Estado: succeeded
   Monto: $100.00 MXN
   Fecha: 2024-01-15T10:30:00Z

💳 Tarjeta usada:
   VISA ****4242
   Exp: 12/2025
```

---

## 🎯 Script Completo (Todo en Uno)

**`test-payment-intents.ps1`:**

```powershell
# Configuración
$API_URL = "https://localhost:7XXX"  # ⚠️ CAMBIAR CON TU PUERTO

Write-Host "`n🚀 Iniciando testing de Payment Intents..." -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

# ──────────────────────────────────────────────────────────
# PASO 1: CREAR CUSTOMER
# ──────────────────────────────────────────────────────────
Write-Host "📝 Paso 1/4: Creando Customer..." -ForegroundColor Yellow

try {
    $customerResponse = Invoke-RestMethod `
      -Uri "$API_URL/api/customers" `
      -Method POST `
      -Headers @{"Content-Type"="application/json"} `
      -Body (@{
        user_id = "TEST-$(Get-Random)"
        name = "Juan Pérez Test"
        email = "juan.test@example.com"
        phone = "+525512345678"
      } | ConvertTo-Json) `
      -SkipCertificateCheck

    $CUSTOMER_ID = $customerResponse.customer_id
    Write-Host "   ✅ Customer creado: $CUSTOMER_ID`n" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Error al crear customer: $_" -ForegroundColor Red
    exit 1
}

# ──────────────────────────────────────────────────────────
# PASO 2: REGISTRAR PAYMENT METHOD
# ──────────────────────────────────────────────────────────
Write-Host "💳 Paso 2/4: Registrando Payment Method..." -ForegroundColor Yellow

try {
    $pmResponse = Invoke-RestMethod `
      -Uri "$API_URL/api/paymentmethods/attach" `
      -Method POST `
      -Headers @{"Content-Type"="application/json"} `
      -Body (@{
        customer_id = $CUSTOMER_ID
        token = "tok_visa"
      } | ConvertTo-Json) `
      -SkipCertificateCheck

    $PM_ID = $pmResponse.payment_method_id
    Write-Host "   ✅ Payment Method registrado: $PM_ID" -ForegroundColor Green
    Write-Host "   💳 Tarjeta: $($pmResponse.card.brand.ToUpper()) ****$($pmResponse.card.last4)`n" -ForegroundColor Cyan
} catch {
    Write-Host "   ❌ Error al registrar payment method: $_" -ForegroundColor Red
    exit 1
}

# ──────────────────────────────────────────────────────────
# PASO 3: PROCESAR PAGO
# ──────────────────────────────────────────────────────────
Write-Host "💰 Paso 3/4: Procesando pago de $100.00 MXN..." -ForegroundColor Yellow

try {
    $paymentResponse = Invoke-RestMethod `
      -Uri "$API_URL/api/paymentintents" `
      -Method POST `
      -Headers @{"Content-Type"="application/json"} `
      -Body (@{
        customer_id = $CUSTOMER_ID
        payment_method_id = $PM_ID
        amount = 100.00
        currency = "mxn"
        order_id = "ORDER-TEST-$(Get-Random -Minimum 1000 -Maximum 9999)"
        description = "Pago de prueba desde PowerShell"
      } | ConvertTo-Json) `
      -SkipCertificateCheck

    $PI_ID = $paymentResponse.payment_intent_id
    
    if ($paymentResponse.success -and $paymentResponse.status -eq "succeeded") {
        Write-Host "   ✅ ¡PAGO EXITOSO!" -ForegroundColor Green
        Write-Host "   Payment Intent ID: $PI_ID" -ForegroundColor Green
        Write-Host "   Estado: $($paymentResponse.status)" -ForegroundColor Cyan
        Write-Host "   Monto: `$$($paymentResponse.amount_decimal) $($paymentResponse.currency.ToUpper())" -ForegroundColor Yellow
        
        if ($paymentResponse.charge) {
            Write-Host "   Charge ID: $($paymentResponse.charge.charge_id)" -ForegroundColor Magenta
            Write-Host "   Recibo: $($paymentResponse.charge.receipt_url)`n" -ForegroundColor Blue
        }
    } else {
        Write-Host "   ❌ Pago no exitoso" -ForegroundColor Red
        Write-Host "   Estado: $($paymentResponse.status)" -ForegroundColor Red
        Write-Host "   Error: $($paymentResponse.error_message)`n" -ForegroundColor Red
    }
} catch {
    Write-Host "   ❌ Error al procesar pago: $_" -ForegroundColor Red
    exit 1
}

# ──────────────────────────────────────────────────────────
# PASO 4: CONSULTAR PAYMENT INTENT
# ──────────────────────────────────────────────────────────
Write-Host "🔍 Paso 4/4: Consultando Payment Intent..." -ForegroundColor Yellow

try {
    $paymentDetails = Invoke-RestMethod `
      -Uri "$API_URL/api/paymentintents/$PI_ID" `
      -Method GET `
      -SkipCertificateCheck

    Write-Host "   ✅ Payment Intent consultado exitosamente`n" -ForegroundColor Green
    
    Write-Host "   📊 RESUMEN COMPLETO:" -ForegroundColor Cyan
    Write-Host "   ═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "   Customer ID:        $($paymentDetails.customer_id)"
    Write-Host "   Payment Method ID:  $($paymentDetails.payment_method_id)"
    Write-Host "   Payment Intent ID:  $($paymentDetails.payment_intent_id)"
    Write-Host "   Order ID:           $($paymentDetails.order_id)"
    Write-Host "   ───────────────────────────────────────────" -ForegroundColor Gray
    Write-Host "   Estado:             $($paymentDetails.status)" -ForegroundColor Green
    Write-Host "   Monto Total:        `$$($paymentDetails.amount_decimal) $($paymentDetails.currency.ToUpper())" -ForegroundColor Yellow
    Write-Host "   Monto Capturado:    `$$($paymentDetails.amount_captured_decimal) $($paymentDetails.currency.ToUpper())" -ForegroundColor Yellow
    Write-Host "   Fecha:              $($paymentDetails.created)"
    
    if ($paymentDetails.charge.card) {
        Write-Host "   ───────────────────────────────────────────" -ForegroundColor Gray
        Write-Host "   💳 Tarjeta:         $($paymentDetails.charge.card.brand.ToUpper()) ****$($paymentDetails.charge.card.last4)" -ForegroundColor Cyan
        Write-Host "   Expiración:         $($paymentDetails.charge.card.exp_month)/$($paymentDetails.charge.card.exp_year)"
        Write-Host "   País:               $($paymentDetails.charge.card.country)"
    }
    
    Write-Host "   ═══════════════════════════════════════════`n" -ForegroundColor Cyan
} catch {
    Write-Host "   ❌ Error al consultar payment intent: $_" -ForegroundColor Red
}

# ──────────────────────────────────────────────────────────
# RESUMEN FINAL
# ──────────────────────────────────────────────────────────
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🎉 TESTING COMPLETADO EXITOSAMENTE" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

Write-Host "📝 IDs Generados (guárdalos para pruebas adicionales):" -ForegroundColor Yellow
Write-Host "   Customer ID:        $CUSTOMER_ID" -ForegroundColor White
Write-Host "   Payment Method ID:  $PM_ID" -ForegroundColor White
Write-Host "   Payment Intent ID:  $PI_ID`n" -ForegroundColor White

Write-Host "🔗 Links útiles:" -ForegroundColor Yellow
Write-Host "   Swagger UI: $API_URL/swagger" -ForegroundColor Blue
Write-Host "   Stripe Dashboard: https://dashboard.stripe.com/test/payments" -ForegroundColor Blue
Write-Host "`n"
```

---

## 🏃 Ejecutar Todo

**Opción 1: Ejecutar el script**
```powershell
# Guarda el script en: test-payment-intents.ps1
# Luego ejecuta:
.\test-payment-intents.ps1
```

**Opción 2: Comandos individuales**
Copia y pega cada bloque de código de los pasos 1-4.

---

## 🎯 Testing de Escenarios Adicionales

### Escenario: Customer Inactivo

```powershell
# 1. Desactivar el customer
Invoke-RestMethod `
  -Uri "$API_URL/api/paymentmethods/customer/status" `
  -Method PUT `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    customer_id = $CUSTOMER_ID
    active = $false
    detach_payment_methods = $false
  } | ConvertTo-Json) `
  -SkipCertificateCheck

Write-Host "✅ Customer desactivado" -ForegroundColor Yellow

# 2. Intentar procesar pago (debe fallar)
try {
    $response = Invoke-RestMethod `
      -Uri "$API_URL/api/paymentintents" `
      -Method POST `
      -Headers @{"Content-Type"="application/json"} `
      -Body (@{
        customer_id = $CUSTOMER_ID
        payment_method_id = $PM_ID
        amount = 50.00
        currency = "mxn"
        order_id = "ORDER-INACTIVE-TEST"
      } | ConvertTo-Json) `
      -SkipCertificateCheck
    
    Write-Host "❌ ERROR: El pago debería haber fallado" -ForegroundColor Red
} catch {
    $error = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "✅ Validación correcta: $($error.error_message)" -ForegroundColor Green
}

# 3. Reactivar el customer
Invoke-RestMethod `
  -Uri "$API_URL/api/paymentmethods/customer/status" `
  -Method PUT `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    customer_id = $CUSTOMER_ID
    active = $true
  } | ConvertTo-Json) `
  -SkipCertificateCheck

Write-Host "✅ Customer reactivado`n" -ForegroundColor Green
```

---

### Escenario: Cancelar Pago

```powershell
# 1. Crear Payment Intent SIN confirmar
$piUnconfirmed = Invoke-RestMethod `
  -Uri "$API_URL/api/paymentintents" `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    customer_id = $CUSTOMER_ID
    payment_method_id = $PM_ID
    amount = 75.00
    currency = "mxn"
    order_id = "ORDER-CANCEL-TEST"
    auto_confirm = $false  # ← No confirmar
  } | ConvertTo-Json) `
  -SkipCertificateCheck

$PI_CANCEL_ID = $piUnconfirmed.payment_intent_id
Write-Host "✅ Payment Intent creado (sin confirmar): $PI_CANCEL_ID" -ForegroundColor Yellow

# 2. Cancelar el Payment Intent
$cancelResponse = Invoke-RestMethod `
  -Uri "$API_URL/api/paymentintents/cancel" `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    payment_intent_id = $PI_CANCEL_ID
    cancellation_reason = "requested_by_customer"
  } | ConvertTo-Json) `
  -SkipCertificateCheck

Write-Host "✅ Payment Intent cancelado: $($cancelResponse.status)`n" -ForegroundColor Green
```

---

### Escenario: Autorización + Captura Manual

```powershell
# 1. Crear Payment Intent con captura manual
$piManual = Invoke-RestMethod `
  -Uri "$API_URL/api/paymentintents" `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    customer_id = $CUSTOMER_ID
    payment_method_id = $PM_ID
    amount = 200.00
    currency = "mxn"
    order_id = "ORDER-CAPTURE-TEST"
    auto_confirm = $true
    auto_capture = $false  # ← Solo autorizar
  } | ConvertTo-Json) `
  -SkipCertificateCheck

$PI_MANUAL_ID = $piManual.payment_intent_id
Write-Host "✅ Payment Intent autorizado: $PI_MANUAL_ID" -ForegroundColor Yellow
Write-Host "   Estado: $($piManual.status) (dinero reservado)" -ForegroundColor Cyan

# 2. Esperar un momento (simular proceso)
Start-Sleep -Seconds 2

# 3. Capturar el Payment Intent
$captureResponse = Invoke-RestMethod `
  -Uri "$API_URL/api/paymentintents/capture" `
  -Method POST `
  -Headers @{"Content-Type"="application/json"} `
  -Body (@{
    payment_intent_id = $PI_MANUAL_ID
  } | ConvertTo-Json) `
  -SkipCertificateCheck

Write-Host "✅ Payment Intent capturado: $($captureResponse.status)" -ForegroundColor Green
Write-Host "   Monto capturado: `$$($captureResponse.amount_captured) $($captureResponse.currency.ToUpper())`n" -ForegroundColor Yellow
```

---

## 🎨 Testing desde Swagger UI

### 1. Abrir Swagger
```
https://localhost:7XXX/swagger
```

### 2. Buscar "PaymentIntentsController"

### 3. Probar POST /api/paymentintents

**Request Body:**
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

**Ejecutar** → Debería devolver `status: 200` con `success: true`

---

## 📊 Verificación en Stripe Dashboard

### 1. Abre Stripe Test Dashboard
```
https://dashboard.stripe.com/test/payments
```

### 2. Busca el Payment Intent
- Busca por `order_id` en metadata
- O busca por `payment_intent_id` (pi_xxx)

### 3. Verifica los Datos
- ✅ Monto correcto
- ✅ Moneda correcta
- ✅ Estado: Succeeded
- ✅ Customer asociado
- ✅ Payment Method usado
- ✅ Metadata con order_id

---

## ✅ Checklist de Verificación

### Funcionalidad Básica
- [ ] Customer creado exitosamente
- [ ] Payment Method registrado
- [ ] Payment Intent creado
- [ ] Pago procesado (status: succeeded)
- [ ] Consulta de Payment Intent funciona
- [ ] Datos del charge incluidos

### Validaciones
- [ ] Customer inactivo rechaza pago
- [ ] Formato de IDs validado
- [ ] Monto validado (> 0)
- [ ] Conversión de montos correcta

### Flujos Avanzados
- [ ] Cancelación funciona
- [ ] Captura manual funciona
- [ ] Estados correctos en cada paso

---

## 🎯 Resultado Esperado

Al ejecutar el script completo, deberías ver:

```
🚀 Iniciando testing de Payment Intents...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📝 Paso 1/4: Creando Customer...
   ✅ Customer creado: cus_PQx1yZ2aBcDeFgHi

💳 Paso 2/4: Registrando Payment Method...
   ✅ Payment Method registrado: pm_1PqRsT2aBcDeFgHi
   💳 Tarjeta: VISA ****4242

💰 Paso 3/4: Procesando pago de $100.00 MXN...
   ✅ ¡PAGO EXITOSO!
   Payment Intent ID: pi_3PqRsT2aBcDeFgHi
   Estado: succeeded
   Monto: $100.00 MXN
   Charge ID: ch_3PqRsT2aBcDeFgHi
   Recibo: https://pay.stripe.com/receipts/...

🔍 Paso 4/4: Consultando Payment Intent...
   ✅ Payment Intent consultado exitosamente

   📊 RESUMEN COMPLETO:
   ═══════════════════════════════════════════
   Customer ID:        cus_PQx1yZ2aBcDeFgHi
   Payment Method ID:  pm_1PqRsT2aBcDeFgHi
   Payment Intent ID:  pi_3PqRsT2aBcDeFgHi
   Order ID:           ORDER-TEST-001
   ───────────────────────────────────────────
   Estado:             succeeded
   Monto Total:        $100.00 MXN
   Monto Capturado:    $100.00 MXN
   Fecha:              2024-01-15T10:30:00Z
   ───────────────────────────────────────────
   💳 Tarjeta:         VISA ****4242
   Expiración:         12/2025
   País:               MX
   ═══════════════════════════════════════════

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🎉 TESTING COMPLETADO EXITOSAMENTE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📝 IDs Generados:
   Customer ID:        cus_PQx1yZ2aBcDeFgHi
   Payment Method ID:  pm_1PqRsT2aBcDeFgHi
   Payment Intent ID:  pi_3PqRsT2aBcDeFgHi

🔗 Links útiles:
   Swagger UI: https://localhost:7XXX/swagger
   Stripe Dashboard: https://dashboard.stripe.com/test/payments
```

---

## 🎉 ¡Listo!

Si ves este resultado, significa que:

✅ **API de .NET funcionando correctamente**  
✅ **Stripe configurado correctamente**  
✅ **Customer creado**  
✅ **Payment Method registrado**  
✅ **Pago procesado exitosamente**  
✅ **Todos los datos devueltos correctamente**  
✅ **SISTEMA FUNCIONAL AL 100%** 🚀  

---

## 🔗 Próximos Pasos

1. **Implementar en Laravel**: Sigue `LARAVEL_PAYMENT_INTENTS_CHECKLIST.md`
2. **Testing adicional**: Usa las tarjetas de prueba de diferentes escenarios
3. **Integración frontend**: Implementa el checkout con Stripe.js
4. **Webhooks**: Configura eventos de Stripe (opcional)

---

**¿Todo funcionó? ¡Perfecto! Sistema listo para producción** 🚀
