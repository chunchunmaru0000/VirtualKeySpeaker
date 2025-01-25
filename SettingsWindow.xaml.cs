using NAudio.Wave;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace VirtualKeySpeaker
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		MainWindow mainWindow { get; set; }
		#region CONST
		const string keyLabelText = "Speak key";
		#endregion

		public SettingsWindow(MainWindow mainWindow)
		{
			InitializeComponent();
			this.mainWindow = mainWindow;

			InitSomeBoxes();
		}

		private void InitSomeBoxes()
		{
			foreach (CultureInfo cultureInfo in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
				langBox.Items.Add($"{cultureInfo.Name}|{cultureInfo.DisplayName}");

			keyLabel.Content = $"{keyLabelText}: {mainWindow.speakKeys}";
		}
		#region SET_TEXT
		public void SetLangBoxName(string text) => langBox.Text = text;

		public void SetKeyLabelTextKey(string text) => keyLabel.Content = $"{keyLabelText}: {text}";

		public void SetOurDeviceText(string text) => speakersBox.Text = text;

		public void SetInputDeviceText(string text) => microsBox.Text = text;

		public void SetLangBoxUnEditable() => langBox.IsEditable = false;
		#endregion
		private void Window_Closed(object sender, EventArgs e)
		{
			Environment.Exit(0);
		}

		private void speakersBox_DropDownOpened(object sender, EventArgs e)
		{
			speakersBox.Items.Clear();
			for (int i = 0; i < WaveOut.DeviceCount; i++)
			{
				WaveOutCapabilities data = WaveOut.GetCapabilities(i);
				speakersBox.Items.Add(data.ProductName);
			}
		}

		private void microsBox_DropDownOpened(object sender, EventArgs e)
		{
			microsBox.Items.Clear();
			for (int i = 0; i < WaveOut.DeviceCount; i++)
			{
				WaveOutCapabilities data = WaveOut.GetCapabilities(i);
				microsBox.Items.Add(data.ProductName);
			}
		}

		private void Window_StateChanged(object sender, EventArgs e)
		{
			if (WindowState == WindowState.Minimized)
			{
				//Hide();
			}
		}

		private void microsBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (microsBox.SelectedItem == null)
				return;

			mainWindow.SetMicro(microsBox.SelectedItem.ToString());
			Focus();
		}

		private void speakersBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (speakersBox.SelectedItem == null)
				return;

			mainWindow.SetSpeaker(speakersBox.SelectedItem.ToString());
			Focus();
		}

		private void langBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			if (langBox.SelectedItem == null)
				return;

			string[] lang = langBox.SelectedItem.ToString().Split('|');

			mainWindow.SetSpeech(lang[0]);

			mainWindow.settings.Language = lang[0];
			mainWindow.settings.Language = lang[1];
			mainWindow.SaveSettings();

			Focus();
		}

		public void SetMicroRect(bool success)
		{
			microRect.Fill = success ? Brushes.LightGreen : Brushes.LightCoral;
		}

		public void SetSpeakerRect(bool success)
		{
			speakerRect.Fill = success ? Brushes.LightGreen : Brushes.LightCoral;
		}

		private void SelectKey(object sender, RoutedEventArgs e)
		{
			mainWindow.isSelectingKey = true;
			keyRect.Fill = Brushes.LightGreen;
		}

		public void SelectedKey(Keys key)
		{
			mainWindow.isSelectingKey = false;
			keyRect.Fill = Brushes.LightCoral;

			Console.WriteLine(key);
			keyLabel.Content = $"{keyLabelText}: {key}";

			mainWindow.settings.SpeakKeys = key;
			mainWindow.speakKeys = key;
			mainWindow.SaveSettings();
		}
	}
}
