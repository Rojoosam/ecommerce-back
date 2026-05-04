using ECommerceAPI.Controllers;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Controllers;

[TestFixture]
public class PaymentMethodsControllerTests
{
    private Mock<IStripePaymentMethodService> _mockService = null!;
    private Mock<ILogger<PaymentMethodsController>> _mockLogger = null!;
    private PaymentMethodsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<IStripePaymentMethodService>();
        _mockLogger = new Mock<ILogger<PaymentMethodsController>>();
        _controller = new PaymentMethodsController(_mockService.Object, _mockLogger.Object);
    }

    // --- AttachPaymentMethod ---

    [Test]
    public async Task AttachPaymentMethod_MissingCustomerId_ReturnsBadRequest()
    {
        var request = new AttachPaymentMethodRequest { CustomerId = "", Token = "tok_test" };

        var result = await _controller.AttachPaymentMethod(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AttachPaymentMethod_InvalidCustomerIdFormat_ReturnsBadRequest()
    {
        var request = new AttachPaymentMethodRequest { CustomerId = "wrong_id", Token = "tok_test" };

        var result = await _controller.AttachPaymentMethod(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AttachPaymentMethod_MissingToken_ReturnsBadRequest()
    {
        var request = new AttachPaymentMethodRequest { CustomerId = "cus_valid", Token = "" };

        var result = await _controller.AttachPaymentMethod(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AttachPaymentMethod_InvalidTokenFormat_ReturnsBadRequest()
    {
        var request = new AttachPaymentMethodRequest { CustomerId = "cus_valid", Token = "invalid_token" };

        var result = await _controller.AttachPaymentMethod(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task AttachPaymentMethod_ValidTokToken_CallsService()
    {
        var request = new AttachPaymentMethodRequest { CustomerId = "cus_valid", Token = "tok_visa" };
        var successResponse = new PaymentMethodResponse { Success = true, PaymentMethodId = "pm_test" };
        _mockService.Setup(s => s.AttachPaymentMethodAsync(It.IsAny<AttachPaymentMethodRequest>()))
                    .ReturnsAsync(successResponse);

        await _controller.AttachPaymentMethod(request);

        _mockService.Verify(s => s.AttachPaymentMethodAsync(It.IsAny<AttachPaymentMethodRequest>()), Times.Once);
    }

    [Test]
    public async Task AttachPaymentMethod_ValidPmToken_ReturnsOk()
    {
        var request = new AttachPaymentMethodRequest { CustomerId = "cus_valid", Token = "pm_card_test" };
        var successResponse = new PaymentMethodResponse { Success = true, PaymentMethodId = "pm_test" };
        _mockService.Setup(s => s.AttachPaymentMethodAsync(It.IsAny<AttachPaymentMethodRequest>()))
                    .ReturnsAsync(successResponse);

        var result = await _controller.AttachPaymentMethod(request);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task AttachPaymentMethod_ServiceReturnsFailure_Returns500()
    {
        var request = new AttachPaymentMethodRequest { CustomerId = "cus_valid", Token = "tok_visa" };
        var failureResponse = new PaymentMethodResponse { Success = false, ErrorMessage = "Stripe error" };
        _mockService.Setup(s => s.AttachPaymentMethodAsync(It.IsAny<AttachPaymentMethodRequest>()))
                    .ReturnsAsync(failureResponse);

        var result = await _controller.AttachPaymentMethod(request);
        var statusResult = result.Result as ObjectResult;

        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // --- DetachPaymentMethod ---

    [Test]
    public async Task DetachPaymentMethod_MissingPaymentMethodId_ReturnsBadRequest()
    {
        var request = new DetachPaymentMethodRequest { PaymentMethodId = "" };

        var result = await _controller.DetachPaymentMethod(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DetachPaymentMethod_InvalidFormat_ReturnsBadRequest()
    {
        var request = new DetachPaymentMethodRequest { PaymentMethodId = "card_invalid" };

        var result = await _controller.DetachPaymentMethod(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DetachPaymentMethod_ValidRequest_ReturnsOk()
    {
        var request = new DetachPaymentMethodRequest { PaymentMethodId = "pm_valid_123" };
        var successResponse = new DetachPaymentMethodResponse { Success = true, PaymentMethodId = "pm_valid_123" };
        _mockService.Setup(s => s.DetachPaymentMethodAsync(It.IsAny<DetachPaymentMethodRequest>()))
                    .ReturnsAsync(successResponse);

        var result = await _controller.DetachPaymentMethod(request);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    // --- GetPaymentMethod ---

    [Test]
    public async Task GetPaymentMethod_InvalidFormat_ReturnsBadRequest()
    {
        var result = await _controller.GetPaymentMethod("card_invalid");

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetPaymentMethod_ValidId_CallsService()
    {
        var successResponse = new PaymentMethodResponse { Success = true, PaymentMethodId = "pm_valid" };
        _mockService.Setup(s => s.GetPaymentMethodAsync("pm_valid")).ReturnsAsync(successResponse);

        await _controller.GetPaymentMethod("pm_valid");

        _mockService.Verify(s => s.GetPaymentMethodAsync("pm_valid"), Times.Once);
    }
}
