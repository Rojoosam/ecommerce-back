using ECommerceAPI.Configuration;
using ECommerceAPI.Models;
using Microsoft.Extensions.Options;
using Stripe;
using System.Diagnostics;

namespace ECommerceAPI.Services;

/// <summary>
/// Servicio de integración real con Stripe
/// </summary>
public class StripePaymentService : IPaymentGateway
{
    private readonly StripeSettings _settings;
    private readonly string _secretKey;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;

    public PaymentGateway GatewayType => PaymentGateway.Stripe;

    public StripePaymentService(
        IOptions<StripeSettings> settings,
        IConfiguration configuration,
        ILogger<StripePaymentService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Leer la clave directamente de IConfiguration
        _secretKey = configuration["Stripe:SecretKey"] ?? string.Empty;

        var client = new StripeClient(_secretKey);
        _paymentIntentService = new PaymentIntentService(client);
        _refundService = new RefundService(client);
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Procesando pago real con Stripe: {Amount} {Currency}",
                request.Amount,
                request.Currency);

            // Crear el Payment Intent
            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = ConvertToSmallestUnit(request.Amount, request.Currency),
                Currency = request.Currency.ToLower(),
                Description = request.Description ?? "Cargo desde E-Commerce API",
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            // Si se proporciona un Payment Method ID, asociarlo y confirmar
            if (!string.IsNullOrEmpty(request.StripePaymentMethodId))
            {
                paymentIntentOptions.PaymentMethod = request.StripePaymentMethodId;
                paymentIntentOptions.Confirm = true;
                paymentIntentOptions.AutomaticPaymentMethods = null; // No usar cuando se especifica un Payment Method
            }
            // Si se proporciona información de tarjeta (solo para testing con tarjetas de prueba)
            // NOTA: En producción, el cliente debe tokenizar la tarjeta con Stripe.js
            else if (request.Card != null)
            {
                // Advertir que esto es solo para testing
                _logger.LogWarning(
                    "Usando información de tarjeta directamente desde el servidor. " +
                    "Esto es solo para testing. En producción use Stripe.js para tokenizar.");

                // Crear el Payment Intent sin confirmar primero
                var unconfirmedIntent = await _paymentIntentService.CreateAsync(paymentIntentOptions);

                // Confirmar el Payment Intent con la información de tarjeta
                var confirmOptions = new PaymentIntentConfirmOptions
                {
                    PaymentMethod = await CreatePaymentMethodIdFromCard(request.Card)
                };

                stopwatch.Stop();

                var confirmedIntent = await _paymentIntentService.ConfirmAsync(unconfirmedIntent.Id, confirmOptions);

                _logger.LogInformation(
                    "Payment Intent creado y confirmado exitosamente: {PaymentIntentId}, Status: {Status}",
                    confirmedIntent.Id,
                    confirmedIntent.Status);

                return MapStripeResponseToPaymentResponse(confirmedIntent, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                // Si no se proporciona ni Payment Method ni Card, habilitar métodos automáticos
                paymentIntentOptions.AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                };
            }

            // Agregar Customer ID si se proporciona
            if (!string.IsNullOrEmpty(request.CustomerId))
            {
                paymentIntentOptions.Customer = request.CustomerId;
            }

            var paymentIntent = await _paymentIntentService.CreateAsync(paymentIntentOptions);

            stopwatch.Stop();

            _logger.LogInformation(
                "Payment Intent creado exitosamente: {PaymentIntentId}, Status: {Status}",
                paymentIntent.Id,
                paymentIntent.Status);

