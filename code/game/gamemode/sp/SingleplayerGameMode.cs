using System;
using Sandbox;
using Sandbox.Events;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;
using Ultraneon.Player;

namespace Ultraneon.Game.GameMode.Sp;

public class SingleplayerGameMode : GameMode,
    ICaptureZoneGameMode,
    IGameEventHandler<CaptureZoneCapturedEvent>,
    IGameEventHandler<PlayerSpawnEvent>,
    IGameEventHandler<CharacterDeathEvent>,
    IGameEventHandler<DamageEvent>
{
    [Property] public float CaptureTime { get; set; } = 15f;
    [Property] public float OvertimeSeconds { get; set; } = 60f;
    [Property] public List<CaptureZoneEntity> CaptureZones { get; set; } = new();
    [Property] public GameObject PlayerPrefab { get; set; }
    [Property] public WaveManager WaveManager { get; set; }

    private PlayerNeon player;
    private bool gameStarted;
    private bool warmupPhase = true;
    private TimeSince overtimeStarted;
    private bool isOvertime;

    public override void Initialize()
    {
        try
        {
            CaptureZones = Scene.GetAllComponents<CaptureZoneEntity>().ToList();
            foreach (var zone in CaptureZones)
            {
                zone.ControllingTeam = Team.Neutral;
                zone.CaptureProgress = 0f;
                zone.GameMode = this;
            }

            gameStarted = false;
            warmupPhase = true;
            isOvertime = false;
            PauseGame();
            Log.Info("[SinglePlayerGameMode] SingleplayerGameMode initialized");
        }
        catch (Exception ex)
        {
            Log.Error($"[SinglePlayerGameMode] Error in Initialize: {ex.Message}");
        }
    }

    public override void Cleanup()
    {
        CaptureZones.Clear();
        gameStarted = false;
        Log.Info("[SinglePlayerGameMode] SingleplayerGameMode cleaned up");
    }

    public override void LogicUpdate()
    {
        try
        {
            if (!gameStarted)
            {
                Log.Warning("[SingleplayerGameMode] LogicUpdate called but game not started");
                return;
            }

            if (warmupPhase)
            {
                CheckWarmupEnd();
            }
            else
            {
                UpdateGameState();
                CheckGameOver();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[SinglePlayerGameMode] Error in LogicUpdate: {ex.Message}");
        }
    }

    public override void PhysicsUpdate() { }

    public override void PauseGame()
    {
        Log.Info("[SinglePlayerGameMode] Paused Game");
        Scene.TimeScale = 0.0f;
        // TODO: Show pause menu
    }

    public override void ResumeGame()
    {
        Log.Info("[SinglePlayerGameMode] Resumed Game");
        Scene.TimeScale = 1.0f;
        // TODO: Hide pause menu
    }

    public override void StartGame()
    {
        SpawnPlayer();
        gameStarted = true;
        warmupPhase = true;
        ResumeGame();
        ShowInfoMessage("Capture the zone to start the game!", UiInfoFeedType.Normal);
        Log.Info("[SinglePlayerGameMode] Let there be light!");
    }

    public override void EndGame()
    {
        ShowInfoMessage("Game over!", UiInfoFeedType.Warning);
        gameStarted = false;
        int maxWave = WaveManager?.GetCurrentWave() ?? 0;
        GameObject.Dispatch(new GameOverEvent(maxWave));
    }

    private void SpawnPlayer()
    {
        Log.Info("[SingleplayerGameMode] Attempting to spawn player");
        var spawnPoint = Scene.GetAllComponents<SpawnPoint>().FirstOrDefault(s => s.Tags.Contains("player") || s.GameObject.Name.Equals("info_player_start"));
        if (spawnPoint != null && PlayerPrefab != null)
        {
            var playerObject = PlayerPrefab.Clone(spawnPoint.Transform.Position, spawnPoint.Transform.Rotation);
            player = playerObject.Components.Get<PlayerNeon>();
            if (player != null)
            {
                Log.Info($"[SingleplayerGameMode] Player spawned at {spawnPoint.Transform.Position}");
                GameObject.Dispatch(new PlayerSpawnEvent(Team.Player));
            }
            else
            {
                Log.Error("[SingleplayerGameMode] Failed to get PlayerNeon component from spawned player object");
            }
        }
        else
        {
            Log.Error("[SingleplayerGameMode] Failed to spawn player: No valid spawn point or player prefab");
        }
    }

    private void CheckWarmupEnd()
    {
        if (CaptureZones.Any(z => z.ControllingTeam == Team.Player))
        {
            warmupPhase = false;
            WaveManager?.StartWaves();
            ShowInfoMessage("Zone captured! Prepare for incoming waves!", UiInfoFeedType.Success);
        }
    }

    private void UpdateGameState()
    {
        bool allZonesEnemyControlled = CaptureZones.All(z => z.ControllingTeam == Team.Enemy);

        if (allZonesEnemyControlled && !isOvertime)
        {
            StartOvertime();
        }
        else if (!allZonesEnemyControlled && isOvertime)
        {
            EndOvertime();
        }

        if (isOvertime)
        {
            UpdateOvertime();
        }
    }

    private void StartOvertime()
    {
        isOvertime = true;
        overtimeStarted = 0f;
        player?.EnterOvertime();
        ShowInfoMessage($"OVERTIME! Recapture the zone in {OvertimeSeconds} seconds or the game is over!", UiInfoFeedType.Warning);

        foreach (var zone in CaptureZones)
        {
            zone.AllowBotCapture = false;
        }
        Log.Info("[SinglePlayerGameMode] Overtime started");
    }

    private void EndOvertime()
    {
        isOvertime = false;
        player?.ExitOvertime();
        ShowInfoMessage("Zone recaptured! Continue defending!", UiInfoFeedType.Success);

        foreach (var zone in CaptureZones)
        {
            zone.AllowBotCapture = true;
        }
        Log.Info("[SinglePlayerGameMode] Overtime ended");
    }

    private void UpdateOvertime()
    {
        float remainingTime = OvertimeSeconds - overtimeStarted;
        if (remainingTime <= 0)
        {
            EndGame();
        }
        else if (remainingTime <= 10 && (int)remainingTime != (int)(remainingTime - Time.Delta))
        {
            ShowInfoMessage($"Overtime ending in {(int)remainingTime} seconds!", UiInfoFeedType.Warning);
        }
    }

    private void CheckGameOver()
    {
        if (isOvertime && (player?.IsDead ?? false))
        {
            EndGame();
        }
    }

    public void OnCaptureStarted(CaptureZoneEntity zone)
    {
        ShowInfoMessage($"Capture of {zone.PointName} has started!", UiInfoFeedType.Debug);
    }

    public void OnCaptureStopped(CaptureZoneEntity zone)
    {
        ShowInfoMessage($"Capture of {zone.PointName} has stopped!", UiInfoFeedType.Debug);
    }

    public void OnPointCaptured(CaptureZoneEntity zone, Team previousTeam, Team newTeam)
    {
        ShowInfoMessage($"{zone.PointName} has been captured by {newTeam}!", UiInfoFeedType.Success);

        if (newTeam == Team.Player && isOvertime)
        {
            EndOvertime();
        }
    }

    public void OnCaptureProgressUpdated(CaptureZoneEntity zone, float progress)
    {
	    // TODO: Update radar with pulse
    }

    public void OnGameEvent(CaptureZoneCapturedEvent capturedEventArgs)
    {
        if (capturedEventArgs.NewTeam == Team.Player && isOvertime)
        {
            EndOvertime();
        }
    }

    public void OnGameEvent(PlayerSpawnEvent eventArgs)
    {
        ShowInfoMessage($"Player spawned for team {eventArgs.Team}", UiInfoFeedType.Debug);
    }

    public void OnGameEvent(CharacterDeathEvent eventArgs)
    {
        if (eventArgs.Victim == player)
        {
            if (isOvertime)
            {
                EndGame();
            }
            else
            {
                SpawnPlayer();
            }
        }
    }

    public void OnGameEvent(DamageEvent eventArgs)
    {
        if (eventArgs.Target is BaseNeonCharacterEntity targetEntity)
        {
            if (targetEntity.Health <= 0)
            {
                var killerEntity = eventArgs.Attacker?.Components.Get<BaseNeonCharacterEntity>();
                GameObject.Dispatch(new CharacterDeathEvent(targetEntity, killerEntity, IsStylishKill(killerEntity, targetEntity)));
            }
        }
    }

    private bool IsStylishKill(BaseNeonCharacterEntity killer, BaseNeonCharacterEntity victim)
    {
        // TODO: Implement logic for determining if it's a stylish kill (airborne, wallbang)
        return false;
    }

    private void ShowInfoMessage(string message, UiInfoFeedType type)
    {
        GameObject.Dispatch(new UiInfoFeedEvent(message, type));
    }
}
