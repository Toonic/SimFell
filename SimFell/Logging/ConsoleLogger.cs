using Microsoft.Extensions.Configuration;

namespace SimFell.Logging;

public static class ConsoleLogger
{
    private static SimulationLogLevel _enabledLevels = SimulationLogLevel.All;

    public static void Configure(IConfiguration config)
    {
        var levelsSection = config.GetSection("SimulationLogging").GetSection("Levels");
        var levelNames = levelsSection.Exists()
            ? levelsSection.GetChildren().Select(c => c.Value).ToArray()
            : Array.Empty<string>();

        if (levelNames.Length > 0)
        {
            SimulationLogLevel flags = 0;
            foreach (var name in levelNames)
            {
                if (Enum.TryParse<SimulationLogLevel>(name, true, out var lvl))
                {
                    flags |= lvl;
                }
            }
            _enabledLevels = flags;
        }
        else _enabledLevels = SimulationLogLevel.All;
    }

    public static void Log(SimulationLogLevel level, string message, string? emoji = null)
    {
        if (!_enabledLevels.HasFlag(level))
        {
            return;
        }

        var formatted = emoji is null ? message : $"{emoji} {message}";
        Console.WriteLine($"Time {SimLoop.Instance.GetElapsed():F2}: {formatted}");
        FileLogger.SimulationEvent(level, $"{SimLoop.Instance.GetElapsed():F2}s -> {formatted}");
    }
}

