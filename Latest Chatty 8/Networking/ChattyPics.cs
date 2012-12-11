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

			var picker = new FileOpenPicker();
			picker.ViewMode = PickerViewMode.Thumbnail;
			picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
			picker.FileTypeFilter.Add(".jpg");
			picker.FileTypeFilter.Add(".jpeg");
			//picker.FileTypeFilter.Add(".gif");
			//picker.FileTypeFilter.Add(".png");
			//picker.FileTypeFilter.Add(".bmp");
			var pickedFile = await picker.PickSingleFileAsync();

			//if (pickedFile != null)
			//{
			//	var readStream = await pickedFile.OpenStreamForReadAsync();

			//	var formContent = new MultipartFormDataContent();
			//	var content = new StreamContent(readStream);
			//	formContent.Add(content, "userfile[]", "LC8.jpg");
			//	var message = new HttpRequestMessage(HttpMethod.Post, new Uri("http://chattypics.com/upload.php"));
			//	message.Content = content;
			//	var client = new HttpClient();
			//	var response = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead);
			//	var s = await response.Content.ReadAsStringAsync();
			//	var match = Regex.Match(s, "http://chattypics\\.com/files/LC8_[^_]+\\.jpg");
			//	if (match.Groups.Count == 1)
			//	{
			//		photoUrl = match.Groups[0].ToString();
			//	}
			//	readStream.Dispose();
			//}
			//return photoUrl;

			if (pickedFile != null)
			{
				try
				{
					char [] buffer;
					using (var reader = new StreamReader(await pickedFile.OpenStreamForReadAsync()))
					{
						buffer = new char[(await pickedFile.GetBasicPropertiesAsync()).Size];
						await reader.ReadAsync(buffer, 0, buffer.Length);
					}

					var request = (HttpWebRequest)HttpWebRequest.Create("http://chattypics.com/upload.php");
					request.Method = "POST";
					string boundary = "----------" + DateTime.Now.Ticks.ToString();
					boundary = "---------------------------7dc101112046e";
					request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);

					var requestStream = await request.GetRequestStreamAsync();
					StreamWriter streamWriter = new StreamWriter(requestStream);
					//await streamWriter.WriteAsync("--");
					//await streamWriter.WriteLineAsync(boundary);
					//await streamWriter.WriteLineAsync(@"Content-Disposition: form-data; name=""userfile[]""; filename=""LC8.jpg""");
					//await streamWriter.WriteLineAsync(@"Content-Type: image/jpeg");
					//await streamWriter.WriteLineAsync(@"Content-Length: " + buffer.Length);
					//await streamWriter.WriteLineAsync();
					//await streamWriter.FlushAsync();
					//await streamWriter.WriteAsync(buffer);
					//await streamWriter.FlushAsync();
					//await streamWriter.WriteLineAsync();
					//await streamWriter.WriteAsync("--");
					//await streamWriter.WriteAsync(boundary);
					//await streamWriter.WriteLineAsync("--");
					//await streamWriter.FlushAsync();
					await streamWriter.WriteLineAsync("--" + boundary);
					await streamWriter.WriteLineAsync(@"Content-Disposition: form-data; name=""type""");
					await streamWriter.WriteLineAsync();
					await streamWriter.WriteLineAsync("direct");
					await streamWriter.WriteLineAsync("--" + boundary);
					await streamWriter.WriteLineAsync(@"Content-Disposition: form-data; name=""userfile[]""; filename=""LC8.jpg""");
					await streamWriter.WriteLineAsync(@"Content-Type: image/jpeg");
					//await streamWriter.WriteLineAsync(@"Content-Length: " + buffer.Length);
					await streamWriter.WriteLineAsync();
					await streamWriter.FlushAsync();
					await streamWriter.WriteAsync(buffer, 0, buffer.Length);
					await streamWriter.WriteLineAsync();
					await streamWriter.WriteAsync("--" + boundary + "--");
					await streamWriter.WriteLineAsync();
					await streamWriter.FlushAsync();
					streamWriter.Dispose();
					var response = await request.GetResponseAsync() as HttpWebResponse;
					var responseReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
					var s = await responseReader.ReadToEndAsync();
					var match = Regex.Match(s, "http://chattypics\\.com/files/LC8_[^_]+\\.jpg");
					if (match.Groups.Count == 1)
					{
						photoUrl = match.Groups[0].ToString();
					}
				}
				catch (Exception e)
				{
					System.Diagnostics.Debug.WriteLine("Exception uploading image - {0}", e);
					throw;
				}
			}
			return photoUrl;
		}
	}
}
