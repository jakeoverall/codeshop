using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using codeshop.Models;
using codeshop.Sockets;
using Microsoft.AspNetCore.Mvc;

namespace codeshop.Controllers
{
	[Route("[controller]")]
	public class NotificationsController : Controller
	{
		private BroadcastMessageHandler _socketHandler { get; set; }

		public NotificationsController(BroadcastMessageHandler notificationsMessageHandler)
		{
			_socketHandler = notificationsMessageHandler;
		}

		// GET api/values
		[HttpPost]
		public async Task<string> SendMessage([FromBody] Notification notification)
		{
			if (!ModelState.IsValid) { return "bad notification body"; }
			try
			{
				await _socketHandler.SendNotification(notification);
				return "message sent";
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}
	}
}
