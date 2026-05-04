using ECommerceAPI.Models;
using ECommerceAPI.Services;

namespace ECommerceAPI.Tests.Fakes;

/// <summary>
/// Implementación fake de IStripePaymentIntentService para tests de integración.
/// Almacena los payment intents en memoria, sin conectarse a la API real de Stripe.
/// </summary>
public class FakeStripePaymentIntentService : IStripePaymentIntentService
{
    private readonly Dictionary<string, FakePaymentIntent> _paymentIntents = new();
    private readonly IStripePaymentMethodService _paymentMethodService;
    private int _idCounter = 1;

    public FakeStripePaymentIntentService(IStripePaymentMethodService paymentMethodService)
    {
        _paymentMethodService = paymentMethodService;
    }

    public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
    {
        // Verificar que el customer esté activo (misma lógica que el servicio real)
        var isActive = await _paymentMethodService.IsCustomerActiveAsync(request.CustomerId);
        if (!isActive)
        {
            return new PaymentIntentResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                OrderId = request.OrderId,
                ErrorMessage = "El Customer está inactivo. No se pueden procesar pagos."
            };
        }

        var paymentIntentId = $"pi_fake_{_idCounter++:D6}";

        _paymentIntents[paymentIntentId] = new FakePaymentIntent
        {
            PaymentIntentId = paymentIntentId,
            CustomerId = request.CustomerId,
            PaymentMethodId = request.PaymentMethodId,
            Amount = request.Amount,
            Currency = request.Currency,
            OrderId = request.OrderId,
            Status = "succeeded",
            CreatedAt = DateTime.UtcNow
        };

        return new PaymentIntentResponse
        {
            Success = true,
            PaymentIntentId = paymentIntentId,
            CustomerId = request.CustomerId,
            PaymentMethodId = request.PaymentMethodId,
            OrderId = request.OrderId,
            AmountDecimal = request.Amount,
            Currency = request.Currency,
            Status = "succeeded"
        };
    }

    public Task<PaymentIntentResponse> GetPaymentIntentAsync(string paymentIntentId)
    {
        if (!_paymentIntents.TryGetValue(paymentIntentId, out var pi))
        {
            return Task.FromResult(new PaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = paymentIntentId,
                ErrorMessage = "No such payment intent"
            });
        }

        return Task.FromResult(new PaymentIntentResponse
        {
            Success = true,
            PaymentIntentId = pi.PaymentIntentId,
            CustomerId = pi.CustomerId,
            PaymentMethodId = pi.PaymentMethodId,
            AmountDecimal = pi.Amount,
            Currency = pi.Currency,
            OrderId = pi.OrderId,
            Status = pi.Status
        });
    }

    public Task<CancelPaymentIntentResponse> CancelPaymentIntentAsync(CancelPaymentIntentRequest request)
    {
        if (!_paymentIntents.TryGetValue(request.PaymentIntentId, out var pi))
        {
            return Task.FromResult(new CancelPaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = request.PaymentIntentId,
                ErrorMessage = "No such payment intent"
            });
        }

        if (pi.Status == "succeeded")
        {
            return Task.FromResult(new CancelPaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = pi.PaymentIntentId,
                ErrorMessage = "No se puede cancelar un Payment Intent ya cobrado"
            });
        }

        pi.Status = "canceled";

        return Task.FromResult(new CancelPaymentIntentResponse
        {
            Success = true,
            PaymentIntentId = pi.PaymentIntentId,
            Status = "canceled",
            CancellationReason = request.CancellationReason,
            Message = "Payment Intent cancelado exitosamente"
        });
    }

    public Task<CapturePaymentIntentResponse> CapturePaymentIntentAsync(CapturePaymentIntentRequest request)
    {
        if (!_paymentIntents.TryGetValue(request.PaymentIntentId, out var pi))
        {
            return Task.FromResult(new CapturePaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = request.PaymentIntentId,
                ErrorMessage = "No such payment intent"
            });
        }

        pi.Status = "succeeded";
        var captured = request.AmountToCapture ?? pi.Amount;

        return Task.FromResult(new CapturePaymentIntentResponse
        {
            Success = true,
            PaymentIntentId = pi.PaymentIntentId,
            Status = "succeeded",
            AmountCaptured = captured,
            Currency = pi.Currency,
            Message = "Pago capturado exitosamente"
        });
    }

    /// <summary>
    /// Verifica si un payment intent existe y tiene un estado determinado
    /// </summary>
    public string? GetStatus(string paymentIntentId) =>
        _paymentIntents.TryGetValue(paymentIntentId, out var pi) ? pi.Status : null;
}

public class FakePaymentIntent
{
    public string PaymentIntentId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string PaymentMethodId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
