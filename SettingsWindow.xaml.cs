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

		public void SetLangBoxUnEditable() => langBox.IsEditable = false;
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

		private void SelectKey(object sender, RoutedEventArgs e)
		{
			mainWindow.isSelectingKey = true;
			keyRect.Fill = Brushes.LightGreen;
			selectingKey = SelectingKey.Speak;
		}

		private void SelectClearKey(object sender, RoutedEventArgs e)
		{
			mainWindow.isSelectingKey = true;
			clearKeyRect.Fill = Brushes.LightGreen;
			selectingKey = SelectingKey.Clear;
		}

		private void SelectBufferKey(object sender, RoutedEventArgs e)
		{
			mainWindow.isSelectingKey = true;
			bufferKeyRect.Fill = Brushes.LightGreen;
			selectingKey = SelectingKey.Buffer;
		}

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
		#endregion

		private void lenBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{

		}
	}

	enum SelectingKey
	{
		Speak,
		Clear,
		Buffer,
		None,
	}
}
