using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace codeshop.Sockets
{
	public class RoomMessageHandler : WebSocketHandler
	{
		public RoomMessageHandler(WebSocketConnectionManager manager) : base(manager)
		{
		}

		public override async void OnConnected(WebSocket socket)
		{
			base.OnConnected(socket);

			var socketId = WSManager.GetId(socket);
			await SendMessageToAllAsync($"{socketId} is now connected");
		}

		public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
		{
			var socketId = WSManager.GetId(socket);
			var message = $"{socketId} said: {Encoding.UTF8.GetString(buffer, 0, result.Count)}";

			await SendMessageToAllAsync(message);
		}
	}
}