﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Core.Arena;
using JetBrains.Annotations;
using Server.Arena;
using Server.Debugging;
using Server.Handlers;
using Server.Sessions;

namespace Server
{
	public class GameHttpServer
	{
		private readonly HttpListener listener;
		private readonly IHttpHandler[] handlers;
		private Task listenerTask;
		private readonly string basePath;
		private readonly ManualResetEvent stopEvent;
		private readonly ArenaState arenaState;
		private readonly SessionManager sessionManager;
		private readonly ConcurrentQueue<Tuple<string, long, DateTime>> lastRequests = new ConcurrentQueue<Tuple<string, long, DateTime>>();
		private readonly ConcurrentDictionary<int, Tuple<string, Stopwatch>> activeRequests = new ConcurrentDictionary<int, Tuple<string, Stopwatch>>();
		private int requestId;

		public GameHttpServer([NotNull] string prefix, [NotNull] ArenaState arenaState, [NotNull] SessionManager sessionManager, [NotNull] IDebuggerManager debuggerManager, [NotNull] ITournamentRunner tournamentRunner, [NotNull] string staticContentPath)
		{
			this.arenaState = arenaState;
			this.sessionManager = sessionManager;
			var baseUri = new Uri(prefix.Replace("*", "localhost").Replace("+", "localhost"));
			DefaultUrl = new Uri(baseUri, string.Format("?godModeSecret={0}", arenaState.GodModeSecret)).AbsoluteUri;
			basePath = baseUri.AbsolutePath;
			listener = new HttpListener();
			listener.Prefixes.Add(prefix);
			handlers = new IHttpHandler[]
			{
				new RootHandler(),
				new IndexHandler(arenaState),
				new NavPanelHandler(arenaState),
				new DebuggerStartHandler(debuggerManager),
				new DebuggerStateHandler(debuggerManager),
				new DebuggerStepHandler(debuggerManager),
				new DebuggerStepToEndHandler(debuggerManager),
				new DebuggerRestartHandler(debuggerManager),
				new DebuggerResetHandler(debuggerManager),
				new DebuggerRemoveBreakpointHandler(debuggerManager),
				new DebuggerAddBreakpointHandler(debuggerManager),
				new DebuggerClearBreakpointsHandler(debuggerManager),
				new DebuggerLoadGameHandler(debuggerManager, arenaState),
				new StaticHandler(staticContentPath),
				new ArenaRankingHandler(arenaState),
				new ArenaSubmitHandler(arenaState, tournamentRunner),
				new ArenaPlayerHandler(arenaState),
				new ArenaRemovePlayerHandler(arenaState),
				new ArenaSubmitFormHandler(arenaState),
				new ArenaSetSubmitIsAllowedHandler(arenaState)
			};
			stopEvent = new ManualResetEvent(false);
		}

		[NotNull]
		public string DefaultUrl { get; private set; }

		public void Run()
		{
			listener.Start();
			listenerTask = Task.Factory.StartNew(() =>
			{
				while (true)
				{
					var asyncResult = listener.BeginGetContext(null, null);
					if (WaitHandle.WaitAny(new[] {asyncResult.AsyncWaitHandle, stopEvent}) == 1)
						break;
					var httpListenerContext = listener.EndGetContext(asyncResult);
					Task.Factory.StartNew(() => HandleRequest(httpListenerContext));
				}
			});
		}

		public void WaitForTermination()
		{
			listenerTask.Wait();
		}

		public void Stop()
		{
			stopEvent.Set();
		}

