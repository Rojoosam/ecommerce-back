namespace ECommerceAPI.Models;

/// <summary>
/// Información de una pasarela de pago disponible
/// </summary>
public class GatewayInfo
{
    /// <summary>
    /// Identificador de la pasarela
    /// </summary>
    public PaymentGateway Gateway { get; set; }

    /// <summary>
    /// Nombre legible de la pasarela
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Descripción de la pasarela
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Monedas soportadas
    /// </summary>
    public List<string> SupportedCurrencies { get; set; } = [];

    /// <summary>
    /// Tipos de tarjeta soportados
    /// </summary>
    public List<CardType> SupportedCardTypes { get; set; } = [];

    /// <summary>
    /// Indica si la pasarela está activa
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Indica que es un entorno de simulación
    /// </summary>
    public bool IsSimulated { get; set; } = true;
}
