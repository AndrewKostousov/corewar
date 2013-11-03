using System;
using System.Net;
using JetBrains.Annotations;
using Server.Sessions;

namespace Server
{
	public class GameHttpContext
	{
		public const string GodModeSecretCookieName = "godModeSecret";
		public const string GodModeCookieName = "godMode";
		private const string sessionIdCookieName = "coreWarSessionId";
		private const string basePathCookieName = "basePath";
		private readonly HttpListenerContext httpListenerContext;

		public GameHttpContext([NotNull] HttpListenerContext httpListenerContext, [NotNull] string basePath, [NotNull] ISessionManager sessionManager)
		{
			BasePath = basePath;
			this.httpListenerContext = httpListenerContext;
			this.SetCookie(basePathCookieName, basePath, persistent: false, httpOnly: false);
			var sessionId = this.TryGetCookie<Guid>(sessionIdCookieName, Guid.TryParse) ?? Guid.NewGuid();
			this.SetCookie(sessionIdCookieName, sessionId.ToString(), persistent: true, httpOnly: true);
			Session = sessionManager.GetSession(sessionId);
		}

		[NotNull]
		public ISession Session { get; private set; }

		[NotNull]
		public HttpListenerRequest Request
		{
			get { return httpListenerContext.Request; }
		}

		[NotNull]
		public HttpListenerResponse Response
		{
			get { return httpListenerContext.Response; }
		}

		[NotNull]
		public string BasePath { get; private set; }
	}
}