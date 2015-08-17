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
";
		public static string GetPostHtml(string postBody)
		{
			return @"
<html xmlns='http://www.w3.org/1999/xhtml'>
<head>
	<meta name='viewport' content='user-scalable=no'/>
	<style type='text/css'>" + WebBrowserHelper.CSS + @"</style>
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
            window.external.notify(JSON.stringify({'eventName': 'rightClickedImage', 'eventData': {'url': url}}));
        }
        function toggleEmbeddedImage(container, url) {
            var target = container.getElementsByTagName('div')[0];
            var alreadyEmbedded = target.getElementsByTagName('img')[0];
            if (alreadyEmbedded === undefined) {
                window.external.notify(JSON.stringify({'eventName': 'debug', 'eventData': {'name': 'Embed Image', 'url': url}}));

                var placeholder = document.createElement('img');
                target.appendChild(placeholder);
                placeholder.onload = function () { window.external.notify(JSON.stringify({'eventName': 'imageloaded', 'eventData': {}})); };
                placeholder.src = 'data:image/gif;base64,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';
                placeholder.onclick = function () { target.removeChild(placeholder); window.external.notify(JSON.stringify({'eventName': 'imageloaded', 'eventData': {}})); }
                var embed = new Image();
                embed.src = url;
                embed.className = 'fullsize';
                embed.onclick = function () { target.removeChild(embed); window.external.notify(JSON.stringify({'eventName': 'imageloaded', 'eventData': {}})); };
                embed.oncontextmenu= function () { rightClickedImage(url); };
                embed.onload = function () {
                    target.removeChild(placeholder);
                    target.appendChild(embed);
                    window.external.notify(JSON.stringify({'eventName': 'imageloaded', 'eventData': {}}));
                };
            } else {
                target.removeChild(alreadyEmbedded);
                window.external.notify(JSON.stringify({'eventName': 'imageloaded', 'eventData': {}}));
            }
            return false;
        }
        function toggleImgurGifv(container, url) {
            var target = container.getElementsByTagName('div')[0];
            var embed = target.getElementsByTagName('video')[0];
            if (embed === undefined) {
                //<iframe class='hidden' allowfullscreen='' frameborder='0' scrolling='no' src=''></iframe>
                var video = document.createElement('video');
                video.setAttribute('autoplay', '');
                video.setAttribute('loop', '');
                video.setAttribute('muted', '');
                var width = GetViewWidth() *.666666;
                var height = width * .5265;
                video.setAttribute('width', width);
                //video.setAttribute('height', height);
                target.onclick =  function (e) { target.removeChild(video); window.external.notify(JSON.stringify({'eventName': 'imageloaded', 'eventData': {}})); };
                var source = document.createElement('source');
                source.src = url;
                source.setAttribute('type', 'video/mp4');
                video.appendChild(source);
                window.external.notify(JSON.stringify({'eventName': 'debug', 'eventData': {'name': 'Embed Gifv', 'width': width, 'height': height}}));
                target.appendChild(video);
            } else {
                target.removeChild(embed);
            }
            window.external.notify(JSON.stringify({'eventName': 'imageloaded', 'eventData': {}}));
            return false;
        }
	</script>
</head>
	<body>
		<div id='commentBody' class='body'>" + postBody.Replace("target=\"_blank\"", "") + @"</div>
	</body>
</html>";
		}
	}
	#endregion
}
