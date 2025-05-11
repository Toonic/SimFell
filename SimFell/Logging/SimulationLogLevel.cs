namespace SimFell.Logging;

[Flags]
public enum SimulationLogLevel
{
    CastEvents = 1 << 0,
    DamageEvents = 1 << 1,
    BuffEvents = 1 << 2,
    DebuffEvents = 1 << 3,
    Debug = 1 << 4,
    All = CastEvents | DamageEvents | BuffEvents | DebuffEvents | Debug
}