using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace codeshop.Sockets
{
	public class WebSocketConnectionManager
	{
		private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

		public WebSocket GetSocketById(string id) => _sockets.FirstOrDefault(s => s.Key == id).Value;

		public ConcurrentDictionary<string, WebSocket> GetAll() => _sockets;

		public string GetId(WebSocket ws) => _sockets.FirstOrDefault(s => s.Value == ws).Key;

		public void AddSocket(WebSocket ws) => _sockets.TryAdd(CreateConnectionId(), ws);

		private string CreateConnectionId() => Guid.NewGuid().ToString();

		public async Task RemoveSocket(string id)
		{
			WebSocket ws;
			_sockets.TryRemove(id, out ws);
			await ws.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure, statusDescription: "Socket Closed by Manager", cancellationToken: CancellationToken.None);
		}

	}

	public abstract class WebSocketHandler
	{
		protected WebSocketConnectionManager WSManager { get; set; }

		public WebSocketHandler(WebSocketConnectionManager manager) => WSManager = manager;

		public virtual void OnConnected(WebSocket ws) => WSManager.AddSocket(ws);

		public virtual async Task OnDisconnected(WebSocket ws) => await WSManager.RemoveSocket(WSManager.GetId(ws));

		public async Task SendMessageAsync(WebSocket ws, string message)
		{
			if (ws.State != WebSocketState.Open) { return; }
			await ws.SendAsync(
				buffer: new ArraySegment<byte>(array: Encoding.ASCII.GetBytes(message),
				offset: 0,
				count: message.Length),
				messageType: WebSocketMessageType.Text,
				endOfMessage: true,
				cancellationToken: CancellationToken.None
			);
		}
		public async Task SendMessageAsync(string socketId, string message) => await SendMessageAsync(WSManager.GetSocketById(socketId), message);

		public async Task SendMessageToAllAsync(string message)
		{
			foreach (var pair in WSManager.GetAll())
			{
				if (pair.Value.State == WebSocketState.Open)
				{
					await SendMessageAsync(pair.Value, message);
				}
			}
		}

		public abstract Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);

	}

	public class WebSocketMiddleware
	{
		private readonly RequestDelegate _next;

		private WebSocketHandler _WSHandler { get; set; }

		public WebSocketMiddleware(RequestDelegate next, WebSocketHandler handler)
		{
			_next = next;
			_WSHandler = handler;
		}

		public async Task Invoke(HttpContext context)
		{
			if (!context.WebSockets.IsWebSocketRequest)
				return;

			var socket = await context.WebSockets.AcceptWebSocketAsync();
			_WSHandler.OnConnected(socket);

			await Receive(socket, async (result, buffer) =>
			{
				if (result.MessageType == WebSocketMessageType.Text)
				{
					await _WSHandler.ReceiveAsync(socket, result, buffer);
					return;
				}

				else if (result.MessageType == WebSocketMessageType.Close)
				{
					await _WSHandler.OnDisconnected(socket);
					return;
				}

			});
		}

		private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
		{
			var buffer = new byte[1024 * 4];

			while (socket.State == WebSocketState.Open)
			{
				var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
																							 cancellationToken: CancellationToken.None);

				handleMessage(result, buffer);
			}
		}

	}

	public static class WebSocketService
	{
		public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app, PathString path, WebSocketHandler handler)
		{
			return app.Map(path, (_app) => _app.UseMiddleware<WebSocketMiddleware>(handler));
		}
		public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
		{
			services.AddTransient<WebSocketConnectionManager>();

			foreach (var type in Assembly.GetEntryAssembly().ExportedTypes)
			{
				if (type.GetTypeInfo().BaseType == typeof(WebSocketHandler))
				{
					services.AddSingleton(type);
				}
			}

			return services;
		}
	}
}