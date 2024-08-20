using Sandbox;

public sealed class SpotAi : Component, Component.ITriggerListener
{
	
	public void OnTriggerEnter( Collider other )
	{
		if ( other.GameObject.Tags.Has( "player" ) )
		{
			GameObject.Parent.Components.Get<EnemyAi>().Target = other.GameObject;
		}
		
	}
}
