# 📡 Guía de API de Webhooks de Stripe

## Descripción General

Esta API proporciona un endpoint seguro para recibir y procesar **webhooks de Stripe** en tiempo real. Los eventos procesados se transforman en notificaciones internas que se reenvían automáticamente a Laravel para actualizar las tablas de órdenes y transacciones.

---

## 🔐 Seguridad y Validación

### Validación de Firma

Todos los webhooks de Stripe son validados utilizando la **firma criptográfica** (`Stripe-Signature` header) para garantizar que:
- ✅ El webhook proviene realmente de Stripe
- ✅ El contenido no ha sido modificado
- ✅ No es un ataque de replay

La validación se realiza automáticamente usando el `WebhookSecret` configurado en `appsettings.json`.

---

## 📋 Eventos Soportados

El sistema escucha y procesa los siguientes eventos de Stripe:

### 1. `payment_intent.succeeded`
**Descripción:** Payment Intent completado exitosamente.

**Cuándo ocurre:**
- El pago fue confirmado y el dinero fue capturado
- El customer completó el pago exitosamente

**Datos procesados:**
- Payment Intent ID
- Monto y moneda
- Customer ID
- Charge ID
- Metadata

---

### 2. `payment_intent.payment_failed`
**Descripción:** Payment Intent falló al procesar el pago.

**Cuándo ocurre:**
- La tarjeta fue declinada
- Fondos insuficientes
- Error en el procesamiento del pago
- Límite de tarjeta excedido

**Datos procesados:**
- Payment Intent ID
- Razón del fallo
- Código de error
- Mensaje de error
- Customer ID
- Metadata

---

### 3. `payment_intent.canceled`
**Descripción:** Payment Intent cancelado.

**Cuándo ocurre:**
- El Payment Intent fue cancelado manualmente
- Expiró el tiempo de pago
- Se canceló desde la API

**Datos procesados:**
- Payment Intent ID
- Razón de cancelación
- Customer ID
- Fecha de cancelación
- Metadata

---

### 4. `charge.refunded`
**Descripción:** Un cargo fue reembolsado (total o parcialmente).

**Cuándo ocurre:**
- Se procesó un refund desde Stripe Dashboard
- Se procesó un refund desde la API
- Refund automático por política de la empresa

**Datos procesados:**
- Charge ID
- Refund ID
- Monto reembolsado
- Payment Intent ID (si aplica)
- Razón del refund
- Metadata

---

### 5. `charge.dispute.created`
**Descripción:** Un cliente creó una disputa (chargeback).

**Cuándo ocurre:**
- El cliente contactó a su banco para disputar el cargo
- Se inició un proceso de chargeback
- Fraude reportado

**Datos procesados:**
- Dispute ID
- Charge ID
- Monto disputado
- Razón de la disputa
- Fecha límite para evidencia
- Metadata

---

## 🌐 Endpoint Principal

### Recibir Webhook de Stripe

**Endpoint:** `POST /api/webhooks/stripe`

**Descripción:** Endpoint público para recibir eventos de Stripe. Este endpoint debe configurarse en el Stripe Dashboard.

**Headers Requeridos:**
```
Content-Type: application/json
Stripe-Signature: t=1234567890,v1=abc123...
```

**Request Body (manejado automáticamente por Stripe):**
```json
{
  "id": "evt_1Abc123xyz",
  "object": "event",
  "type": "payment_intent.succeeded",
  "created": 1234567890,
  "data": {
    "object": {
      "id": "pi_123456789",
      "amount": 2000,
      "currency": "mxn",
      "status": "succeeded",
      ...
    }
  }
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "event_id": "evt_1Abc123xyz",
  "event_type": "payment_intent.succeeded",
  "message": "Webhook procesado exitosamente",
  "sent_to_laravel": true,
  "laravel_notification_url": "http://localhost:8000/api/stripe/webhook-notification"
}
```

**Response (400 Bad Request - Firma inválida):**
```json
{
  "success": false,
  "error_message": "Firma inválida: The signature does not match the expected signature"
}
```

**Response (500 Internal Server Error):**
```json
{
  "success": false,
  "error_message": "Error interno: ..."
}
```

---

## 🔄 Flujo de Procesamiento

