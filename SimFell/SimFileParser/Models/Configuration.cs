using Newtonsoft.Json;
using System.Reflection;
using System.Globalization;
using SimFell.Logging;
using Spectre.Console;

namespace SimFell.SimFileParser.Models;

/// <summary>
/// A condition for an action. Each condition is a left-hand side, operator, and right-hand side.
/// </summary>
public class Condition
{
    public string Left { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public object Right { get; set; } = new();

    public override string ToString()
    {
        return $"{Left} {Operator} {Right}";
    }

    /// <summary>
    /// Check if the condition is met.
    /// </summary>
    /// <param name="caster">The unit to check the condition on.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    public bool Check(Unit caster)
    {
        if (string.IsNullOrEmpty(Left) || string.IsNullOrEmpty(Operator) || Right == null)
            return false;

        if (!double.TryParse(Right.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rightValue))
            return false;

        var parts = Left.Split('.');
        if (parts.Length == 0)
            return false;

        double leftValue;
        switch (parts[0].ToLowerInvariant())
        {
            case "spell":
                if (parts.Length != 3)
                    return false;
                var spellId = parts[1].Replace("_", "-");
                var prop = parts[2].ToLowerInvariant();
                var spell = caster.Rotation.FirstOrDefault(s => s.ID == spellId);
                if (spell == null)
                    return false;
                switch (prop)
                {
                    case "cooldown":
                        var now = SimLoop.Instance.GetElapsed();
                        leftValue = spell.OffCooldown - now;
                        // ConsoleLogger.Log(SimulationLogLevel.Debug, $"-> [{spell.Name}] Cooldown: {leftValue}");
                        break;
                    case "cast_time":
                        leftValue = spell.GetCastTime(caster);
                        break;
                    case "channel_time":
                        leftValue = spell.GetChannelTime(caster);
                        break;
                    case "tick_rate":
                        leftValue = spell.GetTickRate(caster);
                        break;
                    case "gcd":
                        leftValue = spell.GetGCD(caster);
                        break;
                    default:
                        return false;
                }
                break;
            case "character":
                if (parts.Length < 2 || !TryGetNestedMemberDouble(caster, parts, out leftValue))
                    return false;
                break;
            case "buff":
                if (parts.Length != 3)
                    return false;
                var buffId = parts[1].Replace("_", "-");
                var buffProp = parts[2].ToLowerInvariant();
                var auraBuff = caster.Buffs.FirstOrDefault(a => a.ID == buffId);
                switch (buffProp)
                {
                    case "exists":
                        leftValue = auraBuff != null && !auraBuff.IsExpired ? 1 : 0;
                        break;
                    default:
                        return false;
                }
                break;
            case "debuff":
                if (parts.Length != 3)
                    return false;
                var debuffId = parts[1].Replace("_", "-");
                var debuffProp = parts[2].ToLowerInvariant();
                var auraDebuff = caster.Debuffs.FirstOrDefault(a => a.ID == debuffId);
                switch (debuffProp)
                {
                    case "exists":
                        leftValue = auraDebuff != null && !auraDebuff.IsExpired ? 1 : 0;
                        break;
                    default:
                        return false;
                }
                break;
            default:
                ConsoleLogger.Log(SimulationLogLevel.Error, $"Unknown condition: {Left} {Operator} {Right}");
                return false;
        }

        var finalResult = Operator switch
        {
            "==" => leftValue == rightValue,
            "!=" => leftValue != rightValue,
            ">" => leftValue > rightValue,
            ">=" => leftValue >= rightValue,
            "<" => leftValue < rightValue,
            "<=" => leftValue <= rightValue,
            _ => false,
        };

        // ConsoleLogger.Log(SimulationLogLevel.Debug, $"Condition: {Left} {Operator} {Right} => {finalResult}");

        return finalResult;
    }

    /// <summary>
    /// Try to convert a value to a double.
    /// </summary>
    /// <param name="val">The value to convert.</param>
    /// <param name="result">The converted value.</param>
    /// <returns>True if the value was converted, false otherwise.</returns>
    private bool TryConvertToDouble(object val, out double result)
    {
        if (val is double d) { result = d; return true; }
        if (val is float f) { result = f; return true; }
        if (val is int i) { result = i; return true; }
        if (val is long l) { result = l; return true; }
        if (val is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d2)) { result = d2; return true; }
        result = 0; return false;
    }

    /// <summary>
    /// Try to get a nested member of an object as a double.
    /// </summary>
    /// <param name="target">The object to get the nested member from.</param>
    /// <param name="parts">The parts of the path to the nested member.</param>
    /// <param name="result">The converted value.</param>
    /// <returns>True if the value was converted, false otherwise.</returns>
    private bool TryGetNestedMemberDouble(object target, string[] parts, out double result)
    {
        object current = target;
        for (int i = 1; i < parts.Length; i++)
        {
            var name = parts[i].Replace("_", "");
            // ConsoleLogger.Log(SimulationLogLevel.Debug, Markup.Escape($"Name: {name}"));
            var type = current.GetType();
            // ConsoleLogger.Log(SimulationLogLevel.Debug, Markup.Escape($"Current type: {type}"));
            var propInfo = type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propInfo != null)
            {
                current = propInfo.GetValue(current) ?? throw new Exception($"Property {name} not found on {type.Name}");
            }
            else
            {
                var fieldInfo = type.GetField(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    current = fieldInfo.GetValue(current) ?? throw new Exception($"Field {name} not found on {type.Name}");
                }
                else
                {
                    result = 0;
                    return false;
                }
            }
            // ConsoleLogger.Log(SimulationLogLevel.Debug, Markup.Escape($"Current: {current}"));
        }
        return TryConvertToDouble(current, out result);
    }
}

