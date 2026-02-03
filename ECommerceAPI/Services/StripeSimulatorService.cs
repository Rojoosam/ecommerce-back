using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Simulador de pasarela Stripe
/// </summary>
public class StripeSimulatorService : BasePaymentSimulator
{
    public override PaymentGateway GatewayType => PaymentGateway.Stripe;
    
    protected override string GatewayName => "Stripe";
    
    protected override string GatewayDescription => 
        "Stripe es una plataforma de pagos en línea que permite a empresas aceptar pagos por Internet. " +
        "Este es un simulador para propósitos de demostración.";
    
    protected override List<string> SupportedCurrencies => 
        ["USD", "EUR", "GBP", "MXN", "CAD", "AUD", "JPY", "BRL"];
}
