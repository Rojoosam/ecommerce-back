# API de Procesamiento de Pagos Únicos (Payment Intents)

## Descripción General

Esta API proporciona endpoints para procesar **pagos únicos** en Stripe usando Payment Intents. Permite crear, confirmar, consultar y cancelar pagos asociados a Customers y Payment Methods.

---

## 🔑 Conceptos Importantes

### Payment Intent

Un **Payment Intent** representa la intención de cobrar a un cliente. Tiene varios estados:

| Estado | Descripción |
|--------|-------------|
| `requires_payment_method` | Necesita un método de pago |
| `requires_confirmation` | Necesita ser confirmado |
| `requires_action` | Necesita autenticación 3D Secure |
| `processing` | En proceso |
| `requires_capture` | Autorizado, pendiente de captura |
| `canceled` | Cancelado |
| `succeeded` | ✅ Exitoso |

### Captura Automática vs Manual

- **Automática** (default): El pago se cobra inmediatamente
- **Manual**: El pago se autoriza pero no se captura hasta que lo solicites (útil para reservas)

---

## Endpoints Disponibles

### 1. Crear y Confirmar Payment Intent

**Endpoint:** `POST /api/paymentintents`

**Descripción:** Crea y confirma un Payment Intent para procesar un pago único.

**Request Body:**
```json
{
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
  "amount": 150.00,
  "currency": "mxn",
  "order_id": "ORDER-12345",
  "description": "Compra de productos - Pedido #12345",
  "metadata": {
    "order_number": "12345",
    "customer_name": "Juan Pérez",
    "items_count": "3"
  },
  "auto_confirm": true,
  "auto_capture": true
}
```

**Campos Requeridos:**
- `customer_id` (string): ID del Customer en Stripe (cus_xxx)
- `payment_method_id` (string): ID del Payment Method en Stripe (pm_xxx)
- `amount` (decimal): Monto a cobrar en unidades mayores (100.00 = $100.00)
- `currency` (string): Moneda del cargo (usd, mxn, eur, etc.)
- `order_id` (string): ID interno del pedido en Laravel

**Campos Opcionales:**
- `description` (string): Descripción del cargo
- `metadata` (dictionary): Metadatos adicionales
- `auto_confirm` (boolean, default: true): Confirmar automáticamente
- `auto_capture` (boolean, default: true): Capturar automáticamente (false = solo autorizar)

**Response (200 OK) - Pago Exitoso:**
```json
{
  "success": true,
  "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
  "order_id": "ORDER-12345",
  "status": "succeeded",
  "amount": 15000,
  "amount_decimal": 150.00,
  "amount_captured": 15000,
  "amount_captured_decimal": 150.00,
  "currency": "mxn",
  "description": "Compra de productos - Pedido #12345",
  "created": "2024-01-15T10:30:00Z",
  "client_secret": "pi_3PqRsT2aBcDeFgHi_secret_xxx",
  "charge": {
    "charge_id": "ch_3PqRsT2aBcDeFgHi",
    "amount": 15000,
    "amount_decimal": 150.00,
    "currency": "mxn",
    "status": "succeeded",
    "created": "2024-01-15T10:30:00Z",
    "captured": true,
    "receipt_url": "https://pay.stripe.com/receipts/...",
    "card": {
      "brand": "visa",
      "last4": "4242",
      "exp_month": 12,
      "exp_year": 2025,
      "country": "MX",
      "funding": "credit"
    }
  },
  "metadata": {
    "order_id": "ORDER-12345",
    "order_number": "12345",
    "customer_name": "Juan Pérez"
  },
  "error_message": null,
  "error_code": null
}
```

**Response (500) - Pago Rechazado:**
```json
{
  "success": false,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
  "order_id": "ORDER-12345",
  "status": "failed",
  "amount": 15000,
  "amount_decimal": 150.00,
  "currency": "mxn",
  "error_message": "Error de Stripe: Your card was declined.",
  "error_code": "card_declined"
}
```

**Response (400) - Customer Inactivo:**
```json
{
  "success": false,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "order_id": "ORDER-12345",
  "error_message": "El Customer está inactivo. No se pueden procesar pagos."
}
```

---

### 2. Obtener Payment Intent

**Endpoint:** `GET /api/paymentintents/{paymentIntentId}`

**Descripción:** Obtiene los detalles de un Payment Intent existente.

