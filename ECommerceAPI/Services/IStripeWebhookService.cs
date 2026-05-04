using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Interfaz para el servicio de procesamiento de webhooks de Stripe
/// </summary>
public interface IStripeWebhookService
{
    /// <summary>
    /// Procesa un webhook de Stripe, valida la firma y envía notificación a Laravel
    /// </summary>
    /// <param name="json">JSON del evento de Stripe</param>
    /// <param name="signature">Firma del webhook (Stripe-Signature header)</param>
    /// <returns>Respuesta del procesamiento del webhook</returns>
    Task<WebhookResponse> ProcessWebhookAsync(string json, string signature);

    /// <summary>
    /// Envía una notificación procesada a Laravel
    /// </summary>
    /// <param name="notification">Notificación a enviar</param>
    /// <returns>True si se envió exitosamente, False en caso contrario</returns>
    Task<bool> SendNotificationToLaravelAsync(WebhookNotification notification);

    /// <summary>
    /// Procesa un evento de Payment Intent exitoso
    /// </summary>
    /// <param name="eventId">ID del evento</param>
    /// <param name="paymentIntent">Payment Intent de Stripe</param>
    /// <param name="eventCreated">Fecha del evento</param>
    /// <returns>Notificación procesada</returns>
    WebhookNotification ProcessPaymentIntentSucceeded(string eventId, Stripe.PaymentIntent paymentIntent, DateTime eventCreated);

    /// <summary>
    /// Procesa un evento de Payment Intent fallido
    /// </summary>
    /// <param name="eventId">ID del evento</param>
    /// <param name="paymentIntent">Payment Intent de Stripe</param>
    /// <param name="eventCreated">Fecha del evento</param>
    /// <returns>Notificación procesada</returns>
    WebhookNotification ProcessPaymentIntentFailed(string eventId, Stripe.PaymentIntent paymentIntent, DateTime eventCreated);

    /// <summary>
    /// Procesa un evento de Payment Intent cancelado
    /// </summary>
    /// <param name="eventId">ID del evento</param>
    /// <param name="paymentIntent">Payment Intent de Stripe</param>
    /// <param name="eventCreated">Fecha del evento</param>
    /// <returns>Notificación procesada</returns>
    WebhookNotification ProcessPaymentIntentCanceled(string eventId, Stripe.PaymentIntent paymentIntent, DateTime eventCreated);

    /// <summary>
    /// Procesa un evento de Charge reembolsado
    /// </summary>
    /// <param name="eventId">ID del evento</param>
    /// <param name="charge">Charge de Stripe</param>
    /// <param name="eventCreated">Fecha del evento</param>
    /// <returns>Notificación procesada</returns>
    WebhookNotification ProcessChargeRefunded(string eventId, Stripe.Charge charge, DateTime eventCreated);

    /// <summary>
    /// Procesa un evento de Disputa creada
    /// </summary>
    /// <param name="eventId">ID del evento</param>
    /// <param name="dispute">Dispute de Stripe</param>
    /// <param name="eventCreated">Fecha del evento</param>
    /// <returns>Notificación procesada</returns>
    WebhookNotification ProcessChargeDisputeCreated(string eventId, Stripe.Dispute dispute, DateTime eventCreated);

    /// <summary>
    /// Construye la notificación de pago en el formato esperado por Laravel
    /// </summary>
    /// <param name="paymentIntent">Payment Intent de Stripe</param>
    /// <param name="status">Estado del pago (succeeded, failed, canceled)</param>
    /// <returns>Notificación en formato Laravel</returns>
    Task<LaravelPaymentNotification> BuildLaravelPaymentNotificationAsync(Stripe.PaymentIntent paymentIntent, string status);

    /// <summary>
    /// Envía una notificación de pago a Laravel en el formato esperado por POST /webhooks/payments
    /// </summary>
    /// <param name="notification">Notificación de pago</param>
    /// <returns>True si se envió exitosamente</returns>
    Task<bool> SendPaymentNotificationToLaravelAsync(LaravelPaymentNotification notification);

    /// <summary>
    /// Construye la notificación de reembolso en el formato esperado por Laravel
    /// </summary>
    /// <param name="charge">Charge de Stripe con información del reembolso</param>
    /// <returns>Notificación de reembolso en formato Laravel</returns>
    Task<LaravelRefundNotification> BuildLaravelRefundNotificationAsync(Stripe.Charge charge);

    /// <summary>
    /// Envía una notificación de reembolso a Laravel en el formato esperado por POST /webhooks/refunds
    /// </summary>
    /// <param name="notification">Notificación de reembolso</param>
    /// <returns>True si se envió exitosamente</returns>
    Task<bool> SendRefundNotificationToLaravelAsync(LaravelRefundNotification notification);

    /// <summary>
    /// Envía una notificación de método de pago a Laravel en el formato esperado por POST /webhooks/payment-methods
    /// </summary>
    /// <param name="notification">Notificación de método de pago</param>
    /// <returns>True si se envió exitosamente</returns>
    Task<bool> SendPaymentMethodNotificationToLaravelAsync(LaravelPaymentMethodNotification notification);
}
