# ?? Payment Gateway Simulator API

## Documentación de la API

Esta API es un microservicio de pasarela de pagos **simulado** para propósitos de demostración. Permite procesar pagos, consultar transacciones y realizar reembolsos a través de múltiples pasarelas de pago sin realizar transacciones reales.

---

## ?? Tabla de Contenidos

- [Inicio Rápido](#-inicio-rápido)
- [Autenticación](#-autenticación)
- [Endpoints](#-endpoints)
  - [Procesar Pago](#1-procesar-pago)
  - [Consultar Transacción](#2-consultar-transacción)
  - [Procesar Reembolso](#3-procesar-reembolso)
  - [Listar Pasarelas](#4-listar-pasarelas-disponibles)
  - [Health Check](#5-health-check)
- [Tarjetas de Prueba](#-tarjetas-de-prueba)
- [Pasarelas Soportadas](#-pasarelas-soportadas)
- [Modelos de Datos](#-modelos-de-datos)
- [Códigos de Error](#-códigos-de-error)
- [Ejemplos de Uso](#-ejemplos-de-uso)

---

## ?? Inicio Rápido

### Requisitos
- .NET 10 SDK
- Visual Studio 2022+ o VS Code

### Ejecutar la API

```bash
# Clonar el repositorio
git clone https://github.com/Rojoosam/ecommerce-back.git

# Navegar al directorio
cd ECommerceAPI

# Ejecutar la aplicación
dotnet run
```

### URL Base
- **Desarrollo**: `https://localhost:7xxx` o `http://localhost:5xxx`
- **Swagger UI**: `https://localhost:7xxx/swagger`

---

## ?? Autenticación

> ?? **Nota**: Esta es una API de demostración. No requiere autenticación en su estado actual.

Para implementaciones en producción, se recomienda agregar:
- API Keys
- OAuth 2.0 / JWT
- Rate Limiting

---

## ?? Endpoints

### 1. Procesar Pago

Procesa un pago a través de la pasarela especificada.

```
POST /api/payments/process
```

#### Request Body

```json
{
  "amount": 150.00,
  "currency": "USD",
  "gateway": 0,
  "card": {
    "number": "4242424242424242",
    "expiryMonth": 12,
    "expiryYear": 2025,
    "cvc": "123",
    "holderName": "Juan Pérez"
  },
  "description": "Compra en línea #12345",
  "customerId": "cust_001",
  "metadata": {
    "orderId": "ORD-12345",
    "productName": "Laptop HP"
  }
}
```

#### Parámetros

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `amount` | decimal | ? | Monto a cobrar (0.01 - 1,000,000) |
| `currency` | string | ? | Código de moneda ISO 4217 (3 caracteres) |
| `gateway` | enum | ? | Pasarela: `0` = Stripe, `1` = PayPal, `2` = MercadoPago |
| `card` | object | ? | Información de la tarjeta |
| `card.number` | string | ? | Número de tarjeta (13-19 dígitos) |
| `card.expiryMonth` | int | ? | Mes de expiración (1-12) |
| `card.expiryYear` | int | ? | Año de expiración (2024-2050) |
| `card.cvc` | string | ? | Código de seguridad (3-4 dígitos) |
| `card.holderName` | string | ? | Nombre del titular |
| `description` | string | ? | Descripción del pago (máx. 500 caracteres) |
| `customerId` | string | ? | Identificador del cliente |
| `metadata` | object | ? | Metadatos adicionales |

#### Response (200 OK)

```json
{
  "transactionId": "txn_a1b2c3d4e5f6g7h8",
  "status": "Approved",
  "message": "Pago aprobado exitosamente",
  "amount": 150.00,
  "currency": "USD",
  "gateway": "Stripe",
  "authorizationCode": "456789",
  "cardLastFour": "4242",
  "cardType": "Visa",
  "errorCode": null,
  "timestamp": "2024-01-15T10:30:00Z",
  "processingTimeMs": 245
}
```

#### Response (400 Bad Request)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Amount": ["El monto debe estar entre 0.01 y 1,000,000"]
  }
}
```

---

### 2. Consultar Transacción

Obtiene el estado actual de una transacción.

```
GET /api/payments/{id}
```

#### Parámetros de Ruta

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | string | ID de la transacción |

#### Response (200 OK)

```json
{
  "transactionId": "txn_a1b2c3d4e5f6g7h8",
  "status": "Approved",
  "amount": 150.00,
  "currency": "USD",
  "gateway": "Stripe",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": null,
  "refundId": null
}
```

#### Response (404 Not Found)

```json
{
  "error": "Transacción no encontrada",
  "transactionId": "txn_invalid123"
}
```

---

### 3. Procesar Reembolso

Procesa un reembolso para una transacción existente.

```
POST /api/payments/{id}/refund
```

#### Parámetros de Ruta

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | string | ID de la transacción a reembolsar |

#### Request Body

```json
{
  "amount": 50.00,
  "reason": "Producto defectuoso"
}
```

#### Parámetros

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `amount` | decimal | ? | Monto a reembolsar (si no se especifica, reembolso total) |
| `reason` | string | ? | Razón del reembolso (máx. 500 caracteres) |

#### Response (200 OK)

```json
{
  "refundId": "ref_x1y2z3w4v5u6",
  "originalTransactionId": "txn_a1b2c3d4e5f6g7h8",
  "status": "Refunded",
  "amount": 50.00,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-01-15T11:00:00Z"
}
```

#### Response (400 Bad Request)

```json
{
  "refundId": "",
  "originalTransactionId": "txn_a1b2c3d4e5f6g7h8",
  "status": "Failed",
  "amount": 0,
  "currency": "USD",
  "message": "Solo se pueden reembolsar transacciones aprobadas",
  "timestamp": "2024-01-15T11:00:00Z"
}
```

---

### 4. Listar Pasarelas Disponibles

Obtiene la lista de pasarelas de pago disponibles.

```
GET /api/payments/gateways
```

#### Response (200 OK)

```json
[
  {
    "gateway": "Stripe",
    "name": "Stripe",
    "description": "Stripe es una plataforma de pagos en línea...",
    "supportedCurrencies": ["USD", "EUR", "GBP", "MXN", "CAD", "AUD", "JPY", "BRL"],
    "supportedCardTypes": ["Visa", "MasterCard", "AmericanExpress"],
    "isActive": true,
    "isSimulated": true
  },
  {
    "gateway": "PayPal",
    "name": "PayPal",
    "description": "PayPal es un sistema de pagos en línea...",
    "supportedCurrencies": ["USD", "EUR", "GBP", "MXN", "CAD", "AUD", "JPY", "BRL", "CHF", "CZK", "DKK", "HKD"],
    "supportedCardTypes": ["Visa", "MasterCard", "AmericanExpress"],
    "isActive": true,
    "isSimulated": true
  },
  {
    "gateway": "MercadoPago",
    "name": "MercadoPago",
    "description": "MercadoPago es la plataforma de pagos líder en América Latina...",
    "supportedCurrencies": ["MXN", "ARS", "BRL", "CLP", "COP", "PEN", "UYU", "USD"],
    "supportedCardTypes": ["Visa", "MasterCard", "AmericanExpress"],
    "isActive": true,
    "isSimulated": true
  }
]
```

---

### 5. Health Check

Verifica el estado del servicio.

```
GET /api/payments/health
```

#### Response (200 OK)

```json
{
  "status": "healthy",
  "service": "Payment Gateway Simulator",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0"
}
```

---

## ?? Tarjetas de Prueba

Utiliza estas tarjetas para simular diferentes escenarios:

### ? Tarjetas Exitosas

| Número | Tipo | Resultado |
|--------|------|-----------|
| `4242424242424242` | Visa | Pago aprobado |
| `5555555555554444` | MasterCard | Pago aprobado |
| `378282246310005` | American Express | Pago aprobado |

### ? Tarjetas con Error

| Número | Resultado | Código de Error |
|--------|-----------|-----------------|
| `4000000000000002` | Tarjeta declinada | `card_declined` |
| `4000000000000069` | Tarjeta expirada | `expired_card` |
| `4000000000000127` | CVC incorrecto | `incorrect_cvc` |
| `4000000000000119` | Error de procesamiento | `processing_error` |
| `4000000000009995` | Fondos insuficientes | `insufficient_funds` |

### ?? Otras Tarjetas

Cualquier otro número de tarjeta válido producirá resultados aleatorios:
- **80%** de probabilidad de éxito
- **15%** de probabilidad de declinación
- **5%** de probabilidad de error

---

## ?? Pasarelas Soportadas

| Gateway | Enum Value | Monedas Principales |
|---------|------------|---------------------|
| **Stripe** | `0` | USD, EUR, GBP, MXN |
| **PayPal** | `1` | USD, EUR, GBP, MXN, CHF |
| **MercadoPago** | `2` | MXN, ARS, BRL, CLP, COP |

---

## ?? Modelos de Datos

### PaymentStatus (Enum)

| Valor | Descripción |
|-------|-------------|
| `Pending` | Pago pendiente |
| `Processing` | En procesamiento |
| `Approved` | Aprobado |
| `Declined` | Declinado |
| `Failed` | Fallido |
| `Refunded` | Reembolsado |
| `Cancelled` | Cancelado |

### PaymentGateway (Enum)

| Valor | Nombre |
|-------|--------|
| `0` | Stripe |
| `1` | PayPal |
| `2` | MercadoPago |

### CardType (Enum)

| Valor | Nombre |
|-------|--------|
| `0` | Visa |
| `1` | MasterCard |
| `2` | AmericanExpress |
| `3` | Unknown |

---

## ?? Códigos de Error

| Código | Descripción |
|--------|-------------|
| `card_declined` | La tarjeta fue declinada por el banco |
| `expired_card` | La tarjeta ha expirado |
| `incorrect_cvc` | El código CVC es incorrecto |
| `insufficient_funds` | Fondos insuficientes |
| `processing_error` | Error interno del procesador |
| `gateway_error` | Error de conexión con la pasarela |

---

## ?? Ejemplos de Uso

### cURL

#### Procesar un Pago

```bash
curl -X POST "https://localhost:7xxx/api/payments/process" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 99.99,
    "currency": "USD",
    "gateway": 0,
    "card": {
      "number": "4242424242424242",
      "expiryMonth": 12,
      "expiryYear": 2025,
      "cvc": "123",
      "holderName": "John Doe"
    },
    "description": "Test payment"
  }'
```

#### Consultar Transacción

```bash
curl -X GET "https://localhost:7xxx/api/payments/txn_a1b2c3d4e5f6g7h8"
```

#### Procesar Reembolso

```bash
curl -X POST "https://localhost:7xxx/api/payments/txn_a1b2c3d4e5f6g7h8/refund" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Customer request"
  }'
```

### C# HttpClient

```csharp
using var client = new HttpClient();
client.BaseAddress = new Uri("https://localhost:7xxx");

var payment = new
{
    amount = 99.99m,
    currency = "USD",
    gateway = 0, // Stripe
    card = new
    {
        number = "4242424242424242",
        expiryMonth = 12,
        expiryYear = 2025,
        cvc = "123",
        holderName = "John Doe"
    }
};

var response = await client.PostAsJsonAsync("/api/payments/process", payment);
var result = await response.Content.ReadFromJsonAsync<PaymentResponse>();

Console.WriteLine($"Transaction ID: {result.TransactionId}");
Console.WriteLine($"Status: {result.Status}");
```

### JavaScript (Fetch)

```javascript
const payment = {
  amount: 99.99,
  currency: "USD",
  gateway: 0,
  card: {
    number: "4242424242424242",
    expiryMonth: 12,
    expiryYear: 2025,
    cvc: "123",
    holderName: "John Doe"
  }
};

const response = await fetch("https://localhost:7xxx/api/payments/process", {
  method: "POST",
  headers: {
    "Content-Type": "application/json"
  },
  body: JSON.stringify(payment)
});

const result = await response.json();
console.log(`Transaction ID: ${result.transactionId}`);
console.log(`Status: ${result.status}`);
```

### Python (Requests)

```python
import requests

payment = {
    "amount": 99.99,
    "currency": "USD",
    "gateway": 0,
    "card": {
        "number": "4242424242424242",
        "expiryMonth": 12,
        "expiryYear": 2025,
        "cvc": "123",
        "holderName": "John Doe"
    }
}

response = requests.post(
    "https://localhost:7xxx/api/payments/process",
    json=payment
)

result = response.json()
print(f"Transaction ID: {result['transactionId']}")
print(f"Status: {result['status']}")
```

---

## ?? Notas Importantes

> ?? **ADVERTENCIA**: Esta API es solo para demostración y pruebas. **NO** procesa transacciones reales.

1. **Almacenamiento**: Las transacciones se almacenan en memoria y se pierden al reiniciar la aplicación.

2. **Latencia Simulada**: Cada solicitud tiene un delay aleatorio de 100-500ms para simular condiciones reales.

3. **Sin Persistencia**: No hay base de datos. Para producción, implemente almacenamiento persistente.

4. **Sin Autenticación**: Agregue autenticación antes de usar en producción.

---

## ?? Soporte

Para reportar problemas o sugerencias:
- **GitHub Issues**: [https://github.com/Rojoosam/ecommerce-back/issues](https://github.com/Rojoosam/ecommerce-back/issues)

---

## ?? Licencia

Este proyecto es para propósitos educativos y de demostración.

---

*Documentación generada para Payment Gateway Simulator API v1.0.0*
