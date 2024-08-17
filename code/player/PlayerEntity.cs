using Sandbox;
using System;

[Category("Ultraneon")]
public sealed class PlayerEntity : Component, Component.IDamageable
{
	[Property, ReadOnly] private float maxHealth { get; set; } = 100f;
	[Property,ReadOnly] public bool isAlive { get;private set; } = true;
	[Property] public float health { get; private set; }

	protected override void OnEnabled()
	{
		health = maxHealth; 
	}

	public void OnDamage( in DamageInfo damage )
	{
		if ( !isAlive ) return;

		health -= Math.Clamp(health - damage.Damage, 0f, maxHealth);

		if ( health <= 0 ) killPlayer();

	}
	[Button( "kill player" )]
	public void killPlayer()
	{
		health = 0f;
		isAlive = false;
		Log.Info( Network.OwnerConnection.DisplayName + "has been killed" );
	}


	[Button( "take 50 damage" )]
	void debugdamage()
	{
		GameObject.Components.Get<IDamageable>().OnDamage( new DamageInfo() { Damage = 60 } );
	}
}
