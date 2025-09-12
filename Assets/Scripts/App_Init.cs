using Events;

public class App_Init : AEvent<GameStarted>
{
	public override void Run(GameStarted value)
	{
		EventBus.Publish(new ArchSystemTest_1_Event());
	}
}