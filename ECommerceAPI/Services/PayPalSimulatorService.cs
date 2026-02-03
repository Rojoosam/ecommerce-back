using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Simulador de pasarela PayPal
/// </summary>
public class PayPalSimulatorService : BasePaymentSimulator
{
    public override PaymentGateway GatewayType => PaymentGateway.PayPal;
    
    protected override string GatewayName => "PayPal";
    
    protected override string GatewayDescription => 
        "PayPal es un sistema de pagos en línea que permite transferencias de dinero entre usuarios. " +
        "Este es un simulador para propósitos de demostración.";
    
    protected override List<string> SupportedCurrencies => 
        ["USD", "EUR", "GBP", "MXN", "CAD", "AUD", "JPY", "BRL", "CHF", "CZK", "DKK", "HKD"];
}
