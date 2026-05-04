namespace ECommerceAPI.Models;

/// <summary>
/// Request que se recibe en el endpoint de webhook desde Stripe
/// </summary>
public class StripeWebhookRequest
{
    /// <summary>
    /// JSON payload del evento de Stripe
    /// </summary>
    public required string Json { get; set; }

    /// <summary>
    /// Firma del webhook enviada por Stripe (Stripe-Signature header)
    /// </summary>
    public required string Signature { get; set; }
}

/// <summary>
/// Tipos de eventos de Stripe que procesamos
/// </summary>
public enum StripeEventType
{
    /// <summary>
    /// Payment Intent completado exitosamente
    /// </summary>
    PaymentIntentSucceeded,

    /// <summary>
    /// Payment Intent falló
    /// </summary>
    PaymentIntentPaymentFailed,

    /// <summary>
    /// Payment Intent cancelado
    /// </summary>
    PaymentIntentCanceled,

    /// <summary>
    /// Cargo reembolsado
    /// </summary>
    ChargeRefunded,

    /// <summary>
    /// Disputa creada
    /// </summary>
    ChargeDisputeCreated,

    /// <summary>
    /// Evento no soportado
    /// </summary>
    Unknown
}

/// <summary>
/// Notificación procesada para enviar a Laravel
/// </summary>
public class WebhookNotification
{
    /// <summary>
    /// ID del evento de Stripe
    /// </summary>
    public required string EventId { get; set; }

    /// <summary>
    /// Tipo de evento
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Fecha y hora del evento en Stripe
    /// </summary>
    public DateTime EventCreated { get; set; }

    /// <summary>
    /// ID del Payment Intent (si aplica)
    /// </summary>
    public string? PaymentIntentId { get; set; }

    /// <summary>
    /// ID del Charge (si aplica)
    /// </summary>
    public string? ChargeId { get; set; }

    /// <summary>
    /// ID del Refund (si aplica)
    /// </summary>
    public string? RefundId { get; set; }

    /// <summary>
    /// ID del Customer (si aplica)
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Monto involucrado (en centavos)
    /// </summary>
    public long? Amount { get; set; }

    /// <summary>
    /// Moneda del monto
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Estado actual (succeeded, failed, canceled, refunded, etc.)
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Razón de fallo o cancelación (si aplica)
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Mensaje de error (si aplica)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Metadata del Payment Intent u objeto relacionado
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Datos adicionales del evento
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Respuesta del procesamiento del webhook
/// </summary>
public class WebhookResponse
{
    /// <summary>
    /// Indica si el webhook fue procesado exitosamente
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID del evento procesado
    /// </summary>
    public string? EventId { get; set; }

    /// <summary>
    /// Tipo de evento procesado
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Mensaje de resultado
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Indica si la notificación fue enviada a Laravel
    /// </summary>
    public bool SentToLaravel { get; set; }

    /// <summary>
    /// URL de Laravel a la que se envió la notificación
    /// </summary>
    public string? LaravelNotificationUrl { get; set; }

    /// <summary>
    /// Mensaje de error (si hubo algún problema)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Configuración para enviar notificaciones a Laravel
/// </summary>
public class LaravelNotificationSettings
{
    /// <summary>
    /// URL base de Laravel
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint específico para recibir webhooks
    /// </summary>
    public string WebhookEndpoint { get; set; } = "/api/stripe/webhook-notification";

    /// <summary>
    /// Token de autenticación para llamadas a Laravel
    /// </summary>
    public string? AuthToken { get; set; }

    /// <summary>
    /// Timeout en segundos para las llamadas HTTP
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Número de reintentos en caso de fallo
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Habilitar o deshabilitar el envío a Laravel
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Endpoint específico para notificaciones de pagos
    /// </summary>
    public string PaymentsWebhookEndpoint { get; set; } = "/webhooks/payments";

    /// <summary>
    /// Endpoint específico para notificaciones de reembolsos
    /// </summary>
    public string RefundsWebhookEndpoint { get; set; } = "/webhooks/refunds";

    /// <summary>
    /// Endpoint específico para notificaciones de métodos de pago
    /// </summary>
    public string PaymentMethodsWebhookEndpoint { get; set; } = "/webhooks/payment-methods";
}

/// <summary>
/// Payload de notificación de pagos para Laravel
/// Formato esperado por POST /webhooks/payments
/// </summary>
public class LaravelPaymentNotification
{
    /// <summary>
    /// ID de la orden en Laravel (extraído de metadata)
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// ID de la transacción en Laravel (extraído de metadata)
    /// </summary>
    public int? TransactionId { get; set; }

