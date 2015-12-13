using Latest_Chatty_8.Common;
using Latest_Chatty_8.Settings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Latest_Chatty_8.Controls
{
	public sealed partial class RichPostView : UserControl
	{
		private LatestChattySettings Settings;

		public event EventHandler<LinkClickedEventArgs> LinkClicked;

		public RichPostView()
		{
			this.InitializeComponent();
		}

		#region Public Methods

		public void LoadPost(string v, LatestChattySettings settings)
		{
			this.Settings = settings;
			this.PopulateBox(v);
		}
		#endregion

		private class TagFind
		{
			public string TagName { get; private set; }
			public RunType Type { get; private set; }

			public TagFind(string tagName, RunType type)
			{
				this.TagName = tagName;
				this.Type = type;
			}
		}

		private TagFind[] FindTags =
		{
			new TagFind("red", RunType.Red),
			new TagFind("green", RunType.Green),
			new TagFind("blue", RunType.Blue),
			new TagFind("yellow", RunType.Yellow),
			new TagFind("olive", RunType.Olive),
			new TagFind("lime", RunType.Lime),
			new TagFind("orange", RunType.Orange),
			new TagFind("pink", RunType.Pink),
			new TagFind("quote", RunType.Quote),
			new TagFind("sample", RunType.Sample),
			new TagFind("strike", RunType.Strike),
			new TagFind("spoiler", RunType.Spoiler),
			new TagFind("code", RunType.Code)
		};

		private string[] EndTags =
		{
			"</u>",
			"</i>",
			"</b>",
			"</span>",
			"</pre>"
		};

		private void PopulateBox(string body)
		{
			this.postBody.Blocks.Clear();
			var lines = this.ParseLines(body);
			var appliedRunTypes = new Stack<RunType>();
			Paragraph spoiledPara = null;

			foreach (var line in lines)
			{
				var paragraph = new Windows.UI.Xaml.Documents.Paragraph();
				AddRunsToParagraph(ref paragraph, ref spoiledPara, ref appliedRunTypes, line);
				this.postBody.Blocks.Add(paragraph);
			}
		}

		private List<string> ParseLines(string body)
		{
			return body.Split(new string[] { "<br />", "<br>" }, StringSplitOptions.None).ToList();
		}

		private void AddRunsToParagraph(ref Paragraph para, ref Paragraph spoiledPara, ref Stack<RunType> appliedRunTypes, string line)
		{
			var builder = new StringBuilder();
			var iCurrentPosition = 0;

			while (iCurrentPosition < line.Length)
			{
				var result = FindRunTypeAtPosition(line, iCurrentPosition);
				var type = result.Item1;
				var lengthOfTag = result.Item2;
				var positionIncrement = lengthOfTag;
				switch (type)
				{
					case RunType.Hyperlink:
						//Handle special.

						//Complete any current run.
						AddSegment(para, appliedRunTypes, builder, spoiledPara);

						//Find the closing tag.
						var closeLocation = line.IndexOf("</a>", iCurrentPosition + lengthOfTag);
						if (closeLocation > -1)
						{
							var startOfHref = line.IndexOf("href=\"", iCurrentPosition);
							if (startOfHref > -1)
							{
								startOfHref = startOfHref + 6;
								var endOfHref = line.IndexOf("\">", startOfHref);
								var linkText = line.Substring(iCurrentPosition + lengthOfTag, closeLocation - (iCurrentPosition + lengthOfTag));
								var link = line.Substring(startOfHref, endOfHref - startOfHref);
								var hyperLink = new Hyperlink();
								var run = CreateNewRun(appliedRunTypes);
								run.Text = link;
								hyperLink.Foreground = new SolidColorBrush(Color.FromArgb(255, 174, 174, 155));
								hyperLink.Inlines.Add(run);
								hyperLink.Click += HyperLink_Click;
								if (!linkText.Equals(link))
								{
									var r = CreateNewRun(appliedRunTypes);
									r.Text = "(" + linkText + ") - ";
									if (spoiledPara != null)
									{
										spoiledPara.Inlines.Add(r);
									}
									else
									{
										para.Inlines.Add(r);
									}
								}
								if (spoiledPara != null)
								{
									spoiledPara.Inlines.Add(hyperLink);
								}
								else
								{
									para.Inlines.Add(hyperLink);
								}
								positionIncrement = (closeLocation + 4) - iCurrentPosition;
							}
						}
						break;
					case RunType.None:
						builder.Append(line[iCurrentPosition]);
						break;
					default:
						AddSegment(para, appliedRunTypes, builder, spoiledPara);

						if (type == RunType.Spoiler)
						{
							spoiledPara = new Paragraph();
						}

						if (type != RunType.End)
						{
							appliedRunTypes.Push(type);
						}

						if (type == RunType.End)
						{
							var appliedType = appliedRunTypes.Pop();
							if (appliedType == RunType.Spoiler)
							{
								var spoiler = new Spoiler();
								spoiler.SetText(spoiledPara);
								var inlineControl = new InlineUIContainer();
								inlineControl.Child = spoiler;
								para.Inlines.Add(inlineControl);
								spoiledPara = null;
							}
						}
						break;
				}
				iCurrentPosition += positionIncrement;
			}
			AddSegment(para, appliedRunTypes, builder, spoiledPara);
		}

		private void AddSegment(Paragraph para, Stack<RunType> appliedRunTypes, StringBuilder builder, Paragraph spoiledPara)
		{
			if (builder.Length == 0) return;

			Inline toAdd = null;
			var run = CreateNewRun(appliedRunTypes);
			run.Text = builder.ToString();

			if (appliedRunTypes.Any(rt => rt == RunType.Underline))
			{
				var underline = new Underline();
				underline.Inlines.Add(run);
				toAdd = underline;
			}
			else
			{
				toAdd = run;
			}

			if (!string.IsNullOrEmpty(run.Text))
			{
				if (spoiledPara != null)
				{
					spoiledPara.Inlines.Add(toAdd);
				}
				else
				{
					para.Inlines.Add(toAdd);
				}
			}

			builder.Clear();
		}

		private Run CreateNewRun(Stack<RunType> appliedRunTypes)
		{
			var run = new Run();
			run.ApplyTypesToRun(appliedRunTypes.Reverse().ToList());
			return run;
		}

		async private void HyperLink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
		{
			var linkText = ((Run)sender.Inlines[0]).Text;
   //      if (linkText.Contains(".jpg"))
			//{
			//	var imageContainer = new InlineUIContainer();
			//	var image = new Windows.UI.Xaml.Controls.Image();
			//	var req = System.Net.HttpWebRequest.CreateHttp(linkText);
			//	var response = await req.GetResponseAsync();
			//	var responseStream = response.GetResponseStream();
			//	var memoryStream = new MemoryStream();
			//	await responseStream.CopyToAsync(memoryStream);
			//	var bmpImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
			//	bmpImage.SetSource(memoryStream.AsRandomAccessStream());
			//	image.Source = bmpImage;
			//	imageContainer.Child = image;
			//	sender.Inlines.Add(imageContainer);
			//}
			if (this.LinkClicked != null)
			{
            this.LinkClicked(this, new LinkClickedEventArgs(new Uri(linkText)));
			}
		}

		private Tuple<RunType, int> FindRunTypeAtPosition(string line, int position)
		{
			//Possible tag
			if (line[position] == '<')
			{
				if (position + 1 < line.Length)
				{
					if (line[position + 1] != '/')
					{
						if (line.IndexOf("<u>", position) == position)
						{
							return new Tuple<RunType, int>(RunType.Underline, 3);
						}
						if (line.IndexOf("<i>", position) == position)
						{
							return new Tuple<RunType, int>(RunType.Italics, 3);
						}
						if (line.IndexOf("<b>", position) == position)
						{
							return new Tuple<RunType, int>(RunType.Bold, 3);
						}
						//It's a style tag
						if (line.IndexOf("<span class=\"jt_", position) == position)
						{
							foreach (var tagToFind in FindTags)
							{
								if (line.IndexOf(tagToFind.TagName, position + 16) == position + 16)
								{
									return new Tuple<RunType, int>(tagToFind.Type, line.IndexOf('>', position + 16) + 1 - position);
								}
							}
							//There's apparently a WTF242 style, not going to handle that.  Maybe they'll add more later, don't want to break if it's there.
							return new Tuple<RunType, int>(RunType.UnknownStyle, line.IndexOf('>', position + 16) + 1 - position);
						}
						if (line.IndexOf("<a target=\"_blank\" href=\"", position) == position)
						{
							return new Tuple<RunType, int>(RunType.Hyperlink, line.IndexOf('>', position + 40) + 1 - position);
						}
						if(line.IndexOf("<pre class=\"jt_code\">", position) == position)
						{
							return new Tuple<RunType, int>(RunType.Code, position + 21);
						}
					}

					foreach (var tag in this.EndTags)
					{
						if (line.IndexOf(tag, position) == position)
						{
							return new Tuple<RunType, int>(RunType.End, tag.Length);
						}
					}
				}
			}

			return new Tuple<RunType, int>(RunType.None, 1);
		}


	}
}