**Parámetros de URL:**
- `paymentIntentId` (string): ID del Payment Intent en Stripe (pi_xxx)

**Ejemplo:** `GET /api/paymentintents/pi_3PqRsT2aBcDeFgHi`

**Response (200 OK):**
```json
{
  "success": true,
  "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
  "order_id": "ORDER-12345",
  "status": "succeeded",
  "amount": 15000,
  "amount_decimal": 150.00,
  "currency": "mxn",
  "created": "2024-01-15T10:30:00Z",
  "charge": { /* detalles del cargo */ },
  "metadata": {
    "order_id": "ORDER-12345"
  }
}
```

---

### 3. Cancelar Payment Intent

**Endpoint:** `POST /api/paymentintents/cancel`

**Descripción:** Cancela un Payment Intent que aún no ha sido confirmado o capturado.

**Request Body:**
```json
{
  "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
  "cancellation_reason": "requested_by_customer"
}
```

**Campos Requeridos:**
- `payment_intent_id` (string): ID del Payment Intent en Stripe (pi_xxx)

**Campos Opcionales:**
- `cancellation_reason` (string): Razón de cancelación
  - `duplicate`: Duplicado
  - `fraudulent`: Fraudulento
  - `requested_by_customer`: Solicitado por el cliente
  - `abandoned`: Abandonado

**Response (200 OK):**
```json
{
  "success": true,
  "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
  "status": "canceled",
  "cancellation_reason": "requested_by_customer",
  "message": "Payment Intent cancelado exitosamente",
  "error_message": null
}
```

**Response (400) - No se Puede Cancelar:**
```json
{
  "success": false,
  "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
  "error_message": "Error de Stripe: You cannot cancel this PaymentIntent because it has a status of succeeded."
}
```

---

### 4. Capturar Payment Intent

**Endpoint:** `POST /api/paymentintents/capture`

**Descripción:** Captura un Payment Intent que fue autorizado pero no capturado (cuando `auto_capture: false`).

**Request Body:**
```json
{
  "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
  "amount_to_capture": 120.00
}
```

**Campos Requeridos:**
- `payment_intent_id` (string): ID del Payment Intent en Stripe (pi_xxx)

**Campos Opcionales:**
- `amount_to_capture` (decimal): Monto a capturar (si es menor al autorizado)

**Response (200 OK):**
```json
{
  "success": true,
  "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
  "status": "succeeded",
  "amount_captured": 120.00,
  "currency": "mxn",
  "message": "Payment Intent capturado exitosamente",
  "error_message": null
}
```

---

## Integración con Laravel

### Paso 1: Servicio de Payment Intent en Laravel

