using SimFell;
using Microsoft.Extensions.Configuration;
using SimFell.Logging;

// Build configuration and initialize logging
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();
ConsoleLogger.Configure(configuration);
FileLogger.Configure(configuration);

string configFolder = Path.Combine(AppContext.BaseDirectory, "Configs");
string fullPath = Path.Combine(configFolder, "Rime-NoStats.simfell");

var config = SimFell.SimFileParser.SimfellParser.ParseFile(fullPath);
var player = config.Hero switch
{
    "Rime" => new Rime(100),
    _ => throw new Exception($"Hero {config.Hero} not found")
};

player.SetPrimaryStats(
    config.Intellect,
    (int)config.Crit,
    (int)config.Expertise,
    (int)config.Haste,
    (int)config.Spirit
);

foreach (var action in config.ConfigActions)
{
    // Find the spell in the player's spellbook
    var spell = player.SpellBook.FirstOrDefault(s => s.ID.Replace("-", "_") == action.Name);
    if (spell != null)
    {
        if (action.Conditions.Count > 0)
        {
            var originalCanCast = spell.CanCast;
            spell.CanCast = caster =>
            {
                // bool check = true;
                // foreach (var condition in action.Conditions)
                // {
                //     var condCheck = condition.Check(caster);
                //     // ConsoleLogger.Log(SimulationLogLevel.Debug, $"Condition: {condition} => {condCheck}");

                //     // TODO: Switch the order of the checks once debugged.
                //     check = condCheck && check;
                // }

                return (originalCanCast?.Invoke(caster) ?? true) && action.Conditions.All(c => c.Check(caster));
            };
        }

        player.Rotation.Add(spell);
    }
    else
    {
        ConsoleLogger.Log(SimulationLogLevel.Error, $"Spell {action.Name} not found in spellbook");
    }
}

var enemies = new List<Unit>();
for (int i = 0; i < config.Enemies; i++)
{
    enemies.Add(new Unit("Goblin #" + (i + 1), 1000));
    enemies.Add(new Unit("Goblin #" + ("2"), 1000)); //Forced
}

SimRandom.EnableDeterminism();

SimLoop.ShowConfig(config);
// SimLoop.ShowPrettyConfig(config);

SimLoop.Instance.Start(player, enemies, SimLoop.SimulationMode.Time, config.Duration);
