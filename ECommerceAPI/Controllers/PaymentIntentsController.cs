using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers;

/// <summary>
/// Controlador para la gestión de Payment Intents (pagos únicos) en Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentIntentsController : ControllerBase
{
    private readonly IStripePaymentIntentService _paymentIntentService;
    private readonly ILogger<PaymentIntentsController> _logger;

    public PaymentIntentsController(
        IStripePaymentIntentService paymentIntentService,
        ILogger<PaymentIntentsController> logger)
    {
        _paymentIntentService = paymentIntentService;
        _logger = logger;
    }

    /// <summary>
    /// Crea y confirma un Payment Intent para procesar un pago único
    /// </summary>
    /// <param name="request">Datos del pago a procesar</param>
    /// <returns>Respuesta con el payment_intent_id, estado y datos del cargo</returns>
    /// <response code="200">Payment Intent creado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error al procesar el pago</response>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentIntentResponse>> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para crear Payment Intent. Customer: {CustomerId}, Order: {OrderId}, Monto: {Amount} {Currency}",
                request.CustomerId,
                request.OrderId,
                request.Amount,
                request.Currency);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(request.CustomerId))
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    ErrorMessage = "El campo 'customer_id' es requerido"
                });
            }

            if (!request.CustomerId.StartsWith("cus_"))
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El 'customer_id' debe tener el formato 'cus_xxx'"
                });
            }

            if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El campo 'payment_method_id' es requerido"
                });
            }

            if (!request.PaymentMethodId.StartsWith("pm_"))
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    PaymentMethodId = request.PaymentMethodId,
                    ErrorMessage = "El 'payment_method_id' debe tener el formato 'pm_xxx'"
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El monto debe ser mayor a 0"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Currency))
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El campo 'currency' es requerido"
                });
            }

            if (string.IsNullOrWhiteSpace(request.OrderId))
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El campo 'order_id' es requerido"
                });
            }

            var response = await _paymentIntentService.CreatePaymentIntentAsync(request);

            // Log según el resultado
            if (response.Success)
            {
                if (response.Status == "succeeded")
                {
                    _logger.LogInformation(
                        "✅ Payment Intent exitoso. PaymentIntentId: {PaymentIntentId}, Order: {OrderId}, Estado: {Status}",
                        response.PaymentIntentId,
                        response.OrderId,
                        response.Status);
                }
                else
                {
                    _logger.LogWarning(
                        "⚠️ Payment Intent creado pero requiere acción. PaymentIntentId: {PaymentIntentId}, Estado: {Status}",
                        response.PaymentIntentId,
                        response.Status);
                }

                return Ok(response);
            }
            else
            {
                _logger.LogWarning(
                    "❌ Error al crear Payment Intent. Customer: {CustomerId}, Order: {OrderId}, Error: {Error}",
                    request.CustomerId,
                    request.OrderId,
                    response.ErrorMessage);

                return StatusCode(500, response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de Payment Intent");

            return StatusCode(500, new PaymentIntentResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Obtiene los detalles de un Payment Intent
    /// </summary>
    /// <param name="paymentIntentId">ID del Payment Intent en Stripe (pi_xxx)</param>
    /// <returns>Detalles del Payment Intent</returns>
    /// <response code="200">Payment Intent encontrado</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="404">Payment Intent no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("{paymentIntentId}")]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentIntentResponse>> GetPaymentIntent(string paymentIntentId)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para obtener Payment Intent: {PaymentIntentId}",
                paymentIntentId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    ErrorMessage = "El 'payment_intent_id' es requerido"
                });
            }

            if (!paymentIntentId.StartsWith("pi_"))
            {
                return BadRequest(new PaymentIntentResponse
                {
                    Success = false,
                    PaymentIntentId = paymentIntentId,
                    ErrorMessage = "El 'payment_intent_id' debe tener el formato 'pi_xxx'"
                });
            }

            var response = await _paymentIntentService.GetPaymentIntentAsync(paymentIntentId);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al obtener Payment Intent: {PaymentIntentId}, Error: {Error}",
                    paymentIntentId,
                    response.ErrorMessage);

                if (response.ErrorMessage?.Contains("No such payment_intent") == true)
                {
                    return NotFound(response);
                }

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Payment Intent obtenido exitosamente: {PaymentIntentId}, Estado: {Status}",
                response.PaymentIntentId,
                response.Status);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de obtención de Payment Intent");

            return StatusCode(500, new PaymentIntentResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Cancela un Payment Intent que aún no ha sido confirmado o capturado
    /// </summary>
    /// <param name="request">Datos de la cancelación</param>
    /// <returns>Respuesta con el estado actualizado</returns>
    /// <response code="200">Payment Intent cancelado exitosamente</response>
    /// <response code="400">Solicitud inválida o Payment Intent no puede ser cancelado</response>
    /// <response code="404">Payment Intent no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(CancelPaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CancelPaymentIntentResponse>> CancelPaymentIntent([FromBody] CancelPaymentIntentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para cancelar Payment Intent: {PaymentIntentId}",
                request.PaymentIntentId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                return BadRequest(new CancelPaymentIntentResponse
                {
                    Success = false,
                    ErrorMessage = "El campo 'payment_intent_id' es requerido"
                });
            }

            if (!request.PaymentIntentId.StartsWith("pi_"))
            {
                return BadRequest(new CancelPaymentIntentResponse
                {
                    Success = false,
                    PaymentIntentId = request.PaymentIntentId,
                    ErrorMessage = "El 'payment_intent_id' debe tener el formato 'pi_xxx'"
                });
            }

            var response = await _paymentIntentService.CancelPaymentIntentAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al cancelar Payment Intent: {PaymentIntentId}, Error: {Error}",
                    request.PaymentIntentId,
                    response.ErrorMessage);

                if (response.ErrorMessage?.Contains("No such payment_intent") == true)
                {
                    return NotFound(response);
                }

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Payment Intent cancelado exitosamente: {PaymentIntentId}, Estado: {Status}",
                response.PaymentIntentId,
                response.Status);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de cancelación de Payment Intent");

            return StatusCode(500, new CancelPaymentIntentResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Captura un Payment Intent que fue autorizado pero no capturado
    /// </summary>
    /// <param name="request">Datos de la captura</param>
    /// <returns>Respuesta con el estado actualizado</returns>
    /// <response code="200">Payment Intent capturado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="404">Payment Intent no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("capture")]
    [ProducesResponseType(typeof(CapturePaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CapturePaymentIntentResponse>> CapturePaymentIntent([FromBody] CapturePaymentIntentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para capturar Payment Intent: {PaymentIntentId}",
                request.PaymentIntentId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                return BadRequest(new CapturePaymentIntentResponse
                {
                    Success = false,
                    ErrorMessage = "El campo 'payment_intent_id' es requerido"
                });
            }

            if (!request.PaymentIntentId.StartsWith("pi_"))
            {
                return BadRequest(new CapturePaymentIntentResponse
                {
                    Success = false,
                    PaymentIntentId = request.PaymentIntentId,
                    ErrorMessage = "El 'payment_intent_id' debe tener el formato 'pi_xxx'"
                });
            }

            if (request.AmountToCapture.HasValue && request.AmountToCapture.Value <= 0)
            {
                return BadRequest(new CapturePaymentIntentResponse
                {
                    Success = false,
                    PaymentIntentId = request.PaymentIntentId,
                    ErrorMessage = "El monto a capturar debe ser mayor a 0"
                });
            }

            var response = await _paymentIntentService.CapturePaymentIntentAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al capturar Payment Intent: {PaymentIntentId}, Error: {Error}",
                    request.PaymentIntentId,
                    response.ErrorMessage);

                if (response.ErrorMessage?.Contains("No such payment_intent") == true)
                {
                    return NotFound(response);
                }

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Payment Intent capturado exitosamente: {PaymentIntentId}, Monto: {Amount} {Currency}",
                response.PaymentIntentId,
                response.AmountCaptured,
                response.Currency?.ToUpper());

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de captura de Payment Intent");

            return StatusCode(500, new CapturePaymentIntentResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }
}
