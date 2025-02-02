﻿using System;
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
using System.Threading;

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
		public WaveOutEvent waveOutEvent;
		public WaveOutEvent systemSound;
		public BufferedWaveProvider waveProvider;
		public BufferedWaveProvider systemWaveProvider;
		string lastClipboardText { get; set; } = "";
		bool hideOutLabel { get; set; }
		#endregion
		#region VAR_SETTINGS
		public Keys speakKeys { get; set; }
		public Keys clearKeys { get; set; }
		public Keys bufferKeys { get; set; }
		string culture { get; set; }
		VoiceGender voiceGender { get; set; }
		VoiceAge voiceAge { get; set; }
		TimeSpan fadeTime { get; } = TimeSpan.FromSeconds(600);
		TimeSpan bufferDuration { get; set; }
		string Text { get => string.Join("", InputKey.KeysStream.Select(k => k.Key)); }
		public bool isSelectingKey { get; set; }
		#endregion

		public MainWindow()
		{
			InitializeComponent();
			InitClipboard();
			InitSettings(this);
			InitSettings();
			InitHookAndSpeech();
			InitTextWindow();
		}

		#region INITS
		private void InitClipboard()
		{
			Thread clipboardThread = new Thread(() =>
			{
				while (true)
				{
					try
					{
						if (System.Windows.Clipboard.ContainsText())
						{
							string text = System.Windows.Clipboard.GetText();
							if (text != lastClipboardText)
							{
								lastClipboardText = text;
								Console.WriteLine(lastClipboardText);
							}	
						}
					} catch { }
					Thread.Sleep(50);
				}
			});

			clipboardThread.SetApartmentState(ApartmentState.STA);
			clipboardThread.Start();
		}

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

			#region KEYS
			speakKeys = settings.SpeakKeys;
			settingsWindow.SetKeyLabelTextKey(speakKeys.ToString());

			clearKeys = settings.ClearKeys;
			settingsWindow.SetClearKeyLabelTextKey(clearKeys.ToString());

			bufferKeys = settings.BufferKeys;
			settingsWindow.SetBufferKeyLabelTextKey(bufferKeys.ToString());
			#endregion
			culture = settings.Language;
			voiceGender = VoiceGender.Female;
			voiceAge = VoiceAge.Adult;

			bufferDuration = TimeSpan.FromMinutes(settings.SpeechLength);
			settingsWindow.SetSpeechLength(settings.SpeechLength);

			SetMicro(settings.InputDevice, true);
			SetSpeaker(settings.OutDevice, true);
		}

		private void InitHookAndSpeech()
		{
			hook = Hook.GlobalEvents();
			hook.KeyDownTxt += HookKeyDownTxt;
			hook.KeyDown += HookKeyDown;

			SetSpeech(culture);
		}
		#endregion
		#region SET
		private int GetDeviceIndex(string name)
		{
			for (int i = 0; i < WaveOut.DeviceCount; i++)
				if (WaveOut.GetCapabilities(i).ProductName.Contains(name))
					return i;
			return -1;
		}

		private bool SetDevice(string name, ref WaveOutEvent waveOutEvent, ref BufferedWaveProvider waveProvider)
		{
			if (name.Length == 0)
				return false;

			bool success = true;
			try
			{
				waveOutEvent?.Dispose();

				int deviceIndex = GetDeviceIndex(name);

				waveOutEvent = new WaveOutEvent() { DeviceNumber = deviceIndex };
				waveProvider = new BufferedWaveProvider(new WaveFormat(16000, 1))
				{
					BufferDuration = bufferDuration,
					DiscardOnBufferOverflow = true
				};
				waveOutEvent.Init(waveProvider);
				waveOutEvent.Play();
			}
			catch { success = false; }

			Console.WriteLine($"{name} {success}");
			return success;
		}
		
		public void SetMicro(string name, bool setBox = false)
		{
			bool success = SetDevice(name, ref waveOutEvent, ref waveProvider);
			settingsWindow.SetMicroRect(success, name, setBox);
			if (success)
			{
				settings.InputDevice = name;
				SaveSettings();
			}
		}

		public void SetSpeaker(string name, bool setBox = false)
		{
			bool success = SetDevice(name, ref systemSound, ref systemWaveProvider);
			settingsWindow.SetSpeakerRect(success, name, setBox);
			if (success)
			{
				settings.OutDevice = name;
				SaveSettings();
			}
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
			if (hideOutLabel)
			{
				hideOutLabel = false;
				return;
			}
			if (outLabel.Height == 0)
				outLabel.Height = 75;
		}
		#endregion
		// todo: better keyhook
		#region HOOK
		private void HookKeyDownTxt(object sender, KeyDownTxtEventArgs e)
		{
			if (e.Chars.Length > 0 && 
				e.Chars[0] != 8 && // backspacve also text
				e.Chars[0] != '\t'
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
			else if (bufferKeys == e.KeyCode)
				Speak(lastClipboardText);
			else if (speakKeys == e.KeyCode)
				Speak();
			else if (clearKeys == e.KeyCode)
				Clear();
			else if (e.KeyCode == Keys.Back)
				DeleteLast();
		}

		void Speak(string spokenText = "")
		{
			if ( 
				waveProvider != null && 
				systemWaveProvider != null
				)
			{
				string text = spokenText == "" ? Text : spokenText;
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
			if (InputKey.KeysStream.Count == 0)
				outLabel.Height = 0;
			hideOutLabel = true;
			DrawText();
		}

		void Clear()
		{
			outLabel.Content = "";
			outLabel.Height = 0;
			InputKey.Clear();
			hideOutLabel = true;
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
