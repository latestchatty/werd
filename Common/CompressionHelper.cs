using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
	public static class CompressionHelper
	{
		public static string CompressStringToBase64(string data)
		{
			var b = Encoding.ASCII.GetBytes(data.ToString());
			string compressed;
			using (var compressedStream = new MemoryStream())
			using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
			{
				zipStream.Write(b, 0, b.Length);
				zipStream.Flush();
				compressed = Convert.ToBase64String(compressedStream.ToArray());
			}
			return compressed;
		}

		public static string DecompressStringFromBase64(string data)
		{
			using (var msi = new MemoryStream(Convert.FromBase64String(data)))
			using (var mso = new MemoryStream())
			{
				using (var gs = new GZipStream(msi, CompressionMode.Decompress))
				{
					gs.CopyTo(mso);
				}
				return Encoding.ASCII.GetString(mso.ToArray());
			}
		}
	}
}
