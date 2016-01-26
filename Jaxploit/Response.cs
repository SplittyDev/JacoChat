using System;

namespace SplittyDev.JacoChat
{
	public class Response : EventArgs {

		public string Value;

		public Response (string value) {
			Value = value;
		}
	}
}

