using Sandbox;
using System.Security.Cryptography.X509Certificates;

public sealed class UltraneonNetwork : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerHudPrefab { get; set; }

	public void OnActive( Connection channel )
	{
		if ( PlayerHudPrefab is null ) return;
		var player = PlayerHudPrefab.Clone();
		player.NetworkSpawn( channel );
	}
}
