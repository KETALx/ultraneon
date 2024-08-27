using Sandbox.Events;
using System.Runtime.CompilerServices;
using System.Transactions;
using Ultraneon.Domain.Events;
using Ultraneon.Player;

namespace Ultraneon
{
	public enum WeaponType
	{
		Auto,
		Bolt,
		Pistol,
		Semi,
	}

	public sealed class WeaponBaseNeon : Component, Component.ITriggerListener
	{
		[Property, ReadOnly]
		public bool IsPickedUp { get; private set; }

		public TimeSince SinceEquipped { get; private set; }

		public TimeSince SinceShot { get; private set; }
		public TimeSince TimeInReload { get; private set; }

		[Property, Group( "Viewmodel" )]
		public SkinnedModelRenderer Viewmodel { get; set; }

		[Property, Group( "Viewmodel" )]
		public SkinnedModelRenderer Worldmodel { get; set; }

		[Property, Group( "Current Ammo" )]
		public int CurrentAmmo;

		[Property, Group( "Weapon Stats" )]
		public WeaponType WeaponType { get; set; }

		[Property, Group( "Weapon Stats" )]
		public int ClipSize { get; set; }

		[Property, Group( "Weapon Stats" )]
		public float FireRate { get; set; }

		[Property, Group( "Weapon Stats" )]
		public float EquipTime { get; set; }

		[Property, Group( "Weapon Stats" )]
		public float ReloadTime { get; set; }

		[Property, Group( "Weapon Stats" )]
		public bool IsSemiAuto { get; set; }

		[Property, Group( "Weapon Damage" )]
		public float WeaponDamage { get; set; }

		[Property, Range( 1f, 10f, 0.1f ), Group( "Weapon Damage" )]
		public float HeadshotMultiplier { get; set; } = 1.5f;

		[Property, Group( "Weapon Effects" )]
		public SoundEvent ShootSound { get; set; }

		[Property, Group( "Weapon Effects" )]
		public GameObject ImpactPrefab { get; set; }

		[Property, Group( "Weapon Effects" )]
		public GameObject MuzzleObject { get; set; }

		[Property, Group( "Weapon Effects" )]
		public GameObject TrailObject { get; set; }

		public GameObject Owner { get; private set; }

		public int WeaponSlot { get; private set; }

		private bool _hasShot;
		public bool IsReloading { get; private set; }

		protected override void OnStart()
		{
			base.OnStart();
			CurrentAmmo = ClipSize;
		}


		protected override void OnUpdate()
		{
			base.OnUpdate();

			if ( IsPickedUp )
			{
				UpdatePosition();
			}

			if ( !Input.Down( "attack1" ) )
			{
				_hasShot = false;
			}

			if ( IsReloading )
			{
				UpdateReload();
			}
		}

		private void UpdateReload()
		{
			if ( TimeInReload >= ReloadTime )
			{
				CompleteReload();
			}
		}


		private void UpdatePosition()
		{
			var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );
			if ( camera != null )
			{
				Transform.Position = camera.Transform.Position;
				Transform.Rotation = camera.Transform.Rotation;
				//Transform.Position = Transform.Position.LerpTo( camera.Transform.Position, 0.025f );
				//Transform.Rotation = camera.Transform.Rotation.Clamp( camera.Transform.Rotation, 5f );
			}
		}

		public void Shoot()
		{
			if ( IsProxy || !CanShoot() ) return;

			PerformShoot();
		}

		private bool CanShoot()
		{
			return SinceEquipped >= EquipTime &&
			       SinceShot >= FireRate &&
			       (!IsSemiAuto || !_hasShot) &&
			       !IsReloading &&
			       CurrentAmmo > 0;
		}

		private void PerformShoot()
		{
			Sound.Play( ShootSound );
			_hasShot = true;
			CurrentAmmo--;
			Log.Info( $"Weapon {GameObject.Name} fired. Current Ammo: {CurrentAmmo}/{ClipSize}" );

			GameObject.Dispatch( new WeaponStateChangedEvent( this ) );

			Viewmodel?.Set( "b_attack", true );

			var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );
			if ( camera == null ) return;

			var rayStart = camera.Transform.Position;
			var shotTrace = Scene.Trace.Ray( rayStart, rayStart + camera.Transform.World.Forward * 65536f )
				.IgnoreGameObjectHierarchy( GameObject.Parent )
				.UseHitboxes()
				.Run();

			CreateParticles();

