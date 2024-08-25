using System;
using Ultraneon;
using Ultraneon.Domain;
using Ultraneon.Game.GameMode;

[Category( "Ultraneon" )]
[Icon( "place" )]
public sealed class CaptureZoneEntity : Component, Component.ITriggerListener
{
	[Property]
	public string PointName { get; set; } = "Capture Zone";

	[Property]
	public Color NeutralColor { get; set; }

	[Property]
	public Color PlayerColor { get; set; }

	[Property]
	public Color EnemyColor { get; set; }

	[Property]
	public float CaptureTime { get; set; } = 15f;

	[Property, HostSync]
	public Team ControllingTeam { get; set; } = Team.Neutral;

	[Property, ReadOnly, HostSync]
	public float CaptureProgress { get; set; } = 0f;

	[Property]
	public ModelRenderer ZoneModel { get; set; }

	[Property]
	public Action<Team> OnCaptureAction { get; set; }

	[Property]
	public ICaptureZoneGameMode GameMode { get; set; }

	public float MinimapX { get; set; }
	public float MinimapY { get; set; }
	public bool HasChanged { get; set; }

	public bool AllowBotCapture { get; set; } = true;

	private List<BaseNeonCharacterEntity> charactersInZone = new();
	private Team contestingTeam = Team.Neutral;

	protected override void OnStart()
	{
		base.OnStart();
		UpdateZoneVisuals();
		GameMode = Scene.GetAllComponents<ICaptureZoneGameMode>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		UpdateCapture();
		UpdateZoneVisuals();
		GameMode?.OnCaptureProgressUpdated( this, CaptureProgress );
	}

	private void UpdateCapture()
	{
		if ( charactersInZone.Count == 0 )
		{
			// No one in the zone, slowly decay capture progress
			CaptureProgress = Math.Max( 0f, CaptureProgress - Time.Delta / (CaptureTime * 2) );
			return;
		}

		// Determine the contesting team
		contestingTeam = DetermineContestingTeam();

		if ( contestingTeam == Team.Neutral || contestingTeam == ControllingTeam )
		{
			// No valid contesting team or the controlling team is present, decay capture progress
			CaptureProgress = Math.Max( 0f, CaptureProgress - Time.Delta / CaptureTime );
		}
		else
		{
			// Valid contesting team, increase capture progress
			CaptureProgress += Time.Delta / CaptureTime;

			if ( CaptureProgress >= 1f )
			{
				CompleteCapture();
			}
		}

		HasChanged = true;
	}

	private Team DetermineContestingTeam()
	{
		var playerPresent = charactersInZone.Any( c => c.CurrentTeam == Team.Player );
		var enemyPresent = charactersInZone.Any( c => c.CurrentTeam == Team.Enemy );

		if ( playerPresent && !enemyPresent ) return Team.Player;
		if ( enemyPresent && !playerPresent && AllowBotCapture ) return Team.Enemy;
		return Team.Neutral; // Contested or no valid capturers
	}

	private void CompleteCapture()
	{
		var previousTeam = ControllingTeam;
		ControllingTeam = contestingTeam;
		CaptureProgress = 0f;
		OnPointCaptured( previousTeam );
		HasChanged = true;
	}

	private void UpdateZoneVisuals()
	{
		if ( ZoneModel == null ) return;

		Color targetColor = ControllingTeam switch
		{
			Team.Player => PlayerColor,
			Team.Enemy => EnemyColor,
			_ => NeutralColor
		};

		if ( CaptureProgress > 0 && contestingTeam != Team.Neutral )
		{
			Color contestColor = contestingTeam == Team.Player ? PlayerColor : EnemyColor;
			ZoneModel.Tint = Color.Lerp( targetColor, contestColor, CaptureProgress );
		}
		else
		{
			ZoneModel.Tint = targetColor;
		}
	}

	public void OnTriggerEnter( Collider other )
	{
		var character = other.GameObject.Components.Get<BaseNeonCharacterEntity>();
		if ( character != null )
		{
			charactersInZone.Add( character );
			CheckCaptureStart();
		}
	}

	public void OnTriggerExit( Collider other )
	{
		var character = other.GameObject.Components.Get<BaseNeonCharacterEntity>();
		if ( character != null )
		{
			charactersInZone.Remove( character );
			CheckCaptureStop();
		}
	}

	private void CheckCaptureStart()
	{
		if ( contestingTeam != Team.Neutral && contestingTeam != ControllingTeam )
		{
			OnStartCapture();
		}
	}

	private void CheckCaptureStop()
	{
		if ( charactersInZone.Count == 0 || DetermineContestingTeam() == Team.Neutral )
		{
			OnStopCapture();
		}
	}

	private void OnStartCapture()
	{
		Log.Info( $"[CaptureZoneEntity] {PointName} is being captured!" );
		GameMode?.OnCaptureStarted( this );
		HasChanged = true;
	}

	private void OnStopCapture()
	{
		Log.Info( $"[CaptureZoneEntity] {PointName} stopped being captured!" );
		GameMode?.OnCaptureStopped( this );
		HasChanged = true;
	}

	private void OnPointCaptured( Team previousTeam )
	{
		Log.Info( $"[CaptureZoneEntity] {PointName} has been captured by {ControllingTeam}!" );
		GameMode?.OnPointCaptured( this, previousTeam, ControllingTeam );
		OnCaptureAction?.Invoke( previousTeam );
		HasChanged = true;
	}

	public bool IsEntityInZone( BaseNeonCharacterEntity entity )
	{
		return charactersInZone.Contains( entity );
	}
}
