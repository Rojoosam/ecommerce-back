# 🧪 Pruebas para Actualización de Clientes (Requerimiento 5)

## 🎯 Objetivo
Verificar que la funcionalidad de actualización de customers funciona correctamente, cumpliendo con el requerimiento 5.

---

## 📝 Pre-requisitos

1. ✅ API .NET corriendo en `http://localhost:5000`
2. ✅ Configuración de Stripe válida en `appsettings.json`
3. ✅ Un customer existente en Stripe para actualizar

---

## 🧪 Pruebas de Actualización

### Paso 1: Crear un Customer para Pruebas

**Request:**
```bash
curl -X POST http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "test_user_123",
    "name": "María González",
    "email": "maria.original@example.com",
    "phone": "+52 55 1111 2222",
    "address": {
      "line1": "Calle Original 100",
      "city": "Monterrey",
      "state": "Nuevo León",
      "postal_code": "64000",
      "country": "MX"
    }
  }'
```

**✅ Resultado esperado:**
- Status: 200 OK
- Respuesta con `customer_id` (ej: "cus_xxx...")
- Guardar este `customer_id` para las siguientes pruebas

---

### Paso 2: Actualizar Solo el Nombre

**Request:**
```bash
curl -X PUT http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "name": "María Isabel González"
  }'
```

**✅ Resultado esperado:**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "user_id": "test_user_123",
  "name": "María Isabel González",
  "email": "maria.original@example.com",
  "phone": "+52 55 1111 2222",
  "address": {
    "line1": "Calle Original 100",
    "city": "Monterrey",
    "state": "Nuevo León",
    "postal_code": "64000",
    "country": "MX"
  },
  "is_deleted": false,
  "error_message": null
}
```

**✅ Verificaciones:**
- ✅ `success` = true
- ✅ `name` actualizado
- ✅ Otros campos mantienen sus valores originales

---

### Paso 3: Actualizar Email y Teléfono

**Request:**
```bash
curl -X PUT http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "email": "maria.nuevo@example.com",
    "phone": "+52 55 9999 8888"
  }'
```

**✅ Resultado esperado:**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "name": "María Isabel González",
  "email": "maria.nuevo@example.com",
  "phone": "+52 55 9999 8888",
  "error_message": null
}
```

**✅ Verificaciones:**
- ✅ `email` actualizado al nuevo valor
- ✅ `phone` actualizado al nuevo valor
- ✅ `name` mantiene el valor del paso anterior

---

### Paso 4: Actualizar Dirección Completa

**Request:**
```bash
curl -X PUT http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "address": {
      "line1": "Avenida Nueva 500",
      "line2": "Torre A, Piso 5",
      "city": "Guadalajara",
      "state": "Jalisco",
      "postal_code": "44100",
      "country": "MX"
    }
  }'
```

**✅ Resultado esperado:**
```json
{
  "success": true,
  "customer_id": "cus_PQx1yZ2aBcDeFgHi",
  "name": "María Isabel González",
  "email": "maria.nuevo@example.com",
  "address": {
    "line1": "Avenida Nueva 500",
    "line2": "Torre A, Piso 5",
    "city": "Guadalajara",
    "state": "Jalisco",
    "postal_code": "44100",
    "country": "MX"
  },
  "error_message": null
}
```

**✅ Verificaciones:**
- ✅ Dirección completamente actualizada
- ✅ Otros campos se mantienen igual

---

### Paso 5: Actualizar Todo a la Vez

**Request:**
```bash
curl -X PUT http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_PQx1yZ2aBcDeFgHi",
    "name": "María Isabel González Rodríguez",
    "email": "maria.final@example.com",
    "phone": "+52 55 7777 6666",
    "address": {
      "line1": "Boulevard Completo 999",
      "line2": "Departamento 301",
      "city": "Puebla",
      "state": "Puebla",
      "postal_code": "72000",
      "country": "MX"
    },
    "metadata": {
      "updated_from": "laravel_profile_page",
      "last_update": "2025-01-15"
    }
  }'
```

