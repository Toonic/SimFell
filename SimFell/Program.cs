using SimFell;
using Microsoft.Extensions.Configuration;
using SimFell.Logging;

// Build configuration and initialize logging
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();
Logger.Configure(configuration);

string configFolder = Path.Combine(AppContext.BaseDirectory, "Configs");
string fullPath = Path.Combine(configFolder, "test.simfell");

var config = SimFell.SimFileParser.SimfellParser.ParseFile(fullPath);

var player = config.Hero switch
{
    "Rime" => new Rime("Rime", 100),
    _ => throw new Exception($"Hero {config.Hero} not found")
};

player.SetPrimaryStats(
    config.Intellect,
    (int)config.Crit,
    (int)config.Expertise,
    (int)config.Haste,
    (int)config.Spirit
);

var enemies = new List<Unit>();
for (int i = 0; i < config.Enemies; i++)
{
    enemies.Add(new("Goblin #" + (i + 1), 1000));
}

SimRandom.EnableDeterminism();

SimLoop.ShowConfig(config);

SimLoop.Instance.Start(player, enemies, SimLoop.SimulationMode.Time, config.Duration);
