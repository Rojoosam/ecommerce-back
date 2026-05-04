using ECommerceAPI.Configuration;
using ECommerceAPI.Models;
using Microsoft.Extensions.Options;
using Stripe;

namespace ECommerceAPI.Services;

/// <summary>
/// Implementación del servicio de reembolsos de Stripe
/// </summary>
public class StripeRefundService : IStripeRefundService
{
    private readonly StripeSettings _stripeSettings;
    private readonly RefundService _refundService;

    public StripeRefundService(IOptions<StripeSettings> stripeSettings)
    {
        _stripeSettings = stripeSettings.Value;
        var client = new StripeClient(_stripeSettings.SecretKey);
        _refundService = new RefundService(client);
    }

    /// <summary>
    /// Crear un reembolso asociado a un Payment Intent
    /// </summary>
    public async Task<RefundResponse> CreateRefundForPaymentIntentAsync(string paymentIntentId, RefundRequest request)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            };

            // Si se especifica un monto, reembolso parcial
            if (request.Amount.HasValue)
            {
                // Stripe trabaja en centavos
                options.Amount = (long)(request.Amount.Value * 100);
            }

            // Agregar razón si se proporciona
            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                options.Reason = MapReasonToStripeReason(request.Reason);
                options.Metadata = new Dictionary<string, string>
                {
                    { "reason_description", request.Reason }
                };
            }

            var refund = await _refundService.CreateAsync(options);

            return MapStripeRefundToResponse(refund);
        }
        catch (StripeException ex)
        {
            throw new InvalidOperationException($"Error al crear reembolso en Stripe: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Crear un reembolso asociado a un Charge
    /// </summary>
    public async Task<RefundResponse> CreateRefundForChargeAsync(string chargeId, RefundRequest request)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                Charge = chargeId
            };

            // Si se especifica un monto, reembolso parcial
            if (request.Amount.HasValue)
            {
                // Stripe trabaja en centavos
                options.Amount = (long)(request.Amount.Value * 100);
            }

            // Agregar razón si se proporciona
            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                options.Reason = MapReasonToStripeReason(request.Reason);
                options.Metadata = new Dictionary<string, string>
                {
                    { "reason_description", request.Reason }
                };
            }

            var refund = await _refundService.CreateAsync(options);

            return MapStripeRefundToResponse(refund);
        }
        catch (StripeException ex)
        {
            throw new InvalidOperationException($"Error al crear reembolso en Stripe: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Obtener información de un reembolso
    /// </summary>
    public async Task<RefundResponse> GetRefundAsync(string refundId)
    {
        try
        {
            var refund = await _refundService.GetAsync(refundId);
            return MapStripeRefundToResponse(refund);
        }
        catch (StripeException ex)
        {
            throw new InvalidOperationException($"Error al obtener reembolso de Stripe: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Listar todos los reembolsos
    /// </summary>
    public async Task<List<RefundResponse>> ListRefundsAsync(int limit = 10)
    {
        try
        {
            var options = new RefundListOptions
            {
                Limit = limit
            };

            var refunds = await _refundService.ListAsync(options);

            return refunds.Data.Select(MapStripeRefundToResponse).ToList();
        }
        catch (StripeException ex)
        {
            throw new InvalidOperationException($"Error al listar reembolsos de Stripe: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Mapear objeto Refund de Stripe a nuestro modelo de respuesta
    /// </summary>
    private RefundResponse MapStripeRefundToResponse(Refund refund)
    {
        return new RefundResponse
        {
            RefundId = refund.Id,
            OriginalTransactionId = refund.PaymentIntentId ?? refund.ChargeId ?? "",
            Status = MapStripeStatusToPaymentStatus(refund.Status),
            Amount = refund.Amount / 100m, // Convertir de centavos a dólares
            Currency = refund.Currency.ToUpper(),
            Message = GetRefundStatusMessage(refund.Status, refund.FailureReason),
            Timestamp = refund.Created
        };
    }

    /// <summary>
    /// Mapear estado de Stripe a nuestro PaymentStatus
    /// </summary>
    private PaymentStatus MapStripeStatusToPaymentStatus(string status)
    {
        return status switch
        {
            "succeeded" => PaymentStatus.Refunded,
            "pending" => PaymentStatus.Pending,
            "failed" => PaymentStatus.Failed,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Failed
        };
    }

    /// <summary>
    /// Mapear razón proporcionada a razón de Stripe
    /// </summary>
    private string MapReasonToStripeReason(string reason)
    {
        var reasonLower = reason.ToLower();

        if (reasonLower.Contains("duplicate"))
            return "duplicate";
        if (reasonLower.Contains("fraud"))
            return "fraudulent";
        if (reasonLower.Contains("customer") || reasonLower.Contains("cliente"))
            return "requested_by_customer";

        return "requested_by_customer"; // Por defecto
    }

    /// <summary>
    /// Obtener mensaje descriptivo del estado del reembolso
    /// </summary>
    private string GetRefundStatusMessage(string status, string? failureReason)
    {
        return status switch
        {
            "succeeded" => "Reembolso procesado exitosamente",
            "pending" => "Reembolso en proceso",
            "failed" => $"Reembolso fallido: {failureReason ?? "Razón desconocida"}",
            "canceled" => "Reembolso cancelado",
            _ => $"Estado desconocido: {status}"
        };
    }
}
