using ECommerceAPI.Models;
using ECommerceAPI.Services;

namespace ECommerceAPI.Tests.Fakes;

/// <summary>
/// Implementación fake de IStripePaymentMethodService para tests de integración.
/// Almacena los payment methods en memoria, sin conectarse a la API real de Stripe.
/// </summary>
public class FakeStripePaymentMethodService : IStripePaymentMethodService
{
    private readonly Dictionary<string, FakePaymentMethod> _paymentMethods = new();
    private readonly Dictionary<string, bool> _customerStatus = new();
    private int _idCounter = 1;

    public Task<PaymentMethodResponse> AttachPaymentMethodAsync(AttachPaymentMethodRequest request)
    {
        var paymentMethodId = $"pm_fake_{_idCounter++:D6}";

        _paymentMethods[paymentMethodId] = new FakePaymentMethod
        {
            PaymentMethodId = paymentMethodId,
            CustomerId = request.CustomerId,
            Token = request.Token,
            Last4 = "4242",
            Brand = "visa",
            ExpMonth = 12,
            ExpYear = 2030
        };

        // Activar el customer automáticamente al tener un método de pago
        _customerStatus[request.CustomerId] = true;

        return Task.FromResult(new PaymentMethodResponse
        {
            Success = true,
            PaymentMethodId = paymentMethodId,
            CustomerId = request.CustomerId,
            Type = "card",
            Card = new CardDetails
            {
                Brand = "visa",
                Last4 = "4242",
                ExpMonth = 12,
                ExpYear = 2030,
                Funding = "credit"
            },
            Created = DateTime.UtcNow
        });
    }

    public Task<DetachPaymentMethodResponse> DetachPaymentMethodAsync(DetachPaymentMethodRequest request)
    {
        if (!_paymentMethods.Remove(request.PaymentMethodId))
        {
            return Task.FromResult(new DetachPaymentMethodResponse
            {
                Success = false,
                PaymentMethodId = request.PaymentMethodId,
                ErrorMessage = "No such payment method"
            });
        }

        return Task.FromResult(new DetachPaymentMethodResponse
        {
            Success = true,
            PaymentMethodId = request.PaymentMethodId,
            Message = "Payment Method desasociado exitosamente"
        });
    }

    public Task<PaymentMethodResponse> GetPaymentMethodAsync(string paymentMethodId)
    {
        if (!_paymentMethods.TryGetValue(paymentMethodId, out var pm))
        {
            return Task.FromResult(new PaymentMethodResponse
            {
                Success = false,
                PaymentMethodId = paymentMethodId,
                ErrorMessage = "No such payment method"
            });
        }

        return Task.FromResult(new PaymentMethodResponse
        {
            Success = true,
            PaymentMethodId = pm.PaymentMethodId,
            CustomerId = pm.CustomerId,
            Type = "card",
            Card = new CardDetails
            {
                Brand = pm.Brand,
                Last4 = pm.Last4,
                ExpMonth = pm.ExpMonth,
                ExpYear = pm.ExpYear
            }
        });
    }

    public Task<ListPaymentMethodsResponse> ListPaymentMethodsAsync(string customerId, string type = "card")
    {
        var methods = _paymentMethods.Values
            .Where(pm => pm.CustomerId == customerId)
            .Select(pm => new PaymentMethodResponse
            {
                Success = true,
                PaymentMethodId = pm.PaymentMethodId,
                CustomerId = pm.CustomerId,
                Type = "card",
                Card = new CardDetails
                {
                    Brand = pm.Brand,
                    Last4 = pm.Last4,
                    ExpMonth = pm.ExpMonth,
                    ExpYear = pm.ExpYear
                }
            })
            .ToList();

        return Task.FromResult(new ListPaymentMethodsResponse
        {
            Success = true,
            CustomerId = customerId,
            PaymentMethods = methods,
            Count = methods.Count
        });
    }

    public Task<UpdateCustomerStatusResponse> UpdateCustomerStatusAsync(UpdateCustomerStatusRequest request)
    {
        _customerStatus[request.CustomerId] = request.Active;

        var detached = 0;
        if (!request.Active && request.DetachPaymentMethods)
        {
            var toRemove = _paymentMethods.Values
                .Where(pm => pm.CustomerId == request.CustomerId)
                .Select(pm => pm.PaymentMethodId)
                .ToList();

            foreach (var pmId in toRemove)
                _paymentMethods.Remove(pmId);

            detached = toRemove.Count;
        }

        return Task.FromResult(new UpdateCustomerStatusResponse
        {
            Success = true,
            CustomerId = request.CustomerId,
            Active = request.Active,
            PaymentMethodsDetached = detached,
            Message = request.Active ? "Customer activado" : "Customer desactivado"
        });
    }

    public Task<bool> IsCustomerActiveAsync(string customerId)
    {
        var isActive = _customerStatus.TryGetValue(customerId, out var active) && active;
        return Task.FromResult(isActive);
    }

    /// <summary>
    /// Verifica si un payment method existe en el almacén fake
    /// </summary>
    public bool PaymentMethodExists(string paymentMethodId) =>
        _paymentMethods.ContainsKey(paymentMethodId);
}

public class FakePaymentMethod
{
    public string PaymentMethodId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string Last4 { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public long ExpMonth { get; set; }
    public long ExpYear { get; set; }
}
