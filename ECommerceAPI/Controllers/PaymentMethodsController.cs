using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers;

/// <summary>
/// Controlador para la gestión de Payment Methods en Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IStripePaymentMethodService _paymentMethodService;
    private readonly ILogger<PaymentMethodsController> _logger;

    public PaymentMethodsController(
        IStripePaymentMethodService paymentMethodService,
        ILogger<PaymentMethodsController> logger)
    {
        _paymentMethodService = paymentMethodService;
        _logger = logger;
    }

    /// <summary>
    /// Registra y asocia un nuevo Payment Method a un Customer
    /// </summary>
    /// <param name="request">Datos del Payment Method a registrar</param>
    /// <returns>Respuesta con el payment_method_id y datos públicos de la tarjeta</returns>
    /// <response code="200">Payment Method registrado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("attach")]
    [ProducesResponseType(typeof(PaymentMethodResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentMethodResponse>> AttachPaymentMethod([FromBody] AttachPaymentMethodRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para registrar Payment Method. CustomerId: {CustomerId}",
                request.CustomerId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(request.CustomerId))
            {
                return BadRequest(new PaymentMethodResponse
                {
                    Success = false,
                    ErrorMessage = "El campo 'customer_id' es requerido"
                });
            }

            if (!request.CustomerId.StartsWith("cus_"))
            {
                return BadRequest(new PaymentMethodResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El 'customer_id' debe tener el formato 'cus_xxx'"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new PaymentMethodResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El campo 'token' es requerido"
                });
            }

            if (!request.Token.StartsWith("tok_") && !request.Token.StartsWith("pm_"))
            {
                return BadRequest(new PaymentMethodResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El 'token' debe tener el formato 'tok_xxx' o 'pm_xxx'"
                });
            }

            var response = await _paymentMethodService.AttachPaymentMethodAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al registrar Payment Method. CustomerId: {CustomerId}, Error: {Error}",
                    request.CustomerId,
                    response.ErrorMessage);

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Payment Method registrado exitosamente. PaymentMethodId: {PaymentMethodId}, CustomerId: {CustomerId}",
                response.PaymentMethodId,
                response.CustomerId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de registro de Payment Method");

            return StatusCode(500, new PaymentMethodResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Desasocia un Payment Method de un Customer
    /// </summary>
    /// <param name="request">Datos del Payment Method a desasociar</param>
    /// <returns>Respuesta de la desasociación</returns>
    /// <response code="200">Payment Method desasociado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("detach")]
    [ProducesResponseType(typeof(DetachPaymentMethodResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DetachPaymentMethodResponse>> DetachPaymentMethod([FromBody] DetachPaymentMethodRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para desasociar Payment Method: {PaymentMethodId}",
                request.PaymentMethodId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
            {
                return BadRequest(new DetachPaymentMethodResponse
                {
                    Success = false,
                    ErrorMessage = "El campo 'payment_method_id' es requerido"
                });
            }

            if (!request.PaymentMethodId.StartsWith("pm_"))
            {
                return BadRequest(new DetachPaymentMethodResponse
                {
                    Success = false,
                    PaymentMethodId = request.PaymentMethodId,
                    ErrorMessage = "El 'payment_method_id' debe tener el formato 'pm_xxx'"
                });
            }

            var response = await _paymentMethodService.DetachPaymentMethodAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al desasociar Payment Method: {PaymentMethodId}, Error: {Error}",
                    request.PaymentMethodId,
                    response.ErrorMessage);

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Payment Method desasociado exitosamente: {PaymentMethodId}",
                response.PaymentMethodId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de desasociación de Payment Method");

            return StatusCode(500, new DetachPaymentMethodResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Obtiene los detalles de un Payment Method
    /// </summary>
    /// <param name="paymentMethodId">ID del Payment Method en Stripe (pm_xxx)</param>
    /// <returns>Detalles del Payment Method</returns>
    /// <response code="200">Payment Method encontrado</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="404">Payment Method no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("{paymentMethodId}")]
    [ProducesResponseType(typeof(PaymentMethodResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentMethodResponse>> GetPaymentMethod(string paymentMethodId)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para obtener Payment Method: {PaymentMethodId}",
                paymentMethodId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(paymentMethodId))
            {
                return BadRequest(new PaymentMethodResponse
                {
                    Success = false,
                    ErrorMessage = "El 'payment_method_id' es requerido"
                });
            }

            if (!paymentMethodId.StartsWith("pm_"))
            {
                return BadRequest(new PaymentMethodResponse
                {
                    Success = false,
                    PaymentMethodId = paymentMethodId,
                    ErrorMessage = "El 'payment_method_id' debe tener el formato 'pm_xxx'"
                });
            }

            var response = await _paymentMethodService.GetPaymentMethodAsync(paymentMethodId);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al obtener Payment Method: {PaymentMethodId}, Error: {Error}",
                    paymentMethodId,
                    response.ErrorMessage);

                if (response.ErrorMessage?.Contains("No such payment_method") == true)
                {
                    return NotFound(response);
                }

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Payment Method obtenido exitosamente: {PaymentMethodId}",
                response.PaymentMethodId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de obtención de Payment Method");

            return StatusCode(500, new PaymentMethodResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Lista todos los Payment Methods de un Customer
    /// </summary>
    /// <param name="customerId">ID del Customer en Stripe (cus_xxx)</param>
    /// <param name="type">Tipo de método de pago (card por defecto)</param>
    /// <returns>Lista de Payment Methods</returns>
    /// <response code="200">Payment Methods listados exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(ListPaymentMethodsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ListPaymentMethodsResponse>> ListPaymentMethods(
        string customerId,
        [FromQuery] string type = "card")
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para listar Payment Methods del Customer: {CustomerId}",
                customerId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(customerId))
            {
                return BadRequest(new ListPaymentMethodsResponse
                {
                    Success = false,
                    ErrorMessage = "El 'customer_id' es requerido"
                });
            }

            if (!customerId.StartsWith("cus_"))
            {
                return BadRequest(new ListPaymentMethodsResponse
                {
                    Success = false,
                    CustomerId = customerId,
                    ErrorMessage = "El 'customer_id' debe tener el formato 'cus_xxx'"
                });
            }

            var response = await _paymentMethodService.ListPaymentMethodsAsync(customerId, type);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al listar Payment Methods del Customer: {CustomerId}, Error: {Error}",
                    customerId,
                    response.ErrorMessage);

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Payment Methods listados exitosamente. Customer: {CustomerId}, Cantidad: {Count}",
                customerId,
                response.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de listado de Payment Methods");

            return StatusCode(500, new ListPaymentMethodsResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Actualiza el estado activo/inactivo de un Customer
    /// </summary>
    /// <param name="request">Datos de la actualización de estado</param>
    /// <returns>Respuesta de la actualización</returns>
    /// <response code="200">Estado actualizado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPut("customer/status")]
    [ProducesResponseType(typeof(UpdateCustomerStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UpdateCustomerStatusResponse>> UpdateCustomerStatus([FromBody] UpdateCustomerStatusRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para actualizar estado del Customer: {CustomerId} a {Active}",
                request.CustomerId,
                request.Active ? "ACTIVO" : "INACTIVO");

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(request.CustomerId))
            {
                return BadRequest(new UpdateCustomerStatusResponse
                {
                    Success = false,
                    ErrorMessage = "El campo 'customer_id' es requerido"
                });
            }

            if (!request.CustomerId.StartsWith("cus_"))
            {
                return BadRequest(new UpdateCustomerStatusResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El 'customer_id' debe tener el formato 'cus_xxx'"
                });
            }

            var response = await _paymentMethodService.UpdateCustomerStatusAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al actualizar estado del Customer: {CustomerId}, Error: {Error}",
                    request.CustomerId,
                    response.ErrorMessage);

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Estado del Customer actualizado exitosamente: {CustomerId}, Activo: {Active}",
                response.CustomerId,
                response.Active);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de actualización de estado");

            return StatusCode(500, new UpdateCustomerStatusResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }
}
