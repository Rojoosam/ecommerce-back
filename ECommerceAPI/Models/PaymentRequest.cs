using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models;

/// <summary>
/// Modelo de solicitud de pago
/// </summary>
public class PaymentRequest
{
    /// <summary>
    /// Monto a cobrar
    /// </summary>
    [Required]
    [Range(0.01, 1000000, ErrorMessage = "El monto debe estar entre 0.01 y 1,000,000")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Código de moneda (USD, MXN, EUR, etc.)
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "El código de moneda debe tener 3 caracteres")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Pasarela de pago a utilizar
    /// </summary>
    [Required]
    public PaymentGateway Gateway { get; set; }

    /// <summary>
    /// Información de la tarjeta (para simuladores)
    /// </summary>
    public CardInfo? Card { get; set; }

    /// <summary>
    /// Token de pago de Stripe (para producción, reemplaza Card)
    /// Obtenido desde Stripe.js en el cliente
    /// </summary>
    public string? StripeToken { get; set; }

    /// <summary>
    /// ID del Payment Method de Stripe (alternativa al token)
    /// </summary>
    public string? StripePaymentMethodId { get; set; }

    /// <summary>
    /// Descripción del pago (opcional)
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Identificador único del cliente
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Metadatos adicionales
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Información de tarjeta (simulada)
/// </summary>
public class CardInfo
{
    /// <summary>
    /// Número de tarjeta (usar números de prueba)
    /// - 4242424242424242: Éxito
    /// - 4000000000000002: Declinada
    /// - 4000000000000069: Tarjeta expirada
    /// - 4000000000000127: CVC incorrecto
    /// </summary>
    [Required]
    [StringLength(19, MinimumLength = 13)]
    public string Number { get; set; } = null!;

    /// <summary>
    /// Mes de expiración (1-12)
    /// </summary>
    [Required]
    [Range(1, 12)]
    public int ExpiryMonth { get; set; }

    /// <summary>
    /// Año de expiración (formato de 4 dígitos)
    /// </summary>
    [Required]
    [Range(2024, 2050)]
    public int ExpiryYear { get; set; }

    /// <summary>
    /// Código de seguridad (CVC/CVV)
    /// </summary>
    [Required]
    [StringLength(4, MinimumLength = 3)]
    public string Cvc { get; set; } = null!;

    /// <summary>
    /// Nombre del titular de la tarjeta
    /// </summary>
    [Required]
    [StringLength(100)]
    public string HolderName { get; set; } = null!;
}
