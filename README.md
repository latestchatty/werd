# Werd
[![Build status](https://boarder2.visualstudio.com/Latest%20Chatty/_apis/build/status/Latest%20Chatty-Universal%20Windows%20Platform-CI)](https://boarder2.visualstudio.com/Latest%20Chatty/_build/latest?definitionId=5)

A Universal Windows App (UWP) client for shacknews.com's chatty.

This is built on top of the [Simple Chatty API](https://github.com/latestchatty/simple-chatty-server).

Documentatation for the API is available [here](https://winchatty.com/swagger/index.html).

How to build
------
 - Clone the repo
 - Open Latest Chatty 8.sln with Visual Studio
 - Build and go!

Recommended extensions
------
[EditorConfig](https://visualstudiogallery.msdn.microsoft.com/c8bccfe2-650c-4b42-bc5c-845e21f96328) - Makes sure your tab, end of line, and whitespace settings match the project

[XamlStyler](https://visualstudiogallery.msdn.microsoft.com/3de2a3c6-def5-42c4-924d-cc13a29ff5b7) - Makes life easier when formatting XAML. **Be sure to disable reordering Grid and Canvas elements in the options for this project as the z-ordering is dependent on the XAML parsing order.**

Notes
-----
- Sometimes I get lazy and don't clean up code or put things in the right place right away.  I do this project out of love for free in my spare time.  Don't judge me :(