**`app/Services/StripePaymentIntentService.php`:**
```php
<?php

namespace App\Services;

use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Log;

class StripePaymentIntentService
{
    private string $apiBaseUrl;

    public function __construct()
    {
        $this->apiBaseUrl = config('services.ecommerce_api.base_url') . '/api/paymentintents';
    }

    /**
     * Procesar un pago
     */
    public function processPayment(array $data): ?array
    {
        try {
            $response = Http::post($this->apiBaseUrl, [
                'customer_id' => $data['customer_id'],
                'payment_method_id' => $data['payment_method_id'],
                'amount' => $data['amount'],
                'currency' => $data['currency'],
                'order_id' => $data['order_id'],
                'description' => $data['description'] ?? null,
                'metadata' => $data['metadata'] ?? null,
                'auto_confirm' => $data['auto_confirm'] ?? true,
                'auto_capture' => $data['auto_capture'] ?? true,
            ]);

            $result = $response->json();

            if ($result['success'] ?? false) {
                Log::info('Payment Intent procesado exitosamente', [
                    'payment_intent_id' => $result['payment_intent_id'],
                    'status' => $result['status'],
                    'order_id' => $data['order_id']
                ]);

                return $result;
            }

            Log::error('Error al procesar Payment Intent', [
                'error' => $result['error_message'] ?? 'Unknown error',
                'order_id' => $data['order_id']
            ]);

            return $result;
        } catch (\Exception $e) {
            Log::error('Excepción al procesar Payment Intent', [
                'error' => $e->getMessage(),
                'order_id' => $data['order_id']
            ]);
            return null;
        }
    }

    /**
     * Obtener detalles de un Payment Intent
     */
    public function getPaymentIntent(string $paymentIntentId): ?array
    {
        try {
            $response = Http::get("{$this->apiBaseUrl}/{$paymentIntentId}");
            $result = $response->json();

            if ($result['success'] ?? false) {
                return $result;
            }

            return null;
        } catch (\Exception $e) {
            Log::error('Excepción al obtener Payment Intent', [
                'error' => $e->getMessage(),
                'payment_intent_id' => $paymentIntentId
            ]);
            return null;
        }
    }

    /**
     * Cancelar un Payment Intent
     */
    public function cancelPaymentIntent(string $paymentIntentId, ?string $reason = null): bool
    {
        try {
            $response = Http::post("{$this->apiBaseUrl}/cancel", [
                'payment_intent_id' => $paymentIntentId,
                'cancellation_reason' => $reason ?? 'requested_by_customer'
            ]);

            $result = $response->json();

            if ($result['success'] ?? false) {
                Log::info('Payment Intent cancelado', [
                    'payment_intent_id' => $paymentIntentId
                ]);
                return true;
            }

            Log::error('Error al cancelar Payment Intent', [
                'error' => $result['error_message'] ?? 'Unknown error',
                'payment_intent_id' => $paymentIntentId
            ]);

            return false;
        } catch (\Exception $e) {
            Log::error('Excepción al cancelar Payment Intent', [
                'error' => $e->getMessage(),
                'payment_intent_id' => $paymentIntentId
            ]);
            return false;
        }
    }

    /**
     * Capturar un Payment Intent autorizado
     */
    public function capturePaymentIntent(string $paymentIntentId, ?float $amount = null): bool
    {
        try {
            $payload = [
                'payment_intent_id' => $paymentIntentId
            ];

            if ($amount !== null) {
                $payload['amount_to_capture'] = $amount;
            }

            $response = Http::post("{$this->apiBaseUrl}/capture", $payload);
            $result = $response->json();

            if ($result['success'] ?? false) {
                Log::info('Payment Intent capturado', [
                    'payment_intent_id' => $paymentIntentId,
                    'amount' => $amount
                ]);
                return true;
            }

            return false;
        } catch (\Exception $e) {
            Log::error('Excepción al capturar Payment Intent', [
                'error' => $e->getMessage(),
                'payment_intent_id' => $paymentIntentId
            ]);
            return false;
        }
    }
}
```

---

### Paso 2: Controlador Laravel para Procesar Pagos

**`app/Http/Controllers/Api/PaymentController.php`:**
```php
<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Services\StripePaymentIntentService;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;

class PaymentController extends Controller
{
    private StripePaymentIntentService $paymentService;

    public function __construct(StripePaymentIntentService $paymentService)
    {
        $this->paymentService = $paymentService;
    }

    /**
     * Procesar un pago
     */
    public function processPayment(Request $request)
    {
        $validated = $request->validate([
            'payment_method_id' => 'required|string|starts_with:pm_',
            'amount' => 'required|numeric|min:0.01',
            'currency' => 'required|string|size:3',
            'order_id' => 'required|string',
            'description' => 'nullable|string',
            'auto_capture' => 'nullable|boolean'
        ]);

        $user = auth()->user();
        $customerId = $user->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'success' => false,
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        // Procesar el pago
        $result = $this->paymentService->processPayment([
            'customer_id' => $customerId,
            'payment_method_id' => $validated['payment_method_id'],
            'amount' => $validated['amount'],
            'currency' => strtolower($validated['currency']),
            'order_id' => $validated['order_id'],
            'description' => $validated['description'] ?? "Pago de orden {$validated['order_id']}",
            'auto_capture' => $validated['auto_capture'] ?? true,
            'metadata' => [
                'user_id' => $user->id,
                'user_email' => $user->email
            ]
        ]);

        if ($result && ($result['success'] ?? false)) {
            // Actualizar el pedido en la base de datos
            DB::table('orders')
                ->where('id', $validated['order_id'])
                ->update([
                    'stripe_payment_intent_id' => $result['payment_intent_id'],
                    'payment_status' => $result['status'],
                    'paid_at' => $result['status'] === 'succeeded' ? now() : null
                ]);

            return response()->json([
                'success' => true,
                'message' => 'Pago procesado exitosamente',
                'payment' => $result
            ], 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al procesar el pago',
            'error' => $result['error_message'] ?? 'Error desconocido'
        ], 500);
    }

    /**
     * Obtener estado de un pago
     */
    public function getPaymentStatus(string $paymentIntentId)
    {
        $payment = $this->paymentService->getPaymentIntent($paymentIntentId);

        if ($payment) {
            return response()->json($payment, 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Payment Intent no encontrado'
        ], 404);
    }

    /**
     * Cancelar un pago
     */
    public function cancelPayment(Request $request)
    {
        $validated = $request->validate([
            'payment_intent_id' => 'required|string|starts_with:pi_',
            'reason' => 'nullable|string|in:duplicate,fraudulent,requested_by_customer,abandoned'
        ]);

        $success = $this->paymentService->cancelPaymentIntent(
            $validated['payment_intent_id'],
            $validated['reason'] ?? 'requested_by_customer'
        );

        if ($success) {
            // Actualizar el pedido en la base de datos
            DB::table('orders')
                ->where('stripe_payment_intent_id', $validated['payment_intent_id'])
                ->update([
                    'payment_status' => 'canceled',
                    'canceled_at' => now()
                ]);

            return response()->json([
                'success' => true,
                'message' => 'Pago cancelado exitosamente'
            ], 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al cancelar el pago'
        ], 500);
    }
}
```

