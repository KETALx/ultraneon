using Sandbox.Events;
using System;
using System.Linq;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;
using Ultraneon.Player;
using Ultraneon.Game;
using Ultraneon.Services;

namespace Ultraneon.Game.GameMode.Sp;

public class SingleplayerGameMode : GameMode,
	IGameEventHandler<CaptureZoneEvent>,
	IGameEventHandler<PlayerSpawnEvent>,
	IGameEventHandler<CharacterDeathEvent>,
	IGameEventHandler<DamageEvent>
{
	[Property]
	public float CaptureTime { get; set; } = 15f;

	[Property]
	public float OvertimeSeconds { get; set; } = 60f;

	[Property]
	public List<CaptureZoneEntity> CaptureZones { get; set; } = new();

	[Property]
	public GameObject PlayerPrefab { get; set; }

	[Property]
	public WaveManager WaveManager { get; set; }

	private PlayerNeon player;
	private bool gameStarted;
	private bool warmupPhase = true;
	private TimeUntil overtimeEnd;
	private bool isOvertime;

	public override void Initialize()
	{
		CaptureZones = Scene.GetAllComponents<CaptureZoneEntity>().ToList();
		foreach ( var zone in CaptureZones )
		{
			zone.ControllingTeam = Team.Neutral;
			zone.CaptureProgress = 0f;
		}

		gameStarted = false;
		warmupPhase = true;
		isOvertime = false;
		PauseGame();
		Log.Info( "[SinglePlayerGameMode] SingleplayerGameMode initialized" );
	}

	public override void Cleanup()
	{
		CaptureZones.Clear();
		gameStarted = false;
		Log.Info( "[SinglePlayerGameMode] SingleplayerGameMode cleaned up" );
	}

	public override void LogicUpdate()
	{
		if ( !gameStarted )
		{
			Log.Warning( "[SingleplayerGameMode] LogicUpdate called but game not started" );
			return;
		}

		if ( warmupPhase )
		{
			CheckWarmupEnd();
		}
		else
		{
			UpdateGameState();
			CheckGameOver();
		}
	}

	public override void PhysicsUpdate() { }

	public override void PauseGame()
	{
		Log.Info( "[SinglePlayerGameMode] Paused Game" );
		Scene.TimeScale = 0.0f;
		// TODO: Show pause menu
	}

	public override void ResumeGame()
	{
		Log.Info( "[SinglePlayerGameMode] Resumed Game" );
		Scene.TimeScale = 1.0f;
		// TODO: Hide pause menu
	}

	public override void StartGame()
	{
		SpawnPlayer();
		gameStarted = true;
		warmupPhase = true;
		ResumeGame();
		ShowInfoMessage( "Capture the zone to start the game!", UiInfoFeedType.Normal );
		Log.Info( "[SinglePlayerGameMode] Let there be light!" );
	}

	public override void EndGame()
	{
		ShowInfoMessage( "Game over!", UiInfoFeedType.Warning );
		gameStarted = false;
		int maxWave = WaveManager.GetCurrentWave();
		GameObject.Dispatch( new GameOverEvent( maxWave ) );
	}

	private void SpawnPlayer()
	{
		Log.Info( "[SingleplayerGameMode] Attempting to spawn player" );
		var spawnPoint = Scene.GetAllComponents<SpawnPoint>().FirstOrDefault( s => s.Tags.Contains( "player" ) || s.GameObject.Name.Equals( "info_player_start" ) );
		if ( spawnPoint != null && PlayerPrefab != null )
		{
			var playerObject = PlayerPrefab.Clone( spawnPoint.Transform.Position, spawnPoint.Transform.Rotation );
			player = playerObject.Components.Get<PlayerNeon>();
			if ( player != null )
			{
				Log.Info( $"[SingleplayerGameMode] Player spawned at {spawnPoint.Transform.Position}" );
				GameObject.Dispatch( new PlayerSpawnEvent( Team.Player ) );
			}
			else
			{
				Log.Error( "[SingleplayerGameMode] Failed to get PlayerNeon component from spawned player object" );
			}
		}
		else
		{
			Log.Error( "[SingleplayerGameMode] Failed to spawn player: No valid spawn point or player prefab" );
		}
	}

	private void CheckWarmupEnd()
	{
		if ( CaptureZones.Any( z => z.ControllingTeam == Team.Player ) )
		{
			warmupPhase = false;
			WaveManager.StartWaves();
			ShowInfoMessage( "Zone captured! Prepare for incoming waves!", UiInfoFeedType.Success );
		}
	}

	private void UpdateGameState()
	{
		bool allZonesEnemyControlled = CaptureZones.All( z => z.ControllingTeam == Team.Enemy );

		if ( allZonesEnemyControlled && !isOvertime )
		{
			StartOvertime();
		}
		else if ( !allZonesEnemyControlled && isOvertime )
		{
			EndOvertime();
		}

		if ( isOvertime )
		{
			UpdateOvertime();
		}
	}

	private void StartOvertime()
	{
		isOvertime = true;
		overtimeEnd = OvertimeSeconds;
		player.EnterOvertime();
		ShowInfoMessage( $"OVERTIME! Recapture the zone in {OvertimeSeconds} seconds or the game is over!", UiInfoFeedType.Warning );

		foreach ( var zone in CaptureZones )
		{
			zone.AllowBotCapture = false;
		}
	}

	private void EndOvertime()
	{
		isOvertime = false;
		player.ExitOvertime();
		ShowInfoMessage( "Zone recaptured! Continue defending!", UiInfoFeedType.Success );

		foreach ( var zone in CaptureZones )
		{
			zone.AllowBotCapture = true;
		}
	}

	private void UpdateOvertime()
	{
		float remainingTime = overtimeEnd;
		if ( remainingTime <= 0 )
		{
			EndGame();
		}
		else if ( remainingTime <= 10 && (int)remainingTime != (int)(remainingTime + Time.Delta) )
		{
			ShowInfoMessage( $"Overtime ending in {(int)remainingTime} seconds!", UiInfoFeedType.Warning );
		}
	}

	private void CheckGameOver()
	{
		if ( isOvertime && player.IsDead )
		{
			EndGame();
		}
	}

	public void OnGameEvent( CaptureZoneEvent eventArgs )
	{
		if ( eventArgs.NewTeam == Team.Player && isOvertime )
		{
			EndOvertime();
		}
	}

	public void OnGameEvent( PlayerSpawnEvent eventArgs )
	{
		ShowInfoMessage( $"Player spawned for team {eventArgs.Team}", UiInfoFeedType.Normal );
	}

	public void OnGameEvent( CharacterDeathEvent eventArgs )
	{
		if ( eventArgs.Victim == player )
		{
			if ( isOvertime )
			{
				EndGame();
			}
			else
			{
				SpawnPlayer();
			}
		}
	}

	public void OnGameEvent( DamageEvent eventArgs )
	{
		if ( eventArgs.Target is BaseNeonCharacterEntity targetEntity )
		{
			if ( targetEntity.Health <= 0 )
			{
				var killerEntity = eventArgs.Attacker?.Components.Get<BaseNeonCharacterEntity>();
				GameObject.Dispatch( new CharacterDeathEvent( targetEntity, killerEntity, IsStylishKill( killerEntity, targetEntity ) ) );
			}
		}
	}

	private bool IsStylishKill( BaseNeonCharacterEntity killer, BaseNeonCharacterEntity victim )
	{
		// TODO: Implement logic for determining if it's a stylish kill (airborne, wallbang)
		return false;
	}

	private void ShowInfoMessage( string message, UiInfoFeedType type )
	{
		GameObject.Dispatch( new UiInfoFeedEvent( message, type ) );
	}
}
