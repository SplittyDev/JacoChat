using System;
using System.Windows.Forms;

namespace JacoChatGui
{
	class MainClass
	{
		[STAThread]
		public static void Main (string[] args) {
			Application.EnableVisualStyles ();
			using (var frm = new MainForm ()) {
				Application.Run (frm);
			}
		}
	}
}
