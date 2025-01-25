using NAudio.Wave;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace VirtualKeySpeaker
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		MainWindow mainWindow { get; set; }

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
		}

		public void SetLangBoxName(string text)
		{
			langBox.Text = text;
		}

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

			mainWindow.SetSpeech(langBox.SelectedItem.ToString().Split('|')[0]);
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
	}
}
