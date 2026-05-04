# ✅ Laravel Refunds Implementation Checklist

## 📋 Checklist de Implementación - Gestión de Reembolsos

Este documento te guía paso a paso para integrar la API de Refunds de .NET en tu aplicación Laravel.

---

## 🎯 Fase 1: Preparación de Base de Datos

### 1.1 Crear Migración para Refunds

```bash
php artisan make:migration create_refunds_table
```

**Archivo:** `database/migrations/xxxx_create_refunds_table.php`

```php
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('refunds', function (Blueprint $table) {
            $table->id();
            $table->string('refund_id')->unique()->comment('Stripe Refund ID (re_xxx)');
            $table->string('payment_intent_id')->nullable()->comment('Stripe Payment Intent ID (pi_xxx)');
            $table->string('charge_id')->nullable()->comment('Stripe Charge ID (ch_xxx)');
            $table->foreignId('order_id')->nullable()->constrained()->onDelete('cascade');
            $table->decimal('amount', 10, 2)->comment('Monto reembolsado');
            $table->string('currency', 3)->default('USD');
            $table->enum('status', ['succeeded', 'pending', 'failed', 'canceled'])->default('succeeded');
            $table->string('reason', 500)->nullable();
            $table->text('notes')->nullable()->comment('Notas internas');
            $table->timestamp('refunded_at')->nullable();
            $table->timestamps();

            // Índices
            $table->index('payment_intent_id');
            $table->index('charge_id');
            $table->index('order_id');
            $table->index('status');
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('refunds');
    }
};
```

```bash
php artisan migrate
```

- [ ] Migración creada
- [ ] Migración ejecutada exitosamente

---

## 🏗️ Fase 2: Modelo Eloquent

### 2.1 Crear Modelo Refund

```bash
php artisan make:model Refund
```

**Archivo:** `app/Models/Refund.php`

```php
<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Refund extends Model
{
    use HasFactory;

    protected $fillable = [
        'refund_id',
        'payment_intent_id',
        'charge_id',
        'order_id',
        'amount',
        'currency',
        'status',
        'reason',
        'notes',
        'refunded_at'
    ];

    protected $casts = [
        'amount' => 'decimal:2',
        'refunded_at' => 'datetime',
        'created_at' => 'datetime',
        'updated_at' => 'datetime'
    ];

    /**
     * Relación con Order
     */
    public function order()
    {
        return $this->belongsTo(Order::class);
    }

    /**
     * Scope para reembolsos exitosos
     */
    public function scopeSuccessful($query)
    {
        return $query->where('status', 'succeeded');
    }

    /**
     * Scope para reembolsos pendientes
     */
    public function scopePending($query)
    {
        return $query->where('status', 'pending');
    }

    /**
     * Verificar si el reembolso es total
     */
    public function isFullRefund(): bool
    {
        if (!$this->order) {
            return false;
        }

        $totalRefunded = self::where('order_id', $this->order_id)
            ->where('status', 'succeeded')
            ->sum('amount');

        return $totalRefunded >= $this->order->total;
    }
}
```

- [ ] Modelo creado
- [ ] Propiedades `$fillable` configuradas
- [ ] Relaciones definidas

---

## 🔌 Fase 3: Servicio de Refunds

### 3.1 Crear RefundService

**Archivo:** `app/Services/RefundService.php`

