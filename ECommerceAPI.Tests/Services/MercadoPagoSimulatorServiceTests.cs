using ECommerceAPI.Models;
using ECommerceAPI.Services;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Services;

[TestFixture]
public class MercadoPagoSimulatorServiceTests
{
    private MercadoPagoSimulatorService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new MercadoPagoSimulatorService();
    }

    [Test]
    public void GatewayType_ReturnsMercadoPago()
    {
        Assert.That(_service.GatewayType, Is.EqualTo(PaymentGateway.MercadoPago));
    }

    [Test]
    public void GetGatewayInfo_ReturnsMercadoPagoInfo()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.Gateway, Is.EqualTo(PaymentGateway.MercadoPago));
        Assert.That(info.Name, Is.EqualTo("MercadoPago"));
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
    public void GetGatewayInfo_SupportedCurrenciesContainsMXN()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.SupportedCurrencies, Contains.Item("MXN"));
    }

    [Test]
    public async Task ProcessPaymentAsync_ApprovedCard_ReturnsApproved()
    {
        var request = new PaymentRequest
        {
            Amount = 200m,
            Currency = "MXN",
            Gateway = PaymentGateway.MercadoPago,
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
        Assert.That(response.Gateway, Is.EqualTo(PaymentGateway.MercadoPago));
    }

    [Test]
    public async Task ProcessPaymentAsync_InsufficientFunds_ReturnsDeclined()
    {
        var request = new PaymentRequest
        {
            Amount = 200m,
            Currency = "MXN",
            Gateway = PaymentGateway.MercadoPago,
            Card = new CardInfo
            {
                Number = "4000000000009995",
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
            Amount = 200m,
            Currency = "MXN",
            Gateway = PaymentGateway.MercadoPago,
            Card = null
        };

        Assert.That(
            async () => await _service.ProcessPaymentAsync(request),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task ProcessRefundAsync_NonExistentTransaction_ReturnsFailed()
    {
        var refund = await _service.ProcessRefundAsync("non_existent_txn", new RefundRequest());

        Assert.That(refund.Status, Is.EqualTo(PaymentStatus.Failed));
    }

    [Test]
    public async Task ProcessRefundAsync_DeclinedTransaction_ReturnsFailed()
    {
        var payment = await _service.ProcessPaymentAsync(new PaymentRequest
        {
            Amount = 200m,
            Currency = "MXN",
            Gateway = PaymentGateway.MercadoPago,
            Card = new CardInfo
            {
                Number = "4000000000000002",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Cvc = "123",
                HolderName = "Test User"
            }
        });

        var refund = await _service.ProcessRefundAsync(payment.TransactionId, new RefundRequest());

        Assert.That(refund.Status, Is.EqualTo(PaymentStatus.Failed));
    }
}
