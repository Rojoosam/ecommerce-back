# ✅ Checklist de Implementación - Laravel

## 📋 Guía Paso a Paso para Integrar con la API de .NET

---

## Paso 1: Preparar la Base de Datos

### 1.1 Crear Migración
```bash
php artisan make:migration add_stripe_customer_id_to_users_table
```

### 1.2 Editar Migración
**Archivo**: `database/migrations/YYYY_MM_DD_XXXXXX_add_stripe_customer_id_to_users_table.php`

```php
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up()
    {
        Schema::table('users', function (Blueprint $table) {
            $table->string('stripe_customer_id')->nullable()->after('email');
            $table->index('stripe_customer_id');
        });
    }

    public function down()
    {
        Schema::table('users', function (Blueprint $table) {
            $table->dropColumn('stripe_customer_id');
        });
    }
};
```

### 1.3 Ejecutar Migración
```bash
php artisan migrate
```

**✅ Checklist**
- [ ] Migración creada
- [ ] Migración ejecutada sin errores
- [ ] Columna `stripe_customer_id` visible en la tabla `users`

---

## Paso 2: Actualizar el Modelo User

### 2.1 Editar Modelo
**Archivo**: `app/Models/User.php`

```php
<?php

namespace App\Models;

use Illuminate\Foundation\Auth\User as Authenticatable;
use Illuminate\Notifications\Notifiable;

class User extends Authenticatable
{
    use Notifiable;

    protected $fillable = [
        'name',
        'email',
        'password',
        'stripe_customer_id',  // 👈 Agregar esto
    ];

    // ... resto del código
}
```

**✅ Checklist**
- [ ] Campo `stripe_customer_id` agregado a `$fillable`
- [ ] No hay errores de sintaxis

---

## Paso 3: Configurar la API

### 3.1 Actualizar .env
**Archivo**: `.env`

```env
# ... otras configuraciones

ECOMMERCE_API_URL=https://tu-api-dotnet.com
```

### 3.2 Actualizar config/services.php
**Archivo**: `config/services.php`

```php
return [
    // ... otras configuraciones

    'ecommerce_api' => [
        'base_url' => env('ECOMMERCE_API_URL', 'https://localhost:7000'),
    ],
];
```

**✅ Checklist**
- [ ] Variable `ECOMMERCE_API_URL` agregada a `.env`
- [ ] Configuración agregada a `config/services.php`
- [ ] URL apunta correctamente a la API de .NET

---

## Paso 4: Crear Servicio de Stripe Customer

### 4.1 Crear Servicio
```bash
php artisan make:class Services/StripeCustomerService
```

### 4.2 Implementar Servicio
**Archivo**: `app/Services/StripeCustomerService.php`

