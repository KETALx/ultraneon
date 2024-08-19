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

	[Property, Group( "Weapon stats" )]
	public float reloadTime { get; set; }

	[Property, Group( "Weapon stats" )]
	public bool isReloading { get; set; }

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
		//Log.Info( "equpped" );
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
					if ( inventory.weapons[0] != null ) return;
					inventory.weapons[0] = this;
					addToOwner( inventory );
					break;
				case WeaponType.semi:
					if ( inventory.weapons[1] != null ) return;
					inventory.weapons[1] = this;
					addToOwner( inventory );
					break;
				case WeaponType.auto:
					if ( inventory.weapons[2] != null ) return;
					inventory.weapons[2] = this;
					addToOwner( inventory );
					break;				   
				case WeaponType.bolt:
					if ( inventory.weapons[3] != null ) return;
					inventory.weapons[3] = this;
					addToOwner( inventory );
					break;
			}


		}
	}

	void addToOwner(PlayerInventory inventory)
	{
		this.GameObject.SetParent( inventory.GameObject );
		var mdl = GameObject.Components.Get<ModelRenderer>();
		if ( mdl.IsValid() )
		{
			mdl.Enabled = false;
		}
		isPickedUp = true;
		if ( inventory.weapons.Count( x => x != null ) == 1 )
		{
			inventory.SetActive( this );
		}
	}

}