/// <summary>
/// An action is a list of conditions.
/// </summary>
public class ConfigAction
{
    public string Name { get; set; } = string.Empty;
    public List<Condition> Conditions { get; set; } = [];

    public override string ToString()
    {
        return $"{Name} ({string.Join(", ", Conditions)})";
    }
}

/// <summary>
/// A configuration for a SimFell simulation run.
/// </summary>
/// <remarks>
/// This class also provides a <see cref="ParsedJson"/> property
/// which serializes the object to a JSON string.
/// </remarks>
public class SimFellConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Hero { get; set; } = string.Empty;
    public int Intellect { get; set; }
    public double Crit { get; set; }
    public double Expertise { get; set; }
    public double Haste { get; set; }
    public double Spirit { get; set; }
    public string? Talents { get; set; }
    public string? Trinket1 { get; set; }
    public string? Trinket2 { get; set; }
    public int Duration { get; set; }
    public int Enemies { get; set; }
    public int RunCount { get; set; }
    public List<ConfigAction> ConfigActions { get; set; } = [];
    public Gear Gear { get; set; } = new();

    public Unit Player { get; set; }

    // Doesnt work :(
    public string ParsedJson => JsonConvert.SerializeObject(this, Formatting.Indented);

    /// <summary>
    /// A formatted string representation of the SimFellConfiguration object.
    /// </summary>
    public string ToStringFormatted => $@"
        Name: {Name}
        Hero: {Hero}
        ------------
        Intellect: {Intellect}
        Crit: {Crit}
        Expertise: {Expertise}
        Haste: {Haste}
        Spirit: {Spirit}
        ------------
        Talents: {Talents}
        ------------
        Trinket1: {Trinket1}
        Trinket2: {Trinket2}
        ------------
        Duration: {Duration}
        Enemies: {Enemies}
        RunCount: {RunCount}
        ------------
        ConfigActions:
        {string.Join("\n\t-> ", ConfigActions.Select(action => action.ToString()))}
        ------------
        Gear: {Gear}
    ";

    /// <summary>
    /// Parse a SimFell configuration from a file.
    /// </summary>
    /// <param name="path">The path to the file to parse.</param>
    /// <returns>A new SimFellConfiguration object.</returns>
    public static SimFellConfiguration FromFile(string path)
    {
        var config = SimfellParser.ParseFile(path);
        AssignPlayer(config);
        ApplyTalents(config);
        ApplyGems(config);
        WireActions(config);
        return config;
    }

    /// <summary>
    /// Assign a player to the configuration.
    /// </summary>
    /// <param name="config">The configuration to assign the player to.</param>
    private static void AssignPlayer(SimFellConfiguration config)
    {
        config.Player = config.Hero switch
        {
            "Rime" => new Rime(100),
            "Tariq" => new Tariq(100),
            _ => throw new Exception($"Hero {config.Hero} not found")
        };
        config.Player.SetPrimaryStats(
            config.Intellect,
            (int)config.Crit,
            (int)config.Expertise,
            (int)config.Haste,
            (int)config.Spirit
        );
    }

    /// <summary>
    /// Apply talents to the player.
    /// </summary>
    /// <param name="config">The configuration to apply the talents to.</param>
    private static void ApplyTalents(SimFellConfiguration config)
    {
        if (config.Talents == null) return;
        var talentGroups = config.Talents.Split('-');
        for (int i = 0; i < talentGroups.Length; i++)
            for (int j = 0; j < talentGroups[i].Length; j++)
                if (talentGroups[i] != "0")
                    config.Player.ActivateTalent(
                        i + 1, int.Parse(talentGroups[i][j].ToString())
                    );
    }

    /// <summary>
    /// Apply gems to the player.
    /// </summary>
    /// <param name="config">The configuration to apply the gems to.</param>
    private static void ApplyGems(SimFellConfiguration config)
    {
        foreach (var gear in config.Gear.ToList())
        {
            if (gear.Gem != null)
            {
                ConsoleLogger.Log(SimulationLogLevel.Setup, $"Adding '{gear.Gem}' from '{gear.Name}'");
                config.Player.GemDictionary.GemList.First(g => g.Type == gear.Gem.Gem)
                    .AddPower(gear.Gem.Power);
            }
        }
        foreach (var gem in config.Player.GemDictionary.GemList)
        {
            ConsoleLogger.Log(SimulationLogLevel.Setup, $"{gem.Type} Power: {gem.Power}");
            gem.Apply(config.Player);
        }
    }

    /// <summary>
    /// Wire actions to the player.
    /// </summary>
    /// <param name="config">The configuration to wire the actions to.</param>
    private static void WireActions(SimFellConfiguration config)
    {
        foreach (var action in config.ConfigActions)
        {
            var spell = config.Player.SpellBook
                .FirstOrDefault(s => s.ID.Replace("-", "_") == action.Name);
            if (spell == null)
            {
                ConsoleLogger.Log(SimulationLogLevel.Error,
                    $"[bold red]Spell {action.Name} not found in spellbook[/]");
                continue;
            }
            if (action.Conditions.Count > 0)
            {
                var original = spell.CanCast;
                spell.CanCast = caster => (original?.Invoke(caster) ?? true)
                    && action.Conditions.All(c => c.Check(caster));
            }
            config.Player.Rotation.Add(spell);
        }
    }
}
