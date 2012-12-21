using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;

namespace Latest_Chatty_8.Networking
{
	public static class ChattyPics
	{
		/// <summary>
		/// Prompts user to pick a file and upload it.
		/// </summary>
		/// <returns>URL to file on success, empty string on fail or cancel.</returns>
		async public static Task<string> UploadPhoto()
		{
			var photoUrl = string.Empty;
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
							using (var formContent = new MultipartFormDataContent())
							{
								byte[] buffer;
								using (var reader = await pickedFile.OpenStreamForReadAsync())
								{
									buffer = new byte[reader.Length];
									await reader.ReadAsync(buffer, 0, buffer.Length);
								}
								using (var content = new ByteArrayContent(buffer))
								{
									content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.Format("image/{0}", isPng ? "png" : "jpeg"));
									formContent.Add(content, "userfile[]", "LC8" + pickedFile.FileType);
									using (var client = new HttpClient())
									{
										using (var response = client.PostAsync("http://chattypics.com/upload.php", formContent).Result)
										{
											var s = await response.Content.ReadAsStringAsync();
											var match = Regex.Match(s, "http://chattypics\\.com/files/LC8_[^_]+\\" + pickedFile.FileType);
											if (match.Groups.Count == 1)
											{
												photoUrl = match.Groups[0].ToString();
											}
										}
									}
								}
							}
						}
					}
				}
			}
			catch { System.Diagnostics.Debug.Assert(false); }
			return photoUrl;
		}
	}
}
