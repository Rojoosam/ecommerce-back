namespace ECommerceAPI.Models;

/// <summary>
/// Estados posibles de una transacción de pago
/// </summary>
public enum PaymentStatus
{
    Pending,
    Processing,
    Approved,
    Declined,
    Failed,
    Refunded,
    Cancelled
}

/// <summary>
/// Pasarelas de pago disponibles
/// </summary>
public enum PaymentGateway
{
    Stripe,
    PayPal,
    MercadoPago
}

/// <summary>
/// Tipos de tarjeta soportados
/// </summary>
public enum CardType
{
    Visa,
    MasterCard,
    AmericanExpress,
    Unknown
}
