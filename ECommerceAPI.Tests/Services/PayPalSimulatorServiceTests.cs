using ECommerceAPI.Models;
using ECommerceAPI.Services;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Services;

[TestFixture]
public class PayPalSimulatorServiceTests
{
    private PayPalSimulatorService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new PayPalSimulatorService();
    }

    [Test]
    public void GatewayType_ReturnsPayPal()
    {
        Assert.That(_service.GatewayType, Is.EqualTo(PaymentGateway.PayPal));
    }

    [Test]
    public void GetGatewayInfo_ReturnsPayPalInfo()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.Gateway, Is.EqualTo(PaymentGateway.PayPal));
        Assert.That(info.Name, Is.EqualTo("PayPal"));
    }

    [Test]
    public void GetGatewayInfo_IsSimulated()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.IsSimulated, Is.True);
    }

    [Test]
    public void GetGatewayInfo_IsActive()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.IsActive, Is.True);
    }

    [Test]
    public void GetGatewayInfo_SupportedCurrenciesContainsUSD()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.SupportedCurrencies, Contains.Item("USD"));
    }

    [Test]
    public async Task ProcessPaymentAsync_ApprovedCard_ReturnsApproved()
    {
        var request = new PaymentRequest
        {
            Amount = 75m,
            Currency = "USD",
            Gateway = PaymentGateway.PayPal,
            Card = new CardInfo
            {
                Number = "4242424242424242",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Cvc = "123",
                HolderName = "Test User"
            }
        };

        var response = await _service.ProcessPaymentAsync(request);

        Assert.That(response.Status, Is.EqualTo(PaymentStatus.Approved));
        Assert.That(response.Gateway, Is.EqualTo(PaymentGateway.PayPal));
    }

    [Test]
    public async Task ProcessPaymentAsync_DeclinedCard_ReturnsDeclined()
    {
        var request = new PaymentRequest
        {
            Amount = 75m,
            Currency = "USD",
            Gateway = PaymentGateway.PayPal,
            Card = new CardInfo
            {
                Number = "4000000000000002",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Cvc = "123",
                HolderName = "Test User"
            }
        };

        var response = await _service.ProcessPaymentAsync(request);

        Assert.That(response.Status, Is.EqualTo(PaymentStatus.Declined));
    }

    [Test]
    public async Task ProcessPaymentAsync_WithoutCard_ThrowsArgumentException()
    {
        var request = new PaymentRequest
        {
            Amount = 75m,
            Currency = "USD",
            Gateway = PaymentGateway.PayPal,
            Card = null
        };

        Assert.That(
            async () => await _service.ProcessPaymentAsync(request),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task ProcessRefundAsync_AfterApprovedPayment_ReturnsRefunded()
    {
        var payment = await _service.ProcessPaymentAsync(new PaymentRequest
        {
            Amount = 75m,
            Currency = "USD",
            Gateway = PaymentGateway.PayPal,
            Card = new CardInfo
            {
                Number = "4242424242424242",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Cvc = "123",
                HolderName = "Test User"
            }
        });

        var refund = await _service.ProcessRefundAsync(payment.TransactionId, new RefundRequest());

        Assert.That(refund.Status, Is.EqualTo(PaymentStatus.Refunded));
    }
}
