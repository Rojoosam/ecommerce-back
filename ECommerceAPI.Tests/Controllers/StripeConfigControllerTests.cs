using ECommerceAPI.Configuration;
using ECommerceAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Controllers;

[TestFixture]
public class StripeConfigControllerTests
{
    private StripeConfigController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        var settings = Options.Create(new StripeSettings
        {
            PublishableKey = "pk_test_123456",
            SecretKey = "sk_test_secret",
            WebhookSecret = "whsec_test"
        });
        _controller = new StripeConfigController(settings);
    }

    [Test]
    public void GetPublishableKey_ReturnsOkResult()
    {
        var result = _controller.GetPublishableKey();

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public void GetPublishableKey_ReturnsCorrectKey()
    {
        var result = _controller.GetPublishableKey();
        var ok = result.Result as OkObjectResult;
        var response = ok!.Value as PublishableKeyResponse;

        Assert.That(response!.PublishableKey, Is.EqualTo("pk_test_123456"));
    }

    [Test]
    public void GetPublishableKey_EmptyKey_ReturnsEmptyString()
    {
        var settings = Options.Create(new StripeSettings { PublishableKey = string.Empty });
        var controller = new StripeConfigController(settings);

        var result = controller.GetPublishableKey();
        var ok = result.Result as OkObjectResult;
        var response = ok!.Value as PublishableKeyResponse;

        Assert.That(response!.PublishableKey, Is.EqualTo(string.Empty));
    }
}
