# 🔄 Migración de Simulador a Integración Real con Stripe

## 📝 Resumen de Cambios

Tu API ha sido actualizada para soportar **pagos reales con Stripe**, manteniendo la compatibilidad con los simuladores existentes (PayPal y MercadoPago).

### ✅ Lo que se implementó:

1. **Integración Real con Stripe**
   - SDK oficial Stripe.NET v50.4.0
   - Soporte para Payment Intents
   - Manejo de 3D Secure
   - Procesamiento de reembolsos reales

2. **Seguridad**
   - Configuración de API keys mediante variables de entorno
   - Soporte para User Secrets
   - Nunca almacena números de tarjeta completos

3. **Flexibilidad**
   - Opción de usar simulador o servicio real
   - Soporte para tokens de Stripe (producción)
   - Soporte para datos de tarjeta (testing)

## 🚀 Guía de Inicio Rápido

### Paso 1: Obtener Claves de Stripe

1. Ve a [stripe.com](https://stripe.com) y crea una cuenta
2. Accede al [Dashboard](https://dashboard.stripe.com/test/apikeys)
3. Copia tus claves de **Test Mode**:
   - Secret Key (sk_test_...)
   - Publishable Key (pk_test_...)

### Paso 2: Configurar las Claves (User Secrets - Recomendado)

Abre una terminal en la carpeta `ECommerceAPI` y ejecuta:

```bash
# Navegar al proyecto
cd ECommerceAPI

# Configurar las claves usando User Secrets
dotnet user-secrets set "Stripe:SecretKey" "sk_test_TU_CLAVE_AQUI"
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_TU_CLAVE_AQUI"
```

**Alternativa**: Editar `appsettings.Development.json` (solo para pruebas locales):

```json
{
  "Stripe": {
    "SecretKey": "sk_test_TU_CLAVE_AQUI",
    "PublishableKey": "pk_test_TU_CLAVE_AQUI"
  }
}
```

### Paso 3: Ejecutar la API

```bash
dotnet run
```

La API estará disponible en `https://localhost:7001` (o el puerto configurado).

### Paso 4: Probar con Swagger

1. Abre `https://localhost:7001/swagger`
2. Usa el endpoint `POST /api/payments/process`
3. Prueba con esta tarjeta: **4242 4242 4242 4242**

Ejemplo de request:

```json
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
  "description": "Compra de prueba"
}
```

## 🔧 Configuración Avanzada

### Cambiar entre Simulador y Servicio Real

En `Program.cs`, línea ~12:

```csharp
// USAR STRIPE REAL (configuración actual)
builder.Services.AddSingleton<IPaymentGateway, StripePaymentService>();

// O USAR STRIPE SIMULADO (comentar la línea anterior y descomentar esta)
// builder.Services.AddSingleton<IPaymentGateway, StripeSimulatorService>();
```

### Variables de Entorno para Producción

```bash
# Azure App Service
az webapp config appsettings set --name tu-app --resource-group tu-grupo \
  --settings Stripe__SecretKey="sk_live_..." Stripe__PublishableKey="pk_live_..."

# Docker
docker run -e Stripe__SecretKey="sk_live_..." -e Stripe__PublishableKey="pk_live_..." tu-imagen
```

## 🧪 Testing

### Tarjetas de Prueba

Stripe proporciona tarjetas específicas para testing:

| Tarjeta | Resultado |
|---------|-----------|
| 4242 4242 4242 4242 | ✅ Pago exitoso |
| 4000 0000 0000 0002 | ❌ Tarjeta declinada |
| 4000 0000 0000 9995 | ❌ Fondos insuficientes |
| 4000 0025 0000 3155 | 🔐 Requiere 3D Secure |

**Para todas**: Fecha futura (12/2025), CVV cualquier 3 dígitos (123)

### Ejecutar Tests Unitarios

Los tests existentes siguen funcionando con los simuladores:

```bash
dotnet test
```

## 📚 Archivos Nuevos y Modificados

### Archivos Nuevos

- `ECommerceAPI/Services/StripePaymentService.cs` - Servicio real de Stripe
- `ECommerceAPI/Configuration/StripeSettings.cs` - Configuración
- `ECommerceAPI/Controllers/StripeConfigController.cs` - Endpoint para clave pública
- `ECommerceAPI/docs/STRIPE_INTEGRATION_GUIDE.md` - Guía completa
- `ECommerceAPI/wwwroot/stripe-example.html` - Ejemplo de frontend

### Archivos Modificados

- `ECommerceAPI/Models/PaymentRequest.cs` - Soporte para tokens de Stripe
- `ECommerceAPI/Models/PaymentResponse.cs` - Campos adicionales de Stripe
- `ECommerceAPI/Services/BasePaymentSimulator.cs` - Validación de Card opcional
- `ECommerceAPI/Program.cs` - Configuración de servicios
- `ECommerceAPI/appsettings.json` - Configuración de Stripe
- `ECommerceAPI/appsettings.Development.json` - Claves de desarrollo
- `ECommerceAPI/ECommerceAPI.csproj` - Dependencia Stripe.NET

## 🔐 Seguridad - Mejores Prácticas

### ✅ HACER

- ✅ Usar User Secrets para desarrollo
- ✅ Usar Variables de Entorno para producción
- ✅ Implementar Stripe.js en el frontend
- ✅ Validar montos en el servidor
- ✅ Usar HTTPS siempre

### ❌ NO HACER

- ❌ Nunca comitear claves de API
- ❌ Nunca almacenar números de tarjeta completos
- ❌ Nunca usar claves de producción en desarrollo
- ❌ Nunca confiar solo en validación del cliente

## 🌐 Integración Frontend

Ver el archivo `wwwroot/stripe-example.html` para un ejemplo completo.

Pasos básicos:

1. **Obtener clave pública del backend**
   ```javascript
   const response = await fetch('/api/stripeconfig/publishable-key');
   const {publishableKey} = await response.json();
   ```

2. **Inicializar Stripe.js**
   ```javascript
   const stripe = Stripe(publishableKey);
   const elements = stripe.elements();
   const cardElement = elements.create('card');
   ```

3. **Crear Payment Method**
   ```javascript
   const {paymentMethod} = await stripe.createPaymentMethod({
     type: 'card',
     card: cardElement,
   });
   ```

4. **Enviar al backend**
   ```javascript
   const response = await fetch('/api/payments/process', {
     method: 'POST',
     body: JSON.stringify({
       amount: 100,
       currency: 'USD',
       gateway: 0,
       stripePaymentMethodId: paymentMethod.id
     })
   });
   ```

## 📊 Endpoints de la API

### Nuevo Endpoint

```
GET /api/stripeconfig/publishable-key
```
Retorna la clave pública de Stripe para uso en el cliente.

### Endpoints Existentes (con cambios)

```
POST /api/payments/process
```
Ahora acepta:
- `card` (opcional) - Para simuladores o testing
- `stripePaymentMethodId` (opcional) - Para pagos reales
- `stripeToken` (opcional) - Alternativa legacy

```
POST /api/payments/{id}/refund
```
Ahora procesa reembolsos reales con Stripe.

## 🐛 Solución de Problemas

### "No API key provided"

**Causa**: Las claves de Stripe no están configuradas.

**Solución**:
```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
```

### "Se requiere información de tarjeta para el simulador"

**Causa**: Estás usando un simulador pero enviaste `stripePaymentMethodId` en lugar de `card`.

**Solución**: 
- Cambia a `StripePaymentService` en `Program.cs`, o
- Envía el objeto `card` en el request

### Los tests fallan

**Causa**: Los tests usan el servicio real en lugar del simulador.

**Solución**: Los tests deben usar `StripeSimulatorService`. Verifica la configuración de test.

## 📖 Recursos

- 📘 [Guía Completa de Integración](docs/STRIPE_INTEGRATION_GUIDE.md)
- 🌐 [Ejemplo de Frontend](wwwroot/stripe-example.html)
- 📚 [Documentación de Stripe](https://stripe.com/docs)
- 🔧 [Stripe.NET GitHub](https://github.com/stripe/stripe-dotnet)

## 🎯 Próximos Pasos Recomendados

1. **Webhooks**: Implementa webhooks de Stripe para eventos asíncronos
2. **Customers**: Guarda clientes en Stripe para pagos recurrentes
3. **Subscriptions**: Implementa suscripciones si es necesario
4. **Reporting**: Integra con Stripe Dashboard para reportes
5. **Testing**: Añade tests de integración con Stripe en modo test

## 💡 Notas Importantes

- **Test Mode**: Las claves `sk_test_` y `pk_test_` NO realizan cargos reales
- **Live Mode**: Solo usa claves `sk_live_` en producción con HTTPS
- **Costos**: Stripe cobra ~2.9% + $0.30 por transacción exitosa
- **Webhooks**: Configura webhooks para eventos críticos como pagos exitosos
- **PCI Compliance**: Al usar Stripe.js, reduces tu carga de cumplimiento PCI

## 🆘 Soporte

Si tienes problemas:

1. Revisa la [Guía de Integración](docs/STRIPE_INTEGRATION_GUIDE.md)
2. Consulta los logs de la API
3. Verifica el [Dashboard de Stripe](https://dashboard.stripe.com)
4. Revisa la [documentación de Stripe](https://stripe.com/docs)

---

**¡Tu API ya está lista para procesar pagos reales! 🎉**
