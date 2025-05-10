using Newtonsoft.Json;
using System.Collections.Generic;

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
    public float Crit { get; set; }
    public float Expertise { get; set; }
    public float Haste { get; set; }
    public float Spirit { get; set; }
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
