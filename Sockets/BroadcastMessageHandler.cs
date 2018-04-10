using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using codeshop.Models;

namespace codeshop.Sockets
{
	public class BroadcastMessageHandler : WebSocketHandler
	{
		public BroadcastMessageHandler(WebSocketConnectionManager manager) : base(manager)
		{
		}

		public override async void OnConnected(WebSocket socket)
		{
			base.OnConnected(socket);

			var socketId = WSManager.GetId(socket);
			await SendMessageToAllAsync($"{socketId} is now connected");
		}

		public override async Task OnDisconnected(WebSocket socket)
		{
			var id = WSManager.GetId(socket);
			await base.OnDisconnected(socket);
			await SendMessageToAllAsync($"{id} has disconnected");
		}

		public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
		{
			var socketId = WSManager.GetId(socket);
			var message = $"{socketId} said: {Encoding.UTF8.GetString(buffer, 0, result.Count)}";

			await SendMessageToAllAsync(message);
		}

		public async Task SendNotification(Notification notification)
		{
			switch (notification.Channel)
			{
				case "all":
					await SendMessageToAllAsync(notification.Message);
					break;
				case "private":
					await SendMessageAsync(notification.ToId, notification.Message);
					break;
				default:
					throw new Exception("bad notification channel");
			}
		}
	}
}