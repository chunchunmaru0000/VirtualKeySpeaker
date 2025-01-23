using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Speech.Synthesis;
using System.Globalization;
using System.Threading;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;

namespace VirtualKeySpeaker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region VAR
		SpeechSynthesizer synthesizer { get; set; }
		IKeyboardEvents hook { get; set; }
		bool isSpeaking { get; set; }
		#endregion
		#region VAR_SETTINGS
		List<Keys> speakKeys { get; set; }
		List<Keys> clearKeys { get; set; }
		string culture { get; set; }
		VoiceGender voiceGender { get; set; }
		VoiceAge voiceAge { get; set; }
		TimeSpan fadeTime = TimeSpan.FromSeconds(15);
		#endregion

		public MainWindow()
		{
			InitializeComponent();
			InitSettings();
			InitHookAndSpeech();
		}

		#region INITS
		private void InitSettings()
		{
			speakKeys = new List<Keys> { Keys.RControlKey };
			clearKeys = new List<Keys> { Keys.RMenu };

			culture = "ru-Ru";
			voiceGender = VoiceGender.Female;
			voiceAge = VoiceAge.Adult;
		}

		private void InitHookAndSpeech()
		{
			hook = Hook.GlobalEvents();
			hook.KeyDownTxt += HookKeyDownTxt;
			hook.KeyDown += HookKeyDown;

			synthesizer = new SpeechSynthesizer();
			synthesizer.SelectVoiceByHints(
				voiceGender,
				voiceAge, 
				0,
				new CultureInfo(culture)
			);

			/*
			synthesizer.SpeakStarted += (sender, e) => isSpeaking = true;
			synthesizer.StateChanged += (sender, e) =>
			{
				if (e.State == SynthesizerState.)
					isSpeaking = false;
			};*/
		}
		#endregion

		private void DrawText()
		{
			
		}

		#region HOOK
		private void HookKeyDownTxt(object sender, KeyDownTxtEventArgs e)
		{
			InputKey.KeysStream.Add(new InputKey(e.Chars, fadeTime));
			DrawText();
		}


		private void HookKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (speakKeys.Contains(e.KeyCode))
				Speak();
			else if (clearKeys.Contains(e.KeyCode))
				InputKey.Clear();
		}

		void Speak()
		{
			if (!isSpeaking)
			{
				synthesizer.Speak(string.Join("", InputKey.KeysStream.Select(k => k.Key)));
				InputKey.Clear();
			}
		}
		#endregion
	}
}
