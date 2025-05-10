namespace SimFell;

public abstract class SimLoopListener
{
    public SimLoopListener()
    {
        SimLoop.Instance.OnUpdate += Update;
    }

    protected abstract void Update(double deltaTime);

    ~SimLoopListener()
    {
        SimLoop.Instance.OnUpdate -= Update;
    }
}