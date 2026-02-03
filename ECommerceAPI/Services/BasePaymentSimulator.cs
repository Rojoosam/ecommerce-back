using ECommerceAPI.Models;
using System.Diagnostics;

namespace ECommerceAPI.Services;

/// <summary>
/// Clase base para simuladores de pasarelas de pago
/// </summary>
public abstract class BasePaymentSimulator : IPaymentGateway
{
    private static readonly Random _random = new();
    
    // Almacén en memoria de transacciones (solo para demo)
    protected static readonly Dictionary<string, TransactionRecord> _transactions = new();

    public abstract PaymentGateway GatewayType { get; }
    protected abstract string GatewayName { get; }
    protected abstract string GatewayDescription { get; }
    protected abstract List<string> SupportedCurrencies { get; }

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Simular latencia de red (100-500ms)
        await Task.Delay(_random.Next(100, 500));

        var transactionId = GenerateTransactionId();
        var cardType = DetectCardType(request.Card.Number);
        var lastFour = request.Card.Number[^4..];

        // Determinar resultado basado en número de tarjeta
        var (status, message, errorCode, authCode) = DetermineTransactionResult(request.Card.Number);

        stopwatch.Stop();

        var response = new PaymentResponse
        {
            TransactionId = transactionId,
            Status = status,
            Message = message,
            Amount = request.Amount,
            Currency = request.Currency,
            Gateway = GatewayType,
            AuthorizationCode = authCode,
            CardLastFour = lastFour,
            CardType = cardType,
            ErrorCode = errorCode,
            Timestamp = DateTime.UtcNow,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        };

        // Guardar transacción en memoria
        _transactions[transactionId] = new TransactionRecord
        {
            TransactionId = transactionId,
            Status = status,
            Amount = request.Amount,
            Currency = request.Currency,
            Gateway = GatewayType,
            CreatedAt = DateTime.UtcNow
        };

        return response;
    }

    public async Task<RefundResponse> ProcessRefundAsync(string transactionId, RefundRequest request)
    {
        // Simular latencia
        await Task.Delay(_random.Next(100, 300));

        if (!_transactions.TryGetValue(transactionId, out var transaction))
        {
            return new RefundResponse
            {
                RefundId = string.Empty,
                OriginalTransactionId = transactionId,
                Status = PaymentStatus.Failed,
                Amount = 0,
                Currency = "USD",
                Message = "Transacción no encontrada",
                Timestamp = DateTime.UtcNow
            };
        }

        if (transaction.Status != PaymentStatus.Approved)
        {
            return new RefundResponse
            {
                RefundId = string.Empty,
                OriginalTransactionId = transactionId,
                Status = PaymentStatus.Failed,
                Amount = 0,
                Currency = transaction.Currency,
                Message = "Solo se pueden reembolsar transacciones aprobadas",
                Timestamp = DateTime.UtcNow
            };
        }

        var refundAmount = request.Amount ?? transaction.Amount;
        var refundId = $"ref_{Guid.NewGuid():N}"[..20];

        // Actualizar estado de la transacción
        transaction.Status = PaymentStatus.Refunded;
        transaction.UpdatedAt = DateTime.UtcNow;
        transaction.RefundId = refundId;

        return new RefundResponse
        {
            RefundId = refundId,
            OriginalTransactionId = transactionId,
            Status = PaymentStatus.Refunded,
            Amount = refundAmount,
            Currency = transaction.Currency,
            Message = "Reembolso procesado exitosamente",
            Timestamp = DateTime.UtcNow
        };
    }

    public GatewayInfo GetGatewayInfo()
    {
        return new GatewayInfo
        {
            Gateway = GatewayType,
            Name = GatewayName,
            Description = GatewayDescription,
            SupportedCurrencies = SupportedCurrencies,
            SupportedCardTypes = [CardType.Visa, CardType.MasterCard, CardType.AmericanExpress],
            IsActive = true,
            IsSimulated = true
        };
    }

    public static TransactionRecord? GetTransaction(string transactionId)
    {
        return _transactions.TryGetValue(transactionId, out var transaction) ? transaction : null;
    }

    private static string GenerateTransactionId()
    {
        return $"txn_{Guid.NewGuid():N}"[..24];
    }

    private static CardType DetectCardType(string cardNumber)
    {
        var cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");
        
        return cleanNumber[0] switch
        {
            '4' => CardType.Visa,
            '5' => CardType.MasterCard,
            '3' when cleanNumber.Length >= 2 && (cleanNumber[1] == '4' || cleanNumber[1] == '7') => CardType.AmericanExpress,
            _ => CardType.Unknown
        };
    }

    private (PaymentStatus status, string message, string? errorCode, string? authCode) DetermineTransactionResult(string cardNumber)
    {
        var cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");

        // Tarjetas de prueba con resultados específicos
        return cleanNumber switch
        {
            "4242424242424242" => (PaymentStatus.Approved, "Pago aprobado exitosamente", null, GenerateAuthCode()),
            "4000000000000002" => (PaymentStatus.Declined, "Tarjeta declinada", "card_declined", null),
            "4000000000000069" => (PaymentStatus.Declined, "Tarjeta expirada", "expired_card", null),
            "4000000000000127" => (PaymentStatus.Declined, "CVC incorrecto", "incorrect_cvc", null),
            "4000000000000119" => (PaymentStatus.Failed, "Error de procesamiento", "processing_error", null),
            "4000000000009995" => (PaymentStatus.Declined, "Fondos insuficientes", "insufficient_funds", null),
            "5555555555554444" => (PaymentStatus.Approved, "Pago aprobado exitosamente", null, GenerateAuthCode()),
            "378282246310005" => (PaymentStatus.Approved, "Pago aprobado exitosamente", null, GenerateAuthCode()),
            _ => SimulateRandomResult()
        };
    }

    private (PaymentStatus status, string message, string? errorCode, string? authCode) SimulateRandomResult()
    {
        // 80% éxito, 15% declinada, 5% error
        var chance = _random.Next(100);

        return chance switch
        {
            < 80 => (PaymentStatus.Approved, "Pago aprobado exitosamente", null, GenerateAuthCode()),
            < 95 => (PaymentStatus.Declined, "Tarjeta declinada por el banco", "card_declined", null),
            _ => (PaymentStatus.Failed, "Error de conexión con el procesador", "gateway_error", null)
        };
    }

    private static string GenerateAuthCode()
    {
        return _random.Next(100000, 999999).ToString();
    }
}

/// <summary>
/// Registro de transacción para almacenamiento en memoria
/// </summary>
public class TransactionRecord
{
    public string TransactionId { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public PaymentGateway Gateway { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RefundId { get; set; }
}
