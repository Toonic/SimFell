namespace SimFell;

public abstract class SimLoopListener : IDisposable
{
    public SimLoop? SimLoop { get; private set; }
    private bool disposed;

    public void SetSimLoop(SimLoop simLoop)
    {
        SimLoop = simLoop;
        SimLoop.OnUpdate += Update;
    }

    protected abstract void Update();

    public void Stop()
    {
        if (SimLoop != null)
        {
            SimLoop.OnUpdate -= Update;
            SimLoop = null;
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            Stop();
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    // Optional: only if you absolutely must have a finalizer
    ~SimLoopListener()
    {
        // As a last resort, just call Dispose to detach safely
        Dispose();
    }
}