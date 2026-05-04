using ECommerceAPI.Controllers;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Controllers;

[TestFixture]
public class PaymentIntentsControllerTests
{
    private Mock<IStripePaymentIntentService> _mockService = null!;
    private Mock<ILogger<PaymentIntentsController>> _mockLogger = null!;
    private PaymentIntentsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<IStripePaymentIntentService>();
        _mockLogger = new Mock<ILogger<PaymentIntentsController>>();
        _controller = new PaymentIntentsController(_mockService.Object, _mockLogger.Object);
    }

    private static CreatePaymentIntentRequest BuildValidRequest() => new()
    {
        CustomerId = "cus_test123",
        PaymentMethodId = "pm_test456",
        Amount = 100m,
        Currency = "usd",
        OrderId = "order_789"
    };

    // --- CreatePaymentIntent validation ---

    [Test]
    public async Task CreatePaymentIntent_MissingCustomerId_ReturnsBadRequest()
    {
        var request = BuildValidRequest();
        request.CustomerId = "";

        var result = await _controller.CreatePaymentIntent(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreatePaymentIntent_InvalidCustomerIdFormat_ReturnsBadRequest()
    {
        var request = BuildValidRequest();
        request.CustomerId = "usr_wrong";

        var result = await _controller.CreatePaymentIntent(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreatePaymentIntent_MissingPaymentMethodId_ReturnsBadRequest()
    {
        var request = BuildValidRequest();
        request.PaymentMethodId = "";

        var result = await _controller.CreatePaymentIntent(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreatePaymentIntent_InvalidPaymentMethodIdFormat_ReturnsBadRequest()
    {
        var request = BuildValidRequest();
        request.PaymentMethodId = "card_wrong";

        var result = await _controller.CreatePaymentIntent(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreatePaymentIntent_ZeroAmount_ReturnsBadRequest()
    {
        var request = BuildValidRequest();
        request.Amount = 0;

        var result = await _controller.CreatePaymentIntent(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreatePaymentIntent_NegativeAmount_ReturnsBadRequest()
    {
        var request = BuildValidRequest();
        request.Amount = -10m;

        var result = await _controller.CreatePaymentIntent(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreatePaymentIntent_MissingCurrency_ReturnsBadRequest()
    {
        var request = BuildValidRequest();
        request.Currency = "";

        var result = await _controller.CreatePaymentIntent(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreatePaymentIntent_MissingOrderId_ReturnsBadRequest()
    {
        var request = BuildValidRequest();
        request.OrderId = "";

        var result = await _controller.CreatePaymentIntent(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreatePaymentIntent_ValidRequest_CallsServiceOnce()
    {
        var successResponse = new PaymentIntentResponse
        {
            Success = true,
            PaymentIntentId = "pi_test",
            Status = "succeeded"
        };
        _mockService.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentRequest>()))
                    .ReturnsAsync(successResponse);

        await _controller.CreatePaymentIntent(BuildValidRequest());

        _mockService.Verify(s => s.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentRequest>()), Times.Once);
    }

    [Test]
    public async Task CreatePaymentIntent_ValidRequest_ReturnsOk()
    {
        var successResponse = new PaymentIntentResponse
        {
            Success = true,
            PaymentIntentId = "pi_test",
            Status = "succeeded"
        };
        _mockService.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentRequest>()))
                    .ReturnsAsync(successResponse);

        var result = await _controller.CreatePaymentIntent(BuildValidRequest());

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task CreatePaymentIntent_ServiceReturnsFailure_Returns500()
    {
        var failureResponse = new PaymentIntentResponse { Success = false, ErrorMessage = "Card declined" };
        _mockService.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentRequest>()))
                    .ReturnsAsync(failureResponse);

        var result = await _controller.CreatePaymentIntent(BuildValidRequest());
        var statusResult = result.Result as ObjectResult;

        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task CreatePaymentIntent_ServiceThrowsException_Returns500()
    {
        _mockService.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentRequest>()))
                    .ThrowsAsync(new Exception("Stripe connection error"));

        var result = await _controller.CreatePaymentIntent(BuildValidRequest());
        var statusResult = result.Result as ObjectResult;

        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // --- GetPaymentIntent ---

    [Test]
    public async Task GetPaymentIntent_InvalidFormat_ReturnsBadRequest()
    {
        var result = await _controller.GetPaymentIntent("not_pi_format");

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetPaymentIntent_ValidId_CallsService()
    {
        var successResponse = new PaymentIntentResponse { Success = true, PaymentIntentId = "pi_valid" };
        _mockService.Setup(s => s.GetPaymentIntentAsync("pi_valid")).ReturnsAsync(successResponse);

        await _controller.GetPaymentIntent("pi_valid");

        _mockService.Verify(s => s.GetPaymentIntentAsync("pi_valid"), Times.Once);
    }
}
