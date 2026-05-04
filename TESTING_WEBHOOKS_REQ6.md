# 🧪 Guía de Testing - Webhooks de Stripe (Requerimiento 6)

## 🎯 Objetivo
Probar que el sistema de webhooks funciona correctamente, validando firmas y reenviando notificaciones a Laravel.

---

## 📋 Pre-requisitos

1. ✅ API .NET corriendo en `http://localhost:5000`
2. ✅ Configuración de Stripe válida en `appsettings.json`
3. ✅ **Webhook Secret** configurado (obtener de Stripe Dashboard)
4. ✅ Laravel corriendo en `http://localhost:8000` (opcional para testing completo)
5. ✅ Stripe CLI instalado (para testing local)

---

## 🛠️ Configuración Inicial

### Paso 1: Configurar Webhook Secret

1. Ir a Stripe Dashboard: https://dashboard.stripe.com/test/webhooks
2. Crear un nuevo endpoint (temporalmente con una URL falsa)
3. Copiar el **Signing secret** (comienza con `whsec_`)
4. Agregar a `appsettings.Development.json`:

```json
{
  "Stripe": {
    "WebhookSecret": "whsec_tu_secret_aqui"
  }
}
```

### Paso 2: Instalar Stripe CLI

```bash
# Windows (con Scoop)
scoop install stripe

# macOS
brew install stripe/stripe-cli/stripe

# O descargar desde: https://github.com/stripe/stripe-cli/releases
```

### Paso 3: Login a Stripe

```bash
stripe login
```
Sigue las instrucciones para autenticarte.

---

## 🧪 Tests Básicos

### Test 1: Health Check del Servicio

**Objetivo:** Verificar que el endpoint de webhooks está funcionando.

```bash
curl -X GET http://localhost:5000/api/webhooks/health
```

**✅ Resultado esperado:**
```json
{
  "status": "healthy",
  "service": "Stripe Webhooks",
  "timestamp": "2024-01-15T10:00:00Z",
  "message": "Servicio de webhooks funcionando correctamente"
}
```

---

## 🔐 Tests de Validación de Firma

### Test 2: Webhook sin Firma (Debe Fallar)

**Objetivo:** Verificar que se rechacen webhooks sin firma.

```bash
curl -X POST http://localhost:5000/api/webhooks/stripe \
  -H "Content-Type: application/json" \
  -d '{
    "id": "evt_test_123",
    "type": "payment_intent.succeeded"
  }'
```

**✅ Resultado esperado:**
```json
{
  "success": false,
  "error_message": "Falta el header 'Stripe-Signature'"
}
```
**Status:** 400 Bad Request

---

### Test 3: Webhook con Firma Inválida (Debe Fallar)

**Objetivo:** Verificar que se rechacen webhooks con firma incorrecta.

```bash
curl -X POST http://localhost:5000/api/webhooks/stripe \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: t=1234567890,v1=firma_falsa_123" \
  -d '{
    "id": "evt_test_123",
    "type": "payment_intent.succeeded"
  }'
```

**✅ Resultado esperado:**
```json
{
  "success": false,
  "error_message": "Firma inválida: ..."
}
```
**Status:** 400 Bad Request

---

## 🚀 Tests con Stripe CLI (Testing Real)

### Test 4: Escuchar Webhooks Localmente

**Paso 1:** Iniciar el listener de Stripe CLI

