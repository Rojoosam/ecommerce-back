using ECommerceAPI.Models;
using ECommerceAPI.Services;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Services;

[TestFixture]
public class StripeSimulatorServiceTests
{
    private StripeSimulatorService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new StripeSimulatorService();
    }

    [Test]
    public void GatewayType_ReturnsStripe()
    {
        Assert.That(_service.GatewayType, Is.EqualTo(PaymentGateway.Stripe));
    }

    [Test]
    public void GetGatewayInfo_ReturnsStripeInfo()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.Gateway, Is.EqualTo(PaymentGateway.Stripe));
        Assert.That(info.Name, Is.EqualTo("Stripe"));
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
    public void GetGatewayInfo_SupportedCurrenciesNotEmpty()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.SupportedCurrencies, Is.Not.Empty);
    }

    [Test]
    public void GetGatewayInfo_ContainsUSD()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.SupportedCurrencies, Contains.Item("USD"));
    }

    [Test]
    public void GetGatewayInfo_SupportedCardTypesContainsVisa()
    {
        var info = _service.GetGatewayInfo();

        Assert.That(info.SupportedCardTypes, Contains.Item(CardType.Visa));
    }

    [Test]
    public async Task ProcessPaymentAsync_WithoutCard_ThrowsArgumentException()
    {
        var request = new PaymentRequest
        {
            Amount = 50m,
            Currency = "USD",
            Gateway = PaymentGateway.Stripe,
            Card = null
        };

        Assert.That(
            async () => await _service.ProcessPaymentAsync(request),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public async Task ProcessPaymentAsync_CvcIncorrectCard_ReturnsDeclined()
    {
        var request = new PaymentRequest
        {
            Amount = 50m,
            Currency = "USD",
            Gateway = PaymentGateway.Stripe,
            Card = new CardInfo
            {
                Number = "4000000000000127",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Cvc = "000",
                HolderName = "Test"
            }
        };

        var response = await _service.ProcessPaymentAsync(request);

        Assert.That(response.Status, Is.EqualTo(PaymentStatus.Declined));
        Assert.That(response.ErrorCode, Is.EqualTo("incorrect_cvc"));
    }

    [Test]
    public async Task ProcessPaymentAsync_ApprovedCard_SetsAuthorizationCode()
    {
        var request = new PaymentRequest
        {
            Amount = 50m,
            Currency = "USD",
            Gateway = PaymentGateway.Stripe,
            Card = new CardInfo
            {
                Number = "4242424242424242",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Cvc = "123",
                HolderName = "Test"
            }
        };

        var response = await _service.ProcessPaymentAsync(request);

        Assert.That(response.AuthorizationCode, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task ProcessPaymentAsync_SetsProcessingTime()
    {
        var request = new PaymentRequest
        {
            Amount = 50m,
            Currency = "USD",
            Gateway = PaymentGateway.Stripe,
            Card = new CardInfo
            {
                Number = "4242424242424242",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Cvc = "123",
                HolderName = "Test"
            }
        };

        var response = await _service.ProcessPaymentAsync(request);

        Assert.That(response.ProcessingTimeMs, Is.GreaterThan(0));
    }
}
