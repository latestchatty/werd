using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Latest_Chatty_8.Shared.Settings
{
	/// <summary>
	/// Object to help persist complex objects to disk via serialization
	/// </summary>
	public static class ComplexSetting
	{
		/// <summary>
		/// Reads the setting from disk.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name of the setting.</param>
		/// <returns></returns>
		public static async Task<T> ReadSetting<T>(string name)
		{
			var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(name + ".xml", CreationCollisionOption.OpenIfExists);
			if (file == null)
				return default(T);
			using (IInputStream fileStream = await file.OpenReadAsync())
			{
				if (((IRandomAccessStreamWithContentType)fileStream).Size > 0)
				{
					var serializer = new DataContractSerializer(typeof(T), new Type[] { typeof(T) });
					try
					{
						using (var readStream = fileStream.AsStreamForRead())
						{
							return (T)serializer.ReadObject(readStream);
						}
					}
					catch (Exception e)
					{
						System.Diagnostics.Debug.WriteLine("Exception on reading setting. {0}", e);
					}
				}
			}
			return default(T);
		}

		/// <summary>
		/// Persists the setting to disk.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name of the setting.</param>
		/// <param name="value">The value.</param>
		public static async Task SetSetting<T>(string name, T value)
		{
			var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(name + ".xml", CreationCollisionOption.ReplaceExisting);
			using (var randomAccess = await file.OpenAsync(FileAccessMode.ReadWrite))
			{
				using (IOutputStream fileStream = randomAccess.GetOutputStreamAt(0))
				{
					var serializer = new DataContractSerializer(typeof(T), new Type[] { typeof(T) });
					serializer.WriteObject(fileStream.AsStreamForWrite(), value);
					await fileStream.FlushAsync();
				}
			}
		}
	}
}
