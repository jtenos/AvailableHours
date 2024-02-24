using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AvailableHoursGenerator.Config;

internal static class ConfigExtensions
{
    public static IServiceCollection AddAppSettings(this IServiceCollection services, IConfiguration config)
        => services.Configure<AppSettings>(config.GetSection(nameof(AppSettings)));
}
