namespace ECommerceAPI.Models;

/// <summary>
/// Solicitud para crear un Customer en Stripe
/// </summary>
public class CreateCustomerRequest
{
    /// <summary>
    /// ID interno del usuario en Laravel (para referencia)
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Nombre completo del cliente
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Correo electrónico del cliente
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Teléfono del cliente (opcional)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Dirección del cliente (opcional)
    /// </summary>
    public CustomerAddress? Address { get; set; }

    /// <summary>
    /// Metadata adicional (opcional)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Solicitud para actualizar un Customer en Stripe
/// </summary>
public class UpdateCustomerRequest
{
    /// <summary>
    /// ID del Customer en Stripe (cus_xxx)
    /// </summary>
    public required string CustomerId { get; set; }

    /// <summary>
    /// Nombre completo del cliente (opcional para actualización)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Correo electrónico del cliente (opcional para actualización)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Teléfono del cliente (opcional para actualización)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Dirección del cliente (opcional para actualización)
    /// </summary>
    public CustomerAddress? Address { get; set; }

    /// <summary>
    /// Metadata adicional (opcional)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Dirección del cliente
/// </summary>
public class CustomerAddress
{
    /// <summary>
    /// Línea 1 de la dirección
    /// </summary>
    public string? Line1 { get; set; }

    /// <summary>
    /// Línea 2 de la dirección (opcional)
    /// </summary>
    public string? Line2 { get; set; }

    /// <summary>
    /// Ciudad
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Estado/Provincia
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Código postal
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Código de país (ISO 3166-1 alpha-2)
    /// </summary>
    public string? Country { get; set; }
}

/// <summary>
/// Respuesta al crear o actualizar un Customer
/// </summary>
public class CustomerResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Customer en Stripe (cus_xxx)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// ID interno del usuario en Laravel (devuelto para asignación)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Nombre del cliente
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Correo electrónico del cliente
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Teléfono del cliente
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Dirección del cliente
    /// </summary>
    public CustomerAddress? Address { get; set; }

    /// <summary>
    /// Fecha de creación del customer en Stripe
    /// </summary>
    public DateTime? Created { get; set; }

    /// <summary>
    /// Indica si el customer está activo o eliminado
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Mensaje de error (si hubo algún problema)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Metadata adicional
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Respuesta al eliminar un Customer
/// </summary>
public class DeleteCustomerResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Customer eliminado en Stripe (cus_xxx)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Indica si el customer fue eliminado en Stripe
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// Mensaje de la operación
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Mensaje de error (si hubo algún problema)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Respuesta al obtener detalles de un Customer
/// </summary>
public class GetCustomerResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Customer en Stripe (cus_xxx)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Nombre del cliente
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Correo electrónico del cliente
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Teléfono del cliente
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Dirección del cliente
    /// </summary>
    public CustomerAddress? Address { get; set; }

    /// <summary>
    /// Fecha de creación del customer en Stripe
    /// </summary>
    public DateTime? Created { get; set; }

    /// <summary>
    /// Balance actual del customer en Stripe (en centavos)
    /// </summary>
    public long? Balance { get; set; }

    /// <summary>
    /// Moneda del balance
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Indica si el customer está activo o eliminado
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Metadata adicional
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Mensaje de error (si hubo algún problema)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
