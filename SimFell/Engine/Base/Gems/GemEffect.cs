namespace SimFell.Engine.Base.Gems;

public class GemEffect : CharacterEffect<GemEffect>
{
    public string Id;
    public string Name;
    public int RequiredPower { get; }

    public GemEffect(string id, string name, int requiredPower)
    {
        Id = id;
        Name = name;
        RequiredPower = requiredPower;
    }

    public void Activate(Unit unit, int currentPower)
    {
        if (currentPower >= RequiredPower) Activate(unit);
    }
}