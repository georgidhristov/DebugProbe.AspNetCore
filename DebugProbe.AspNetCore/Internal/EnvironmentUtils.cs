namespace DebugProbe.AspNetCore.Internal;

internal static class EnvironmentUtils
{
    public static string TryGetEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "";
    }
}
