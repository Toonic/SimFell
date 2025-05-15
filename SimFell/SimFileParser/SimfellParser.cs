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
        /// Parse conditions from a string.
        /// </summary>
        /// <param name="condStr">The string to parse the conditions from.</param>
        /// <returns>A list of conditions.</returns>
        private static List<Condition> ParseConditions(string condStr)
        {
            var conds = new List<Condition>();
            var parts = condStr.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var p = part.Trim();

                // Handle 'exists' without a comparison operator
                var pLower = p.ToLowerInvariant();
                if (pLower.StartsWith("not ") && pLower.EndsWith(".exists"))
                {
                    // 'not buff.xxx.exists' -> buff.xxx.exists == 0
                    var inner = p.Substring(4).Trim();
                    conds.Add(new Condition { Left = inner, Operator = "==", Right = "0" });
                    continue;
                }
                else if (pLower.EndsWith(".exists"))
                {
                    // 'buff.xxx.exists' -> buff.xxx.exists == 1
                    conds.Add(new Condition { Left = p, Operator = "==", Right = "1" });
                    continue;
                }

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

        /// <summary>
        /// Parse an equipment string into an Equipment object.
        /// </summary>
        /// <param name="equipmentStr">The string to parse the equipment from.</param>
        /// <returns>An Equipment object.</returns>
        private static Equipment ParseEquipment(string equipmentStr)
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
                        if (eq.Gem != null)
                        {
                            double total = eq.Gem.Power * (1 + (eq.GemBonus ?? 0) / 100.0);
                            eq.Gem.Power = (int)total;
                        }
                        break;
                    case "gem":
                        var gemParts = val.Split('_');
                        if (gemParts.Length == 2
                            && Enum.TryParse<GemType>(gemParts[0], true, out var gemEnum)
                            && Enum.TryParse<Tier>(gemParts[1].ToUpperInvariant(), out var tierEnum))
                        {
                            eq.Gem = new GemTier { Gem = gemEnum, Tier = tierEnum, Power = (int)tierEnum * 100 };
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

        /// <summary>
        /// Parse an action specification from a line.
        /// </summary>
        /// <param name="line">The line to parse the action from.</param>
        /// <returns>An action specification.</returns>
        private static SimAction ParseAction(string line)
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
        /// <param name="path">The path to the file to parse.</param>
        /// <returns>A new SimFellConfiguration object.</returns>
        public static SimFellConfiguration ParseFile(string path)
        {
            var lines = File.ReadAllLines(path);
            return ParseLines(lines);
        }

        /// <summary>
        /// Parse lines of a .simfell file into a configuration.
        /// </summary>
        /// <param name="lines">The lines to parse.</param>
        /// <returns>A new SimFellConfiguration object.</returns>
        public static SimFellConfiguration ParseLines(IEnumerable<string> lines)
        {
            var config = new SimFellConfiguration();
            foreach (var rawLine in lines)
            {
                var line = CleanLine(rawLine);
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("action=") || line.StartsWith("action+="))
                {
                    config.ConfigActions.Add(ParseAction(line));
                }
                else
                {
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    var key = line.Substring(0, idx).Trim().ToLowerInvariant();
                    var val = line.Substring(idx + 1).Trim().Trim('"');
                    ProcessSetting(config, key, val);
                }
            }
            return config;
        }

        /// <summary>
        /// Removes comments and trims whitespace from a raw configuration line.
        /// </summary>
        /// <param name="rawLine">The raw line to clean.</param>
        /// <returns>A cleaned line.</returns>
        private static string CleanLine(string rawLine)
        {
            var trimmed = rawLine.Trim();
            if (trimmed.StartsWith("#")) return string.Empty;
            return trimmed.Split('#')[0].Trim();
        }

        /// <summary>
        /// Applies a key/value pair to the configuration.
        /// </summary>
        /// <param name="config">The configuration to apply the key/value pair to.</param>
        /// <param name="key">The key to apply.</param>
        /// <param name="val">The value to apply.</param>
        private static void ProcessSetting(SimFellConfiguration config, string key, string val)
        {
            switch (key)
            {
                case "name": config.Name = val; break;
                case "hero": config.Hero = val; break;
                case "intellect": config.Intellect = int.Parse(val); break;
                case "crit": config.Crit = double.Parse(val); break;
                case "expertise": config.Expertise = double.Parse(val); break;
                case "haste": config.Haste = double.Parse(val); break;
                case "spirit": config.Spirit = double.Parse(val); break;
                case "talents": config.Talents = val; break;
                case "trinket1": config.Trinket1 = val; break;
                case "trinket2": config.Trinket2 = val; break;
                case "duration": config.Duration = int.Parse(val); break;
                case "enemies": config.Enemies = int.Parse(val); break;
                case "run_count": config.RunCount = int.Parse(val); break;
                case "gear_helmet": config.Gear.Helmet = ParseEquipment(val); break;
                case "gear_shoulder": config.Gear.Shoulder = ParseEquipment(val); break;
                // TODO: parse other gear slots into Equipment objects
                default: break;
            }
        }
    }
}