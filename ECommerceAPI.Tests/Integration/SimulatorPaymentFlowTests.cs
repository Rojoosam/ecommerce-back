using ECommerceAPI.Controllers;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using ECommerceAPI.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ECommerceAPI.Tests.Integration;

/// <summary>
/// Tests de flujo completo para la pasarela de simuladores (Stripe/PayPal/MercadoPago).
/// Usa los servicios reales en memoria — no requiere API keys.
/// Flujo: Procesar pago → Verificar estado → Reembolsar
/// </summary>
[TestFixture]
public class SimulatorPaymentFlowTests
{
    private PaymentsController _controller = null!;
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

        _controller = new PaymentsController(
            _factory,
            new Mock<ILogger<PaymentsController>>().Object);
    }

    private static PaymentRequest BuildRequest(string cardNumber, PaymentGateway gateway = PaymentGateway.Stripe,
        decimal amount = 150m, string currency = "USD") => new()
    {
        Amount = amount,
        Currency = currency,
        Gateway = gateway,
        Card = new CardInfo
        {
            Number = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = 2030,
            Cvc = "123",
            HolderName = "Test User"
        }
    };

    // ─── Flujo completo: Pago exitoso → consultar estado → reembolsar ────────

    [Test]
    public async Task StripeFlow_ProcessApprovedPayment_ThenCheckStatus_ThenRefund()
    {
        // 1. Procesar pago
        var payResult = await _controller.ProcessPayment(BuildRequest("4242424242424242"));
        var payOk = payResult.Result as OkObjectResult;
        var payment = payOk!.Value as PaymentResponse;

        Assert.That(payment!.Status, Is.EqualTo(PaymentStatus.Approved), "El pago debe ser aprobado");
        Assert.That(payment.TransactionId, Is.Not.Null.And.Not.Empty);

        // 2. Consultar estado de la transacción
        var statusResult = _controller.GetTransactionStatus(payment.TransactionId);
        var statusOk = statusResult.Result as OkObjectResult;
        var status = statusOk!.Value as TransactionStatusResponse;

        Assert.That(status!.TransactionId, Is.EqualTo(payment.TransactionId));
        Assert.That(status.Status, Is.EqualTo(PaymentStatus.Approved));
        Assert.That(status.Amount, Is.EqualTo(150m));

        // 3. Reembolsar la transacción
        var refundResult = await _controller.ProcessRefund(payment.TransactionId, new RefundRequest());
        var refundOk = refundResult.Result as OkObjectResult;
        var refund = refundOk!.Value as RefundResponse;

        Assert.That(refund!.Status, Is.EqualTo(PaymentStatus.Refunded));
        Assert.That(refund.OriginalTransactionId, Is.EqualTo(payment.TransactionId));

        // 4. Verificar que el estado final es Refunded
        var finalStatus = _controller.GetTransactionStatus(payment.TransactionId);
        var finalOk = finalStatus.Result as OkObjectResult;
        var final = finalOk!.Value as TransactionStatusResponse;

        Assert.That(final!.Status, Is.EqualTo(PaymentStatus.Refunded));
        Assert.That(final.RefundId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task PayPalFlow_ProcessApprovedPayment_ThenCheckStatus_ThenRefund()
    {
        var payResult = await _controller.ProcessPayment(BuildRequest("5555555555554444", PaymentGateway.PayPal));
        var payment = (payResult.Result as OkObjectResult)!.Value as PaymentResponse;

        Assert.That(payment!.Status, Is.EqualTo(PaymentStatus.Approved));
        Assert.That(payment.Gateway, Is.EqualTo(PaymentGateway.PayPal));

        var statusResult = _controller.GetTransactionStatus(payment.TransactionId);
        Assert.That(statusResult.Result, Is.TypeOf<OkObjectResult>());

        var refundResult = await _controller.ProcessRefund(payment.TransactionId, new RefundRequest());
        var refund = (refundResult.Result as OkObjectResult)!.Value as RefundResponse;

        Assert.That(refund!.Status, Is.EqualTo(PaymentStatus.Refunded));
    }

    [Test]
    public async Task MercadoPagoFlow_ProcessApprovedPayment_ThenCheckStatus_ThenRefund()
    {
        var payResult = await _controller.ProcessPayment(
            BuildRequest("4242424242424242", PaymentGateway.MercadoPago, 500m, "MXN"));
        var payment = (payResult.Result as OkObjectResult)!.Value as PaymentResponse;

        Assert.That(payment!.Status, Is.EqualTo(PaymentStatus.Approved));
        Assert.That(payment.Gateway, Is.EqualTo(PaymentGateway.MercadoPago));
        Assert.That(payment.Currency, Is.EqualTo("MXN"));

        var statusResult = _controller.GetTransactionStatus(payment.TransactionId);
        Assert.That(statusResult.Result, Is.TypeOf<OkObjectResult>());

        var refundResult = await _controller.ProcessRefund(payment.TransactionId, new RefundRequest());
        var refund = (refundResult.Result as OkObjectResult)!.Value as RefundResponse;

        Assert.That(refund!.Status, Is.EqualTo(PaymentStatus.Refunded));
    }

    // ─── Flujo declinado: Pago declinado → intentar reembolsar falla ─────────

    [Test]
    public async Task Flow_DeclinedPayment_CannotBeRefunded()
    {
        var payResult = await _controller.ProcessPayment(BuildRequest("4000000000000002"));
        var payment = (payResult.Result as OkObjectResult)!.Value as PaymentResponse;

        Assert.That(payment!.Status, Is.EqualTo(PaymentStatus.Declined));

        // Intentar reembolsar una transacción declinada debe fallar con 400
        var refundResult = await _controller.ProcessRefund(payment.TransactionId, new RefundRequest());

        Assert.That(refundResult.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    // ─── Flujo de reembolso parcial ───────────────────────────────────────────

    [Test]
    public async Task Flow_ApprovedPayment_PartialRefund_ReturnsCorrectAmount()
    {
        var payResult = await _controller.ProcessPayment(BuildRequest("4242424242424242", amount: 200m));
        var payment = (payResult.Result as OkObjectResult)!.Value as PaymentResponse;

        Assert.That(payment!.Status, Is.EqualTo(PaymentStatus.Approved));

        var refundResult = await _controller.ProcessRefund(
            payment.TransactionId,
            new RefundRequest { Amount = 75m, Reason = "Devolución parcial" });

        var refund = (refundResult.Result as OkObjectResult)!.Value as RefundResponse;

        Assert.That(refund!.Status, Is.EqualTo(PaymentStatus.Refunded));
        Assert.That(refund.Amount, Is.EqualTo(75m));
    }

    // ─── Transacción inexistente ──────────────────────────────────────────────

    [Test]
    public void Flow_QueryNonExistentTransaction_Returns404()
    {
        var result = _controller.GetTransactionStatus("txn_does_not_exist");

        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Flow_RefundNonExistentTransaction_Returns404()
    {
        var result = await _controller.ProcessRefund("txn_does_not_exist", new RefundRequest());

        Assert.That(result.Result, Is.TypeOf<NotFoundObjectResult>());
    }

    // ─── Múltiples pagos independientes ──────────────────────────────────────

    [Test]
    public async Task Flow_MultipleIndependentPayments_EachHasUniqueTransactionId()
    {
        var pay1 = await _controller.ProcessPayment(BuildRequest("4242424242424242"));
        var pay2 = await _controller.ProcessPayment(BuildRequest("5555555555554444", PaymentGateway.PayPal));
        var pay3 = await _controller.ProcessPayment(BuildRequest("378282246310005"));

        var txn1 = ((pay1.Result as OkObjectResult)!.Value as PaymentResponse)!.TransactionId;
        var txn2 = ((pay2.Result as OkObjectResult)!.Value as PaymentResponse)!.TransactionId;
        var txn3 = ((pay3.Result as OkObjectResult)!.Value as PaymentResponse)!.TransactionId;

        Assert.That(new[] { txn1, txn2, txn3 }, Is.Unique);
    }
}