```
┌─────────┐      ┌─────────────┐      ┌──────────────┐      ┌─────────┐
│ Stripe  │─────▶│ .NET API    │─────▶│ Validación   │─────▶│ Laravel │
│ Event   │      │ /webhooks/  │      │ y Procesado  │      │ /api/   │
└─────────┘      └─────────────┘      └──────────────┘      └─────────┘
     │                  │                     │                    │
     │  1. POST         │  2. Valida firma    │  3. Envía         │
     │  webhook         │  con WebhookSecret  │  notificación     │
     │                  │                     │                    │
     │                  │  4. Procesa evento  │  4. Actualiza     │
     │                  │  según tipo         │  BD Laravel       │
     │                  │                     │                    │
     └──────────────────┴─────────────────────┴────────────────────┘
```

### Pasos del flujo:

1. **Stripe envía evento:** Cuando ocurre un evento (pago exitoso, refund, etc.)
2. **Validación de firma:** .NET valida que el webhook proviene de Stripe
3. **Procesamiento:** Se extrae y transforma la información relevante
4. **Notificación a Laravel:** Se envía la notificación procesada a Laravel
5. **Actualización en Laravel:** Laravel actualiza sus tablas de órdenes/transacciones

---

## 📤 Notificación a Laravel

### Formato de Notificación

Después de procesar un webhook, se envía la siguiente notificación a Laravel:

**Endpoint de Laravel:** `POST http://localhost:8000/api/stripe/webhook-notification`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer {token}  // Si está configurado
```

**Body (ejemplo para payment_intent.succeeded):**
```json
{
  "event_id": "evt_1Abc123xyz",
  "event_type": "payment_intent.succeeded",
  "event_created": "2024-01-15T10:30:00Z",
  "payment_intent_id": "pi_123456789",
  "charge_id": "ch_987654321",
  "customer_id": "cus_ABC123DEF",
  "amount": 2000,
  "currency": "mxn",
  "status": "succeeded",
  "failure_reason": null,
  "error_message": null,
  "metadata": {
    "order_id": "ORDER-12345",
    "user_id": "123"
  },
  "additional_data": {
    "payment_method": "pm_123456",
    "receipt_email": "customer@example.com",
    "description": "Compra de productos"
  }
}
```

**Body (ejemplo para payment_intent.payment_failed):**
```json
{
  "event_id": "evt_1Def456xyz",
  "event_type": "payment_intent.payment_failed",
  "event_created": "2024-01-15T10:35:00Z",
  "payment_intent_id": "pi_987654321",
  "customer_id": "cus_ABC123DEF",
  "amount": 1500,
  "currency": "mxn",
  "status": "failed",
  "failure_reason": "card_declined",
  "error_message": "Your card was declined",
  "metadata": {
    "order_id": "ORDER-12346",
    "user_id": "456"
  },
  "additional_data": {
    "payment_method": "pm_789012",
    "last_payment_error_type": "card_error",
    "last_payment_error_decline_code": "insufficient_funds"
  }
}
```

**Body (ejemplo para charge.refunded):**
```json
{
  "event_id": "evt_1Ghi789xyz",
  "event_type": "charge.refunded",
  "event_created": "2024-01-15T11:00:00Z",
  "charge_id": "ch_123456789",
  "refund_id": "re_987654321",
  "payment_intent_id": "pi_111222333",
  "customer_id": "cus_DEF456GHI",
  "amount": 1000,
  "currency": "mxn",
  "status": "refunded",
  "failure_reason": "requested_by_customer",
  "metadata": {
    "order_id": "ORDER-12347"
  },
  "additional_data": {
    "amount_refunded": 1000,
    "refunded": true,
    "refund_status": "succeeded"
  }
}
```

**Body (ejemplo para charge.dispute.created):**
```json
{
  "event_id": "evt_1Jkl012xyz",
  "event_type": "charge.dispute.created",
  "event_created": "2024-01-15T12:00:00Z",
  "charge_id": "ch_555666777",
  "payment_intent_id": "pi_444555666",
  "amount": 5000,
  "currency": "mxn",
  "status": "needs_response",
  "failure_reason": "fraudulent",
  "metadata": {
    "order_id": "ORDER-12348"
  },
  "additional_data": {
    "dispute_id": "dp_123456789",
    "evidence_details_due_by": "2024-01-30T23:59:59Z",
    "is_charge_refundable": true
  }
}
```

---

## ⚙️ Configuración

### 1. Configurar en .NET (appsettings.json)

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  },
  "LaravelNotification": {
    "BaseUrl": "http://localhost:8000",
    "WebhookEndpoint": "/api/stripe/webhook-notification",
    "AuthToken": "your_laravel_api_token_here",
    "TimeoutSeconds": 30,
    "RetryAttempts": 3,
    "Enabled": true
  }
}
```

### 2. Configurar Webhook en Stripe Dashboard

1. **Acceder a Stripe Dashboard:**
   - https://dashboard.stripe.com/webhooks

