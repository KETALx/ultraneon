namespace Ultraneon.Game.GameMode;

public abstract class GameMode : Component
{
	public abstract void Initialize();
	public abstract void Cleanup();
	public abstract void LogicUpdate();
	public abstract void PhysicsUpdate();
	public abstract void StartGame();
	public abstract void EndGame();
	public abstract void PauseGame();
	public abstract void ResumeGame();
}
