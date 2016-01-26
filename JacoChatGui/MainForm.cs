using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SplittyDev.JacoChat;

namespace JacoChatGui
{
	public class MainForm : Form {

		readonly JacoChatClient Client;
		new MenuStrip Menu;
		Random Rng;
		RichTextBox Content;
		TextBox Input;
		AutoCompleteStringCollection Autocomplete;

		public MainForm () {
			Rng = new Random ();
			Client = new JacoChatClient ();
			Client.TopicReceived += Client_TopicReceived;
			Client.NamesReceived += Client_NamesReceived;
			Client.MessageReceived += Client_MessageReceived;
			Client.UnhandledResponseReceived += Client_UnhandledResponseReceived;
			InitializeComponent ();
		}

		void Client_NamesReceived (object sender, NameInformation e) {
			Log ("* Users in this channel: {0}", e);
		}

		void Client_UnhandledResponseReceived (object sender, Response e) {
			Log ("* Unhandled: {0}", e.Value);
		}

		void Client_MessageReceived (object sender, PrivateMessage e) {
			Log ("[{0}] {1}", e.Sender, e.Message);
		}

		void Client_TopicReceived (object sender, TopicInformation e) {
			var act = new Action (() => Text = string.Format ("JacoChat | Topic: {0}", e.Topic));
			if (e.Channel == "#main") {
				if (InvokeRequired)
					BeginInvoke (act);
				else
					act ();
			}
		}

		void InitializeComponent () {

			// this
			Width = 640;
			Height = 480;
			Text = "JacoChat";
			Font = new Font ("Consolas", 11.25f);
			BackColor = Color.FromArgb (40, 40, 40);

			// Menu
			Menu = new MenuStrip ();
			Menu.Items.Add ("Connect to hassiumlang.com", null, new EventHandler ((o, e) => {
				var oldtext = Input.Text;
				Input.Clear ();
				Input.Focus ();
				Task.Factory.StartNew (() => {
					const string targetText = "/connect hassiumlang.com:1337";
					var i = 0;
					while (i < targetText.Length) {
						Input.BeginInvoke (new MethodInvoker (() => {
							Input.Text += targetText [i++];
							Input.SelectionStart = Input.Text.Length;
							Input.SelectionLength = 0;
							Input.Update ();
						}));
						Thread.Sleep (50);
					}
					Input.BeginInvoke (new MethodInvoker (() => {
						Input.Focus ();
						Input.SelectionStart = Input.Text.Length;
						Input.SelectionLength = 0;
						Input.Update ();
						SendKeys.SendWait ("~");
						Input.SelectionStart = Input.Text.Length;
						Input.SelectionLength = 0;
						Input.Update ();
						Input.Text = oldtext;
					}));
				});
			}));

			// Input
			Autocomplete = new AutoCompleteStringCollection ();
			Autocomplete.AddRange (new [] {
				"/connect",
			});
			Input = new TextBox {
				BorderStyle = BorderStyle.FixedSingle,
				BackColor = Color.FromArgb (40, 40, 40),
				ForeColor = Color.FromArgb (235, 235, 235),
				Left = 0,
				Width = ClientSize.Width,
				Dock = DockStyle.Bottom,
				Text = string.Empty,
				AutoCompleteMode = AutoCompleteMode.SuggestAppend,
				AutoCompleteSource = AutoCompleteSource.CustomSource,
				AutoCompleteCustomSource = Autocomplete,
			};
			Input.KeyDown += Input_KeyDown;

			// Content
			Content = new RichTextBox {
				AcceptsTab = false,
				AllowDrop = false,
				Capture = false,
				DetectUrls = true,
				EnableAutoDragDrop = false,
				Multiline = true,
				TabStop = false,
				BorderStyle = BorderStyle.None,
				ReadOnly = true,
				BackColor = Color.FromArgb (40, 40, 40),
				ForeColor = Color.FromArgb (235, 235, 235),
				Left = 0,
				Top = Menu.Height,
				Width = ClientSize.Width,
				Height = ClientSize.Height - Menu.Height - Input.Height - 5,
				Anchor = 0x0
					| AnchorStyles.Left
					| AnchorStyles.Top
					| AnchorStyles.Right
					| AnchorStyles.Bottom,
				Text = "Type /connect <server>:<port> to connect."
			};
			Content.TextChanged += Content_TextChanged;
			Content.LinkClicked += Content_LinkClicked;

			// Add controls
			Controls.Add (Menu);
			Controls.Add (Content);
			Controls.Add (Input);
		}

