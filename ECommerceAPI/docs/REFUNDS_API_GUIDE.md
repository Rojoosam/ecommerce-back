# 🔄 Refunds API - Guía de Implementación

## 📋 Descripción General

API para gestionar **reembolsos (Refunds)** en Stripe. Permite crear reembolsos totales o parciales asociados a Payment Intents o Charges, consultar el estado de reembolsos existentes y listar todos los reembolsos.

---

## 🎯 Características Principales

✅ **Crear reembolsos** desde Payment Intent o Charge  
✅ **Reembolsos parciales o totales**  
✅ **Consultar estado** de reembolsos existentes  
✅ **Listar reembolsos** con paginación  
✅ **Validación** de IDs y montos  
✅ **Logging** completo de operaciones  

---

## 🔌 Endpoints Disponibles

### 1️⃣ Crear Reembolso desde Payment Intent

**POST** `/api/refunds/payment-intent/{paymentIntentId}`

Crea un reembolso asociado a un Payment Intent existente.

**Parámetros de URL:**
- `paymentIntentId` (string, required): ID del Payment Intent (formato: `pi_xxx`)

**Body (JSON):**
```json
{
  "amount": 50.00,  // Opcional: monto a reembolsar (parcial). Si se omite, reembolsa el total
  "reason": "requested_by_customer"  // Opcional: razón del reembolso
}
```

**Respuesta Exitosa (200):**
```json
{
  "refundId": "re_1PQRSTuvwxyz123456",
  "originalTransactionId": "pi_1ABCDEfghijk789012",
  "status": "Refunded",
  "amount": 50.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Ejemplo cURL:**
```bash
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABCDEfghijk789012" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 50.00,
    "reason": "Cliente solicitó reembolso"
  }'
```

---

### 2️⃣ Crear Reembolso desde Charge

**POST** `/api/refunds/charge/{chargeId}`

Crea un reembolso asociado a un Charge existente.

**Parámetros de URL:**
- `chargeId` (string, required): ID del Charge (formato: `ch_xxx`)

**Body (JSON):**
```json
{
  "amount": 25.50,  // Opcional: monto a reembolsar (parcial)
  "reason": "duplicate"  // Opcional: razón del reembolso
}
```

**Respuesta Exitosa (200):**
```json
{
  "refundId": "re_1XYZABCdefghi456789",
  "originalTransactionId": "ch_1MNOPQrstuvw345678",
  "status": "Refunded",
  "amount": 25.50,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T11:00:00Z"
}
```

**Ejemplo cURL:**
```bash
curl -X POST "https://localhost:7001/api/refunds/charge/ch_1MNOPQrstuvw345678" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 25.50,
    "reason": "duplicate"
  }'
