using ECommerceAPI.Models;
using ECommerceAPI.Services;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Services;

[TestFixture]
public class PaymentGatewayFactoryTests
{
    private PaymentGatewayFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new PaymentGatewayFactory(
        [
            new StripeSimulatorService(),
            new PayPalSimulatorService(),
            new MercadoPagoSimulatorService()
        ]);
    }

    [Test]
    public void GetGateway_Stripe_ReturnsStripeSimulator()
    {
        var gateway = _factory.GetGateway(PaymentGateway.Stripe);

        Assert.That(gateway, Is.InstanceOf<StripeSimulatorService>());
    }

    [Test]
    public void GetGateway_PayPal_ReturnsPayPalSimulator()
    {
        var gateway = _factory.GetGateway(PaymentGateway.PayPal);

        Assert.That(gateway, Is.InstanceOf<PayPalSimulatorService>());
    }

    [Test]
    public void GetGateway_MercadoPago_ReturnsMercadoPagoSimulator()
    {
        var gateway = _factory.GetGateway(PaymentGateway.MercadoPago);

        Assert.That(gateway, Is.InstanceOf<MercadoPagoSimulatorService>());
    }

    [Test]
    public void GetGateway_UnsupportedGateway_ThrowsArgumentException()
    {
        var invalidGateway = (PaymentGateway)999;

        Assert.That(() => _factory.GetGateway(invalidGateway), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void GetAllGateways_ReturnsAllThreeGateways()
    {
        var gateways = _factory.GetAllGateways();

        Assert.That(gateways.Count(), Is.EqualTo(3));
    }

    [Test]
    public void GetAllGateways_ContainsAllGatewayTypes()
    {
        var gatewayTypes = _factory.GetAllGateways().Select(g => g.Gateway).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(gatewayTypes, Contains.Item(PaymentGateway.Stripe));
            Assert.That(gatewayTypes, Contains.Item(PaymentGateway.PayPal));
            Assert.That(gatewayTypes, Contains.Item(PaymentGateway.MercadoPago));
        });
    }
}
