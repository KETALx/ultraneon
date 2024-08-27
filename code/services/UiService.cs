﻿using Sandbox.Events;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;
using Ultraneon.UI;

namespace Ultraneon.Services;

public class UiService : Component,
	IGameEventHandler<CaptureZoneCapturedEvent>,
	IGameEventHandler<CharacterSpawnEvent>,
	IGameEventHandler<CharacterDeathEvent>,
	IGameEventHandler<DamageEvent>,
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

	private void ShowHud()
	{
		MainMenuPanel?.Hide();
		HudPanel?.Show();
		GameOverPanel?.Hide();
		GameObject.Dispatch( new GameResumedEvent() );
	}

	private void HideHud()
	{
		HudPanel?.Hide();
	}

	public void OnGameEvent( CaptureZoneCapturedEvent capturedEventArgs )
	{
		HudPanel?.AddInfoMessage( $"Zone {capturedEventArgs.ZoneName} captured by {capturedEventArgs.NewTeam}!", InfoFeedPanel.InfoType.Success );
		HudPanel?.DismissStickyMessages();
	}

	public void OnGameEvent( CharacterSpawnEvent eventArgs )
	{
		if ( eventArgs.character.CurrentTeam == Team.Player )
		{
			HudPanel.Show();
		}
	}

	public void OnGameEvent( CharacterDeathEvent eventArgs )
	{
		// HudPanel?.AddInfoMessage( $"{eventArgs.Victim.EntityName} was killed by {eventArgs.Killer?.EntityName ?? "unknown"}", InfoFeedPanel.InfoType.Warning );
		// HudPanel?.Hide();
	}

	public void OnGameEvent( DamageEvent eventArgs )
	{
		if ( eventArgs.Target.CurrentTeam == Team.Player )
		{
			Log.Info( "[UiService] Showing hurt indicator" );
			HudPanel?.ShowHurtIndicator();
			return;
		}
		
		if ( eventArgs.Attacker is not BaseNeonCharacterEntity attackerCharacter )
		{
			return;
		}

		if ( attackerCharacter.CurrentTeam == Team.Player )
		{
			Log.Info( "[UiService] Showing hitmarker" );
			HudPanel?.ShowHitmarker( false );
		}
		else if ( eventArgs.Target.CurrentTeam == Team.Player )
		{
			Log.Info( "[UiService] Showing hurt indicator" );
			HudPanel?.ShowHurtIndicator();
		}
	}

	public void OnGameEvent( GameModeActivatedEvent eventArgs )
	{
		ShowHud();
		HudPanel?.AddInfoMessage( "Singleplayer Initialized!", InfoFeedPanel.InfoType.Debug );
	}

	public void OnGameEvent( UiInfoFeedEvent eventArgs )
	{
		HudPanel?.AddInfoMessage( eventArgs.Message, (InfoFeedPanel.InfoType)eventArgs.Type );
	}

	public void OnGameEvent( GameOverEvent eventArgs )
	{
		HudPanel?.Hide();
		MainMenuPanel?.Hide();
		GameOverPanel?.Show( eventArgs.MaxWaveReached );
		HudPanel?.AddInfoMessage( $"Game Over! Max wave reached: {eventArgs.MaxWaveReached}", InfoFeedPanel.InfoType.Warning );
	}
}
