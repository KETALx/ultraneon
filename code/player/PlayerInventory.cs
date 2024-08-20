using Sandbox;
using Sandbox.UI;
using System;
using System.ComponentModel.DataAnnotations;

[Category( "Ultraneon" )]
[Icon( "backpack" )]

public sealed class PlayerInventory : Component
{
	[Property,Sync] public WeaponBaseNeon activeWeapon { get; set; }

	[Property,Sync] public WeaponBaseNeon[] weapons { get; set; } = new WeaponBaseNeon[4];

	[Property,Sync] public int SelectedSlot { get; set; } = 0;


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
			
			SelectedSlot -= delta;
			SelectedSlot = Math.Clamp(SelectedSlot,0,weapons.Length - 1 );
			if ( SelectedSlot == lastSlot ) return;
			

			for ( int i = SelectedSlot; i >=0 && i < weapons.Length;i -= delta )
			{
				if ( weapons[i] != null )
				{
					SelectedSlot = i;
					SetActive(weapons[i]);
					break;
				}
				else
				{
					SelectedSlot = lastSlot;

				}
				
			}
			
		}
	}

	

	public void SetActive( WeaponBaseNeon weapon )
	{
		if ( weapon is null ) return;
		if ( weapon == activeWeapon ) return;

		SelectedSlot = (int)weapon.weaponType;

		activeWeapon = weapon;
		activeWeapon?.Equip();
	}

}
