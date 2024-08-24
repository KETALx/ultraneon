using Sandbox;

[Category( "Ultraneon" ), Title( "Explosive item" ), Icon( "local_fire_department" )]
public sealed class ExplosiveEntity : Component, Component.ITriggerListener, Component.IDamageable
{
	[Property] float damage { get; set; }
	[Property] ParticleSystem explosionParticle { get; set; }

	[Property,RequireComponent] SphereCollider sphereCollider { get; set; }

	[Property, RequireComponent] Prop prop { get; set; }

	protected override void OnAwake()
	{
		sphereCollider.Enabled = false;


		sphereCollider.IsTrigger = true;


	}

	public async void Explode()
	{
		GameObject.Tags.Add( "debris" );
		prop.Components.Get<ModelRenderer>().Enabled = false;
		prop.Components.Get<ModelCollider>().Enabled = false;
		prop.CreateGibs();

		var particle = Components.Create<LegacyParticleSystem>();
		particle.Particles = explosionParticle;
		await GameTask.DelaySeconds( 5 );
		GameObject?.Destroy();
	}

	public void OnTriggerEnter( Collider other )
	{
		
			var idmg = other.GameObject.Components.Get<IDamageable>();
			if ( idmg != null )
			{
			Log.Info( other );
			idmg.OnDamage( new DamageInfo()
			{
				Damage = damage,
				Attacker = GameObject,
				Position = GameObject.Transform.Position
			} ); ;
			}
		sphereCollider.Enabled = false;
	}

	public void OnDamage( in DamageInfo damage )
	{
		Explode();
	}
}
