# Integración Real con Stripe - Guía de Uso

## 📋 Requisitos Previos

1. **Cuenta de Stripe**: Crea una cuenta en [stripe.com](https://stripe.com)
2. **Claves de API**: Obtén tus claves de prueba desde el [Dashboard de Stripe](https://dashboard.stripe.com/test/apikeys)

## 🔑 Configuración de API Keys

### Opción 1: Variables de Entorno (Recomendado para Producción)

```bash
# Windows
setx STRIPE__SECRETKEY "sk_test_tu_clave_secreta_aqui"
setx STRIPE__PUBLISHABLEKEY "pk_test_tu_clave_publica_aqui"

# Linux/Mac
export STRIPE__SECRETKEY="sk_test_tu_clave_secreta_aqui"
export STRIPE__PUBLISHABLEKEY="pk_test_tu_clave_publica_aqui"
```

### Opción 2: User Secrets (Recomendado para Desarrollo)

```bash
cd ECommerceAPI
dotnet user-secrets set "Stripe:SecretKey" "sk_test_tu_clave_secreta_aqui"
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_tu_clave_publica_aqui"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_tu_webhook_secret_aqui"
```

### Opción 3: appsettings.Development.json (Solo para pruebas locales)

⚠️ **NUNCA subas este archivo al repositorio con claves reales**

Edita `appsettings.Development.json`:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_51ABC...",
    "PublishableKey": "pk_test_51ABC...",
    "WebhookSecret": "whsec_ABC..."
  }
}
```

## 🧪 Tarjetas de Prueba de Stripe

Stripe proporciona tarjetas de prueba para diferentes escenarios:

### Pagos Exitosos
- `4242 4242 4242 4242` - Visa exitosa
- `5555 5555 5555 4444` - Mastercard exitosa
- `3782 822463 10005` - American Express exitosa

### Pagos con Errores
- `4000 0000 0000 0002` - Tarjeta declinada
- `4000 0000 0000 9995` - Fondos insuficientes
- `4000 0000 0000 0069` - Tarjeta expirada
- `4000 0000 0000 0127` - CVC incorrecto

### Autenticación 3D Secure
- `4000 0025 0000 3155` - Requiere autenticación

**Para todas las tarjetas de prueba:**
- Fecha de expiración: Cualquier fecha futura (ej: 12/2025)
- CVC: Cualquier 3 dígitos (ej: 123)
- Nombre: Cualquier nombre

## 🚀 Uso de la API

### 1. Obtener la Clave Pública (para el Frontend)

```bash
GET /api/stripeconfig/publishable-key
```

Respuesta:
```json
{
  "publishableKey": "pk_test_51ABC..."
}
```

### 2. Procesar un Pago

#### Opción A: Enviar información de tarjeta directamente (Solo para Testing)

```bash
POST /api/payments/process
Content-Type: application/json

{
  "amount": 100.50,
  "currency": "USD",
  "gateway": 0,
  "card": {
    "number": "4242424242424242",
    "expiryMonth": 12,
    "expiryYear": 2025,
    "cvc": "123",
    "holderName": "John Doe"
  },
  "description": "Compra de productos",
  "customerId": "cus_ABC123",
  "metadata": {
    "orderId": "ORD-12345",
    "product": "Widget"
  }
}
```

#### Opción B: Usar Payment Method ID (Recomendado para Producción)

**En el Frontend (usando Stripe.js):**

```javascript
// 1. Cargar Stripe.js
const stripe = Stripe('pk_test_tu_clave_publica');

// 2. Crear un Payment Method
const {error, paymentMethod} = await stripe.createPaymentMethod({
  type: 'card',
  card: cardElement,
  billing_details: {
    name: 'John Doe',
  },
});

// 3. Enviar el Payment Method ID al backend
const response = await fetch('/api/payments/process', {
  method: 'POST',
  headers: {'Content-Type': 'application/json'},
  body: JSON.stringify({
    amount: 100.50,
    currency: 'USD',
    gateway: 0,
    stripePaymentMethodId: paymentMethod.id,
    description: 'Compra de productos'
  })
});
```

**En el Backend (tu API lo procesa automáticamente)**

### Respuesta Exitosa

```json
{
  "transactionId": "pi_3ABC123...",
  "status": 1,
  "message": "Pago procesado exitosamente",
  "amount": 100.50,
  "currency": "USD",
  "gateway": 0,
  "authorizationCode": "pi_3ABC123...",
  "cardLastFour": "4242",
  "cardType": 1,
  "timestamp": "2024-03-20T10:30:00Z",
  "processingTimeMs": 1234,
  "stripePaymentIntentId": "pi_3ABC123...",
  "stripeClientSecret": "pi_3ABC123..._secret_XYZ",
  "requiresAction": false
}
```

### Respuesta con Autenticación Requerida (3D Secure)

```json
{
  "transactionId": "pi_3ABC123...",
  "status": 2,
  "message": "El pago requiere autenticación adicional (3D Secure)",
  "requiresAction": true,
  "stripeClientSecret": "pi_3ABC123..._secret_XYZ"
}
```

**En el Frontend:**

```javascript
if (response.requiresAction) {
  const {error} = await stripe.confirmCardPayment(response.stripeClientSecret);
  
  if (error) {
    console.error('Autenticación fallida:', error);
  } else {
    console.log('Pago confirmado exitosamente');
  }
}
```

### 3. Procesar un Reembolso

```bash
POST /api/payments/{transactionId}/refund
Content-Type: application/json

