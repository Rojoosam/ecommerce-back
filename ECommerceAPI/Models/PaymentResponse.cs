namespace ECommerceAPI.Models;

/// <summary>
/// Respuesta de una transacción de pago
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// Identificador único de la transacción
    /// </summary>
    public string TransactionId { get; set; } = null!;

    /// <summary>
    /// Estado de la transacción
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Monto procesado
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Moneda utilizada
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Pasarela utilizada
    /// </summary>
    public PaymentGateway Gateway { get; set; }

    /// <summary>
    /// Código de autorización (si fue aprobada)
    /// </summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// Últimos 4 dígitos de la tarjeta
    /// </summary>
    public string? CardLastFour { get; set; }

    /// <summary>
    /// Tipo de tarjeta detectada
    /// </summary>
    public CardType? CardType { get; set; }

    /// <summary>
    /// Código de error (si falló)
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Fecha y hora de la transacción
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Tiempo de procesamiento en milisegundos
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Respuesta al consultar el estado de una transacción
/// </summary>
public class TransactionStatusResponse
{
    public string TransactionId { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public PaymentGateway Gateway { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RefundId { get; set; }
}
