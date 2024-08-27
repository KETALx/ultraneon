using System;
using Sandbox.Events;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;
using Ultraneon.Player;
using Ultraneon.Services;

namespace Ultraneon.Game.GameMode.Sp;

public class SingleplayerGameMode : GameMode,
	ICaptureZoneGameMode,
	IGameEventHandler<CaptureZoneCapturedEvent>,
	IGameEventHandler<CharacterDeathEvent>,
	IGameEventHandler<DamageEvent>
{
	[Property]
	public float PlayerRespawnTime { get; set; } = 6f;

	[Property]
	public float OvertimeSeconds { get; set; } = 60f;

	[Property]
	public List<CaptureZoneEntity> CaptureZones { get; set; } = new();

	[Property]
	public GameObject PlayerPrefab { get; set; }

	[Property]
	private PlayerNeon Player;

	[Property]
	public WaveManager WaveManager { get; set; }

	private bool gameStarted;
	private bool warmupPhase = true;
	private TimeSince overtimeStarted;
	private TimeSince timeSincePlayerDeath;
	private bool isOvertime;

	[Button( "Kill Player", "close" )]
	public void DebugKillPlayer()
	{
		if ( Player != null && Player.IsAlive )
		{
			var damageInfo = new DamageInfo { Damage = 200.0f, Attacker = GameObject, Position = Player.Transform.Position };
			Player.OnDamage( damageInfo );
		}
	}

	[Button( "Damage Player", "close" )]
	public void DebugDamagePlayer()
	{
		if ( Player != null && Player.IsAlive )
		{
			var damageInfo = new DamageInfo { Damage = 10.0f, Attacker = GameObject, Position = Player.Transform.Position };
			Player.OnDamage( damageInfo );
		}
	}

	[Button( "Kill All Bots", "smart_toy" )]
	public void DebugKillBots()
	{
		var bots = Scene.GetAllComponents<BotAi>();
		foreach ( var bot in bots )
		{
			if ( bot.IsAlive )
			{
				var damageInfo = new DamageInfo { Damage = 200.0f, Attacker = GameObject, Position = bot.Transform.Position };
				bot.OnDamage( damageInfo );
			}
		}
	}

	[Button( "Instant Overtime", "watch" )]
	public void DebugOvertime()
	{
		warmupPhase = false;
		CaptureZones.ForEach( c => c.ControllingTeam = Team.Enemy );
		StartOvertime();
	}

	public override void Initialize()
	{
		CaptureZones = Scene.GetAllComponents<CaptureZoneEntity>().ToList();
		foreach ( var zone in CaptureZones )
		{
			zone.ControllingTeam = Team.Neutral;
			zone.CaptureProgress = 0f;
			zone.GameMode = this;
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
			return;
		}

		UpdatePlayerRespawn();

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
		SpawnOrRespawnPlayer();
		gameStarted = true;
		warmupPhase = true;
		ResumeGame();
		ShowInfoMessage( "Capture the zone to start the game!", UiInfoFeedType.Sticky );
		Log.Info( "[SinglePlayerGameMode] Let there be light!" );
	}

	public override void EndGame()
	{
		gameStarted = false;
		int maxWave = WaveManager?.GetCurrentWave() ?? 0;
		GameObject.Dispatch( new GameOverEvent( maxWave ) );
	}

	private SpawnPoint FindPlayerSpawnPoint()
	{
		return Scene.GetAllComponents<SpawnPoint>().FirstOrDefault( s => s.Tags.Contains( "player" ) || s.GameObject.Name.Equals( "info_player_start" ) );
	}

	private void SpawnOrRespawnPlayer()
	{
		Log.Info( "[SingleplayerGameMode] Attempting to spawn player" );
		var spawnPoint = FindPlayerSpawnPoint();
		if ( spawnPoint == null )
		{
			Log.Error( "[SingleplayerGameMode] Failed to spawn player: No valid spawn point with Component SpawnPoint, and tag 'player' or name 'info_player_start'" );
			return;
		}

		if ( Player != null )
		{
			if ( Player.IsAlive )
			{
				return;
			}

			if ( isOvertime )
			{
				return;
			}

			Player.Transform.Position = spawnPoint.Transform.Position;
			Player.Transform.Rotation = spawnPoint.Transform.Rotation;
			// ShowInfoMessage( "Player Respawn!", UiInfoFeedType.Debug );
		}
		else
		{
			var playerObject = PlayerPrefab.Clone( spawnPoint.Transform.Position, spawnPoint.Transform.Rotation );
			Player = playerObject.Components.Get<PlayerNeon>();
			// ShowInfoMessage( "Fresh Player Spawn!", UiInfoFeedType.Debug );
		}

		if ( Player == null )
		{
			Log.Error( "[SingleplayerGameMode] Failed to find player component" );
			return;
		}

		Log.Info( $"[SingleplayerGameMode] Player spawned at {spawnPoint.Transform.Position}" );
		Scene.Dispatch( new CharacterSpawnEvent( Player, spawnPoint.Transform.Position ) );
	}

	private void CheckWarmupEnd()
	{
		if ( CaptureZones.Any( z => z.ControllingTeam == Team.Player ) )
		{
			warmupPhase = false;
			WaveManager?.StartWaves();
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

	private void UpdatePlayerRespawn()
	{
		if ( Player is { IsAlive: false } && timeSincePlayerDeath >= PlayerRespawnTime )
		{
			SpawnOrRespawnPlayer();
		}
	}

	private void StartOvertime()
	{
		isOvertime = true;
		overtimeStarted = 0f;

		if ( Player != null && Player.IsAlive )
		{
			ShowInfoMessage( $"OVERTIME! Recapture the zone in {OvertimeSeconds} seconds or the game is over!", UiInfoFeedType.Warning );
		}

		foreach ( var zone in CaptureZones )
		{
			zone.AllowBotCapture = false;
		}

		Log.Info( "[SinglePlayerGameMode] Overtime started" );
	}

	private void EndOvertime()
	{
		isOvertime = false;
		ShowInfoMessage( "Zone recaptured! Continue defending!", UiInfoFeedType.Success );

		foreach ( var zone in CaptureZones )
		{
			zone.AllowBotCapture = true;
		}

		Log.Info( "[SinglePlayerGameMode] Overtime ended" );
	}

	private void UpdateOvertime()
	{
		float remainingTime = OvertimeSeconds - overtimeStarted;
		if ( remainingTime <= 0 )
		{
			EndGame();
		}
		else if ( remainingTime <= 10 && (int)remainingTime != (int)(remainingTime - Time.Delta) )
		{
			ShowInfoMessage( $"Overtime ending in {(int)remainingTime} seconds!", UiInfoFeedType.Warning );
		}
	}

	private void CheckGameOver()
	{
		if ( isOvertime && (Player == null || !Player.IsAlive) )
		{
			EndGame();
		}
	}

	public void OnCaptureStarted( CaptureZoneEntity zone )
	{
		// ShowInfoMessage( $"Capture of {zone.PointName} has started!", UiInfoFeedType.Debug );
	}

	public void OnCaptureStopped( CaptureZoneEntity zone )
	{
		// ShowInfoMessage( $"Capture of {zone.PointName} has stopped!", UiInfoFeedType.Debug );
	}

	public void OnPointCaptured( CaptureZoneEntity zone, Team previousTeam, Team newTeam )
	{
		Scene.Dispatch( new CaptureZoneCapturedEvent( zone.PointName, previousTeam, newTeam ) );

		if ( newTeam == Team.Player && isOvertime )
		{
			EndOvertime();
		}

		if ( CaptureZones.All( z => z.ControllingTeam == Team.Enemy ) && !isOvertime )
		{
			StartOvertime();
		}
	}

	public void OnCaptureProgressUpdated( CaptureZoneEntity zone, float progress )
	{
		Scene.Dispatch( new CaptureZoneProgressUpdatedEvent( zone, progress ) );
	}

	public void OnGameEvent( CaptureZoneCapturedEvent capturedEventArgs )
	{
		if ( capturedEventArgs.NewTeam == Team.Player && isOvertime )
		{
			EndOvertime();
		}
	}

	public void OnGameEvent( CharacterDeathEvent eventArgs )
	{
		if ( eventArgs.Victim == null )
		{
			return;
		}

		Log.Info( $"[SingleplayerGameMode] Character {eventArgs.Victim.GameObject.Name} has died!" );

		// ShowInfoMessage( $"Character {eventArgs.Victim.CurrentTeam} has died!", UiInfoFeedType.Debug );
		var zonesWithCharacters = CaptureZones.Where( z => z.IsEntityInZone( eventArgs.Victim ) );
		foreach ( var zone in zonesWithCharacters )
		{
			zone.RemoveDeadCharacterFromZone( eventArgs.Victim );
		}

		if ( eventArgs.Victim == Player )
		{
			if ( isOvertime )
			{
				EndGame();
			}
			else
			{
				timeSincePlayerDeath = 0;
			}
		}
	}

	public void OnGameEvent( DamageEvent eventArgs )
	{
	}

	private void ShowInfoMessage( string message, UiInfoFeedType type )
	{
		GameObject.Dispatch( new UiInfoFeedEvent( message, type ) );
	}
}
