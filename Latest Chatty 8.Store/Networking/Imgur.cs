using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Latest_Chatty_8.Networking
{
	public static class Imgur
	{

		private static readonly List<Single> QualitySteps = new List<Single>
		{
			.95f,
			.9f,
			.8f,
			.65f,
			.5f,
			.2f,
			.1f
		};

		private const int MaxSize = 10485760;

#if !WINDOWS_PHONE_APP
		/// <summary>
		/// Prompts user to pick a file and upload it.
		/// </summary>
		/// <returns>URL to file on success, empty string on fail or cancel.</returns>
		public async static Task<string> UploadPhotoUsingPicker()
		{
			try
			{
				var picker = new FileOpenPicker();
				picker.ViewMode = PickerViewMode.Thumbnail;
				picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
				picker.FileTypeFilter.Add(".jpg");
				picker.FileTypeFilter.Add(".jpeg");
				picker.FileTypeFilter.Add(".heic");
				picker.FileTypeFilter.Add(".gif");
				picker.FileTypeFilter.Add(".png");
				picker.FileTypeFilter.Add(".bmp");
				var pickedFile = await picker.PickSingleFileAsync();

				if (pickedFile != null)
				{
					return await UploadPhoto(pickedFile);
				}
			}
			catch (Exception ignored)
			{ Debug.Assert(false); }
			return string.Empty;
		}
#endif

		public async static Task<string> UploadPhoto(StorageFile pickedFile)
		{
			byte[] fileData;
			if ((await pickedFile.GetBasicPropertiesAsync()).Size > MaxSize)
			{
				fileData = await ResizeImage(pickedFile);
			}
			else
			{
				fileData = await GetFileBytes(pickedFile);
			}

			if (fileData != null)
			{
				var isPng = pickedFile.FileType.Equals(".png", StringComparison.OrdinalIgnoreCase);
				using (var formContent = new MultipartFormDataContent())
				{
					using (var content = new ByteArrayContent(fileData))
					{
						//content.Headers.ContentType = new MediaTypeHeaderValue(string.Format("image/{0}", isPng ? "png" : "jpeg"));
						formContent.Add(content, "image", "LCUWP" + Guid.NewGuid());
						using (var client = new HttpClient())
						{
							using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.imgur.com/3/image"))
							{
								//Set this environment variable
								var clientId = Environment.GetEnvironmentVariable("IMGUR_CLIENT_ID");
								if (clientId == null)
								{
									clientId = "{{IMGUR_CLIENT_ID}}";
								}
								httpRequest.Headers.Authorization = AuthenticationHeaderValue.Parse($"Client-ID {clientId}");
								httpRequest.Content = content;
								using (var response = client.SendAsync(httpRequest).Result)
								{
									var s = await response.Content.ReadAsStringAsync();
									Debug.WriteLine("Imgur result: " + s);
									var result = JObject.Parse(s);
									if (result["data"]["gifv"] != null)
									{
										return result["data"]["gifv"].Value<string>();
									}
									return result["data"]["link"].Value<string>();
								}
							}
						}
					}
				}
			}
			return string.Empty;
		}

		private static async Task<byte[]> GetFileBytes(StorageFile pickedFile)
		{
			using (var _ = await pickedFile.OpenStreamForReadAsync())
			{
				using (var reader = await pickedFile.OpenStreamForReadAsync())
				{
					var fileData = new byte[reader.Length];
					await reader.ReadAsync(fileData, 0, fileData.Length);
					return fileData;
				}
			}
		}


		private static async Task<byte[]> ResizeImage(StorageFile pickedFile)
		{
			using (var originalImageStream = await pickedFile.OpenReadAsync())
			{
				var decoder = await BitmapDecoder.CreateAsync(originalImageStream);
				var transform = new BitmapTransform
				{
					ScaledHeight = decoder.PixelHeight,
					ScaledWidth = decoder.PixelWidth
				};

				var pixelProvider = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, transform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
				var pixelData = pixelProvider.DetachPixelData();

				foreach (var quality in QualitySteps)
				{
					using (var newImageStream = new InMemoryRandomAccessStream())
					{
						var propertySet = new BitmapPropertySet();
						var qualityValue = new BitmapTypedValue(quality, PropertyType.Single);
						propertySet.Add("ImageQuality", qualityValue);
						var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, newImageStream, propertySet);
						encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, decoder.OrientedPixelWidth, decoder.OrientedPixelHeight, decoder.DpiX, decoder.DpiY, pixelData);
						await encoder.FlushAsync();

						if (newImageStream.Size < MaxSize)
						{
							var newData = new byte[newImageStream.Size];
							await newImageStream.AsStream().ReadAsync(newData, 0, newData.Length);
							return newData;
						}
					}
				}
				return null;
			}
		}
	}
}
