using Events;

public class App_Init : Event<GameStarted>
{
    public override void Run(GameStarted value)
    {
        EventBus.Publish(new ArchSystem_1_Test());
    }
}