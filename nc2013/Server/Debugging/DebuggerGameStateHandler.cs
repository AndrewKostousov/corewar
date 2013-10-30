using System.Net;
using JetBrains.Annotations;
using Server.Sessions;

namespace Server.Debugging
{
	public class DebuggerGameStateHandler : DebuggerHandlerBase
	{
		public DebuggerGameStateHandler([NotNull] IHttpSessionManager httpSessionManager, [NotNull] IDebuggerManager debuggerManager) : base("debugger/state", httpSessionManager, debuggerManager) {}

		protected override void DoHandle([NotNull] HttpListenerContext context, [NotNull] IDebugger debugger)
		{
			context.SendResponse(debugger.GameState);
		}
	}
}