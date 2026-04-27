
namespace DebugProbe.AspNetCore.Internal;

internal class EnvironmentUtils
{
    public static string TryGetEnvironment()
    {
         return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "";
    }
}
