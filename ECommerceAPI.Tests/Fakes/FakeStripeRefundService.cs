using ECommerceAPI.Models;
using ECommerceAPI.Services;

namespace ECommerceAPI.Tests.Fakes;

/// <summary>
/// Implementación fake de IStripeRefundService para tests de integración.
/// Almacena los reembolsos en memoria, sin conectarse a la API real de Stripe.
/// </summary>
public class FakeStripeRefundService : IStripeRefundService
{
    private readonly Dictionary<string, FakeRefund> _refunds = new();
    private int _idCounter = 1;

    public Task<RefundResponse> CreateRefundForPaymentIntentAsync(string paymentIntentId, RefundRequest request)
    {
        var refundId = $"re_fake_{_idCounter++:D6}";

        _refunds[refundId] = new FakeRefund
        {
            RefundId = refundId,
            OriginalId = paymentIntentId,
            Amount = request.Amount ?? 0,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        };

        return Task.FromResult(new RefundResponse
        {
            RefundId = refundId,
            OriginalTransactionId = paymentIntentId,
            Status = PaymentStatus.Refunded,
            Amount = request.Amount ?? 0,
            Currency = "usd",
            Message = "Reembolso procesado exitosamente",
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<RefundResponse> CreateRefundForChargeAsync(string chargeId, RefundRequest request)
    {
        var refundId = $"re_fake_{_idCounter++:D6}";

        _refunds[refundId] = new FakeRefund
        {
            RefundId = refundId,
            OriginalId = chargeId,
            Amount = request.Amount ?? 0,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        };

        return Task.FromResult(new RefundResponse
        {
            RefundId = refundId,
            OriginalTransactionId = chargeId,
            Status = PaymentStatus.Refunded,
            Amount = request.Amount ?? 0,
            Currency = "usd",
            Message = "Reembolso de cargo procesado exitosamente",
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<RefundResponse> GetRefundAsync(string refundId)
    {
        if (!_refunds.TryGetValue(refundId, out var refund))
        {
            return Task.FromResult(new RefundResponse
            {
                RefundId = refundId,
                Status = PaymentStatus.Failed,
                Message = "No such refund"
            });
        }

        return Task.FromResult(new RefundResponse
        {
            RefundId = refund.RefundId,
            OriginalTransactionId = refund.OriginalId,
            Status = PaymentStatus.Refunded,
            Amount = refund.Amount,
            Currency = "usd",
            Message = "OK",
            Timestamp = refund.CreatedAt
        });
    }

    public Task<List<RefundResponse>> ListRefundsAsync(int limit = 10)
    {
        var list = _refunds.Values
            .Take(limit)
            .Select(r => new RefundResponse
            {
                RefundId = r.RefundId,
                OriginalTransactionId = r.OriginalId,
                Status = PaymentStatus.Refunded,
                Amount = r.Amount,
                Currency = "usd",
                Timestamp = r.CreatedAt
            })
            .ToList();

        return Task.FromResult(list);
    }

    /// <summary>
    /// Cantidad de reembolsos creados (útil para assertions)
    /// </summary>
    public int RefundCount => _refunds.Count;
}

public class FakeRefund
{
    public string RefundId { get; set; } = null!;
    public string OriginalId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
