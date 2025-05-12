using System.ComponentModel;

namespace SimFell.SimFileParser.Enums;

/// <summary>
/// Represents a gem.
/// Each value has defined Description attribute
/// which can be accessed using <see cref="DescriptionAttribute"/>.
/// </summary>
/// <remarks>
/// The extension is defined in <see cref="EnumExtensions.Name"/>.
/// <example>
/// You can access the Description attribute like:
/// <code>
/// var gem = Gem.RUBY;
/// var description = gem.Name();
/// </code>
/// </example>
/// </remarks>
public enum Gem
{
    [Description("Ruby"), Identifier("ruby")]
    RUBY,
    [Description("Amethyst"), Identifier("amethyst")]
    AMETHYST,
    [Description("Topaz"), Identifier("topaz")]
    TOPAZ,
    [Description("Emerald"), Identifier("emerald")]
    EMERALD,
    [Description("Sapphire"), Identifier("sapphire")]
    SAPPHIRE,
    [Description("Diamond"), Identifier("diamond")]
    DIAMOND
}
