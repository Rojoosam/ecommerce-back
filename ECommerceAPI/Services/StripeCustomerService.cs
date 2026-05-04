using ECommerceAPI.Configuration;
using ECommerceAPI.Models;
using Microsoft.Extensions.Options;
using Stripe;

namespace ECommerceAPI.Services;

/// <summary>
/// Servicio de gestión de Customers en Stripe
/// </summary>
public class StripeCustomerService : IStripeCustomerService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeCustomerService> _logger;
    private readonly CustomerService _customerService;

    public StripeCustomerService(
        IOptions<StripeSettings> settings,
        ILogger<StripeCustomerService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var client = new StripeClient(_settings.SecretKey);
        _customerService = new CustomerService(client);
    }

    public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creando Customer en Stripe para usuario: {UserId}, Email: {Email}",
                request.UserId,
                request.Email);

            // Preparar las opciones para crear el customer
            var options = new CustomerCreateOptions
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            // Agregar el user_id de Laravel al metadata para referencia
            options.Metadata["user_id"] = request.UserId;

            // Marcar el customer como activo por defecto
            options.Metadata["active"] = "true";

            // Agregar dirección si se proporciona
            if (request.Address != null)
            {
                options.Address = new Stripe.AddressOptions
                {
                    Line1 = request.Address.Line1,
                    Line2 = request.Address.Line2,
                    City = request.Address.City,
                    State = request.Address.State,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.Country
                };
            }

            // Crear el customer en Stripe
            var customer = await _customerService.CreateAsync(options);

            _logger.LogInformation(
                "Customer creado exitosamente en Stripe: {CustomerId} para usuario: {UserId}",
                customer.Id,
                request.UserId);

            // Mapear la respuesta
            return new CustomerResponse
            {
                Success = true,
                CustomerId = customer.Id,
                UserId = request.UserId,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = MapStripeAddressToCustomerAddress(customer.Address),
                Created = customer.Created,
                IsDeleted = customer.Deleted ?? false,
                Metadata = customer.Metadata
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al crear customer para usuario: {UserId}. Error: {Error}",
                request.UserId,
                ex.Message);

            return new CustomerResponse
            {
                Success = false,
                UserId = request.UserId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al crear customer para usuario: {UserId}",
                request.UserId);

            return new CustomerResponse
            {
                Success = false,
                UserId = request.UserId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(UpdateCustomerRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Actualizando Customer en Stripe: {CustomerId}",
                request.CustomerId);

            // Preparar las opciones para actualizar el customer
            var options = new CustomerUpdateOptions();

            // Solo actualizar los campos que se proporcionan
            if (!string.IsNullOrEmpty(request.Name))
            {
                options.Name = request.Name;
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                options.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.Phone))
            {
                options.Phone = request.Phone;
            }

            if (request.Metadata != null && request.Metadata.Any())
            {
                options.Metadata = request.Metadata;
            }

            // Actualizar dirección si se proporciona
            if (request.Address != null)
            {
                options.Address = new Stripe.AddressOptions
                {
                    Line1 = request.Address.Line1,
                    Line2 = request.Address.Line2,
                    City = request.Address.City,
                    State = request.Address.State,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.Country
                };
            }

            // Actualizar el customer en Stripe
            var customer = await _customerService.UpdateAsync(request.CustomerId, options);

            _logger.LogInformation(
                "Customer actualizado exitosamente en Stripe: {CustomerId}",
                customer.Id);

            // Extraer el user_id del metadata si existe
            string? userId = customer.Metadata.TryGetValue("user_id", out var userIdValue) 
                ? userIdValue 
                : null;

            return new CustomerResponse
            {
                Success = true,
                CustomerId = customer.Id,
                UserId = userId,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = MapStripeAddressToCustomerAddress(customer.Address),
                Created = customer.Created,
                IsDeleted = customer.Deleted ?? false,
                Metadata = customer.Metadata
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al actualizar customer: {CustomerId}. Error: {Error}",
                request.CustomerId,
                ex.Message);

            return new CustomerResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al actualizar customer: {CustomerId}",
                request.CustomerId);

            return new CustomerResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<GetCustomerResponse> GetCustomerAsync(string customerId)
    {
        try
        {
            _logger.LogInformation(
                "Obteniendo detalles del Customer desde Stripe: {CustomerId}",
                customerId);

            // Obtener el customer desde Stripe
            var customer = await _customerService.GetAsync(customerId);

            _logger.LogInformation(
                "Customer obtenido exitosamente desde Stripe: {CustomerId}",
                customer.Id);

            return new GetCustomerResponse
            {
                Success = true,
                CustomerId = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = MapStripeAddressToCustomerAddress(customer.Address),
                Created = customer.Created,
                Balance = customer.Balance,
                Currency = customer.Currency,
                IsDeleted = customer.Deleted ?? false,
                Metadata = customer.Metadata
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al obtener customer: {CustomerId}. Error: {Error}",
                customerId,
                ex.Message);

            return new GetCustomerResponse
            {
                Success = false,
                CustomerId = customerId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al obtener customer: {CustomerId}",
                customerId);

            return new GetCustomerResponse
            {
                Success = false,
                CustomerId = customerId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<DeleteCustomerResponse> DeleteCustomerAsync(string customerId)
    {
        try
        {
            _logger.LogInformation(
                "Eliminando Customer en Stripe: {CustomerId}",
                customerId);

            // Eliminar el customer en Stripe
            var deletedCustomer = await _customerService.DeleteAsync(customerId);

            _logger.LogInformation(
                "Customer eliminado exitosamente en Stripe: {CustomerId}",
                customerId);

            return new DeleteCustomerResponse
            {
                Success = true,
                CustomerId = deletedCustomer.Id,
                Deleted = deletedCustomer.Deleted ?? false,
                Message = "Customer eliminado exitosamente en Stripe"
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al eliminar customer: {CustomerId}. Error: {Error}",
                customerId,
                ex.Message);

            return new DeleteCustomerResponse
            {
                Success = false,
                CustomerId = customerId,
                Deleted = false,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al eliminar customer: {CustomerId}",
                customerId);

            return new DeleteCustomerResponse
            {
                Success = false,
                CustomerId = customerId,
                Deleted = false,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Mapea una dirección de Stripe a nuestro modelo de CustomerAddress
    /// </summary>
    private CustomerAddress? MapStripeAddressToCustomerAddress(Stripe.Address? stripeAddress)
    {
        if (stripeAddress == null)
            return null;

        return new CustomerAddress
        {
            Line1 = stripeAddress.Line1,
            Line2 = stripeAddress.Line2,
            City = stripeAddress.City,
            State = stripeAddress.State,
            PostalCode = stripeAddress.PostalCode,
            Country = stripeAddress.Country
        };
    }
}
