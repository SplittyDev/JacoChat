using System;

namespace SplittyDev.JacoChat
{
	public class NameInformation : EventArgs {
		public string Channel;
		public string[] Names;

		public NameInformation (string channel, string[] names) {
			Channel = channel;
			Names = names;
		}

		public override string ToString () {
			return string.Join (", ", Names);
		}
	}
}

