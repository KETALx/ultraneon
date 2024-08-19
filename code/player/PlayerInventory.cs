using Sandbox;
using Sandbox.UI;
using System;
using System.ComponentModel.DataAnnotations;

[Category( "Ultraneon" )]


public sealed class PlayerInventory : Component
{
	[Property] public WeaponBaseNeon activeWeapon { get; set; }

	[Property] public WeaponBaseNeon[] weapons { get; set; } = new WeaponBaseNeon[4];

	[Property] public int SelectedSlot { get; set; } = 0;


	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();



		if ( Input.Pressed( "Slot1" )) SetActive( weapons[0] );
		if ( Input.Pressed( "Slot2" ) ) SetActive( weapons[1] );
		if ( Input.Pressed( "Slot3" ) ) SetActive(weapons[2]);
		if ( Input.Pressed( "Slot4" ) ) SetActive(weapons[3]);
		if ( Input.Pressed( "Slot5" ) ) SetActive( null );

		if ( Input.Down( "attack1" ) ) activeWeapon?.Shoot();

		if ( Input.MouseWheel.Length != 0)
		{
			var lastSlot = SelectedSlot;
			if (  weapons.Count( x => x != null ) <= 1 ) return;
			

			var delta = (int)Input.MouseWheel.y;
			
			SelectedSlot += delta;
			SelectedSlot = Math.Clamp(SelectedSlot,0,weapons.Length - 1 );
			if ( SelectedSlot == lastSlot ) return;
			

			for ( int i = SelectedSlot; i >=0 && i < weapons.Length;i += delta )
			{
				if ( weapons[i] != null )
				{
					Log.Info( i );
					SelectedSlot = i;
					SetActive(weapons[i]);
					break;
				}
				else
				{
					SelectedSlot = lastSlot;
					Log.Info("else:" + SelectedSlot);
				}
				
			}
			
		}
	}

	/*
	public void SelectWeapon( int weaponId )
	{
		
		
		switch (weaponId)
		{

			case 0:
				SetActive( weapons[0] );
				SelectedSlot = 0;
				break;
			case 1:
				SetActive( weapons[1] );
				SelectedSlot = 1;
				break;
			case 2:
				SetActive( weapons[2] );
				SelectedSlot = 2;
				break;
			case 3:
				SetActive( weapons[3] );
				SelectedSlot = 3;
				break;
		}
	}*/

	public void SetActive( WeaponBaseNeon weapon )
	{
		if ( weapon is null ) return;
		if ( weapon == activeWeapon ) return;

		SelectedSlot = (int)weapon.weaponType;
		Log.Info( SelectedSlot );

		activeWeapon = weapon;
		activeWeapon?.Equip();
	}

}
