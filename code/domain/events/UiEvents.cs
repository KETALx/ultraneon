using Sandbox.Events;

namespace Ultraneon.Domain.Events;

public enum MenuAction
{
	Play,
	Settings,
	Quit
}

public record MenuActionEvent( MenuAction Action ) : IGameEvent;

public enum UiInfoFeedType
{
	Normal,
	Warning,
	Success,
	Debug
}

public record UiInfoFeedEvent(string Message, UiInfoFeedType Type) : IGameEvent;
