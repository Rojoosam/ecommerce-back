using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Interfaz común para todas las pasarelas de pago
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Identificador de la pasarela
    /// </summary>
    PaymentGateway GatewayType { get; }

    /// <summary>
    /// Procesa un pago
    /// </summary>
    Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request);

    /// <summary>
    /// Procesa un reembolso
    /// </summary>
    Task<RefundResponse> ProcessRefundAsync(string transactionId, RefundRequest request);

    /// <summary>
    /// Obtiene información de la pasarela
    /// </summary>
    GatewayInfo GetGatewayInfo();
}
