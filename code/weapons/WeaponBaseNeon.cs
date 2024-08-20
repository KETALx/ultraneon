using Sandbox;
using Sandbox.Citizen;
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


	[Property,ReadOnly]
	public bool isPickedUp { get; set; } = false;

	public TimeSince sinceEquippd { get; set; } = 0f;
	public TimeSince sinceShot { get; set; } = 0f;
	bool hasShoot { get; set; } = false;

	#region weapon stats
	[Property, Group( "Weapon stats" )]
	public WeaponType weaponType { get; set; }

	[Property,Group("Weapon stats")]
	public int clipSize { get; set; }

	[Property, Group( "Weapon stats" )]
	public float fireRate { get; set; }

	[Property, Group( "Weapon stats" )]
	public float equipTime { get; set; }

	[Property, Group( "Weapon stats" )]
	public float reloadTime { get; set; }

	[Property, Group( "Weapon stats" )]
	public bool isReloading { get; set; }
	[Property, Group( "Weapon stats" )]
	public bool isSemiAuto { get; set; }
	#endregion

	#region weapon damage
	[Property, Group( "Weapon damage" )]
	public float weaponDamage { get; set; }

	[Property, Range( 1f, 10f, 0.1f ), Group( "Weapon damage" )]
	public float headShotMultiplier { get; set; }

	#endregion


	[Property, Group( "Weapon effects" )]
	public SoundEvent shootSound { get; set; }

	[Property, Group( "Weapon effects" )]
	public GameObject ImpactPrefab { get; set; }




	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( !Input.Down( "attack1" ) && hasShoot )
		{
			hasShoot = false;
		}
	}
	public void Shoot()
	{
		if ( IsProxy ) return;
		if ( sinceEquippd < equipTime ) return;
		if ( sinceShot < fireRate ) return;
		if(hasShoot && isSemiAuto) return;

		Sound.Play( shootSound );
		hasShoot= true;
		var camera = Scene.GetAllComponents<CameraComponent>().Where( x => x.IsMainCamera ).FirstOrDefault();
		if ( camera is null ) return;
		var rayStart = camera.Transform.Position;
		var shotTrace = Scene.Trace.Ray( rayStart, rayStart + camera.Transform.World.Forward * 65536f )
			.IgnoreGameObjectHierarchy( GameObject.Parent )
			.UseHitboxes()
			.Run();
		if ( shotTrace.Hit )
		{

			//GameObject impact = ImpactPrefab.Clone( shotTrace.EndPosition, Rotation.LookAt( -shotTrace.Normal ) );

			if ( shotTrace.GameObject.Components.Get<Entity>() == null ) return;
			var totalDamage = weaponDamage;
			//if ( shotTrace.Hitbox.Bone.Name == "head" ) totalDamage *= headShotMultiplier;
			var dmg = shotTrace.GameObject.Components.Get<IDamageable>();
			if ( dmg != null )
			{

				dmg.OnDamage( new DamageInfo()
				{
					Damage = totalDamage,
					Attacker = GameObject.Parent,
					Position = Transform.Position,
					
				} );
				Log.Info( totalDamage);
			}

		}


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
