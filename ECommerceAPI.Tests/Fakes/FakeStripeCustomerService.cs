using ECommerceAPI.Models;
using ECommerceAPI.Services;

namespace ECommerceAPI.Tests.Fakes;

/// <summary>
/// Implementación fake de IStripeCustomerService para tests de integración.
/// Almacena los customers en memoria, sin conectarse a la API real de Stripe.
/// </summary>
public class FakeStripeCustomerService : IStripeCustomerService
{
    private readonly Dictionary<string, FakeCustomer> _customers = new();
    private int _idCounter = 1;

    public Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var customerId = $"cus_fake_{_idCounter++:D6}";

        _customers[customerId] = new FakeCustomer
        {
            CustomerId = customerId,
            UserId = request.UserId,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Task.FromResult(new CustomerResponse
        {
            Success = true,
            CustomerId = customerId,
            UserId = request.UserId,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone
        });
    }

    public Task<CustomerResponse> UpdateCustomerAsync(UpdateCustomerRequest request)
    {
        if (!_customers.TryGetValue(request.CustomerId, out var customer))
        {
            return Task.FromResult(new CustomerResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                ErrorMessage = "No such customer"
            });
        }

        if (request.Name != null) customer.Name = request.Name;
        if (request.Email != null) customer.Email = request.Email;
        if (request.Phone != null) customer.Phone = request.Phone;

        return Task.FromResult(new CustomerResponse
        {
            Success = true,
            CustomerId = customer.CustomerId,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone
        });
    }

    public Task<GetCustomerResponse> GetCustomerAsync(string customerId)
    {
        if (!_customers.TryGetValue(customerId, out var customer))
        {
            return Task.FromResult(new GetCustomerResponse
            {
                Success = false,
                CustomerId = customerId,
                ErrorMessage = "No such customer"
            });
        }

        return Task.FromResult(new GetCustomerResponse
        {
            Success = true,
            CustomerId = customer.CustomerId,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            IsDeleted = false,
            Created = customer.CreatedAt
        });
    }

    public Task<DeleteCustomerResponse> DeleteCustomerAsync(string customerId)
    {
        if (!_customers.Remove(customerId))
        {
            return Task.FromResult(new DeleteCustomerResponse
            {
                Success = false,
                CustomerId = customerId,
                ErrorMessage = "No such customer"
            });
        }

        return Task.FromResult(new DeleteCustomerResponse
        {
            Success = true,
            CustomerId = customerId,
            Deleted = true,
            Message = "Customer eliminado exitosamente"
        });
    }

    /// <summary>
    /// Verifica si un customer existe en el almacén fake
    /// </summary>
    public bool CustomerExists(string customerId) => _customers.ContainsKey(customerId);
}

public class FakeCustomer
{
    public string CustomerId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
