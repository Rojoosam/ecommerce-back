using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers;

/// <summary>
/// Controlador para gestionar reembolsos en Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RefundsController : ControllerBase
{
    private readonly IStripeRefundService _refundService;
    private readonly ILogger<RefundsController> _logger;

    public RefundsController(
        IStripeRefundService refundService,
        ILogger<RefundsController> logger)
    {
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Crear un reembolso para un Payment Intent
    /// </summary>
    /// <param name="paymentIntentId">ID del Payment Intent (pi_xxx)</param>
    /// <param name="request">Datos del reembolso</param>
    /// <returns>Información del reembolso creado</returns>
    /// <response code="200">Reembolso creado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error del servidor</response>
    [HttpPost("payment-intent/{paymentIntentId}")]
    [ProducesResponseType(typeof(RefundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RefundResponse>> CreateRefundForPaymentIntent(
        string paymentIntentId,
        [FromBody] RefundRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creando reembolso para Payment Intent: {PaymentIntentId}, Monto: {Amount}",
                paymentIntentId,
                request.Amount?.ToString() ?? "Total");

            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                return BadRequest(new { error = "El Payment Intent ID es requerido" });
            }

            if (!paymentIntentId.StartsWith("pi_"))
            {
                return BadRequest(new { error = "El Payment Intent ID debe comenzar con 'pi_'" });
            }

            var response = await _refundService.CreateRefundForPaymentIntentAsync(paymentIntentId, request);

            _logger.LogInformation(
                "Reembolso creado exitosamente. RefundId: {RefundId}, Estado: {Status}",
                response.RefundId,
                response.Status);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error al crear reembolso para Payment Intent: {PaymentIntentId}", paymentIntentId);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear reembolso");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear un reembolso para un Charge
    /// </summary>
    /// <param name="chargeId">ID del Charge (ch_xxx)</param>
    /// <param name="request">Datos del reembolso</param>
    /// <returns>Información del reembolso creado</returns>
    /// <response code="200">Reembolso creado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error del servidor</response>
    [HttpPost("charge/{chargeId}")]
    [ProducesResponseType(typeof(RefundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RefundResponse>> CreateRefundForCharge(
        string chargeId,
        [FromBody] RefundRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creando reembolso para Charge: {ChargeId}, Monto: {Amount}",
                chargeId,
                request.Amount?.ToString() ?? "Total");

            if (string.IsNullOrWhiteSpace(chargeId))
            {
                return BadRequest(new { error = "El Charge ID es requerido" });
            }

            if (!chargeId.StartsWith("ch_"))
            {
                return BadRequest(new { error = "El Charge ID debe comenzar con 'ch_'" });
            }

            var response = await _refundService.CreateRefundForChargeAsync(chargeId, request);

            _logger.LogInformation(
                "Reembolso creado exitosamente. RefundId: {RefundId}, Estado: {Status}",
                response.RefundId,
                response.Status);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error al crear reembolso para Charge: {ChargeId}", chargeId);
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear reembolso");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener información de un reembolso específico
    /// </summary>
    /// <param name="refundId">ID del reembolso (re_xxx)</param>
    /// <returns>Información del reembolso</returns>
    /// <response code="200">Reembolso encontrado</response>
    /// <response code="404">Reembolso no encontrado</response>
    /// <response code="500">Error del servidor</response>
    [HttpGet("{refundId}")]
    [ProducesResponseType(typeof(RefundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RefundResponse>> GetRefund(string refundId)
    {
        try
        {
            _logger.LogInformation("Obteniendo información del reembolso: {RefundId}", refundId);

            if (string.IsNullOrWhiteSpace(refundId))
            {
                return BadRequest(new { error = "El Refund ID es requerido" });
            }

            if (!refundId.StartsWith("re_"))
            {
                return BadRequest(new { error = "El Refund ID debe comenzar con 're_'" });
            }

            var response = await _refundService.GetRefundAsync(refundId);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error al obtener reembolso: {RefundId}", refundId);
            
            if (ex.Message.Contains("No such refund"))
            {
                return NotFound(new { error = $"Reembolso no encontrado: {refundId}" });
            }

            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener reembolso");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Listar todos los reembolsos
    /// </summary>
    /// <param name="limit">Número máximo de resultados (default: 10, max: 100)</param>
    /// <returns>Lista de reembolsos</returns>
    /// <response code="200">Lista de reembolsos obtenida</response>
    /// <response code="500">Error del servidor</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<RefundResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<RefundResponse>>> ListRefunds([FromQuery] int limit = 10)
    {
        try
        {
            // Validar límite
            if (limit < 1 || limit > 100)
            {
                return BadRequest(new { error = "El límite debe estar entre 1 y 100" });
            }

            _logger.LogInformation("Listando reembolsos con límite: {Limit}", limit);

            var response = await _refundService.ListRefundsAsync(limit);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar reembolsos");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }
}
