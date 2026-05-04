using ECommerceAPI.Controllers;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Controllers;

[TestFixture]
public class RefundsControllerTests
{
    private Mock<IStripeRefundService> _mockService = null!;
    private Mock<ILogger<RefundsController>> _mockLogger = null!;
    private RefundsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<IStripeRefundService>();
        _mockLogger = new Mock<ILogger<RefundsController>>();
        _controller = new RefundsController(_mockService.Object, _mockLogger.Object);
    }

    // --- CreateRefundForPaymentIntent ---

    [Test]
    public async Task CreateRefundForPaymentIntent_EmptyId_ReturnsBadRequest()
    {
        var result = await _controller.CreateRefundForPaymentIntent("   ", new RefundRequest());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateRefundForPaymentIntent_InvalidFormat_ReturnsBadRequest()
    {
        var result = await _controller.CreateRefundForPaymentIntent("ch_not_pi", new RefundRequest());

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateRefundForPaymentIntent_ValidId_CallsServiceOnce()
    {
        var successResponse = new RefundResponse
        {
            RefundId = "re_test",
            Status = PaymentStatus.Refunded,
            Message = "OK"
        };
        _mockService.Setup(s => s.CreateRefundForPaymentIntentAsync("pi_valid_123", It.IsAny<RefundRequest>()))
                    .ReturnsAsync(successResponse);

        await _controller.CreateRefundForPaymentIntent("pi_valid_123", new RefundRequest());

        _mockService.Verify(s => s.CreateRefundForPaymentIntentAsync("pi_valid_123", It.IsAny<RefundRequest>()), Times.Once);
    }

    [Test]
    public async Task CreateRefundForPaymentIntent_ValidRequest_ReturnsOk()
    {
        var successResponse = new RefundResponse
        {
            RefundId = "re_test",
            Status = PaymentStatus.Refunded,
            Message = "Reembolso exitoso"
        };
        _mockService.Setup(s => s.CreateRefundForPaymentIntentAsync(It.IsAny<string>(), It.IsAny<RefundRequest>()))
                    .ReturnsAsync(successResponse);

        var result = await _controller.CreateRefundForPaymentIntent("pi_valid_123", new RefundRequest());

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task CreateRefundForPaymentIntent_ServiceThrowsInvalidOperation_Returns500()
    {
        _mockService.Setup(s => s.CreateRefundForPaymentIntentAsync(It.IsAny<string>(), It.IsAny<RefundRequest>()))
                    .ThrowsAsync(new InvalidOperationException("Payment intent already refunded"));

        var result = await _controller.CreateRefundForPaymentIntent("pi_valid_123", new RefundRequest());
        var statusResult = result.Result as ObjectResult;

        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task CreateRefundForPaymentIntent_ServiceThrowsException_Returns500()
    {
        _mockService.Setup(s => s.CreateRefundForPaymentIntentAsync(It.IsAny<string>(), It.IsAny<RefundRequest>()))
                    .ThrowsAsync(new Exception("Unexpected error"));

        var result = await _controller.CreateRefundForPaymentIntent("pi_valid_123", new RefundRequest());
        var statusResult = result.Result as ObjectResult;

        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task CreateRefundForPaymentIntent_WithPartialAmount_CallsServiceWithCorrectAmount()
    {
        var refundRequest = new RefundRequest { Amount = 50m, Reason = "Partial refund" };
        var successResponse = new RefundResponse
        {
            RefundId = "re_test",
            Status = PaymentStatus.Refunded,
            Amount = 50m
        };
        _mockService.Setup(s => s.CreateRefundForPaymentIntentAsync("pi_valid_123", refundRequest))
                    .ReturnsAsync(successResponse);

        var result = await _controller.CreateRefundForPaymentIntent("pi_valid_123", refundRequest);
        var ok = result.Result as OkObjectResult;
        var response = ok!.Value as RefundResponse;

        Assert.That(response!.Amount, Is.EqualTo(50m));
    }
}
