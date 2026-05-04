using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Interfaz para el servicio de gestión de Payment Methods en Stripe
/// </summary>
public interface IStripePaymentMethodService
{
    /// <summary>
    /// Registra un nuevo Payment Method y lo asocia a un Customer
    /// </summary>
    /// <param name="request">Datos del Payment Method a registrar</param>
    /// <returns>Respuesta con el payment_method_id y datos públicos de la tarjeta</returns>
    Task<PaymentMethodResponse> AttachPaymentMethodAsync(AttachPaymentMethodRequest request);

    /// <summary>
    /// Desasocia un Payment Method de un Customer
    /// </summary>
    /// <param name="request">Datos del Payment Method a desasociar</param>
    /// <returns>Respuesta de la desasociación</returns>
    Task<DetachPaymentMethodResponse> DetachPaymentMethodAsync(DetachPaymentMethodRequest request);

    /// <summary>
    /// Obtiene los detalles de un Payment Method
    /// </summary>
    /// <param name="paymentMethodId">ID del Payment Method en Stripe (pm_xxx)</param>
    /// <returns>Detalles del Payment Method</returns>
    Task<PaymentMethodResponse> GetPaymentMethodAsync(string paymentMethodId);

    /// <summary>
    /// Lista todos los Payment Methods de un Customer
    /// </summary>
    /// <param name="customerId">ID del Customer en Stripe (cus_xxx)</param>
    /// <param name="type">Tipo de método de pago (card, por defecto)</param>
    /// <returns>Lista de Payment Methods del Customer</returns>
    Task<ListPaymentMethodsResponse> ListPaymentMethodsAsync(string customerId, string type = "card");

    /// <summary>
    /// Actualiza el estado activo/inactivo de un Customer
    /// </summary>
    /// <param name="request">Datos de la actualización de estado</param>
    /// <returns>Respuesta de la actualización</returns>
    Task<UpdateCustomerStatusResponse> UpdateCustomerStatusAsync(UpdateCustomerStatusRequest request);

    /// <summary>
    /// Verifica si un Customer está activo
    /// </summary>
    /// <param name="customerId">ID del Customer en Stripe (cus_xxx)</param>
    /// <returns>True si está activo, False si está inactivo</returns>
    Task<bool> IsCustomerActiveAsync(string customerId);
}
