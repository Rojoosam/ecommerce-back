using ECommerceAPI.Controllers;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Controllers;

[TestFixture]
public class PaymentsControllerTests
{
    private Mock<IPaymentGatewayFactory> _mockFactory = null!;
    private Mock<ILogger<PaymentsController>> _mockLogger = null!;
    private Mock<IPaymentGateway> _mockGateway = null!;
    private PaymentsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockFactory = new Mock<IPaymentGatewayFactory>();
        _mockLogger = new Mock<ILogger<PaymentsController>>();
        _mockGateway = new Mock<IPaymentGateway>();
        _controller = new PaymentsController(_mockFactory.Object, _mockLogger.Object);
    }

    private static PaymentRequest BuildValidRequest() => new()
    {
        Amount = 100.00m,
        Currency = "USD",
        Gateway = PaymentGateway.Stripe,
        Card = new CardInfo
        {
            Number = "4242424242424242",
            ExpiryMonth = 12,
            ExpiryYear = 2030,
            Cvc = "123",
            HolderName = "Test User"
        }
    };

    [Test]
    public async Task ProcessPayment_ValidRequest_ReturnsOkResult()
    {
        var paymentResponse = new PaymentResponse { TransactionId = "txn_test", Status = PaymentStatus.Approved };
        _mockGateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<PaymentRequest>())).ReturnsAsync(paymentResponse);
        _mockFactory.Setup(f => f.GetGateway(PaymentGateway.Stripe)).Returns(_mockGateway.Object);

        var result = await _controller.ProcessPayment(BuildValidRequest());

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task ProcessPayment_ValidRequest_ReturnsPaymentResponse()
    {
        var paymentResponse = new PaymentResponse { TransactionId = "txn_test", Status = PaymentStatus.Approved };
        _mockGateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<PaymentRequest>())).ReturnsAsync(paymentResponse);
        _mockFactory.Setup(f => f.GetGateway(PaymentGateway.Stripe)).Returns(_mockGateway.Object);

        var result = await _controller.ProcessPayment(BuildValidRequest());
        var ok = result.Result as OkObjectResult;

        Assert.That(ok!.Value, Is.EqualTo(paymentResponse));
    }

    [Test]
    public async Task ProcessPayment_ValidRequest_CallsGatewayOnce()
    {
        var paymentResponse = new PaymentResponse { TransactionId = "txn_test", Status = PaymentStatus.Approved };
        _mockGateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<PaymentRequest>())).ReturnsAsync(paymentResponse);
        _mockFactory.Setup(f => f.GetGateway(PaymentGateway.Stripe)).Returns(_mockGateway.Object);

        await _controller.ProcessPayment(BuildValidRequest());

        _mockGateway.Verify(g => g.ProcessPaymentAsync(It.IsAny<PaymentRequest>()), Times.Once);
    }

    [Test]
    public async Task ProcessPayment_InvalidGateway_ReturnsBadRequest()
    {
        _mockFactory.Setup(f => f.GetGateway(It.IsAny<PaymentGateway>()))
                    .Throws(new ArgumentException("Pasarela no soportada"));

        var result = await _controller.ProcessPayment(BuildValidRequest());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetTransactionStatus_NonExistentTransaction_ReturnsNotFound()
    {
        var result = _controller.GetTransactionStatus("non_existent_id");

        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public void GetAvailableGateways_ReturnsOkResult()
    {
        _mockFactory.Setup(f => f.GetAllGateways()).Returns([]);

        var result = _controller.GetAvailableGateways();

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public void GetAvailableGateways_ReturnsGatewayList()
    {
        var gateways = new List<GatewayInfo>
        {
            new() { Gateway = PaymentGateway.Stripe, Name = "Stripe" }
        };
        _mockFactory.Setup(f => f.GetAllGateways()).Returns(gateways);

        var result = _controller.GetAvailableGateways();
        var ok = result.Result as OkObjectResult;

        Assert.That(ok!.Value, Is.EqualTo(gateways));
    }

    [Test]
    public async Task ProcessRefund_NonExistentTransaction_ReturnsNotFound()
    {
        var result = await _controller.ProcessRefund("non_existent_id", new RefundRequest());

        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task ProcessRefund_FailedRefund_ReturnsBadRequest()
    {
        // Primero procesamos un pago real para tener una transacción en memoria
        var simulator = new StripeSimulatorService();
        var payment = await simulator.ProcessPaymentAsync(BuildValidRequest());

        // Configuramos el mock para devolver un reembolso fallido
        var failedRefund = new RefundResponse { Status = PaymentStatus.Failed, Message = "No se puede reembolsar" };
        _mockGateway.Setup(g => g.GatewayType).Returns(PaymentGateway.Stripe);
        _mockGateway.Setup(g => g.ProcessRefundAsync(payment.TransactionId, It.IsAny<RefundRequest>()))
                    .ReturnsAsync(failedRefund);
        _mockFactory.Setup(f => f.GetGateway(PaymentGateway.Stripe)).Returns(_mockGateway.Object);

        var result = await _controller.ProcessRefund(payment.TransactionId, new RefundRequest());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }
}
