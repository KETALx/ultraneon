using System;
using Sandbox;
using Sandbox.Citizen;
using Ultraneon;
using Ultraneon.Domain;
using Ultraneon.Player;

public sealed class BotAi : BaseNeonCharacterEntity
{
    [Property] public NavMeshAgent Agent { get; set; }
    [Property] public CitizenAnimationHelper AnimationHelper { get; set; }
    [Property] public float StopDistance { get; set; } = 100f;
    [Property] public float AttackRange { get; set; } = 200f;

    private PlayerNeon CurrentTarget { get; set; }
    private CaptureZoneEntity TargetZone { get; set; }

    protected override void OnStart()
    {
        base.OnStart();

        if (Agent == null)
        {
            Agent = Components.GetOrCreate<NavMeshAgent>();
        }

        if (AnimationHelper == null)
        {
            AnimationHelper = Components.GetOrCreate<CitizenAnimationHelper>();
        }

        CurrentTeam = Team.Enemy;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if (!isAlive) return;

        if (CurrentTarget != null && !CurrentTarget.isAlive)
        {
            CurrentTarget = null;
        }

        if (TargetZone == null || TargetZone.ControllingTeam == Team.Enemy)
        {
            FindNewTargetZone();
        }

        if (TargetZone != null)
        {
            if (CurrentTarget == null || Random.Shared.NextSingle() < 0.1f) // 10% chance to switch focus to zone
            {
                MoveToZone();
            }
            else
            {
                AttackTarget();
            }
        }
        else if (CurrentTarget != null)
        {
            AttackTarget();
        }
        else
        {
            Agent.Stop();
        }

        UpdateAnimation();
    }

    private void FindNewTargetZone()
    {
        var zones = Scene.GetAllComponents<CaptureZoneEntity>();
        TargetZone = zones.FirstOrDefault(z => z.ControllingTeam != Team.Enemy && z.AllowBotCapture);
    }

    private void MoveToZone()
    {
        if (TargetZone == null) return;

        float distanceToZone = Vector3.DistanceBetween(Transform.Position, TargetZone.Transform.Position);

        if (distanceToZone > StopDistance)
        {
            Agent.MoveTo(TargetZone.Transform.Position);
        }
        else
        {
            Agent.Stop();
            if (TargetZone.AllowBotCapture)
            {
                // Attempt to capture the zone
                TargetZone.OnTriggerEnter(Agent.Components.Get<Collider>());
            }
        }
    }

    private void AttackTarget()
    {
        if (CurrentTarget == null) return;

        float distanceToTarget = Vector3.DistanceBetween(Transform.Position, CurrentTarget.Transform.Position);

        if (distanceToTarget > StopDistance)
        {
            Agent.MoveTo(CurrentTarget.Transform.Position);
        }
        else
        {
            Agent.Stop();

            if (distanceToTarget <= AttackRange)
            {
                // Perform attack
                // TODO: Implement attack logic
                Log.Info($"BotAi {EntityName} is attacking {CurrentTarget.EntityName}");
            }
        }
    }

    private void UpdateAnimation()
    {
        if (AnimationHelper != null)
        {
            AnimationHelper.WithVelocity(Agent.Velocity);

            if (CurrentTarget != null)
            {
                AnimationHelper.WithLook(CurrentTarget.Transform.Position - Transform.Position);
            }
            else if (TargetZone != null)
            {
                AnimationHelper.WithLook(TargetZone.Transform.Position - Transform.Position);
            }
        }
    }

    public void SetTarget(PlayerNeon newTarget)
    {
        if (newTarget != null && newTarget != CurrentTarget && newTarget.isAlive)
        {
            CurrentTarget = newTarget;
            Log.Info($"BotAi {EntityName} is now targeting {CurrentTarget.EntityName}");
        }
    }
}