```php
<?php

namespace App\Services;

use App\Models\Refund;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Log;

class RefundService
{
    private string $baseUrl;

    public function __construct()
    {
        $this->baseUrl = config('services.stripe_api.base_url', 'https://localhost:7001/api');
    }

    /**
     * Crear un reembolso para un Payment Intent
     */
    public function createRefundForPaymentIntent(
        string $paymentIntentId,
        ?float $amount = null,
        ?string $reason = null,
        ?int $orderId = null
    ): array {
        try {
            Log::info("Creando reembolso para Payment Intent", [
                'payment_intent_id' => $paymentIntentId,
                'amount' => $amount,
                'reason' => $reason
            ]);

            $response = Http::post(
                "{$this->baseUrl}/refunds/payment-intent/{$paymentIntentId}",
                array_filter([
                    'amount' => $amount,
                    'reason' => $reason ?? 'requested_by_customer'
                ])
            );

            if (!$response->successful()) {
                throw new \Exception($response->json()['error'] ?? 'Error al crear reembolso');
            }

            $data = $response->json();

            // Guardar en base de datos
            $refund = Refund::create([
                'refund_id' => $data['refundId'],
                'payment_intent_id' => $paymentIntentId,
                'order_id' => $orderId,
                'amount' => $data['amount'],
                'currency' => $data['currency'],
                'status' => $this->mapStatus($data['status']),
                'reason' => $reason,
                'refunded_at' => now()
            ]);

            Log::info("Reembolso creado exitosamente", [
                'refund_id' => $data['refundId'],
                'amount' => $data['amount']
            ]);

            return $data;
        } catch (\Exception $e) {
            Log::error("Error al crear reembolso", [
                'payment_intent_id' => $paymentIntentId,
                'error' => $e->getMessage()
            ]);
            throw $e;
        }
    }

    /**
     * Crear un reembolso para un Charge
     */
    public function createRefundForCharge(
        string $chargeId,
        ?float $amount = null,
        ?string $reason = null,
        ?int $orderId = null
    ): array {
        try {
            Log::info("Creando reembolso para Charge", [
                'charge_id' => $chargeId,
                'amount' => $amount
            ]);

            $response = Http::post(
                "{$this->baseUrl}/refunds/charge/{$chargeId}",
                array_filter([
                    'amount' => $amount,
                    'reason' => $reason ?? 'requested_by_customer'
                ])
            );

            if (!$response->successful()) {
                throw new \Exception($response->json()['error'] ?? 'Error al crear reembolso');
            }

            $data = $response->json();

            // Guardar en base de datos
            $refund = Refund::create([
                'refund_id' => $data['refundId'],
                'charge_id' => $chargeId,
                'order_id' => $orderId,
                'amount' => $data['amount'],
                'currency' => $data['currency'],
                'status' => $this->mapStatus($data['status']),
                'reason' => $reason,
                'refunded_at' => now()
            ]);

            return $data;
        } catch (\Exception $e) {
            Log::error("Error al crear reembolso", [
                'charge_id' => $chargeId,
                'error' => $e->getMessage()
            ]);
            throw $e;
        }
    }

    /**
     * Obtener información de un reembolso
     */
    public function getRefund(string $refundId): array
    {
        $response = Http::get("{$this->baseUrl}/refunds/{$refundId}");

        if (!$response->successful()) {
            throw new \Exception($response->json()['error'] ?? 'Error al obtener reembolso');
        }

        return $response->json();
    }

    /**
     * Listar reembolsos
     */
    public function listRefunds(int $limit = 10): array
    {
        $response = Http::get("{$this->baseUrl}/refunds", [
            'limit' => $limit
        ]);

        if (!$response->successful()) {
            throw new \Exception('Error al listar reembolsos');
        }

        return $response->json();
    }

    /**
     * Mapear estado de .NET a Laravel
     */
    private function mapStatus(string $status): string
    {
        return match ($status) {
            'Refunded' => 'succeeded',
            'Pending' => 'pending',
            'Failed' => 'failed',
            'Cancelled' => 'canceled',
            default => 'pending'
        };
    }
}
```

- [ ] Servicio creado
- [ ] Métodos implementados
- [ ] Logging configurado

---

## 🎮 Fase 4: Controlador API

### 4.1 Crear RefundController

```bash
php artisan make:controller Api/RefundController
```

**Archivo:** `app/Http/Controllers/Api/RefundController.php`

