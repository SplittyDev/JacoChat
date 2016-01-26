using System;

namespace SplittyDev.JacoChat
{
	public class TopicInformation : EventArgs {
		public string Channel;
		public string Topic;

		public TopicInformation (string channel, string topic) {
			Channel = channel;
			Topic = topic;
		}
	}
}

