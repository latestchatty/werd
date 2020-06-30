using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Werd.Settings
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
			var file = await ApplicationData.Current.RoamingFolder.CreateFileAsync(name + ".xml", CreationCollisionOption.OpenIfExists);
			if (file == null)
				return default(T);
			using (IInputStream fileStream = await file.OpenReadAsync())
			{
				var serializer = new DataContractSerializer(typeof(T), new[] { typeof(T) });
				try
				{
					var fileProperties = await file.GetBasicPropertiesAsync();
					if (fileProperties.Size > 0 && fileProperties.Size < uint.MaxValue)
					{
						using (var reader = new DataReader(fileStream))
						{
							await reader.LoadAsync((uint)fileProperties.Size);
							if (reader.UnconsumedBufferLength > 0)
							{
								var data = new byte[reader.UnconsumedBufferLength];
								reader.ReadBytes(data);
								using (var compressedMemoryStream = new MemoryStream(data))
								{
									using (var decompressionStream = new DeflateStream(compressedMemoryStream, CompressionMode.Decompress))
									{
										return (T)serializer.ReadObject(decompressionStream);
									}
								}
							}
						}
					}
				}
				catch (Exception e)
				{
					await AppGlobal.DebugLog.AddException("Exception on reading setting.", e);
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
			try
			{
				var file = await ApplicationData.Current.RoamingFolder.CreateFileAsync(name + ".xml", CreationCollisionOption.ReplaceExisting);
				using (var randomAccess = await file.OpenAsync(FileAccessMode.ReadWrite))
				{
					using (IOutputStream fileStream = randomAccess.GetOutputStreamAt(0))
					{
						var serializer = new DataContractSerializer(typeof(T), new[] { typeof(T) });
						using (var ms = new MemoryStream())
						{
							using (var compressionStream = new DeflateStream(ms, CompressionMode.Compress))
							{
								serializer.WriteObject(compressionStream, value);
							}
							using (var dw = new DataWriter(fileStream))
							{
								dw.WriteBytes(ms.ToArray());
								await dw.StoreAsync();
								await fileStream.FlushAsync();
							}
						}
					}
				}
			}
			catch (UnauthorizedAccessException)
			{ /* Ignore because someone else is already writing to the setting.  Ideally we'd lock, but... eh. */ }
		}
	}
}
