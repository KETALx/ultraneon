using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Threading.Channels;

[Category("Ultraneon")]
public sealed class PlayerEntity : Component, Component.IDamageable, Component.INetworkListener
{
	[Property, ReadOnly] private float maxHealth { get; set; } = 100f;
	[Property,ReadOnly] public bool isAlive { get;private set; } = true;
	[Property,HostSync, Change( nameof( OnDamage ) )] public float health { get; set; }

	public void OnDamage( in DamageInfo damage )
	{
		if ( !isAlive ) return;
		Assert.True( Networking.IsHost );

		health = Math.Clamp(health - damage.Damage, 0f, maxHealth);

		if ( health <= 0 ) killPlayer();
		Log.Info( damage.Attacker );

	}

	protected override void OnEnabled()
	{
		health = maxHealth;
	}
	[Button( "kill player" )]
	public void killPlayer()
	{
		health = 0f;
		isAlive = false;
		Log.Info( Network.OwnerConnection.DisplayName + "has been killed" );
	}


}