2. **Agregar endpoint:**
   - Click en "Add endpoint"
   - URL: `https://tu-dominio.com/api/webhooks/stripe`

3. **Seleccionar eventos:**
   - ☑️ `payment_intent.succeeded`
   - ☑️ `payment_intent.payment_failed`
   - ☑️ `payment_intent.canceled`
   - ☑️ `charge.refunded`
   - ☑️ `charge.dispute.created`

4. **Obtener Webhook Secret:**
   - Después de crear el endpoint, copiar el `Signing secret` (whsec_...)
   - Agregar a `appsettings.json` en `Stripe.WebhookSecret`

### 3. Testing Local con Stripe CLI

Para probar webhooks localmente:

```bash
# Instalar Stripe CLI
# https://stripe.com/docs/stripe-cli

# Login a Stripe
stripe login

# Escuchar webhooks y reenviarlos a tu local
stripe listen --forward-to localhost:5000/api/webhooks/stripe

# En otro terminal, enviar un evento de prueba
stripe trigger payment_intent.succeeded
```

---

## 🛠️ Implementación en Laravel

### Controlador de Laravel

```php
<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;
use App\Models\Order;
use App\Models\Transaction;
use Illuminate\Support\Facades\Log;

class StripeWebhookController extends Controller
{
    /**
     * Recibir notificación de webhook desde .NET API
     */
    public function handleWebhookNotification(Request $request)
    {
        try {
            $data = $request->all();

            Log::info('Webhook notification received', [
                'event_type' => $data['event_type'] ?? null,
                'event_id' => $data['event_id'] ?? null
            ]);

            // Procesar según el tipo de evento
            switch ($data['event_type']) {
                case 'payment_intent.succeeded':
                    $this->handlePaymentSucceeded($data);
                    break;

                case 'payment_intent.payment_failed':
                    $this->handlePaymentFailed($data);
                    break;

                case 'payment_intent.canceled':
                    $this->handlePaymentCanceled($data);
                    break;

                case 'charge.refunded':
                    $this->handleChargeRefunded($data);
                    break;

                case 'charge.dispute.created':
                    $this->handleDisputeCreated($data);
                    break;

                default:
                    Log::warning('Unknown event type', ['type' => $data['event_type']]);
            }

            return response()->json([
                'success' => true,
                'message' => 'Webhook processed successfully'
            ], 200);

        } catch (\Exception $e) {
            Log::error('Error processing webhook notification', [
                'error' => $e->getMessage(),
                'data' => $request->all()
            ]);

            return response()->json([
                'success' => false,
                'message' => 'Error processing webhook'
            ], 500);
        }
    }

    /**
     * Manejar pago exitoso
     */
    private function handlePaymentSucceeded(array $data)
    {
        $orderId = $data['metadata']['order_id'] ?? null;

        if (!$orderId) {
            Log::warning('Payment succeeded but no order_id in metadata');
            return;
        }

        $order = Order::where('id', $orderId)->first();

        if (!$order) {
            Log::warning('Order not found', ['order_id' => $orderId]);
            return;
        }

        // Actualizar orden
        $order->update([
            'status' => 'paid',
            'payment_intent_id' => $data['payment_intent_id'],
            'charge_id' => $data['charge_id'],
            'paid_at' => now()
        ]);

        // Crear/Actualizar transacción
        Transaction::updateOrCreate(
            ['payment_intent_id' => $data['payment_intent_id']],
            [
                'order_id' => $orderId,
                'amount' => $data['amount'] / 100, // Convertir de centavos
                'currency' => $data['currency'],
                'status' => 'succeeded',
                'stripe_event_id' => $data['event_id'],
                'processed_at' => now()
            ]
        );

        Log::info('Payment succeeded processed', ['order_id' => $orderId]);
    }

    /**
     * Manejar pago fallido
     */
    private function handlePaymentFailed(array $data)
    {
        $orderId = $data['metadata']['order_id'] ?? null;

        if (!$orderId) {
            return;
        }

        $order = Order::where('id', $orderId)->first();

        if ($order) {
            $order->update([
                'status' => 'payment_failed',
                'payment_error' => $data['error_message'],
                'payment_error_code' => $data['failure_reason']
            ]);
        }

        // Registrar transacción fallida
        Transaction::create([
            'order_id' => $orderId,
            'payment_intent_id' => $data['payment_intent_id'],
            'amount' => $data['amount'] / 100,
            'currency' => $data['currency'],
            'status' => 'failed',
            'error_message' => $data['error_message'],
            'error_code' => $data['failure_reason'],
            'stripe_event_id' => $data['event_id']
        ]);

        Log::info('Payment failed processed', ['order_id' => $orderId]);
    }

    /**
     * Manejar pago cancelado
     */
    private function handlePaymentCanceled(array $data)
    {
        $orderId = $data['metadata']['order_id'] ?? null;

        if (!$orderId) {
            return;
        }

        $order = Order::where('id', $orderId)->first();

        if ($order) {
            $order->update([
                'status' => 'canceled',
                'canceled_reason' => $data['failure_reason'],
                'canceled_at' => now()
            ]);
        }

        Log::info('Payment canceled processed', ['order_id' => $orderId]);
    }

    /**
     * Manejar reembolso
     */
    private function handleChargeRefunded(array $data)
    {
        $orderId = $data['metadata']['order_id'] ?? null;

        if (!$orderId) {
            return;
        }

        $order = Order::where('id', $orderId)->first();

        if ($order) {
            $order->update([
                'status' => 'refunded',
                'refund_id' => $data['refund_id'],
                'refunded_amount' => $data['amount'] / 100,
                'refunded_at' => now()
            ]);
        }

        // Registrar reembolso en transacciones
        Transaction::create([
            'order_id' => $orderId,
            'charge_id' => $data['charge_id'],
            'refund_id' => $data['refund_id'],
            'amount' => -($data['amount'] / 100), // Negativo para reembolso
            'currency' => $data['currency'],
            'status' => 'refunded',
            'stripe_event_id' => $data['event_id']
        ]);

        Log::info('Refund processed', ['order_id' => $orderId]);
    }

    /**
     * Manejar disputa creada
     */
    private function handleDisputeCreated(array $data)
    {
        $orderId = $data['metadata']['order_id'] ?? null;

        if (!$orderId) {
            return;
        }

        $order = Order::where('id', $orderId)->first();

        if ($order) {
            $order->update([
                'status' => 'disputed',
                'dispute_reason' => $data['failure_reason'],
                'disputed_at' => now()
            ]);
        }

        // Crear registro de disputa
        Transaction::create([
            'order_id' => $orderId,
            'charge_id' => $data['charge_id'],
            'amount' => $data['amount'] / 100,
            'currency' => $data['currency'],
            'status' => 'disputed',
            'dispute_reason' => $data['failure_reason'],
            'stripe_event_id' => $data['event_id']
        ]);

        // Notificar al equipo de soporte
        // dispatch(new NotifyDisputeCreated($order));

        Log::warning('Dispute created', ['order_id' => $orderId]);
    }
}
```

