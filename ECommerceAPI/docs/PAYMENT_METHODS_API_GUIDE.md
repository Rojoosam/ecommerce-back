# API de Gestión de Payment Methods de Stripe

## Descripción General

Esta API proporciona endpoints para gestionar **Payment Methods** (métodos de pago) en Stripe desde Laravel. Permite registrar tarjetas, asociarlas a Customers, consultar y eliminar métodos de pago.

---

## 🔑 Conceptos Importantes

### Payment Method vs Token

- **Token (tok_xxx)**: Token efímero generado en el frontend por Stripe.js. Es de un solo uso y expira rápidamente.
- **Payment Method (pm_xxx)**: Objeto persistente en Stripe que representa un método de pago (tarjeta).

### Estado del Customer

Los Customers pueden estar **activos** o **inactivos**:
- **Activo**: Puede agregar y usar métodos de pago
- **Inactivo**: No puede agregar nuevos métodos de pago. Al desactivar, opcionalmente se desasocian todos sus métodos de pago.

El estado se maneja mediante **metadata** del Customer (`"active": "true/false"`).

---

## Endpoints Disponibles

### 1. Registrar y Asociar Payment Method

**Endpoint:** `POST /api/paymentmethods/attach`

**Descripción:** Registra un nuevo Payment Method usando un token efímero del frontend y lo asocia al Customer.

**Request Body:**
```json
{
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "token": "tok_1PqRsT2aBcDeFgHi",
  "metadata": {
    "source": "web_checkout",
    "user_agent": "Mozilla/5.0..."
  }
}
```

**Campos Requeridos:**
- `customer_id` (string): ID del Customer en Stripe (cus_xxx)
- `token` (string): Token efímero (tok_xxx) o Payment Method ID (pm_xxx)

**Campos Opcionales:**
- `metadata` (dictionary): Metadatos adicionales

**Response (200 OK):**
```json
{
  "success": true,
  "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "type": "card",
  "card": {
    "brand": "visa",
    "last4": "4242",
    "exp_month": 12,
    "exp_year": 2025,
    "country": "US",
    "funding": "credit",
    "cardholder_name": null
  },
  "created": "2024-01-15T10:30:00Z",
  "metadata": {
    "source": "web_checkout"
  },
  "error_message": null
}
```

**Response (400 Bad Request) - Customer Inactivo:**
```json
{
  "success": false,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "error_message": "El Customer está inactivo. No se pueden agregar métodos de pago."
}
```

**Response (500 Internal Server Error):**
```json
{
  "success": false,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "error_message": "Error de Stripe: This customer has no payment method with ID pm_xxx"
}
```

---

### 2. Desasociar Payment Method

**Endpoint:** `POST /api/paymentmethods/detach`

**Descripción:** Desasocia (elimina) un Payment Method de un Customer.

**Request Body:**
```json
{
  "payment_method_id": "pm_1PqRsT2aBcDeFgHi"
}
```

**Campos Requeridos:**
- `payment_method_id` (string): ID del Payment Method en Stripe (pm_xxx)

**Response (200 OK):**
```json
{
  "success": true,
  "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
  "message": "Payment Method desasociado exitosamente",
  "error_message": null
}
```

**Response (500 Internal Server Error):**
```json
{
  "success": false,
  "payment_method_id": "pm_invalid",
  "message": null,
  "error_message": "Error de Stripe: No such payment_method: 'pm_invalid'"
}
```

---

### 3. Obtener Detalles de Payment Method

**Endpoint:** `GET /api/paymentmethods/{paymentMethodId}`

**Descripción:** Obtiene los detalles de un Payment Method específico.

**Parámetros de URL:**
- `paymentMethodId` (string): ID del Payment Method en Stripe (pm_xxx)

**Ejemplo:** `GET /api/paymentmethods/pm_1PqRsT2aBcDeFgHi`

**Response (200 OK):**
```json
{
  "success": true,
  "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "type": "card",
  "card": {
    "brand": "visa",
    "last4": "4242",
    "exp_month": 12,
    "exp_year": 2025,
    "country": "US",
    "funding": "credit"
  },
  "created": "2024-01-15T10:30:00Z",
  "metadata": {},
  "error_message": null
}
```

**Response (404 Not Found):**
```json
{
  "success": false,
  "payment_method_id": "pm_invalid",
  "error_message": "Error de Stripe: No such payment_method: 'pm_invalid'"
}
```

---

### 4. Listar Payment Methods de un Customer

**Endpoint:** `GET /api/paymentmethods/customer/{customerId}?type=card`

**Descripción:** Lista todos los Payment Methods asociados a un Customer.

**Parámetros de URL:**
- `customerId` (string): ID del Customer en Stripe (cus_xxx)

**Parámetros Query:**
- `type` (string, opcional): Tipo de método de pago (default: "card")

**Ejemplo:** `GET /api/paymentmethods/customer/cus_PQx1yZ2aBcDeFgHi`

