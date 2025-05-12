using Newtonsoft.Json;
using System.Reflection;
using System.Globalization;
using SimFell.Logging;
using System.IO.Pipelines;
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
                var spellId = parts[1].Replace("-", "_");
                var prop = parts[2].ToLowerInvariant();
                var spell = caster.SpellBook.FirstOrDefault(s => s.ID == spellId);
                if (spell == null)
                    return false;
                switch (prop)
                {
                    case "cooldown":
                        var now = SimLoop.Instance.GetElapsed();
                        leftValue = spell.OffCooldown - now;
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
                if (parts.Length != 2)
                    return false;
                var charProp = parts[1].Replace("_", "");
                var charType = caster.GetType();
                var propertyInfo = charType.GetProperty(charProp, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null)
                {
                    var val = propertyInfo.GetValue(caster) ?? throw new Exception($"Property {charProp} not found on {caster.Name}");
                    if (!TryConvertToDouble(val, out leftValue))
                        return false;
                }
                else
                {
                    var fieldInfo = charType.GetField(charProp, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        var val = fieldInfo.GetValue(caster) ?? throw new Exception($"Field {charProp} not found on {caster.Name}");
                        if (!TryConvertToDouble(val, out leftValue))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                break;
            case "buff":
                if (parts.Length != 3)
                    return false;
                var buffId = parts[1];
                var buffProp = parts[2].ToLowerInvariant();
                var auraBuff = caster.Buffs.FirstOrDefault(a => a.ID == buffId);
                if (auraBuff == null)
                    return false;
                switch (buffProp)
                {
                    case "duration":
                        leftValue = auraBuff.Duration;
                        break;
                    default:
                        return false;
                }
                break;
            case "debuff":
                if (parts.Length != 3)
                    return false;
                var debuffId = parts[1];
                var debuffProp = parts[2].ToLowerInvariant();
                var auraDebuff = caster.Debuffs.FirstOrDefault(a => a.ID == debuffId);
                if (auraDebuff == null)
                    return false;
                switch (debuffProp)
                {
                    case "duration":
                        leftValue = auraDebuff.Duration;
                        break;
                    default:
                        return false;
                }
                break;
            default:
                var exists = caster.Rotation.Any(s => s.ID == Left) || caster.SpellBook.Any(s => s.ID == Left);
                leftValue = exists ? 1 : 0;
                break;
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

        return finalResult;
    }

    private bool TryConvertToDouble(object val, out double result)
    {
        if (val is double d) { result = d; return true; }
        if (val is float f) { result = f; return true; }
        if (val is int i) { result = i; return true; }
        if (val is long l) { result = l; return true; }
        if (val is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d2)) { result = d2; return true; }
        result = 0; return false;
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
}
