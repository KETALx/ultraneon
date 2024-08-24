using Sandbox.Events;
using Ultraneon.Domain.Events;
using Ultraneon.UI;

namespace Ultraneon.Services;

public class UiService : Component,
	IGameEventHandler<CaptureZoneEvent>,
	IGameEventHandler<PlayerSpawnEvent>,
	IGameEventHandler<CharacterDeathEvent>,
	IGameEventHandler<DamageEvent>,
	IGameEventHandler<MenuActionEvent>,
	IGameEventHandler<GameModeActivatedEvent>
{
	[Property]
	public ScreenPanel RootPanel { get; set; }

	[Property]
	public string CanvasTag = "canvasroot";

	[Property]
	public MainMenu MainMenuPanel { get; set; }

	[Property]
	public Hud HudPanel { get; set; }

	[Property]
	public GameService GameService { get; set; }

	protected override void OnStart()
	{
		base.OnStart();
		RootPanel ??= Scene.GetAllComponents<ScreenPanel>().FirstOrDefault( x => x.GameObject.Tags.Has( CanvasTag ) );

		if ( MainMenuPanel == null )
		{
			Log.Warning( "MainMenu panel is not set in UiService" );
		}

		if ( HudPanel == null )
		{
			Log.Warning( "HUD panel is not set in UiService" );
		}

		ShowHud();
		// ShowMainMenu(); TODO Enable when I find out why Menu isn't sending events
	}

	private void ShowMainMenu()
	{
		MainMenuPanel?.Show();
		HudPanel?.Hide();
		GameObject.Dispatch( new GamePausedEvent() );
		Log.Info( "[UiService] ShowMainMenu called" );
	}

	private void ShowHud()
	{
		MainMenuPanel?.Hide();
		HudPanel?.Show();
		GameObject.Dispatch( new GameResumedEvent() );
		Log.Info( "[UiService] ShowHud called" );
	}

	public void OnGameEvent( CaptureZoneEvent eventArgs )
	{
		// TODO Update UI for zone capture
	}

	public void OnGameEvent( PlayerSpawnEvent eventArgs )
	{
		// TODO Update UI for player spawn
	}

	public void OnGameEvent( CharacterDeathEvent eventArgs )
	{
		// TODO Update UI for character death
	}

	public void OnGameEvent( DamageEvent eventArgs )
	{
		// TODO Update UI for damage events
	}

	public void OnGameEvent( MenuActionEvent eventArgs )
	{
		Log.Info( $"[UiService] Received MenuActionEvent: {eventArgs.Action}" );
		switch ( eventArgs.Action )
		{
			case MenuAction.Play:
				var gameMode = GameService?.GameModes.FirstOrDefault();
				if ( gameMode != null )
				{
					Log.Info( $"[UiService] Activating game mode: {gameMode.GetType().Name}" );
					GameObject.Dispatch( new GameModeActivatedEvent( gameMode ) );
					ShowHud();
				}
				else
				{
					Log.Error( "[UiService] No game mode found to activate." );
				}

				break;
			case MenuAction.Settings:
				// TODO
				Log.Info( "[UiService] Settings!!!" );
				break;
			case MenuAction.Quit:
				// TODO
				Log.Info( "[UiService] Quit!!!" );
				break;
		}
	}

	public void OnGameEvent( GameModeActivatedEvent eventArgs )
	{
		ShowHud();
	}
}
