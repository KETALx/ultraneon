using System;
using Sandbox.Events;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;
using Ultraneon.Services;

namespace Ultraneon.Player;

public class PlayerNeon : BaseNeonCharacterEntity, IGameEventHandler<CharacterSpawnEvent>, IGameEventHandler<CharacterDeathEvent>
{
	[RequireComponent]
	public PlayerInventory Inventory { get; private set; }

	[Property]
	public CameraComponent MainCamera { get; private set; }

	[Property]
	public CameraComponent DeathCamera { get; private set; }

	public override void SetupCharacter()
	{
		base.SetupCharacter();
		CurrentTeam = Team.Player;
		Inventory = Components.Get<PlayerInventory>();

		MainCamera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.Tags.Contains( "maincamera" ) );
		DeathCamera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.Tags.Contains( "deathcamera" ) );
		
		Log.Info( $"[PlayerNeon] Player initialized. Health: {Health}, Team: {CurrentTeam}" );
	}

	public void OnGameEvent( CharacterSpawnEvent eventArgs )
	{
		if ( eventArgs.character != this )
		{
			return;
		}

		Transform.Position = eventArgs.spawnPosition;
		SetupCharacter();
		EnableControls();
		Log.Info( $"PlayerNeon received CharacterSpawnEvent with args {eventArgs.character.GameObject.Name} " );
	}

	public void OnGameEvent( CharacterDeathEvent eventArgs )
	{
		if ( eventArgs.Victim != this )
		{
			return;
		}

		DisableControls();
	}

	public void DisableControls()
	{
		Log.Info( "[PlayerNeon] Player controls disabled" );

		if ( MainCamera == null || DeathCamera == null ) { return; }

		MainCamera.Priority = 1;
		DeathCamera.Priority = 2;
	}

	protected override void BecomeRagdoll()
	{
	}

	public void EnableControls()
	{
		Log.Info( "[PlayerNeon] Player controls enabled" );
		if ( MainCamera == null || DeathCamera == null ) { return; }

		MainCamera.Priority = 2;
		DeathCamera.Priority = 1;
	}
}
