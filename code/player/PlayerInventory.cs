using Sandbox;
using System;

[Category( "Ultraneon" )]


public sealed class PlayerInventory : Component
{
	[Property] public WeaponBaseNeon pistol { get; set; }
	[Property] public WeaponBaseNeon semiauto { get; set; }
	[Property] public WeaponBaseNeon fullauto { get; set; }
	[Property] public WeaponBaseNeon boltrifle { get; set; }

	[Property] public WeaponBaseNeon activeWeapon { get; set; }

	private int SelectedSlot { get; set; }

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( Input.Pressed( "Slot1" )) SelectWeapon( 0 );
		if ( Input.Pressed( "Slot2" ) ) SelectWeapon( 1 );
		if ( Input.Pressed( "Slot3" ) ) SelectWeapon( 2 );
		if ( Input.Pressed( "Slot4" ) ) SelectWeapon( 3 );
		if ( Input.Pressed( "Slot5" ) ) SetActive( null );

		if ( Input.Down( "attack1" ) ) activeWeapon?.Shoot();

		if ( Input.MouseWheel.Length > 0)
		{
			SelectedSlot += (int)Input.MouseWheel.y;
			SelectedSlot = Math.Clamp( SelectedSlot, 0, 3 );
			SelectWeapon( SelectedSlot );
			Log.Info( SelectedSlot );
		}
		
	}


	private void SelectWeapon( int weaponId )
	{

		switch(weaponId)
		{
			case 0:
				SetActive( pistol );
				break;
			case 1:
				SetActive( semiauto );
				break;
			case 2:
				SetActive( fullauto );
				break;
			case 3:
				SetActive( boltrifle );
				break;
		}
	}

	private void SetActive( WeaponBaseNeon weapon )
	{
		if ( weapon is null ) return;

			activeWeapon = weapon;
			activeWeapon?.Equip();
	}

}