**Response (200 OK):**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "payment_methods": [
    {
      "success": true,
      "payment_method_id": "pm_1PqRsT2aBcDeFgHi",
      "customer_id": "cus_PQx1yZ2aBcDeFgHi",
      "type": "card",
      "card": {
        "brand": "visa",
        "last4": "4242",
        "exp_month": 12,
        "exp_year": 2025,
        "country": "US",
        "funding": "credit"
      },
      "created": "2024-01-15T10:30:00Z",
      "metadata": {}
    },
    {
      "success": true,
      "payment_method_id": "pm_2AbCdE3fGhIjKl",
      "customer_id": "cus_PQx1yZ2aBcDeFgHi",
      "type": "card",
      "card": {
        "brand": "mastercard",
        "last4": "5555",
        "exp_month": 6,
        "exp_year": 2026,
        "country": "MX",
        "funding": "debit"
      },
      "created": "2024-01-20T14:45:00Z",
      "metadata": {}
    }
  ],
  "count": 2,
  "error_message": null
}
```

**Response (200 OK) - Sin Payment Methods:**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "payment_methods": [],
  "count": 0,
  "error_message": null
}
```

---

### 5. Actualizar Estado del Customer (Activar/Desactivar)

**Endpoint:** `PUT /api/paymentmethods/customer/status`

**Descripción:** Activa o desactiva un Customer. Al desactivar, opcionalmente desasocia todos sus Payment Methods.

**Request Body:**
```json
{
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "active": false,
  "detach_payment_methods": true
}
```

**Campos Requeridos:**
- `customer_id` (string): ID del Customer en Stripe (cus_xxx)
- `active` (boolean): True para activar, False para desactivar

**Campos Opcionales:**
- `detach_payment_methods` (boolean, default: true): Si es false y el customer se desactiva, también desasociar todos sus Payment Methods

**Response (200 OK) - Desactivar:**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "active": false,
  "payment_methods_detached": 2,
  "message": "Customer desactivado exitosamente. 2 Payment Methods desasociados.",
  "error_message": null
}
```

**Response (200 OK) - Activar:**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "active": true,
  "payment_methods_detached": 0,
  "message": "Customer activado exitosamente",
  "error_message": null
}
```

---

## Integración con Laravel

### Paso 1: Generar Token en el Frontend

**En el frontend (JavaScript con Stripe.js):**
```javascript
// Inicializar Stripe
const stripe = Stripe('pk_test_...');
const elements = stripe.elements();
const cardElement = elements.create('card');
cardElement.mount('#card-element');

// Al enviar el formulario
async function handleSubmit(event) {
  event.preventDefault();
  
  const {token, error} = await stripe.createToken(cardElement);
  
  if (error) {
    console.error(error);
  } else {
    // Enviar token.id al backend de Laravel
    await fetch('/api/payment-methods/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + authToken
      },
      body: JSON.stringify({
        token: token.id  // tok_xxx
      })
    });
  }
}
```

### Paso 2: Controlador Laravel para Registrar Payment Method

```php
<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Log;

class PaymentMethodController extends Controller
{
    private string $apiBaseUrl;

    public function __construct()
    {
        $this->apiBaseUrl = config('services.ecommerce_api.base_url') . '/api/paymentmethods';
    }

    /**
     * Registrar un nuevo Payment Method
     */
    public function register(Request $request)
    {
        $validated = $request->validate([
            'token' => 'required|string|starts_with:tok_,pm_'
        ]);

        $customerId = auth()->user()->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'success' => false,
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        // Llamar a la API de .NET
        $response = Http::post("{$this->apiBaseUrl}/attach", [
            'customer_id' => $customerId,
            'token' => $validated['token']
        ]);

        $data = $response->json();

        if ($data['success'] ?? false) {
            // Opcional: Guardar payment_method_id en BD
            // auth()->user()->paymentMethods()->create([
            //     'stripe_payment_method_id' => $data['payment_method_id'],
            //     'brand' => $data['card']['brand'],
            //     'last4' => $data['card']['last4'],
            //     'exp_month' => $data['card']['exp_month'],
            //     'exp_year' => $data['card']['exp_year']
            // ]);

            return response()->json([
                'success' => true,
                'message' => 'Método de pago registrado exitosamente',
                'payment_method' => $data
            ], 200);
        }

        Log::error('Error al registrar Payment Method', [
            'error' => $data['error_message'] ?? 'Unknown error'
        ]);

        return response()->json([
            'success' => false,
            'message' => 'Error al registrar método de pago',
            'error' => $data['error_message'] ?? 'Unknown error'
        ], 500);
    }

    /**
     * Listar Payment Methods del usuario
     */
    public function index()
    {
        $customerId = auth()->user()->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'success' => false,
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        $response = Http::get("{$this->apiBaseUrl}/customer/{$customerId}");
        $data = $response->json();

        if ($data['success'] ?? false) {
            return response()->json($data, 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al obtener métodos de pago'
        ], 500);
    }

    /**
     * Eliminar (desasociar) un Payment Method
     */
    public function destroy(Request $request)
    {
        $validated = $request->validate([
            'payment_method_id' => 'required|string|starts_with:pm_'
        ]);

        $response = Http::post("{$this->apiBaseUrl}/detach", [
            'payment_method_id' => $validated['payment_method_id']
        ]);

        $data = $response->json();

        if ($data['success'] ?? false) {
            return response()->json([
                'success' => true,
                'message' => 'Método de pago eliminado exitosamente'
            ], 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al eliminar método de pago',
            'error' => $data['error_message'] ?? 'Unknown error'
        ], 500);
    }

    /**
     * Desactivar usuario (y sus métodos de pago)
     */
    public function deactivateUser()
    {
        $customerId = auth()->user()->stripe_customer_id;

        if (!$customerId) {
            return response()->json([
                'success' => false,
                'message' => 'El usuario no tiene un customer asociado en Stripe'
            ], 404);
        }

        $response = Http::put("{$this->apiBaseUrl}/customer/status", [
            'customer_id' => $customerId,
            'active' => false,
            'detach_payment_methods' => true
        ]);

        $data = $response->json();

        if ($data['success'] ?? false) {
            // Marcar usuario como inactivo en tu BD
            auth()->user()->update([
                'is_active' => false
            ]);

            return response()->json([
                'success' => true,
                'message' => 'Usuario desactivado exitosamente',
                'payment_methods_removed' => $data['payment_methods_detached']
            ], 200);
        }

        return response()->json([
            'success' => false,
            'message' => 'Error al desactivar usuario'
        ], 500);
    }
}
```

