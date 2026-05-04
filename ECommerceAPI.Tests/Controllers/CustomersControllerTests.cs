using ECommerceAPI.Controllers;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Controllers;

[TestFixture]
public class CustomersControllerTests
{
    private Mock<IStripeCustomerService> _mockService = null!;
    private Mock<ILogger<CustomersController>> _mockLogger = null!;
    private CustomersController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mockService = new Mock<IStripeCustomerService>();
        _mockLogger = new Mock<ILogger<CustomersController>>();
        _controller = new CustomersController(_mockService.Object, _mockLogger.Object);
    }

    private static CreateCustomerRequest BuildValidCreateRequest() => new()
    {
        UserId = "user_123",
        Name = "Juan Perez",
        Email = "juan@example.com"
    };

    // --- CreateCustomer validation ---

    [Test]
    public async Task CreateCustomer_MissingUserId_ReturnsBadRequest()
    {
        var request = new CreateCustomerRequest { UserId = "", Name = "Juan", Email = "juan@example.com" };

        var result = await _controller.CreateCustomer(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateCustomer_MissingName_ReturnsBadRequest()
    {
        var request = new CreateCustomerRequest { UserId = "user_1", Name = "", Email = "juan@example.com" };

        var result = await _controller.CreateCustomer(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateCustomer_MissingEmail_ReturnsBadRequest()
    {
        var request = new CreateCustomerRequest { UserId = "user_1", Name = "Juan", Email = "" };

        var result = await _controller.CreateCustomer(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateCustomer_ValidRequest_CallsServiceOnce()
    {
        var successResponse = new CustomerResponse { Success = true, CustomerId = "cus_test", UserId = "user_123" };
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerRequest>()))
                    .ReturnsAsync(successResponse);

        await _controller.CreateCustomer(BuildValidCreateRequest());

        _mockService.Verify(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerRequest>()), Times.Once);
    }

    [Test]
    public async Task CreateCustomer_ValidRequest_ReturnsOk()
    {
        var successResponse = new CustomerResponse { Success = true, CustomerId = "cus_test", UserId = "user_123" };
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerRequest>()))
                    .ReturnsAsync(successResponse);

        var result = await _controller.CreateCustomer(BuildValidCreateRequest());

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task CreateCustomer_ServiceReturnsFailure_Returns500()
    {
        var failureResponse = new CustomerResponse { Success = false, ErrorMessage = "Stripe error" };
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerRequest>()))
                    .ReturnsAsync(failureResponse);

        var result = await _controller.CreateCustomer(BuildValidCreateRequest());
        var statusResult = result.Result as ObjectResult;

        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task CreateCustomer_ServiceThrowsException_Returns500()
    {
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerRequest>()))
                    .ThrowsAsync(new Exception("Unexpected error"));

        var result = await _controller.CreateCustomer(BuildValidCreateRequest());
        var statusResult = result.Result as ObjectResult;

        Assert.That(statusResult!.StatusCode, Is.EqualTo(500));
    }

    // --- UpdateCustomer validation ---

    [Test]
    public async Task UpdateCustomer_MissingCustomerId_ReturnsBadRequest()
    {
        var request = new UpdateCustomerRequest { CustomerId = "" };

        var result = await _controller.UpdateCustomer(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateCustomer_InvalidCustomerIdFormat_ReturnsBadRequest()
    {
        var request = new UpdateCustomerRequest { CustomerId = "invalid_format" };

        var result = await _controller.UpdateCustomer(request);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateCustomer_ValidRequest_ReturnsOk()
    {
        var successResponse = new CustomerResponse { Success = true, CustomerId = "cus_valid" };
        _mockService.Setup(s => s.UpdateCustomerAsync(It.IsAny<UpdateCustomerRequest>()))
                    .ReturnsAsync(successResponse);

        var result = await _controller.UpdateCustomer(new UpdateCustomerRequest { CustomerId = "cus_valid" });

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    // --- GetCustomer validation ---

    [Test]
    public async Task GetCustomer_InvalidFormat_ReturnsBadRequest()
    {
        var result = await _controller.GetCustomer("not_cus_format");

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetCustomer_ValidId_CallsService()
    {
        var successResponse = new GetCustomerResponse { Success = true, CustomerId = "cus_valid" };
        _mockService.Setup(s => s.GetCustomerAsync("cus_valid")).ReturnsAsync(successResponse);

        await _controller.GetCustomer("cus_valid");

        _mockService.Verify(s => s.GetCustomerAsync("cus_valid"), Times.Once);
    }

    // --- DeleteCustomer validation ---

    [Test]
    public async Task DeleteCustomer_InvalidFormat_ReturnsBadRequest()
    {
        var result = await _controller.DeleteCustomer("not_cus_format");

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }
}
