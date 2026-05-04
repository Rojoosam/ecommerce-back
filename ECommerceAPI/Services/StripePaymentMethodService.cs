using ECommerceAPI.Configuration;
using ECommerceAPI.Models;
using Microsoft.Extensions.Options;
using Stripe;

namespace ECommerceAPI.Services;

/// <summary>
/// Servicio de gestión de Payment Methods en Stripe
/// </summary>
public class StripePaymentMethodService : IStripePaymentMethodService
{
    private readonly StripeSettings _settings;
    private readonly LaravelNotificationSettings _laravelSettings;
    private readonly ILogger<StripePaymentMethodService> _logger;
    private readonly PaymentMethodService _paymentMethodService;
    private readonly CustomerService _customerService;
    private readonly TokenService _tokenService;
    private readonly IStripeWebhookService _webhookService;

    public StripePaymentMethodService(
        IOptions<StripeSettings> settings,
        IOptions<LaravelNotificationSettings> laravelSettings,
        ILogger<StripePaymentMethodService> logger,
        IStripeWebhookService webhookService)
    {
        _settings = settings.Value;
        _laravelSettings = laravelSettings.Value;
        _logger = logger;
        _webhookService = webhookService;

        var client = new StripeClient(_settings.SecretKey);
        _paymentMethodService = new PaymentMethodService(client);
        _customerService = new CustomerService(client);
        _tokenService = new TokenService(client);
    }

    public async Task<PaymentMethodResponse> AttachPaymentMethodAsync(AttachPaymentMethodRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Registrando Payment Method para Customer: {CustomerId}",
                request.CustomerId);