```php
<?php

namespace App\Services;

use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Log;

class StripeCustomerService
{
    private string $apiBaseUrl;

    public function __construct()
    {
        $this->apiBaseUrl = config('services.ecommerce_api.base_url') . '/api/customers';
    }

    /**
     * Crear un customer en Stripe
     */
    public function createCustomer(array $data): ?string
    {
        try {
            $response = Http::post($this->apiBaseUrl, [
                'user_id' => (string)$data['user_id'],
                'name' => $data['name'],
                'email' => $data['email'],
                'phone' => $data['phone'] ?? null,
                'address' => $data['address'] ?? null,
            ]);

            $result = $response->json();

            if ($result['success'] ?? false) {
                Log::info('Customer creado en Stripe', [
                    'customer_id' => $result['customer_id'],
                    'user_id' => $data['user_id']
                ]);
                return $result['customer_id'];
            }

            Log::error('Error al crear customer en Stripe', [
                'error' => $result['error_message'] ?? 'Unknown error',
                'user_id' => $data['user_id']
            ]);

            return null;
        } catch (\Exception $e) {
            Log::error('Excepción al crear customer en Stripe', [
                'error' => $e->getMessage(),
                'user_id' => $data['user_id']
            ]);
            return null;
        }
    }

    /**
     * Actualizar un customer en Stripe
     */
    public function updateCustomer(string $customerId, array $data): bool
    {
        try {
            $payload = array_filter([
                'customer_id' => $customerId,
                'name' => $data['name'] ?? null,
                'email' => $data['email'] ?? null,
                'phone' => $data['phone'] ?? null,
                'address' => $data['address'] ?? null,
            ], fn($value) => $value !== null);

            $response = Http::put($this->apiBaseUrl, $payload);
            $result = $response->json();

            $success = $result['success'] ?? false;

            if ($success) {
                Log::info('Customer actualizado en Stripe', [
                    'customer_id' => $customerId
                ]);
            }

            return $success;
        } catch (\Exception $e) {
            Log::error('Excepción al actualizar customer en Stripe', [
                'error' => $e->getMessage(),
                'customer_id' => $customerId
            ]);
            return false;
        }
    }

    /**
     * Obtener un customer desde Stripe
     */
    public function getCustomer(string $customerId): ?array
    {
        try {
            $response = Http::get("{$this->apiBaseUrl}/{$customerId}");
            $result = $response->json();

            if ($result['success'] ?? false) {
                return $result;
            }

            Log::warning('Customer no encontrado en Stripe', [
                'customer_id' => $customerId
            ]);

            return null;
        } catch (\Exception $e) {
            Log::error('Excepción al obtener customer desde Stripe', [
                'error' => $e->getMessage(),
                'customer_id' => $customerId
            ]);
            return null;
        }
    }

    /**
     * Eliminar un customer de Stripe
     */
    public function deleteCustomer(string $customerId): bool
    {
        try {
            $response = Http::delete("{$this->apiBaseUrl}/{$customerId}");
            $result = $response->json();

            $success = $result['success'] ?? false;

            if ($success) {
                Log::info('Customer eliminado en Stripe', [
                    'customer_id' => $customerId
                ]);
            }

            return $success;
        } catch (\Exception $e) {
            Log::error('Excepción al eliminar customer de Stripe', [
                'error' => $e->getMessage(),
                'customer_id' => $customerId
            ]);
            return false;
        }
    }
}
```

**✅ Checklist**
- [ ] Servicio creado en `app/Services/StripeCustomerService.php`
- [ ] Todos los métodos implementados
- [ ] No hay errores de sintaxis

---

## Paso 5: Crear Controlador

### 5.1 Crear Controlador
```bash
php artisan make:controller Api/CustomerController
```

### 5.2 Implementar Controlador
**Archivo**: `app/Http/Controllers/Api/CustomerController.php`

