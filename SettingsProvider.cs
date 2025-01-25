using SettingsProviderNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace VirtualKeySpeaker
{
	public class VKSSettings
	{
		[DefaultValue("ru-Ru")]
		public string Language { get; set; }

		[DefaultValue("Русский")]
		public string LangName { get; set; }

		[DefaultValue(new [] { Keys.RControlKey })]
		public Keys[] SpeakKeys { get; set; }

		[DefaultValue(new[] { Keys.RMenu })]
		public Keys[] ClearKeys { get; set; }
	}
}
