# Latest-Chatty-8

A Universal Windows App (UWP) client for shacknews.com's chatty.

This is built on top of the [WinChatty v2 API](https://github.com/electroly/winchatty-server).

Documentatation for the WinChatty v2 API is available [here](http://winchatty.com/v2/readme).

--------

Current functionality
-----------------
 - Universal app capable of being distributed for phone and tablet/pc
 - LOL support only for "positive" tags, lol, inf, unf. Negative tags such as ugh, wtf, etc will not be supported.
 - Inline image viewing
 - Inline comment expansion in the tablet/pc version.
 - Responsive UI that responds well to being resized/docked.
 - Touch friendly as well as keyboard/mouse friendly.
 - Maintain a constantly updated "live" version of the chatty locally. (use the WinChatty pollForEvent mechanism to avoid open network connections on mobile devices)
 
 Build
 -----------------
 - Clone the repo
 - Open Latest Chatty 8.sln with Visual Studio 2015 and the UWP SDK build 10240 or higher.
 - Add appropriate values to ApplicationInsights.config, or remove the reference to the config from the solution entirely if you do not want to use Application Insights
 - Build and go!