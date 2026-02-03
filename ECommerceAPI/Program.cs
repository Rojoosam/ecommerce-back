using ECommerceAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Registrar simuladores de pasarelas de pago
builder.Services.AddSingleton<IPaymentGateway, StripeSimulatorService>();
builder.Services.AddSingleton<IPaymentGateway, PayPalSimulatorService>();
builder.Services.AddSingleton<IPaymentGateway, MercadoPagoSimulatorService>();
builder.Services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Payment Gateway Simulator API",
        Version = "v1",
        Description = "Microservicio de pasarela de pagos simulado para E-commerce. " +
                      "Soporta Stripe, PayPal y MercadoPago en modo demo."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Gateway Simulator API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
