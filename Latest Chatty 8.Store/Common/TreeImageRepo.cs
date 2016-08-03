using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Latest_Chatty_8.Common
{
	public static class TreeImageRepo
	{

		private static Dictionary<string, WriteableBitmap> Cache = new Dictionary<string, WriteableBitmap>();

		public const char END = 'e';
		public const char EMPTY = ' ';
		public const char JUNCTION = 'j';
		public const char PASSTHROUGH = 'p';

#if DEBUG
		private static long generateTime = 0;
		private static int cacheHits = 0;
		private static int callCount = 0;
		private static int generatedImageCount = 0;

		public static void PrintDebugInfo()
		{
			System.Diagnostics.Debug.WriteLine("Time spent generating images: {0}ms", generateTime / TimeSpan.TicksPerMillisecond);
			System.Diagnostics.Debug.WriteLine("Number of times FetchTreeImage was called: {0}", callCount);
			System.Diagnostics.Debug.WriteLine("Number of cache hits: {0}", cacheHits);
			System.Diagnostics.Debug.WriteLine("Number of images generated: {0}", generatedImageCount);
		}
#endif

		private static int imageHeight = -1;
		private static int ImageHeight
		{
			get
			{
				if (imageHeight == -1)
				{
					var textBlock = new Windows.UI.Xaml.Controls.TextBlock();
					textBlock.Text = "."; //Doesn't seem to matter what goes in, the height will always be the same.
					textBlock.FontSize = (double)App.Current.Resources["ControlContentThemeFontSize"];
					textBlock.Measure(new Windows.Foundation.Size(Double.PositiveInfinity, Double.PositiveInfinity));
					imageHeight = Math.Max((int)textBlock.DesiredSize.Height, 16); //Minimum size of a row is 30, so don't go smaller even if the font is.
					imageHeight += 8; //Force some padding, but we can't do it with xaml otherwise lines won't connect;
				}
				return imageHeight;
			}
		}

		//We use a character representation because we can't key off enums without generating hashes.  This way should still be fast.
		public static WriteableBitmap FetchTreeImage(char[] treeRepresentation)
		{
#if DEBUG
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			callCount++;
#endif
			var key = new string(treeRepresentation);
			if (Cache.ContainsKey(key))
			{
#if DEBUG
				cacheHits++;
#endif
				return Cache[key];
			}

			if (treeRepresentation.Length == 0)
				return null;

			//BRGA
			var sectionPixelWidth =  (int)(ImageHeight / 2.5);
			var sectionPixelHeight = ImageHeight;
			var sectionByteWidth = sectionPixelWidth * 4;
			var bmpData = new byte[(treeRepresentation.Length * sectionByteWidth) * sectionPixelHeight];
			var writeableBitmap = new WriteableBitmap(treeRepresentation.Length * sectionPixelWidth, sectionPixelHeight);
			var scanLineWidth = treeRepresentation.Length * sectionByteWidth;
			var color = (App.Current.Resources["ThemeHighlight"] as SolidColorBrush).Color;

			for (int iDepth = 0; iDepth < treeRepresentation.Length; iDepth++)
			{
				switch (treeRepresentation[iDepth])
				{
					case END:
						for (var y = 0; y < sectionPixelHeight / 2; y++)
						{
							var x = sectionPixelWidth / 2;
							var offset = (iDepth * sectionByteWidth) + (x * 4) + (scanLineWidth * y);
							SetColor(ref bmpData, offset, color);
						}
						for (var x = sectionPixelWidth / 2; x < sectionPixelWidth; x++)
						{
							var y = sectionPixelHeight / 2;
							var offset = (iDepth * sectionByteWidth) + (x * 4) + (scanLineWidth * y);
							SetColor(ref bmpData, offset, color);
						}
						break;
					case JUNCTION:
						for (var y = 0; y < sectionPixelHeight; y++)
						{
							var x = sectionPixelWidth / 2;
							var offset = (iDepth * sectionByteWidth) + (x * 4) + (scanLineWidth * y);
							SetColor(ref bmpData, offset, color);
						}
						for (var x = sectionPixelWidth / 2; x < sectionPixelWidth; x++)
						{
							var y = sectionPixelHeight / 2;
							var offset = (iDepth * sectionByteWidth) + (x * 4) + (scanLineWidth * y);
							SetColor(ref bmpData, offset, color);
						}
						break;
					case PASSTHROUGH:
						for (var y = 0; y < sectionPixelHeight; y++)
						{
							var x = sectionPixelWidth / 2;
							var offset = (iDepth * sectionByteWidth) + (x * 4) + (scanLineWidth * y);
							SetColor(ref bmpData, offset, color);
						}
						break;
				}
			}
			var pixelStream = writeableBitmap.PixelBuffer.AsStream();
			pixelStream.Write(bmpData, 0, bmpData.Length);

			writeableBitmap.Invalidate();

			Cache.Add(key, writeableBitmap);
#if DEBUG
			generatedImageCount++;
			sw.Stop();
			generateTime += sw.ElapsedTicks;
#endif
			return writeableBitmap;
		}
		private static void SetColor(ref byte[] imageData, int offset, Windows.UI.Color color)
		{
			imageData[offset] = color.B;
			imageData[offset + 1] = color.G;
			imageData[offset + 2] = color.R;
			imageData[offset + 3] = color.A;
		}
	}
}
