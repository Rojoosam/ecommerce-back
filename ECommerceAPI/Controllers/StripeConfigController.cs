using ECommerceAPI.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ECommerceAPI.Controllers;

/// <summary>
/// Controlador para configuración de Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StripeConfigController : ControllerBase
{
    private readonly StripeSettings _settings;

    public StripeConfigController(IOptions<StripeSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>
    /// Obtiene la clave pública de Stripe para uso en el cliente
    /// </summary>
    /// <returns>Clave pública de Stripe</returns>
    [HttpGet("publishable-key")]
    [ProducesResponseType(typeof(PublishableKeyResponse), StatusCodes.Status200OK)]
    public ActionResult<PublishableKeyResponse> GetPublishableKey()
    {
        return Ok(new PublishableKeyResponse
        {
            PublishableKey = _settings.PublishableKey
        });
    }
}

public class PublishableKeyResponse
{
    public string PublishableKey { get; set; } = string.Empty;
}
