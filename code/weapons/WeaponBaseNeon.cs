using Sandbox;
using Sandbox.Diagnostics;
using System;

public enum WeaponType
{
	pistol,
	semi,
	auto,
	bolt
}

public sealed class WeaponBaseNeon : Component, Component.ITriggerListener
{
	[Property]
	public WeaponType weaponType { get; set; }

	[Property]
	public bool isPickedUp { get; set; } = false;

	[Property,Group("Weapon stats")]
	public int clipSize { get; set; }
	[Property, Group( "Weapon stats" )]
	public float fireRate { get; set; }
	[Property, Group( "Weapon stats" )]
	public float dryTime { get; set; }

	[Property] public TimeSince sinceEquippd { get; set; } = 0f;
	[Property] public TimeSince sinceShot { get; set; } = 0f;
	bool canShoot { get; set; } = false;


	public void Shoot()
	{
		if ( sinceEquippd < dryTime ) return;
		if ( sinceShot < fireRate ) return;

			canShoot = true;
			Log.Info( "shot" );

		sinceShot = 0f;
	}

	public void Holster()
	{

	}

	public void Equip()
	{
		sinceEquippd = 0f;
		Log.Info( "equpped" );
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( isPickedUp ) return;
		var inventory = other.GameObject.Components.Get<PlayerInventory>();
		if ( inventory.IsValid() )
		{
			switch ( weaponType )
			{
				case WeaponType.pistol:
					inventory.weapons[0] = GameObject;
					break;
				case WeaponType.semi:
					inventory.weapons[1] = GameObject;
					break;
				case WeaponType.auto:
					inventory.weapons[2] = GameObject;
					break;
				case WeaponType.bolt:
					inventory.weapons[3] = GameObject;
					break;
			}

			this.GameObject.SetParent( inventory.GameObject );
			var mdl = GameObject.Components.Get<ModelRenderer>();
			if ( mdl.IsValid() )
			{
				mdl.Enabled = false;
			}
			isPickedUp = true;
		}
	}


}
