using NAudio.Wave;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace VirtualKeySpeaker
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		MainWindow mainWindow { get; set; }
		SelectingKey selectingKey { get; set; } = SelectingKey.None;
		#region CONST
		const string keyLabelText = "Speak key";
		const string clearKeyLabelText = "Clear key";
		const string bufferKeyLabelText = "Buffer speak key";
		#endregion

		public SettingsWindow(MainWindow mainWindow)
		{
			InitializeComponent();
			this.mainWindow = mainWindow;

			InitSomeBoxes();
		}

		#region INITS
		private void InitSomeBoxes()
		{
			foreach (CultureInfo cultureInfo in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
				langBox.Items.Add($"{cultureInfo.Name}|{cultureInfo.DisplayName}");
		}
		#endregion
		#region SET_TEXT
		public void SetLangBoxName(string text) 
		{
			langBox.Items.Add(text);
			langBox.SelectedIndex = langBox.Items.Count - 1;
		}

		public void SetKeyLabelTextKey(string text) => keyLabel.Content = $"{keyLabelText}: {text}";

		public void SetClearKeyLabelTextKey(string text) => clearKeyLabel.Content = $"{clearKeyLabelText}: {text}";

		public void SetBufferKeyLabelTextKey(string text) => bufferKeyLabel.Content = $"{bufferKeyLabelText}: {text}";
		#endregion
		#region HANDLERS
		private void Window_Closed(object sender, EventArgs e)
		{
			App.Current.Shutdown(0);
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

		private void microsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (microsBox.SelectedItem == null)
				return;

			mainWindow.SetMicro(microsBox.SelectedItem.ToString());
			Focus();
		}

		private void speakersBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (speakersBox.SelectedItem == null)
				return;

			mainWindow.SetSpeaker(speakersBox.SelectedItem.ToString());
			Focus();
		}

		private void langBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (langBox.SelectedItem == null && langBox.Items.Count == 1)
				return;

			string[] lang = langBox.SelectedItem.ToString().Split('|');

			mainWindow.SetSpeech(lang[0]);

			mainWindow.settings.Language = lang[0];
			mainWindow.settings.LangName = lang[1];
			mainWindow.SaveSettings();

			Focus();
		}
		#endregion
		#region SELECTS
		public void SetMicroRect(bool success, string name, bool setBox = false)
		{
			if (success)
			{
				microRect.Fill = Brushes.LightGreen;
				if (setBox)
				{
					microsBox.Items.Add(name);
					microsBox.SelectedIndex = microsBox.Items.Count - 1;
				}
			}
			else
				microRect.Fill = Brushes.LightCoral;
		}

		public void SetSpeakerRect(bool success, string name, bool setBox = false)
		{
			if (success)
			{
				speakerRect.Fill = Brushes.LightGreen;
				if (setBox)
				{
					speakersBox.Items.Add(name);
					speakersBox.SelectedIndex = speakersBox.Items.Count - 1;
				}
			}
			else
				speakerRect.Fill = Brushes.LightCoral;
		}

		public void SetSpeechLength(double minutes) =>
			lenBox.SelectedIndex = Enumerable
				.Range(0, lenBox.Items.Count)
				.FirstOrDefault(i => (lenBox.Items[i] as ComboBoxItem).Content.ToString() == Convert.ToInt32(minutes).ToString());

		void SelectSelectingKey(System.Windows.Shapes.Rectangle rect, SelectingKey key)
		{
			if (mainWindow.isSelectingKey)
				return;

			mainWindow.isSelectingKey = true;
			rect.Fill = Brushes.LightGreen;
			selectingKey = key;
		}

		private void SelectKey(object sender, RoutedEventArgs e) => SelectSelectingKey(keyRect, SelectingKey.Speak);

		private void SelectClearKey(object sender, RoutedEventArgs e) => SelectSelectingKey(clearKeyRect, SelectingKey.Clear);

		private void SelectBufferKey(object sender, RoutedEventArgs e) => SelectSelectingKey(bufferKeyRect, SelectingKey.Buffer);

		public void SelectedKey(Keys key)
		{
			mainWindow.isSelectingKey = false;
			Console.WriteLine(key);

			switch (selectingKey)
			{
				case SelectingKey.Speak:
					keyRect.Fill = Brushes.LightCoral;
					keyLabel.Content = $"{keyLabelText}: {key}";

					mainWindow.settings.SpeakKeys = key;
					mainWindow.speakKeys = key;
					break;
				case SelectingKey.Clear:
					clearKeyRect.Fill = Brushes.LightCoral;
					clearKeyLabel.Content = $"{clearKeyLabelText}: {key}";
					
					mainWindow.settings.ClearKeys = key;
					mainWindow.clearKeys = key;
					break;
				case SelectingKey.Buffer:
					bufferKeyRect.Fill = Brushes.LightCoral;
					bufferKeyLabel.Content = $"{bufferKeyLabelText}: {key}";

					mainWindow.settings.BufferKeys = key;
					mainWindow.bufferKeys = key;
					break;
				case SelectingKey.None:
					break;
				default:
					break;
			}
			
			mainWindow.SaveSettings();
		}

		private void lenBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			double minutes = Convert.ToDouble((lenBox.SelectedItem as ComboBoxItem).Content);
			mainWindow.settings.SpeechLength = minutes;
			mainWindow.SaveSettings();

			if (mainWindow.waveProvider == null || mainWindow.systemWaveProvider == null)
				return;
			mainWindow.waveProvider.BufferDuration = TimeSpan.FromMinutes(minutes);
			mainWindow.systemWaveProvider.BufferDuration = TimeSpan.FromMinutes(minutes);
		}
		#endregion
	}

	enum SelectingKey
	{
		Speak,
		Clear,
		Buffer,
		None,
	}
}