```php
<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Services\RefundService;
use Illuminate\Http\Request;
use Illuminate\Http\JsonResponse;

class RefundController extends Controller
{
    private RefundService $refundService;

    public function __construct(RefundService $refundService)
    {
        $this->refundService = $refundService;
    }

    /**
     * Crear reembolso para un Payment Intent
     */
    public function createForPaymentIntent(Request $request, string $paymentIntentId): JsonResponse
    {
        $validated = $request->validate([
            'amount' => 'nullable|numeric|min:0.01|max:1000000',
            'reason' => 'nullable|string|max:500',
            'order_id' => 'nullable|integer|exists:orders,id'
        ]);

        try {
            $refund = $this->refundService->createRefundForPaymentIntent(
                $paymentIntentId,
                $validated['amount'] ?? null,
                $validated['reason'] ?? null,
                $validated['order_id'] ?? null
            );

            return response()->json([
                'success' => true,
                'data' => $refund,
                'message' => 'Reembolso creado exitosamente'
            ], 200);
        } catch (\Exception $e) {
            return response()->json([
                'success' => false,
                'error' => $e->getMessage()
            ], 500);
        }
    }

    /**
     * Crear reembolso para un Charge
     */
    public function createForCharge(Request $request, string $chargeId): JsonResponse
    {
        $validated = $request->validate([
            'amount' => 'nullable|numeric|min:0.01|max:1000000',
            'reason' => 'nullable|string|max:500',
            'order_id' => 'nullable|integer|exists:orders,id'
        ]);

        try {
            $refund = $this->refundService->createRefundForCharge(
                $chargeId,
                $validated['amount'] ?? null,
                $validated['reason'] ?? null,
                $validated['order_id'] ?? null
            );

            return response()->json([
                'success' => true,
                'data' => $refund,
                'message' => 'Reembolso creado exitosamente'
            ], 200);
        } catch (\Exception $e) {
            return response()->json([
                'success' => false,
                'error' => $e->getMessage()
            ], 500);
        }
    }

    /**
     * Obtener información de un reembolso
     */
    public function show(string $refundId): JsonResponse
    {
        try {
            $refund = $this->refundService->getRefund($refundId);

            return response()->json([
                'success' => true,
                'data' => $refund
            ], 200);
        } catch (\Exception $e) {
            return response()->json([
                'success' => false,
                'error' => $e->getMessage()
            ], 404);
        }
    }

    /**
     * Listar reembolsos
     */
    public function index(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'limit' => 'nullable|integer|min:1|max:100'
        ]);

        try {
            $refunds = $this->refundService->listRefunds(
                $validated['limit'] ?? 10
            );

            return response()->json([
                'success' => true,
                'data' => $refunds,
                'count' => count($refunds)
            ], 200);
        } catch (\Exception $e) {
            return response()->json([
                'success' => false,
                'error' => $e->getMessage()
            ], 500);
        }
    }
}
```

- [ ] Controlador creado
- [ ] Validaciones implementadas
- [ ] Respuestas estructuradas correctamente

---

## 🛣️ Fase 5: Rutas API

### 5.1 Definir Rutas

**Archivo:** `routes/api.php`

```php
use App\Http\Controllers\Api\RefundController;

Route::prefix('refunds')->middleware(['api'])->group(function () {
    // Crear reembolsos
    Route::post('/payment-intent/{paymentIntentId}', [RefundController::class, 'createForPaymentIntent']);
    Route::post('/charge/{chargeId}', [RefundController::class, 'createForCharge']);
    
    // Consultar reembolsos
    Route::get('/{refundId}', [RefundController::class, 'show']);
    Route::get('/', [RefundController::class, 'index']);
});
```

- [ ] Rutas definidas
- [ ] Middleware aplicado (si es necesario)
- [ ] Prefijos correctos

---

## 🔧 Fase 6: Configuración

### 6.1 Actualizar config/services.php

```php
'stripe_api' => [
    'base_url' => env('STRIPE_API_BASE_URL', 'https://localhost:7001/api'),
    'timeout' => env('STRIPE_API_TIMEOUT', 30),
],
```

### 6.2 Actualizar .env

```env
STRIPE_API_BASE_URL=https://localhost:7001/api
STRIPE_API_TIMEOUT=30
```

- [ ] Configuración añadida
- [ ] Variables de entorno definidas

---

## 📊 Fase 7: Lógica de Negocio en Order

### 7.1 Actualizar Order Model

**Archivo:** `app/Models/Order.php`

```php
/**
 * Relación con Refunds
 */
public function refunds()
{
    return $this->hasMany(Refund::class);
}

/**
 * Calcular monto total reembolsado
 */
public function getTotalRefundedAttribute(): float
{
    return $this->refunds()
        ->where('status', 'succeeded')
        ->sum('amount');
}

/**
 * Verificar si el pedido está completamente reembolsado
 */
public function isFullyRefunded(): bool
{
    return $this->totalRefunded >= $this->total;
}

/**
 * Verificar si el pedido está parcialmente reembolsado
 */
public function isPartiallyRefunded(): bool
{
    return $this->totalRefunded > 0 && $this->totalRefunded < $this->total;
}

/**
 * Obtener monto disponible para reembolsar
 */
public function getRefundableAmountAttribute(): float
{
    return max(0, $this->total - $this->totalRefunded);
}
```

