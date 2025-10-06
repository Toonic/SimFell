namespace SimFell;

public abstract class SimLoopListener
{
    public Simulator Simulator { get; set; }
    private bool disposed;

    public void SetSimulator(Simulator simulator)
    {
        Simulator = simulator;
    }
}