```

---

### 3️⃣ Obtener Información de un Reembolso

**GET** `/api/refunds/{refundId}`

Consulta el estado y detalles de un reembolso existente.

**Parámetros de URL:**
- `refundId` (string, required): ID del reembolso (formato: `re_xxx`)

**Respuesta Exitosa (200):**
```json
{
  "refundId": "re_1PQRSTuvwxyz123456",
  "originalTransactionId": "pi_1ABCDEfghijk789012",
  "status": "Refunded",
  "amount": 50.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Ejemplo cURL:**
```bash
curl -X GET "https://localhost:7001/api/refunds/re_1PQRSTuvwxyz123456"
```

---

### 4️⃣ Listar Todos los Reembolsos

**GET** `/api/refunds?limit=10`

Lista todos los reembolsos con paginación.

**Parámetros de Query:**
- `limit` (int, optional): Número máximo de resultados (default: 10, max: 100)

**Respuesta Exitosa (200):**
```json
[
  {
    "refundId": "re_1PQRSTuvwxyz123456",
    "originalTransactionId": "pi_1ABCDEfghijk789012",
    "status": "Refunded",
    "amount": 50.00,
    "currency": "USD",
    "message": "Reembolso procesado exitosamente",
    "timestamp": "2024-01-15T10:30:00Z"
  },
  {
    "refundId": "re_1XYZABCdefghi456789",
    "originalTransactionId": "ch_1MNOPQrstuvw345678",
    "status": "Refunded",
    "amount": 25.50,
    "currency": "USD",
    "message": "Reembolso procesado exitosamente",
    "timestamp": "2024-01-15T09:00:00Z"
  }
]
```

**Ejemplo cURL:**
```bash
curl -X GET "https://localhost:7001/api/refunds?limit=20"
```

---

## 📊 Modelos de Datos

### RefundRequest
```csharp
public class RefundRequest
{
    [Range(0.01, 1000000)]
    public decimal? Amount { get; set; }  // Monto a reembolsar (opcional)
    
    [StringLength(500)]
    public string? Reason { get; set; }   // Razón del reembolso (opcional)
}
```

### RefundResponse
```csharp
public class RefundResponse
{
    public string RefundId { get; set; }              // ID del reembolso (re_xxx)
    public string OriginalTransactionId { get; set; } // ID del PI o Charge original
    public PaymentStatus Status { get; set; }         // Estado del reembolso
    public decimal Amount { get; set; }               // Monto reembolsado
    public string Currency { get; set; }              // Moneda (USD, EUR, etc.)
    public string Message { get; set; }               // Mensaje descriptivo
    public DateTime Timestamp { get; set; }           // Fecha/hora del reembolso
}
```

---

## 🎨 Estados de Reembolso

| Estado      | Descripción                                    |
|-------------|------------------------------------------------|
| `Refunded`  | Reembolso procesado exitosamente               |
| `Pending`   | Reembolso en proceso                           |
| `Failed`    | Reembolso fallido                              |
| `Cancelled` | Reembolso cancelado                            |

---

## ⚙️ Razones de Reembolso en Stripe

Stripe acepta las siguientes razones estándar:

| Razón                     | Descripción                              |
|---------------------------|------------------------------------------|
| `duplicate`               | Transacción duplicada                    |
| `fraudulent`              | Transacción fraudulenta                  |
| `requested_by_customer`   | Cliente solicitó el reembolso (default)  |

El servicio mapea automáticamente las razones proporcionadas a estos valores estándar.

---

## 💡 Ejemplos de Uso

### Ejemplo 1: Reembolso Total de Payment Intent

```bash
# Reembolsar el monto total de un Payment Intent
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABCDEfghijk789012" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Cliente cambió de opinión"
  }'
```

### Ejemplo 2: Reembolso Parcial de Payment Intent

```bash
# Reembolsar solo $25 de un Payment Intent de $100
curl -X POST "https://localhost:7001/api/refunds/payment-intent/pi_1ABCDEfghijk789012" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 25.00,
    "reason": "Ajuste por error en el precio"
  }'
```

### Ejemplo 3: Reembolso desde Charge

```bash
# Reembolsar desde un Charge (legacy)
curl -X POST "https://localhost:7001/api/refunds/charge/ch_1MNOPQrstuvw345678" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 50.00,
    "reason": "fraudulent"
  }'
```

### Ejemplo 4: Consultar Estado de Reembolso

```bash
# Verificar el estado de un reembolso
curl -X GET "https://localhost:7001/api/refunds/re_1PQRSTuvwxyz123456"
```

### Ejemplo 5: Listar Últimos Reembolsos

```bash
# Obtener los últimos 20 reembolsos
curl -X GET "https://localhost:7001/api/refunds?limit=20"
```

---

## 🔐 Validaciones Implementadas

✅ **Payment Intent ID** debe comenzar con `pi_`  
✅ **Charge ID** debe comenzar con `ch_`  
✅ **Refund ID** debe comenzar con `re_`  
✅ **Amount** debe estar entre 0.01 y 1,000,000  
✅ **Limit** debe estar entre 1 y 100  
✅ **IDs no pueden estar vacíos**  

---

## ⚠️ Consideraciones Importantes

### 1. **Los reembolsos son definitivos**
- Una vez creado, un reembolso **NO se puede editar**.
- Los reembolsos **NO se pueden eliminar**.
- Solo se pueden crear y consultar.

### 2. **Reembolsos Parciales**
- Puedes realizar múltiples reembolsos parciales hasta el monto total del pago.
- La suma de todos los reembolsos no puede exceder el monto original.

### 3. **Tiempo de Procesamiento**
- Los reembolsos pueden tardar entre **5-10 días hábiles** en aparecer en la cuenta del cliente.
- El estado inicial es generalmente `succeeded`, pero el procesamiento bancario es asíncrono.

### 4. **Compatibilidad**
- **Recomendado**: Usar Payment Intent IDs (`pi_xxx`) para reembolsos.
- **Legacy**: Usar Charge IDs (`ch_xxx`) solo si es necesario por compatibilidad.

---

## 📝 Integración con Laravel

### Controlador Laravel

```php
<?php

namespace App\Http\Controllers\Api;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Http;

class RefundController extends Controller
{
    private $baseUrl = 'https://localhost:7001/api';

    /**
     * Crear un reembolso para un Payment Intent
     */
    public function createRefund(Request $request)
    {
        $validated = $request->validate([
            'payment_intent_id' => 'required|string|starts_with:pi_',
            'amount' => 'nullable|numeric|min:0.01',
            'reason' => 'nullable|string|max:500'
        ]);

        $response = Http::post(
            "{$this->baseUrl}/refunds/payment-intent/{$validated['payment_intent_id']}", 
            [
                'amount' => $validated['amount'] ?? null,
                'reason' => $validated['reason'] ?? 'requested_by_customer'
            ]
        );

        if ($response->successful()) {
            return response()->json($response->json(), 200);
        }

        return response()->json([
            'error' => $response->json()['error'] ?? 'Error al crear reembolso'
        ], $response->status());
    }

    /**
     * Obtener información de un reembolso
     */
    public function getRefund($refundId)
    {
        $response = Http::get("{$this->baseUrl}/refunds/{$refundId}");

        if ($response->successful()) {
            return response()->json($response->json(), 200);
        }

        return response()->json([
            'error' => $response->json()['error'] ?? 'Reembolso no encontrado'
        ], $response->status());
    }

    /**
     * Listar reembolsos
     */
    public function listRefunds(Request $request)
    {
        $limit = $request->query('limit', 10);
        
        $response = Http::get("{$this->baseUrl}/refunds", [
            'limit' => $limit
        ]);

        if ($response->successful()) {
            return response()->json($response->json(), 200);
        }

        return response()->json([
            'error' => 'Error al listar reembolsos'
        ], $response->status());
    }
}
```

### Rutas Laravel

```php
// routes/api.php
use App\Http\Controllers\Api\RefundController;

Route::prefix('refunds')->group(function () {
    Route::post('/payment-intent/{paymentIntentId}', [RefundController::class, 'createRefund']);
    Route::post('/charge/{chargeId}', [RefundController::class, 'createRefund']);
    Route::get('/{refundId}', [RefundController::class, 'getRefund']);
    Route::get('/', [RefundController::class, 'listRefunds']);
});
```

---

## 🧪 Ejemplos de Prueba

### 1. Crear Reembolso Total

**Request:**
```bash
POST /api/refunds/payment-intent/pi_1ABCDEfghijk789012
Content-Type: application/json

{
  "reason": "Cliente solicitó cancelación"
}
```

**Response:**
```json
{
  "refundId": "re_1PQRSTuvwxyz123456",
  "originalTransactionId": "pi_1ABCDEfghijk789012",
  "status": "Refunded",
  "amount": 100.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### 2. Crear Reembolso Parcial

**Request:**
```bash
POST /api/refunds/payment-intent/pi_1ABCDEfghijk789012
Content-Type: application/json

{
  "amount": 30.00,
  "reason": "Ajuste de precio"
}
```

**Response:**
```json
{
  "refundId": "re_1XYZABCdefghi456789",
  "originalTransactionId": "pi_1ABCDEfghijk789012",
  "status": "Refunded",
  "amount": 30.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T11:00:00Z"
}
```

### 3. Consultar Reembolso

**Request:**
```bash
GET /api/refunds/re_1PQRSTuvwxyz123456
```

**Response:**
```json
{
  "refundId": "re_1PQRSTuvwxyz123456",
  "originalTransactionId": "pi_1ABCDEfghijk789012",
  "status": "Refunded",
  "amount": 100.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### 4. Listar Reembolsos

**Request:**
```bash
GET /api/refunds?limit=5
```

**Response:**
```json
[
  {
    "refundId": "re_1PQRSTuvwxyz123456",
    "originalTransactionId": "pi_1ABCDEfghijk789012",
    "status": "Refunded",
    "amount": 100.00,
    "currency": "USD",
    "message": "Reembolso procesado exitosamente",
    "timestamp": "2024-01-15T10:30:00Z"
  },
  {
    "refundId": "re_1XYZABCdefghi456789",
    "originalTransactionId": "pi_1ABCDEfghijk789012",
    "status": "Refunded",
    "amount": 30.00,
    "currency": "USD",
    "message": "Reembolso procesado exitosamente",
    "timestamp": "2024-01-15T11:00:00Z"
  }
]
```

---

## 🚨 Manejo de Errores

### Error 400: Solicitud Inválida

```json
{
  "error": "El Payment Intent ID debe comenzar con 'pi_'"
}
```

### Error 404: Reembolso No Encontrado

```json
{
  "error": "Reembolso no encontrado: re_1InvalidID"
}
```

### Error 500: Error de Stripe

```json
{
  "error": "Error al crear reembolso en Stripe: Charge ch_xxx has already been refunded."
}
```

---

## 🔄 Flujo de Trabajo Típico

```
1. Laravel: Cliente solicita reembolso
   ↓
2. Laravel → .NET: POST /api/refunds/payment-intent/{pi_id}
   ↓
3. .NET → Stripe: Crear Refund
   ↓
4. Stripe: Procesa el reembolso
   ↓
5. .NET → Laravel: Devuelve RefundResponse con re_xxx
   ↓
6. Laravel: Guarda refund_id en base de datos
   ↓
7. Laravel: Notifica al cliente sobre el reembolso
```

---

## 🛡️ Limitaciones de Stripe

1. **No se pueden reembolsar pagos mayores a 1 año de antigüedad**
2. **El monto total de reembolsos no puede exceder el monto del pago original**
3. **Los reembolsos son definitivos y no se pueden cancelar**
4. **Algunos métodos de pago tienen restricciones específicas**

---

## 📌 Casos de Uso Comunes

### Caso 1: Cancelación de Pedido
```bash
# Cliente cancela pedido antes del envío - reembolso total
POST /api/refunds/payment-intent/pi_1ABC...
{
  "reason": "Cliente canceló el pedido"
}
```

### Caso 2: Producto Defectuoso
```bash
# Reembolso parcial por producto defectuoso
POST /api/refunds/payment-intent/pi_1ABC...
{
  "amount": 45.00,
  "reason": "Producto llegó dañado"
}
```

### Caso 3: Ajuste de Precio
```bash
# Reembolso parcial por diferencia de precio
POST /api/refunds/payment-intent/pi_1ABC...
{
  "amount": 10.00,
  "reason": "Ajuste de precio promocional"
}
```

### Caso 4: Múltiples Reembolsos Parciales
```bash
# Primer reembolso parcial ($30 de $100)
POST /api/refunds/payment-intent/pi_1ABC...
{"amount": 30.00, "reason": "Artículo A devuelto"}

# Segundo reembolso parcial ($20 de $100)
POST /api/refunds/payment-intent/pi_1ABC...
{"amount": 20.00, "reason": "Artículo B devuelto"}

# Total reembolsado: $50 de $100
# Restante: $50 aún capturados
```

---

## 🔍 Testing con Postman

### Colección de Pruebas

1. **Crear Reembolso Total**
   - Method: `POST`
   - URL: `{{baseUrl}}/api/refunds/payment-intent/{{paymentIntentId}}`
   - Body: `{ "reason": "requested_by_customer" }`

2. **Crear Reembolso Parcial**
   - Method: `POST`
   - URL: `{{baseUrl}}/api/refunds/payment-intent/{{paymentIntentId}}`
   - Body: `{ "amount": 25.00, "reason": "partial refund" }`

3. **Obtener Reembolso**
   - Method: `GET`
   - URL: `{{baseUrl}}/api/refunds/{{refundId}}`

4. **Listar Reembolsos**
   - Method: `GET`
   - URL: `{{baseUrl}}/api/refunds?limit=10`

---

## 🎯 Diferencias: Payment Intent vs Charge

| Aspecto               | Payment Intent (pi_xxx)                    | Charge (ch_xxx)                     |
|-----------------------|--------------------------------------------|-------------------------------------|
| **Recomendación**     | ✅ Método moderno recomendado              | ⚠️ API legacy, usar solo si necesario |
| **Flexibilidad**      | Mejor manejo de flujos de pago complejos   | Flujo simple de cargo directo       |
| **Reembolsos**        | Asociados al intent completo               | Asociados al cargo específico       |
| **Uso Típico**        | Nuevas implementaciones                    | Sistemas legacy existentes          |

---

## 📚 Documentación Relacionada

- [PAYMENT_INTENTS_API_GUIDE.md](./PAYMENT_INTENTS_API_GUIDE.md) - Gestión de Payment Intents
- [CUSTOMERS_API_GUIDE.md](./CUSTOMERS_API_GUIDE.md) - Gestión de Customers
- [PAYMENT_METHODS_API_GUIDE.md](./PAYMENT_METHODS_API_GUIDE.md) - Gestión de Payment Methods
- [Stripe Refunds API](https://stripe.com/docs/api/refunds) - Documentación oficial

---

## 🐛 Troubleshooting

### Problema: "Charge has already been refunded"
**Solución:** El cargo ya fue reembolsado completamente. Verifica el estado del Payment Intent.

### Problema: "Amount exceeds the amount available to refund"
**Solución:** El monto del reembolso excede el monto disponible. Verifica reembolsos anteriores.

### Problema: "Cannot refund a charge that is not captured"
**Solución:** El pago debe estar capturado antes de poder reembolsarlo. Confirma que el Payment Intent esté en estado `succeeded`.

---

## ✅ Checklist de Implementación

- [x] Modelos creados (`RefundRequest`, `RefundResponse`)
- [x] Interfaz de servicio (`IStripeRefundService`)
- [x] Implementación del servicio (`StripeRefundService`)
- [x] Controlador API (`RefundsController`)
- [x] Registro en `Program.cs`
- [x] Documentación completa
- [ ] Pruebas con Postman/cURL
- [ ] Integración con Laravel
- [ ] Testing en modo desarrollo

---

## 🎉 Listo para Usar

Tu API de reembolsos está completamente implementada y lista para ser consumida por Laravel. ¡Pruébala con los ejemplos proporcionados!

**Swagger UI:** `https://localhost:7001/swagger`
