using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Servicio para gestionar reembolsos en Stripe
/// </summary>
public interface IStripeRefundService
{
    /// <summary>
    /// Crear un reembolso asociado a un Payment Intent
    /// </summary>
    /// <param name="paymentIntentId">ID del Payment Intent (pi_xxx)</param>
    /// <param name="request">Datos del reembolso</param>
    /// <returns>Respuesta con la información del reembolso</returns>
    Task<RefundResponse> CreateRefundForPaymentIntentAsync(string paymentIntentId, RefundRequest request);

    /// <summary>
    /// Crear un reembolso asociado a un Charge
    /// </summary>
    /// <param name="chargeId">ID del Charge (ch_xxx)</param>
    /// <param name="request">Datos del reembolso</param>
    /// <returns>Respuesta con la información del reembolso</returns>
    Task<RefundResponse> CreateRefundForChargeAsync(string chargeId, RefundRequest request);

    /// <summary>
    /// Obtener información de un reembolso
    /// </summary>
    /// <param name="refundId">ID del reembolso (re_xxx)</param>
    /// <returns>Información del reembolso</returns>
    Task<RefundResponse> GetRefundAsync(string refundId);

    /// <summary>
    /// Listar todos los reembolsos
    /// </summary>
    /// <param name="limit">Número máximo de resultados</param>
    /// <returns>Lista de reembolsos</returns>
    Task<List<RefundResponse>> ListRefundsAsync(int limit = 10);
}