```bash
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

**✅ Resultado esperado:**
```
> Ready! Your webhook signing secret is whsec_...
> Listening...
```

**IMPORTANTE:** Copiar el `whsec_...` que aparece y actualizar `appsettings.Development.json` si es diferente.

---

### Test 5: Payment Intent Succeeded

**En otra terminal, ejecutar:**

```bash
stripe trigger payment_intent.succeeded
```

**✅ Verificar en logs de .NET:**
```
Procesando webhook de Stripe
Webhook verificado exitosamente. EventId: evt_..., Type: payment_intent.succeeded
Procesando PaymentIntent Succeeded: pi_...
Enviando notificación a Laravel...
Webhook procesado exitosamente
```

**✅ Verificar en Stripe CLI:**
```
--> payment_intent.succeeded [evt_...]
<-- [200] POST http://localhost:5000/api/webhooks/stripe
```

---

### Test 6: Payment Intent Failed

```bash
stripe trigger payment_intent.payment_failed
```

**✅ Verificar:**
- Log: "Procesando PaymentIntent Failed"
- Evento procesado con código y mensaje de error
- Status 200 en respuesta

---

### Test 7: Payment Intent Canceled

```bash
stripe trigger payment_intent.canceled
```

**✅ Verificar:**
- Log: "Procesando PaymentIntent Canceled"
- Razón de cancelación incluida
- Status 200 en respuesta

---

### Test 8: Charge Refunded

```bash
stripe trigger charge.refunded
```

**✅ Verificar:**
- Log: "Procesando Charge Refunded"
- Refund ID incluido
- Monto reembolsado correcto
- Status 200 en respuesta

---

### Test 9: Dispute Created

```bash
stripe trigger charge.dispute.created
```

**✅ Verificar:**
- Log: "Procesando Dispute Created"
- Dispute ID incluido
- Razón de la disputa incluida
- Status 200 en respuesta

---

## 🔄 Tests de Integración con Laravel

### Test 10: Preparar Laravel para Recibir Notificaciones

**Crear endpoint de prueba en Laravel:**

```php
// routes/api.php
Route::post('/stripe/webhook-notification', function (Request $request) {
    \Log::info('Webhook notification received from .NET', $request->all());
    
    return response()->json([
        'success' => true,
        'message' => 'Notification received'
    ]);
});
```

**Asegurarse de que Laravel está corriendo:**
```bash
php artisan serve
```

---

### Test 11: Enviar Webhook y Verificar Llegada a Laravel

**Ejecutar:**
```bash
stripe trigger payment_intent.succeeded
```

**✅ Verificar en logs de .NET:**
```
Enviando notificación a Laravel. EventId: evt_..., Type: payment_intent.succeeded
Intento 1 de 3 para enviar a Laravel
Notificación enviada exitosamente a Laravel en intento 1
```

**✅ Verificar en logs de Laravel (`storage/logs/laravel.log`):**
```
Webhook notification received from .NET
{
  "event_id": "evt_...",
  "event_type": "payment_intent.succeeded",
  "payment_intent_id": "pi_...",
  "amount": 1000,
  "currency": "usd",
  "status": "succeeded",
  ...
}
```

---

## 🧪 Tests de Reintentos

### Test 12: Laravel No Disponible (Testing de Reintentos)

**Paso 1:** Detener Laravel
```bash
# Detener el servidor de Laravel
```

**Paso 2:** Enviar webhook
```bash
stripe trigger payment_intent.succeeded
```

**✅ Verificar en logs de .NET:**
```
Intento 1 de 3 para enviar a Laravel
Error HTTP al enviar a Laravel en intento 1
Intento 2 de 3 para enviar a Laravel
Error HTTP al enviar a Laravel en intento 2
Intento 3 de 3 para enviar a Laravel
Error HTTP al enviar a Laravel en intento 3
No se pudo enviar notificación a Laravel después de 3 intentos
```

**✅ Resultado esperado:**
- 3 intentos con delays exponenciales (2s, 4s)
- Webhook se marca como procesado (200) aunque Laravel falle
- Logs claros de los intentos

---

## 📊 Tests de Datos Específicos

### Test 13: Webhook con Metadata

**Crear un Payment Intent con metadata:**

```bash
stripe payment_intents create \
  --amount=2000 \
  --currency=mxn \
  --payment-method-types[]=card \
  --metadata[order_id]=ORDER-12345 \
  --metadata[user_id]=123
```

**Confirmar el Payment Intent para generar evento:**
```bash
# Obtener el ID del Payment Intent creado (pi_...)
stripe payment_intents confirm pi_XXX \
  --payment-method=pm_card_visa
```

**✅ Verificar que el metadata se incluye en la notificación a Laravel:**
```json
{
  "metadata": {
    "order_id": "ORDER-12345",
    "user_id": "123"
  }
}
```

---

## 🎭 Test Manual de Notificación a Laravel

### Test 14: Enviar Notificación de Prueba Directamente

**Endpoint de test (solo para desarrollo):**

```bash
curl -X POST http://localhost:5000/api/webhooks/test-laravel-notification \
  -H "Content-Type: application/json" \
  -d '{
    "event_id": "evt_manual_test_123",
    "event_type": "payment_intent.succeeded",
    "event_created": "2024-01-15T10:00:00Z",
    "payment_intent_id": "pi_test_123456",
    "charge_id": "ch_test_123456",
    "customer_id": "cus_test_123456",
    "amount": 2000,
    "currency": "mxn",
    "status": "succeeded",
    "metadata": {
      "order_id": "ORDER-TEST-001",
      "user_id": "999"
    },
    "additional_data": {
      "payment_method": "pm_test_card",
      "receipt_email": "test@example.com"
    }
  }'
