using System;
using System.ComponentModel.DataAnnotations;
using Sandbox.Events;
using Ultraneon.Domain.Events;

namespace Ultraneon.Game.GameMode.Sp;

public class WaveManager : Component
{
	/// <summary>
	/// A gameobject with this tag and SpawnPoint objects under it must be in the scene for the WaveManager to function.
	/// </summary>
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
	public float WaveDuration { get; set; } = 60f;

	[Property]
	public int MaxBotsAlive { get; set; } = 30;

	private int currentWave = 0;
	private List<BotAi> activeBots = new();
	private TimeSince timeSinceWaveStart;
	private GameObject spawnPool;

	protected override void OnStart()
	{
		base.OnStart();
		spawnPool = Scene.GetAllObjects( true ).FirstOrDefault( x => x.Tags.Contains( "botspawnpool" ) );

		if ( spawnPool == null )
		{
			Log.Error( "[WaveManager] Spawn pool could not be found. Did you add a gameobject with tag 'botspawnpool' to your scene?" );
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
		timeSinceWaveStart = 0f;

		ShowInfoMessage( $"Wave {currentWave} started! {botsToSpawn} enemies incoming!", UiInfoFeedType.Warning );
	}

	private int CalculateBotsForWave( int wave )
	{
		return Math.Min( InitialBotCount + (wave - 1) * 2, MaxBotsAlive );
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
				ShowInfoMessage( $"Bot Spawned!", UiInfoFeedType.Debug );
				activeBots.Add( bot );
			}
		}
	}

	protected override void OnUpdate()
	{
		if ( currentWave == 0 ) return;

		activeBots.RemoveAll( bot => !bot.IsValid() || !bot.isAlive );

		if ( timeSinceWaveStart >= WaveDuration )
		{
			StartNextWave();
		}
	}

	public int GetCurrentWave() => currentWave;

	private void ShowInfoMessage( string message, UiInfoFeedType type )
	{
		GameObject.Dispatch( new UiInfoFeedEvent( message, type ) );
	}
}
