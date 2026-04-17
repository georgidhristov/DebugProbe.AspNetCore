using DebugProbe.AspNetCore.Middleware;
using DebugProbe.AspNetCore.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DebugProbe.AspNetCore.Extensions;
public static class DebugProbeExtensions
{
    public static IApplicationBuilder UseDebugProbe(this IApplicationBuilder app)
    {
        return app.UseMiddleware<DebugProbeMiddleware>();
    }

    public static IServiceCollection AddDebugProbe(this IServiceCollection services)
    {
        services.AddSingleton<RequestStore>();
        return services;
    }
}