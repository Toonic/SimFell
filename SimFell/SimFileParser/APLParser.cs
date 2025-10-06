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

    private static readonly Regex OperatorPattern = new Regex(@"(>=|<=|!=|==|>|<|=)");
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
        string remaining;

        if (trimmed.StartsWith("action+="))
        {
            remaining = trimmed.Substring(8);
        }
        else if (trimmed.StartsWith("action="))
        {
            remaining = trimmed.Substring(7);
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
            // Log the special action but skip processing it as a spell action
            var actionType = remaining.Split(',').First();
            Console.WriteLine($"WARNING: APL action type '{actionType}' is not a spell action and will be skipped: {trimmed}");
            return null;
        }

        // Remove the "/"
        remaining = remaining.Substring(1);

        string spellId;
        string conditionText = null;

        // Split the action ID from the rest of the parameters (including ,if=)
        var firstCommaIndex = remaining.IndexOf(',');
        var ifIndex = remaining.IndexOf(",if=");
        if (ifIndex >= 0)
        {
            spellId = remaining.Substring(0, ifIndex);
            conditionText = remaining.Substring(ifIndex + 4); // ",if=".Length = 4
        }
        else if (firstCommaIndex >= 0)
        {
            // Action has other parameters but no condition
            spellId = remaining.Substring(0, firstCommaIndex);
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
        if (string.IsNullOrWhiteSpace(conditionText))
            return _ => true;

        lock (_cacheLock)
        {
            if (_conditionCache.TryGetValue(conditionText, out var cached))
            {
                return cached;
            }

        }
        var compiled = CompileCondition(conditionText, originalLine);

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
            { "talent", HandleTalentCondition },
            // Add more handlers as needed: cooldown, resource, target, etc.
            
            // Temporary handler for tokenized conditions (inner parenthesis results)
            { "__TOKEN", HandleTokenCondition }
        };

    /// <summary>
    /// Compiles the condition string, respecting operator precedence (not -> and -> or).
    /// </summary>
    private static Func<Unit, bool> CompileCondition(string conditionText, string originalLine = null)
    {
        if (string.IsNullOrWhiteSpace(conditionText))
            return _ => true;

        // 1. Convert symbols and recursively resolve inner parenthesis expressions
        var preprocessedCondition = PreProcessCondition(conditionText, originalLine);
        var trimmed = preprocessedCondition.Trim();

        // 2. Handle "not " (Highest precedence, unary)
        if (trimmed.StartsWith("not "))
        {
            var inner = CompileCondition(trimmed.Substring(4).Trim(), originalLine);
            return unit => !inner(unit);
        }

        // 3. Handle 'or' (Lowest precedence - split by 'or' first to ensure 'and's are compiled together)
        if (trimmed.Contains(" or "))
        {
            return CompileCompoundCondition(trimmed, " or ", (a, b) => a || b, originalLine);
        }

        // 4. Handle 'and' (Medium precedence)
        if (trimmed.Contains(" and "))
        {
            return CompileCompoundCondition(trimmed, " and ", (a, b) => a && b, originalLine);
        }

        // 5. Handle single condition (Base case)
        return CompileSingleCondition(trimmed, originalLine);
    }

    /// <summary>
    /// Converts symbols and recursively handles inner parenthesis expressions by tokenizing them.
    /// </summary>
    private static string PreProcessCondition(string conditionText, string originalLine)
    {
        var cleaned = conditionText;

        // Replace '!' with ' not ' (add a space after 'not ' to distinguish it)
        // We use Regex to ensure we don't interfere with '!='
        // This targets '!' followed by any character that is NOT '='
        cleaned = Regex.Replace(cleaned, @"!([^=])", " not $1");

        // Replace '&&' and '||' before their single-symbol counterparts
        cleaned = cleaned.Replace("&&", " and ");
        cleaned = cleaned.Replace("||", " or ");

        // Replace single symbols aggressively, as SimC often omits spaces
        cleaned = Regex.Replace(cleaned, @"(?<![&|])&(?![&|])", " and ");
        cleaned = Regex.Replace(cleaned, @"(?<![&|])\|(?![&|])", " or ");

        // Clean up any double spaces
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        // 2. Recursively find and replace inner parenthesized expressions with unique tokens.
        while (cleaned.Contains('(') || cleaned.Contains(')'))
        {
            var openIndex = cleaned.LastIndexOf('(');
            if (openIndex == -1) break;

            var closeIndex = cleaned.IndexOf(')', openIndex);

            // Check if the parenthesis is part of a function call (e.g., buff.id.duration(3)) 
            // We assume a non-dot prefix means an expression grouping.
            bool isFunctionCall = (openIndex > 0 && cleaned[openIndex - 1] == '.');

            if (isFunctionCall)
            {
                // If it looks like a function call, stop processing it recursively 
                // and rely on the specific condition handlers (which aren't fully implemented)
                break;
            }

            // If it's a true expression group, compile the inner expression
            var innerExpression = cleaned.Substring(openIndex + 1, closeIndex - openIndex - 1).Trim();

            // Compile the inner expression
            var compiledInner = CompileCondition(innerExpression, originalLine);

            // Create a unique token for the compiled function
            var token = $"__TOKEN_{Guid.NewGuid():N}";
            lock (_cacheLock)
            {
                // Store the compiled function in the cache
                _conditionCache[token] = compiledInner;
            }

            // Replace the original expression (including parentheses) with the token
            cleaned = cleaned.Remove(openIndex, closeIndex - openIndex + 1);
            cleaned = cleaned.Insert(openIndex, token);
        }

        return cleaned;
    }

    /// <summary>
    /// Handles the unique token generated from a parenthesized expression.
    /// </summary>
    private static Func<Unit, bool> HandleTokenCondition(string conditionText, string originalLine)
    {
        lock (_cacheLock)
        {
            if (_conditionCache.TryGetValue(conditionText, out var compiledInner))
            {
                return compiledInner;
            }
        }
        return CreateErrorCondition($"Failed to resolve tokenized condition: {conditionText}", originalLine);
    }


    /// <summary>
    /// Compiles a compound condition (e.g., c1 or c2 or c3) using the specified operator and combiner.
    /// </summary>
    private static Func<Unit, bool> CompileCompoundCondition(string conditionText, string operatorText,
        Func<bool, bool, bool> combineFunc, string originalLine)
    {
        // Use StringSplitOptions.TrimEntries to ensure clean splits
        var parts = conditionText.Split(new[] { operatorText }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            // This case should ideally not be reached if CompileCondition logic is sound, but handle defensively
            return CreateErrorCondition($"Invalid compound condition structure: {conditionText}", originalLine);
        }

        // Recursively compile each sub-part. This is crucial: if operatorText is 'or', 
        // the parts might still contain 'and', which will be handled recursively.
        var compiledParts = parts
            .Select(p => CompileCondition(p, originalLine))
            .ToArray();

        return unit =>
        {
            bool result = compiledParts[0](unit);
            for (int i = 1; i < compiledParts.Length; i++)
            {
                // Short-circuiting for OR/AND
                if (operatorText.Equals(" or ") && result) return true;
                if (operatorText.Equals(" and ") && !result) return false;

                result = combineFunc(result, compiledParts[i](unit));
            }
            return result;
        };
    }

    /// <summary>
    /// Dispatches the parsing of a single condition based on its type (e.g., buff, spell, targets).
    /// </summary>
    private static Func<Unit, bool> CompileSingleCondition(string conditionText, string originalLine = null)
    {
        var trimmed = conditionText.Trim();

        // 1. Check if it's a token generated by parenthesis preprocessing
        if (trimmed.StartsWith("__TOKEN_"))
        {
            var tokenType = trimmed.Substring(0, 8); // Should be "__TOKEN_"
            if (_conditionHandlers.TryGetValue(tokenType, out var tokenHandler))
            {
                return tokenHandler(trimmed, originalLine);
            }
        }

        // 2. Check if it's a simple condition (like "targets>3" or "time>10")
        if (!trimmed.Contains('.'))
        {
            return HandleSimpleCondition(trimmed, originalLine);
        }

        // 3. Handle dot-notation condition (e.g., buff.buffid.property)
        var dotIndex = trimmed.IndexOf('.');
        if (dotIndex <= 0)
        {
            return CreateErrorCondition($"Invalid dot-notation condition format: {trimmed}", originalLine);
        }

        var conditionType = trimmed.Substring(0, dotIndex);

        // Look up the appropriate handler
        if (_conditionHandlers.TryGetValue(conditionType, out var handler))
        {
            return handler(trimmed, originalLine);
        }

        // Default: condition type not recognized
        return CreateErrorCondition($"Unknown condition type '{conditionType}' in '{trimmed}'", originalLine);
    }

    // --- CONDITION HANDLER IMPLEMENTATIONS ---

    private static Func<Unit, bool> HandleBuffCondition(string conditionText, string originalLine = null)
    {
        var parts = conditionText.Split('.');
        if (parts.Length < 3)
        {
            return CreateErrorCondition($"APL buff condition invalid format: {conditionText}", originalLine);
        }

        var buffId = parts[1];
        var propertyPartWithComparison = string.Join(".", parts.Skip(2)).Trim(); // Join the rest of the parts

        // Handle existence checks (buff.ID.up/down/exists)
        if (propertyPartWithComparison == "exists" || propertyPartWithComparison == "up")
        {
            return unit => unit.Buffs.Any(aura => aura.ID == buffId);
        }
        if (propertyPartWithComparison == "down")
        {
            return unit => !unit.Buffs.Any(aura => aura.ID == buffId);
        }

        // For properties needing comparison (e.g., buff.ID.duration<5)
        return unit =>
        {
            var buff = unit.Buffs.FirstOrDefault(aura => aura.ID == buffId);
            if (buff == null) return false;

            // Use the start of the string before the operator to check property type
            var match = OperatorPattern.Match(propertyPartWithComparison);
            var propertyKey = match.Success
                ? propertyPartWithComparison.Substring(0, match.Index).Trim()
                : propertyPartWithComparison;

            return propertyKey switch
            {
                "duration" => CompareValues(buff.Duration, propertyPartWithComparison),
                // Add more buff properties here (e.g., "remains", "stacks")
                _ => CreateUnknownPropertyErrorCondition(propertyPartWithComparison, conditionText, originalLine)(unit)
            };
        };
    }

    private static Func<Unit, bool> HandleDebuffCondition(string conditionText, string originalLine = null)
    {
        // Logic mirrors HandleBuffCondition, operating on Unit.Debuffs
        var parts = conditionText.Split('.');
        if (parts.Length < 3)
        {
            return CreateErrorCondition($"APL debuff condition invalid format: {conditionText}", originalLine);
        }


        var debuffId = parts[1];
        var propertyPartWithComparison = string.Join(".", parts.Skip(2)).Trim();

        if (propertyPartWithComparison == "exists" || propertyPartWithComparison == "up")
        {
            return unit => unit.PrimaryTarget == null || unit.PrimaryTarget.Debuffs.Any(aura => aura.ID == debuffId);
        }
        if (propertyPartWithComparison == "down")
        {
            return unit => unit.PrimaryTarget == null || !unit.PrimaryTarget.Debuffs.Any(aura => aura.ID == debuffId);
        }

        return unit =>
        {
            var target = unit.PrimaryTarget;
            if (target == null) return false;
            var debuff = target.Debuffs.FirstOrDefault(aura => aura.ID == debuffId);
            if (debuff == null) return false;

            var match = OperatorPattern.Match(propertyPartWithComparison);
            var propertyKey = match.Success
                ? propertyPartWithComparison.Substring(0, match.Index).Trim()
                : propertyPartWithComparison;

            return propertyKey switch
            {
                "duration" => CompareValues(debuff.Duration, propertyPartWithComparison),
                _ => CreateUnknownPropertyErrorCondition(propertyPartWithComparison, conditionText, originalLine)(unit)
            };
        };
    }

    private static Func<Unit, bool> HandleSpellCondition(string conditionText, string originalLine = null)
    {
        // Parse: spell.spellname.property[operator][value]
        var parts = conditionText.Split('.');
        if (parts.Length < 3)
        {
            return CreateErrorCondition($"APL spell condition invalid format: {conditionText}", originalLine);
        }

        var spellId = parts[1];
        var propertyPartWithComparison = string.Join(".", parts.Skip(2)).Trim();

        // Handle spell.ID.ready (or available)
        if (propertyPartWithComparison == "ready" || propertyPartWithComparison == "available")
        {
            return unit =>
            {
                var spell = unit.SpellBook.FirstOrDefault(s => s.ID == spellId);
                return spell != null && unit.CanCast(spell);
            };
        }

        // For numeric properties
        return unit =>
        {
            var spell = unit.SpellBook.FirstOrDefault(s => s.ID == spellId);

            var match = OperatorPattern.Match(propertyPartWithComparison);
            var propertyKey = match.Success
                ? propertyPartWithComparison.Substring(0, match.Index).Trim()
                : propertyPartWithComparison;

            // Spell must exist to check numeric properties
            if (spell == null) return false;

            return propertyKey switch
            {
                "cooldown" => CompareValues((spell.OffCooldown) - unit.Simulator.Now, propertyPartWithComparison),
                // Add more spell properties as needed (e.g., "charges")
                _ => CreateUnknownPropertyErrorCondition(propertyPartWithComparison, conditionText, originalLine)(unit)
            };
        };
    }

    private static Func<Unit, bool> HandleSimpleCondition(string conditionText, string originalLine = null)
    {
        // Handle simple conditions like "targets>3", "time>10", or "fury<=90"

        // Find the operator to separate the variable name from the comparison logic.
        var match = OperatorPattern.Match(conditionText);

        string propertyNameKey;
        string comparisonPart;

        if (match.Success)
        {
            propertyNameKey = conditionText.Substring(0, match.Index).Trim();
            comparisonPart = conditionText; // comparisonPart is the whole string for CompareValues
        }
        else
        {
            return CreateErrorCondition($"Simple condition must include comparison operator: {conditionText}", originalLine);
        }

        return propertyNameKey switch
        {
            "targets" => unit => CompareValues(unit.Targets.Count, comparisonPart),
            // Add other simple properties here (e.g., 'time', 'fury', 'health')
            _ => CreateErrorCondition($"Unknown simple condition property '{propertyNameKey}' in '{conditionText}'", originalLine)
        };
    }

    /// <summary>
    /// Handles conditions that map directly to Unit properties via reflection.
    /// </summary>
    private static Func<Unit, bool> HandleCharacterCondition(string conditionText, string originalLine = null)
    {
        var parts = conditionText.Split('.');
        if (parts.Length < 2)
        {
            return CreateErrorCondition($"APL character condition invalid format: {conditionText}", originalLine);
        }

        var propertyPartWithComparison = string.Join(".", parts.Skip(1)).Trim();

        // Use regex to find where the variable name ends and the operator begins
        var match = OperatorPattern.Match(propertyPartWithComparison);

        string propertyNameKey;
        string comparisonPart;

        if (match.Success)
        {
            propertyNameKey = propertyPartWithComparison.Substring(0, match.Index).Trim();
            comparisonPart = propertyPartWithComparison; // Pass the whole string to CompareValues
        }
        else
        {
            // If no operator is found, this is an error for a character stat check
            return CreateErrorCondition($"Character condition must include comparison operator: {conditionText}", originalLine);
        }

        // Map APL key (e.g., "winter_orbs") to C# Property Name (e.g., "WinterOrbs")
        var propertyName = propertyNameKey switch
        {
            "winter_orbs" => "WinterOrbs",
            "anima" => "Anima",
            "builder_one" => "BuilderOne",
            "builder_two" => "BuilderTwo",
            _ => null
        };

        if (propertyName == null)
        {
            return CreateUnknownPropertyErrorCondition(propertyPartWithComparison, conditionText, originalLine);
        }

        // Return the compiled condition delegate
        return unit =>
        {
            double actualValue = TryGetUnitProperty(unit, propertyName);
            return CompareValues(actualValue, comparisonPart);
        };
    }

    private static Func<Unit, bool> HandleTalentCondition(string conditionText, string originalLine = null)
    {
        // Expected format: "talent.talentId" (optionally with ".enabled" or ".disabled")
        var parts = conditionText.Split('.');
        if (parts.Length < 2)
        {
            return CreateErrorCondition($"APL talent condition invalid format: {conditionText}", originalLine);
        }

        var talentId = parts[1].Replace('_', '-');
        string property = parts.Length > 2 ? parts[2] : "enabled"; // Default to "enabled" if unspecified

        return unit =>
        {
            var talent = unit.Talents.FirstOrDefault(t => t.Id == talentId);
            if (talent == null)
            {
                CreateErrorCondition($"Talent '{talentId}' not found on unit {unit.Name}", originalLine);
                return false;
            }

            return property switch
            {
                "enabled" => talent.IsActive,
                "disabled" => !talent.IsActive,
                _ => CreateUnknownPropertyErrorCondition(property, conditionText, originalLine)(unit)
            };
        };
    }



    // --- HELPER METHODS ---

    private static Func<Unit, bool> CreateErrorCondition(string errorMessage, string originalLine)
    {
        Console.WriteLine($"ERROR: APL condition not implemented - {errorMessage}" +
                          (originalLine != null ? $" in line: {originalLine}" : ""));
        return _ => false;
    }

    private static Func<Unit, bool> CreateUnknownPropertyErrorCondition(string propertyPart, string conditionText, string originalLine)
    {
        Console.WriteLine(
            $"ERROR: APL condition not implemented - unknown property '{propertyPart}' in '{conditionText}'" +
            (originalLine != null ? $" in line: {originalLine}" : ""));
        return _ => false;
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
                if (value is bool boolValue) return boolValue ? 1.0 : 0.0;
            }
        }
        catch 
        {
            // Property doesn't exist or can't be accessed
        }

        return 0.0; // Default to 0 if property doesn't exist or isn't accessible
    }

    // Helper method to parse operator and value from a property string
    private static (string op, double? value) ParseOperatorAndValue(string propertyText)
    {
        var operators = new[] { ">=", "<=", "==", "!=", ">", "<", "=" };

        foreach (var op in operators)
        {
            var opIndex = propertyText.IndexOf(op);
            if (opIndex >= 0)
            {
                var valueText = propertyText.Substring(opIndex + op.Length).Trim();
                // Ensure we use double.TryParse for consistency with TryGetUnitProperty's return type
                if (double.TryParse(valueText, out var value))
                {
                    string effectiveOp = (op == "=") ? "==" : op;
                    return (effectiveOp, value);
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
        {
            return false;
        }

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