using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Interfaz para el servicio de gestión de Customers en Stripe
/// </summary>
public interface IStripeCustomerService
{
    /// <summary>
    /// Crea un nuevo Customer en Stripe
    /// </summary>
    /// <param name="request">Datos del cliente a crear</param>
    /// <returns>Respuesta con el customer_id generado</returns>
    Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request);

    /// <summary>
    /// Actualiza los datos de un Customer existente en Stripe
    /// </summary>
    /// <param name="request">Datos del cliente a actualizar</param>
    /// <returns>Respuesta con los datos actualizados</returns>
    Task<CustomerResponse> UpdateCustomerAsync(UpdateCustomerRequest request);

    /// <summary>
    /// Obtiene los detalles de un Customer desde Stripe
    /// </summary>
    /// <param name="customerId">ID del Customer en Stripe (cus_xxx)</param>
    /// <returns>Detalles del customer</returns>
    Task<GetCustomerResponse> GetCustomerAsync(string customerId);

    /// <summary>
    /// Elimina un Customer en Stripe
    /// </summary>
    /// <param name="customerId">ID del Customer en Stripe (cus_xxx)</param>
    /// <returns>Respuesta de la eliminación</returns>
    Task<DeleteCustomerResponse> DeleteCustomerAsync(string customerId);
}
