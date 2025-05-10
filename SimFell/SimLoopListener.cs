namespace SimFell;

public abstract class SimLoopListener
{
    public SimLoopListener()
    {
        SimLoop.Instance.OnUpdate += Update;
    }

    protected abstract void Update(double deltaTime);

    public void Stop()
    {
        SimLoop.Instance.OnUpdate -= Update;
    }

    ~SimLoopListener()
    {
        SimLoop.Instance.OnUpdate -= Update;
    }
}