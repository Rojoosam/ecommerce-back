using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers;

/// <summary>
/// Controlador para recibir y procesar webhooks de Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WebhooksController : ControllerBase
{
    private readonly IStripeWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IStripeWebhookService webhookService,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint para recibir webhooks de Stripe
    /// </summary>
    /// <remarks>
    /// Este endpoint recibe eventos de Stripe, valida la firma y procesa los siguientes eventos:
    /// - payment_intent.succeeded
    /// - payment_intent.payment_failed
    /// - payment_intent.canceled
    /// - charge.refunded
    /// - charge.dispute.created
    /// 
    /// Configura este endpoint en tu Stripe Dashboard: https://dashboard.stripe.com/webhooks
    /// URL del webhook: https://tu-dominio.com/api/webhooks/stripe
    /// </remarks>
    /// <response code="200">Webhook procesado exitosamente</response>
    /// <response code="400">Firma inválida o error de validación</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("stripe")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WebhookResponse>> StripeWebhook()
    {
        try
        {
            // Leer el cuerpo del request (JSON del evento)
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();

            // Obtener la firma del header
            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogWarning("Webhook recibido sin firma (Stripe-Signature header)");
                return BadRequest(new WebhookResponse
                {
                    Success = false,
                    ErrorMessage = "Falta el header 'Stripe-Signature'"
                });
            }

            _logger.LogInformation("Webhook recibido de Stripe");

            // Procesar el webhook
            var response = await _webhookService.ProcessWebhookAsync(json, signature);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al procesar webhook: {Error}",
                    response.ErrorMessage);

                // Si es un error de firma, devolver 400
                if (response.ErrorMessage?.Contains("Firma inválida") == true ||
                    response.ErrorMessage?.Contains("Webhook secret no configurado") == true)
                {
                    return BadRequest(response);
                }

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Webhook procesado exitosamente. EventId: {EventId}, Type: {EventType}, SentToLaravel: {Sent}",
                response.EventId,
                response.EventType,
                response.SentToLaravel);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar webhook de Stripe");

            return StatusCode(500, new WebhookResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Endpoint de prueba para verificar que el servicio de webhooks está funcionando
    /// </summary>
    /// <remarks>
    /// Este endpoint es solo para testing y debería deshabilitarse en producción
    /// </remarks>
    /// <response code="200">Servicio funcionando correctamente</response>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "Stripe Webhooks",
            timestamp = DateTime.UtcNow,
            message = "Servicio de webhooks funcionando correctamente"
        });
    }

    /// <summary>
    /// Endpoint para simular el reenvío de una notificación a Laravel (solo para testing)
    /// </summary>
    /// <remarks>
    /// Este endpoint es solo para testing y debería deshabilitarse en producción
    /// </remarks>
    /// <param name="notification">Notificación de prueba a enviar</param>
    /// <response code="200">Notificación enviada exitosamente</response>
    /// <response code="500">Error al enviar notificación</response>
    [HttpPost("test-laravel-notification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestLaravelNotification([FromBody] WebhookNotification notification)
    {
        try
        {
            _logger.LogInformation("Enviando notificación de prueba a Laravel");

            var success = await _webhookService.SendNotificationToLaravelAsync(notification);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "Notificación enviada exitosamente a Laravel"
                });
            }

            return StatusCode(500, new
            {
                success = false,
                message = "No se pudo enviar la notificación a Laravel"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación de prueba a Laravel");
            return StatusCode(500, new
            {
                success = false,
                message = $"Error: {ex.Message}"
            });
        }
    }
}
