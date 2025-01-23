using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Controls;

namespace VirtualKeySpeaker
{
	internal class InputKey : IDisposable
	{
		public static List<InputKey> KeysStream = new List<InputKey>();
		public string Key { get; set; }
		Timer CeaseTimer { get; set; }
		// static
		static readonly TimeSpan MinusOneMilisecond = TimeSpan.FromMilliseconds(-1);

		public InputKey(string key, TimeSpan fadeTime)
		{
			Key = key;
			CeaseTimer = new Timer(CeaseTimerCallback, null, fadeTime, MinusOneMilisecond);
		}

		private void CeaseTimerCallback(object state)
		{
			if (KeysStream.Count > 0)
				KeysStream.RemoveAt(0);
			Dispose();
		}

		public static void DeleteLast()
		{
			if (KeysStream.Count > 0)
			{
				KeysStream.Last().Dispose();
				KeysStream.RemoveAt(KeysStream.Count - 1);
			}
		}

		public static void Clear()
		{
			foreach (InputKey key in KeysStream)
				key.Dispose();
			KeysStream.Clear();
		}

		public void Dispose()
		{
			CeaseTimer?.Dispose();
		}
	}
}
