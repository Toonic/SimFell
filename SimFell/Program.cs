using SimFell;
using Microsoft.Extensions.Configuration;
using SimFell.Logging;
using SimFell.SimFileParser.Models;

// Build configuration and initialize logging
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .Build();
ConsoleLogger.Configure(configuration);
FileLogger.Configure(configuration);

string configFolder = Path.Combine(AppContext.BaseDirectory, "Configs");
string fullPath = Path.Combine(configFolder, "Rime-NoStats.simfell");

var config = SimFellConfiguration.FromFile(fullPath);

var enemies = new List<Unit>();
for (int i = 0; i < config.Enemies; i++)
{
    enemies.Add(new Unit("Goblin #" + (i + 1), 1000));
}

SimRandom.EnableDeterminism();

SimLoop.ShowConfig(config);
// SimLoop.ShowPrettyConfig(config);

SimLoop.Instance.Start(config.Player, enemies, SimLoop.SimulationMode.Time, config.Duration);
