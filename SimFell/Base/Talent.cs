namespace SimFell;

public class Talent
{
    public string Id { get; }
    public string Name { get; }

    public Action<Unit>? OnApply { get; set; }
    public Action<Unit>? OnRemove { get; set; }

    public Action<Unit, object>? OnCrit { get; set; }
    public Func<Unit, Spell, float, float>? ModifyCritChance { get; set; }

    public Talent(string id, string name)
    {
        Id = id;
        Name = name;
    }
}