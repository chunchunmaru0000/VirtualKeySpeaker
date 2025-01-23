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

namespace VirtualKeySpeaker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region VAR
		SpeechSynthesizer synthesizer { get; set; }
		MemoryStream soundStream { get; set; }
		IKeyboardEvents hook { get; set; }
		bool isSpeaking { get; set; }
		int deviceIndex { get; set; }
		int systemDeviceIndex { get; set; }
		WaveOutEvent waveOutEvent { get; set; }
		WaveOutEvent systemSound { get; set; }
		BufferedWaveProvider waveProvider { get; set; }
		BufferedWaveProvider systemWaveProvider { get; set; }
		#endregion
		#region VAR_SETTINGS
		List<Keys> speakKeys { get; set; }
		List<Keys> clearKeys { get; set; }
		string culture { get; set; }
		VoiceGender voiceGender { get; set; }
		VoiceAge voiceAge { get; set; }
		TimeSpan fadeTime = TimeSpan.FromSeconds(15);
		#endregion
		#region CONST
		const string deviceName = "CABLE Input";
		const string systemDeviceName = "3S Stereo";
		#endregion

		public MainWindow()
		{
			InitializeComponent();
			InitSettings();
			InitHookAndSpeech();
			InitMicro();
		}


		#region INITS
		private void InitMicro()
		{
			for (int i = 0; i < WaveOut.DeviceCount; i++)
			{
				WaveOutCapabilities data = WaveOut.GetCapabilities(i);
					Console.WriteLine(data.ProductName);
				if (data.ProductName.Contains(deviceName))
					deviceIndex = i;
				else if (data.ProductName.Contains(systemDeviceName))
					systemDeviceIndex = i;
			}

			waveOutEvent = new WaveOutEvent() { DeviceNumber = deviceIndex };
			waveProvider = new BufferedWaveProvider(new WaveFormat(16000, 1));
			waveOutEvent.Init(waveProvider);
			waveOutEvent.Play();

			systemSound = new WaveOutEvent() { DeviceNumber = systemDeviceIndex };
			systemWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 1));
			systemSound.Init(systemWaveProvider);
			systemSound.Play();
		}

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

			soundStream = new MemoryStream();
			synthesizer.SetOutputToAudioStream(
				soundStream, 
				new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono)
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
			string text = string.Join("", InputKey.KeysStream.Select(k => k.Key));
			outLabel.Content = text;
		}

		#region HOOK
		private void HookKeyDownTxt(object sender, KeyDownTxtEventArgs e)
		{
			if (e.Chars.Length > 0 && e.Chars[0] != 8) // backspacve also text
			{
				InputKey.KeysStream.Add(new InputKey(e.Chars, fadeTime));
				DrawText();
			}
		}


		private void HookKeyDown(object sender, KeyEventArgs e)
		{
			if (speakKeys.Contains(e.KeyCode))
				Speak();
			else if (clearKeys.Contains(e.KeyCode))
				Clear();
			else if (e.KeyCode == Keys.Back)
				DeleteLast();
		}

		void Speak()
		{
			if (!isSpeaking)
			{
				string text = string.Join("", InputKey.KeysStream.Select(k => k.Key));
				Clear();

				synthesizer.Speak(text);
				Console.WriteLine(text);

				soundStream.Seek(0, SeekOrigin.Begin);
				byte[] buffer = soundStream.ToArray();

				waveProvider.AddSamples(buffer, 0, buffer.Length);
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
	}
}
