namespace ECommerceAPI.Models;

/// <summary>
/// Solicitud para registrar un nuevo Payment Method
/// </summary>
public class AttachPaymentMethodRequest
{
    /// <summary>
    /// ID del Customer en Stripe (cus_xxx)
    /// </summary>
    public required string CustomerId { get; set; }

    /// <summary>
    /// Token efímero generado en frontend (tok_xxx) o Payment Method ID (pm_xxx)
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Metadata adicional (opcional)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Solicitud para desasociar un Payment Method
/// </summary>
public class DetachPaymentMethodRequest
{
    /// <summary>
    /// ID del Payment Method en Stripe (pm_xxx)
    /// </summary>
    public required string PaymentMethodId { get; set; }
}

/// <summary>
/// Respuesta al registrar o consultar un Payment Method
/// </summary>
public class PaymentMethodResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Payment Method en Stripe (pm_xxx)
    /// </summary>
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// ID del Customer asociado (cus_xxx)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Tipo de método de pago (card, etc.)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Datos públicos de la tarjeta
    /// </summary>
    public CardDetails? Card { get; set; }

    /// <summary>
    /// Fecha de creación
    /// </summary>
    public DateTime? Created { get; set; }

    /// <summary>
    /// Metadata del Payment Method
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Mensaje de error (si hubo algún problema)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Datos públicos de una tarjeta
/// </summary>
public class CardDetails
{
    /// <summary>
    /// Marca de la tarjeta (visa, mastercard, amex, etc.)
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Últimos 4 dígitos de la tarjeta
    /// </summary>
    public string? Last4 { get; set; }

    /// <summary>
    /// Mes de expiración (1-12)
    /// </summary>
    public long? ExpMonth { get; set; }

    /// <summary>
    /// Año de expiración (4 dígitos)
    /// </summary>
    public long? ExpYear { get; set; }

    /// <summary>
    /// País de emisión (ISO 3166-1 alpha-2)
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Tipo de financiamiento (credit, debit, prepaid, unknown)
    /// </summary>
    public string? Funding { get; set; }

    /// <summary>
    /// Nombre del titular (si está disponible)
    /// </summary>
    public string? CardholderName { get; set; }
}

/// <summary>
/// Respuesta al desasociar un Payment Method
/// </summary>
public class DetachPaymentMethodResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Payment Method desasociado (pm_xxx)
    /// </summary>
    public string? PaymentMethodId { get; set; }

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
/// Respuesta al listar Payment Methods de un Customer
/// </summary>
public class ListPaymentMethodsResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Customer (cus_xxx)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Lista de Payment Methods del Customer
    /// </summary>
    public List<PaymentMethodResponse>? PaymentMethods { get; set; }

    /// <summary>
    /// Cantidad total de Payment Methods
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Mensaje de error (si hubo algún problema)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Solicitud para actualizar el estado de un Customer
/// </summary>
public class UpdateCustomerStatusRequest
{
    /// <summary>
    /// ID del Customer en Stripe (cus_xxx)
    /// </summary>
    public required string CustomerId { get; set; }

    /// <summary>
    /// Estado activo/inactivo
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Si es false, también desasociar todos los Payment Methods
    /// </summary>
    public bool DetachPaymentMethods { get; set; } = true;
}

/// <summary>
/// Respuesta al actualizar el estado de un Customer
/// </summary>
public class UpdateCustomerStatusResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Customer (cus_xxx)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Estado actual (activo/inactivo)
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Cantidad de Payment Methods desasociados (si aplica)
    /// </summary>
    public int PaymentMethodsDetached { get; set; }

    /// <summary>
    /// Mensaje de la operación
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Mensaje de error (si hubo algún problema)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
