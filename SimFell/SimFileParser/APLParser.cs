namespace SimFell.SimmyRewrite;

using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using System.Linq;

public static class APLParser
{
    // Cache compiled condition functions only (safe to share between units)
    private static readonly Dictionary<string, Func<Unit, bool>> _conditionCache =
        new Dictionary<string, Func<Unit, bool>>();

    private static readonly object _cacheLock = new object();

    /// <summary>
    /// Clear the condition cache (useful for testing or memory management)
    /// </summary>
    public static void ClearConditionCache()
    {
        lock (_cacheLock)
        {
            _conditionCache.Clear();
        }
    }

    /// <summary>
    /// Parse an entire APL (list of action lines) into a list of SimActions
    /// </summary>
    public static List<SimAction> ParseAPL(List<string> aplLines, Unit unit)
    {
        var simActions = new List<SimAction>();

        foreach (var line in aplLines)
        {
            var simAction = ParseLine(line, unit);
            if (simAction != null)
            {
                simActions.Add(simAction);
            }
        }

        return simActions;
    }

    /// <summary>
    /// Parse a line, referencing Unit.
    /// </summary>
    private static SimAction ParseLine(string line, Unit unit)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var trimmed = line.Trim();

        // Must start with "action+=" or "action="
        string remaining;
        if (trimmed.StartsWith("action+="))
        {
            remaining = trimmed.Substring(8); // "action+=".Length = 8
        }
        else if (trimmed.StartsWith("action="))
        {
            remaining = trimmed.Substring(7); // "action=".Length = 7
        }
        else
        {
            Console.WriteLine(
                $"ERROR: APL line not implemented - line does not start with 'action=' or 'action+=': {trimmed}");
            return null;
        }

        // Must start with "/"
        if (!remaining.StartsWith("/"))
        {
            Console.WriteLine($"ERROR: APL line not implemented - action does not start with '/': {trimmed}");
            return null;
        }

        // Remove the "/"
        remaining = remaining.Substring(1);

        string spellId;
        string conditionText = null;

        // Check if there's a condition (,if=...)
        var ifIndex = remaining.IndexOf(",if=");
        if (ifIndex >= 0)
        {
            spellId = remaining.Substring(0, ifIndex);
            conditionText = remaining.Substring(ifIndex + 4); // ",if=".Length = 4
        }
        else
        {
            spellId = remaining;
        }

        // Find the spell in the unit's spellbook (NO CACHING - unit-specific)
        var spell = unit.SpellBook.FirstOrDefault(s => s.ID == spellId);
        if (spell == null)
        {
            // Spell not found in spellbook
            Console.WriteLine($"ERROR: APL line not implemented - spell '{spellId}' not found in spellbook: {trimmed}");
            return null;
        }

        // Create new SimAction for this unit (NO CACHING - contains unit-specific spell reference)
        var simAction = new SimAction
        {
            Spell = spell,
            Raw = trimmed,
            ConditionCheck = GetCachedCondition(conditionText, trimmed) // Pass trimmed line for error reporting
        };