    /// <summary>
    /// ID externo del PaymentIntent en Stripe (pi_xxx)
    /// </summary>
    public required string ExternalId { get; set; }

    /// <summary>
    /// Estado del pago (succeeded, failed, canceled)
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Proveedor de pago
    /// </summary>
    public string Provider { get; set; } = "stripe";

    /// <summary>
    /// Moneda del pago (en mayúsculas: MXN, USD, etc.)
    /// </summary>
    public required string Currency { get; set; }

    /// <summary>
    /// Monto del pago en unidades base de la moneda (pesos, dólares, etc.)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Información del método de pago utilizado
    /// </summary>
    public LaravelPaymentMethodInfo? PaymentMethod { get; set; }
}

/// <summary>
/// Información del método de pago para notificación a Laravel
/// </summary>
public class LaravelPaymentMethodInfo
{
    /// <summary>
    /// ID externo del PaymentMethod en Stripe (pm_xxx)
    /// </summary>
    public required string ExternalId { get; set; }

    /// <summary>
    /// Marca de la tarjeta (Visa, Mastercard, etc.)
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Últimos 4 dígitos de la tarjeta
    /// </summary>
    public string? LastFour { get; set; }

    /// <summary>
    /// Mes de expiración
    /// </summary>
    public string? ExpMonth { get; set; }

    /// <summary>
    /// Año de expiración
    /// </summary>
    public string? ExpYear { get; set; }
}

/// <summary>
/// Payload de notificación de reembolsos para Laravel
/// Formato esperado por POST /webhooks/refunds
/// </summary>
public class LaravelRefundNotification
{
    /// <summary>
    /// ID de la orden en Laravel (extraído de metadata)
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// ID de la transacción en Laravel (extraído de metadata)
    /// </summary>
    public int? TransactionId { get; set; }

    /// <summary>
    /// ID externo del PaymentIntent en Stripe (pi_xxx)
    /// </summary>
    public required string ExternalId { get; set; }

    /// <summary>
    /// Estado del reembolso (refunded)
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Proveedor de pago
    /// </summary>
    public string Provider { get; set; } = "stripe";

    /// <summary>
    /// Moneda del reembolso (en mayúsculas: MXN, USD, etc.)
    /// </summary>
    public required string Currency { get; set; }

    /// <summary>
    /// Monto del reembolso en unidades base de la moneda (pesos, dólares, etc.)
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Payload de notificación de métodos de pago para Laravel
/// Formato esperado por POST /webhooks/payment-methods
/// </summary>
public class LaravelPaymentMethodNotification
{
    /// <summary>
    /// ID del usuario en Laravel (extraído del metadata del Customer en Stripe)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// ID externo del PaymentMethod en Stripe (pm_xxx)
    /// </summary>
    public required string ExternalId { get; set; }

    /// <summary>
    /// Marca de la tarjeta (Visa, MasterCard, etc.)
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Últimos 4 dígitos de la tarjeta
    /// </summary>
    public string? LastFour { get; set; }

    /// <summary>
    /// Mes de expiración (con cero inicial: "01"-"12")
    /// </summary>
    public string? ExpMonth { get; set; }

    /// <summary>
    /// Año de expiración
    /// </summary>
    public string? ExpYear { get; set; }

    /// <summary>
    /// Acción realizada: "attached" o "detached"
    /// </summary>
    public required string Action { get; set; }
}

/// <summary>
/// Detalles de un evento de Payment Intent exitoso
/// </summary>
public class PaymentIntentSucceededEvent
{
    public required string PaymentIntentId { get; set; }
    public long Amount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public string? CustomerId { get; set; }
    public string? ChargeId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Detalles de un evento de Payment Intent fallido
/// </summary>
public class PaymentIntentFailedEvent
{
    public required string PaymentIntentId { get; set; }
    public long Amount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public string? CustomerId { get; set; }
    public string? LastPaymentErrorCode { get; set; }
    public string? LastPaymentErrorMessage { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Detalles de un evento de Payment Intent cancelado
/// </summary>
public class PaymentIntentCanceledEvent
{
    public required string PaymentIntentId { get; set; }
    public long Amount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public string? CustomerId { get; set; }
    public string? CancellationReason { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Detalles de un evento de Charge reembolsado
/// </summary>
public class ChargeRefundedEvent
{
    public required string ChargeId { get; set; }
    public required string RefundId { get; set; }
    public long Amount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? CustomerId { get; set; }
    public string? Reason { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Detalles de un evento de Disputa creada
/// </summary>
public class ChargeDisputeCreatedEvent
{
    public required string DisputeId { get; set; }
    public required string ChargeId { get; set; }
    public long Amount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public required string Reason { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? CustomerId { get; set; }
    public DateTime EvidenceDueBy { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