- [ ] Relaciones agregadas
- [ ] Métodos auxiliares implementados
- [ ] Atributos calculados definidos

---

## 🎯 Fase 8: Form Request para Validación

### 8.1 Crear Form Request

```bash
php artisan make:request CreateRefundRequest
```

**Archivo:** `app/Http/Requests/CreateRefundRequest.php`

```php
<?php

namespace App\Http\Requests;

use Illuminate\Foundation\Http\FormRequest;

class CreateRefundRequest extends FormRequest
{
    public function authorize(): bool
    {
        return true; // Ajustar según tus políticas de autorización
    }

    public function rules(): array
    {
        return [
            'amount' => 'nullable|numeric|min:0.01|max:1000000',
            'reason' => 'nullable|string|max:500',
            'order_id' => 'required|integer|exists:orders,id',
            'notes' => 'nullable|string|max:1000'
        ];
    }

    public function messages(): array
    {
        return [
            'amount.min' => 'El monto mínimo de reembolso es $0.01',
            'amount.max' => 'El monto máximo de reembolso es $1,000,000',
            'order_id.required' => 'El ID del pedido es requerido',
            'order_id.exists' => 'El pedido no existe'
        ];
    }
}
```

- [ ] Form Request creado
- [ ] Reglas de validación definidas
- [ ] Mensajes personalizados agregados

---

## 🔔 Fase 9: Notificaciones

### 9.1 Crear Notificación de Reembolso

```bash
php artisan make:notification RefundCreatedNotification
```

**Archivo:** `app/Notifications/RefundCreatedNotification.php`

```php
<?php

namespace App\Notifications;

use App\Models\Refund;
use Illuminate\Notifications\Notification;
use Illuminate\Notifications\Messages\MailMessage;

class RefundCreatedNotification extends Notification
{
    private Refund $refund;

    public function __construct(Refund $refund)
    {
        $this->refund = $refund;
    }

    public function via($notifiable): array
    {
        return ['mail', 'database'];
    }

    public function toMail($notifiable): MailMessage
    {
        return (new MailMessage)
            ->subject('Reembolso Procesado')
            ->greeting('¡Hola ' . $notifiable->name . '!')
            ->line('Tu solicitud de reembolso ha sido procesada.')
            ->line('Monto reembolsado: $' . number_format($this->refund->amount, 2))
            ->line('El dinero debería aparecer en tu cuenta en 5-10 días hábiles.')
            ->line('Referencia: ' . $this->refund->refund_id);
    }

    public function toArray($notifiable): array
    {
        return [
            'refund_id' => $this->refund->refund_id,
            'amount' => $this->refund->amount,
            'currency' => $this->refund->currency,
            'order_id' => $this->refund->order_id
        ];
    }
}
```

- [ ] Notificación creada
- [ ] Canales configurados (mail, database)
- [ ] Contenido del email personalizado

---

## 🧩 Fase 10: Jobs para Procesamiento Asíncrono

### 10.1 Crear Job de Reembolso

```bash
php artisan make:job ProcessRefundJob
```

**Archivo:** `app/Jobs/ProcessRefundJob.php`

```php
<?php

namespace App\Jobs;

use App\Models\Order;
use App\Services\RefundService;
use App\Notifications\RefundCreatedNotification;
use Illuminate\Bus\Queueable;
use Illuminate\Contracts\Queue\ShouldQueue;
use Illuminate\Foundation\Bus\Dispatchable;
use Illuminate\Queue\InteractsWithQueue;
use Illuminate\Queue\SerializesModels;

class ProcessRefundJob implements ShouldQueue
{
    use Dispatchable, InteractsWithQueue, Queueable, SerializesModels;

    public function __construct(
        private int $orderId,
        private ?float $amount,
        private ?string $reason
    ) {}

    public function handle(RefundService $refundService): void
    {
        $order = Order::findOrFail($this->orderId);

        // Verificar que el pedido tenga un payment_intent_id
        if (empty($order->payment_intent_id)) {
            throw new \Exception('El pedido no tiene un Payment Intent asociado');
        }

        // Crear el reembolso
        $refundData = $refundService->createRefundForPaymentIntent(
            $order->payment_intent_id,
            $this->amount,
            $this->reason,
            $this->orderId
        );

        // Actualizar estado del pedido si es reembolso total
        if ($order->isFullyRefunded()) {
            $order->update(['status' => 'refunded']);
        } elseif ($order->isPartiallyRefunded()) {
            $order->update(['status' => 'partially_refunded']);
        }

        // Notificar al cliente
        $order->user->notify(new RefundCreatedNotification($order->refunds()->latest()->first()));
    }
}
```