{
  "amount": 50.25,
  "reason": "requested_by_customer"
}
```

Respuesta:
```json
{
  "refundId": "re_ABC123...",
  "originalTransactionId": "pi_3ABC123...",
  "status": 4,
  "amount": 50.25,
  "currency": "USD",
  "message": "Reembolso procesado exitosamente",
  "timestamp": "2024-03-20T10:35:00Z"
}
```

## 🔐 Mejores Prácticas de Seguridad

### ✅ HACER

1. **Nunca almacenar números de tarjeta**
   - Usa Stripe.js en el cliente para tokenizar tarjetas
   - Solo envía tokens/Payment Method IDs al servidor

2. **Usar Variables de Entorno**
   - Nunca comitear claves de API en el código
   - Usa Azure Key Vault, AWS Secrets Manager, etc. en producción

3. **Validar en el Servidor**
   - Siempre valida montos y datos en el backend
   - No confíes solo en la validación del cliente

4. **Implementar Webhooks**
   - Usa webhooks de Stripe para eventos críticos
   - Verifica la firma del webhook

5. **Usar HTTPS**
   - Siempre usa HTTPS en producción
   - Nunca envíes datos sensibles por HTTP

### ❌ NO HACER

1. ❌ No envíes números de tarjeta completos al servidor en producción
2. ❌ No almacenes CVV/CVC nunca
3. ❌ No incluyas claves de API en el repositorio
4. ❌ No uses la clave secreta en el frontend
5. ❌ No proceses pagos sin validación en el servidor

## 🌍 Monedas Soportadas

Stripe soporta 135+ monedas. Las más comunes:

- USD - Dólar estadounidense
- EUR - Euro
- GBP - Libra esterlina
- MXN - Peso mexicano
- CAD - Dólar canadiense
- AUD - Dólar australiano
- JPY - Yen japonés (sin decimales)
- BRL - Real brasileño

## 🔄 Estados de Pago

| Estado en la API | Status en Stripe | Descripción |
|-----------------|------------------|-------------|
| Approved (1) | succeeded | Pago completado exitosamente |
| Pending (2) | processing/requires_action | Pago en proceso o requiere autenticación |
| Failed (3) | requires_payment_method/canceled | Pago fallido o cancelado |
| Refunded (4) | - | Pago reembolsado |

## 🧪 Testing

### Test Mode vs Live Mode

Stripe tiene dos modos:

1. **Test Mode** (Claves que empiezan con `sk_test_` y `pk_test_`)
   - Usa tarjetas de prueba
   - No se realizan cargos reales
   - Ideal para desarrollo y pruebas

2. **Live Mode** (Claves que empiezan con `sk_live_` y `pk_live_`)
   - Cargos reales
   - Solo para producción

### Cambiar entre Simulador y Servicio Real

En `Program.cs`, cambia el registro del servicio:

```csharp
// Usar servicio REAL de Stripe
builder.Services.AddSingleton<IPaymentGateway, StripePaymentService>();

// O usar SIMULADOR de Stripe
builder.Services.AddSingleton<IPaymentGateway, StripeSimulatorService>();
```

## 📊 Monitoreo y Logs

La API registra automáticamente:
- Intentos de pago
- Errores de Stripe
- IDs de transacciones
- Tiempos de procesamiento

Revisa los logs en:
- Consola durante desarrollo
- Azure Application Insights en producción

## 🆘 Solución de Problemas

### Error: "No API key provided"

**Solución:** Configura las claves de API usando User Secrets o variables de entorno.

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
```

### Error: "Your card was declined"

**Solución:** Usa una tarjeta de prueba válida de Stripe (ej: 4242424242424242)

### Error: "Invalid API Key"

**Solución:** Verifica que la clave sea correcta y esté en el modo correcto (test/live)

## 📚 Recursos Adicionales

- [Documentación de Stripe](https://stripe.com/docs)
- [Stripe.NET SDK](https://github.com/stripe/stripe-dotnet)
- [Dashboard de Stripe](https://dashboard.stripe.com)
- [Tarjetas de Prueba](https://stripe.com/docs/testing)
- [Stripe.js Reference](https://stripe.com/docs/js)

## 🚀 Próximos Pasos

1. ✅ Configura tus claves de API
2. ✅ Prueba con tarjetas de test
3. 📝 Implementa el frontend con Stripe.js
4. 🔔 Configura webhooks
5. 🌐 Despliega a producción con claves live
