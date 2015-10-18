using System;
using System.Text;

namespace Latest_Chatty_8.Common
{
	public class WebBrowserHelper
	{
		#region Browser Templates
		public const string CSS = @"body
		{
			overflow:hidden;
			background:#212121;
			font-family:'Segoe UI';
			color:#FFF;
			margin:0;
			padding:0;
		}

		div.wrapper
		{
			padding:10px;
		}

		div.body
		{
			color:#FFF;
			clear:both;
			padding:20px 0;
		}

		div.body a
		{
			color:#AEAE9B;
		}

		span.jt_red
		{
			color:red;
		}

		span.jt_green
		{
			color:#8DC63F;
		}

		span.jt_blue
		{
			color:#44AEDF;
		}

		span.jt_yellow
		{
			color:#FFDE00;
		}

		span.jt_olive
		{
			color:olive;
		}

		span.jt_lime
		{
			color:#C0FFC0;
		}

		span.jt_orange
		{
			color:#F7941C;
		}

		span.jt_pink
		{
			color:#F49AC1;
		}

		span.jt_spoiler
		{
			background-color:#383838;
			color:#383838;
		}

		span.jt_strike
		{
			text-decoration:line-through;
		}

		span.jt_quote
		{
			font-family:serif;
			font-size:110%;
		}

		pre.jt_code
		{
			border-left:1px solid #666;
			display:block;
			font-family:monospace;
			margin:5px 0 5px 10px;
			padding:3px 0 3px 10px;
		}

		div.youtube-widget
		{
			text-align:center;
		}

		h1.story-title
		{
			font-size:150%;
			text-shadow:#000 1px 5px 7px;
		}

		div.focalbox
		{
			background:#444;
			text-align:center;
			-webkit-border-radius:20px;
			margin:10px 0;
			padding:10px;
		}

		div.focalbox img
		{
			border:2px solid #888;
			margin:10px;
		}

		div.story-content a
		{
			color:#CF262D;
			font-weight:700;
		}

		span.jt_sample,div.youtube-widget a
		{
			font-size:80%;
		}
		img.embedded
		{
			vertical-align: middle;
			max-height: 500px;
			max-width: 500px;
			width: auto;
		}
		img.fullsize
		{
			vertical-align: middle;
			max-width:100%;
			height: auto;
		}
        img.hidden {
            display:none;
        }
		a.openExternal {
			font-family: Segoe MDL2 Assets;
			font-size:125%;
			text-decoration: none;
			padding-left:6px;
			vertical-align: top;
		}
		video {
			max-width: 100%;
		}
";
		public static string GetPostHtml(string postBody, EmbedTypes embeddedTypes)
		{
			return @"
<html xmlns='http://www.w3.org/1999/xhtml'>
<head>
	<meta name='viewport' content='user-scalable=no'/>
	<style type='text/css'>" + WebBrowserHelper.CSS + @"</style>
	" + EmbedHelper.GetEmbeddedScript(embeddedTypes) + @"
	<script type='text/javascript'>
		function SetFontSize(size) {
			var html = document.getElementById('commentBody');
			html.style.fontSize = size + 'pt';
		}
		function SetViewSize(size) {
			var html = document.getElementById('commentBody');
			html.style.width = size + 'px';
		}
		function GetViewSize() {
			var html = document.getElementById('commentBody');
			var height = Math.max(html.clientHeight, html.scrollHeight, html.offsetHeight);
			return height.toString();
		}
		function GetViewWidth() {
			var html = document.getElementById('commentBody');
			return Math.max(html.clientWidth, html.scrollWidth, html.offsetWidth);
		}
		function rightClickedImage(url) {
			emitMessage('rightClickedImage', {'url': url});
		}
		function emitMessage(eventName, eventData) {
			window.external.notify(JSON.stringify({'eventName': eventName, 'eventData': eventData}));
		}
	</script>
</head>
	<body>
		<div id='commentBody' class='body'>"
			+ postBody.Replace("target=\"_blank\"", "") + @"
		</div>
	</body>
</html>";
		}
	}
	#endregion
}
