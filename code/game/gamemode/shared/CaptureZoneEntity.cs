using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Events;
using Ultraneon;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;
using Ultraneon.Player;

[Category("Ultraneon")]
[Icon("place")]
public sealed class CaptureZoneEntity : Component, Component.ITriggerListener
{
    [Property] public string PointName { get; set; } = "Capture Zone";
    [Property] public Color NeutralColor { get; set; }
    [Property] public Color PlayerColor { get; set; }
    [Property] public Color EnemyColor { get; set; }
    [Property] public float CaptureTime { get; set; } = 15f;
    [Property, HostSync] public Team ControllingTeam { get; set; } = Team.Neutral;
    [Property, ReadOnly, HostSync] public float CaptureProgress { get; set; } = 0f;
    [Property] public ModelRenderer ZoneModel { get; set; }
    [Property] public Action<Team> OnCaptureAction { get; set; }

    public float MinimapX { get; set; }
    public float MinimapY { get; set; }
    public Team PreviousTeam { get; set; }
    public bool HasChanged { get; set; }
    public bool AllowBotCapture { get; set; } = true;

    private TimeSince timeSinceLastCapture;
    private HashSet<BaseNeonCharacterEntity> charactersInZone = new();

    protected override void OnStart()
    {
        base.OnStart();
        timeSinceLastCapture = 0f;
        UpdateZoneVisuals();
    }

    protected override void OnUpdate()
    {
        if (IsProxy) return;

        UpdateCapture();
        UpdateZoneVisuals();
    }

    private void UpdateCapture()
    {
        if (charactersInZone.Any())
        {
            var dominantTeam = charactersInZone
                .GroupBy(p => p.CurrentTeam)
                .OrderByDescending(g => g.Count())
                .First().Key;

            if (dominantTeam != ControllingTeam)
            {
                // Check if it's a bot trying to capture during overtime
                if (!AllowBotCapture && dominantTeam == Team.Enemy)
                {
                    return;
                }

                CaptureProgress += Time.Delta / CaptureTime * charactersInZone.Count(p => p.CurrentTeam == dominantTeam);
                if (CaptureProgress >= 1f)
                {
                    var previousTeam = ControllingTeam;
                    ControllingTeam = dominantTeam;
                    CaptureProgress = 0f;
                    OnPointCaptured(previousTeam);
                }
            }
            else
            {
                CaptureProgress = Math.Max(0f, CaptureProgress - Time.Delta / CaptureTime);
            }

            timeSinceLastCapture = 0f;
        }
        else if (CaptureProgress > 0)
        {
            CaptureProgress -= Time.Delta / CaptureTime;
        }
    }

    private void UpdateZoneVisuals()
    {
        if (ZoneModel != null)
        {
            Color teamColor = ControllingTeam switch
            {
                Team.Player => PlayerColor,
                Team.Enemy => EnemyColor,
                _ => NeutralColor
            };

            ZoneModel.Tint = teamColor;

            // TODO: Add particle effects or other visual indicators for capture progress
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        var character = other.GameObject.Components.Get<BaseNeonCharacterEntity>();
        if (character != null)
        {
            charactersInZone.Add(character);
            if (charactersInZone.Count == 1)
            {
                OnStartCapture();
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        var character = other.GameObject.Components.Get<BaseNeonCharacterEntity>();
        if (character != null)
        {
            charactersInZone.Remove(character);
        }
    }

    private void OnStartCapture()
    {
        Log.Info($"{PointName} is being captured!");
        // TODO: Send a lightwave in radius to alert other players
    }

    private void OnPointCaptured(Team previousTeam)
    {
        Log.Info($"{PointName} has been captured by {ControllingTeam}!");
        PreviousTeam = previousTeam;
        GameObject.Dispatch(new CaptureZoneEvent(PointName, previousTeam, ControllingTeam));

        if (OnCaptureAction != null) OnCaptureAction(previousTeam);
    }

    public bool IsEntityInZone(BaseNeonCharacterEntity entity)
    {
        return charactersInZone.Contains(entity);
    }

    public bool CanCapture(BaseNeonCharacterEntity entity)
    {
        return AllowBotCapture || entity is PlayerNeon;
    }
}
