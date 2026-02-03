using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Simulador de pasarela MercadoPago
/// </summary>
public class MercadoPagoSimulatorService : BasePaymentSimulator
{
    public override PaymentGateway GatewayType => PaymentGateway.MercadoPago;
    
    protected override string GatewayName => "MercadoPago";
    
    protected override string GatewayDescription => 
        "MercadoPago es la plataforma de pagos líder en América Latina, " +
        "ofreciendo soluciones de pago para comercio electrónico. " +
        "Este es un simulador para propósitos de demostración.";
    
    protected override List<string> SupportedCurrencies => 
        ["MXN", "ARS", "BRL", "CLP", "COP", "PEN", "UYU", "USD"];
}
