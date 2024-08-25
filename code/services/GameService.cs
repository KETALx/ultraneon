using Sandbox.Events;
using Ultraneon.Domain.Events;
using Ultraneon.Game.GameMode;

namespace Ultraneon.Services;

public class GameService : Component, IGameEventHandler<GameModeActivatedEvent>
{
	[Property]
	public List<GameMode> GameModes { get; set; }

	[Property]
	public GameMode ActiveGameMode { get; set; }

	public void InitializeService()
	{
		GameModes = GameObject.Components.GetAll<GameMode>().ToList();
		Log.Info( $"[GameService] Initialized with {GameModes.Count} game modes" );

		var gameMode = GameModes?.FirstOrDefault();
		if ( gameMode != null )
		{
			Log.Info( $"[GameService] Activating game mode: {gameMode.GetType().Name}" );
			GameObject.Dispatch( new GameModeActivatedEvent( gameMode ) );
		}
		else
		{
			Log.Warning( "[GameService] No game mode found to activate" );
		}
	}

	protected override void OnStart()
	{
		if ( IsProxy ) return;
		InitializeService();
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
		ActiveGameMode = gameModeToActivate;
		ActiveGameMode.Initialize();
		ActiveGameMode.StartGame();
		Log.Info( $"[GameService] Activating {gameModeToActivate.GetType().Name}" );
	}
}
