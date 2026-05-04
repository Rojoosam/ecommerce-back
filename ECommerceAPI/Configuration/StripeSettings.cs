namespace ECommerceAPI.Configuration;

/// <summary>
/// Configuración para Stripe
/// </summary>
public class StripeSettings
{
    /// <summary>
    /// Secret Key de Stripe (usar en el servidor)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Publishable Key de Stripe (usar en el cliente)
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret para validar webhooks de Stripe
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