---

### Paso 3: Rutas en Laravel

**`routes/api.php`:**
```php
use App\Http\Controllers\Api\PaymentController;

Route::middleware('auth:sanctum')->prefix('payments')->group(function () {
    Route::post('/process', [PaymentController::class, 'processPayment']);
    Route::get('/{paymentIntentId}', [PaymentController::class, 'getPaymentStatus']);
    Route::post('/cancel', [PaymentController::class, 'cancelPayment']);
});
```

---

## Flujo Completo de Pago

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Usuario selecciona método de pago y confirma compra     │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Laravel envía datos del pago a .NET API                 │
│    POST /api/paymentintents                                 │
│    {                                                        │
│      customer_id: "cus_xxx",                               │
│      payment_method_id: "pm_xxx",                          │
│      amount: 150.00,                                        │
│      currency: "mxn",                                       │
│      order_id: "ORDER-12345"                               │
│    }                                                        │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. .NET API valida y procesa                               │
│    - Verifica customer activo                              │
│    - Crea Payment Intent en Stripe                         │
│    - Confirma automáticamente                              │
│    - Cobra al Payment Method                               │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Stripe procesa el pago                                  │
│    - Valida la tarjeta                                     │
│    - Verifica fondos                                       │
│    - Ejecuta el cargo                                      │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. .NET devuelve resultado                                 │
│    {                                                        │
│      success: true,                                         │
│      payment_intent_id: "pi_xxx",                          │
│      status: "succeeded",                                   │
│      amount_decimal: 150.00,                               │
│      currency: "mxn",                                       │
│      charge: { /* detalles del cargo */ }                  │
│    }                                                        │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Laravel actualiza el pedido                             │
│    - Guarda payment_intent_id                              │
│    - Actualiza estado a "pagado"                           │
│    - Envía confirmación al cliente                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Estados del Pago

### Estados Normales

1. **succeeded** ✅
   - Pago completado exitosamente
   - El dinero fue cobrado
   - Se puede emitir el pedido

2. **processing** ⏳
   - Pago en proceso (puede tardar varios días)
   - Común en transferencias bancarias
   - Esperar confirmación

3. **requires_capture** 💰
   - Pago autorizado pero no capturado
   - El dinero está "reservado"
   - Debes capturarlo manualmente

4. **requires_action** 🔐
   - Requiere autenticación 3D Secure
   - El cliente debe completar la verificación
   - Laravel debe manejar esto en frontend

### Estados de Error/Cancelación

5. **canceled** ❌
   - Pago cancelado
   - No se cobró

6. **failed** ❌
   - Pago fallido
   - Tarjeta rechazada u otro error

---

## Manejo de Errores Comunes

### 1. Tarjeta Rechazada
```json
{
  "success": false,
  "status": "failed",
  "error_message": "Your card was declined.",
  "error_code": "card_declined"
}
```

**Acción en Laravel:** Notificar al usuario que intente con otra tarjeta.

---

### 2. Fondos Insuficientes
```json
{
  "success": false,
  "status": "failed",
  "error_message": "Your card has insufficient funds.",
  "error_code": "insufficient_funds"
}
```

**Acción en Laravel:** Notificar al usuario sobre fondos insuficientes.

