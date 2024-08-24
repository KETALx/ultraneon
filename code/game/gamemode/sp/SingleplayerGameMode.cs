using Sandbox.Events;
using System;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;
using Ultraneon.Player;

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
	public float HealInterval { get; set; } = 5f;

	[Property]
	public float HealAmount { get; set; } = 10f;

	[Property]
	public float RespawnDelay { get; set; } = 3f;

	[Property]
	public int ScoreLoseZone { get; set; } = 50;

	[Property]
	public int ScoreCaptureZone { get; set; } = 100;

	[Property]
	public int ScoreToWin { get; set; } = 1000;

	[Property]
	public List<CaptureZoneEntity> CaptureZones { get; set; } = new();

	[Property]
	public GameObject PlayerPrefab { get; set; }

	[Property]
	public GameObject BotPrefab { get; set; }

	[Property]
	public int MaxBots { get; set; } = 10;

	private PlayerNeon player;
	private List<BotAi> bots = new();
	private Dictionary<Team, int> teamScores = new();
	private Dictionary<BaseNeonCharacterEntity, TimeSince> lastHealTimes = new();
	private bool gameStarted;
	private bool gameEnded;

	public override void Initialize()
	{
		CaptureZones = Scene.GetAllComponents<CaptureZoneEntity>().ToList();
		foreach ( var zone in CaptureZones )
		{
			zone.ControllingTeam = Team.Neutral;
			zone.CaptureProgress = 0f;
		}

		teamScores[Team.Player] = 0;
		teamScores[Team.Enemy] = 0;
		gameEnded = false;
	}

	public override void Cleanup()
	{
		CaptureZones.Clear();
		bots.Clear();
		gameStarted = false;
	}

	public override void LogicUpdate()
	{
		if ( !gameStarted || gameEnded ) return;

		UpdateScores();
		UpdateHealing();
		SpawnBots();

		if ( teamScores[Team.Player] >= ScoreToWin )
		{
			EndGame( true );
		}
		else if ( teamScores[Team.Enemy] >= ScoreToWin )
		{
			EndGame( false );
		}
	}

	public override void PhysicsUpdate()
	{
	}

	public override void StartGame()
	{
		SpawnPlayer();
		gameStarted = true;
		GameObject.Dispatch( new GameModeActivatedEvent( this ) );
	}

	public override void EndGame()
	{
	}

	private void EndGame( bool playerWon )
	{
		gameEnded = true;
		Log.Info( $"Game Over! {(playerWon ? "Player won!" : "Enemies won!")}" );
		// TODO: Implement game end logic (show results, restart, etc.)
	}

	public override void PauseGame()
	{
		Scene.TimeScale = 0.0f;
	}

	public override void ResumeGame()
	{
		Scene.TimeScale = 1.0f;
	}

	private void UpdateScores()
	{
		foreach ( var zone in CaptureZones )
		{
			if ( zone.ControllingTeam != Team.Neutral )
			{
				teamScores[zone.ControllingTeam]++;
			}
		}
	}

	private void UpdateHealing()
	{
		foreach ( var character in lastHealTimes.Keys.ToList() )
		{
			if ( character.isAlive && IsInCapturedZone( character ) && lastHealTimes[character] >= HealInterval )
			{
				character.Health = Math.Min( character.Health + HealAmount, character.MaxHealth );
				lastHealTimes[character] = 0f;
			}
		}
	}

	private bool IsInCapturedZone( BaseNeonCharacterEntity character )
	{
		return CaptureZones.Any( zone => zone.ControllingTeam == character.CurrentTeam && zone.IsPlayerInZone( (PlayerNeon)character ) );
	}

	private void SpawnPlayer()
	{
		var spawnPoint = Scene.GetAllComponents<SpawnPoint>().FirstOrDefault();
		if ( spawnPoint != null && PlayerPrefab != null )
		{
			var playerObject = PlayerPrefab.Clone( spawnPoint.Transform.Position, spawnPoint.Transform.Rotation );
			player = playerObject.Components.Get<PlayerNeon>();
			if ( player != null )
			{
				lastHealTimes[player] = 0f;
				GameObject.Dispatch( new PlayerSpawnEvent( Team.Player ) );
			}
		}
	}

	private void SpawnBots()
	{
		if ( bots.Count < MaxBots )
		{
			var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToList();
			var spawnPoint = spawnPoints[Random.Shared.Int( 0, spawnPoints.Count - 1 )];
			if ( spawnPoint != null && BotPrefab != null )
			{
				var botObject = BotPrefab.Clone( spawnPoint.Transform.Position, spawnPoint.Transform.Rotation );
				var bot = botObject.Components.Get<BotAi>();
				if ( bot != null )
				{
					bot.CurrentTeam = Team.Enemy;
					bots.Add( bot );
					lastHealTimes[bot] = 0f;
				}
			}
		}
	}

	private void RespawnCharacter( BaseNeonCharacterEntity character )
	{
		CaptureZoneEntity spawnZone = null;
		if ( character.CurrentTeam == Team.Player )
		{
			spawnZone = CaptureZones.FirstOrDefault( z => z.ControllingTeam == Team.Player );
		}
		else
		{
			spawnZone = CaptureZones.FirstOrDefault( z => z.ControllingTeam == Team.Enemy );
		}

		if ( spawnZone != null )
		{
			// Create a beam effect and clear the area
			CreateRespawnBeamEffect( spawnZone.Transform.Position );
			ClearAreaAroundSpawnPoint( spawnZone.Transform.Position );

			// Respawn the character
			character.Transform.Position = spawnZone.Transform.Position;
			character.Health = character.MaxHealth;
			// Remove the assignment to isAlive as it's not accessible
		}
		else if ( character is PlayerNeon )
		{
			// Player has no captured zones, game over
			EndGame( false );
		}
		else
		{
			// Bot has no captured zones, remove it from the game
			bots.Remove( character as BotAi );
			character.GameObject.Destroy();
		}
	}

	private void CreateRespawnBeamEffect( Vector3 position )
	{
		// TODO: Implement respawn beam effect
	}

	private void ClearAreaAroundSpawnPoint( Vector3 position )
	{
		// TODO: Implement area clearing logic
	}

	public void OnGameEvent( CaptureZoneEvent eventArgs )
	{
		if ( eventArgs.PreviousTeam != Team.Neutral )
		{
			teamScores[eventArgs.PreviousTeam] -= ScoreLoseZone;
		}

		if ( eventArgs.NewTeam != Team.Neutral )
		{
			teamScores[eventArgs.NewTeam] += ScoreCaptureZone;
		}

		Log.Info( $"Zone {eventArgs.ZoneName} captured by {eventArgs.NewTeam}. Scores - Player: {teamScores[Team.Player]}, Enemy: {teamScores[Team.Enemy]}" );
	}

	public void OnGameEvent( PlayerSpawnEvent eventArgs )
	{
		Log.Info( $"Player spawned for team {eventArgs.Team}" );
	}

	public void OnGameEvent( CharacterDeathEvent eventArgs )
	{
		if ( eventArgs.Killer is BaseNeonCharacterEntity killer )
		{
			var scoreAward = eventArgs.IsStylish ? 40 : 10;
			teamScores[killer.CurrentTeam] += scoreAward;
			Log.Info( $"{killer.CurrentTeam} scored {scoreAward} points for a kill" );
		}

		if ( eventArgs.Victim is PlayerNeon || eventArgs.Victim is BotAi )
		{
			Task.Delay( TimeSpan.FromSeconds( RespawnDelay ).Milliseconds )
				.ContinueWith( _ => RespawnCharacter( eventArgs.Victim ) );
		}
	}

	public void OnGameEvent( DamageEvent eventArgs )
	{
		if ( eventArgs.Target is BaseNeonCharacterEntity targetEntity )
		{
			Log.Info( $"{targetEntity.EntityName} took {eventArgs.Damage} damage from {eventArgs.Attacker?.EntityName ?? "unknown"}. Remaining health: {targetEntity.Health}" );

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
}
