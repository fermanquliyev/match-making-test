using MatchMaking.Application.Implementations;
using MatchMaking.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MatchMaking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMatchSearchService, MatchSearchService>();
        services.AddScoped<IMatchQueryService, MatchQueryService>();
        services.AddScoped<IMatchCompletionHandler, MatchCompletionHandler>();
        return services;
    }
}
