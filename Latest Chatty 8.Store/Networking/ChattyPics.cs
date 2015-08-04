using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Latest_Chatty_8.Networking
{
	public static class ChattyPics
	{

#if !WINDOWS_PHONE_APP
		/// <summary>
		/// Prompts user to pick a file and upload it.
		/// </summary>
		/// <returns>URL to file on success, empty string on fail or cancel.</returns>
		async public static Task<string> UploadPhotoUsingPicker()
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

		async public static Task<string> UploadPhoto(Windows.Storage.StorageFile pickedFile)
		{
			if ((await pickedFile.GetBasicPropertiesAsync()).Size > 3145728)
			{
				var dialog = new Windows.UI.Popups.MessageDialog("Files must be smaller than 3MB to use ChattyPics.");
				await dialog.ShowAsync();
			}
			else
			{
				var isPng = pickedFile.FileType.Equals(".png", StringComparison.OrdinalIgnoreCase);
				using (var readStream = await pickedFile.OpenStreamForReadAsync())
				{
					byte[] buffer;
					using (var reader = await pickedFile.OpenStreamForReadAsync())
					{
						buffer = new byte[reader.Length];
						await reader.ReadAsync(buffer, 0, buffer.Length);
					}
					using (var formContent = new MultipartFormDataContent())
					{
						using (var content = new ByteArrayContent(buffer))
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
			}
			return string.Empty;
		}
	}
}