			if ( shotTrace.Hit )
			{
				HandleHit( shotTrace );
			}

			SinceShot = 0f;

			if ( CurrentAmmo == 0 )
			{
				StartReload();
			}
		}

		private async void CreateParticles()
		{
			var to = TrailObject.Clone();
			to.SetParent(GameObject, false );
			to.Transform.Position = Viewmodel.GetAttachment( "muzzle", true ).Value.Position;

			

			MuzzleObject.Transform.Position = Viewmodel.GetAttachment( "muzzle", true ).Value.Position;
			MuzzleObject.Enabled = true;

			await Task.Delay( 20 );
			to.SetParent( null, true );
			await Task.Delay( 200 );
			MuzzleObject.Enabled = false;
			to.Destroy();

		}

		private void HandleHit( SceneTraceResult shotTrace )
		{
			// SpawnImpactEffect( shotTrace );

			var damageable = shotTrace.GameObject?.Components.Get<IDamageable>();
			if ( damageable != null )
			{
				Log.Info( damageable.ToString() );
				float totalDamage = CalculateDamage( shotTrace );
				ApplyDamage( damageable, totalDamage, shotTrace );
			}
		}

		private void SpawnImpactEffect( SceneTraceResult shotTrace )
		{
			if ( ImpactPrefab != null )
			{
				ImpactPrefab.Clone( shotTrace.EndPosition, Rotation.LookAt( -shotTrace.Normal ) );
			}
		}

		private float CalculateDamage( SceneTraceResult shotTrace )
		{
			float damage = WeaponDamage;
			if ( IsHeadshot( shotTrace ) )
			{
				damage *= HeadshotMultiplier;
			}

			return damage;
		}

		private bool IsHeadshot( SceneTraceResult shotTrace )
		{
			// TODO Implement proper headshot detection logic here
			return shotTrace.Hitbox?.Bone?.Name.ToLower().Contains( "head" ) ?? false;
		}

		private void ApplyDamage( IDamageable damageable, float damage, SceneTraceResult shotTrace )
		{
			var damageInfo = new DamageInfo { Damage = damage, Attacker = Owner, Position = shotTrace.EndPosition };
			damageable.OnDamage( damageInfo );
		}

		public void StartReload()
		{
			if ( IsReloading || CurrentAmmo == ClipSize ) return;
			IsReloading = true;
			TimeInReload = 0f;

			// TODO: Play reload animation
			Viewmodel?.Set( "b_reload", true );
			// TODO: Play reload sound

			Log.Info( $"Weapon {GameObject.Name} started reloading." );
			GameObject.Dispatch( new WeaponStateChangedEvent( this ) );
		}


		private void CompleteReload()
		{
			CurrentAmmo = ClipSize;
			IsReloading = false;
			Log.Info( $"Weapon {GameObject.Name} reloaded. Current Ammo: {CurrentAmmo}/{ClipSize}" );
			GameObject.Dispatch( new WeaponStateChangedEvent( this ) );

			Viewmodel?.Set( "b_reload", false );
			// TODO: Play reload complete animation
			// TODO: Play reload complete sound
		}

		public void SetVisible( bool visible )
		{
			if ( Viewmodel != null )
			{
				Viewmodel.Enabled = visible;
			}
		}

		public void Equip()
		{
			SinceEquipped = 0f;
			SetVisible( true );
			//Viewmodel?.Set( "b_deploy", true );
			// TODO: Play equip sound
		}

		public void Holster()
		{
			SetVisible( false );
			// TODO: Play holster animation
			// TODO: Play holster sound
		}

		public void OnTriggerEnter( Collider other )
		{
			if ( IsPickedUp || !other.GameObject.Tags.Has( "player" ) ) return;

			var inventory = other.GameObject.Components.Get<PlayerInventory>();
			if ( inventory != null )
			{
				if ( inventory.AddWeapon( this ) )
				{
					AddToOwner( inventory );
				}
			}
		}

		private void AddToOwner( PlayerInventory inventory )
		{
			GameObject.SetParent( inventory.GameObject, false );
			Transform.Position = inventory.GameObject.Transform.Position;
			Transform.Rotation = inventory.GameObject.Transform.Rotation;

			if ( Worldmodel is not null )
			{
				Worldmodel.Enabled = false;
			}

			if ( Viewmodel is not null )
			{
				Viewmodel.Enabled = true;
			}

			Owner = inventory.GameObject;
			IsPickedUp = true;

			inventory.SetActive( this );
		}
	}
}
