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
			switch( weaponType )
			{
				case WeaponType.pistol:
					if(inventory.pistol == null ) inventory.pistol = this;
					break;
			}
			switch ( weaponType )
			{
				case WeaponType.semi:
					inventory.semiauto = this;
					break;
			}
			switch ( weaponType )
			{
				case WeaponType.auto:
					inventory.fullauto = this;
					break;
			}
			switch ( weaponType )
			{
				case WeaponType.bolt:
					inventory.boltrifle = this;
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
