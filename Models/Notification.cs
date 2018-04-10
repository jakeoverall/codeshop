using System.ComponentModel.DataAnnotations;

namespace codeshop.Models
{
	public class Notification
	{
		[Required]
		public string Channel { get; set; }

		[Required]
		public string Message { get; set; }
		public string ToId { get; set; }
	}
}