namespace Desuwa;

internal static class AppLogger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "startup.log");

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:O}] {message}\n");
        }
        catch
        {
            // Ignore logging failures in restricted or read-only environments
        }
    }
}