- [ ] Job creado
- [ ] Lógica de negocio implementada
- [ ] Notificaciones integradas

---

## 🌐 Fase 11: Controlador Web (Opcional)

### 11.1 Crear Controlador para Admin Panel

```bash
php artisan make:controller Admin/RefundController
```

**Archivo:** `app/Http/Controllers/Admin/RefundController.php`

```php
<?php

namespace App\Http\Controllers\Admin;

use App\Http\Controllers\Controller;
use App\Models\Order;
use App\Jobs\ProcessRefundJob;
use Illuminate\Http\Request;

class RefundController extends Controller
{
    /**
     * Mostrar formulario de reembolso
     */
    public function create(Order $order)
    {
        // Verificar que el pedido se pueda reembolsar
        if ($order->refundableAmount <= 0) {
            return back()->with('error', 'Este pedido ya está completamente reembolsado');
        }

        return view('admin.refunds.create', compact('order'));
    }

    /**
     * Procesar reembolso
     */
    public function store(Request $request, Order $order)
    {
        $validated = $request->validate([
            'amount' => 'nullable|numeric|min:0.01|max:' . $order->refundableAmount,
            'reason' => 'required|string|max:500',
            'notes' => 'nullable|string|max:1000'
        ]);

        // Encolar el job de reembolso
        ProcessRefundJob::dispatch(
            $order->id,
            $validated['amount'] ?? null,
            $validated['reason']
        );

        return redirect()->route('admin.orders.show', $order)
            ->with('success', 'Reembolso iniciado. Se procesará en breve.');
    }

    /**
     * Ver historial de reembolsos de un pedido
     */
    public function index(Order $order)
    {
        $refunds = $order->refunds()->latest()->get();

        return view('admin.refunds.index', compact('order', 'refunds'));
    }
}
```

- [ ] Controlador admin creado
- [ ] Validaciones por Order implementadas
- [ ] Jobs integrados

---

## 🎨 Fase 12: Vistas Blade (Opcional)

### 12.1 Vista para Crear Reembolso

**Archivo:** `resources/views/admin/refunds/create.blade.php`

```blade
@extends('layouts.admin')

@section('content')
<div class="container">
    <h1>Crear Reembolso - Pedido #{{ $order->id }}</h1>

    <div class="card mb-3">
        <div class="card-body">
            <h5>Información del Pedido</h5>
            <p><strong>Total:</strong> ${{ number_format($order->total, 2) }}</p>
            <p><strong>Reembolsado:</strong> ${{ number_format($order->totalRefunded, 2) }}</p>
            <p><strong>Disponible para reembolsar:</strong> ${{ number_format($order->refundableAmount, 2) }}</p>
        </div>
    </div>

    <form action="{{ route('admin.refunds.store', $order) }}" method="POST">
        @csrf

        <div class="mb-3">
            <label class="form-label">Monto a Reembolsar</label>
            <input type="number" name="amount" class="form-control" 
                   step="0.01" min="0.01" max="{{ $order->refundableAmount }}"
                   placeholder="Dejar vacío para reembolso total">
            <small class="text-muted">Máximo: ${{ number_format($order->refundableAmount, 2) }}</small>
        </div>

        <div class="mb-3">
            <label class="form-label">Razón del Reembolso *</label>
            <select name="reason" class="form-select" required>
                <option value="">Seleccionar...</option>
                <option value="requested_by_customer">Solicitado por cliente</option>
                <option value="duplicate">Transacción duplicada</option>
                <option value="fraudulent">Transacción fraudulenta</option>
            </select>
        </div>

        <div class="mb-3">
            <label class="form-label">Notas Internas</label>
            <textarea name="notes" class="form-control" rows="3"></textarea>
        </div>

        <button type="submit" class="btn btn-primary">Procesar Reembolso</button>
        <a href="{{ route('admin.orders.show', $order) }}" class="btn btn-secondary">Cancelar</a>
    </form>
</div>
@endsection
```

