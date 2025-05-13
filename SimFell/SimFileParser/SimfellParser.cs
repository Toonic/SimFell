using SimFell.SimFileParser.Models;
using SimAction = SimFell.SimFileParser.Models.ConfigAction;
using SimFell.SimFileParser.Enums;

namespace SimFell.SimFileParser
{
    /// <summary>
    /// Parses .simfell files into <see cref="SimFellConfiguration"/> objects.
    /// </summary>
    public static class SimfellParser
    {
        /// <summary>
        /// Parses multiple conditions joined by ' and '.
        /// </summary>
        private static List<Condition> ParseConditions(string condStr)
        {
            var conds = new List<Condition>();
            var parts = condStr.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var p = part.Trim();
                foreach (var candidate in new[] { ">=", "<=", "==", "!=", ">", "<" })
                {
                    var idx = p.IndexOf(candidate, StringComparison.Ordinal);
                    if (idx > 0)
                    {
                        var left = p.Substring(0, idx).Trim();
                        var right = p.Substring(idx + candidate.Length).Trim();
                        conds.Add(new Condition { Left = left, Operator = candidate, Right = right });
                        break;
                    }
                }
            }
            return conds;
        }

        private static Equipment ParseEquipment_(string equipmentStr)
        {
            var parts = equipmentStr.Split(',');
            var eq = new Equipment();
            if (parts.Length > 0)
                eq.Name = parts[0].Trim();
            for (int i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                var kv = part.Split(new[] { '=' }, 2);
                if (kv.Length != 2)
                    continue;
                var prop = kv[0].Trim().ToLowerInvariant();
                var val = kv[1].Trim();
                switch (prop)
                {
                    case "int":
                        eq.Intellect = int.Parse(val);
                        break;
                    case "stam":
                        eq.Stamina = int.Parse(val);
                        break;
                    case "exp":
                        eq.Expertise = int.Parse(val);
                        break;
                    case "crit":
                        eq.Crit = int.Parse(val);
                        break;
                    case "haste":
                        eq.Haste = int.Parse(val);
                        break;
                    case "spirit":
                        eq.Spirit = int.Parse(val);
                        break;
                    case "gem_bonus":
                        eq.GemBonus = int.Parse(val);
                        break;
                    case "gem":
                        var gemParts = val.Split('_');
                        if (gemParts.Length == 2
                            && Enum.TryParse<GemType>(gemParts[0], true, out var gemEnum)
                            && Enum.TryParse<Tier>(gemParts[1].ToUpperInvariant(), out var tierEnum))
                        {
                            eq.Gem = new GemTier { Gem = gemEnum, Tier = tierEnum };
                        }
                        break;
                    case "ilvl":
                        eq.Ilvl = int.Parse(val);
                        break;
                    case "tier":
                        if (Enum.TryParse<Tier>(val, true, out var tier))
                            eq.Tier = tier;
                        break;
                    case "set":
                        eq.TierSet = Enum.GetValues<TierSet>()
                            .FirstOrDefault(t => t.Identifier() == val);
                        break;
                    default:
                        break;
                }
            }
            return eq;
        }

        private static SimAction ParseAction_(string line)
        {
            var eqIndex = line.IndexOf('=');
            var actionPart = line.Substring(eqIndex + 1).Trim();
            var conditions = new List<Condition>();
            const string condMarker = ",if=";
            var condIndex = actionPart.IndexOf(condMarker, StringComparison.Ordinal);
            if (condIndex >= 0)
            {
                var condPart = actionPart.Substring(condIndex + condMarker.Length);
                conditions.AddRange(ParseConditions(condPart));
                actionPart = actionPart.Substring(0, condIndex).Trim();
            }
            if (actionPart.StartsWith("/"))
                actionPart = actionPart.Substring(1);
            return new SimAction { Name = actionPart, Conditions = conditions };
        }

        /// <summary>
        /// Reads a .simfell file from disk and parses it.
        /// </summary>
        public static SimFellConfiguration ParseFile(string path)
        {
            var lines = File.ReadAllLines(path);
            return ParseLines(lines);
        }

        /// <summary>
        /// Parses lines of a .simfell file into a configuration.
        /// </summary>
        public static SimFellConfiguration ParseLines(IEnumerable<string> lines)
        {
            var config = new SimFellConfiguration();
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                // Remove comments
                line = line.Split('#')[0].Trim();

                // Handle actions (single or +=) with optional conditions
                if (line.StartsWith("action=") || line.StartsWith("actions+="))
                {
                    var action = ParseAction_(line);
                    config.ConfigActions.Add(action);
                    continue;
                }

                // Split into key and value
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                    continue;
                var key = parts[0].Trim().ToLowerInvariant();
                var val = parts[1].Trim();
                if (val.StartsWith('"') && val.EndsWith('"'))
                    val = val.Substring(1, val.Length - 2);

                // Map values to configuration properties
                switch (key)
                {
                    case "name":
                        config.Name = val;
                        break;
                    case "hero":
                        config.Hero = val;
                        break;
                    case "intellect":
                        config.Intellect = int.Parse(val);
                        break;
                    case "crit":
                        config.Crit = double.Parse(val);
                        break;
                    case "expertise":
                        config.Expertise = double.Parse(val);
                        break;
                    case "haste":
                        config.Haste = double.Parse(val);
                        break;
                    case "spirit":
                        config.Spirit = double.Parse(val);
                        break;
                    case "talents":
                        config.Talents = val;
                        break;
                    case "trinket1":
                        config.Trinket1 = val;
                        break;
                    case "trinket2":
                        config.Trinket2 = val;
                        break;
                    case "duration":
                        config.Duration = int.Parse(val);
                        break;
                    case "enemies":
                        config.Enemies = int.Parse(val);
                        break;
                    case "run_count":
                        config.RunCount = int.Parse(val);
                        break;
                    case "gear_helmet":
                        config.Gear.Helmet = ParseEquipment_(val);
                        break;
                    case "gear_shoulder":
                        config.Gear.Shoulder = ParseEquipment_(val);
                        break;
                    // TODO: parse gear slots into Equipment objects
                    default:
                        break;
                }
            }
            return config;
        }
    }
}