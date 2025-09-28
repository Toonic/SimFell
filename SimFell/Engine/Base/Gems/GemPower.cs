using SimFell.Engine.Base.Gems;

namespace SimFell;

public abstract class GemPower
{
    public GemType GemType { get; protected set; }
    public int CurrentPower;
    public List<GemEffect> gemEffects = new List<GemEffect>();

    protected GemPower()
    {
        Initialize();
    }

    private void Initialize()
    {
        ConfigureGemEffects();
    }

    public void Activate(Unit unit, int gemPower)
    {
        CurrentPower = gemPower;
        foreach (GemEffect gemEffect in gemEffects)
        {
            gemEffect.Activate(unit, gemPower);
        }
    }

    protected abstract void ConfigureGemEffects();
}