# API de Gestión de Customers de Stripe

## Descripción General

Esta API proporciona endpoints para gestionar **Customers** en Stripe desde Laravel. Permite crear, actualizar, consultar y eliminar clientes en Stripe, manteniendo la referencia con el `user_id` de Laravel.

---

## Endpoints Disponibles

### 1. Crear Customer

**Endpoint:** `POST /api/customers`

**Descripción:** Crea un nuevo Customer en Stripe a partir de los datos básicos del usuario.

**Request Body:**
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
  },
  "metadata": {
    "source": "laravel_app",
    "registration_date": "2024-01-15"
  }
}
```

**Campos Requeridos:**
- `user_id` (string): ID interno del usuario en Laravel
- `name` (string): Nombre completo del cliente
- `email` (string): Correo electrónico del cliente

**Campos Opcionales:**
- `phone` (string): Teléfono del cliente
- `address` (object): Dirección del cliente
  - `line1` (string): Línea 1 de la dirección
  - `line2` (string): Línea 2 de la dirección (opcional)
  - `city` (string): Ciudad
  - `state` (string): Estado/Provincia
  - `postal_code` (string): Código postal
  - `country` (string): Código de país (ISO 3166-1 alpha-2)
- `metadata` (dictionary): Metadatos adicionales (clave-valor)

**Response (200 OK):**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
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
  },
  "created": "2024-01-15T10:30:00Z",
  "is_deleted": false,
  "metadata": {
    "user_id": "123",
    "source": "laravel_app",
    "registration_date": "2024-01-15"
  },
  "error_message": null
}
```

**Response (400 Bad Request):**
```json
{
  "success": false,
  "customer_id": null,
  "user_id": null,
  "error_message": "El campo 'email' es requerido"
}
```

**Response (500 Internal Server Error):**
```json
{
  "success": false,
  "customer_id": null,
  "user_id": "123",
  "error_message": "Error de Stripe: Invalid email address"
}
```

---

### 2. Actualizar Customer

**Endpoint:** `PUT /api/customers`

**Descripción:** Actualiza los datos de un Customer existente en Stripe.

**Request Body:**
```json
{
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "name": "Juan Antonio Pérez",
  "email": "juan.antonio@example.com",
  "phone": "+52 55 9876 5432",
  "address": {
    "line1": "Paseo de la Reforma 500",
    "line2": "Piso 10",
    "city": "Ciudad de México",
    "state": "CDMX",
    "postal_code": "06600",
    "country": "MX"
  },
  "metadata": {
    "updated_from": "laravel_admin_panel"
  }
}
```

**Campos Requeridos:**
- `customer_id` (string): ID del Customer en Stripe (formato: `cus_xxx`)

**Campos Opcionales (se actualizan solo los que se envíen):**
- `name` (string): Nombre completo del cliente
- `email` (string): Correo electrónico del cliente
- `phone` (string): Teléfono del cliente
- `address` (object): Dirección del cliente
- `metadata` (dictionary): Metadatos adicionales

**Response (200 OK):**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "user_id": "123",
  "name": "Juan Antonio Pérez",
  "email": "juan.antonio@example.com",
  "phone": "+52 55 9876 5432",
  "address": {
    "line1": "Paseo de la Reforma 500",
    "line2": "Piso 10",
    "city": "Ciudad de México",
    "state": "CDMX",
    "postal_code": "06600",
    "country": "MX"
  },
  "created": "2024-01-15T10:30:00Z",
  "is_deleted": false,
  "metadata": {
    "user_id": "123",
    "updated_from": "laravel_admin_panel"
  },
  "error_message": null
}
```

---

### 3. Obtener Customer

**Endpoint:** `GET /api/customers/{customerId}`

**Descripción:** Obtiene los detalles de un Customer desde Stripe.

**Parámetros de URL:**
- `customerId` (string): ID del Customer en Stripe (formato: `cus_xxx`)

**Ejemplo:** `GET /api/customers/cus_PQx1yZ2aBcDeFgHi`

**Response (200 OK):**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "name": "Juan Antonio Pérez",
  "email": "juan.antonio@example.com",
  "phone": "+52 55 9876 5432",
  "address": {
    "line1": "Paseo de la Reforma 500",
    "line2": "Piso 10",
    "city": "Ciudad de México",
    "state": "CDMX",
    "postal_code": "06600",
    "country": "MX"
  },
  "created": "2024-01-15T10:30:00Z",
  "balance": 0,
  "currency": "mxn",
  "is_deleted": false,
  "metadata": {
    "user_id": "123"
  },
  "error_message": null
}
```

**Response (404 Not Found):**
```json
{
  "success": false,
  "customer_id": "cus_InvalidId",
  "error_message": "Error de Stripe: No such customer: 'cus_InvalidId'"
}
```

---

### 4. Eliminar Customer

**Endpoint:** `DELETE /api/customers/{customerId}`

**Descripción:** Elimina un Customer en Stripe. Esta operación es permanente.

**Parámetros de URL:**
- `customerId` (string): ID del Customer en Stripe (formato: `cus_xxx`)

**Ejemplo:** `DELETE /api/customers/cus_PQx1yZ2aBcDeFgHi`

