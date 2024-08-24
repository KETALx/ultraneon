using Sandbox.Events;
using Ultraneon.Domain.Events;
using Ultraneon.Game.GameMode;

namespace Ultraneon.Services;

public class GameService : Component, IGameEventHandler<GameModeActivatedEvent>
{
	private GameMode ActiveGameMode { get; set; }

	public List<GameMode> GameModes { get; set; }

	public void InitializeService()
	{
		GameModes = GameObject.Components.GetAll<GameMode>().ToList();
		Log.Info( GameModes );
	}

	protected override void OnStart()
	{
		if ( IsProxy ) return;
		InitializeService();
		
		var gameMode = GameModes?.FirstOrDefault();
		if ( gameMode != null )
		{
			ActiveGameMode.Initialize();
			ActiveGameMode.StartGame();
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( ActiveGameMode is not null )
		{
			ActiveGameMode.LogicUpdate();
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy ) return;

		if ( ActiveGameMode is not null )
		{
			ActiveGameMode.PhysicsUpdate();
		}
	}

	public void OnGameEvent( GameModeActivatedEvent eventArgs )
	{
		var gameModeToActivate = eventArgs.GameMode;
		Log.Info( $"[GameService] Activating {gameModeToActivate.GetType().Name}" );
		ActiveGameMode = gameModeToActivate;
		ActiveGameMode.Initialize();
		ActiveGameMode.StartGame();
	}
}