		private void HandleRequest([NotNull] HttpListenerContext httpListenerContext)
		{
			var currentRequestId = Interlocked.Increment(ref requestId);
			try
			{
				var requestUrl = httpListenerContext.Request.RawUrl;
				Log.For(this).DebugFormat("Incoming request: {0}", requestUrl);
				var handleTime = Stopwatch.StartNew();
				activeRequests[currentRequestId] = Tuple.Create(requestUrl, handleTime);
				var context = new GameHttpContext(httpListenerContext, basePath, sessionManager, arenaState.GodModeSecret);
				if (TryHandleActivity(context))
					return;
				try
				{
					lock (context.Session)
					{
						if (arenaState.GodAccessOnly && !context.GodMode)
						{
							if (!RequestedContentIsPublic(context))
								throw new HttpException(HttpStatusCode.Forbidden, "GodAccessOnly mode is ON");
						}

						var handlersThatCanHandle = handlers.Where(h => h.CanHandle(context)).ToArray();
						if (handlersThatCanHandle.Length == 1)
						{
							Log.For(this).DebugFormat("Handling request with {0}: {1}", handlersThatCanHandle[0].GetType().Name, requestUrl);
							handlersThatCanHandle[0].Handle(context);
							context.Response.Close();
							Log.For(this).DebugFormat("Request handled in {0} ms: {1}", handleTime.ElapsedMilliseconds, requestUrl);
						}
						else if (handlersThatCanHandle.Length == 0)
							throw new HttpException(HttpStatusCode.NotImplemented, string.Format("Method '{0}' is not implemented", requestUrl));
						else
							throw new HttpException(HttpStatusCode.InternalServerError, string.Format("Method '{0}' can be handled with many handlers: {1}", requestUrl, string.Join(", ", handlersThatCanHandle.Select(h => h.GetType().Name))));
					}
				}
				catch (HttpListenerException)
				{
					throw;
				}
				catch (HttpException e)
				{
					context.Response.Headers.Clear();
					context.Response.ContentType = "text/plain; charset: utf-8";
					e.WriteToResponse(context.Response);
					context.Response.Close();
				}
				catch (Exception e)
				{
					Log.For(this).Error("Request failed", e);
					httpListenerContext.Response.Headers.Clear();
					httpListenerContext.Response.ContentType = "text/plain; charset: utf-8";
					httpListenerContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
					using (var writer = new StreamWriter(httpListenerContext.Response.OutputStream))
						writer.Write(e.ToString());
					httpListenerContext.Response.Close();
				}
			}
			catch (HttpListenerException e)
			{
				Log.Network.Debug("HttpListener failure", e);
			}
			finally
			{
				Tuple<string, Stopwatch> lastRequest;
				if (activeRequests.TryRemove(currentRequestId, out lastRequest))
				{
					lastRequests.Enqueue(Tuple.Create(lastRequest.Item1, lastRequest.Item2.ElapsedMilliseconds, DateTime.UtcNow - lastRequest.Item2.Elapsed));
					while (lastRequests.Count > 100)
					{
						Tuple<string, long, DateTime> dummy;
						lastRequests.TryDequeue(out dummy);
					}
				}
			}
		}

		private static bool RequestedContentIsPublic([NotNull] GameHttpContext context)
		{
			if (context.IsRootPathRequested()) return true;
			var requestedPath = context.Request.Url.LocalPath;
			if (requestedPath.EndsWith(".js")) return true;
			if (requestedPath.EndsWith(".css")) return true;
			if (requestedPath.EndsWith(".ico")) return true;
			if (requestedPath.StartsWith("fonts")) return true;
			if (requestedPath.EndsWith("/nav")) return true;
			if (requestedPath.EndsWith("/index")) return true;
			if (requestedPath.EndsWith("/nav.html")) return true;
			if (requestedPath.EndsWith("/index.html")) return true;
			if (requestedPath.EndsWith("/tutorial.html")) return true;
			return false;
		}

		private bool TryHandleActivity([NotNull] GameHttpContext context)
		{
			if (!string.Equals(context.Request.Url.AbsolutePath, basePath + "activity", StringComparison.OrdinalIgnoreCase))
				return false;
			if (!context.GodMode)
				return false;
			using (var writer = new StreamWriter(context.Response.OutputStream))
			{
				writer.WriteLine("Active requests:");
				foreach (var activeRequest in activeRequests.Values.OrderBy(x => x.Item1, StringComparer.OrdinalIgnoreCase).Where(x => !x.Item1.EndsWith("/activity", StringComparison.OrdinalIgnoreCase)).ToArray())
					writer.WriteLine("{0} running for {1} ms", activeRequest.Item1, activeRequest.Item2.ElapsedMilliseconds);
				writer.WriteLine();
				writer.WriteLine("Last requests:");
				foreach (var lastRequest in lastRequests.Reverse().Where(x => !x.Item1.EndsWith("/activity", StringComparison.OrdinalIgnoreCase)).ToArray())
					writer.WriteLine("[{0}] {1} ms - {2}", lastRequest.Item3.ToLocalTime(), lastRequest.Item2, lastRequest.Item1);
			}
			context.Response.Close();
			return true;
		}
	}
}