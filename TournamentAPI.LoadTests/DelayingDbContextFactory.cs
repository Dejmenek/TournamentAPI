using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TournamentAPI.LoadTests;

internal sealed class DelayingDbContextFactory<TContext>(IDbContextFactory<TContext> inner, TimeSpan delay)
    : IDbContextFactory<TContext> where TContext : DbContext
{
    public TContext CreateDbContext()
    {
        Thread.Sleep(delay);
        return inner.CreateDbContext();
    }

    public async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(delay, cancellationToken);
        return await inner.CreateDbContextAsync(cancellationToken);
    }
}

internal static class DelayingDbContextFactoryExtensions
{
    private const string InnerFactoryKey = "delaying-db-context-factory-inner";

    public static IServiceCollection DelayDbContextCreation<TContext>(
        this IServiceCollection services,
        TimeSpan delay) where TContext : DbContext
    {
        var original = services.Last(d => d.ServiceType == typeof(IDbContextFactory<TContext>));
        services.Remove(original);

        services.Add(new ServiceDescriptor(
            typeof(IDbContextFactory<TContext>),
            InnerFactoryKey,
            (serviceProvider, _) => CreateOriginal(serviceProvider, original),
            original.Lifetime));

        services.Add(new ServiceDescriptor(
            typeof(IDbContextFactory<TContext>),
            serviceProvider => new DelayingDbContextFactory<TContext>(
                serviceProvider.GetRequiredKeyedService<IDbContextFactory<TContext>>(InnerFactoryKey),
                delay),
            original.Lifetime));

        return services;
    }

    private static object CreateOriginal(IServiceProvider serviceProvider, ServiceDescriptor original)
    {
        if (original.ImplementationFactory is not null)
        {
            return original.ImplementationFactory(serviceProvider);
        }

        return ActivatorUtilities.CreateInstance(serviceProvider, original.ImplementationType!);
    }
}
