using System;
using Sandbox.Events;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;

namespace Ultraneon.Player;

public class PlayerNeon : BaseNeonCharacterEntity
{
	[Property]
	public float RespawnDelay { get; set; } = 3f;

	private TimeSince TimeSinceDeath { get; set; }

	[RequireComponent]
	public PlayerInventory Inventory { get; private set; }

	public bool IsDead => Health <= 0;
	public bool IsInOvertime { get; private set; }

	protected override void OnStart()
	{
		base.OnStart();
		if ( IsProxy ) return;

		CurrentTeam = Team.Player;
		Health = MaxHealth;
		Inventory = Components.Get<PlayerInventory>();
		IsInOvertime = false;
		Log.Info($"[PlayerNeon] Player initialized. Health: {Health}, Team: {CurrentTeam}");
	}

	protected override void OnUpdate()
	{
		if ( IsProxy || !IsDead ) return;

		if ( !IsInOvertime && TimeSinceDeath >= RespawnDelay )
		{
			Respawn();
		}
	}

	public override void OnDamage( in DamageInfo info )
	{
		base.OnDamage( info );

		if ( IsProxy || IsDead ) return;

		if ( info.Attacker.Components.Get<Entity>() is { } entity )
		{
			GameObject.Dispatch( new DamageEvent( this, entity, info.Damage, info.Position ) );
		}

		if ( Health <= 0 )
		{
			Die( info.Attacker.Components.Get<BaseNeonCharacterEntity>() );
		}
	}

	private void Die( BaseNeonCharacterEntity killer )
	{
		if ( IsProxy ) return;

		Health = 0;
		TimeSinceDeath = 0;

		GameObject.Dispatch( new CharacterDeathEvent( this, killer ) );

		DisableControls();

		if ( IsInOvertime )
		{
			GameObject.Dispatch( new GameOverEvent( 0 ) ); // TODO: Pass the correct max wave reached
		}
	}

	private void Respawn()
	{
		if ( IsProxy ) return;

		Health = MaxHealth;
		var spawnPoint = Scene.GetAllComponents<SpawnPoint>().OrderBy( _ => Random.Shared.Next() ).FirstOrDefault();
		if ( spawnPoint != null )
		{
			Transform.Position = spawnPoint.Transform.Position;
			Transform.Rotation = spawnPoint.Transform.Rotation;
		}

		EnableControls();

		GameObject.Dispatch( new PlayerSpawnEvent( Team.Player ) );
	}

	private bool IsStylishKill( BaseNeonCharacterEntity killer )
	{
		// TODO: Implement logic for determining if it's a stylish kill (airborne, wallbang)
		return false;
	}

	private void DisableControls()
	{
		// TODO: Implement disabling player controls
	}

	
	private void EnableControls()
	{
		// TODO: Implement enabling player controls
		Log.Info("[PlayerNeon] Player controls enabled");
	}

	public void EnterOvertime()
	{
		IsInOvertime = true;
		// TODO: Implement any player-specific overtime behavior
	}

	public void ExitOvertime()
	{
		IsInOvertime = false;
		// TODO: Implement any player-specific behavior when exiting overtime
	}
}
