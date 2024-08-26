using Sandbox.Events;
using Ultraneon.Game.GameMode;

namespace Ultraneon.Domain.Events;

// Weapons (used to update ui)
public record WeaponStateChangedEvent( WeaponBaseNeon Weapon ) : IGameEvent;

public record ActiveWeaponChangedEvent( WeaponBaseNeon OldWeapon, WeaponBaseNeon NewWeapon ) : IGameEvent;

// Zone Capture Stuff
public record CaptureZoneCharacterEnteredEvent( string ZoneName, Team CurrentTeam, BaseNeonCharacterEntity EnteredCharacter ) : IGameEvent;

public record CaptureZoneCharacterExitedEvent( string ZoneName, Team CurrentTeam, BaseNeonCharacterEntity ExitCharacter ) : IGameEvent;

public record CaptureZoneCapturedEvent( string ZoneName, Team PreviousTeam, Team NewTeam ) : IGameEvent;

// Game Events
public record GamePausedEvent() : IGameEvent;

public record GameResumedEvent() : IGameEvent;

public record CharacterSpawnEvent( BaseNeonCharacterEntity character, Vector3 spawnPosition ) : IGameEvent;

public record DamageEvent( BaseNeonCharacterEntity Target, Entity Attacker, float Damage, Vector3 Position ) : IGameEvent;

public record CharacterDeathEvent( BaseNeonCharacterEntity Victim, Entity Killer, bool IsStylish = false ) : IGameEvent;

public record GameModeActivatedEvent( GameMode GameMode ) : IGameEvent;

public record GameOverEvent( int MaxWaveReached ) : IGameEvent;
