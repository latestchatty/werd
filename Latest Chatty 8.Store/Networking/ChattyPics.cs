using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;

namespace Latest_Chatty_8.Networking
{
	public static class ChattyPics
	{

		private static List<Single> QualitySteps = new List<Single>
		{
			.95f,
			.9f,
			.8f,
			.65f,
			.5f,
			.2f,
			.1f
		};

		private const int MAX_SIZE = 5242880;

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
				//picker.FileTypeFilter.Add(".gif");
				picker.FileTypeFilter.Add(".png");
				//picker.FileTypeFilter.Add(".bmp");
				var pickedFile = await picker.PickSingleFileAsync();

				if (pickedFile != null)
				{
					return await UploadPhoto(pickedFile);
				}
			}
			catch
			{ System.Diagnostics.Debug.Assert(false); }
			return string.Empty;
		}
#endif

		public async static Task<string> UploadPhoto(Windows.Storage.StorageFile pickedFile)
		{
			byte[] fileData = null;
			if ((await pickedFile.GetBasicPropertiesAsync()).Size > MAX_SIZE)
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
						content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.Format("image/{0}", isPng ? "png" : "jpeg"));
						formContent.Add(content, "userfile[]", "LC8" + (isPng ? ".png" : ".jpg"));
						using (var client = new HttpClient())
						{
							using (var response = client.PostAsync("http://chattypics.com/upload.php", formContent).Result)
							{
								var s = await response.Content.ReadAsStringAsync();
								var match = Regex.Match(s, "http://chattypics\\.com/files/LC8_[^_]+\\" + (isPng ? ".png" : ".jpg"));
								if (match.Groups.Count == 1)
								{
									return match.Groups[0].ToString();
								}
							}
						}
					}
				}
			}
			return string.Empty;
		}

		private static async Task<byte[]> GetFileBytes(Windows.Storage.StorageFile pickedFile)
		{
			using (var readStream = await pickedFile.OpenStreamForReadAsync())
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
						var qualityValue = new BitmapTypedValue(quality, Windows.Foundation.PropertyType.Single);
						propertySet.Add("ImageQuality", qualityValue);
						var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, newImageStream, propertySet);
						encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, decoder.OrientedPixelWidth, decoder.OrientedPixelHeight, decoder.DpiX, decoder.DpiY, pixelData);
						await encoder.FlushAsync();

						if (newImageStream.Size < MAX_SIZE)
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
