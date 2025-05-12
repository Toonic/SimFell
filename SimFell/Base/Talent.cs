namespace SimFell;

public class Talent
{
    public string Id { get; }
    public string Name { get; }
    public string GridPos { get; }

    public Action<Unit>? OnActivate { get; set; }
    public Action<Unit>? OnDeactivate { get; set; }

    public Talent(string id, string name, string gridPos, Action<Unit>? onActivate = null, Action<Unit>? onDeactivate = null)
    {
        Id = id;
        Name = name;
        GridPos = gridPos;
        OnActivate = onActivate;
        OnDeactivate = onDeactivate;
    }

    public void Activate(Unit unit)
    {
        OnActivate?.Invoke(unit);
    }
}