using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SimFell.Logging;

public static class ConsoleLogger
{
    private static SimulationLogLevel _enabledLevels = SimulationLogLevel.All;

    public static bool Enabled { get; set; } = true;
    public static Simulator Simulator { get; set; }

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
        else _enabledLevels = SimulationLogLevel.Default;
    }

    public static void SetLevel(SimulationLogLevel level)
    {
        _enabledLevels = level;
    }

    public static SimulationLogLevel GetLevel()
    {
        return _enabledLevels;
    }

    public static void Log(SimulationLogLevel level, string message, string? emoji = null)
    {
        if (!Enabled) return;
        if (!_enabledLevels.HasFlag(level)) return;

        var formatted = emoji is null ? message : $"{emoji} {message}";
        var time = Simulator == null ? 0 : Simulator.Now;

        var cleanMessage = Regex.Replace(Markup.Escape(formatted), @"\[.*?\]", "").Replace("]", "");
        FileLogger.SimulationEvent(level, $"{time:F2}s : {cleanMessage}");

        if (level == SimulationLogLevel.Setup)
            AnsiConsole.MarkupLine(formatted);
        else
            AnsiConsole.MarkupLine($"Time [aqua]{time:F2}[/]: {formatted}");
    }

    public static void Log(SimulationLogLevel level, IRenderable renderable)
    {
        if (!Enabled) return;
        if (!_enabledLevels.HasFlag(level)) return;

        var time = Simulator == null ? 0 : Simulator.Now;
        if (level != SimulationLogLevel.Setup)
        {
            AnsiConsole.MarkupLine($"Time [aqua]{time:F2}[/]:");
        }

        AnsiConsole.Write(renderable);

        FileLogger.SimulationEvent(level, $"{time:F2}s : {renderable} (Reference console output)");
    }
}