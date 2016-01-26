using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace SplittyDev.JacoChat
{
	public class JacoChatClient : IDisposable {

		public event EventHandler<NameInformation> NamesReceived;
		public event EventHandler<TopicInformation> TopicReceived;
		public event EventHandler<Response> UnhandledResponseReceived;
		public event EventHandler<PrivateMessage> MessageReceived;

		public bool Connected { get; private set; }
		public bool NickSet { get; private set; }
		public string NickString { get; private set; }

		readonly CancellationTokenSource TokenSource;
		readonly CancellationToken Token;
		readonly TcpClient Client;

		string Host;
		int Port;
		StreamReader Reader;
		StreamWriter Writer;

		public JacoChatClient () {
			TokenSource = new CancellationTokenSource ();
			Token = TokenSource.Token;
			Client = new TcpClient ();
			Connected = false;
			NickSet = false;
			NickString = string.Empty;
		}

		public JacoChatClient (string host, int port) : this () {
			Host = host;
			Port = port;
		}

		public void Connect () {
			if (Host == null)
				throw new ArgumentException ("No host given.");
			Console.WriteLine ("Connecting to {0}/{1}", Host, Port);
			Client.Connect (Host, Port);
			Connected = true;
			Client.SendTimeout = 10000;
			Client.ReceiveTimeout = 10000;
			var ns = Client.GetStream ();
			Reader = new StreamReader (ns);
			Writer = new StreamWriter (ns);
			Listen ();
		}

		public void Connect (string host, int port) {
			Host = host;
			Port = port;
			Connect ();
		}

		public void Connect (string nick, string channel = null) {
			Connect ();
			Nick (nick);
			if (!string.IsNullOrEmpty (channel))
				Join (channel);
		}

		public void Connect (string host, int port, string nick, string channel = null) {
			Connect (host, port);
			Nick (nick);
			if (!string.IsNullOrEmpty (channel))
				Join (channel);
		}

		public void Nick (string nick) {
			Send ("NICK {0}", nick);
			NickString = nick;
			NickSet = true;
		}

		public void Join (string channel) {
			Send ("JOIN {0}", channel);
		}

		public void Msg (string target, string message) {
			Send ("PRIVMSG {0} {1}", target, message);
		}

		public void Send (string format, params object[] args) {
			Writer.Write ("{0}\r\n", string.Format (format, args));
			Writer.Flush ();
		}

		void Listen () {
			var fun = new ThreadStart (() => {
				while (ReceiveOne ()) {
				}
			});
			var trd = new Thread (fun);
			trd.SetApartmentState (ApartmentState.STA);
			trd.Start ();
		}

		bool ReceiveOne () {
			if (Token.IsCancellationRequested)
				return false;
			if (Client.Client.Poll (0, SelectMode.SelectRead)) {
				var buf = new byte [1];
				if (Client.Client.Receive (buf, SocketFlags.Peek) == 0)
					return false;
			}
			var handled = true;
			var skip = false;
			string msg;
			try {
				msg = Reader.ReadLine ().Trim ();
			} catch (Exception) {
				return false;
			}
			var parts = msg.Split (' ');
			if (msg == "PING") {
				Send ("PONG");
				return true;
			}
			if (parts.Skip (1).FirstOrDefault () != default (string)) {
				var sender = parts.First ();
				var command = parts.Skip (1).First ();
				if (sender == "server") {
					skip = true;
					switch (command) {
					case "TOPIC":
						{
							var channel = parts.Skip (2).First ();
							var topic = msg.Substring (msg.IndexOf (':') + 1).Trim ();
							var topicInfo = new TopicInformation (channel, topic);
							if (TopicReceived != null)
								TopicReceived (this, topicInfo);
						}
						break;
					case "NAMES":
						{
							var channel = parts.Skip (2).First ();
							var names = msg.Substring (msg.IndexOf (':') + 1).Trim ().Split (' ');
							var nameInfo = new NameInformation (channel, names);
							if (NamesReceived != null)
								NamesReceived (this, nameInfo);
						}
						break;
					default:
						skip = false;
						handled = false;
						break;
					}
				}
				if (skip)
					return true;
				skip = true;
				switch (command) {
				case "PRIVMSG":
					if (MessageReceived != null)
						MessageReceived (this, new PrivateMessage (
							sender: sender,
							target: parts.Skip (2).First (),
							message: msg.Substring (msg.IndexOf (':') + 1).Trim ()
						));
					break;
				default:
					skip = false;
					handled = false;
					break;
				}
				if (skip)
					return true;
			}
			if (!handled && UnhandledResponseReceived != null)
				UnhandledResponseReceived (this, new Response (msg));
			return true;
		}

		public void Dispose () {
			try {
				TokenSource.Cancel ();
			// Analysis disable once EmptyGeneralCatchClause
			} catch (Exception) { }
			Client.Client.Dispose ();
		}
	}
}

