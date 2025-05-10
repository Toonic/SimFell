// See https://aka.ms/new-console-template for more information

using SimFell;

var player = new Rime("Rime", 100);
player.SetPrimaryStats(100, 0,0,0,0);
var enemies = new List<Unit>
{
    new("Goblin1", 1000),
    new("Goblin2", 1000)
};
SimRandom.DisableDeterminism();
//SimRandom.EnableDeterminism();
SimLoop.Instance.Start(player, enemies);