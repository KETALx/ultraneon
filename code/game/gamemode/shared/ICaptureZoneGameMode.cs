namespace Ultraneon.Game.GameMode;

public interface ICaptureZoneGameMode
{
	public void OnCaptureStarted( CaptureZoneEntity zone );
	public void OnCaptureProgressUpdated( CaptureZoneEntity zone, float progress );

	public void OnCaptureStopped( CaptureZoneEntity zone );

	public void OnPointCaptured( CaptureZoneEntity zone, Team previousTeam, Team newTeam );
}
