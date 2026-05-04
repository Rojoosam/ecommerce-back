using ECommerceAPI.Controllers;
using ECommerceAPI.Models;
using ECommerceAPI.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Integration;

/// <summary>
/// Tests de flujo completo para la integración real con Stripe.
/// Usa implementaciones Fake en memoria — no requiere API keys de Stripe.
///
/// Flujo principal:
///   1. POST /api/customers          → Crear usuario/customer
///   2. POST /api/paymentmethods/attach → Registrar tarjeta
///   3. POST /api/paymentintents     → Procesar pago
///   4. POST /api/refunds/payment-intent/{id} → Reembolsar
/// </summary>
[TestFixture]
public class StripeFullFlowTests
{
    // Controladores bajo prueba
    private CustomersController _customersController = null!;
    private PaymentMethodsController _paymentMethodsController = null!;
    private PaymentIntentsController _paymentIntentsController = null!;
    private RefundsController _refundsController = null!;

    // Fakes compartidos (misma instancia = mismo "estado" en memoria)
    private FakeStripeCustomerService _fakeCustomers = null!;
    private FakeStripePaymentMethodService _fakePaymentMethods = null!;
    private FakeStripePaymentIntentService _fakePaymentIntents = null!;
    private FakeStripeRefundService _fakeRefunds = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeCustomers = new FakeStripeCustomerService();
        _fakePaymentMethods = new FakeStripePaymentMethodService();
        _fakePaymentIntents = new FakeStripePaymentIntentService(_fakePaymentMethods);
        _fakeRefunds = new FakeStripeRefundService();

        _customersController = new CustomersController(
            _fakeCustomers,
            new Mock<ILogger<CustomersController>>().Object);

        _paymentMethodsController = new PaymentMethodsController(
            _fakePaymentMethods,
            new Mock<ILogger<PaymentMethodsController>>().Object);

        _paymentIntentsController = new PaymentIntentsController(
            _fakePaymentIntents,
            new Mock<ILogger<PaymentIntentsController>>().Object);