### Rutas de Laravel (routes/api.php)

```php
use App\Http\Controllers\Api\StripeWebhookController;

// Ruta para recibir notificaciones de webhooks desde .NET
Route::post('/stripe/webhook-notification', [StripeWebhookController::class, 'handleWebhookNotification'])
    ->middleware('api'); // o tu middleware de autenticación
```

### Migraciones de Laravel

```php
// Migration para orders
Schema::table('orders', function (Blueprint $table) {
    $table->string('payment_intent_id')->nullable();
    $table->string('charge_id')->nullable();
    $table->string('refund_id')->nullable();
    $table->string('payment_error')->nullable();
    $table->string('payment_error_code')->nullable();
    $table->string('canceled_reason')->nullable();
    $table->string('dispute_reason')->nullable();
    $table->timestamp('paid_at')->nullable();
    $table->timestamp('canceled_at')->nullable();
    $table->timestamp('refunded_at')->nullable();
    $table->timestamp('disputed_at')->nullable();
});

// Migration para transactions
Schema::create('transactions', function (Blueprint $table) {
    $table->id();
    $table->foreignId('order_id')->constrained()->onDelete('cascade');
    $table->string('payment_intent_id')->nullable();
    $table->string('charge_id')->nullable();
    $table->string('refund_id')->nullable();
    $table->decimal('amount', 10, 2);
    $table->string('currency');
    $table->string('status');
    $table->string('error_message')->nullable();
    $table->string('error_code')->nullable();
    $table->string('dispute_reason')->nullable();
    $table->string('stripe_event_id')->unique();
    $table->timestamp('processed_at')->nullable();
    $table->timestamps();
});
```

---

## 🧪 Testing

### Test de Health Check

```bash
curl -X GET http://localhost:5000/api/webhooks/health
```

**Response:**
```json
{
  "status": "healthy",
  "service": "Stripe Webhooks",
  "timestamp": "2024-01-15T10:00:00Z",
  "message": "Servicio de webhooks funcionando correctamente"
}
```

