using Sandbox;
using Sandbox.Citizen;

public sealed class EnemyAi : Component
{
	[Property, ToggleGroup( "Path" )] public GameObject Target { get; set; }
	[Property, ToggleGroup( "Path" )] public NavMeshAgent agent { get; set; }

	[Property, ToggleGroup( "Path" )] public CitizenAnimationHelper animationHelper { get; set; }
	protected override void OnUpdate()
	{

		if(Target is null) return;
		agent.MoveTo( Target.Transform.Position );
		animationHelper.WithVelocity( agent.Velocity );
		animationHelper.WithLook( Target.Transform.Position, 1,1,1 );

		if ( Vector3.DistanceBetween( Target.Transform.Position, agent.Transform.Position ) < 100f )
		{
			agent.Stop();
		}
	}
}
