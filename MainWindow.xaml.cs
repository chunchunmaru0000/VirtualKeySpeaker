using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Speech.Synthesis;
using System.Globalization;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;
using NAudio.Wave;
using System.IO;
using System.Speech.AudioFormat;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SettingsProviderNet;
using System.Diagnostics;

namespace VirtualKeySpeaker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region VAR
		public SettingsProvider settingsProvider { get; set; }
		public VKSSettings settings { get; set; }
		SettingsWindow settingsWindow { get; set; }
		SpeechSynthesizer synthesizer { get; set; }
		MemoryStream soundStream { get; set; }
		IKeyboardEvents hook { get; set; }
		int deviceIndex { get; set; }
		int systemDeviceIndex { get; set; }
		public WaveOutEvent waveOutEvent { get; set; }
		public WaveOutEvent systemSound { get; set; }
		public BufferedWaveProvider waveProvider { get; set; }
		public BufferedWaveProvider systemWaveProvider { get; set; }
		#endregion
		#region VAR_SETTINGS
		public Keys speakKeys { get; set; }
		public Keys clearKeys { get; set; }
		string culture { get; set; }
		VoiceGender voiceGender { get; set; }
		VoiceAge voiceAge { get; set; }
		TimeSpan fadeTime { get; } = TimeSpan.FromSeconds(60);
		string Text { get => string.Join("", InputKey.KeysStream.Select(k => k.Key)); }
		public bool isSelectingKey { get; set; }
		#endregion
		#region CONST
		#endregion

		public MainWindow()
		{
			InitializeComponent();
			InitSettings(this);
			InitSettings();
			InitHookAndSpeech();
			InitTextWindow();
		}

		#region INITS
		private void InitSettings(MainWindow mainWindow)
		{
			settingsWindow = new SettingsWindow(mainWindow);
			settingsWindow.Show();
		}

		private void InitTextWindow()
		{
			Left = SystemParameters.PrimaryScreenWidth - Width;
			Top = SystemParameters.PrimaryScreenHeight - Height - 40;
		}

		private void InitSettings()
		{
			RoamingAppDataStorage storage = new RoamingAppDataStorage("VKS");
			
			settingsProvider = new SettingsProvider(new RoamingAppDataStorage("VKS"));
			settings = settingsProvider.GetSettings<VKSSettings>();
			settingsWindow.SetLangBoxName($"{settings.Language}|{settings.LangName}");
			//settingsWindow.SetLangBoxUnEditable();

			speakKeys = settings.SpeakKeys;
			settingsWindow.SetKeyLabelTextKey(speakKeys.ToString());

			clearKeys = settings.ClearKeys;
			settingsWindow.SetClearKeyLabelTextKey(clearKeys.ToString());
			
			culture = settings.Language;
			voiceGender = VoiceGender.Female;
			voiceAge = VoiceAge.Adult;

			SetMicro(settings.InputDevice);
			settingsWindow.SetInputDeviceText(settings.InputDevice);

			SetSpeaker(settings.OutDevice);
			settingsWindow.SetOurDeviceText(settings.OutDevice);
		}
		#endregion
		#region SET
		private void InitHookAndSpeech()
		{
			hook = Hook.GlobalEvents();
			hook.KeyDownTxt += HookKeyDownTxt;
			hook.KeyDown += HookKeyDown;

			SetSpeech(culture);
		}

		public void SetMicro(string name)
		{
			bool success = true;
			try
			{
				waveOutEvent?.Dispose();

				for (int i = 0; i < WaveOut.DeviceCount; i++)
					if (WaveOut.GetCapabilities(i).ProductName.Contains(name))
						deviceIndex = i;

				waveOutEvent = new WaveOutEvent() { DeviceNumber = deviceIndex };
				waveProvider = new BufferedWaveProvider(new WaveFormat(16000, 1)) { BufferDuration = TimeSpan.FromMinutes(1) };
				waveOutEvent.Init(waveProvider);
				waveOutEvent.Play();

				settings.InputDevice = name;
				SaveSettings();
			}
			catch { success = false; }

			settingsWindow.SetMicroRect(success);
			Console.WriteLine($"{name} {success}");
		}

		public void SetSpeaker(string name)
		{
			bool success = true;
			try
			{
				systemSound?.Dispose();
				for (int i = 0; i < WaveOut.DeviceCount; i++)
					if (WaveOut.GetCapabilities(i).ProductName.Contains(name))
						systemDeviceIndex = i;

				systemSound = new WaveOutEvent() { DeviceNumber = systemDeviceIndex };
				systemWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 1)) { BufferDuration = TimeSpan.FromMinutes(1) };
				systemSound.Init(systemWaveProvider);
				systemSound.Play();

				settings.OutDevice = name;
				SaveSettings();
			}
			catch { success = false; }

			settingsWindow.SetSpeakerRect(success);
			Console.WriteLine($"{name} {success}");
		}

		public void SaveSettings() => settingsProvider.SaveSettings(settings);

		public void SetSpeech(string culture)
		{
			synthesizer?.Dispose();

			synthesizer = new SpeechSynthesizer();
			synthesizer.SelectVoiceByHints(
				voiceGender,
				voiceAge,
				0,
				new CultureInfo(culture)
			);

			soundStream = new MemoryStream();
			synthesizer.SetOutputToAudioStream(
				soundStream,
				new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono)
			);
		}
		#endregion
		#region DRAW
		private void DrawText()
		{
			outLabel.Content = Text.Replace("\r", "").Replace("\n", "");
		}
		#endregion
		// todo: better keyhook
		// todo: speak buffer
		#region HOOK
		private void HookKeyDownTxt(object sender, KeyDownTxtEventArgs e)
		{
			if (e.Chars.Length > 0 && 
				e.Chars[0] != 8 // backspacve also text
				)
			{
				InputKey.KeysStream.Add(new InputKey(e.Chars, fadeTime));
				DrawText();
			}
		}

		private void HookKeyDown(object sender, KeyEventArgs e)
		{
			if (isSelectingKey)
				settingsWindow.SelectedKey(e.KeyCode);
			if (speakKeys == e.KeyCode)
				Speak();
			else if (clearKeys == e.KeyCode)
				Clear();
			else if (e.KeyCode == Keys.Back)
				DeleteLast();
		}

		void Speak()
		{
			if ( 
				waveProvider != null && 
				systemWaveProvider != null
				)
			{
				string text = Text;
				Clear();

				// todo: dont clear stream and just next messages play after this
				soundStream.SetLength(0);
				synthesizer.Speak(text);
				Console.WriteLine(text);

				soundStream.Seek(0, SeekOrigin.Begin);
				byte[] buffer = soundStream.ToArray();

				if (waveProvider.BufferLength - waveProvider.BufferedBytes < buffer.Length)
					return;

				waveProvider.ClearBuffer();
				waveProvider.AddSamples(buffer, 0, buffer.Length);

				systemWaveProvider.ClearBuffer();
				systemWaveProvider.AddSamples(buffer, 0, buffer.Length);
			}
		}

		void DeleteLast()
		{
			InputKey.DeleteLast();
			DrawText();
		}

		void Clear()
		{
			outLabel.Content = "";
			InputKey.Clear();
			DrawText();
		}
		#endregion
		#region OVERRIDE
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			var hwnd = new WindowInteropHelper(this).Handle;
			WindowsServices.SetWindowExTransparent(hwnd);
		}
		#endregion
	}
}
