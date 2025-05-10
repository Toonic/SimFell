using System.ComponentModel;

namespace SimFell.SimFileParser.Enums;

/// <summary>
/// Extension methods for enums.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the description of the enum value.
    /// </summary>
    /// <param name="value">The Enum value.</param>
    /// <returns>The description of the enum value.</returns>
    public static string Name(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return string.Empty;

        DescriptionAttribute[] attributes = (DescriptionAttribute[])field
            .GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : string.Empty;
    }

    public static string Identifier(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return string.Empty;

        var attributes = (IdentifierAttribute[])field
            .GetCustomAttributes(typeof(IdentifierAttribute), false);
        return attributes.Length > 0 ? attributes[0].Identifier : string.Empty;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class IdentifierAttribute : Attribute
{
    public string Identifier { get; }
    public IdentifierAttribute(string identifier) => Identifier = identifier;
}

