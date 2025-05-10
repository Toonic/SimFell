namespace SimFell.Logging;

[Flags]
public enum SimulationLogLevel
{
    TypeA = 1 << 0,
    TypeB = 1 << 1,
    TypeC = 1 << 2,
    TypeD = 1 << 3,
    TypeE = 1 << 4,
    All = TypeA | TypeB | TypeC | TypeD | TypeE
}