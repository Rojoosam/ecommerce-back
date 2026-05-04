namespace ECommerceAPI.Models;

/// <summary>
/// Solicitud para crear y confirmar un Payment Intent
/// </summary>
public class CreatePaymentIntentRequest
{
    /// <summary>
    /// ID del Customer en Stripe (cus_xxx)
    /// </summary>
    public required string CustomerId { get; set; }

    /// <summary>
    /// ID del Payment Method en Stripe (pm_xxx)
    /// </summary>
    public required string PaymentMethodId { get; set; }

    /// <summary>
    /// Monto a cobrar (en unidades mayores: 100.00 = $100.00)
    /// </summary>
    public required decimal Amount { get; set; }

    /// <summary>
    /// Moneda del cargo (usd, mxn, etc.)
    /// </summary>
    public required string Currency { get; set; }

    /// <summary>
    /// ID interno del pedido en Laravel (para referencia)
    /// </summary>
    public required string OrderId { get; set; }

    /// <summary>
    /// Descripción del cargo (opcional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Metadata adicional (opcional)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Confirmar automáticamente el Payment Intent (default: true)
    /// </summary>
    public bool AutoConfirm { get; set; } = true;

    /// <summary>
    /// Capturar el pago automáticamente (default: true)
    /// Si es false, el pago se autoriza pero no se captura hasta llamar a Capture
    /// </summary>
    public bool AutoCapture { get; set; } = true;
}

/// <summary>
/// Solicitud para cancelar un Payment Intent
/// </summary>
public class CancelPaymentIntentRequest
{
    /// <summary>
    /// ID del Payment Intent en Stripe (pi_xxx)
    /// </summary>
    public required string PaymentIntentId { get; set; }

    /// <summary>
    /// Razón de cancelación (opcional)
    /// </summary>
    public string? CancellationReason { get; set; }
}

/// <summary>
/// Solicitud para capturar un Payment Intent autorizado
/// </summary>
public class CapturePaymentIntentRequest
{
    /// <summary>
    /// ID del Payment Intent en Stripe (pi_xxx)
    /// </summary>
    public required string PaymentIntentId { get; set; }

    /// <summary>
    /// Monto a capturar (opcional, si es menor al autorizado)
    /// </summary>
    public decimal? AmountToCapture { get; set; }
}

/// <summary>
/// Respuesta de Payment Intent
/// </summary>
public class PaymentIntentResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Payment Intent en Stripe (pi_xxx)
    /// </summary>
    public string? PaymentIntentId { get; set; }

    /// <summary>
    /// ID del Customer asociado (cus_xxx)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// ID del Payment Method usado (pm_xxx)
    /// </summary>
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// ID interno del pedido de Laravel
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Estado del Payment Intent
    /// (requires_payment_method, requires_confirmation, requires_action, processing, requires_capture, canceled, succeeded)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Monto del cargo (en unidades menores: 10000 = $100.00)
    /// </summary>
    public long? Amount { get; set; }

    /// <summary>
    /// Monto en unidades mayores (100.00 = $100.00)
    /// </summary>
    public decimal? AmountDecimal { get; set; }

    /// <summary>
    /// Monto capturado (en unidades menores)
    /// </summary>
    public long? AmountCaptured { get; set; }

    /// <summary>
    /// Monto capturado en unidades mayores
    /// </summary>
    public decimal? AmountCapturedDecimal { get; set; }

    /// <summary>
    /// Moneda del cargo (usd, mxn, etc.)
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Descripción del cargo
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Fecha de creación
    /// </summary>
    public DateTime? Created { get; set; }

    /// <summary>
    /// Client secret para acciones del cliente (si es necesario)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Detalles del cargo (si el pago fue exitoso)
    /// </summary>
    public ChargeDetails? Charge { get; set; }

    /// <summary>
    /// Metadata del Payment Intent
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Mensaje de error (si hubo algún problema)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Código de error de Stripe (si aplica)
    /// </summary>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Detalles del cargo realizado
/// </summary>
public class ChargeDetails
{
    /// <summary>
    /// ID del Charge en Stripe (ch_xxx)
    /// </summary>
    public string? ChargeId { get; set; }

    /// <summary>
    /// Monto cobrado (en unidades menores)
    /// </summary>
    public long? Amount { get; set; }

    /// <summary>
    /// Monto cobrado en unidades mayores
    /// </summary>
    public decimal? AmountDecimal { get; set; }

    /// <summary>
    /// Moneda
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Estado del cargo (succeeded, pending, failed)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Fecha del cargo
    /// </summary>
    public DateTime? Created { get; set; }

    /// <summary>
    /// Indica si fue capturado
    /// </summary>
    public bool Captured { get; set; }

    /// <summary>
    /// Número de recibo de Stripe
    /// </summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Datos de la tarjeta usada
    /// </summary>
    public CardDetails? Card { get; set; }
}

/// <summary>
/// Respuesta al cancelar un Payment Intent
/// </summary>
public class CancelPaymentIntentResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Payment Intent cancelado (pi_xxx)
    /// </summary>
    public string? PaymentIntentId { get; set; }

    /// <summary>
    /// Estado actualizado (debe ser "canceled")
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Razón de cancelación
    /// </summary>
    public string? CancellationReason { get; set; }

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
/// Respuesta al capturar un Payment Intent
/// </summary>
public class CapturePaymentIntentResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del Payment Intent capturado (pi_xxx)
    /// </summary>
    public string? PaymentIntentId { get; set; }

    /// <summary>
    /// Estado actualizado (debe ser "succeeded")
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Monto capturado (en unidades mayores)
    /// </summary>
    public decimal? AmountCaptured { get; set; }

    /// <summary>
    /// Moneda
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Mensaje de la operación
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Mensaje de error (si hubo algún problema)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