            // Verificar que el customer esté activo
            var isActive = await IsCustomerActiveAsync(request.CustomerId);
            if (!isActive)
            {
                _logger.LogWarning(
                    "Intento de agregar Payment Method a Customer inactivo: {CustomerId}",
                    request.CustomerId);

                return new PaymentMethodResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El Customer está inactivo. No se pueden agregar métodos de pago."
                };
            }

            PaymentMethod paymentMethod;

            // Determinar si es un token o un payment method ID
            if (request.Token.StartsWith("tok_"))
            {
                // Es un token, primero obtener los detalles del token
                _logger.LogInformation("Procesando token: {Token}", request.Token);

                // Crear Payment Method desde el token
                var createOptions = new PaymentMethodCreateOptions
                {
                    Type = "card",
                    Card = new PaymentMethodCardOptions
                    {
                        Token = request.Token
                    },
                    Metadata = request.Metadata ?? new Dictionary<string, string>()
                };

                paymentMethod = await _paymentMethodService.CreateAsync(createOptions);
                
                _logger.LogInformation(
                    "Payment Method creado desde token: {PaymentMethodId}",
                    paymentMethod.Id);
            }
            else if (request.Token.StartsWith("pm_"))
            {
                // Ya es un Payment Method ID, solo obtenerlo
                _logger.LogInformation("Usando Payment Method existente: {PaymentMethodId}", request.Token);
                paymentMethod = await _paymentMethodService.GetAsync(request.Token);
            }
            else
            {
                return new PaymentMethodResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    ErrorMessage = "El token debe empezar con 'tok_' o 'pm_'"
                };
            }

            // Asociar el Payment Method al Customer
            var attachOptions = new PaymentMethodAttachOptions
            {
                Customer = request.CustomerId
            };

            paymentMethod = await _paymentMethodService.AttachAsync(paymentMethod.Id, attachOptions);

            _logger.LogInformation(
                "Payment Method {PaymentMethodId} asociado exitosamente al Customer {CustomerId}",
                paymentMethod.Id,
                request.CustomerId);

            // Notificar a Laravel sobre el nuevo método de pago
            await NotifyLaravelPaymentMethodAsync(
                paymentMethod,
                request.CustomerId,
                "attached");

            // Mapear la respuesta
            return new PaymentMethodResponse
            {
                Success = true,
                PaymentMethodId = paymentMethod.Id,
                CustomerId = paymentMethod.CustomerId,
                Type = paymentMethod.Type,
                Card = MapStripeCardToCardDetails(paymentMethod.Card),
                Created = paymentMethod.Created,
                Metadata = paymentMethod.Metadata
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al registrar Payment Method para Customer: {CustomerId}. Error: {Error}",
                request.CustomerId,
                ex.Message);

            return new PaymentMethodResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al registrar Payment Method para Customer: {CustomerId}",
                request.CustomerId);

            return new PaymentMethodResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<DetachPaymentMethodResponse> DetachPaymentMethodAsync(DetachPaymentMethodRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Desasociando Payment Method: {PaymentMethodId}",
                request.PaymentMethodId);

            // Obtener detalles del Payment Method ANTES de desasociar
            // (después del detach, el customerId ya no estará disponible)
            PaymentMethod? paymentMethodBefore = null;
            try
            {
                paymentMethodBefore = await _paymentMethodService.GetAsync(request.PaymentMethodId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo obtener detalles del Payment Method antes de desasociar: {PaymentMethodId}",
                    request.PaymentMethodId);
            }

            // Desasociar el Payment Method
            var paymentMethod = await _paymentMethodService.DetachAsync(request.PaymentMethodId);

            _logger.LogInformation(
                "Payment Method {PaymentMethodId} desasociado exitosamente",
                request.PaymentMethodId);

            // Notificar a Laravel sobre la eliminación del método de pago
            if (paymentMethodBefore != null)
            {
                await NotifyLaravelPaymentMethodAsync(
                    paymentMethodBefore,
                    paymentMethodBefore.CustomerId,
                    "detached");
            }

            return new DetachPaymentMethodResponse
            {
                Success = true,
                PaymentMethodId = paymentMethod.Id,
                Message = "Payment Method desasociado exitosamente"
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al desasociar Payment Method: {PaymentMethodId}. Error: {Error}",
                request.PaymentMethodId,
                ex.Message);

            return new DetachPaymentMethodResponse
            {
                Success = false,
                PaymentMethodId = request.PaymentMethodId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al desasociar Payment Method: {PaymentMethodId}",
                request.PaymentMethodId);

            return new DetachPaymentMethodResponse
            {
                Success = false,
                PaymentMethodId = request.PaymentMethodId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<PaymentMethodResponse> GetPaymentMethodAsync(string paymentMethodId)
    {
        try
        {
            _logger.LogInformation(
                "Obteniendo Payment Method: {PaymentMethodId}",
                paymentMethodId);

            var paymentMethod = await _paymentMethodService.GetAsync(paymentMethodId);

            _logger.LogInformation(
                "Payment Method obtenido exitosamente: {PaymentMethodId}",
                paymentMethodId);

            return new PaymentMethodResponse
            {
                Success = true,
                PaymentMethodId = paymentMethod.Id,
                CustomerId = paymentMethod.CustomerId,
                Type = paymentMethod.Type,
                Card = MapStripeCardToCardDetails(paymentMethod.Card),
                Created = paymentMethod.Created,
                Metadata = paymentMethod.Metadata
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al obtener Payment Method: {PaymentMethodId}. Error: {Error}",
                paymentMethodId,
                ex.Message);

            return new PaymentMethodResponse
            {
                Success = false,
                PaymentMethodId = paymentMethodId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al obtener Payment Method: {PaymentMethodId}",
                paymentMethodId);

            return new PaymentMethodResponse
            {
                Success = false,
                PaymentMethodId = paymentMethodId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<ListPaymentMethodsResponse> ListPaymentMethodsAsync(string customerId, string type = "card")
    {
        try
        {
            _logger.LogInformation(
                "Listando Payment Methods del Customer: {CustomerId}",
                customerId);

            var options = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = type
            };

            var paymentMethods = await _paymentMethodService.ListAsync(options);

            var paymentMethodsList = paymentMethods.Data.Select(pm => new PaymentMethodResponse
            {
                Success = true,
                PaymentMethodId = pm.Id,
                CustomerId = pm.CustomerId,
                Type = pm.Type,
                Card = MapStripeCardToCardDetails(pm.Card),
                Created = pm.Created,
                Metadata = pm.Metadata
            }).ToList();

            _logger.LogInformation(
                "Payment Methods listados exitosamente. Customer: {CustomerId}, Cantidad: {Count}",
                customerId,
                paymentMethodsList.Count);

            return new ListPaymentMethodsResponse
            {
                Success = true,
                CustomerId = customerId,
                PaymentMethods = paymentMethodsList,
                Count = paymentMethodsList.Count
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al listar Payment Methods del Customer: {CustomerId}. Error: {Error}",
                customerId,
                ex.Message);

            return new ListPaymentMethodsResponse
            {
                Success = false,
                CustomerId = customerId,
                PaymentMethods = new List<PaymentMethodResponse>(),
                Count = 0,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al listar Payment Methods del Customer: {CustomerId}",
                customerId);

            return new ListPaymentMethodsResponse
            {
                Success = false,
                CustomerId = customerId,
                PaymentMethods = new List<PaymentMethodResponse>(),
                Count = 0,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<UpdateCustomerStatusResponse> UpdateCustomerStatusAsync(UpdateCustomerStatusRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Actualizando estado del Customer: {CustomerId} a {Active}",
                request.CustomerId,
                request.Active ? "ACTIVO" : "INACTIVO");

            // Actualizar metadata del customer
            var updateOptions = new CustomerUpdateOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "active", request.Active.ToString().ToLower() }
                }
            };

            var customer = await _customerService.UpdateAsync(request.CustomerId, updateOptions);

            int paymentMethodsDetached = 0;

            // Si se desactiva el customer y se solicita desasociar payment methods
            if (!request.Active && request.DetachPaymentMethods)
            {
                _logger.LogInformation(
                    "Desasociando Payment Methods del Customer: {CustomerId}",
                    request.CustomerId);

                var paymentMethodsList = await ListPaymentMethodsAsync(request.CustomerId);

                if (paymentMethodsList.Success && paymentMethodsList.PaymentMethods != null)
                {
                    foreach (var pm in paymentMethodsList.PaymentMethods)
                    {
                        if (pm.PaymentMethodId != null)
                        {
                            var detachResult = await DetachPaymentMethodAsync(new DetachPaymentMethodRequest
                            {
                                PaymentMethodId = pm.PaymentMethodId
                            });

                            if (detachResult.Success)
                            {
                                paymentMethodsDetached++;
                            }
                        }
                    }
                }
            }

            _logger.LogInformation(
                "Estado del Customer actualizado exitosamente. Customer: {CustomerId}, Activo: {Active}, Payment Methods desasociados: {Count}",
                request.CustomerId,
                request.Active,
                paymentMethodsDetached);

            return new UpdateCustomerStatusResponse
            {
                Success = true,
                CustomerId = customer.Id,
                Active = request.Active,
                PaymentMethodsDetached = paymentMethodsDetached,
                Message = request.Active
                    ? "Customer activado exitosamente"
                    : $"Customer desactivado exitosamente. {paymentMethodsDetached} Payment Methods desasociados."
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al actualizar estado del Customer: {CustomerId}. Error: {Error}",
                request.CustomerId,
                ex.Message);

            return new UpdateCustomerStatusResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al actualizar estado del Customer: {CustomerId}",
                request.CustomerId);

            return new UpdateCustomerStatusResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<bool> IsCustomerActiveAsync(string customerId)
    {
        try
        {
            var customer = await _customerService.GetAsync(customerId);

            // Verificar metadata para el estado activo
            if (customer.Metadata != null && customer.Metadata.TryGetValue("active", out var activeValue))
            {
                return activeValue.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // Por defecto, si no tiene metadata de "active", se considera activo
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al verificar estado del Customer: {CustomerId}",
                customerId);
            
            // En caso de error, asumir que está activo para no bloquear operaciones
            return true;
        }
    }

    /// <summary>
    /// Mapea una tarjeta de Stripe a nuestro modelo CardDetails
    /// </summary>
    private CardDetails? MapStripeCardToCardDetails(Stripe.PaymentMethodCard? stripeCard)
    {
        if (stripeCard == null)
            return null;

        return new CardDetails
        {
            Brand = stripeCard.Brand,
            Last4 = stripeCard.Last4,
            ExpMonth = stripeCard.ExpMonth,
            ExpYear = stripeCard.ExpYear,
            Country = stripeCard.Country,
            Funding = stripeCard.Funding,
            CardholderName = stripeCard.Networks?.Available?.FirstOrDefault()
        };
    }

    /// <summary>
    /// Notifica a Laravel sobre un cambio en un método de pago (attached/detached)
    /// </summary>
    private async Task NotifyLaravelPaymentMethodAsync(
        PaymentMethod paymentMethod,
        string? customerId,
        string action)
    {
        if (!_laravelSettings.Enabled)
        {
            _logger.LogInformation("Envío a Laravel deshabilitado en configuración");
            return;
        }

        try
        {
            _logger.LogInformation(
                "Notificando a Laravel sobre Payment Method {Action}: {PaymentMethodId}",
                action,
                paymentMethod.Id);

            // Obtener user_id del metadata del Customer
            int? userId = null;
            if (!string.IsNullOrEmpty(customerId))
            {
                try
                {
                    var customer = await _customerService.GetAsync(customerId);
                    if (customer.Metadata != null &&
                        customer.Metadata.TryGetValue("user_id", out var userIdStr) &&
                        int.TryParse(userIdStr, out var parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo obtener user_id del Customer: {CustomerId}",
                        customerId);
                }
            }

            var notification = new LaravelPaymentMethodNotification
            {
                UserId = userId,
                ExternalId = paymentMethod.Id,
                Brand = paymentMethod.Card?.Brand != null
                    ? System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(paymentMethod.Card.Brand)
                    : null,
                LastFour = paymentMethod.Card?.Last4,
                ExpMonth = paymentMethod.Card?.ExpMonth.ToString("D2"),
                ExpYear = paymentMethod.Card?.ExpYear.ToString(),
                Action = action
            };

            var sent = await _webhookService.SendPaymentMethodNotificationToLaravelAsync(notification);

            if (!sent)
            {
                _logger.LogWarning(
                    "No se pudo enviar notificación de Payment Method a Laravel. PaymentMethodId: {PaymentMethodId}, Action: {Action}",
                    paymentMethod.Id,
                    action);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al notificar a Laravel sobre Payment Method {Action}: {PaymentMethodId}",
                action,
                paymentMethod.Id);
        }
    }
}
