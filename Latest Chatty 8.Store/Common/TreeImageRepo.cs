using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Latest_Chatty_8.Common
{
	public enum TreeIndicator
	{
		Empty,
		Passthrough,
		End,
		Junction
	}

	public static class TreeImageRepo
	{
		public static Dictionary<TreeIndicator[], WriteableBitmap> Cache = new Dictionary<TreeIndicator[], WriteableBitmap>();

		public static WriteableBitmap FetchTreeImage(TreeIndicator[] treeRepresentation)
		{
			if (Cache.ContainsKey(treeRepresentation))
				return Cache[treeRepresentation];

			if (treeRepresentation.Length == 0)
				return null;

			//BRGA
			var bmpData = new byte[((treeRepresentation.Length * 15) * 30) * 4];
			var writeableBitmap = new WriteableBitmap(treeRepresentation.Length * 15, 30);
			for (int iDepth = 0; iDepth < treeRepresentation.Length; iDepth++)
			{
				switch (treeRepresentation[iDepth])
				{
					case TreeIndicator.Empty:

						break;
					case TreeIndicator.End:
						//var x = 0;
						//var y = 0;
						//do
						//{
						//	var offset = (iDepth * 60) + (y * ((treeRepresentation.Length - 1) * 4)) + (x * 4);
						//	bmpData[offset] = 255;
						//	bmpData[offset + 3] = 255;
						//	x++;
						//	if(x > 15)
						//	{
						//		x = 0;
						//		y++;
						//	}
						//} while (x < 15 || y < 30);

						break;
				}
			}
			var pixelStream = writeableBitmap.PixelBuffer.AsStream();
			pixelStream.Write(bmpData, 0, bmpData.Length);

			writeableBitmap.Invalidate();

			Cache.Add(treeRepresentation, writeableBitmap);
			return writeableBitmap;
		}
	}
}
