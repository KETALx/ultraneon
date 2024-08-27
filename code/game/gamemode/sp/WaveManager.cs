using System;
using System.ComponentModel.DataAnnotations;
using Sandbox.Events;
using Ultraneon.Domain.Events;

namespace Ultraneon.Game.GameMode.Sp;

public class WaveManager : Component
{
	[Property]
	[Required]
	public string BotSpawnPoolTag { get; set; } = "botspawnpool";

	[Property]
	public GameObject BotPrefab { get; set; }

	[Property]
	public List<SpawnPoint> BotSpawnPoints { get; set; }

	[Property]
	public int InitialBotCount { get; set; } = 2;

	[Property]
	public float BaseWaveDuration { get; set; } = 60f; // Base duration for 3 enemies

	[Property]
	public int MaxBotsAlive { get; set; } = 30;

	[Property]
	public float EarlyWaveStartDelay { get; set; } = 10f; // Delay before starting next wave early

	private int currentWave = 0;
	private List<BotAi> activeBots = new();
	private TimeSince timeSinceWaveStart;
	private TimeSince timeSinceLastBotKilled;
	private GameObject spawnPool;
	private float currentWaveDuration;
	private bool isWaitingForEarlyStart;
	private int totalEnemiesSpawned = 0;

	protected override void OnStart()
	{
		base.OnStart();
		spawnPool = Scene.GetAllObjects( true ).FirstOrDefault( x => x.Tags.Contains( BotSpawnPoolTag ) );

		if ( spawnPool == null )
		{
			Log.Error( $"[WaveManager] Spawn pool could not be found. Did you add a gameobject with tag '{BotSpawnPoolTag}' to your scene?" );
			return;
		}

		BotSpawnPoints = spawnPool.Children
			.SelectMany( x => x.Components.GetAll<SpawnPoint>() )
			.ToList();

		Log.Info( $"[WaveManager] Found {BotSpawnPoints.Count} bot spawn points" );
	}

	public void StartWaves()
	{
		currentWave = 0;
		StartNextWave();
	}

	private void StartNextWave()
	{
		currentWave++;
		var botsToSpawn = CalculateBotsForWave( currentWave );
		SpawnBots( botsToSpawn );
		currentWaveDuration = CalculateWaveDuration( botsToSpawn );
		timeSinceWaveStart = 0f;
		isWaitingForEarlyStart = false;
		totalEnemiesSpawned += botsToSpawn;

		ShowInfoMessage( $"Wave {currentWave} started! {botsToSpawn} enemies incoming!", UiInfoFeedType.Warning );
		DispatchWaveProgressEvent();
	}

	private int CalculateBotsForWave( int wave )
	{
		return Math.Min( InitialBotCount + (wave - 1) * 2, MaxBotsAlive );
	}

	private float CalculateWaveDuration( int botCount )
	{
		// 60 seconds for 3 bots, 120 seconds for 6 bots
		return BaseWaveDuration * (botCount / 3f);
	}

	private void SpawnBots( int count )
	{
		if ( BotSpawnPoints == null || BotSpawnPoints.Count == 0 )
		{
			Log.Error( "[WaveManager] Failed to find bot spawn points" );
			return;
		}

		for ( int i = 0; i < count; i++ )
		{
			if ( activeBots.Count >= MaxBotsAlive ) break;

			var spawnPoint = BotSpawnPoints[Random.Shared.Int( 0, BotSpawnPoints.Count - 1 )];
			var botObject = BotPrefab.Clone( spawnPoint.Transform.Position, spawnPoint.Transform.Rotation );
			var bot = botObject.Components.Get<BotAi>();
			if ( bot != null )
			{
				bot.CurrentTeam = Team.Enemy;
				activeBots.Add( bot );
			}
		}

		totalEnemiesSpawned += count;
		DispatchWaveProgressEvent();
	}

	protected override void OnUpdate()
	{
		if ( currentWave == 0 ) return;

		UpdateActiveBots();

		if ( timeSinceWaveStart >= currentWaveDuration )
		{
			StartNextWave();
		}
		else if ( activeBots.Count == 0 && !isWaitingForEarlyStart )
		{
			StartEarlyWaveCountdown();
		}
		else if ( isWaitingForEarlyStart && timeSinceLastBotKilled >= EarlyWaveStartDelay )
		{
			StartNextWave();
		}

		DispatchWaveProgressEvent();
	}

	private void UpdateActiveBots()
	{
		for ( int i = activeBots.Count - 1; i >= 0; i-- )
		{
			if ( !activeBots[i].IsValid() || !activeBots[i].IsAlive )
			{
				activeBots.RemoveAt( i );
				timeSinceLastBotKilled = 0;
			}
		}
	}

	private void StartEarlyWaveCountdown()
	{
		isWaitingForEarlyStart = true;
		timeSinceLastBotKilled = 0;
		ShowInfoMessage( $"All enemies defeated! Next wave starting in {EarlyWaveStartDelay} seconds.", UiInfoFeedType.Success );
	}

	public int GetCurrentWave() => currentWave;

	public float GetTimeUntilNextWave()
	{
		if ( isWaitingForEarlyStart )
		{
			return EarlyWaveStartDelay - timeSinceLastBotKilled;
		}

		return currentWaveDuration - timeSinceWaveStart;
	}

	private void ShowInfoMessage( string message, UiInfoFeedType type )
	{
		Scene.Dispatch( new UiInfoFeedEvent( message, type ) );
	}

	private void DispatchWaveProgressEvent()
	{
		Scene.Dispatch( new WaveProgressUpdatedEvent(
			currentWave,
			activeBots.Count,
			totalEnemiesSpawned,
			GetTimeUntilNextWave()
		) );
	}
}
