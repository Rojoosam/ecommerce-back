# 🧪 Testing Rápido - API de Customers de Stripe

## 🚀 Endpoints para Testing

Reemplaza `https://localhost:7XXX` con la URL de tu API cuando la ejecutes.

---

## 1. ✅ Crear Customer

### Request
```bash
curl -X POST "https://localhost:7XXX/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "laravel_user_123",
    "name": "Juan Pérez García",
    "email": "juan.perez@ejemplo.com",
    "phone": "+52 55 1234 5678",
    "address": {
      "line1": "Av. Insurgentes Sur 1234",
      "line2": "Col. Del Valle, Piso 5",
      "city": "Ciudad de México",
      "state": "CDMX",
      "postal_code": "03100",
      "country": "MX"
    },
    "metadata": {
      "source": "web_app",
      "registration_date": "2024-01-15"
    }
  }'
```

### Respuesta Esperada (200 OK)
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "user_id": "laravel_user_123",
  "name": "Juan Pérez García",
  "email": "juan.perez@ejemplo.com",
  "phone": "+52 55 1234 5678",
  "address": {
    "line1": "Av. Insurgentes Sur 1234",
    "line2": "Col. Del Valle, Piso 5",
    "city": "Ciudad de México",
    "state": "CDMX",
    "postal_code": "03100",
    "country": "MX"
  },
  "created": "2024-01-15T15:30:45Z",
  "is_deleted": false,
  "metadata": {
    "user_id": "laravel_user_123",
    "source": "web_app",
    "registration_date": "2024-01-15"
  },
  "error_message": null
}
```

**⭐ IMPORTANTE**: Guarda el `customer_id` devuelto (ej: `cus_PQx1yZ2aBcDeFgHi`) para usarlo en las siguientes pruebas.

---

## 2. ✅ Crear Customer (Solo Campos Requeridos)

### Request
```bash
curl -X POST "https://localhost:7XXX/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "laravel_user_456",
    "name": "María González",
    "email": "maria.gonzalez@ejemplo.com"
  }'
```

### Respuesta Esperada (200 OK)
```json
{
  "success": true,
  "customer_id": "cus_ABC123XYZ456",
  "user_id": "laravel_user_456",
  "name": "María González",
  "email": "maria.gonzalez@ejemplo.com",
  "phone": null,
  "address": null,
  "created": "2024-01-15T15:35:20Z",
  "is_deleted": false,
  "metadata": {
    "user_id": "laravel_user_456"
  },
  "error_message": null
}
```

---

## 3. ✅ Actualizar Customer

### Request (Actualizar nombre y teléfono)
```bash
curl -X PUT "https://localhost:7XXX/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "name": "Juan Antonio Pérez García",
    "phone": "+52 55 9876 5432"
  }'
```

### Request (Actualizar dirección completa)
```bash
curl -X PUT "https://localhost:7XXX/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "address": {
      "line1": "Paseo de la Reforma 500",
      "line2": "Piso 10",
      "city": "Ciudad de México",
      "state": "CDMX",
      "postal_code": "06600",
      "country": "MX"
    }
  }'
```

### Respuesta Esperada (200 OK)
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "user_id": "laravel_user_123",
  "name": "Juan Antonio Pérez García",
  "email": "juan.perez@ejemplo.com",
  "phone": "+52 55 9876 5432",
  "address": {
    "line1": "Paseo de la Reforma 500",
    "line2": "Piso 10",
    "city": "Ciudad de México",
    "state": "CDMX",
    "postal_code": "06600",
    "country": "MX"
  },
  "created": "2024-01-15T15:30:45Z",
  "is_deleted": false,
  "metadata": {
    "user_id": "laravel_user_123"
  },
  "error_message": null
}
```

---

## 4. ✅ Obtener Customer

### Request
```bash
curl -X GET "https://localhost:7XXX/api/customers/cus_PQx1yZ2aBcDeFgHi" \
  -H "Content-Type: application/json"
```

### Respuesta Esperada (200 OK)
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "name": "Juan Antonio Pérez García",
  "email": "juan.perez@ejemplo.com",
  "phone": "+52 55 9876 5432",
  "address": {
    "line1": "Paseo de la Reforma 500",
    "line2": "Piso 10",
    "city": "Ciudad de México",
    "state": "CDMX",
    "postal_code": "06600",
    "country": "MX"
  },
  "created": "2024-01-15T15:30:45Z",
  "balance": 0,
  "currency": "mxn",
  "is_deleted": false,
  "metadata": {
    "user_id": "laravel_user_123"
  },
  "error_message": null
}
```

---

## 5. ✅ Eliminar Customer

### Request
```bash
curl -X DELETE "https://localhost:7XXX/api/customers/cus_PQx1yZ2aBcDeFgHi" \
  -H "Content-Type: application/json"
```

### Respuesta Esperada (200 OK)
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "deleted": true,
  "message": "Customer eliminado exitosamente en Stripe",
  "error_message": null
}
```