		void Content_LinkClicked (object sender, LinkClickedEventArgs e) {
			Process.Start ("explorer.exe", e.LinkText);
		}

		void Content_TextChanged (object sender, EventArgs e) {
			var act = new Action (() => {
				Content.SelectionStart = Content.Text.Length;
				Content.ScrollToCaret ();
			});
			if (Content.InvokeRequired)
				Content.BeginInvoke (act);
			else
				act ();
		}

		void Input_KeyDown (object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Enter) {
				ProcessCommand (Input.Text);
				Input.Clear ();
			}
		}

		void ProcessCommand (string cmd) {
			cmd = cmd.Trim ();
			var parts = cmd.Split (' ');
			if (cmd.StartsWith ("/")) {
				var command = parts.First ().TrimStart ('/').ToLowerInvariant ();
				switch (command) {
				case "connect":
					{
						if (Client.Connected) {
							Log ("* You are already connected.");
							return;
						}
						var cstr = parts.Skip (1).FirstOrDefault ();
						if (cstr != default (string)) {
							var server = string.Empty;
							var port = 22;
							if (cstr.Contains (":")) {
								server = cstr.Split (':').First ();
								if (!int.TryParse (cstr.Split (':').Last (), out port)) {
									MessageBox.Show ("Invalid port! Must be a number.");
									return;
								}
								Content.Clear ();
								Log ("Connecting to server: {0}/{1}", server, port);
								Task.Factory.StartNew (() => {
									try {
										Client.Connect (server, port);
										// Analysis disable once EmptyGeneralCatchClause
									} catch (Exception) {
										Log ("Error: Could not connect to target.");
										return;
									}
								}).Wait ();
								if (Client.Connected) {
									Autocomplete.AddRange (new [] {
										"/raw",
										"/nick",
									});
									Content.Clear ();
									Log ("Connected. Set your nick by typing /nick <name>");
								}
							}
						}
					}
					break;
				case "nick":
					{
						if (!Client.Connected)
							return;
						var nick = parts.Skip (1).FirstOrDefault ();
						if (nick != default (string)) {
							var prevnick = Client.NickString;
							var prevnickset = Client.NickSet;
							Client.Nick (nick);
							if (prevnickset)
								Log ("* {0} is now known as {1}", prevnick, nick);
							else
								Client.Join ("#main");
						}
					}
					break;
				case "raw":
					{
						if (!Client.Connected)
							return;
						Client.Send (string.Join (" ", parts.Skip (1)));
					}
					break;
				}
			} else {
				if (Client.Connected && Client.NickSet) {
					Log ("[{0}] {1}", Client.NickString, cmd);
					Client.Msg ("#main", cmd);
				} else
					Log ("Please connect and set your nick first.");
			}
		}

		void Log (string format, params object[] args) {
			var msg = string.Format (format, args);
			msg = msg.StartsWith ("*")
				? string.Format ("* {0}", msg.TrimStart ('*', ' '))
				: string.Format ("  {0}", msg);
			if (Content.InvokeRequired)
				Content.BeginInvoke (new MethodInvoker (()
					=> Content.AppendText (string.Format ("{0}\n", msg))));
			else
				Content.AppendText (string.Format ("{0}\n", msg));
		}

		protected override void OnFormClosed (FormClosedEventArgs e) {
			Client.Dispose ();
			base.OnFormClosed (e);
		}
	}
}

