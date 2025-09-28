using SimFell.Logging;

namespace SimFell;

public class Talent : CharacterEffect<Talent>
{
    public string Id { get; }
    public string Name { get; }
    public string GridPos { get; }

    public Talent(string id, string name, string gridPos)
    {
        Id = id;
        Name = name;
        GridPos = gridPos;
    }

    [Obsolete]
    public Talent(string id, string name, string gridPos, Action<Unit>? onActivate = null,
        Action<Unit>? onDeactivate = null)
    {
        //ConsoleLogger.Log(SimulationLogLevel.Error, "Talent Deprecated. Use other constructor.");
        Id = id;
        Name = name;
        GridPos = gridPos;
        OnActivate = onActivate;
        OnDeactivate = onDeactivate;
    }
}