---

## ❌ Casos de Error

### Error 1: Crear sin campos requeridos
```bash
curl -X POST "https://localhost:7XXX/api/customers" \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "123"
  }'
```

**Respuesta (400 Bad Request)**
```json
{
  "success": false,
  "customer_id": null,
  "user_id": "123",
  "name": null,
  "email": null,
  "error_message": "El campo 'name' es requerido"
}
```

---

### Error 2: Customer ID inválido (formato incorrecto)
```bash
curl -X GET "https://localhost:7XXX/api/customers/invalid_id" \
  -H "Content-Type: application/json"
```

**Respuesta (400 Bad Request)**
```json
{
  "success": false,
  "customer_id": "invalid_id",
  "error_message": "El 'customer_id' debe tener el formato 'cus_xxx'"
}
```

---

### Error 3: Customer no encontrado
```bash
curl -X GET "https://localhost:7XXX/api/customers/cus_nonexistent123" \
  -H "Content-Type: application/json"
```

**Respuesta (404 Not Found)**
```json
{
  "success": false,
  "customer_id": "cus_nonexistent123",
  "error_message": "Error de Stripe: No such customer: 'cus_nonexistent123'"
}
```

---

## 🔧 Testing en PowerShell (Windows)

Si estás en Windows y prefieres PowerShell:

### Crear Customer
```powershell
$body = @{
    user_id = "laravel_user_123"
    name = "Juan Pérez"
    email = "juan.perez@ejemplo.com"
    phone = "+52 55 1234 5678"
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:7XXX/api/customers" `
  -Body $body `
  -ContentType "application/json"
```

### Obtener Customer
```powershell
Invoke-RestMethod -Method Get `
  -Uri "https://localhost:7XXX/api/customers/cus_PQx1yZ2aBcDeFgHi" `
  -ContentType "application/json"
```

### Actualizar Customer
```powershell
$body = @{
    customer_id = "cus_PQx1yZ2aBcDeFgHi"
    phone = "+52 55 9876 5432"
} | ConvertTo-Json

Invoke-RestMethod -Method Put `
  -Uri "https://localhost:7XXX/api/customers" `
  -Body $body `
  -ContentType "application/json"
```

### Eliminar Customer
```powershell
Invoke-RestMethod -Method Delete `
  -Uri "https://localhost:7XXX/api/customers/cus_PQx1yZ2aBcDeFgHi" `
  -ContentType "application/json"
```

---

## 📝 Notas para Testing

1. **Ejecuta la API primero**: 
   ```bash
   cd ECommerceAPI
   dotnet run
   ```

2. **Anota la URL**: Cuando la API se ejecute, mostrará algo como:
   ```
   Now listening on: https://localhost:7123
   ```
   Usa esa URL en tus comandos curl.

3. **Stripe Test Mode**: Asegúrate de usar claves de **TEST** de Stripe en `appsettings.json`:
   ```json
   {
     "Stripe": {
       "SecretKey": "sk_test_...",
       "PublishableKey": "pk_test_..."
     }
   }
   ```

4. **Swagger UI**: También puedes probar desde el navegador:
   ```
   https://localhost:7XXX/swagger
   ```

5. **Verifica en Stripe Dashboard**: 
   - Inicia sesión en https://dashboard.stripe.com/test/customers
   - Deberías ver los customers creados allí

---

## ✅ Checklist de Testing

- [ ] Crear customer con todos los campos
- [ ] Crear customer solo con campos requeridos
- [ ] Actualizar customer (nombre)
- [ ] Actualizar customer (teléfono)
- [ ] Actualizar customer (dirección)
- [ ] Obtener customer existente
- [ ] Intentar obtener customer inexistente (debe dar 404)
- [ ] Eliminar customer
- [ ] Verificar que el customer aparece en Stripe Dashboard
- [ ] Verificar que el `user_id` está guardado en metadata

---

## 🎯 Flujo Completo de Testing

```bash
# 1. Crear customer
CUSTOMER_ID=$(curl -X POST "https://localhost:7XXX/api/customers" \
  -H "Content-Type: application/json" \
  -d '{"user_id":"test_123","name":"Test User","email":"test@test.com"}' \
  | jq -r '.customer_id')

echo "Customer creado: $CUSTOMER_ID"

# 2. Obtener customer
curl -X GET "https://localhost:7XXX/api/customers/$CUSTOMER_ID"

# 3. Actualizar customer
curl -X PUT "https://localhost:7XXX/api/customers" \
  -H "Content-Type: application/json" \
  -d "{\"customer_id\":\"$CUSTOMER_ID\",\"phone\":\"+52 55 1234 5678\"}"

# 4. Eliminar customer
curl -X DELETE "https://localhost:7XXX/api/customers/$CUSTOMER_ID"
```

---

**¡Listo para testing!** 🚀