**✅ Resultado esperado:**
- ✅ Todos los campos actualizados correctamente
- ✅ Metadata actualizado con nuevos valores
- ✅ `success` = true

---

## ❌ Pruebas de Validación y Errores

### Prueba 6: Customer ID Faltante

**Request:**
```bash
curl -X PUT http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Nombre Sin ID"
  }'
```

**✅ Resultado esperado:**
```json
{
  "success": false,
  "error_message": "El campo 'customer_id' es requerido"
}
```
- Status: 400 Bad Request

---

### Prueba 7: Customer ID con Formato Incorrecto

**Request:**
```bash
curl -X PUT http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "invalid_id_123",
    "name": "Nombre Nuevo"
  }'
```

**✅ Resultado esperado:**
```json
{
  "success": false,
  "customer_id": "invalid_id_123",
  "error_message": "El 'customer_id' debe tener el formato 'cus_xxx'"
}
```
- Status: 400 Bad Request

---

### Prueba 8: Customer ID No Existente

**Request:**
```bash
curl -X PUT http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "customer_id": "cus_NoExiste123456",
    "name": "Nombre Nuevo"
  }'
```

**✅ Resultado esperado:**
```json
{
  "success": false,
  "customer_id": "cus_NoExiste123456",
  "error_message": "Error de Stripe: No such customer: 'cus_NoExiste123456'"
}
```
- Status: 500 Internal Server Error

---

## 🔄 Verificación en Stripe Dashboard

Después de ejecutar las actualizaciones:

1. **Acceder a Stripe Dashboard:**
   - https://dashboard.stripe.com/test/customers

2. **Buscar el customer por ID:**
   - Buscar `cus_PQx1yZ2aBcDeFgHi`

3. **✅ Verificar que los datos estén actualizados:**
   - Nombre actualizado
   - Email actualizado
   - Teléfono actualizado
   - Dirección actualizada
   - Metadata actualizado

---

## 📋 Checklist de Validación Completa

### Funcionalidad
- [x] Endpoint `PUT /api/customers` existe
- [x] Recibe `customer_id` (cus_xxx)
- [x] Recibe campos opcionales: name, email, phone, address
- [x] Actualiza solo los campos enviados
- [x] Actualiza dirección completa cuando se envía
- [x] Actualiza metadata personalizado
- [x] Llama correctamente a Stripe API

### Validaciones
- [x] Valida customer_id requerido
- [x] Valida formato de customer_id (cus_xxx)
- [x] Maneja customer no encontrado
- [x] Maneja errores de Stripe

### Respuesta
- [x] Devuelve `success: true` en éxito
- [x] Devuelve todos los datos actualizados
- [x] Incluye customer_id y user_id
- [x] Devuelve `error_message` en fallos
- [x] Códigos HTTP apropiados (200, 400, 500)

### Integración
- [x] Compatible con llamadas desde Laravel
- [x] Formato JSON estándar
- [x] Documentación disponible

---

## 📖 Documentación Relacionada

- **Guía completa:** `ECommerceAPI\docs\CUSTOMERS_API_GUIDE.md`
- **Modelos:** `ECommerceAPI\Models\CustomerModels.cs`
- **Servicio:** `ECommerceAPI\Services\StripeCustomerService.cs`
- **Controlador:** `ECommerceAPI\Controllers\CustomersController.cs`

---

## ✅ Resumen

**El Requerimiento 5 está completamente implementado y funcional.**

El endpoint `PUT /api/customers`:
1. ✅ Recibe el `customer_id` (cus_xxx) desde Laravel
2. ✅ Recibe los nuevos datos (nombre, correo, dirección, etc.)
3. ✅ Actualiza el customer en Stripe
4. ✅ Devuelve confirmación de actualización con todos los datos

**No se requieren cambios ni ajustes adicionales.**

---

**Estado final:** ✅ COMPLETADO  
**Fecha:** Enero 2025
