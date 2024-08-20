using Sandbox;
using System;

public sealed class SpotAi : Component, Component.ITriggerListener
{
	[Property, ReadOnly] public GameObject player { get; set; }
	[Property, ReadOnly] public Vector3 lastSeenLocation { get; set; }
	public void OnTriggerEnter( Collider other )
	{
		if ( other.GameObject.Name == "player" )
		{
			player = other.GameObject;
		}

	}

	public void OnTriggerExit( Collider other )
	{
		player = null;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( player != null )
		{
			var spotTrace = Scene.Trace.Ray( Transform.Position, player.Transform.Position )
				.Run();
			DebugOverlay.Trace( spotTrace );
			if ( spotTrace.Hit && spotTrace.GameObject.Tags.Has( "player" ) )
			{
				GameObject.Parent.Components.Get<EnemyAi>().Target = spotTrace.GameObject;
			}
		}
	}
}
