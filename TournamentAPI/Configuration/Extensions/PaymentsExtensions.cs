using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe;
using TournamentAPI.Payments;

namespace TournamentAPI.Configuration.Extensions;

internal static class PaymentsExtensions
{
    internal static IServiceCollection AddApplicationPayments(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var stripeOptions = sp.GetRequiredService<IOptions<StripeOptions>>().Value;
            return new StripeClient(stripeOptions.SecretKey);
        });

        services.AddSingleton<IStripeGateway, StripeGateway>();
        services.AddScoped<StripeWebhookProcessor>();
        services.AddSingleton<IStripeWebhookHandler, StripeAccountUpdatedHandler>();
        services.AddSingleton<IStripeWebhookHandler, StripeCheckoutSessionCompletedHandler>();
        services.AddSingleton<IStripeWebhookHandler, StripeCheckoutSessionExpiredHandler>();

        return services;
    }
}
