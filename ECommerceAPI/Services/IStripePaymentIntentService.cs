using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Interfaz para el servicio de gestión de Payment Intents en Stripe
/// </summary>
public interface IStripePaymentIntentService
{
    /// <summary>
    /// Crea y confirma un Payment Intent en Stripe
    /// </summary>
    /// <param name="request">Datos del pago a procesar</param>
    /// <returns>Respuesta con el payment_intent_id y estado del pago</returns>
    Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);

    /// <summary>
    /// Obtiene los detalles de un Payment Intent
    /// </summary>
    /// <param name="paymentIntentId">ID del Payment Intent en Stripe (pi_xxx)</param>
    /// <returns>Detalles del Payment Intent</returns>
    Task<PaymentIntentResponse> GetPaymentIntentAsync(string paymentIntentId);

    /// <summary>
    /// Cancela un Payment Intent que aún no ha sido confirmado o capturado
    /// </summary>
    /// <param name="request">Datos de la cancelación</param>
    /// <returns>Respuesta de la cancelación</returns>
    Task<CancelPaymentIntentResponse> CancelPaymentIntentAsync(CancelPaymentIntentRequest request);

    /// <summary>
    /// Captura un Payment Intent que fue autorizado pero no capturado
    /// </summary>
    /// <param name="request">Datos de la captura</param>
    /// <returns>Respuesta de la captura</returns>
    Task<CapturePaymentIntentResponse> CapturePaymentIntentAsync(CapturePaymentIntentRequest request);
}