```php
<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Services\StripeCustomerService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class CustomerController extends Controller
{
    private StripeCustomerService $customerService;

    public function __construct(StripeCustomerService $customerService)
    {
        $this->customerService = $customerService;
    }

    /**
     * Crear customer en Stripe
     */
    public function create(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'name' => 'required|string|max:255',
            'email' => 'required|email',
            'phone' => 'nullable|string',
            'address' => 'nullable|array',
            'address.line1' => 'nullable|string',
            'address.line2' => 'nullable|string',
            'address.city' => 'nullable|string',
            'address.state' => 'nullable|string',
            'address.postal_code' => 'nullable|string',
            'address.country' => 'nullable|string|size:2',
        ]);

        $customerId = $this->customerService->createCustomer([
            'user_id' => auth()->id(),
            'name' => $validated['name'],
            'email' => $validated['email'],
            'phone' => $validated['phone'] ?? null,
            'address' => $validated['address'] ?? null,
        ]);

        if ($customerId) {
            auth()->user()->update([
                'stripe_customer_id' => $customerId
            ]);

            return response()->json([
                'success' => true,
                'message' => 'Customer creado exitosamente',
                'customer_id' => $customerId
            ], 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al crear customer en Stripe'
        ], 500);
    }

    /**
     * Actualizar customer en Stripe
     */
    public function update(Request $request): JsonResponse
    {
        $customerId = auth()->user()->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'success' => false,
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        $validated = $request->validate([
            'name' => 'nullable|string|max:255',
            'email' => 'nullable|email',
            'phone' => 'nullable|string',
            'address' => 'nullable|array',
        ]);

        $success = $this->customerService->updateCustomer($customerId, $validated);

        if ($success) {
            return response()->json([
                'success' => true,
                'message' => 'Customer actualizado exitosamente'
            ], 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al actualizar customer'
        ], 500);
    }

    /**
     * Obtener customer desde Stripe
     */
    public function show(): JsonResponse
    {
        $customerId = auth()->user()->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'success' => false,
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        $customer = $this->customerService->getCustomer($customerId);

        if ($customer) {
            return response()->json($customer, 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al obtener customer'
        ], 500);
    }

    /**
     * Eliminar customer de Stripe
     */
    public function destroy(): JsonResponse
    {
        $customerId = auth()->user()->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'success' => false,
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        $success = $this->customerService->deleteCustomer($customerId);

        if ($success) {
            auth()->user()->update([
                'stripe_customer_id' => null
            ]);

            return response()->json([
                'success' => true,
                'message' => 'Customer eliminado exitosamente'
            ], 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al eliminar customer'
        ], 500);
    }
}
```

**✅ Checklist**
- [ ] Controlador creado
- [ ] Métodos CRUD implementados
- [ ] Validaciones en su lugar
- [ ] No hay errores de sintaxis

---

## Paso 6: Registrar Rutas

### 6.1 Editar routes/api.php
**Archivo**: `routes/api.php`

```php
use App\Http\Controllers\Api\CustomerController;

Route::middleware('auth:sanctum')->prefix('stripe')->group(function () {
    Route::post('/customer', [CustomerController::class, 'create']);
    Route::put('/customer', [CustomerController::class, 'update']);
    Route::get('/customer', [CustomerController::class, 'show']);
    Route::delete('/customer', [CustomerController::class, 'destroy']);
});
```

**✅ Checklist**
- [ ] Rutas agregadas a `routes/api.php`
- [ ] Middleware de autenticación aplicado
- [ ] Rutas agrupadas bajo prefijo `/stripe`

---

## Paso 7: (Opcional) Auto-crear Customer al Registrar

### 7.1 Crear Observer
```bash
php artisan make:observer UserObserver --model=User
```

### 7.2 Implementar Observer
**Archivo**: `app/Observers/UserObserver.php`

```php
<?php

namespace App\Observers;

use App\Models\User;
use App\Services\StripeCustomerService;
use Illuminate\Support\Facades\Log;

class UserObserver
{
    private StripeCustomerService $customerService;

    public function __construct(StripeCustomerService $customerService)
    {
        $this->customerService = $customerService;
    }

    /**
     * Handle the User "created" event.
     */
    public function created(User $user): void
    {
        // Crear customer en Stripe automáticamente
        $customerId = $this->customerService->createCustomer([
            'user_id' => $user->id,
            'name' => $user->name,
            'email' => $user->email,
        ]);

        if ($customerId) {
            $user->update([
                'stripe_customer_id' => $customerId
            ]);

            Log::info("Customer de Stripe creado automáticamente para usuario {$user->id}: {$customerId}");
        } else {
            Log::error("Error al crear Customer de Stripe automáticamente para usuario {$user->id}");
        }
    }
}
```

### 7.3 Registrar Observer
**Archivo**: `app/Providers/EventServiceProvider.php`

```php
<?php

namespace App\Providers;

use App\Models\User;
use App\Observers\UserObserver;
use Illuminate\Foundation\Support\Providers\EventServiceProvider as ServiceProvider;

class EventServiceProvider extends ServiceProvider
{
    protected $listen = [
        // ... otros eventos
    ];

    public function boot(): void
    {
        User::observe(UserObserver::class);
    }
}
```

