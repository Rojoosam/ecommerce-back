using ECommerceAPI.Models;
using ECommerceAPI.Services;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Services;

[TestFixture]
public class BasePaymentSimulatorTests
{
    private StripeSimulatorService _simulator = null!;

    [SetUp]
    public void SetUp()
    {
        _simulator = new StripeSimulatorService();
    }

    private static PaymentRequest BuildRequest(string cardNumber) => new()
    {
        Amount = 100.00m,
        Currency = "USD",
        Gateway = PaymentGateway.Stripe,
        Card = new CardInfo
        {
            Number = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = 2030,
            Cvc = "123",
            HolderName = "Test User"
        }
    };

    [Test]
    public async Task ProcessPaymentAsync_ApprovedCard_ReturnsApprovedStatus()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4242424242424242"));

        Assert.That(response.Status, Is.EqualTo(PaymentStatus.Approved));
    }

    [Test]
    public async Task ProcessPaymentAsync_DeclinedCard_ReturnsDeclinedStatus()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4000000000000002"));

        Assert.That(response.Status, Is.EqualTo(PaymentStatus.Declined));
    }

    [Test]
    public async Task ProcessPaymentAsync_ExpiredCard_ReturnsDeclinedStatus()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4000000000000069"));

        Assert.That(response.Status, Is.EqualTo(PaymentStatus.Declined));
    }

    [Test]
    public async Task ProcessPaymentAsync_InsufficientFundsCard_ReturnsDeclinedStatus()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4000000000009995"));

        Assert.That(response.Status, Is.EqualTo(PaymentStatus.Declined));
    }

    [Test]
    public async Task ProcessPaymentAsync_ProcessingErrorCard_ReturnsFailedStatus()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4000000000000119"));

        Assert.That(response.Status, Is.EqualTo(PaymentStatus.Failed));
    }

    [Test]
    public async Task ProcessPaymentAsync_SetsCorrectGatewayType()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4242424242424242"));

        Assert.That(response.Gateway, Is.EqualTo(PaymentGateway.Stripe));
    }

    [Test]
    public async Task ProcessPaymentAsync_SetsCorrectAmount()
    {
        var request = BuildRequest("4242424242424242");
        var response = await _simulator.ProcessPaymentAsync(request);

        Assert.That(response.Amount, Is.EqualTo(request.Amount));
    }

    [Test]
    public async Task ProcessPaymentAsync_SetsLastFourDigits()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4242424242424242"));

        Assert.That(response.CardLastFour, Is.EqualTo("4242"));
    }

    [Test]
    public async Task ProcessPaymentAsync_VisaCard_SetsCardTypeVisa()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4242424242424242"));

        Assert.That(response.CardType, Is.EqualTo(CardType.Visa));
    }

    [Test]
    public async Task ProcessPaymentAsync_MasterCard_SetsCardTypeMasterCard()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("5555555555554444"));

        Assert.That(response.CardType, Is.EqualTo(CardType.MasterCard));
    }

    [Test]
    public async Task ProcessPaymentAsync_AmexCard_SetsCardTypeAmericanExpress()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("378282246310005"));

        Assert.That(response.CardType, Is.EqualTo(CardType.AmericanExpress));
    }

    [Test]
    public async Task ProcessPaymentAsync_GeneratesTransactionId()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4242424242424242"));

        Assert.That(response.TransactionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task ProcessPaymentAsync_SavesTransactionInMemory()
    {
        var response = await _simulator.ProcessPaymentAsync(BuildRequest("4242424242424242"));
        var stored = BasePaymentSimulator.GetTransaction(response.TransactionId);

        Assert.That(stored, Is.Not.Null);
    }

    [Test]
    public async Task ProcessRefundAsync_ApprovedTransaction_ReturnsRefunded()
    {
        var payment = await _simulator.ProcessPaymentAsync(BuildRequest("4242424242424242"));

        var refund = await _simulator.ProcessRefundAsync(payment.TransactionId, new RefundRequest());

        Assert.That(refund.Status, Is.EqualTo(PaymentStatus.Refunded));
    }

    [Test]
    public async Task ProcessRefundAsync_ApprovedTransaction_ReturnsCorrectAmount()
    {
        var payment = await _simulator.ProcessPaymentAsync(BuildRequest("4242424242424242"));

        var refund = await _simulator.ProcessRefundAsync(payment.TransactionId, new RefundRequest());

        Assert.That(refund.Amount, Is.EqualTo(100.00m));
    }

    [Test]
    public async Task ProcessRefundAsync_NonExistentTransaction_ReturnsFailed()
    {
        var refund = await _simulator.ProcessRefundAsync("non_existent_txn_id", new RefundRequest());

        Assert.That(refund.Status, Is.EqualTo(PaymentStatus.Failed));
    }

    [Test]
    public async Task ProcessRefundAsync_DeclinedTransaction_ReturnsFailed()
    {
        var payment = await _simulator.ProcessPaymentAsync(BuildRequest("4000000000000002"));

        var refund = await _simulator.ProcessRefundAsync(payment.TransactionId, new RefundRequest());

        Assert.That(refund.Status, Is.EqualTo(PaymentStatus.Failed));
    }

    [Test]
    public void GetGatewayInfo_ReturnsCorrectStripeInfo()
    {
        var info = _simulator.GetGatewayInfo();

        Assert.Multiple(() =>
        {
            Assert.That(info.Gateway, Is.EqualTo(PaymentGateway.Stripe));
            Assert.That(info.Name, Is.EqualTo("Stripe"));
            Assert.That(info.IsActive, Is.True);
            Assert.That(info.IsSimulated, Is.True);
        });
    }

    [Test]
    public void GetGatewayInfo_ContainsSupportedCurrencies()
    {
        var info = _simulator.GetGatewayInfo();

        Assert.That(info.SupportedCurrencies, Contains.Item("USD"));
    }

    [Test]
    public static void GetTransaction_UnknownId_ReturnsNull()
    {
        var result = BasePaymentSimulator.GetTransaction("completely_unknown_id");

        Assert.That(result, Is.Null);
    }
}
