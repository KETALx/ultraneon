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
	IGameEventHandler<GameModeActivatedEvent>,
	IGameEventHandler<UiInfoFeedEvent>,
	IGameEventHandler<GameOverEvent>
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
	public SpGameOver GameOverPanel { get; set; }

	[Property]
	public GameService GameService { get; set; }

	protected override void OnStart()
	{
		base.OnStart();
		RootPanel ??= Scene.GetAllComponents<ScreenPanel>().FirstOrDefault( x => x.GameObject.Tags.Has( CanvasTag ) );

		if ( MainMenuPanel == null ) Log.Warning( "MainMenu panel is not set in UiService" );
		if ( HudPanel == null ) Log.Warning( "HUD panel is not set in UiService" );
		if ( GameOverPanel == null ) Log.Warning( "GameOverPanel is not set in UiService" );

		ShowHud();
	}

	private void ShowMainMenu()
	{
		MainMenuPanel?.Show();
		HudPanel?.Hide();
		GameOverPanel?.Hide();
		GameObject.Dispatch( new GamePausedEvent() );
	}

	private void ShowHud()
	{
		MainMenuPanel?.Hide();
		HudPanel?.Show();
		GameOverPanel?.Hide();
		GameObject.Dispatch( new GameResumedEvent() );
	}

	public void OnGameEvent( CaptureZoneEvent eventArgs )
	{
		HudPanel?.AddInfoMessage( $"Zone {eventArgs.ZoneName} captured by {eventArgs.NewTeam}!", InfoFeedPanel.InfoType.Success );
	}

	public void OnGameEvent( PlayerSpawnEvent eventArgs )
	{
		HudPanel?.AddInfoMessage( "Player spawned", InfoFeedPanel.InfoType.Normal );
	}

	public void OnGameEvent( CharacterDeathEvent eventArgs )
	{
		HudPanel?.AddInfoMessage( $"{eventArgs.Victim.EntityName} was killed by {eventArgs.Killer?.EntityName ?? "unknown"}", InfoFeedPanel.InfoType.Warning );
	}

	public void OnGameEvent( DamageEvent eventArgs )
	{
		// Handle damage event if needed
	}

	public void OnGameEvent( MenuActionEvent eventArgs )
	{
		switch ( eventArgs.Action )
		{
			case MenuAction.Play:
				var gameMode = GameService?.GameModes.FirstOrDefault();
				if ( gameMode != null )
				{
					GameObject.Dispatch( new GameModeActivatedEvent( gameMode ) );
					ShowHud();
				}
				else
				{
					Log.Error( "[UiService] No game mode found to activate." );
				}
				break;
			case MenuAction.Settings:
				Log.Info( "[UiService] Settings!!!" );
				break;
			case MenuAction.Quit:
				Log.Info( "[UiService] Quit!!!" );
				break;
		}
	}

	public void OnGameEvent( GameModeActivatedEvent eventArgs )
	{
		ShowHud();
		HudPanel?.AddInfoMessage( "Game started!", InfoFeedPanel.InfoType.Success );
	}

	public void OnGameEvent( UiInfoFeedEvent eventArgs )
	{
		HudPanel?.AddInfoMessage( eventArgs.Message, (InfoFeedPanel.InfoType)eventArgs.Type );
	}

	public void OnGameEvent( GameOverEvent eventArgs )
	{
		HudPanel?.Hide();
		GameOverPanel?.Show( eventArgs.MaxWaveReached );
		HudPanel?.AddInfoMessage( $"Game Over! Max wave reached: {eventArgs.MaxWaveReached}", InfoFeedPanel.InfoType.Warning );
	}
}
