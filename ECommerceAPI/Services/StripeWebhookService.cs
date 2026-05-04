using ECommerceAPI.Configuration;
using ECommerceAPI.Models;
using Microsoft.Extensions.Options;
using Stripe;
using System.Text;
using System.Text.Json;

namespace ECommerceAPI.Services;

/// <summary>
/// Servicio de procesamiento de webhooks de Stripe
/// </summary>
public class StripeWebhookService : IStripeWebhookService
{
    private readonly StripeSettings _stripeSettings;
    private readonly LaravelNotificationSettings _laravelSettings;
    private readonly ILogger<StripeWebhookService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public StripeWebhookService(
        IOptions<StripeSettings> stripeSettings,
        IOptions<LaravelNotificationSettings> laravelSettings,
        ILogger<StripeWebhookService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _stripeSettings = stripeSettings.Value;
        _laravelSettings = laravelSettings.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WebhookResponse> ProcessWebhookAsync(string json, string signature)
    {
        try
        {
            _logger.LogInformation("Procesando webhook de Stripe");

            // Validar que el webhook secret esté configurado
            if (string.IsNullOrWhiteSpace(_stripeSettings.WebhookSecret))
            {
                _logger.LogError("Webhook secret no está configurado en appsettings");
                return new WebhookResponse
                {
                    Success = false,
                    ErrorMessage = "Webhook secret no configurado"
                };
            }

            // Construir y verificar el evento de Stripe con la firma
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    _stripeSettings.WebhookSecret,
                    throwOnApiVersionMismatch: false
                );

                _logger.LogInformation(
                    "Webhook verificado exitosamente. EventId: {EventId}, Type: {Type}",
                    stripeEvent.Id,
                    stripeEvent.Type);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error al verificar la firma del webhook");
                return new WebhookResponse
                {
                    Success = false,
                    ErrorMessage = $"Firma inválida: {ex.Message}"
                };
            }

            // Procesar el evento según su tipo
            WebhookNotification? notification = null;

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    var succeededIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (succeededIntent != null)
                    {
                        notification = ProcessPaymentIntentSucceeded(
                            stripeEvent.Id,
                            succeededIntent,
                            stripeEvent.Created);
                    }
                    break;

                case "payment_intent.payment_failed":
                    var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (failedIntent != null)
                    {
                        notification = ProcessPaymentIntentFailed(
                            stripeEvent.Id,
                            failedIntent,
                            stripeEvent.Created);
                    }
                    break;

                case "payment_intent.canceled":
                    var canceledIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (canceledIntent != null)
                    {
                        notification = ProcessPaymentIntentCanceled(
                            stripeEvent.Id,
                            canceledIntent,
                            stripeEvent.Created);
                    }
                    break;

                case "charge.refunded":
                    var charge = stripeEvent.Data.Object as Charge;
                    if (charge != null)
                    {
                        notification = ProcessChargeRefunded(
                            stripeEvent.Id,
                            charge,
                            stripeEvent.Created);
                    }
                    break;

                case "charge.dispute.created":
                    var dispute = stripeEvent.Data.Object as Dispute;
                    if (dispute != null)
                    {
                        notification = ProcessChargeDisputeCreated(
                            stripeEvent.Id,
                            dispute,
                            stripeEvent.Created);
                    }
                    break;

                default:
                    _logger.LogInformation(
                        "Tipo de evento no soportado: {EventType}",
                        stripeEvent.Type);
                    return new WebhookResponse
                    {
                        Success = true,
                        EventId = stripeEvent.Id,
                        EventType = stripeEvent.Type,
                        Message = "Evento recibido pero no procesado (tipo no soportado)",
                        SentToLaravel = false
                    };
            }

            if (notification == null)
            {
                _logger.LogWarning(
                    "No se pudo procesar el evento: {EventId}, Type: {Type}",
                    stripeEvent.Id,
                    stripeEvent.Type);
                return new WebhookResponse
                {
                    Success = false,
                    EventId = stripeEvent.Id,
                    EventType = stripeEvent.Type,
                    ErrorMessage = "No se pudo extraer datos del evento"
                };
            }

