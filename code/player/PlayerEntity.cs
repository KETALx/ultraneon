using Sandbox;
using System;
using System.Threading.Channels;

[Category("Ultraneon")]
public sealed class PlayerEntity : Component, Component.IDamageable, Component.INetworkListener
{
	[Property, ReadOnly] private float maxHealth { get; set; } = 100f;
	[Property,ReadOnly] public bool isAlive { get;private set; } = true;
	[Property, Sync()] public float health { get; private set; }

	[ConCmd("set_health")]
	public static void SetHealth(int num)
	{
		var pl = Game.ActiveScene.GetAllComponents<PlayerEntity>().FirstOrDefault();

		pl.health = num;
	}
	protected override void OnEnabled()
	{
		health = maxHealth;
	}

	public void OnDamage( in DamageInfo damage )
	{
		if ( !isAlive ) return;

		health = Math.Clamp(health - damage.Damage, 0f, maxHealth);

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
