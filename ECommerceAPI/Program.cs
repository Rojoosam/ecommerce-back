using ECommerceAPI.Configuration;
using ECommerceAPI.Models;
using ECommerceAPI.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configurar Stripe
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

// Inicializar la API key de Stripe UNA SOLA VEZ globalmente
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Configurar notificaciones a Laravel
builder.Services.Configure<LaravelNotificationSettings>(
    builder.Configuration.GetSection("LaravelNotification"));

// Registrar HttpClientFactory para llamadas HTTP
builder.Services.AddHttpClient();

// Registrar servicios de pago
// IMPORTANTE: Descomentar solo UNO de estos bloques según tu necesidad

// OPCIÓN 1: Usar el servicio REAL de Stripe
builder.Services.AddSingleton<IPaymentGateway, StripePaymentService>();
builder.Services.AddSingleton<IPaymentGateway, PayPalSimulatorService>();
builder.Services.AddSingleton<IPaymentGateway, MercadoPagoSimulatorService>();

// OPCIÓN 2: Usar solo simuladores (comentar el bloque anterior y descomentar este)
// builder.Services.AddSingleton<IPaymentGateway, StripeSimulatorService>();
// builder.Services.AddSingleton<IPaymentGateway, PayPalSimulatorService>();
// builder.Services.AddSingleton<IPaymentGateway, MercadoPagoSimulatorService>();

builder.Services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();

// Registrar servicio de gestión de Customers de Stripe
builder.Services.AddSingleton<IStripeCustomerService, StripeCustomerService>();

// Registrar servicio de gestión de Payment Methods de Stripe
builder.Services.AddSingleton<IStripePaymentMethodService, StripePaymentMethodService>();

// Registrar servicio de gestión de Payment Intents de Stripe
builder.Services.AddSingleton<IStripePaymentIntentService, StripePaymentIntentService>();

// Registrar servicio de gestión de Refunds de Stripe
builder.Services.AddSingleton<IStripeRefundService, StripeRefundService>();

// Registrar servicio de gestión de Webhooks de Stripe
builder.Services.AddSingleton<IStripeWebhookService, StripeWebhookService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "E-Commerce Payment & Customer API",
        Version = "v1",
        Description = "Microservicio de pasarela de pagos con integración real a Stripe. " +
                      "Soporta Stripe (real), PayPal (simulado) y MercadoPago (simulado). " +
                      "Incluye gestión completa de Customers, Payment Methods, Payment Intents, Refunds y Webhooks de Stripe para Laravel."
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
