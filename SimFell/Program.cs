// See https://aka.ms/new-console-template for more information

using SimFell;

var player = new Mage("Rime", 100);
var enemies = new List<Unit>
{
    new("Goblin1", 1000),
    new("Goblin2", 1000)
};
var trinket = new Trinket();
trinket.Equip(player);
SimLoop.Instance.Start(player, enemies);