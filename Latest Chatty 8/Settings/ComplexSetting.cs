using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Latest_Chatty_8.Settings
{
	public static class ComplexSetting
	{
		public static async Task<T> ReadSetting<T>(string name)
		{
			var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(name + ".xml", CreationCollisionOption.OpenIfExists);
			if (file == null)
				return default(T);
			IInputStream fileStream = await file.OpenReadAsync();
			if (((IRandomAccessStreamWithContentType)fileStream).Size > 0)
			{
				var serializer = new DataContractSerializer(typeof(T), new Type[] { typeof(T) });
				try
				{
					return (T)serializer.ReadObject(fileStream.AsStreamForRead());
				}
				catch (Exception e)
				{
					System.Diagnostics.Debug.WriteLine("Exception on reading setting. {0}", e);
				}
			}
			return default(T);
		}

		public static async void SetSetting<T>(string name, T value)
		{
			var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(name + ".xml", CreationCollisionOption.ReplaceExisting);
			var randomAccess = await file.OpenAsync(FileAccessMode.ReadWrite);
			IOutputStream fileStream = randomAccess.GetOutputStreamAt(0);
			var serializer = new DataContractSerializer(typeof(T), new Type[] { typeof(T) });
			serializer.WriteObject(fileStream.AsStreamForWrite(), value);
			await fileStream.FlushAsync();
		}
	}
}
