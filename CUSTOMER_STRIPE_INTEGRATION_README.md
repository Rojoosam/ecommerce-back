# Implementación de Gestión de Customers de Stripe - Integración con Laravel

## ✅ Implementación Completada

Se ha implementado exitosamente la funcionalidad completa de gestión de Customers de Stripe en la API de .NET, lista para ser consumida por Laravel.

---

## 📋 Características Implementadas

### ✅ **1. Crear Customer en Stripe**
- Crea un Customer en Stripe con datos básicos del usuario
- Devuelve `customer_id` (cus_xxx) y el `user_id` de Laravel
- Almacena el `user_id` de Laravel en el metadata del Customer para referencia bidireccional
- Soporta datos opcionales: teléfono, dirección y metadata personalizada

### ✅ **2. Actualizar Customer en Stripe**
- Permite actualizar nombre, correo, teléfono y dirección
- Actualización parcial: solo se modifican los campos enviados
- Mantiene los valores existentes para campos no enviados

### ✅ **3. Obtener Customer desde Stripe**
- Consulta y devuelve todos los detalles de un Customer
- Incluye balance, fecha de creación y metadata

### ✅ **4. Eliminar Customer en Stripe**
- Eliminación permanente del Customer en Stripe
- Validación de existencia antes de eliminar

---

## 🚀 Endpoints Disponibles

### Base URL
```
https://tu-api-dotnet.com/api/customers
```

### 1. **POST** `/api/customers` - Crear Customer
```json
{
  "user_id": "123",
  "name": "Juan Pérez",
  "email": "juan.perez@example.com",
  "phone": "+52 55 1234 5678",
  "address": {
    "line1": "Av. Insurgentes Sur 1234",
    "line2": "Col. Del Valle",
    "city": "Ciudad de México",
    "state": "CDMX",
    "postal_code": "03100",
    "country": "MX"
  }
}
```

**Respuesta:**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "user_id": "123",
  "name": "Juan Pérez",
  "email": "juan.perez@example.com",
  "phone": "+52 55 1234 5678",
  "address": { ... },
  "created": "2024-01-15T10:30:00Z",
  "is_deleted": false,
  "metadata": {
    "user_id": "123"
  }
}
```

### 2. **PUT** `/api/customers` - Actualizar Customer
```json
{
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "name": "Juan Antonio Pérez",
  "email": "juan.antonio@example.com"
}
```

### 3. **GET** `/api/customers/{customerId}` - Obtener Customer
```
GET /api/customers/cus_PQx1yZ2aBcDeFgHi
```

### 4. **DELETE** `/api/customers/{customerId}` - Eliminar Customer
```
DELETE /api/customers/cus_PQx1yZ2aBcDeFgHi
```

---

## 📁 Archivos Creados/Modificados

### ✅ **Nuevos Archivos:**
1. `ECommerceAPI\Models\CustomerModels.cs` - Modelos para Customer (Request/Response)
2. `ECommerceAPI\Services\IStripeCustomerService.cs` - Interfaz del servicio
3. `ECommerceAPI\Services\StripeCustomerService.cs` - Implementación del servicio
4. `ECommerceAPI\Controllers\CustomersController.cs` - Controlador de API
5. `ECommerceAPI\docs\CUSTOMERS_API_GUIDE.md` - Documentación completa

### ✅ **Archivos Modificados:**
1. `ECommerceAPI\Program.cs` - Registro del servicio en DI container

---

## 🔧 Configuración Requerida

### En `appsettings.json` (ya configurado):
```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

---

## 💻 Implementación en Laravel

### Paso 1: Agregar Campo en la Tabla de Usuarios

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

Ejecutar migración:
```bash
php artisan migrate
```

### Paso 2: Actualizar el Modelo User

**`app/Models/User.php`:**
```php
<?php

namespace App\Models;

use Illuminate\Foundation\Auth\User as Authenticatable;

class User extends Authenticatable
{
    protected $fillable = [
        'name',
        'email',
        'password',
        'stripe_customer_id', // Agregar esto
    ];

    // ... resto del código
}
```

### Paso 3: Crear Servicio de Customer en Laravel

