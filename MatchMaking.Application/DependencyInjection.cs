using MatchMaking.Application.Implementations;
using MatchMaking.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MatchMaking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMatchSearchService, MatchSearchService>();
        services.AddSingleton<IMatchQueryService, MatchQueryService>();
        services.AddSingleton<IMatchCompletionHandler, MatchCompletionHandler>();
        return services;
    }
}