            // Enviar notificación a Laravel
            bool sentToLaravel = false;
            string? laravelUrl = null;

            if (_laravelSettings.Enabled)
            {
                // Para eventos de pago, usar el formato y endpoint específicos
                if (IsPaymentEvent(stripeEvent.Type))
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        var paymentNotification = await BuildLaravelPaymentNotificationAsync(
                            paymentIntent,
                            notification.Status);
                        sentToLaravel = await SendPaymentNotificationToLaravelAsync(paymentNotification);
                        laravelUrl = $"{_laravelSettings.BaseUrl}{_laravelSettings.PaymentsWebhookEndpoint}";
                    }
                }
                else if (IsRefundEvent(stripeEvent.Type))
                {
                    var charge = stripeEvent.Data.Object as Charge;
                    if (charge != null)
                    {
                        var refundNotification = await BuildLaravelRefundNotificationAsync(charge);
                        sentToLaravel = await SendRefundNotificationToLaravelAsync(refundNotification);
                        laravelUrl = $"{_laravelSettings.BaseUrl}{_laravelSettings.RefundsWebhookEndpoint}";
                    }
                }
                else
                {
                    sentToLaravel = await SendNotificationToLaravelAsync(notification);
                    laravelUrl = $"{_laravelSettings.BaseUrl}{_laravelSettings.WebhookEndpoint}";
                }

                if (!sentToLaravel)
                {
                    _logger.LogWarning(
                        "No se pudo enviar notificación a Laravel para evento: {EventId}",
                        stripeEvent.Id);
                }
            }
            else
            {
                _logger.LogInformation("Envío a Laravel deshabilitado en configuración");
            }

            return new WebhookResponse
            {
                Success = true,
                EventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                Message = "Webhook procesado exitosamente",
                SentToLaravel = sentToLaravel,
                LaravelNotificationUrl = laravelUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar webhook");
            return new WebhookResponse
            {
                Success = false,
                ErrorMessage = $"Error interno: {ex.Message}"
            };
        }
    }

    public WebhookNotification ProcessPaymentIntentSucceeded(
        string eventId,
        PaymentIntent paymentIntent,
        DateTime eventCreated)
    {
        _logger.LogInformation(
            "Procesando PaymentIntent Succeeded: {PaymentIntentId}",
            paymentIntent.Id);

        return new WebhookNotification
        {
            EventId = eventId,
            EventType = "payment_intent.succeeded",
            EventCreated = eventCreated,
            PaymentIntentId = paymentIntent.Id,
            ChargeId = paymentIntent.LatestChargeId,
            CustomerId = paymentIntent.CustomerId,
            Amount = paymentIntent.Amount,
            Currency = paymentIntent.Currency,
            Status = "succeeded",
            Metadata = paymentIntent.Metadata,
            AdditionalData = new Dictionary<string, object>
            {
                { "payment_method", paymentIntent.PaymentMethodId ?? "" },
                { "receipt_email", paymentIntent.ReceiptEmail ?? "" },
                { "description", paymentIntent.Description ?? "" }
            }
        };
    }

    public WebhookNotification ProcessPaymentIntentFailed(
        string eventId,
        PaymentIntent paymentIntent,
        DateTime eventCreated)
    {
        _logger.LogInformation(
            "Procesando PaymentIntent Failed: {PaymentIntentId}",
            paymentIntent.Id);

        var errorCode = paymentIntent.LastPaymentError?.Code;
        var errorMessage = paymentIntent.LastPaymentError?.Message;

        return new WebhookNotification
        {
            EventId = eventId,
            EventType = "payment_intent.payment_failed",
            EventCreated = eventCreated,
            PaymentIntentId = paymentIntent.Id,
            CustomerId = paymentIntent.CustomerId,
            Amount = paymentIntent.Amount,
            Currency = paymentIntent.Currency,
            Status = "failed",
            FailureReason = errorCode,
            ErrorMessage = errorMessage,
            Metadata = paymentIntent.Metadata,
            AdditionalData = new Dictionary<string, object>
            {
                { "payment_method", paymentIntent.PaymentMethodId ?? "" },
                { "last_payment_error_type", paymentIntent.LastPaymentError?.Type ?? "" },
                { "last_payment_error_decline_code", paymentIntent.LastPaymentError?.DeclineCode ?? "" }
            }
        };
    }

    public WebhookNotification ProcessPaymentIntentCanceled(
        string eventId,
        PaymentIntent paymentIntent,
        DateTime eventCreated)
    {
        _logger.LogInformation(
            "Procesando PaymentIntent Canceled: {PaymentIntentId}",
            paymentIntent.Id);

        return new WebhookNotification
        {
            EventId = eventId,
            EventType = "payment_intent.canceled",
            EventCreated = eventCreated,
            PaymentIntentId = paymentIntent.Id,
            CustomerId = paymentIntent.CustomerId,
            Amount = paymentIntent.Amount,
            Currency = paymentIntent.Currency,
            Status = "canceled",
            FailureReason = paymentIntent.CancellationReason?.ToString(),
            Metadata = paymentIntent.Metadata,
            AdditionalData = new Dictionary<string, object>
            {
                { "canceled_at", paymentIntent.CanceledAt?.ToString() ?? "" }
            }
        };
    }

    public WebhookNotification ProcessChargeRefunded(
        string eventId,
        Charge charge,
        DateTime eventCreated)
    {
        _logger.LogInformation(
            "Procesando Charge Refunded: {ChargeId}",
            charge.Id);

        // Obtener el último refund
        var lastRefund = charge.Refunds?.Data?.FirstOrDefault();

        return new WebhookNotification
        {
            EventId = eventId,
            EventType = "charge.refunded",
            EventCreated = eventCreated,
            ChargeId = charge.Id,
            RefundId = lastRefund?.Id,
            PaymentIntentId = charge.PaymentIntentId,
            CustomerId = charge.CustomerId,
            Amount = lastRefund?.Amount ?? charge.AmountRefunded,
            Currency = charge.Currency,
            Status = "refunded",
            FailureReason = lastRefund?.Reason,
            Metadata = charge.Metadata,
            AdditionalData = new Dictionary<string, object>
            {
                { "amount_refunded", charge.AmountRefunded },
                { "refunded", charge.Refunded },
                { "refund_status", lastRefund?.Status ?? "" }
            }
        };
    }

    public WebhookNotification ProcessChargeDisputeCreated(
        string eventId,
        Dispute dispute,
        DateTime eventCreated)
    {
        _logger.LogInformation(
            "Procesando Dispute Created: {DisputeId}",
            dispute.Id);

        return new WebhookNotification
        {
            EventId = eventId,
            EventType = "charge.dispute.created",
            EventCreated = eventCreated,
            ChargeId = dispute.ChargeId,
            PaymentIntentId = dispute.PaymentIntentId,
            Amount = dispute.Amount,
            Currency = dispute.Currency,
            Status = dispute.Status,
            FailureReason = dispute.Reason,
            Metadata = dispute.Metadata,
            AdditionalData = new Dictionary<string, object>
            {
                { "dispute_id", dispute.Id },
                { "evidence_details_due_by", dispute.EvidenceDetails?.DueBy?.ToString() ?? "" },
                { "is_charge_refundable", dispute.IsChargeRefundable }
            }
        };
    }

    public async Task<bool> SendNotificationToLaravelAsync(WebhookNotification notification)
    {
        return await SendToLaravelAsync(notification, _laravelSettings.WebhookEndpoint, notification.EventId);
    }

    public async Task<bool> SendPaymentNotificationToLaravelAsync(LaravelPaymentNotification notification)
    {
        return await SendToLaravelAsync(notification, _laravelSettings.PaymentsWebhookEndpoint, notification.ExternalId);
    }

    public async Task<bool> SendRefundNotificationToLaravelAsync(LaravelRefundNotification notification)
    {
        return await SendToLaravelAsync(notification, _laravelSettings.RefundsWebhookEndpoint, notification.ExternalId);
    }

    public async Task<bool> SendPaymentMethodNotificationToLaravelAsync(LaravelPaymentMethodNotification notification)
    {
        return await SendToLaravelAsync(notification, _laravelSettings.PaymentMethodsWebhookEndpoint, notification.ExternalId);
    }

    public async Task<LaravelRefundNotification> BuildLaravelRefundNotificationAsync(Charge charge)
    {
        _logger.LogInformation(
            "Construyendo notificación de reembolso para Laravel. ChargeId: {ChargeId}, PaymentIntentId: {PaymentIntentId}",
            charge.Id,
            charge.PaymentIntentId);

        // Extraer order_id y transaction_id del metadata del charge
        int? orderId = null;
        int? transactionId = null;

        // Primero intentar desde el metadata del charge
        if (charge.Metadata != null)
        {
            if (charge.Metadata.TryGetValue("order_id", out var orderIdStr) &&
                int.TryParse(orderIdStr, out var parsedOrderId))
            {
                orderId = parsedOrderId;
            }

            if (charge.Metadata.TryGetValue("transaction_id", out var transactionIdStr) &&
                int.TryParse(transactionIdStr, out var parsedTransactionId))
            {
                transactionId = parsedTransactionId;
            }
        }

        // Si no se encontró en el charge, intentar desde el PaymentIntent
        if (orderId == null && !string.IsNullOrEmpty(charge.PaymentIntentId))
        {
            try
            {
                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.GetAsync(charge.PaymentIntentId);

                if (paymentIntent.Metadata != null)
                {
                    if (orderId == null &&
                        paymentIntent.Metadata.TryGetValue("order_id", out var piOrderIdStr) &&
                        int.TryParse(piOrderIdStr, out var piParsedOrderId))
                    {
                        orderId = piParsedOrderId;
                    }

                    if (transactionId == null &&
                        paymentIntent.Metadata.TryGetValue("transaction_id", out var piTransactionIdStr) &&
                        int.TryParse(piTransactionIdStr, out var piParsedTransactionId))
                    {
                        transactionId = piParsedTransactionId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo obtener metadata del PaymentIntent: {PaymentIntentId}",
                    charge.PaymentIntentId);
            }
        }

        // Obtener el monto del último refund o el total reembolsado
        var lastRefund = charge.Refunds?.Data?.FirstOrDefault();
        var refundAmount = lastRefund?.Amount ?? charge.AmountRefunded;
        var amount = ConvertFromStripeAmount(refundAmount, charge.Currency);

        return new LaravelRefundNotification
        {
            OrderId = orderId,
            TransactionId = transactionId,
            ExternalId = charge.PaymentIntentId ?? charge.Id,
            Status = "refunded",
            Provider = "stripe",
            Currency = charge.Currency?.ToUpperInvariant() ?? "MXN",
            Amount = amount
        };
    }

    public async Task<LaravelPaymentNotification> BuildLaravelPaymentNotificationAsync(
        PaymentIntent paymentIntent,
        string status)
    {
        _logger.LogInformation(
            "Construyendo notificación de pago para Laravel. PaymentIntentId: {PaymentIntentId}, Status: {Status}",
            paymentIntent.Id,
            status);

        // Extraer order_id y transaction_id del metadata
        int? orderId = null;
        int? transactionId = null;

        if (paymentIntent.Metadata != null)
        {
            if (paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr) &&
                int.TryParse(orderIdStr, out var parsedOrderId))
            {
                orderId = parsedOrderId;
            }

            if (paymentIntent.Metadata.TryGetValue("transaction_id", out var transactionIdStr) &&
                int.TryParse(transactionIdStr, out var parsedTransactionId))
            {
                transactionId = parsedTransactionId;
            }
        }

        // Convertir monto de centavos a unidades base
        var amount = ConvertFromStripeAmount(paymentIntent.Amount, paymentIntent.Currency);

        // Construir la notificación
        var notification = new LaravelPaymentNotification
        {
            OrderId = orderId,
            TransactionId = transactionId,
            ExternalId = paymentIntent.Id,
            Status = status,
            Provider = "stripe",
            Currency = paymentIntent.Currency?.ToUpperInvariant() ?? "MXN",
            Amount = amount
        };

        // Obtener detalles del método de pago
        if (!string.IsNullOrEmpty(paymentIntent.PaymentMethodId))
        {
            try
            {
                var paymentMethodService = new PaymentMethodService();
                var paymentMethod = await paymentMethodService.GetAsync(paymentIntent.PaymentMethodId);

                notification.PaymentMethod = new LaravelPaymentMethodInfo
                {
                    ExternalId = paymentMethod.Id,
                    Brand = paymentMethod.Card?.Brand != null
                        ? System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(paymentMethod.Card.Brand)
                        : null,
                    LastFour = paymentMethod.Card?.Last4,
                    ExpMonth = paymentMethod.Card?.ExpMonth.ToString(),
                    ExpYear = paymentMethod.Card?.ExpYear.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo obtener detalles del PaymentMethod: {PaymentMethodId}",
                    paymentIntent.PaymentMethodId);

                // Enviar al menos el ID externo del método de pago
                notification.PaymentMethod = new LaravelPaymentMethodInfo
                {
                    ExternalId = paymentIntent.PaymentMethodId
                };
            }
        }

        return notification;
    }

    private async Task<bool> SendToLaravelAsync<T>(T payload, string endpoint, string logIdentifier)
    {
        try
        {
            _logger.LogInformation(
                "Enviando notificación a Laravel. Endpoint: {Endpoint}, Ref: {Ref}",
                endpoint,
                logIdentifier);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(_laravelSettings.TimeoutSeconds);

            var url = $"{_laravelSettings.BaseUrl}{endpoint}";

            var jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Agregar header de autenticación si está configurado
            if (!string.IsNullOrWhiteSpace(_laravelSettings.AuthToken))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_laravelSettings.AuthToken}");
            }

            // Reintentos
            for (int attempt = 1; attempt <= _laravelSettings.RetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "Intento {Attempt} de {Total} para enviar a Laravel ({Endpoint})",
                        attempt,
                        _laravelSettings.RetryAttempts,
                        endpoint);

                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation(
                            "Notificación enviada exitosamente a Laravel en intento {Attempt}",
                            attempt);
                        return true;
                    }

                    _logger.LogWarning(
                        "Respuesta no exitosa de Laravel: {StatusCode}. Intento {Attempt}",
                        response.StatusCode,
                        attempt);

                    if (attempt < _laravelSettings.RetryAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex,
                        "Error HTTP al enviar a Laravel en intento {Attempt}",
                        attempt);

                    if (attempt < _laravelSettings.RetryAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                    }
                }
            }

            _logger.LogError(
                "No se pudo enviar notificación a Laravel después de {Attempts} intentos",
                _laravelSettings.RetryAttempts);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar notificación a Laravel");
            return false;
        }
    }

    private static bool IsPaymentEvent(string eventType)
    {
        return eventType is "payment_intent.succeeded"
            or "payment_intent.payment_failed"
            or "payment_intent.canceled";
    }

    private static bool IsRefundEvent(string eventType)
    {
        return eventType is "charge.refunded";
    }

    private static decimal ConvertFromStripeAmount(long? amountInCents, string? currency)
    {
        if (amountInCents == null) return 0;

        var zeroDecimalCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga",
            "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf"
        };

        if (currency != null && zeroDecimalCurrencies.Contains(currency))
            return amountInCents.Value;

        return amountInCents.Value / 100m;
    }
}
