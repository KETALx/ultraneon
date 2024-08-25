using System;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Events;
using Ultraneon;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;
using Ultraneon.Player;

public sealed class BotAi : BaseNeonCharacterEntity
{
    [Property]
    public NavMeshAgent Agent { get; set; }

    [Property]
    public CitizenAnimationHelper AnimationHelper { get; set; }

    [Property]
    public float StopDistance { get; set; } = 100f;

    [Property]
    public float AttackRange { get; set; } = 200f;

    [Property]
    public float AttackRate { get; set; } = 1.0f;

    [Property]
    public float AttackDamage { get; set; } = 10.0f;

    [Property]
    public float RagdollDespawnTime { get; set; } = 5.0f;

    private PlayerNeon CurrentTarget { get; set; }
    private CaptureZoneEntity TargetZone { get; set; }

    private bool isAttacking = false;
    private float timeSinceAttackStart = 0.0f;
    private bool isDead = false;
    private RealTimeSince timeSinceDeath;

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

        if (isDead)
        {
            if (timeSinceDeath >= RagdollDespawnTime)
            {
                GameObject.Destroy();
            }
            return;
        }

        if (!isAlive)
        {
            Die();
            return;
        }

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

    public override void OnDamage(in DamageInfo info)
    {
        base.OnDamage(info);

        if (Health <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        timeSinceDeath = 0;

        // Disable NavMeshAgent and other components
        if (Agent != null) Agent.Enabled = false;
        if (AnimationHelper != null) AnimationHelper.Enabled = false;

        // FIXME: Ragdoll goes through map
        var ragdollPhysics = GameObject.Components.Get<ModelPhysics>();
        if (ragdollPhysics != null)
        {
	        ragdollPhysics.Enabled = true;
        }
        GameObject.Tags.Add("debris");

        // Notify about bot death
        GameObject.Dispatch(new CharacterDeathEvent(this, null, false));
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

        if (distanceToTarget > AttackRange)
        {
            Agent.MoveTo(CurrentTarget.Transform.Position);
        }
        else
        {
            Agent.Stop();

            if (timeSinceAttackStart >= AttackRate)
            {
                Log.Info($"BotAi {EntityName} is attacking {CurrentTarget.EntityName}");
                isAttacking = true;
                timeSinceAttackStart = 0.0f;

                var damageInfo = new DamageInfo { Damage = AttackDamage, Attacker = GameObject, Position = CurrentTarget.Transform.Position };
                CurrentTarget.OnDamage(damageInfo);
            }
        }

        timeSinceAttackStart += Time.Delta;
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
