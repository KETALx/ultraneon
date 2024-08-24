using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Events;
using Ultraneon;
using Ultraneon.Domain;
using Ultraneon.Domain.Events;
using Ultraneon.Player;

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

	[Property, HostSync]
	public Team ContestingTeam { get; set; } = Team.Neutral;

	[Property, ReadOnly, HostSync]
	public float CaptureProgress { get; set; } = 0f;

	[Property]
	public ModelRenderer ZoneModel { get; set; }

	[Property]
	public Action<Team> OnCaptureAction { get; set; }

	public float MinimapX { get; set; }
	public float MinimapY { get; set; }
	public Team PreviousTeam { get; set; }
	public bool HasChanged { get; set; }
	public bool AllowBotCapture { get; set; } = true;

	private TimeSince timeSinceLastCapture;
	private List<BaseNeonCharacterEntity> charactersInZone = new();

	protected override void OnStart()
	{
		base.OnStart();
		timeSinceLastCapture = 0f;
		UpdateZoneVisuals();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		UpdateCapture();
		UpdateZoneVisuals();
	}

	private void UpdateCapture()
	{
		if ( charactersInZone.Any() )
		{
			ContestingTeam = charactersInZone
				.GroupBy( p => p.CurrentTeam )
				.OrderByDescending( g => g.Count() )
				.First().Key;

			if ( ContestingTeam != ControllingTeam )
			{
				// Check if it's a bot trying to capture during overtime
				if ( !AllowBotCapture && ContestingTeam == Team.Enemy )
				{
					Log.Error( $"[CaptureZoneEntity-UpdateCapture] {PointName} bot trying to capture during overtime" );
					return;
				}

				CaptureProgress += Time.Delta / CaptureTime * charactersInZone.Count( p => p.CurrentTeam == ContestingTeam );
				if ( CaptureProgress >= 1f )
				{
					var previousTeam = ControllingTeam;
					ControllingTeam = ContestingTeam;
					CaptureProgress = 0f;
					OnPointCaptured( previousTeam );
				}
			}
			else
			{
				CaptureProgress = Math.Max( 0f, CaptureProgress - Time.Delta / CaptureTime );
			}

			timeSinceLastCapture = 0f;
		}
		else if ( CaptureProgress > 0 )
		{
			CaptureProgress -= Time.Delta / CaptureTime;
		}
	}

	private void UpdateZoneVisuals()
	{
		if ( ZoneModel != null )
		{
			Color dominantTeamColor = ControllingTeam switch
			{
				Team.Player => PlayerColor,
				Team.Enemy => EnemyColor,
				_ => NeutralColor
			};
			
			if ( CaptureProgress >= 1.0f )
			{
				ZoneModel.Tint = dominantTeamColor;
				return;
			}

			Color contestingTeamColor = ControllingTeam switch
			{
				Team.Player => PlayerColor,
				Team.Enemy => EnemyColor,
				_ => NeutralColor
			};

			ZoneModel.Tint = Color.Lerp( dominantTeamColor, contestingTeamColor, CaptureProgress );
			// TODO: Add particle effects or other visual indicators for capture progress
		}
	}

	public void OnTriggerEnter( Collider other )
	{
		var character = other.GameObject.Components.Get<BaseNeonCharacterEntity>();
		if ( character != null )
		{
			charactersInZone.Add( character );
			if ( charactersInZone.Count == 1 )
			{
				OnStartCapture();
			}
		}
	}

	public void OnTriggerExit( Collider other )
	{
		var character = other.GameObject.Components.Get<BaseNeonCharacterEntity>();
		if ( character != null )
		{
			charactersInZone.Remove( character );
		}
	}

	private void OnStartCapture()
	{
		Log.Info( $"[CaptureZoneEntity] {PointName} is being captured!" );
		GameObject.Dispatch( new UiInfoFeedEvent( $"{{PointName}} is being captured!", UiInfoFeedType.Warning ) );
		// TODO: Send a lightwave in radius to alert other players
	}


	private void OnStopCapture()
	{
		Log.Info( $"[CaptureZoneEntity] {PointName} stopped being captured!" );
		GameObject.Dispatch( new UiInfoFeedEvent( $"{{PointName}} stopped being captured!", UiInfoFeedType.Warning ) );
		// TODO: Send a lightwave in radius to alert other players
	}

	private void OnPointCaptured( Team previousTeam )
	{
		GameObject.Dispatch( new UiInfoFeedEvent( $"{{PointName}} has been captured by {{ControllingTeam}}!", UiInfoFeedType.Success ) );
		Log.Info( $"[CaptureZoneEntity] {PointName} has been captured by {ControllingTeam}!" );
		PreviousTeam = previousTeam;
		GameObject.Dispatch( new CaptureZoneCapturedEvent( PointName, previousTeam, ControllingTeam ) );

		if ( OnCaptureAction != null ) OnCaptureAction( previousTeam );
	}

	public bool IsEntityInZone( BaseNeonCharacterEntity entity )
	{
		return charactersInZone.Contains( entity );
	}

	public bool CanCapture( BaseNeonCharacterEntity entity )
	{
		return AllowBotCapture || entity is PlayerNeon;
	}
}