### Test con Stripe CLI

```bash
# Instalar Stripe CLI
stripe listen --forward-to localhost:5000/api/webhooks/stripe

# En otro terminal, enviar eventos de prueba
stripe trigger payment_intent.succeeded
stripe trigger payment_intent.payment_failed
stripe trigger payment_intent.canceled
stripe trigger charge.refunded
stripe trigger charge.dispute.created
```

### Test Manual (No recomendado en producción)

```bash
# Generar un evento de prueba en Stripe Dashboard
# y copiarlo para usar en el test

curl -X POST http://localhost:5000/api/webhooks/test-laravel-notification \
  -H "Content-Type: application/json" \
  -d '{
    "event_id": "evt_test_123",
    "event_type": "payment_intent.succeeded",
    "event_created": "2024-01-15T10:00:00Z",
    "payment_intent_id": "pi_test_123",
    "charge_id": "ch_test_123",
    "customer_id": "cus_test_123",
    "amount": 2000,
    "currency": "mxn",
    "status": "succeeded",
    "metadata": {
      "order_id": "ORDER-12345",
      "user_id": "123"
    }
  }'
```

---

## 🔄 Reintentos y Resiliencia

### Política de Reintentos

El sistema incluye reintentos automáticos al enviar notificaciones a Laravel:

- **Número de intentos:** Configurable (default: 3)
- **Estrategia:** Exponential backoff (2^intento segundos)
- **Timeouts:** Configurable (default: 30 segundos)

**Ejemplo de reintentos:**
1. Intento 1: Inmediato
2. Intento 2: Después de 2 segundos
3. Intento 3: Después de 4 segundos

### Manejo de Fallos

Si después de todos los reintentos no se puede enviar la notificación a Laravel:
- Se registra un error en los logs
- El webhook de Stripe se marca como procesado exitosamente (return 200)
- Se debe implementar un sistema de recuperación manual o queue en Laravel

---

## 📊 Logs y Monitoreo

### Logs de .NET

```csharp
// Webhook recibido
"Procesando webhook de Stripe"

// Webhook verificado
"Webhook verificado exitosamente. EventId: {EventId}, Type: {Type}"

// Evento procesado
"Procesando PaymentIntent Succeeded: {PaymentIntentId}"

// Notificación enviada
"Notificación enviada exitosamente a Laravel en intento {Attempt}"

// Error
"Error al verificar la firma del webhook"
"No se pudo enviar notificación a Laravel después de {Attempts} intentos"
```

### Monitoreo Recomendado

1. **Stripe Dashboard:** Revisar el estado de los webhooks
2. **Application Insights:** Monitorear errores y latencia
3. **Logs de Laravel:** Verificar que las notificaciones se procesen correctamente
4. **Alertas:** Configurar alertas para webhooks fallidos

---

## 🚨 Errores Comunes

### 1. Firma Inválida

**Causa:** `WebhookSecret` incorrecto o no configurado

**Solución:**
- Verificar que el `WebhookSecret` en `appsettings.json` coincide con el de Stripe Dashboard
- Asegurarse de que el secret comienza con `whsec_`

### 2. Laravel no recibe notificaciones

**Causa:** URL incorrecta o Laravel no está corriendo

**Solución:**
- Verificar `LaravelNotification.BaseUrl` en `appsettings.json`
- Verificar que Laravel está corriendo y accesible
- Revisar firewall y permisos de red

### 3. Evento no procesado

**Causa:** Tipo de evento no soportado

**Solución:**
- Verificar que el evento está en la lista de eventos soportados
- Agregar manejo para el nuevo tipo de evento si es necesario

---

## 🔐 Seguridad Best Practices

1. **HTTPS obligatorio en producción:** Los webhooks deben recibirse solo por HTTPS
2. **Validar siempre la firma:** Nunca omitir la validación de firma
3. **Webhook Secret seguro:** No compartir ni commitear el webhook secret
4. **Rate limiting:** Implementar límites de requests si es necesario
5. **Idempotencia:** Laravel debe manejar webhooks duplicados correctamente
6. **Logs seguros:** No logear información sensible de tarjetas

---

## 📚 Referencias

- [Stripe Webhooks Documentation](https://stripe.com/docs/webhooks)
- [Stripe CLI](https://stripe.com/docs/stripe-cli)
- [Webhook Signing](https://stripe.com/docs/webhooks/signatures)
- [Event Types](https://stripe.com/docs/api/events/types)

---

**Última actualización:** Enero 2025  
**Versión:** .NET 10  
**Estado:** ✅ Completamente Implementado
