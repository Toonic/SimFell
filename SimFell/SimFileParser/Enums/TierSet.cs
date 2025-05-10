using System.ComponentModel;

namespace SimFell.SimFileParser.Enums;

/// <summary>
/// Represents a tier set.
/// Each value has defined Description attribute
/// which can be accessed using <see cref="DescriptionAttribute"/>.
/// </summary>
/// <remarks>
/// The extension is defined in <see cref="EnumExtensions.Name"/>.
/// <example>
/// You can access the Description attribute like:
/// <code>
/// var tierSet = TierSet.WYRMLING_VIGOR;
/// var description = tierSet.Name();
/// </code>
/// </example>
/// </remarks>
public enum TierSet
{
    [Description("Wyrmling Vigor"), Identifier("wyrmling_vigor")]
    WYRMLING_VIGOR
}