            return MapStripeResponseToPaymentResponse(paymentIntent, stopwatch.ElapsedMilliseconds);
        }
        catch (StripeException ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Error de Stripe procesando pago: {ErrorCode} - {ErrorMessage}",
                ex.StripeError?.Code,
                ex.Message);

            return new PaymentResponse
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                Status = PaymentStatus.Failed,
                Message = ex.StripeError?.Message ?? "Error procesando el pago",
                Amount = request.Amount,
                Currency = request.Currency,
                Gateway = PaymentGateway.Stripe,
                ErrorCode = ex.StripeError?.Code,
                Timestamp = DateTime.UtcNow,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Error inesperado procesando pago con Stripe");

            return new PaymentResponse
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                Status = PaymentStatus.Failed,
                Message = "Error interno procesando el pago",
                Amount = request.Amount,
                Currency = request.Currency,
                Gateway = PaymentGateway.Stripe,
                ErrorCode = "internal_error",
                Timestamp = DateTime.UtcNow,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<RefundResponse> ProcessRefundAsync(string transactionId, RefundRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Procesando reembolso para transacción: {TransactionId}",
                transactionId);

            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = transactionId,
                Reason = request.Reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    "requested_by_customer" => "requested_by_customer",
                    _ => null
                }
            };

            // Si se especifica un monto parcial
            if (request.Amount.HasValue)
            {
                // Obtener el Payment Intent para saber la moneda
                var paymentIntent = await _paymentIntentService.GetAsync(transactionId);
                refundOptions.Amount = ConvertToSmallestUnit(request.Amount.Value, paymentIntent.Currency);
            }

            var refund = await _refundService.CreateAsync(refundOptions);

            _logger.LogInformation(
                "Reembolso creado exitosamente: {RefundId}, Status: {Status}",
                refund.Id,
                refund.Status);

            return new RefundResponse
            {
                RefundId = refund.Id,
                OriginalTransactionId = transactionId,
                Status = refund.Status switch
                {
                    "succeeded" => PaymentStatus.Refunded,
                    "pending" => PaymentStatus.Pending,
                    "failed" => PaymentStatus.Failed,
                    "canceled" => PaymentStatus.Failed,
                    _ => PaymentStatus.Failed
                },
                Amount = ConvertFromSmallestUnit(refund.Amount, refund.Currency),
                Currency = refund.Currency.ToUpper(),
                Message = "Reembolso procesado exitosamente",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Error de Stripe procesando reembolso: {ErrorCode} - {ErrorMessage}",
                ex.StripeError?.Code,
                ex.Message);

            return new RefundResponse
            {
                RefundId = string.Empty,
                OriginalTransactionId = transactionId,
                Status = PaymentStatus.Failed,
                Amount = request.Amount ?? 0,
                Currency = "USD",
                Message = ex.StripeError?.Message ?? "Error procesando el reembolso",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public GatewayInfo GetGatewayInfo()
    {
        return new GatewayInfo
        {
            Gateway = PaymentGateway.Stripe,
            Name = "Stripe",
            Description = "Stripe es una plataforma de pagos en línea líder mundial. " +
                         "Esta es una integración real con Stripe.",
            SupportedCurrencies =
            [
                "USD", "EUR", "GBP", "MXN", "CAD", "AUD", "JPY", "BRL",
                "CHF", "CNY", "DKK", "HKD", "INR", "NOK", "NZD", "SEK", "SGD"
            ],
            SupportedCardTypes = [Models.CardType.Visa, Models.CardType.MasterCard, Models.CardType.AmericanExpress],
            IsActive = !string.IsNullOrEmpty(_settings.SecretKey),
            IsSimulated = false
        };
    }

    /// <summary>
    /// Crea un PaymentMethod de Stripe desde información de tarjeta.
    /// SOLO para testing con tarjetas de prueba. En producción, use Stripe.js en el cliente.
    /// </summary>
    private async Task<string> CreatePaymentMethodIdFromCard(CardInfo card)
    {
        var client = new StripeClient(_secretKey);
        var pmService = new PaymentMethodService(client);

        var pmOptions = new PaymentMethodCreateOptions
        {
            Type = "card",
            Card = new PaymentMethodCardOptions
            {
                Number = card.Number.Replace(" ", "").Replace("-", ""),
                ExpMonth = card.ExpiryMonth,
                ExpYear = card.ExpiryYear,
                Cvc = card.Cvc
            },
            BillingDetails = string.IsNullOrWhiteSpace(card.HolderName)
                ? null
                : new PaymentMethodBillingDetailsOptions { Name = card.HolderName }
        };

        var paymentMethod = await pmService.CreateAsync(pmOptions);
        return paymentMethod.Id;
    }

    /// <summary>
    /// Mapea la respuesta de Stripe a nuestro modelo de respuesta
    /// </summary>
    private PaymentResponse MapStripeResponseToPaymentResponse(PaymentIntent paymentIntent, long processingTimeMs)
    {
        var status = paymentIntent.Status switch
        {
            "succeeded" => PaymentStatus.Approved,
            "processing" => PaymentStatus.Pending,
            "requires_payment_method" => PaymentStatus.Failed,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.Pending,
            "canceled" => PaymentStatus.Failed,
            _ => PaymentStatus.Failed
        };

        var paymentMethod = paymentIntent.PaymentMethod as PaymentMethod;
        var card = paymentMethod?.Card;

        return new PaymentResponse
        {
            TransactionId = paymentIntent.Id,
            Status = status,
            Message = GetStatusMessage(paymentIntent.Status),
            Amount = ConvertFromSmallestUnit(paymentIntent.Amount, paymentIntent.Currency),
            Currency = paymentIntent.Currency.ToUpper(),
            Gateway = PaymentGateway.Stripe,
            AuthorizationCode = paymentIntent.Id,
            CardLastFour = card?.Last4,
            CardType = card?.Brand switch
            {
                "visa" => Models.CardType.Visa,
                "mastercard" => Models.CardType.MasterCard,
                "amex" => Models.CardType.AmericanExpress,
                _ => Models.CardType.Unknown
            },
            ErrorCode = paymentIntent.LastPaymentError?.Code,
            Timestamp = DateTime.UtcNow,
            ProcessingTimeMs = processingTimeMs,
            StripePaymentIntentId = paymentIntent.Id,
            StripeClientSecret = paymentIntent.ClientSecret,
            RequiresAction = paymentIntent.Status == "requires_action"
        };
    }

    /// <summary>
    /// Convierte el monto a la unidad más pequeña de la moneda
    /// (centavos para USD, céntimos para EUR, etc.)
    /// </summary>
    private long ConvertToSmallestUnit(decimal amount, string currency)
    {
        // Monedas sin decimales (Yen japonés, won coreano, etc.)
        var zeroDecimalCurrencies = new HashSet<string> { "JPY", "KRW", "VND", "CLP" };

        if (zeroDecimalCurrencies.Contains(currency.ToUpper()))
        {
            return (long)Math.Round(amount);
        }

        // La mayoría de las monedas usan 2 decimales
        return (long)Math.Round(amount * 100);
    }

    /// <summary>
    /// Convierte desde la unidad más pequeña al monto decimal
    /// </summary>
    private decimal ConvertFromSmallestUnit(long amount, string currency)
    {
        var zeroDecimalCurrencies = new HashSet<string> { "jpy", "krw", "vnd", "clp" };

        if (zeroDecimalCurrencies.Contains(currency.ToLower()))
        {
            return amount;
        }

        return amount / 100m;
    }

    /// <summary>
    /// Obtiene un mensaje descriptivo basado en el estado del Payment Intent
    /// </summary>
    private string GetStatusMessage(string status)
    {
        return status switch
        {
            "succeeded" => "Pago procesado exitosamente",
            "processing" => "El pago está siendo procesado",
            "requires_payment_method" => "Se requiere un método de pago válido",
            "requires_confirmation" => "El pago requiere confirmación",
            "requires_action" => "El pago requiere autenticación adicional (3D Secure)",
            "canceled" => "El pago fue cancelado",
            _ => "Estado desconocido"
        };
    }
}