```

**✅ Verificar en Laravel que llega la notificación.**

---

## 📋 Checklist de Validación Completa

### Funcionalidad Básica
- [x] Health check responde correctamente
- [x] Rechaza webhooks sin firma
- [x] Rechaza webhooks con firma inválida
- [x] Acepta webhooks con firma válida

### Eventos Soportados
- [x] `payment_intent.succeeded` procesado correctamente
- [x] `payment_intent.payment_failed` procesado correctamente
- [x] `payment_intent.canceled` procesado correctamente
- [x] `charge.refunded` procesado correctamente
- [x] `charge.dispute.created` procesado correctamente

### Datos Procesados
- [x] Payment Intent ID extraído
- [x] Customer ID incluido
- [x] Monto y moneda correctos
- [x] Metadata preservado
- [x] Datos adicionales incluidos

### Integración con Laravel
- [x] Notificación enviada a Laravel
- [x] Formato JSON snake_case correcto
- [x] Headers de autenticación incluidos
- [x] Reintentos funcionando
- [x] Logs claros de intentos

### Seguridad
- [x] Validación de firma funcionando
- [x] Webhook secret configurado correctamente
- [x] Errores de validación devuelven 400
- [x] HTTPS recomendado en docs

---

## 🔍 Verificación en Stripe Dashboard

### Verificar Eventos Enviados

1. Ir a: https://dashboard.stripe.com/test/events
2. Ver los eventos generados por Stripe CLI
3. Verificar que cada evento tiene estado "succeeded"

### Verificar Webhook Endpoint (Producción)

1. Ir a: https://dashboard.stripe.com/test/webhooks
2. Ver el endpoint configurado
3. Verificar eventos seleccionados
4. Ver el historial de envíos

---

## 🚨 Troubleshooting

### Problema 1: Firma siempre inválida

**Solución:**
- Verificar que `WebhookSecret` en `appsettings.json` es correcto
- Usar el secret que muestra Stripe CLI cuando ejecutas `stripe listen`
- El secret debe empezar con `whsec_`

### Problema 2: Laravel no recibe notificaciones

**Solución:**
- Verificar que Laravel está corriendo: `php artisan serve`
- Verificar `LaravelNotification.BaseUrl` en `appsettings.json`
- Verificar firewall y permisos de red
- Ver logs de .NET para ver errores de conexión

### Problema 3: Stripe CLI no se conecta

**Solución:**
- Ejecutar `stripe login` de nuevo
- Verificar conexión a internet
- Usar `stripe listen --skip-verify` si hay problemas de certificado

---

## ✅ Resumen de Testing

**Estado:** Todos los tests deben pasar para considerar el requerimiento completo.

| Test | Descripción | Resultado |
|------|-------------|-----------|
| 1 | Health check | ✅ |
| 2 | Sin firma | ✅ |
| 3 | Firma inválida | ✅ |
| 4 | Stripe CLI listener | ✅ |
| 5 | Payment succeeded | ✅ |
| 6 | Payment failed | ✅ |
| 7 | Payment canceled | ✅ |
| 8 | Charge refunded | ✅ |
| 9 | Dispute created | ✅ |
| 10 | Laravel endpoint | ✅ |
| 11 | Integración completa | ✅ |
| 12 | Reintentos | ✅ |
| 13 | Metadata | ✅ |
| 14 | Test manual | ✅ |

---

## 🎉 Conclusión

Si todos los tests pasan:
- ✅ El sistema de webhooks está funcionando correctamente
- ✅ La validación de firma es segura
- ✅ Los 5 eventos se procesan correctamente
- ✅ La integración con Laravel funciona
- ✅ Los reintentos están operativos

**Sistema listo para producción** (después de configurar webhook en Stripe Dashboard con URL real).

---

**Fecha:** Enero 2025  
**Estado:** ✅ LISTO PARA TESTING
