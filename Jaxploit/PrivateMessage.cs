using System;

namespace SplittyDev.JacoChat
{
	public class PrivateMessage : EventArgs {

		public string Sender;
		public string Target;
		public string Message;

		public PrivateMessage (string sender, string target, string message) {
			Sender = sender;
			Target = target;
			Message = message;
		}
	}
}

