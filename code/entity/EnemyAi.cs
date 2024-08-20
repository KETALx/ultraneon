using Sandbox;
using Sandbox.Citizen;

public sealed class EnemyAi : Component, Component.ITriggerListener
{
	[Property, ToggleGroup( "Nav" )] public GameObject Target { get; set; }
	[Property, ToggleGroup( "Nav" )] public NavMeshAgent agent { get; set; }

	[Property, ToggleGroup( "Nav" )] public CitizenAnimationHelper animationHelper { get; set; }

	[Property, ToggleGroup( "Gameplay" )] public float stopDistance { get; set; }
	protected override void OnUpdate()
	{
		if ( !agent.Enabled ) return;
		if ( Target is null ) return;
		agent.MoveTo( Target.Transform.Position );
		animationHelper.WithVelocity( agent.Velocity );
		//animationHelper.WithLook( Target.Transform.Position, 1,1,1 );

		if ( Vector3.DistanceBetween( Target.Transform.Position, agent.Transform.Position ) < stopDistance )
		{
			agent.Stop();
		}
	}


}