- [ ] Vista de creación implementada
- [ ] Validaciones del lado del cliente agregadas
- [ ] UX clara y simple

---

## 🧪 Fase 13: Testing con PHPUnit

### 13.1 Crear Test

```bash
php artisan make:test RefundServiceTest
```

**Archivo:** `tests/Feature/RefundServiceTest.php`

```php
<?php

namespace Tests\Feature;

use Tests\TestCase;
use App\Services\RefundService;
use App\Models\Order;
use Illuminate\Foundation\Testing\RefreshDatabase;

class RefundServiceTest extends TestCase
{
    use RefreshDatabase;

    private RefundService $refundService;

    protected function setUp(): void
    {
        parent::setUp();
        $this->refundService = new RefundService();
    }

    /** @test */
    public function it_can_create_full_refund_for_payment_intent()
    {
        // Arrange: Crear un pedido con payment_intent_id
        $order = Order::factory()->create([
            'payment_intent_id' => 'pi_test_123',
            'total' => 100.00
        ]);

        // Act: Crear reembolso
        $refund = $this->refundService->createRefundForPaymentIntent(
            $order->payment_intent_id,
            null, // Reembolso total
            'Test reason',
            $order->id
        );

        // Assert
        $this->assertArrayHasKey('refundId', $refund);
        $this->assertEquals($order->payment_intent_id, $refund['originalTransactionId']);
        $this->assertDatabaseHas('refunds', [
            'payment_intent_id' => $order->payment_intent_id,
            'order_id' => $order->id
        ]);
    }

    /** @test */
    public function it_can_create_partial_refund()
    {
        $order = Order::factory()->create([
            'payment_intent_id' => 'pi_test_456',
            'total' => 100.00
        ]);

        $refund = $this->refundService->createRefundForPaymentIntent(
            $order->payment_intent_id,
            25.00, // Reembolso parcial
            'Partial refund test',
            $order->id
        );

        $this->assertEquals(25.00, $refund['amount']);
    }

    /** @test */
    public function it_updates_order_status_after_full_refund()
    {
        $order = Order::factory()->create([
            'payment_intent_id' => 'pi_test_789',
            'total' => 100.00,
            'status' => 'completed'
        ]);

        ProcessRefundJob::dispatch($order->id, null, 'Full refund');

        $order->refresh();
        $this->assertEquals('refunded', $order->status);
    }
}
```

- [ ] Tests creados
- [ ] Tests pasan exitosamente

---

## 🎭 Fase 14: Casos de Uso Implementados

### 14.1 Reembolso desde Admin Panel

```php
// En el controlador de Orders
public function refund(Order $order)
{
    // Verificar que se puede reembolsar
    if ($order->refundableAmount <= 0) {
        return back()->with('error', 'No hay monto disponible para reembolsar');
    }

    // Redireccionar al formulario de reembolso
    return redirect()->route('admin.refunds.create', $order);
}
```

### 14.2 Reembolso Automático por Timeout

```php
// Job para reembolsar pedidos no enviados en X días
class RefundUnshippedOrdersJob implements ShouldQueue
{
    public function handle(RefundService $refundService): void
    {
        $orders = Order::where('status', 'paid')
            ->where('created_at', '<', now()->subDays(7))
            ->whereNull('shipped_at')
            ->get();

        foreach ($orders as $order) {
            $refundService->createRefundForPaymentIntent(
                $order->payment_intent_id,
                null, // Reembolso total
                'Pedido no enviado en tiempo',
                $order->id
            );

            $order->update(['status' => 'refunded']);
        }
    }
}
```

### 14.3 API Endpoint para Cliente

```php
// Permitir que clientes soliciten reembolsos
Route::post('/my/orders/{order}/refund', function (Order $order) {
    // Verificar que el usuario es el dueño
    if ($order->user_id !== auth()->id()) {
        abort(403);
    }

    // Verificar reglas de negocio (ej: dentro de 30 días)
    if ($order->created_at->diffInDays(now()) > 30) {
        return response()->json(['error' => 'El período de reembolso ha expirado'], 422);
    }

    // Encolar reembolso
    ProcessRefundJob::dispatch(
        $order->id,
        null,
        'requested_by_customer'
    );

    return response()->json(['message' => 'Solicitud de reembolso enviada']);
})->middleware('auth:sanctum');
```

