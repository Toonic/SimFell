using Microsoft.Extensions.Configuration;
using Spectre.Console;

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
        // AnsiConsole.Console.WriteLine($"\u001b[0;30mTime \u001b[1;36m{SimLoop.Instance.GetElapsed():F2}\u001b[0;30m: {formatted}");
        var safeFormatted = Markup.Escape(formatted);
        var time = SimLoop.Instance.GetElapsed();

        AnsiConsole.MarkupLine($"Time [aqua]{time:F2}[/]: {safeFormatted}");
        // Console.WriteLine($"Time [aqua]{time:F4}[/]: {safeFormatted}");
        FileLogger.SimulationEvent(level, $"{time:F2}s -> {formatted}");
    }
}

