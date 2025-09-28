namespace SimFell;

public class CharacterEffect<T> where T : CharacterEffect<T>
{
    public bool IsActive { get; set; } = false;

    public Action<Unit>? OnActivate { get; set; }
    public Action<Unit>? OnDeactivate { get; set; }

    public CharacterEffect(Action<Unit>? onActivate = null, Action<Unit>? onDeactivate = null)
    {
        OnActivate = onActivate;
        OnDeactivate = onDeactivate;
    }

    public T WithOnActivate(Action<Unit> onActivate)
    {
        OnActivate = onActivate;
        return (T)this;
    }

    public T WithOnDeactivate(Action<Unit> onDeactivate)
    {
        OnDeactivate = onDeactivate;
        return (T)this;
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