# Latest-Chatty-8

The "win81" branch of Latest Chatty 8 is a major rework of the original Windows Store app Latest Chatty 8.  It is being built on top of the WinChatty v2 API.  

Documentatation for the WinChatty v2 API is available [here](http://winchatty.com/v2/readme).

--------

Rewrite Goals
-----------------
 - Universal app capable of being distributed for phone and tablet/pc
 - LOL support only for "positive" tags, lol, inf, unf. Negative tags such as ugh, wtf, etc will not be supported.
 - Maintain as much existing functionality as possible - Cloud based pinning, image upload, inline image viewing, visible indications of new posts, etc.
 - Inline comment expansion in the tablet/pc version.  Make it work like shacknews.com does.  This approach doesn't seem to work well on phones so we'll stick with the split view on phones.
 - UI that responds well to being resized/docked.
 - Touch friendly as well as keyboard/mouse friendly.
 - Maintain a constantly updated "live" version of the chatty locally. (use the WinChatty pollForEvent mechanism to avoid open network connections on mobile devices)