**`app/Services/StripeCustomerService.php`:**
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
                'user_id' => $data['user_id'],
                'name' => $data['name'],
                'email' => $data['email'],
                'phone' => $data['phone'] ?? null,
                'address' => $data['address'] ?? null,
            ]);

            $result = $response->json();

            if ($result['success'] ?? false) {
                return $result['customer_id'];
            }

            Log::error('Error al crear customer en Stripe', [
                'error' => $result['error_message'] ?? 'Unknown error'
            ]);

            return null;
        } catch (\Exception $e) {
            Log::error('Excepción al crear customer en Stripe', [
                'error' => $e->getMessage()
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
            ]);

            $response = Http::put($this->apiBaseUrl, $payload);
            $result = $response->json();

            return $result['success'] ?? false;
        } catch (\Exception $e) {
            Log::error('Excepción al actualizar customer en Stripe', [
                'error' => $e->getMessage()
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

            return null;
        } catch (\Exception $e) {
            Log::error('Excepción al obtener customer desde Stripe', [
                'error' => $e->getMessage()
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

            return $result['success'] ?? false;
        } catch (\Exception $e) {
            Log::error('Excepción al eliminar customer de Stripe', [
                'error' => $e->getMessage()
            ]);
            return false;
        }
    }
}
```

### Paso 4: Configurar URL de la API

**`config/services.php`:**
```php
return [
    // ... otras configuraciones

    'ecommerce_api' => [
        'base_url' => env('ECOMMERCE_API_URL', 'https://tu-api-dotnet.com'),
    ],
];
```

**`.env`:**
```env
ECOMMERCE_API_URL=https://tu-api-dotnet.com
```

### Paso 5: Crear Controlador para Gestión de Customers

**`app/Http/Controllers/CustomerController.php`:**
```php
<?php

namespace App\Http\Controllers;

use App\Services\StripeCustomerService;
use Illuminate\Http\Request;

class CustomerController extends Controller
{
    private StripeCustomerService $customerService;

    public function __construct(StripeCustomerService $customerService)
    {
        $this->customerService = $customerService;
    }

    /**
     * Crear customer al registrar usuario
     */
    public function createCustomer(Request $request)
    {
        $validated = $request->validate([
            'name' => 'required|string|max:255',
            'email' => 'required|email',
            'phone' => 'nullable|string',
            'address' => 'nullable|array',
            'address.line1' => 'nullable|string',
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
            // Guardar en la base de datos
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
     * Actualizar customer
     */
    public function updateCustomer(Request $request)
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
     * Obtener customer
     */
    public function getCustomer()
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
     * Eliminar customer
     */
    public function deleteCustomer()
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
            // Limpiar el customer_id de la base de datos
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

### Paso 6: Registrar Rutas

**`routes/api.php`:**
```php
use App\Http\Controllers\CustomerController;

Route::middleware('auth:sanctum')->group(function () {
    Route::post('/customers', [CustomerController::class, 'createCustomer']);
    Route::put('/customers', [CustomerController::class, 'updateCustomer']);
    Route::get('/customers', [CustomerController::class, 'getCustomer']);
    Route::delete('/customers', [CustomerController::class, 'deleteCustomer']);
});
```

### Paso 7: Automáticamente Crear Customer al Registrar Usuario

**`app/Listeners/CreateStripeCustomer.php`:**
```php
<?php

namespace App\Listeners;

use App\Events\UserRegistered;
use App\Services\StripeCustomerService;
use Illuminate\Support\Facades\Log;

class CreateStripeCustomer
{
    private StripeCustomerService $customerService;

    public function __construct(StripeCustomerService $customerService)
    {
        $this->customerService = $customerService;
    }

    public function handle(UserRegistered $event)
    {
        $user = $event->user;

        $customerId = $this->customerService->createCustomer([
            'user_id' => $user->id,
            'name' => $user->name,
            'email' => $user->email,
        ]);

        if ($customerId) {
            $user->update([
                'stripe_customer_id' => $customerId
            ]);

            Log::info("Customer de Stripe creado para usuario {$user->id}: {$customerId}");
        } else {
            Log::error("Error al crear Customer de Stripe para usuario {$user->id}");
        }
    }
}
```

---

## 🧪 Testing

### Desde Laravel con cURL:
```bash
# Crear customer
curl -X POST http://tu-laravel-app.com/api/customers \
  -H "Authorization: Bearer tu_token" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Juan Pérez",
    "email": "juan.perez@example.com",
    "phone": "+52 55 1234 5678"
  }'

# Actualizar customer
curl -X PUT http://tu-laravel-app.com/api/customers \
  -H "Authorization: Bearer tu_token" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Juan Antonio Pérez"
  }'

# Obtener customer
curl -X GET http://tu-laravel-app.com/api/customers \
  -H "Authorization: Bearer tu_token"

# Eliminar customer
curl -X DELETE http://tu-laravel-app.com/api/customers \
  -H "Authorization: Bearer tu_token"
```

### Directamente a la API de .NET:
```bash
curl -X POST https://tu-api-dotnet.com/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "123",
    "name": "Juan Pérez",
    "email": "juan.perez@example.com"
  }'
```

---

## 📚 Documentación Adicional

Para más detalles, consulta:
- `ECommerceAPI\docs\CUSTOMERS_API_GUIDE.md` - Guía completa de la API
- Documentación Swagger: `https://tu-api-dotnet.com/swagger`

---

## ✅ Checklist de Implementación

- [x] Crear modelos de Customer (Request/Response)
- [x] Implementar servicio de Stripe Customer
- [x] Crear controlador de API
- [x] Registrar servicios en Program.cs
- [x] Validación de datos en endpoints
- [x] Manejo de errores completo
- [x] Logging detallado
- [x] Documentación de API
- [x] Ejemplos de integración con Laravel

---

## 🎯 Próximos Pasos

Este es el **primer requerimiento completado**. Los siguientes pasos para la app web son:

1. **Payment Methods** - Gestión de métodos de pago del customer
2. **Payment Intents** - Procesamiento de pagos asociados al customer
3. **Subscriptions** - Gestión de suscripciones (si aplica)
4. **Invoices** - Facturación (si aplica)

---

## 🆘 Soporte

Si tienes algún problema o pregunta:
1. Revisa los logs en la API de .NET
2. Revisa los logs en Laravel (`storage/logs/laravel.log`)
3. Verifica las credenciales de Stripe en `appsettings.json`
4. Asegúrate de que la URL de la API esté configurada correctamente en Laravel

---

**¡Listo para usar!** 🚀
