using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

/// <summary>
/// Solicitud de reembolso
/// </summary>
public class RefundRequest
{
    /// <summary>
    /// Monto a reembolsar (opcional, si no se especifica se reembolsa el total)
    /// </summary>
    [Range(0.01, 1000000)]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Razón del reembolso
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Respuesta de reembolso
/// </summary>
public class RefundResponse
{
    /// <summary>
    /// Identificador único del reembolso
    /// </summary>
    public string RefundId { get; set; } = null!;

    /// <summary>
    /// Identificador de la transacción original
    /// </summary>
    public string OriginalTransactionId { get; set; } = null!;

    /// <summary>
    /// Estado del reembolso
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Monto reembolsado
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Moneda
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Mensaje descriptivo
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Fecha y hora del reembolso
    /// </summary>
    public DateTime Timestamp { get; set; }
}