---

### 3. Customer Inactivo
```json
{
  "success": false,
  "error_message": "El Customer está inactivo. No se pueden procesar pagos."
}
```

**Acción en Laravel:** No permitir pagos, mostrar mensaje de cuenta suspendida.

---

### 4. Payment Method No Válido
```json
{
  "success": false,
  "error_message": "Error de Stripe: This payment method was previously used without being attached to a customer or was detached from a customer."
}
```

**Acción en Laravel:** Solicitar al usuario que registre nuevamente su tarjeta.

---

## Testing

### Tarjetas de Prueba para Diferentes Escenarios

| Tarjeta | Número | Resultado |
|---------|--------|-----------|
| Visa - Éxito | 4242 4242 4242 4242 | ✅ succeeded |
| Visa - Rechazada | 4000 0000 0000 0002 | ❌ card_declined |
| Visa - Fondos insuficientes | 4000 0000 0000 9995 | ❌ insufficient_funds |
| Visa - 3D Secure | 4000 0027 6000 3184 | 🔐 requires_action |
| Visa - Autorizar sin capturar | 4000 0000 0000 3063 | 💰 requires_capture |

**Fecha**: Cualquier fecha futura  
**CVC**: Cualquier 3-4 dígitos

---

### Ejemplos de Testing con cURL

#### Procesar Pago Exitoso
```bash
curl -X POST "https://tu-api-dotnet.com/api/paymentintents" \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
    "amount": 150.00,
    "currency": "mxn",
    "order_id": "ORDER-12345",
    "description": "Compra de productos"
  }'
```

#### Cancelar Pago
```bash
curl -X POST "https://tu-api-dotnet.com/api/paymentintents/cancel" \
  -H "Content-Type: application/json" \
  -d '{
    "payment_intent_id": "pi_3PqRsT2aBcDeFgHi",
    "cancellation_reason": "requested_by_customer"
  }'
```

#### Obtener Estado del Pago
```bash
curl -X GET "https://tu-api-dotnet.com/api/paymentintents/pi_3PqRsT2aBcDeFgHi"
```

---

## Actualización de Base de Datos

### Migración para Agregar Campos de Pago

```php
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up()
    {
        Schema::table('orders', function (Blueprint $table) {
            $table->string('stripe_payment_intent_id')->nullable()->after('id');
            $table->enum('payment_status', [
                'pending', 
                'processing', 
                'succeeded', 
                'failed', 
                'canceled',
                'requires_action',
                'requires_capture'
            ])->default('pending')->after('stripe_payment_intent_id');
            $table->timestamp('paid_at')->nullable()->after('payment_status');
            $table->index('stripe_payment_intent_id');
        });
    }

    public function down()
    {
        Schema::table('orders', function (Blueprint $table) {
            $table->dropColumn(['stripe_payment_intent_id', 'payment_status', 'paid_at']);
        });
    }
};
```

---

## Webhooks (Opcional pero Recomendado)

Para casos donde el pago tarda en procesarse (3D Secure, transferencias), es recomendable implementar webhooks:

```php
// Webhook para recibir notificaciones de Stripe
Route::post('/webhooks/stripe', [WebhookController::class, 'handleStripe']);
```

Eventos importantes:
- `payment_intent.succeeded` - Pago exitoso
- `payment_intent.payment_failed` - Pago fallido
- `payment_intent.canceled` - Pago cancelado

---

## Notas Importantes

1. **Captura Automática**: Por defecto, los pagos se capturan automáticamente. Si necesitas autorizar sin cobrar (reservas, pre-autorizaciones), usa `auto_capture: false`.

2. **3D Secure**: Si el pago requiere autenticación, deberás manejar esto en el frontend con Stripe.js.

3. **Cancelación**: Solo se pueden cancelar Payment Intents que NO estén en estado `succeeded`. Si ya fue exitoso, debes hacer un **refund**.

4. **Order ID**: Siempre se guarda en el metadata del Payment Intent para referencia.

5. **Customer Activo**: Los Customers inactivos no pueden realizar pagos.

---

## Soporte

Para más información:
- **Payment Intents**: https://stripe.com/docs/api/payment_intents
- **Testing**: https://stripe.com/docs/testing
- **3D Secure**: https://stripe.com/docs/payments/3d-secure

---

**¡API lista para procesar pagos!** 🚀
