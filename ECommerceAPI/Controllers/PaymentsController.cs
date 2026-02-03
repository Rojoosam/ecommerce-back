using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers;

/// <summary>
/// Controlador de pagos - Microservicio de pasarela de pagos simulado
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentGatewayFactory gatewayFactory, ILogger<PaymentsController> logger)
    {
        _gatewayFactory = gatewayFactory;
        _logger = logger;
    }

    /// <summary>
    /// Procesa un pago a través de la pasarela especificada
    /// </summary>
    /// <param name="request">Datos del pago</param>
    /// <returns>Resultado de la transacción</returns>
    /// <remarks>
    /// Tarjetas de prueba:
    /// - 4242424242424242: Pago exitoso
    /// - 4000000000000002: Tarjeta declinada
    /// - 4000000000000069: Tarjeta expirada
    /// - 4000000000000127: CVC incorrecto
    /// - 4000000000009995: Fondos insuficientes
    /// - 5555555555554444: MasterCard exitosa
    /// - 378282246310005: Amex exitosa
    /// </remarks>
    [HttpPost("process")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentResponse>> ProcessPayment([FromBody] PaymentRequest request)
    {
        _logger.LogInformation(
            "Procesando pago de {Amount} {Currency} a través de {Gateway}",
            request.Amount,
            request.Currency,
            request.Gateway);

        try
        {
            var gateway = _gatewayFactory.GetGateway(request.Gateway);
            var response = await gateway.ProcessPaymentAsync(request);

            _logger.LogInformation(
                "Transacción {TransactionId} completada con estado {Status}",
                response.TransactionId,
                response.Status);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Pasarela no válida: {Gateway}", request.Gateway);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el estado de una transacción
    /// </summary>
    /// <param name="id">ID de la transacción</param>
    /// <returns>Estado actual de la transacción</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransactionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TransactionStatusResponse> GetTransactionStatus(string id)
    {
        var transaction = BasePaymentSimulator.GetTransaction(id);

        if (transaction == null)
        {
            return NotFound(new { error = "Transacción no encontrada", transactionId = id });
        }

        return Ok(new TransactionStatusResponse
        {
            TransactionId = transaction.TransactionId,
            Status = transaction.Status,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Gateway = transaction.Gateway,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            RefundId = transaction.RefundId
        });
    }

    /// <summary>
    /// Procesa un reembolso para una transacción existente
    /// </summary>
    /// <param name="id">ID de la transacción a reembolsar</param>
    /// <param name="request">Datos del reembolso</param>
    /// <returns>Resultado del reembolso</returns>
    [HttpPost("{id}/refund")]
    [ProducesResponseType(typeof(RefundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RefundResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RefundResponse>> ProcessRefund(string id, [FromBody] RefundRequest request)
    {
        _logger.LogInformation("Procesando reembolso para transacción {TransactionId}", id);

        var transaction = BasePaymentSimulator.GetTransaction(id);

        if (transaction == null)
        {
            return NotFound(new { error = "Transacción no encontrada", transactionId = id });
        }

        var gateway = _gatewayFactory.GetGateway(transaction.Gateway);
        var response = await gateway.ProcessRefundAsync(id, request);

        if (response.Status == PaymentStatus.Failed)
        {
            return BadRequest(response);
        }

        _logger.LogInformation(
            "Reembolso {RefundId} procesado para transacción {TransactionId}",
            response.RefundId,
            id);

        return Ok(response);
    }

    /// <summary>
    /// Lista las pasarelas de pago disponibles
    /// </summary>
    /// <returns>Lista de pasarelas disponibles con su información</returns>
    [HttpGet("gateways")]
    [ProducesResponseType(typeof(IEnumerable<GatewayInfo>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<GatewayInfo>> GetAvailableGateways()
    {
        var gateways = _gatewayFactory.GetAllGateways();
        return Ok(gateways);
    }

    /// <summary>
    /// Endpoint de salud del servicio
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            service = "Payment Gateway Simulator",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
