namespace SimFell;

public class Talent
{
    public string Id { get; }
    public string Name { get; }
    public string GridPos { get; }

    public bool IsActive { get; private set; } = false;

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
        if (IsActive) return;
        OnActivate?.Invoke(unit);
        IsActive = true;
    }

    public void Deactivate(Unit unit)
    {
        if (!IsActive) return;
        OnDeactivate?.Invoke(unit);
        IsActive = false;
    }
}