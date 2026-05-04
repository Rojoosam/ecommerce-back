# ✅ Laravel Payment Intents Checklist

## 🎯 Guía de Implementación Paso a Paso

Esta guía te ayudará a implementar el procesamiento de pagos en Laravel usando la API de .NET que acabas de crear.

---

## 📋 Pre-requisitos

Antes de comenzar, asegúrate de tener:

- [x] Customer creado en Stripe (Requerimiento #1)
- [x] Payment Method registrado (Requerimiento #2)
- [x] API de .NET corriendo
- [x] Stripe en modo TEST

---

## 🚀 Paso 1: Configurar Variables de Entorno

**`.env`**
```env
ECOMMERCE_API_URL=https://tu-api-dotnet.com
STRIPE_KEY=pk_test_xxx
STRIPE_SECRET=sk_test_xxx
```

**`config/services.php`**
```php
'ecommerce_api' => [
    'base_url' => env('ECOMMERCE_API_URL'),
],
'stripe' => [
    'key' => env('STRIPE_KEY'),
    'secret' => env('STRIPE_SECRET'),
],
```

---

## 🔧 Paso 2: Crear Servicio de Payment Intent

**`app/Services/StripePaymentIntentService.php`**

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
            Log::info('Procesando pago', [
                'order_id' => $data['order_id'],
                'amount' => $data['amount'],
                'currency' => $data['currency']
            ]);

            $response = Http::timeout(30)->post($this->apiBaseUrl, [
                'customer_id' => $data['customer_id'],
                'payment_method_id' => $data['payment_method_id'],
                'amount' => (float)$data['amount'],
                'currency' => strtolower($data['currency']),
                'order_id' => $data['order_id'],
                'description' => $data['description'] ?? null,
                'metadata' => $data['metadata'] ?? [],
                'auto_confirm' => $data['auto_confirm'] ?? true,
                'auto_capture' => $data['auto_capture'] ?? true,
            ]);

            if (!$response->successful()) {
                Log::error('Error HTTP al procesar pago', [
                    'status' => $response->status(),
                    'body' => $response->body()
                ]);
                return null;
            }

            $result = $response->json();

            if ($result['success'] ?? false) {
                Log::info('✅ Pago procesado exitosamente', [
                    'payment_intent_id' => $result['payment_intent_id'],
                    'status' => $result['status'],
                    'order_id' => $data['order_id']
                ]);
            } else {
                Log::error('❌ Error al procesar pago', [
                    'error' => $result['error_message'] ?? 'Unknown',
                    'order_id' => $data['order_id']
                ]);
            }

            return $result;
        } catch (\Exception $e) {
            Log::error('❌ Excepción al procesar pago', [
                'error' => $e->getMessage(),
                'order_id' => $data['order_id'] ?? 'N/A'
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

            if ($response->successful()) {
                return $response->json();
            }

            return null;
        } catch (\Exception $e) {
            Log::error('Error al obtener Payment Intent', [
                'payment_intent_id' => $paymentIntentId,
                'error' => $e->getMessage()
            ]);
            return null;
        }
    }

    /**
     * Cancelar un Payment Intent
     */
    public function cancelPaymentIntent(string $paymentIntentId, ?string $reason = null): ?array
    {
        try {
            Log::info('Cancelando pago', [
                'payment_intent_id' => $paymentIntentId,
                'reason' => $reason ?? 'requested_by_customer'
            ]);

            $response = Http::post("{$this->apiBaseUrl}/cancel", [
                'payment_intent_id' => $paymentIntentId,
                'cancellation_reason' => $reason ?? 'requested_by_customer'
            ]);

            if ($response->successful()) {
                $result = $response->json();
                
                if ($result['success'] ?? false) {
                    Log::info('✅ Pago cancelado', [
                        'payment_intent_id' => $paymentIntentId
                    ]);
                }

                return $result;
            }

            return null;
        } catch (\Exception $e) {
            Log::error('Error al cancelar pago', [
                'payment_intent_id' => $paymentIntentId,
                'error' => $e->getMessage()
            ]);
            return null;
        }
    }

    /**
     * Capturar un Payment Intent autorizado
     */
    public function capturePaymentIntent(string $paymentIntentId, ?float $amount = null): ?array
    {
        try {
            Log::info('Capturando pago', [
                'payment_intent_id' => $paymentIntentId,
                'amount' => $amount
            ]);

            $payload = ['payment_intent_id' => $paymentIntentId];

            if ($amount !== null) {
                $payload['amount_to_capture'] = $amount;
            }

            $response = Http::post("{$this->apiBaseUrl}/capture", $payload);

            if ($response->successful()) {
                $result = $response->json();
                
                if ($result['success'] ?? false) {
                    Log::info('✅ Pago capturado', [
                        'payment_intent_id' => $paymentIntentId,
                        'amount' => $result['amount_captured']
                    ]);
                }

                return $result;
            }

            return null;
        } catch (\Exception $e) {
            Log::error('Error al capturar pago', [
                'payment_intent_id' => $paymentIntentId,
                'error' => $e->getMessage()
            ]);
            return null;
        }
    }
}
```

---

## 🎮 Paso 3: Crear Controlador de Pagos

**`app/Http/Controllers/Api/PaymentController.php`**

```php
<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Services\StripePaymentIntentService;
use App\Models\Order;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Mail;
use App\Mail\PaymentConfirmation;

class PaymentController extends Controller
{
    private StripePaymentIntentService $paymentService;

    public function __construct(StripePaymentIntentService $paymentService)
    {
        $this->paymentService = $paymentService;
    }

    /**
     * Procesar un pago
     * POST /api/payments/process
     */
    public function processPayment(Request $request)
    {
        $validated = $request->validate([
            'payment_method_id' => 'required|string|starts_with:pm_',
            'amount' => 'required|numeric|min:0.01',
            'currency' => 'required|string|size:3',
            'order_id' => 'required|string',
            'description' => 'nullable|string|max:500',
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

        // Verificar que la orden exista y pertenezca al usuario
        $order = Order::where('id', $validated['order_id'])
            ->where('user_id', $user->id)
            ->first();

        if (!$order) {
            return response()->json([
                'success' => false,
                'message' => 'Orden no encontrada'
            ], 404);
        }

        // Verificar que la orden no esté ya pagada
        if ($order->payment_status === 'succeeded') {
            return response()->json([
                'success' => false,
                'message' => 'Esta orden ya fue pagada'
            ], 400);
        }

        // Procesar el pago
        $result = $this->paymentService->processPayment([
            'customer_id' => $customerId,
            'payment_method_id' => $validated['payment_method_id'],
            'amount' => $validated['amount'],
            'currency' => strtolower($validated['currency']),
            'order_id' => $validated['order_id'],
            'description' => $validated['description'] ?? "Pedido #{$order->number}",
            'auto_capture' => $validated['auto_capture'] ?? true,
            'metadata' => [
                'user_id' => $user->id,
                'user_email' => $user->email,
                'order_number' => $order->number
            ]
        ]);

        if ($result && ($result['success'] ?? false)) {
            // Actualizar la orden
            $order->update([
                'stripe_payment_intent_id' => $result['payment_intent_id'],
                'payment_status' => $result['status'],
                'paid_at' => $result['status'] === 'succeeded' ? now() : null
            ]);

            // Si el pago fue exitoso, enviar confirmación
            if ($result['status'] === 'succeeded') {
                Mail::to($user->email)->send(new PaymentConfirmation($order, $result));
            }

            return response()->json([
                'success' => true,
                'message' => $this->getStatusMessage($result['status']),
                'payment' => $result
            ], 200);
        }

        return response()->json([
            'success' => false,
            'message' => $this->getErrorMessage($result['error_code'] ?? null),
            'error' => $result['error_message'] ?? 'Error desconocido'
        ], 400);
    }

    /**
     * Obtener estado de un pago
     * GET /api/payments/{paymentIntentId}
     */
    public function getPaymentStatus(string $paymentIntentId)
    {
        $payment = $this->paymentService->getPaymentIntent($paymentIntentId);

        if ($payment && ($payment['success'] ?? false)) {
            return response()->json($payment, 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Payment Intent no encontrado'
        ], 404);
    }

    /**
     * Cancelar un pago
     * POST /api/payments/cancel
     */
    public function cancelPayment(Request $request)
    {
        $validated = $request->validate([
            'payment_intent_id' => 'required|string|starts_with:pi_',
            'reason' => 'nullable|string|in:duplicate,fraudulent,requested_by_customer,abandoned'
        ]);

        // Buscar la orden por payment_intent_id
        $order = Order::where('stripe_payment_intent_id', $validated['payment_intent_id'])
            ->where('user_id', auth()->id())
            ->first();

        if (!$order) {
            return response()->json([
                'success' => false,
                'message' => 'Orden no encontrada'
            ], 404);
        }

        // Verificar que el pago pueda ser cancelado
        if ($order->payment_status === 'succeeded') {
            return response()->json([
                'success' => false,
                'message' => 'No se puede cancelar un pago exitoso. Debes hacer un reembolso.'
            ], 400);
        }

        $result = $this->paymentService->cancelPaymentIntent(
            $validated['payment_intent_id'],
            $validated['reason'] ?? 'requested_by_customer'
        );

        if ($result && ($result['success'] ?? false)) {
            // Actualizar la orden
            $order->update([
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
            'message' => 'Error al cancelar el pago',
            'error' => $result['error_message'] ?? 'Error desconocido'
        ], 500);
    }

    /**
     * Capturar un pago autorizado
     * POST /api/payments/capture
     */
    public function capturePayment(Request $request)
    {
        $validated = $request->validate([
            'payment_intent_id' => 'required|string|starts_with:pi_',
            'amount' => 'nullable|numeric|min:0.01'
        ]);

        // Buscar la orden
        $order = Order::where('stripe_payment_intent_id', $validated['payment_intent_id'])
            ->where('user_id', auth()->id())
            ->first();

        if (!$order) {
            return response()->json([
                'success' => false,
                'message' => 'Orden no encontrada'
            ], 404);
        }

        // Verificar que el pago esté pendiente de captura
        if ($order->payment_status !== 'requires_capture') {
            return response()->json([
                'success' => false,
                'message' => 'El pago no está pendiente de captura'
            ], 400);
        }

        $result = $this->paymentService->capturePaymentIntent(
            $validated['payment_intent_id'],
            $validated['amount'] ?? null
        );

        if ($result && ($result['success'] ?? false)) {
            // Actualizar la orden
            $order->update([
                'payment_status' => 'succeeded',
                'paid_at' => now()
            ]);

            // Enviar confirmación por email
            Mail::to(auth()->user()->email)->send(new PaymentConfirmation($order, $result));

            return response()->json([
                'success' => true,
                'message' => 'Pago capturado exitosamente',
                'payment' => $result
            ], 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al capturar el pago'
        ], 500);
    }

    /**
     * Mensaje amigable según el estado del pago
     */
    private function getStatusMessage(string $status): string
    {
        return match($status) {
            'succeeded' => '¡Pago exitoso! Tu pedido está en proceso.',
            'processing' => 'Tu pago está siendo procesado. Te notificaremos cuando se confirme.',
            'requires_capture' => 'Pago autorizado. Será capturado al enviar el pedido.',
            'requires_action' => 'Tu banco requiere autenticación adicional.',
            'canceled' => 'El pago fue cancelado.',
            default => 'Estado del pago: ' . $status
        };
    }

    /**
     * Mensaje de error amigable
     */
    private function getErrorMessage(?string $errorCode): string
    {
        if (!$errorCode) {
            return 'Error al procesar el pago';
        }

        return match($errorCode) {
            'card_declined' => 'Tu tarjeta fue rechazada. Intenta con otra tarjeta.',
            'insufficient_funds' => 'Fondos insuficientes en tu tarjeta.',
            'expired_card' => 'Tu tarjeta ha expirado. Actualiza los datos.',
            'incorrect_cvc' => 'El código de seguridad (CVC) es incorrecto.',
            'processing_error' => 'Error al procesar el pago. Intenta nuevamente.',
            'incorrect_number' => 'El número de tarjeta es incorrecto.',
            'invalid_expiry_year' => 'El año de expiración es inválido.',
            'invalid_expiry_month' => 'El mes de expiración es inválido.',
            default => 'Error al procesar el pago: ' . $errorCode
        };
    }
}
```

---

## 🎮 Paso 3: Crear Controlador de Pagos

Ya está incluido en el servicio anterior (opcionalmente puedes separarlo).

---

## 🛣️ Paso 4: Registrar Rutas

**`routes/api.php`**

```php
use App\Http\Controllers\Api\PaymentController;

Route::middleware('auth:sanctum')->prefix('payments')->group(function () {
    // Procesar pago
    Route::post('/process', [PaymentController::class, 'processPayment']);
    
    // Consultar estado del pago
    Route::get('/{paymentIntentId}', [PaymentController::class, 'getPaymentStatus']);
    
    // Cancelar pago
    Route::post('/cancel', [PaymentController::class, 'cancelPayment']);
    
    // Capturar pago autorizado
    Route::post('/capture', [PaymentController::class, 'capturePayment']);
});
```

---

## 📊 Paso 5: Actualizar Modelo Order

**Migración:**
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
            $table->timestamp('canceled_at')->nullable()->after('paid_at');
            
            // Índices
            $table->index('stripe_payment_intent_id');
            $table->index('payment_status');
        });
    }

    public function down()
    {
        Schema::table('orders', function (Blueprint $table) {
            $table->dropColumn([
                'stripe_payment_intent_id',
                'payment_status',
                'paid_at',
                'canceled_at'
            ]);
        });
    }
};
```

**Modelo:**
```php
<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Order extends Model
{
    protected $fillable = [
        'user_id',
        'number',
        'total',
        'currency',
        'stripe_payment_intent_id',
        'payment_status',
        'paid_at',
        'canceled_at'
    ];

    protected $casts = [
        'total' => 'decimal:2',
        'paid_at' => 'datetime',
        'canceled_at' => 'datetime'
    ];

    public function user()
    {
        return $this->belongsTo(User::class);
    }

    public function isPaid(): bool
    {
        return $this->payment_status === 'succeeded';
    }

    public function isPending(): bool
    {
        return in_array($this->payment_status, ['pending', 'processing', 'requires_capture']);
    }

    public function isCanceled(): bool
    {
        return $this->payment_status === 'canceled';
    }

    public function canBeCanceled(): bool
    {
        return !in_array($this->payment_status, ['succeeded', 'canceled']);
    }
}
```

---

## 🎨 Paso 6: Crear Vista de Checkout

**`resources/views/checkout/payment.blade.php`**

```html
@extends('layouts.app')

@section('content')
<div class="container" x-data="checkoutForm()">
    <h2>Checkout - Pago</h2>

    <!-- Resumen del pedido -->
    <div class="order-summary">
        <h3>Resumen del Pedido</h3>
        <p>Order ID: {{ $order->number }}</p>
        <p>Total: ${{ number_format($order->total, 2) }} {{ strtoupper($order->currency) }}</p>
    </div>

    <!-- Seleccionar tarjeta -->
    <div class="payment-methods">
        <h3>Método de Pago</h3>
        
        <select x-model="selectedPaymentMethod" class="form-control">
            <option value="">Seleccionar tarjeta...</option>
            <template x-for="pm in paymentMethods" :key="pm.payment_method_id">
                <option :value="pm.payment_method_id">
                    <span x-text="pm.card.brand.toUpperCase()"></span>
                    ****<span x-text="pm.card.last4"></span>
                    (Exp: <span x-text="pm.card.exp_month"></span>/<span x-text="pm.card.exp_year"></span>)
                </option>
            </template>
        </select>

        <a href="{{ route('payment-methods.create') }}" class="btn btn-link">
            + Agregar nueva tarjeta
        </a>
    </div>

    <!-- Botón de pago -->
    <div class="payment-actions">
        <button 
            @click="processPayment" 
            :disabled="!selectedPaymentMethod || processing"
            class="btn btn-primary btn-lg">
            <span x-show="!processing">
                Pagar ${{ number_format($order->total, 2) }}
            </span>
            <span x-show="processing">
                <i class="fa fa-spinner fa-spin"></i> Procesando...
            </span>
        </button>
    </div>

    <!-- Resultado -->
    <div x-show="result" 
         :class="result?.success ? 'alert alert-success' : 'alert alert-danger'">
        <span x-text="result?.message"></span>
    </div>
</div>

@push('scripts')
<script>
function checkoutForm() {
    return {
        paymentMethods: [],
        selectedPaymentMethod: '',
        processing: false,
        result: null,

        async init() {
            await this.loadPaymentMethods();
        },

        async loadPaymentMethods() {
            try {
                const response = await fetch('/api/payment-methods', {
                    headers: {
                        'Authorization': 'Bearer ' + document.querySelector('meta[name="api-token"]').content
                    }
                });
                const data = await response.json();
                this.paymentMethods = data.payment_methods || [];
                
                // Seleccionar la primera tarjeta por defecto
                if (this.paymentMethods.length > 0) {
                    this.selectedPaymentMethod = this.paymentMethods[0].payment_method_id;
                }
            } catch (error) {
                console.error('Error al cargar métodos de pago:', error);
            }
        },

        async processPayment() {
            if (!this.selectedPaymentMethod) {
                alert('Por favor selecciona un método de pago');
                return;
            }

            this.processing = true;
            this.result = null;

            try {
                const response = await fetch('/api/payments/process', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-CSRF-TOKEN': document.querySelector('meta[name="csrf-token"]').content,
                        'Authorization': 'Bearer ' + document.querySelector('meta[name="api-token"]').content
                    },
                    body: JSON.stringify({
                        payment_method_id: this.selectedPaymentMethod,
                        amount: {{ $order->total }},
                        currency: '{{ $order->currency }}',
                        order_id: '{{ $order->id }}',
                        description: 'Pedido #{{ $order->number }}'
                    })
                });

                const data = await response.json();

                if (data.success && data.payment?.status === 'succeeded') {
                    this.result = {
                        success: true,
                        message: '¡Pago exitoso! Redirigiendo...'
                    };

                    // Redirigir a la página de confirmación
                    setTimeout(() => {
                        window.location.href = '/orders/{{ $order->id }}/confirmation';
                    }, 2000);
                } else {
                    this.result = {
                        success: false,
                        message: data.message || data.error || 'Error al procesar el pago'
                    };
                }
            } catch (error) {
                console.error('Error:', error);
                this.result = {
                    success: false,
                    message: 'Error de conexión. Por favor intenta nuevamente.'
                };
            } finally {
                this.processing = false;
            }
        }
    }
}
</script>
@endpush
@endsection
```

---

## 📧 Paso 7: Email de Confirmación (Opcional)

**`app/Mail/PaymentConfirmation.php`**

```php
<?php

namespace App\Mail;

use App\Models\Order;
use Illuminate\Bus\Queueable;
use Illuminate\Mail\Mailable;
use Illuminate\Queue\SerializesModels;

class PaymentConfirmation extends Mailable
{
    use Queueable, SerializesModels;

    public Order $order;
    public array $paymentDetails;

    public function __construct(Order $order, array $paymentDetails)
    {
        $this->order = $order;
        $this->paymentDetails = $paymentDetails;
    }

    public function build()
    {
        return $this->subject('Confirmación de Pago - Pedido #' . $this->order->number)
            ->view('emails.payment-confirmation');
    }
}
```

**`resources/views/emails/payment-confirmation.blade.php`**

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #4CAF50; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9f9f9; }
        .footer { padding: 10px; text-align: center; color: #666; }
        .amount { font-size: 24px; font-weight: bold; color: #4CAF50; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>✅ ¡Pago Exitoso!</h1>
        </div>
        
        <div class="content">
            <p>Hola {{ $order->user->name }},</p>
            
            <p>Tu pago ha sido procesado exitosamente.</p>
            
            <h3>Detalles del Pago:</h3>
            <ul>
                <li><strong>Pedido:</strong> #{{ $order->number }}</li>
                <li><strong>Monto:</strong> 
                    <span class="amount">
                        ${{ number_format($paymentDetails['amount_decimal'], 2) }} 
                        {{ strtoupper($paymentDetails['currency']) }}
                    </span>
                </li>
                <li><strong>Fecha:</strong> {{ now()->format('d/m/Y H:i') }}</li>
                <li><strong>Payment Intent ID:</strong> {{ $paymentDetails['payment_intent_id'] }}</li>
                @if(isset($paymentDetails['charge']['receipt_url']))
                <li>
                    <strong>Recibo:</strong> 
                    <a href="{{ $paymentDetails['charge']['receipt_url'] }}">Ver recibo</a>
                </li>
                @endif
            </ul>

            @if(isset($paymentDetails['charge']['card']))
            <h3>Tarjeta Usada:</h3>
            <p>
                {{ ucfirst($paymentDetails['charge']['card']['brand']) }} 
                ****{{ $paymentDetails['charge']['card']['last4'] }}
            </p>
            @endif

            <p>Gracias por tu compra!</p>
        </div>
        
        <div class="footer">
            <p>© {{ date('Y') }} Tu Empresa</p>
        </div>
    </div>
</body>
</html>
```

---

## ✅ Checklist de Implementación

### Backend Laravel
- [ ] Configurar variables de entorno (.env)
- [ ] Crear servicio StripePaymentIntentService
- [ ] Crear controlador PaymentController
- [ ] Registrar rutas en api.php
- [ ] Actualizar modelo Order
- [ ] Crear migración de campos de pago
- [ ] Crear email de confirmación (opcional)

### Frontend Laravel
- [ ] Crear vista de checkout
- [ ] Implementar Alpine.js para interactividad
- [ ] Cargar métodos de pago del usuario
- [ ] Botón de procesar pago
- [ ] Manejo de estados (loading, success, error)
- [ ] Redirección después del pago

### Testing
- [ ] Pago exitoso
- [ ] Pago rechazado
- [ ] Customer inactivo
- [ ] Orden ya pagada
- [ ] Cancelar pago
- [ ] Capturar pago autorizado

---

## 🧪 Testing Rápido (Artisan Tinker)

### 1. Abrir Tinker
```bash
php artisan tinker
```

### 2. Procesar Pago de Prueba
```php
$service = new App\Services\StripePaymentIntentService();

$result = $service->processPayment([
    'customer_id' => 'cus_xxx', // Tu customer ID real
    'payment_method_id' => 'pm_xxx', // Tu payment method ID real
    'amount' => 100.00,
    'currency' => 'mxn',
    'order_id' => 'ORDER-TINKER-001',
    'description' => 'Prueba desde Tinker'
]);

dd($result);
```

**Expected Output:**
```php
[
    "success" => true,
    "payment_intent_id" => "pi_xxx",
    "status" => "succeeded",
    "amount_decimal" => 100.0,
    "currency" => "mxn",
    // ...
]
```

---

## 🎯 Flujo de Testing Completo

### 1. Crear Customer (si no existe)
```php
$customerService = new App\Services\StripeCustomerService();
$customer = $customerService->createCustomer([
    'user_id' => '1',
    'name' => 'Test User',
    'email' => 'test@example.com',
    'phone' => '+525512345678'
]);
// Guardar: $customer['customer_id']
```

### 2. Registrar Payment Method (si no existe)
```php
$pmService = new App\Services\StripePaymentMethodService();
$pm = $pmService->attachPaymentMethod([
    'customer_id' => 'cus_xxx',
    'token' => 'tok_visa' // Token de prueba de Stripe
]);
// Guardar: $pm['payment_method_id']
```

### 3. Procesar Pago
```php
$paymentService = new App\Services\StripePaymentIntentService();
$result = $paymentService->processPayment([
    'customer_id' => 'cus_xxx',
    'payment_method_id' => 'pm_xxx',
    'amount' => 100.00,
    'currency' => 'mxn',
    'order_id' => 'ORDER-001'
]);
```

### 4. Verificar Resultado
```php
if ($result['success'] && $result['status'] === 'succeeded') {
    echo "✅ Pago exitoso!\n";
    echo "Payment Intent ID: {$result['payment_intent_id']}\n";
    echo "Monto: \${$result['amount_decimal']} {$result['currency']}\n";
    
    if (isset($result['charge']['receipt_url'])) {
        echo "Recibo: {$result['charge']['receipt_url']}\n";
    }
} else {
    echo "❌ Error: {$result['error_message']}\n";
}
```

---

## 📊 Estados del Pago y Acciones

| Estado | Descripción | Acción en Laravel |
|--------|-------------|-------------------|
| **succeeded** | ✅ Pagado | Emitir pedido, enviar confirmación |
| **processing** | ⏳ Procesando | Esperar webhook de confirmación |
| **requires_capture** | 💰 Autorizado | Capturar al enviar el pedido |
| **requires_action** | 🔐 Requiere 3DS | Frontend debe manejar |
| **canceled** | ❌ Cancelado | Marcar orden cancelada |
| **failed** | ❌ Fallido | Mostrar error, permitir reintentar |

---

## 🔄 Manejo de Estados en Laravel

### Ejemplo de Observer

**`app/Observers/OrderObserver.php`**

```php
<?php

namespace App\Observers;

use App\Models\Order;
use App\Jobs\ProcessOrder;
use App\Jobs\NotifyOrderCancellation;

class OrderObserver
{
    public function updated(Order $order)
    {
        // Si el pago fue exitoso, procesar el pedido
        if ($order->isDirty('payment_status') && $order->payment_status === 'succeeded') {
            ProcessOrder::dispatch($order);
        }

        // Si el pago fue cancelado, notificar
        if ($order->isDirty('payment_status') && $order->payment_status === 'canceled') {
            NotifyOrderCancellation::dispatch($order);
        }
    }
}
```

**Registrar el Observer en `AppServiceProvider`:**
```php
use App\Models\Order;
use App\Observers\OrderObserver;

public function boot()
{
    Order::observe(OrderObserver::class);
}
```

---

## 🎉 Testing Exitoso

### Deberías Ver:

1. **En Logs de Laravel:**
   ```
   [2024-01-15 10:30:00] Procesando pago {"order_id":"ORDER-001","amount":100,"currency":"mxn"}
   [2024-01-15 10:30:01] ✅ Pago procesado exitosamente {"payment_intent_id":"pi_xxx","status":"succeeded"}
   ```

2. **En Stripe Dashboard:**
   - Payment Intent creado
   - Estado: Succeeded
   - Monto correcto
   - Metadata con order_id

3. **En Base de Datos:**
   ```sql
   SELECT * FROM orders WHERE id = 'ORDER-001';
   -- stripe_payment_intent_id: pi_xxx
   -- payment_status: succeeded
   -- paid_at: 2024-01-15 10:30:01
   ```

---

## 🚨 Errores Comunes y Soluciones

### Error: "Customer inactivo"
**Solución:** Activar el customer
```php
$service = new App\Services\StripeCustomerService();
$service->updateCustomerStatus([
    'customer_id' => 'cus_xxx',
    'active' => true
]);
```

### Error: "Payment Method desasociado"
**Solución:** Registrar nuevamente el Payment Method

### Error: "No se puede cancelar succeeded"
**Solución:** Solo se pueden cancelar pagos NO exitosos. Para pagos exitosos, usa refunds.

---

## ✅ Checklist Final

### Implementación
- [ ] Servicio creado
- [ ] Controlador creado
- [ ] Rutas registradas
- [ ] Migración ejecutada
- [ ] Vista de checkout creada

### Testing Básico
- [ ] Pago exitoso
- [ ] Consultar pago
- [ ] Cancelar pago

### Testing Avanzado
- [ ] Diferentes monedas
- [ ] Autorización + Captura
- [ ] Manejo de errores
- [ ] Customer inactivo

---

**¡Laravel integrado exitosamente!** 🚀

**Tiempo estimado: 2-3 horas**