**✅ Checklist**
- [ ] Observer creado (opcional)
- [ ] Observer registrado en `EventServiceProvider` (opcional)
- [ ] Crear customer automáticamente funciona (opcional)

---

## Paso 8: Testing

### 8.1 Testing Manual
```bash
# Crear usuario de prueba y autenticar
php artisan tinker

# En tinker:
$user = User::create([
    'name' => 'Test User',
    'email' => 'test@example.com',
    'password' => bcrypt('password')
]);

# Probar crear customer manualmente
$service = new \App\Services\StripeCustomerService();
$customerId = $service->createCustomer([
    'user_id' => $user->id,
    'name' => $user->name,
    'email' => $user->email
]);

echo $customerId; // Debería mostrar cus_xxx
```

### 8.2 Testing con API (Postman/Insomnia)
```http
### Crear Customer
POST http://tu-laravel-app.test/api/stripe/customer
Authorization: Bearer {tu_token}
Content-Type: application/json

{
  "name": "Juan Pérez",
  "email": "juan@example.com",
  "phone": "+52 55 1234 5678"
}

### Obtener Customer
GET http://tu-laravel-app.test/api/stripe/customer
Authorization: Bearer {tu_token}

### Actualizar Customer
PUT http://tu-laravel-app.test/api/stripe/customer
Authorization: Bearer {tu_token}
Content-Type: application/json

{
  "phone": "+52 55 9876 5432"
}

### Eliminar Customer
DELETE http://tu-laravel-app.test/api/stripe/customer
Authorization: Bearer {tu_token}
```

**✅ Checklist**
- [ ] Crear customer funciona
- [ ] Actualizar customer funciona
- [ ] Obtener customer funciona
- [ ] Eliminar customer funciona
- [ ] `stripe_customer_id` se guarda en BD
- [ ] Customer aparece en Stripe Dashboard

---

## 📊 Checklist Final de Implementación

- [ ] **Base de Datos**
  - [ ] Migración creada y ejecutada
  - [ ] Columna `stripe_customer_id` en tabla `users`
  
- [ ] **Modelo**
  - [ ] Campo agregado a `$fillable` en modelo `User`
  
- [ ] **Configuración**
  - [ ] Variable `ECOMMERCE_API_URL` en `.env`
  - [ ] Configuración en `config/services.php`
  
- [ ] **Servicios**
  - [ ] `StripeCustomerService` creado e implementado
  
- [ ] **Controlador**
  - [ ] `CustomerController` creado e implementado
  - [ ] Validaciones en su lugar
  
- [ ] **Rutas**
  - [ ] Rutas registradas en `routes/api.php`
  - [ ] Middleware de autenticación aplicado
  
- [ ] **Testing**
  - [ ] API de .NET funcionando
  - [ ] Crear customer funciona desde Laravel
  - [ ] Actualizar customer funciona
  - [ ] Obtener customer funciona
  - [ ] Eliminar customer funciona
  - [ ] Customer visible en Stripe Dashboard
  
- [ ] **Opcional**
  - [ ] Observer para auto-crear customer (si lo deseas)

---

## 🎯 Siguiente Paso

Una vez completado este checklist, tendrás:
- ✅ Integración completa con la API de .NET
- ✅ Gestión de Customers de Stripe desde Laravel
- ✅ Persistencia del `customer_id` en tu base de datos

**¡Listo para el siguiente requerimiento!** 🚀

---

## 📞 ¿Problemas?

Si encuentras algún error:
1. Revisa los logs de Laravel: `storage/logs/laravel.log`
2. Revisa que la API de .NET esté corriendo
3. Verifica las credenciales de Stripe
4. Asegúrate de que la URL de la API sea correcta

**Estado**: ✅ Listo para implementar
