using System;
using Sandbox.Events;
using Ultraneon.Domain.Events;

namespace Ultraneon.Domain;

[Category( "Ultraneon" )]
[Icon( "settings_accessibility" )]
public class BaseNeonCharacterEntity : Entity, Component.INetworkListener
{
	[Property, ReadOnly]
	public float MaxHealth { get; set; } = 100f;

	[Property, HostSync, Change( nameof(OnHealthChanged) )]
	public float Health { get; set; }

	[Property, ReadOnly]
	public bool IsAlive { get; private set; } = true;

	[Property]
	public Team CurrentTeam { get; set; } = Team.Neutral;

	[Property]
	public GameObject LastAttacker { get; set; }

	public virtual void SetupCharacter()
	{
		Health = MaxHealth;
		IsAlive = true;
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
		SetupCharacter();
	}

	public override void OnDamage( in DamageInfo damage )
	{
		if ( !IsAlive ) return;
		Health = Math.Clamp( Health - damage.Damage, 0f, MaxHealth );
		LastAttacker = damage.Attacker;
		Log.Info( $"{EntityName} took {damage.Damage} damage from {damage.Attacker?.Name ?? "unknown"}. Remaining health: {Health}" );

		Scene.Dispatch( new DamageEvent( this, damage.Attacker?.Components.Get<Entity>(), damage.Damage, damage.Position ) );

		if ( Health <= 0f )
		{
			Die( damage.Attacker );
		}
	}

	protected virtual void OnHealthChanged( float oldValue, float newValue )
	{
		if ( newValue <= 0f && IsAlive )
		{
			Die( LastAttacker );
		}
	}

	protected virtual void Die( GameObject attacker = null )
	{
		if ( !IsAlive ) return;

		IsAlive = false;
		Health = 0f;

		BecomeRagdoll();

		Log.Info( $"[BaseNeonCharacterEntity] {EntityName} has died. Attacker: {attacker?.Name ?? "Unknown"}" );
		Scene.Dispatch( new CharacterDeathEvent( this, attacker?.Components.Get<Entity>(), IsStylishKill( attacker ) ) );
	}

	protected virtual void BecomeRagdoll()
	{
		var collider = GameObject.Components.Get<BoxCollider>();
		if ( collider != null )
		{
			collider.Enabled = false;
		}

		var ragdoll = GameObject.Components.Get<ModelPhysics>( true );
		if ( ragdoll != null )
		{
			ragdoll.Enabled = true;
		}

		GameObject.Tags.Add( "debris" );
	}

	protected virtual bool IsStylishKill( GameObject attacker )
	{
		// TODO: Implement logic for determining if it's a stylish kill (airborne, wallbang)
		return false;
	}
}