### Paso 3: Registrar Rutas en Laravel

**`routes/api.php`:**
```php
use App\Http\Controllers\Api\PaymentMethodController;

Route::middleware('auth:sanctum')->prefix('payment-methods')->group(function () {
    Route::post('/register', [PaymentMethodController::class, 'register']);
    Route::get('/', [PaymentMethodController::class, 'index']);
    Route::delete('/', [PaymentMethodController::class, 'destroy']);
    Route::post('/deactivate-user', [PaymentMethodController::class, 'deactivateUser']);
});
```

---

## Notas Importantes

### 1. Seguridad de Tokens

- **NUNCA** envíes datos de tarjeta desde el backend
- Los tokens deben generarse en el frontend con Stripe.js
- Los tokens son efímeros y de un solo uso

### 2. No se Pueden Editar Payment Methods

Stripe **NO permite editar** los datos de un Payment Method existente. Si los datos cambian (nueva tarjeta, nueva expiración, etc.), debes:
1. Crear un nuevo Payment Method
2. Desasociar el antiguo (opcional)

### 3. Estado del Customer

- Customers nuevos se crean como **activos** por defecto
- Customers inactivos **NO pueden agregar** nuevos Payment Methods
- Al desactivar, puedes optar por mantener o eliminar los Payment Methods existentes

### 4. Testing con Tarjetas de Prueba

Usa estas tarjetas de prueba de Stripe:

| Tarjeta | Número | Comportamiento |
|---------|--------|----------------|
| Visa | 4242 4242 4242 4242 | Siempre exitosa |
| Visa (debit) | 4000 0566 5566 5556 | Siempre exitosa |
| Mastercard | 5555 5555 5555 4444 | Siempre exitosa |
| Amex | 3782 822463 10005 | Siempre exitosa |
| Visa (declined) | 4000 0000 0000 0002 | Siempre rechazada |

**Fecha de expiración:** Cualquier fecha futura  
**CVC:** Cualquier 3-4 dígitos

---

## Flujo Completo

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Usuario ingresa datos de tarjeta en el frontend         │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Stripe.js genera token (tok_xxx)                        │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Laravel envía token a .NET API                          │
│    POST /api/paymentmethods/attach                         │
│    { customer_id, token }                                   │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. .NET API crea Payment Method en Stripe                  │
│    - Verifica que customer esté activo                     │
│    - Crea Payment Method desde token                       │
│    - Asocia al Customer                                     │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. .NET devuelve payment_method_id + datos públicos        │
│    { success, payment_method_id, card: {brand, last4...} } │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Laravel (opcional) guarda datos en BD                   │
└─────────────────────────────────────────────────────────────┘
```

---

## Testing con cURL

### Registrar Payment Method
```bash
curl -X POST "https://tu-api-dotnet.com/api/paymentmethods/attach" \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "token": "tok_visa"
  }'
```

### Listar Payment Methods
```bash
curl -X GET "https://tu-api-dotnet.com/api/paymentmethods/customer/cus_PQx1yZ2aBcDeFgHi"
```

### Desasociar Payment Method
```bash
curl -X POST "https://tu-api-dotnet.com/api/paymentmethods/detach" \
  -H "Content-Type: application/json" \
  -d '{
    "payment_method_id": "pm_1PqRsT2aBcDeFgHi"
  }'
```

### Desactivar Customer
```bash
curl -X PUT "https://tu-api-dotnet.com/api/paymentmethods/customer/status" \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "active": false,
    "detach_payment_methods": true
  }'
```

---

## Soporte

Para más información:
- **Stripe Payment Methods**: https://stripe.com/docs/api/payment_methods
- **Stripe.js**: https://stripe.com/docs/js
- **Testing**: https://stripe.com/docs/testing

---

**¡API lista para usar!** 🚀
