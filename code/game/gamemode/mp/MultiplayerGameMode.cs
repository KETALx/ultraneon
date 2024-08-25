using Sandbox.Events;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;

namespace Ultraneon.Game.GameMode.Mp;

public class MultiplayerGameMode : GameMode,
	IGameEventHandler<CaptureZoneCapturedEvent>,
	IGameEventHandler<PlayerSpawnEvent>,
	IGameEventHandler<CharacterDeathEvent>,
	IGameEventHandler<DamageEvent>
{
	public bool isMultiplayer { get; set; }

	/// <summary>
	/// Time of the round, 0 means no round timer
	/// </summary>
	public float RoundTime { get; set; } = 600f; // 10 min

	/// <summary>
	/// 0 means disabled
	/// </summary>
	public int ScoreToWin { get; set; } = 1000;

	[Property]
	public List<CaptureZoneEntity> CaptureZones { get; set; } = new();

	public int ScoreCaptureZone { get; set; } = 100;

	public int ScoreLoseZone { get; set; } = 50;

	private TimeSince roundStartTime;
	private Dictionary<Team, int> teamScores = new();

	private bool gameStarted;
	private bool gamePaused;
	private bool gameEnded;

	public override void Initialize()
	{
		// Initialize capture zones
		CaptureZones = Scene.GetAllComponents<CaptureZoneEntity>().ToList();
		foreach ( var zone in CaptureZones )
		{
			zone.ControllingTeam = Team.Neutral;
			zone.CaptureProgress = 0f;
		}

		roundStartTime = 0f;
		teamScores[Team.Player] = 0;
		teamScores[Team.Enemy] = 0;

		gamePaused = false;
		gameEnded = false;
	}

	public override void Cleanup()
	{
		CaptureZones.RemoveAll( x => true );
		gameStarted = false;
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

	private void SpawnPlayers()
	{
		// TODO: Implement player spawning logic
		GameObject.Dispatch( new PlayerSpawnEvent( Team.Player ) );
		GameObject.Dispatch( new PlayerSpawnEvent( Team.Enemy ) );
	}

	public override void LogicUpdate()
	{
		if ( !gameStarted )
		{
			return;
		}

		if ( roundStartTime >= RoundTime && RoundTime > 0 )
		{
			EndGame(); // TODO
			return;
		}

		UpdateScores();

		if ( teamScores[Team.Player] >= ScoreToWin || teamScores[Team.Enemy] >= ScoreToWin && ScoreToWin > 0 )
		{
			if ( !gameEnded )
			{
				EndGame(); // TODO
			}
		}
	}

	public override void PhysicsUpdate()
	{
		if ( !gameStarted )
		{
			return;
		}
	}

	public override void StartGame()
	{
		SpawnPlayers();
		gameStarted = true;
	}

	public override void EndGame()
	{
		var winningTeam = teamScores[Team.Player] > teamScores[Team.Enemy] ? Team.Player : Team.Enemy;
		Log.Info( $"Game Over! {winningTeam} wins with a score of {teamScores[winningTeam]}" );
		// TODO: Implement game end logic (show results, restart, etc.)
		gameEnded = true;
	}

	public override void PauseGame()
	{
		Scene.TimeScale = 0.0f;
	}

	public override void ResumeGame()
	{
		Scene.TimeScale = 1.0f;
	}

	public void OnGameEvent( DamageEvent eventArgs )
	{
		if ( eventArgs.Target is BaseNeonCharacterEntity targetEntity )
		{
			targetEntity.Health -= eventArgs.Damage;
			Log.Info(
				$"{targetEntity.EntityName} took {eventArgs.Damage} damage from {eventArgs.Attacker?.EntityName ?? "unknown"}. Remaining health: {targetEntity.Health}" );

			if ( targetEntity.Health <= 0 )
			{
				var killerEntity = eventArgs.Attacker?.Components.Get<BaseNeonCharacterEntity>();
				GameObject.Dispatch( new CharacterDeathEvent( targetEntity, killerEntity, true ) );
			}
		}
	}

	public void OnGameEvent( CaptureZoneCapturedEvent capturedEventArgs )
	{
		if ( capturedEventArgs.PreviousTeam != Team.Neutral )
		{
			teamScores[capturedEventArgs.PreviousTeam] += ScoreLoseZone; // Lose points for losing a zone
		}

		if ( capturedEventArgs.NewTeam != Team.Neutral )
		{
			teamScores[capturedEventArgs.PreviousTeam] += ScoreCaptureZone; // Lose points for losing a zone
		}

		Log.Info(
			$"Zone {capturedEventArgs.ZoneName} captured by {capturedEventArgs.NewTeam}. New scores - Player: {teamScores[Team.Player]}, Enemy: {teamScores[Team.Enemy]}" );
	}

	public void OnGameEvent( PlayerSpawnEvent eventArgs )
	{
		// TODO: Implement player spawning logic
		Log.Info( $"Player spawned for team {eventArgs.Team}" );
	}

	public void OnGameEvent( CharacterDeathEvent eventArgs )
	{
		if ( eventArgs.Killer is not BaseNeonCharacterEntity killer )
		{
			return;
		}

		if ( killer.CurrentTeam != eventArgs.Victim.CurrentTeam )
		{
			var scoreAward = eventArgs.IsStylish ? 40 : 10;
			teamScores[killer.CurrentTeam] += scoreAward;
			Log.Info( $"{killer.CurrentTeam} scored {scoreAward} points for a kill" );
		}
	}
}
