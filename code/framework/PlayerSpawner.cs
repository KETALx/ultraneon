using Sandbox;
using System;

[Title( "Player spawner" )]
[Category( "Ultraneon" )]
[Icon( "add" )]
public sealed class PlayerSpawner : Component
{
	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

	protected override void OnEnabled()
	{
		if ( PlayerPrefab is null )
			return;

		var startLocation = Scene.GetAllComponents<SpawnPoint>().FirstOrDefault().Transform.World;
		PlayerPrefab.Clone( startLocation );
	}

}
