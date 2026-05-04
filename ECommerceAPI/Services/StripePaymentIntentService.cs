using ECommerceAPI.Configuration;
using ECommerceAPI.Models;
using Microsoft.Extensions.Options;
using Stripe;

namespace ECommerceAPI.Services;

/// <summary>
/// Servicio de gestión de Payment Intents en Stripe
/// </summary>
public class StripePaymentIntentService : IStripePaymentIntentService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentIntentService> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly IStripePaymentMethodService _paymentMethodService;

    public StripePaymentIntentService(
        IOptions<StripeSettings> settings,
        ILogger<StripePaymentIntentService> logger,
        IStripePaymentMethodService paymentMethodService)
    {
        _settings = settings.Value;
        _logger = logger;
        _paymentMethodService = paymentMethodService;

        var client = new StripeClient(_settings.SecretKey);
        _paymentIntentService = new PaymentIntentService(client);
    }

    public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creando Payment Intent. Customer: {CustomerId}, Monto: {Amount} {Currency}, Order: {OrderId}",
                request.CustomerId,
                request.Amount,
                request.Currency,
                request.OrderId);

            // Verificar que el Customer esté activo
            var isActive = await _paymentMethodService.IsCustomerActiveAsync(request.CustomerId);
            if (!isActive)
            {
                _logger.LogWarning(
                    "Intento de crear Payment Intent para Customer inactivo: {CustomerId}",
                    request.CustomerId);

                return new PaymentIntentResponse
                {
                    Success = false,
                    CustomerId = request.CustomerId,
                    OrderId = request.OrderId,
                    ErrorMessage = "El Customer está inactivo. No se pueden procesar pagos."
                };
            }

            // Convertir el monto a unidades menores (centavos)
            var amountInCents = ConvertToSmallestUnit(request.Amount, request.Currency);

            // Preparar metadata
            var metadata = request.Metadata ?? new Dictionary<string, string>();
            metadata["order_id"] = request.OrderId;

            // Preparar las opciones para crear el Payment Intent
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = request.Currency.ToLower(),
                Customer = request.CustomerId,
                PaymentMethod = request.PaymentMethodId,
                Description = request.Description ?? $"Pago de orden {request.OrderId}",
                Metadata = metadata,
                Confirm = request.AutoConfirm,
                CaptureMethod = request.AutoCapture ? "automatic" : "manual",
                // Deshabilitar email de recibo automático (Laravel lo maneja)
                ReceiptEmail = null,
                // Configurar comportamiento de error
                ErrorOnRequiresAction = false, // No fallar si requiere autenticación 3D Secure
                OffSession = false, // El cliente está presente
                // Configurar para que falle rápido si la tarjeta es rechazada
                SetupFutureUsage = null
            };

            // Crear el Payment Intent
            var paymentIntent = await _paymentIntentService.CreateAsync(options);

            _logger.LogInformation(
                "Payment Intent creado. PaymentIntentId: {PaymentIntentId}, Estado: {Status}, Order: {OrderId}",
                paymentIntent.Id,
                paymentIntent.Status,
                request.OrderId);

            // Mapear la respuesta
            var response = await MapStripePaymentIntentToResponseAsync(paymentIntent);
            response.OrderId = request.OrderId;

            // Log detallado según el estado
            if (paymentIntent.Status == "succeeded")
            {
                _logger.LogInformation(
                    "✅ Pago exitoso. PaymentIntent: {PaymentIntentId}, Monto: {Amount} {Currency}, Order: {OrderId}",
                    paymentIntent.Id,
                    request.Amount,
                    request.Currency.ToUpper(),
                    request.OrderId);
            }
            else if (paymentIntent.Status == "requires_action")
            {
                _logger.LogWarning(
                    "⚠️ Payment Intent requiere acción del cliente (3D Secure). PaymentIntentId: {PaymentIntentId}, Order: {OrderId}",
                    paymentIntent.Id,
                    request.OrderId);
            }
            else if (paymentIntent.Status == "requires_capture")
            {
                _logger.LogInformation(
                    "💰 Payment Intent autorizado, pendiente de captura. PaymentIntentId: {PaymentIntentId}, Order: {OrderId}",
                    paymentIntent.Id,
                    request.OrderId);
            }

            return response;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "❌ Error de Stripe al crear Payment Intent. Customer: {CustomerId}, Order: {OrderId}, Error: {Error}",
                request.CustomerId,
                request.OrderId,
                ex.Message);

            return new PaymentIntentResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                PaymentMethodId = request.PaymentMethodId,
                OrderId = request.OrderId,
                Amount = ConvertToSmallestUnit(request.Amount, request.Currency),
                AmountDecimal = request.Amount,
                Currency = request.Currency.ToLower(),
                Status = "failed",
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}",
                ErrorCode = ex.StripeError?.Code
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Error inesperado al crear Payment Intent. Customer: {CustomerId}, Order: {OrderId}",
                request.CustomerId,
                request.OrderId);

            return new PaymentIntentResponse
            {
                Success = false,
                CustomerId = request.CustomerId,
                OrderId = request.OrderId,
                Status = "failed",
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<PaymentIntentResponse> GetPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            _logger.LogInformation(
                "Obteniendo Payment Intent: {PaymentIntentId}",
                paymentIntentId);

            var paymentIntent = await _paymentIntentService.GetAsync(paymentIntentId);

            _logger.LogInformation(
                "Payment Intent obtenido. PaymentIntentId: {PaymentIntentId}, Estado: {Status}",
                paymentIntent.Id,
                paymentIntent.Status);

            var response = await MapStripePaymentIntentToResponseAsync(paymentIntent);

            // Extraer order_id del metadata si existe
            if (paymentIntent.Metadata != null && paymentIntent.Metadata.TryGetValue("order_id", out var orderId))
            {
                response.OrderId = orderId;
            }

            return response;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe al obtener Payment Intent: {PaymentIntentId}. Error: {Error}",
                paymentIntentId,
                ex.Message);

            return new PaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = paymentIntentId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}",
                ErrorCode = ex.StripeError?.Code
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado al obtener Payment Intent: {PaymentIntentId}",
                paymentIntentId);

            return new PaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = paymentIntentId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<CancelPaymentIntentResponse> CancelPaymentIntentAsync(CancelPaymentIntentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Cancelando Payment Intent: {PaymentIntentId}, Razón: {Reason}",
                request.PaymentIntentId,
                request.CancellationReason ?? "No especificada");

            // Preparar opciones de cancelación
            var options = new PaymentIntentCancelOptions();

            if (!string.IsNullOrEmpty(request.CancellationReason))
            {
                options.CancellationReason = request.CancellationReason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    "requested_by_customer" => "requested_by_customer",
                    "abandoned" => "abandoned",
                    _ => "requested_by_customer"
                };
            }

            // Cancelar el Payment Intent
            var paymentIntent = await _paymentIntentService.CancelAsync(request.PaymentIntentId, options);

            _logger.LogInformation(
                "✅ Payment Intent cancelado exitosamente: {PaymentIntentId}, Estado: {Status}",
                paymentIntent.Id,
                paymentIntent.Status);

            return new CancelPaymentIntentResponse
            {
                Success = true,
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                CancellationReason = paymentIntent.CancellationReason,
                Message = "Payment Intent cancelado exitosamente"
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "❌ Error de Stripe al cancelar Payment Intent: {PaymentIntentId}. Error: {Error}",
                request.PaymentIntentId,
                ex.Message);

            return new CancelPaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = request.PaymentIntentId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Error inesperado al cancelar Payment Intent: {PaymentIntentId}",
                request.PaymentIntentId);

            return new CancelPaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = request.PaymentIntentId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public async Task<CapturePaymentIntentResponse> CapturePaymentIntentAsync(CapturePaymentIntentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Capturando Payment Intent: {PaymentIntentId}",
                request.PaymentIntentId);

            // Preparar opciones de captura
            var options = new PaymentIntentCaptureOptions();

            if (request.AmountToCapture.HasValue)
            {
                // Si se especifica un monto menor, capturar solo ese monto
                var paymentIntent = await _paymentIntentService.GetAsync(request.PaymentIntentId);
                var amountInCents = ConvertToSmallestUnit(request.AmountToCapture.Value, paymentIntent.Currency);
                options.AmountToCapture = amountInCents;
            }

            // Capturar el Payment Intent
            var capturedPaymentIntent = await _paymentIntentService.CaptureAsync(request.PaymentIntentId, options);

            _logger.LogInformation(
                "✅ Payment Intent capturado exitosamente: {PaymentIntentId}, Monto: {Amount} {Currency}",
                capturedPaymentIntent.Id,
                ConvertToDecimal(capturedPaymentIntent.AmountReceived, capturedPaymentIntent.Currency),
                capturedPaymentIntent.Currency.ToUpper());

            return new CapturePaymentIntentResponse
            {
                Success = true,
                PaymentIntentId = capturedPaymentIntent.Id,
                Status = capturedPaymentIntent.Status,
                AmountCaptured = ConvertToDecimal(capturedPaymentIntent.AmountReceived, capturedPaymentIntent.Currency),
                Currency = capturedPaymentIntent.Currency,
                Message = "Payment Intent capturado exitosamente"
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "❌ Error de Stripe al capturar Payment Intent: {PaymentIntentId}. Error: {Error}",
                request.PaymentIntentId,
                ex.Message);

            return new CapturePaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = request.PaymentIntentId,
                ErrorMessage = $"Error de Stripe: {ex.StripeError?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Error inesperado al capturar Payment Intent: {PaymentIntentId}",
                request.PaymentIntentId);

            return new CapturePaymentIntentResponse
            {
                Success = false,
                PaymentIntentId = request.PaymentIntentId,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Mapea un Payment Intent de Stripe a nuestra respuesta
    /// </summary>
    private async Task<PaymentIntentResponse> MapStripePaymentIntentToResponseAsync(Stripe.PaymentIntent paymentIntent)
    {
        var response = new PaymentIntentResponse
        {
            Success = paymentIntent.Status == "succeeded" || 
                      paymentIntent.Status == "requires_capture" ||
                      paymentIntent.Status == "processing",
            PaymentIntentId = paymentIntent.Id,
            CustomerId = paymentIntent.CustomerId,
            PaymentMethodId = paymentIntent.PaymentMethodId,
            Status = paymentIntent.Status,
            Amount = paymentIntent.Amount,
            AmountDecimal = ConvertToDecimal(paymentIntent.Amount, paymentIntent.Currency),
            AmountCaptured = paymentIntent.AmountReceived,
            AmountCapturedDecimal = ConvertToDecimal(paymentIntent.AmountReceived, paymentIntent.Currency),
            Currency = paymentIntent.Currency,
            Description = paymentIntent.Description,
            Created = paymentIntent.Created,
            ClientSecret = paymentIntent.ClientSecret,
            Metadata = paymentIntent.Metadata
        };

        // Para obtener los detalles del charge, necesitamos expandir el campo
        // o hacer una consulta separada si el Payment Intent tiene un latest_charge
        if (!string.IsNullOrEmpty(paymentIntent.LatestChargeId))
        {
            try
            {
                var chargeService = new ChargeService();
                var charge = await chargeService.GetAsync(paymentIntent.LatestChargeId);

                response.Charge = new ChargeDetails
                {
                    ChargeId = charge.Id,
                    Amount = charge.Amount,
                    AmountDecimal = ConvertToDecimal(charge.Amount, charge.Currency),
                    Currency = charge.Currency,
                    Status = charge.Status,
                    Created = charge.Created,
                    Captured = charge.Captured,
                    ReceiptUrl = charge.ReceiptUrl,
                    Card = charge.PaymentMethodDetails?.Card != null
                        ? new CardDetails
                        {
                            Brand = charge.PaymentMethodDetails.Card.Brand,
                            Last4 = charge.PaymentMethodDetails.Card.Last4,
                            ExpMonth = charge.PaymentMethodDetails.Card.ExpMonth,
                            ExpYear = charge.PaymentMethodDetails.Card.ExpYear,
                            Country = charge.PaymentMethodDetails.Card.Country,
                            Funding = charge.PaymentMethodDetails.Card.Funding
                        }
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo obtener detalles del charge para Payment Intent: {PaymentIntentId}", paymentIntent.Id);
            }
        }

        // Determinar si fue exitoso
        response.Success = paymentIntent.Status switch
        {
            "succeeded" => true,
            "processing" => true,
            "requires_capture" => true,
            _ => false
        };

        return response;
    }

    /// <summary>
    /// Convierte un monto en unidades mayores a unidades menores (centavos)
    /// </summary>
    private long ConvertToSmallestUnit(decimal amount, string currency)
    {
        // La mayoría de las monedas usan 2 decimales (centavos)
        // Algunas excepciones: JPY, KRW no usan decimales
        var zeroDecimalCurrencies = new[] { "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf" };

        if (zeroDecimalCurrencies.Contains(currency.ToLower()))
        {
            return (long)amount;
        }

        return (long)(amount * 100);
    }

    /// <summary>
    /// Convierte un monto en unidades menores a unidades mayores
    /// </summary>
    private decimal ConvertToDecimal(long amount, string currency)
    {
        var zeroDecimalCurrencies = new[] { "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf" };

        if (zeroDecimalCurrencies.Contains(currency.ToLower()))
        {
            return amount;
        }

        return amount / 100m;
    }
}
