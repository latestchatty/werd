using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Latest_Chatty_8.Common
{
	public static class TreeImageRepo
	{

		private static readonly Dictionary<string, WriteableBitmap> Cache = new Dictionary<string, WriteableBitmap>();

		public const char End = 'e';
		public const char Empty = ' ';
		public const char Junction = 'j';
		public const char Passthrough = 'p';

#if DEBUG
		private static long _generateTime;
		private static int _cacheHits;
		private static int _callCount;
		private static int _generatedImageCount;

		public static void PrintDebugInfo()
		{
			Debug.WriteLine("Time spent generating images: {0}ms", _generateTime / TimeSpan.TicksPerMillisecond);
			Debug.WriteLine("Number of times FetchTreeImage was called: {0}", _callCount);
			Debug.WriteLine("Number of cache hits: {0}", _cacheHits);
			Debug.WriteLine("Number of images generated: {0}", _generatedImageCount);
		}
#endif

		private static int _imageHeight = -1;
		private static int ImageHeight
		{
			get
			{
				if (_imageHeight == -1)
				{
					var textBlock = new TextBlock();
					textBlock.Text = "."; //Doesn't seem to matter what goes in, the height will always be the same.
					textBlock.FontSize = (double)Application.Current.Resources["ControlContentThemeFontSize"];
					textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
					_imageHeight = Math.Max((int)textBlock.DesiredSize.Height, 16); //Minimum size of a row is 30, so don't go smaller even if the font is.
					_imageHeight += 8; //Force some padding, but we can't do it with xaml otherwise lines won't connect;
				}
				return _imageHeight;
			}
		}

		//We use a character representation because we can't key off enums without generating hashes.  This way should still be fast.
		public static WriteableBitmap FetchTreeImage(char[] treeRepresentation)
		{
#if DEBUG
			var sw = new Stopwatch();
			sw.Start();
			_callCount++;
#endif
			var key = new string(treeRepresentation);
			if (Cache.ContainsKey(key))
			{
#if DEBUG
				_cacheHits++;
#endif
				return Cache[key];
			}

			if (treeRepresentation.Length == 0)
				return null;

			//BRGA
			var sectionPixelWidth =  (int)(ImageHeight / 2);
			var sectionPixelHeight = ImageHeight;
			var sectionByteWidth = sectionPixelWidth * 4;
			var bmpData = new byte[(treeRepresentation.Length * sectionByteWidth) * sectionPixelHeight];
			var writeableBitmap = new WriteableBitmap(treeRepresentation.Length * sectionPixelWidth, sectionPixelHeight);
			var scanLineWidth = treeRepresentation.Length * sectionByteWidth;
			//var color = ((SolidColorBrush) Application.Current.Resources["ThemeHighlight"]).Color;
			var color = Colors.DimGray;

			for (int iDepth = 0; iDepth < treeRepresentation.Length; iDepth++)
			{
				switch (treeRepresentation[iDepth])
				{
					case End:
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
					case Junction:
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
					case Passthrough:
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
			_generatedImageCount++;
			sw.Stop();
			_generateTime += sw.ElapsedTicks;
#endif
			return writeableBitmap;
		}
		private static void SetColor(ref byte[] imageData, int offset, Color color)
		{
			imageData[offset] = color.B;
			imageData[offset + 1] = color.G;
			imageData[offset + 2] = color.R;
			imageData[offset + 3] = color.A;
		}
	}
}
