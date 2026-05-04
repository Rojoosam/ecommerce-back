using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.Controllers;

/// <summary>
/// Controlador para la gestión de Customers en Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private readonly IStripeCustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        IStripeCustomerService customerService,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Crea un nuevo Customer en Stripe
    /// </summary>
    /// <param name="request">Datos del cliente a crear</param>
    /// <returns>Respuesta con el customer_id (cus_xxx) generado y el user_id de Laravel</returns>
    /// <response code="200">Customer creado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para crear customer. UserId: {UserId}, Email: {Email}",
                request.UserId,
                request.Email);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new CustomerResponse
                {
                    Success = false,
                    ErrorMessage = "El campo 'user_id' es requerido"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new CustomerResponse
                {
                    Success = false,
                    UserId = request.UserId,
                    ErrorMessage = "El campo 'name' es requerido"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new CustomerResponse
                {
                    Success = false,
                    UserId = request.UserId,
                    ErrorMessage = "El campo 'email' es requerido"
                });
            }

            var response = await _customerService.CreateCustomerAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al crear customer. UserId: {UserId}, Error: {Error}",
                    request.UserId,
                    response.ErrorMessage);

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Customer creado exitosamente. CustomerId: {CustomerId}, UserId: {UserId}",
                response.CustomerId,
                response.UserId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de creación de customer");

            return StatusCode(500, new CustomerResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Actualiza los datos de un Customer existente en Stripe
    /// </summary>
    /// <param name="request">Datos del cliente a actualizar</param>
    /// <returns>Respuesta con los datos actualizados del customer</returns>
    /// <response code="200">Customer actualizado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPut]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomer([FromBody] UpdateCustomerRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para actualizar customer. CustomerId: {CustomerId}",
                request.CustomerId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(request.CustomerId))
            {
                return BadRequest(new CustomerResponse
                {
                    Success = false,
                    ErrorMessage = "El campo 'customer_id' es requerido"
                });
            }

            // Validar que el CustomerId tenga el formato correcto de Stripe (cus_xxx)
            if (!request.CustomerId.StartsWith("cus_"))
            {
                return BadRequest(new CustomerResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El 'customer_id' debe tener el formato 'cus_xxx'"
                });
            }

            var response = await _customerService.UpdateCustomerAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al actualizar customer. CustomerId: {CustomerId}, Error: {Error}",
                    request.CustomerId,
                    response.ErrorMessage);

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Customer actualizado exitosamente. CustomerId: {CustomerId}",
                response.CustomerId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de actualización de customer");

            return StatusCode(500, new CustomerResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Obtiene los detalles de un Customer desde Stripe
    /// </summary>
    /// <param name="customerId">ID del Customer en Stripe (cus_xxx)</param>
    /// <returns>Detalles del customer</returns>
    /// <response code="200">Customer encontrado</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="404">Customer no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("{customerId}")]
    [ProducesResponseType(typeof(GetCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetCustomerResponse>> GetCustomer(string customerId)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para obtener customer. CustomerId: {CustomerId}",
                customerId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(customerId))
            {
                return BadRequest(new GetCustomerResponse
                {
                    Success = false,
                    ErrorMessage = "El 'customer_id' es requerido"
                });
            }

            // Validar que el CustomerId tenga el formato correcto de Stripe (cus_xxx)
            if (!customerId.StartsWith("cus_"))
            {
                return BadRequest(new GetCustomerResponse
                {
                    Success = false,
                    CustomerId = customerId,
                    ErrorMessage = "El 'customer_id' debe tener el formato 'cus_xxx'"
                });
            }

            var response = await _customerService.GetCustomerAsync(customerId);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al obtener customer. CustomerId: {CustomerId}, Error: {Error}",
                    customerId,
                    response.ErrorMessage);

                // Si el error indica que no se encontró el customer, devolver 404
                if (response.ErrorMessage?.Contains("No such customer") == true)
                {
                    return NotFound(response);
                }

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Customer obtenido exitosamente. CustomerId: {CustomerId}",
                response.CustomerId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de obtención de customer");

            return StatusCode(500, new GetCustomerResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Elimina un Customer en Stripe
    /// </summary>
    /// <param name="customerId">ID del Customer en Stripe (cus_xxx)</param>
    /// <returns>Respuesta de la eliminación</returns>
    /// <response code="200">Customer eliminado exitosamente</response>
    /// <response code="400">Solicitud inválida</response>
    /// <response code="404">Customer no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpDelete("{customerId}")]
    [ProducesResponseType(typeof(DeleteCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteCustomerResponse>> DeleteCustomer(string customerId)
    {
        try
        {
            _logger.LogInformation(
                "Recibida solicitud para eliminar customer. CustomerId: {CustomerId}",
                customerId);

            // Validar datos requeridos
            if (string.IsNullOrWhiteSpace(customerId))
            {
                return BadRequest(new DeleteCustomerResponse
                {
                    Success = false,
                    ErrorMessage = "El 'customer_id' es requerido"
                });
            }

            // Validar que el CustomerId tenga el formato correcto de Stripe (cus_xxx)
            if (!customerId.StartsWith("cus_"))
            {
                return BadRequest(new DeleteCustomerResponse
                {
                    Success = false,
                    CustomerId = customerId,
                    ErrorMessage = "El 'customer_id' debe tener el formato 'cus_xxx'"
                });
            }

            var response = await _customerService.DeleteCustomerAsync(customerId);

            if (!response.Success)
            {
                _logger.LogWarning(
                    "Error al eliminar customer. CustomerId: {CustomerId}, Error: {Error}",
                    customerId,
                    response.ErrorMessage);

                // Si el error indica que no se encontró el customer, devolver 404
                if (response.ErrorMessage?.Contains("No such customer") == true)
                {
                    return NotFound(response);
                }

                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Customer eliminado exitosamente. CustomerId: {CustomerId}",
                response.CustomerId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar solicitud de eliminación de customer");

            return StatusCode(500, new DeleteCustomerResponse
            {
                Success = false,
                ErrorMessage = $"Error interno del servidor: {ex.Message}"
            });
        }
    }
}
