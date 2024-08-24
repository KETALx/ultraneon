using Sandbox.Events;

namespace Ultraneon.Domain.Events;

public enum MenuAction
{
	Play,
	Settings,
	Quit
}

public record MenuActionEvent( MenuAction Action ) : IGameEvent;