        _refundsController = new RefundsController(
            _fakeRefunds,
            new Mock<ILogger<RefundsController>>().Object);
    }

    // ─── Flujo 1: Crear cliente → Agregar tarjeta → Pagar → Reembolsar ───────

    [Test]
    public async Task FullFlow_CreateCustomer_AttachCard_Pay_ThenRefund()
    {
        // ── PASO 1: Crear el customer (usuario nuevo) ──────────────────────
        var createCustomerResult = await _customersController.CreateCustomer(new CreateCustomerRequest
        {
            UserId = "user_laravel_001",
            Name = "Ana García",
            Email = "ana.garcia@example.com",
            Phone = "+52 55 1234 5678"
        });

        var customerOk = createCustomerResult.Result as OkObjectResult;
        var customer = customerOk!.Value as CustomerResponse;

        Assert.That(customer!.Success, Is.True, "El customer debe crearse exitosamente");
        Assert.That(customer.CustomerId, Does.StartWith("cus_"), "El ID debe tener formato cus_xxx");
        Assert.That(customer.UserId, Is.EqualTo("user_laravel_001"));

        var customerId = customer.CustomerId!;

        // ── PASO 2: Registrar una tarjeta al customer ─────────────────────
        var attachResult = await _paymentMethodsController.AttachPaymentMethod(new AttachPaymentMethodRequest
        {
            CustomerId = customerId,
            Token = "tok_visa"
        });

        var attachOk = attachResult.Result as OkObjectResult;
        var paymentMethod = attachOk!.Value as PaymentMethodResponse;

        Assert.That(paymentMethod!.Success, Is.True, "El payment method debe registrarse exitosamente");
        Assert.That(paymentMethod.PaymentMethodId, Does.StartWith("pm_"), "El ID debe tener formato pm_xxx");
        Assert.That(paymentMethod.CustomerId, Is.EqualTo(customerId));
        Assert.That(paymentMethod.Card!.Brand, Is.EqualTo("visa"));

        var paymentMethodId = paymentMethod.PaymentMethodId!;

        // ── PASO 3: Procesar un pago con la tarjeta registrada ────────────
        var paymentResult = await _paymentIntentsController.CreatePaymentIntent(new CreatePaymentIntentRequest
        {
            CustomerId = customerId,
            PaymentMethodId = paymentMethodId,
            Amount = 299.99m,
            Currency = "usd",
            OrderId = "order_abc_001",
            Description = "Compra en ECommerce"
        });

        var paymentOk = paymentResult.Result as OkObjectResult;
        var payment = paymentOk!.Value as PaymentIntentResponse;

        Assert.That(payment!.Success, Is.True, "El pago debe procesarse exitosamente");
        Assert.That(payment.PaymentIntentId, Does.StartWith("pi_"), "El ID debe tener formato pi_xxx");
        Assert.That(payment.Status, Is.EqualTo("succeeded"));
        Assert.That(payment.AmountDecimal, Is.EqualTo(299.99m));
        Assert.That(payment.OrderId, Is.EqualTo("order_abc_001"));

        var paymentIntentId = payment.PaymentIntentId!;

        // ── PASO 4: Reembolsar el pago ────────────────────────────────────
        var refundResult = await _refundsController.CreateRefundForPaymentIntent(
            paymentIntentId,
            new RefundRequest { Reason = "El cliente canceló el pedido" });

        var refundOk = refundResult.Result as OkObjectResult;
        var refund = refundOk!.Value as RefundResponse;

        Assert.That(refund!.Status, Is.EqualTo(PaymentStatus.Refunded));
        Assert.That(refund.RefundId, Does.StartWith("re_"), "El ID de reembolso debe tener formato re_xxx");
        Assert.That(refund.OriginalTransactionId, Is.EqualTo(paymentIntentId));
    }

    // ─── Flujo 2: Múltiples pagos del mismo cliente ───────────────────────────

    [Test]
    public async Task FullFlow_SameCustomer_MultiplePayments_EachSucceeds()
    {
        // Crear customer
        var customerResult = await _customersController.CreateCustomer(new CreateCustomerRequest
        {
            UserId = "user_laravel_002",
            Name = "Carlos López",
            Email = "carlos@example.com"
        });
        var customerId = ((customerResult.Result as OkObjectResult)!.Value as CustomerResponse)!.CustomerId!;

        // Registrar tarjeta
        var attachResult = await _paymentMethodsController.AttachPaymentMethod(new AttachPaymentMethodRequest
        {
            CustomerId = customerId,
            Token = "tok_mastercard"
        });
        var paymentMethodId = ((attachResult.Result as OkObjectResult)!.Value as PaymentMethodResponse)!.PaymentMethodId!;

        // Realizar 3 pagos distintos
        var orders = new[] { ("order_001", 100m), ("order_002", 200m), ("order_003", 50m) };
        var paymentIntentIds = new List<string>();

        foreach (var (orderId, amount) in orders)
        {
            var result = await _paymentIntentsController.CreatePaymentIntent(new CreatePaymentIntentRequest
            {
                CustomerId = customerId,
                PaymentMethodId = paymentMethodId,
                Amount = amount,
                Currency = "usd",
                OrderId = orderId
            });

            var pi = ((result.Result as OkObjectResult)!.Value as PaymentIntentResponse)!;
            Assert.That(pi.Success, Is.True, $"El pago de {orderId} debe ser exitoso");
            paymentIntentIds.Add(pi.PaymentIntentId!);
        }

        // Los 3 pagos deben tener IDs únicos
        Assert.That(paymentIntentIds, Is.Unique, "Cada pago debe tener un PaymentIntentId único");
        Assert.That(paymentIntentIds.Count, Is.EqualTo(3));
    }

    // ─── Flujo 3: Verificar que un customer inactivo no puede pagar ───────────

    [Test]
    public async Task FullFlow_DeactivateCustomer_PaymentFails()
    {
        // Crear customer y registrar tarjeta
        var customerResult = await _customersController.CreateCustomer(new CreateCustomerRequest
        {
            UserId = "user_laravel_003",
            Name = "Pedro Martínez",
            Email = "pedro@example.com"
        });
        var customerId = ((customerResult.Result as OkObjectResult)!.Value as CustomerResponse)!.CustomerId!;

        var attachResult = await _paymentMethodsController.AttachPaymentMethod(new AttachPaymentMethodRequest
        {
            CustomerId = customerId,
            Token = "tok_visa"
        });
        var paymentMethodId = ((attachResult.Result as OkObjectResult)!.Value as PaymentMethodResponse)!.PaymentMethodId!;

        // Verificar que puede pagar mientras está activo
        var firstPayment = await _paymentIntentsController.CreatePaymentIntent(new CreatePaymentIntentRequest
        {
            CustomerId = customerId,
            PaymentMethodId = paymentMethodId,
            Amount = 100m,
            Currency = "usd",
            OrderId = "order_active_001"
        });
        var firstPi = ((firstPayment.Result as OkObjectResult)!.Value as PaymentIntentResponse)!;
        Assert.That(firstPi.Success, Is.True, "El primer pago debe funcionar mientras el customer está activo");

        // Desactivar el customer
        await _paymentMethodsController.UpdateCustomerStatus(new UpdateCustomerStatusRequest
        {
            CustomerId = customerId,
            Active = false,
            DetachPaymentMethods = false
        });

        // Intentar otro pago — debe fallar con 500 (customer inactivo)
        var blockedPayment = await _paymentIntentsController.CreatePaymentIntent(new CreatePaymentIntentRequest
        {
            CustomerId = customerId,
            PaymentMethodId = paymentMethodId,
            Amount = 200m,
            Currency = "usd",
            OrderId = "order_inactive_001"
        });

        var blockedResult = blockedPayment.Result as ObjectResult;
        Assert.That(blockedResult!.StatusCode, Is.EqualTo(500));

        var blockedPi = blockedResult.Value as PaymentIntentResponse;
        Assert.That(blockedPi!.Success, Is.False);
        Assert.That(blockedPi.ErrorMessage, Does.Contain("inactivo"));
    }

    // ─── Flujo 4: Actualizar datos del cliente y pagar ────────────────────────

    [Test]
    public async Task FullFlow_CreateCustomer_UpdateData_ThenPay()
    {
        // Crear customer con datos iniciales
        var createResult = await _customersController.CreateCustomer(new CreateCustomerRequest
        {
            UserId = "user_laravel_004",
            Name = "María Nombre Viejo",
            Email = "viejo@example.com"
        });
        var customerId = ((createResult.Result as OkObjectResult)!.Value as CustomerResponse)!.CustomerId!;

        // Actualizar datos del customer
        var updateResult = await _customersController.UpdateCustomer(new UpdateCustomerRequest
        {
            CustomerId = customerId,
            Name = "María Nombre Nuevo",
            Email = "nuevo@example.com"
        });

        var updatedCustomer = ((updateResult.Result as OkObjectResult)!.Value as CustomerResponse)!;
        Assert.That(updatedCustomer.Success, Is.True);
        Assert.That(updatedCustomer.Name, Is.EqualTo("María Nombre Nuevo"));
        Assert.That(updatedCustomer.Email, Is.EqualTo("nuevo@example.com"));

        // Registrar tarjeta y procesar pago normalmente
        var attachResult = await _paymentMethodsController.AttachPaymentMethod(new AttachPaymentMethodRequest
        {
            CustomerId = customerId,
            Token = "tok_visa"
        });
        var paymentMethodId = ((attachResult.Result as OkObjectResult)!.Value as PaymentMethodResponse)!.PaymentMethodId!;

        var paymentResult = await _paymentIntentsController.CreatePaymentIntent(new CreatePaymentIntentRequest
        {
            CustomerId = customerId,
            PaymentMethodId = paymentMethodId,
            Amount = 75m,
            Currency = "usd",
            OrderId = "order_updated_001"
        });

        var payment = ((paymentResult.Result as OkObjectResult)!.Value as PaymentIntentResponse)!;
        Assert.That(payment.Success, Is.True, "El pago debe procesarse con los datos actualizados");
        Assert.That(payment.Status, Is.EqualTo("succeeded"));
    }

    // ─── Flujo 5: Listar tarjetas registradas del customer ───────────────────

    [Test]
    public async Task FullFlow_CreateCustomer_AttachMultipleCards_ListAll()
    {
        var createResult = await _customersController.CreateCustomer(new CreateCustomerRequest
        {
            UserId = "user_laravel_005",
            Name = "Luis Sánchez",
            Email = "luis@example.com"
        });
        var customerId = ((createResult.Result as OkObjectResult)!.Value as CustomerResponse)!.CustomerId!;

        // Registrar 2 tarjetas
        await _paymentMethodsController.AttachPaymentMethod(new AttachPaymentMethodRequest
        {
            CustomerId = customerId,
            Token = "tok_visa"
        });
        await _paymentMethodsController.AttachPaymentMethod(new AttachPaymentMethodRequest
        {
            CustomerId = customerId,
            Token = "pm_card_mastercard"
        });

        // Listar tarjetas del customer
        var listResult = await _paymentMethodsController.ListPaymentMethods(customerId);
        var listOk = listResult.Result as OkObjectResult;
        var listResponse = listOk!.Value as ListPaymentMethodsResponse;

        Assert.That(listResponse!.Success, Is.True);
        Assert.That(listResponse.Count, Is.EqualTo(2));
        Assert.That(listResponse.PaymentMethods, Has.All.Matches<PaymentMethodResponse>(pm =>
            pm.CustomerId == customerId));
    }

    // ─── Flujo 6: Reembolso parcial ───────────────────────────────────────────

    [Test]
    public async Task FullFlow_Pay_ThenPartialRefund()
    {
        var (customerId, paymentMethodId) = await CreateActiveCustomerWithCard();

        var paymentResult = await _paymentIntentsController.CreatePaymentIntent(new CreatePaymentIntentRequest
        {
            CustomerId = customerId,
            PaymentMethodId = paymentMethodId,
            Amount = 500m,
            Currency = "usd",
            OrderId = "order_partial_001"
        });
        var paymentIntentId = ((paymentResult.Result as OkObjectResult)!.Value as PaymentIntentResponse)!.PaymentIntentId!;

        // Reembolsar solo $100 de los $500
        var refundResult = await _refundsController.CreateRefundForPaymentIntent(
            paymentIntentId,
            new RefundRequest { Amount = 100m, Reason = "Descuento retroactivo" });

        var refund = ((refundResult.Result as OkObjectResult)!.Value as RefundResponse)!;

        Assert.That(refund.Status, Is.EqualTo(PaymentStatus.Refunded));
        Assert.That(refund.Amount, Is.EqualTo(100m));
        Assert.That(_fakeRefunds.RefundCount, Is.EqualTo(1));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Helper: crea un customer con una tarjeta registrada y listo para pagar
    /// </summary>
    private async Task<(string customerId, string paymentMethodId)> CreateActiveCustomerWithCard()
    {
        var customerResult = await _customersController.CreateCustomer(new CreateCustomerRequest
        {
            UserId = $"user_helper_{Guid.NewGuid():N}",
            Name = "Helper User",
            Email = "helper@example.com"
        });
        var customerId = ((customerResult.Result as OkObjectResult)!.Value as CustomerResponse)!.CustomerId!;

        var attachResult = await _paymentMethodsController.AttachPaymentMethod(new AttachPaymentMethodRequest
        {
            CustomerId = customerId,
            Token = "tok_visa"
        });
        var paymentMethodId = ((attachResult.Result as OkObjectResult)!.Value as PaymentMethodResponse)!.PaymentMethodId!;

        return (customerId, paymentMethodId);
    }
}