**Response (200 OK):**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "deleted": true,
  "message": "Customer eliminado exitosamente en Stripe",
  "error_message": null
}
```

**Response (404 Not Found):**
```json
{
  "success": false,
  "customer_id": "cus_InvalidId",
  "deleted": false,
  "message": null,
  "error_message": "Error de Stripe: No such customer: 'cus_InvalidId'"
}
```

---

## Integración con Laravel

### Ejemplo de uso en Laravel (Controlador)

```php
<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Http;

class CustomerController extends Controller
{
    private $apiBaseUrl = 'https://tu-api-dotnet.com/api';

    /**
     * Crear un customer en Stripe cuando se registra un nuevo usuario
     */
    public function createCustomer(Request $request)
    {
        $validated = $request->validate([
            'name' => 'required|string|max:255',
            'email' => 'required|email',
            'phone' => 'nullable|string',
            'address' => 'nullable|array'
        ]);

        $response = Http::post("{$this->apiBaseUrl}/customers", [
            'user_id' => auth()->id(),
            'name' => $validated['name'],
            'email' => $validated['email'],
            'phone' => $validated['phone'] ?? null,
            'address' => $validated['address'] ?? null,
        ]);

        $data = $response->json();

        if ($data['success']) {
            // Guardar el customer_id en la base de datos del usuario
            auth()->user()->update([
                'stripe_customer_id' => $data['customer_id']
            ]);

            return response()->json([
                'message' => 'Customer creado exitosamente',
                'customer_id' => $data['customer_id']
            ], 200);
        }

        return response()->json([
            'message' => 'Error al crear customer',
            'error' => $data['error_message']
        ], 500);
    }

    /**
     * Actualizar datos del customer en Stripe
     */
    public function updateCustomer(Request $request)
    {
        $validated = $request->validate([
            'name' => 'nullable|string|max:255',
            'email' => 'nullable|email',
            'phone' => 'nullable|string',
            'address' => 'nullable|array'
        ]);

        $customerId = auth()->user()->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        $response = Http::put("{$this->apiBaseUrl}/customers", [
            'customer_id' => $customerId,
            'name' => $validated['name'] ?? null,
            'email' => $validated['email'] ?? null,
            'phone' => $validated['phone'] ?? null,
            'address' => $validated['address'] ?? null,
        ]);

        $data = $response->json();

        if ($data['success']) {
            return response()->json([
                'message' => 'Customer actualizado exitosamente',
                'customer' => $data
            ], 200);
        }

        return response()->json([
            'message' => 'Error al actualizar customer',
            'error' => $data['error_message']
        ], 500);
    }

    /**
     * Obtener datos del customer desde Stripe
     */
    public function getCustomer()
    {
        $customerId = auth()->user()->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        $response = Http::get("{$this->apiBaseUrl}/customers/{$customerId}");
        $data = $response->json();

        if ($data['success']) {
            return response()->json($data, 200);
        }

        return response()->json([
            'message' => 'Error al obtener customer',
            'error' => $data['error_message']
        ], 500);
    }

    /**
     * Eliminar customer de Stripe
     */
    public function deleteCustomer()
    {
        $customerId = auth()->user()->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        $response = Http::delete("{$this->apiBaseUrl}/customers/{$customerId}");
        $data = $response->json();

        if ($data['success']) {
            // Limpiar el customer_id de la base de datos
            auth()->user()->update([
                'stripe_customer_id' => null
            ]);

            return response()->json([
                'message' => 'Customer eliminado exitosamente'
            ], 200);
        }

        return response()->json([
            'message' => 'Error al eliminar customer',
            'error' => $data['error_message']
        ], 500);
    }
}
```

### Ejemplo de migración para agregar stripe_customer_id

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

---

## Notas Importantes

1. **Metadata**: El `user_id` de Laravel se guarda automáticamente en el metadata del Customer en Stripe para mantener la referencia bidireccional.

2. **Validación de IDs**: Los endpoints validan que el `customer_id` tenga el formato correcto de Stripe (`cus_xxx`).

3. **Actualización Parcial**: En el endpoint de actualización, solo se modifican los campos que se envían en el request. Los campos omitidos mantienen su valor actual.

4. **Eliminación**: La eliminación de un Customer en Stripe es permanente. Asegúrate de que no haya subscripciones activas antes de eliminar.

5. **Manejo de Errores**: Todos los endpoints devuelven información detallada de errores en el campo `error_message` cuando `success` es `false`.

6. **CORS**: Asegúrate de configurar CORS en la API de .NET si Laravel hace llamadas desde un dominio diferente.

---

## Testing con Postman/cURL

### Crear Customer
```bash
curl -X POST https://tu-api-dotnet.com/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "123",
    "name": "Juan Pérez",
    "email": "juan.perez@example.com",
    "phone": "+52 55 1234 5678"
  }'
```

### Actualizar Customer
```bash
curl -X PUT https://tu-api-dotnet.com/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "name": "Juan Antonio Pérez"
  }'
```

### Obtener Customer
```bash
curl -X GET https://tu-api-dotnet.com/api/customers/cus_PQx1yZ2aBcDeFgHi
```

### Eliminar Customer
```bash
curl -X DELETE https://tu-api-dotnet.com/api/customers/cus_PQx1yZ2aBcDeFgHi
```

---

## Configuración Requerida

Asegúrate de tener configuradas las claves de Stripe en `appsettings.json`:

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

## Soporte

Para más información sobre la API de Stripe Customers, consulta la [documentación oficial de Stripe](https://stripe.com/docs/api/customers).
