using ECommerceAPI.Models;

namespace ECommerceAPI.Services;

/// <summary>
/// Factory para obtener la pasarela de pago correcta
/// </summary>
public interface IPaymentGatewayFactory
{
    IPaymentGateway GetGateway(PaymentGateway gateway);
    IEnumerable<GatewayInfo> GetAllGateways();
}

/// <summary>
/// Implementación del factory de pasarelas de pago
/// </summary>
public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly Dictionary<PaymentGateway, IPaymentGateway> _gateways;

    public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways.ToDictionary(g => g.GatewayType);
    }

    public IPaymentGateway GetGateway(PaymentGateway gateway)
    {
        if (_gateways.TryGetValue(gateway, out var paymentGateway))
        {
            return paymentGateway;
        }

        throw new ArgumentException($"Pasarela de pago '{gateway}' no soportada", nameof(gateway));
    }

    public IEnumerable<GatewayInfo> GetAllGateways()
    {
        return _gateways.Values.Select(g => g.GetGatewayInfo());
    }
}