- [ ] Casos de uso implementados según necesidades del negocio

---

## 📈 Fase 15: Reportes y Analytics

### 15.1 Dashboard de Reembolsos

```php
// app/Http/Controllers/Admin/DashboardController.php

public function refundsReport()
{
    $stats = [
        'total_refunds' => Refund::where('status', 'succeeded')->count(),
        'total_amount' => Refund::where('status', 'succeeded')->sum('amount'),
        'refunds_today' => Refund::whereDate('created_at', today())->count(),
        'refunds_this_month' => Refund::whereMonth('created_at', now()->month)->count(),
        'refund_rate' => $this->calculateRefundRate()
    ];

    $recentRefunds = Refund::with('order.user')
        ->latest()
        ->take(20)
        ->get();

    return view('admin.reports.refunds', compact('stats', 'recentRefunds'));
}

private function calculateRefundRate(): float
{
    $totalOrders = Order::where('status', '!=', 'canceled')->count();
    $refundedOrders = Order::whereIn('status', ['refunded', 'partially_refunded'])->count();

    return $totalOrders > 0 ? ($refundedOrders / $totalOrders) * 100 : 0;
}
```

- [ ] Dashboard de reembolsos implementado
- [ ] Métricas clave agregadas

---

## ✅ Checklist de Verificación Final

### Base de Datos
- [ ] Tabla `refunds` creada correctamente
- [ ] Índices configurados
- [ ] Relaciones funcionando

### Servicios
- [ ] `RefundService` funcionando
- [ ] Errores manejados correctamente
- [ ] Logs implementados

### Controladores
- [ ] API Controller funcionando
- [ ] Validaciones implementadas
- [ ] Respuestas consistentes

### Integración con .NET API
- [ ] Conexión exitosa con el microservicio
- [ ] Reembolsos se crean en Stripe
- [ ] Datos se sincronizan correctamente

### Notificaciones
- [ ] Emails de reembolso enviándose
- [ ] Notificaciones en base de datos guardándose

### Testing
- [ ] Tests unitarios pasan
- [ ] Tests de integración pasan
- [ ] Tests manuales verificados

---

## 🚀 Prueba End-to-End Completa

```bash
# 1. Crear un pedido y procesarlo
php artisan tinker
>>> $order = Order::create([
    'user_id' => 1,
    'total' => 100.00,
    'status' => 'paid',
    'payment_intent_id' => 'pi_test_complete_flow'
]);

# 2. Procesar un reembolso parcial
>>> dispatch(new \App\Jobs\ProcessRefundJob($order->id, 40.00, 'Artículo devuelto'));

# 3. Verificar en la base de datos
>>> $order->refresh();
>>> $order->totalRefunded; // Debe ser 40.00
>>> $order->refundableAmount; // Debe ser 60.00
>>> $order->status; // Debe ser 'partially_refunded'

# 4. Verificar el reembolso
>>> $refund = $order->refunds->first();
>>> $refund->refund_id; // Debe comenzar con 're_'
>>> $refund->status; // Debe ser 'succeeded'
```

- [ ] Flujo completo funciona sin errores
- [ ] Estados se actualizan correctamente
- [ ] Datos en DB son correctos

---

## 📚 Documentación a Crear

- [ ] README.md actualizado
- [ ] API documentation actualizada
- [ ] Guía de usuario para admin panel
- [ ] Política de reembolsos documentada

---

## 🎉 Implementación Completada

Cuando todos los items estén marcados, tu integración de Refunds estará completa y lista para producción.

---

## 🆘 Soporte

**Problemas comunes:**
1. **Timeout en la conexión**: Aumentar `STRIPE_API_TIMEOUT`
2. **Certificado SSL**: Configurar `Http::withoutVerifying()` en desarrollo
3. **Payment Intent no encontrado**: Verificar que el ID sea correcto y exista en Stripe

**Recursos:**
- [REFUNDS_API_GUIDE.md](ECommerceAPI/docs/REFUNDS_API_GUIDE.md) - Documentación de la API .NET
- [Stripe Refunds Docs](https://stripe.com/docs/refunds) - Documentación oficial

---

**¡Feliz codificación! 🚀**