        return simAction;
    }

    private static Func<Unit, bool> GetCachedCondition(string conditionText, string originalLine = null)
    {
        // No condition = always true
        if (string.IsNullOrWhiteSpace(conditionText))
            return _ => true;

        // Check if we already compiled this condition
        lock (_cacheLock)
        {
            if (_conditionCache.TryGetValue(conditionText, out var cached))
                return cached;
        }

        // Compile the condition for the first time
        var compiled = CompileCondition(conditionText, originalLine);

        // Cache it
        lock (_cacheLock)
        {
            _conditionCache[conditionText] = compiled;
        }

        return compiled;
    }

    // Delegate for condition handlers
    private delegate Func<Unit, bool> ConditionHandler(string conditionText, string originalLine);

    // Registry of condition handlers
    private static readonly Dictionary<string, ConditionHandler> _conditionHandlers =
        new Dictionary<string, ConditionHandler>
        {
            { "buff", HandleBuffCondition },
            { "debuff", HandleDebuffCondition },
            { "spell", HandleSpellCondition },
            { "character", HandleCharacterCondition },
            // Add more handlers as needed: cooldown, resource, target, etc.
        };

    private static Func<Unit, bool> CompileCondition(string conditionText, string originalLine = null)
    {
        if (string.IsNullOrWhiteSpace(conditionText))
            return _ => true; // Always true if no condition

        // Handle compound conditions with "and"/"or"
        if (conditionText.Contains(" and "))
        {
            return CompileCompoundCondition(conditionText, " and ", (a, b) => a && b, originalLine);
        }

        if (conditionText.Contains(" or "))
        {
            return CompileCompoundCondition(conditionText, " or ", (a, b) => a || b, originalLine);
        }

        // Handle single condition
        return CompileSingleCondition(conditionText, originalLine);
    }

    private static Func<Unit, bool> CompileCompoundCondition(string conditionText, string operatorText,
        Func<bool, bool, bool> combineFunc, string originalLine)
    {
        var parts = conditionText.Split(new[] { operatorText }, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            Console.WriteLine(
                $"ERROR: APL condition not implemented - invalid compound condition format '{conditionText}'" +
                (originalLine != null ? $" in line: {originalLine}" : ""));
            return _ => false;
        }

        var leftCondition = CompileSingleCondition(parts[0].Trim(), originalLine);
        var rightCondition = CompileSingleCondition(parts[1].Trim(), originalLine);

        return unit => combineFunc(leftCondition(unit), rightCondition(unit));
    }

    private static Func<Unit, bool> CompileSingleCondition(string conditionText, string originalLine = null)
    {
        // Check if it's a simple numeric condition without dots (like "targets>3")
        if (!conditionText.Contains('.'))
        {
            return HandleSimpleCondition(conditionText, originalLine);
        }

        // Find the condition type (first part before the dot)
        var dotIndex = conditionText.IndexOf('.');
        if (dotIndex <= 0)
        {
            Console.WriteLine($"ERROR: APL condition not implemented - invalid condition format '{conditionText}'" +
                              (originalLine != null ? $" in line: {originalLine}" : ""));
            return _ => false; // Invalid format
        }

        var conditionType = conditionText.Substring(0, dotIndex);

        // Look up the appropriate handler
        if (_conditionHandlers.TryGetValue(conditionType, out var handler))
        {
            return handler(conditionText, originalLine);
        }

        // Default: condition type not recognized
        Console.WriteLine(
            $"ERROR: APL condition not implemented - unknown condition type '{conditionType}' in '{conditionText}'" +
            (originalLine != null ? $" in line: {originalLine}" : ""));
        return _ => false;
    }

    private static Func<Unit, bool> HandleBuffCondition(string conditionText, string originalLine = null)
    {
        var parts = conditionText.Split('.');
        if (parts.Length < 3)
        {
            Console.WriteLine($"ERROR: APL buff condition not implemented - invalid format '{conditionText}'" +
                              (originalLine != null ? $" in line: {originalLine}" : ""));
            return _ => false;
        }

        var buffId = parts[1];
        var propertyPart = parts[2];

        // Special case: "exists" doesn't need the buff instance
        if (propertyPart == "exists")
        {
            return unit => unit.Buffs.Any(aura => aura.ID == buffId);
        }

        // For all other properties, we need to handle the buff instance
        return unit =>
        {
            var buff = unit.Buffs.FirstOrDefault(aura => aura.ID == buffId);

            // Handle different property types
            return propertyPart switch
            {
                var prop when prop.StartsWith("duration") =>
                    CompareValues(buff?.Duration ?? 0, propertyPart),
                // Add more properties as needed
                _ => LogUnknownProperty(propertyPart, conditionText, originalLine)
            };
        };
    }

    private static Func<Unit, bool> HandleDebuffCondition(string conditionText, string originalLine = null)
    {
        var parts = conditionText.Split('.');
        if (parts.Length < 3)
        {
            Console.WriteLine($"ERROR: APL debuff condition not implemented - invalid format '{conditionText}'" +
                              (originalLine != null ? $" in line: {originalLine}" : ""));
            return _ => false;
        }

        var debuffId = parts[1];
        var propertyPart = parts[2];

        // Special case: "exists" doesn't need the debuff instance
        if (propertyPart == "exists")
        {
            return unit => unit.Debuffs.Any(aura => aura.ID == debuffId);
        }

        // For all other properties, we need to handle the debuff instance
        return unit =>
        {
            var debuff = unit.Debuffs.FirstOrDefault(aura => aura.ID == debuffId);

            // Handle different property types
            return propertyPart switch
            {
                var prop when prop.StartsWith("duration") =>
                    CompareValues(debuff?.Duration ?? 0, propertyPart),
                // Add more properties as needed
                _ => LogUnknownProperty(propertyPart, conditionText, originalLine)
            };
        };
    }

    private static Func<Unit, bool> HandleSpellCondition(string conditionText, string originalLine = null)
    {
        // Parse: spell.spellname.property[operator][value]
        var parts = conditionText.Split('.');
        if (parts.Length < 3)
        {
            Console.WriteLine($"ERROR: APL spell condition not implemented - invalid format '{conditionText}'" +
                              (originalLine != null ? $" in line: {originalLine}" : ""));
            return _ => false;
        }

        var spellId = parts[1];
        var propertyPart = parts[2];

        // Handle spell.spellname.ready (or available, can_cast, etc.)
        if (propertyPart == "ready" || propertyPart == "available")
        {
            return unit =>
            {
                var spell = unit.SpellBook.FirstOrDefault(s => s.ID == spellId);
                if (spell == null)
                    return false;
                return spell.CheckCanCast(unit);
            };
        }

        // For numeric properties, get the spell upfront
        return unit =>
        {
            var spell = unit.SpellBook.FirstOrDefault(s => s.ID == spellId);

            // Handle different property types
            return propertyPart switch
            {
                var prop when prop.StartsWith("cooldown") =>
                    CompareValues((spell?.OffCooldown ?? 0) - unit.SimLoop.GetElapsed(), propertyPart),
                // Add more spell properties as needed
                _ => LogUnknownProperty(propertyPart, conditionText, originalLine)
            };
        };
    }

    private static Func<Unit, bool> HandleSimpleCondition(string conditionText, string originalLine = null)
    {
        // Handle simple conditions like "targets>3" that don't have dot notation
        return conditionText switch
        {
            var cond when cond.StartsWith("targets") =>
                unit => CompareValues(unit.Targets.Count, conditionText),

            // Add more simple conditions as needed
            _ => CreateErrorCondition(conditionText, originalLine)
        };
    }

    private static Func<Unit, bool> CreateErrorCondition(string conditionText, string originalLine)
    {
        Console.WriteLine($"ERROR: APL condition not implemented - unknown simple condition '{conditionText}'" +
                          (originalLine != null ? $" in line: {originalLine}" : ""));
        return _ => false;
    }

    private static Func<Unit, bool> HandleCharacterCondition(string conditionText, string originalLine = null)
    {
        var parts = conditionText.Split('.');
        if (parts.Length < 2)
        {
            Console.WriteLine($"ERROR: APL character condition not implemented - invalid format '{conditionText}'" +
                              (originalLine != null ? $" in line: {originalLine}" : ""));
            return _ => false;
        }

        var propertyPart = parts[1]; // Use parts[1] instead of parts[2]

        // Handle character properties
        return unit =>
        {
            return propertyPart switch
            {
                var prop when prop.StartsWith("winter_orbs") =>
                    CompareValues(TryGetUnitProperty(unit, "WinterOrbs"), propertyPart),
                var prop when prop.StartsWith("anima") =>
                    CompareValues(TryGetUnitProperty(unit, "Anima"), propertyPart),
                // Add more character properties as needed
                _ => LogUnknownProperty(propertyPart, conditionText, originalLine)
            };
        };
    }

    private static bool LogUnknownProperty(string propertyPart, string conditionText, string originalLine)
    {
        Console.WriteLine(
            $"ERROR: APL condition not implemented - unknown property '{propertyPart}' in '{conditionText}'" +
            (originalLine != null ? $" in line: {originalLine}" : ""));
        return false;
    }

    private static double TryGetUnitProperty(Unit unit, string propertyName)
    {
        try
        {
            var property = unit.GetType().GetProperty(propertyName);
            if (property != null)
            {
                var value = property.GetValue(unit);
                if (value is int intValue) return intValue;
                if (value is double doubleValue) return doubleValue;
                if (value is float floatValue) return floatValue;
            }
        }
        catch
        {
            // Property doesn't exist or can't be accessed
        }

        return 0; // Default to 0 if property doesn't exist or isn't accessible
    }

    // Helper method to parse operator and value from a property string
    private static (string op, float? value) ParseOperatorAndValue(string propertyText)
    {
        var operators = new[] { ">=", "<=", "==", "!=", ">", "<" };

        foreach (var op in operators)
        {
            var opIndex = propertyText.IndexOf(op);
            if (opIndex >= 0)
            {
                var valueText = propertyText.Substring(opIndex + op.Length).Trim();
                if (float.TryParse(valueText, out var value))
                {
                    return (op, value);
                }
            }
        }

        return (null, null);
    }

    // Helper method to compare values using the specified operator from property string
    private static bool CompareValues(double actual, string propertyText)
    {
        var (op, expectedValue) = ParseOperatorAndValue(propertyText);
        if (op == null || !expectedValue.HasValue)
            return false;

        const double Tolerance = 0.000001;

        return op switch
        {
            ">" => actual > expectedValue.Value,
            ">=" => actual >= expectedValue.Value,
            "<" => actual < expectedValue.Value,
            "<=" => actual <= expectedValue.Value,
            "==" => Math.Abs(actual - expectedValue.Value) < Tolerance,
            "!=" => Math.Abs(actual - expectedValue.Value) >= Tolerance,
            _ => false
        };
    }
}