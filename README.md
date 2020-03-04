<img src="https://github.com/BarRaider/streamdeck-chatpager/blob/master/_images/chatpage.png" height="120" width="120"/> 

## Twitch Tools plugin for the Elgato Stream Deck

**Author's website and contact information:** [https://barraider.com](https://barraider.com)

## New in v2.2
- :new: `GiveAway` action - Control your Stream giveaways from the Stream Deck
    - Customizable registration command and giveaway title
    - Shows number of people currently entered on the key
    - Randomly selects the winner
    - Saves winners to file (so you don't have to deal with the who won what while streaming)
    - Shows winner's name on stream
    - `Auto-Draw` feature sets a countdown and automatically selects a winner when time is up
    - Auto-Reminders in chat that giveaway is active
- `Live Streamers` action now lets you choose if a long press Raids or Hosts the streamer
- `Shoutout` commands can now be sent to any channel (not just your own) -> More control for mods!
- **Support for the Stream Deck Mini!**

For more information see: https://github.com/BarRaider/streamdeck-chatpager/

## Features

**Chat Pager**  
The Twitch Chat Pager plugin listens to your Twitch chat and gives you a visual alert if someone uses the !page command.  
[Demo](https://streamable.com/1wxjh)

**Channel Monitor**  
Shows you when your favorite streamer is live. Clicking the button will take you to their stream.

**Live Streamers**  
Pressing this action will use all your Stream Deck keys to shows you which of the streamers you follow are currently live + viewer count. Clicking the button will take you to their stream.

**Send Message**  
Allows you to send messages to any channel you want (not just your own!).
- Support for reading the message from a file, allows you to create dynamic messages (like current song played on Spotify)

**Shoutout**  
Shows you a list of the latest people that chatted/raided/subscribed to your channel, allowing you to send them an automated message in chat. 

* Both `Shoutout` and `Send Message` now support sending `/commands`. (Try writing a message starting with `/me`). Create a Shoutout with `/ban {USERNAME}` to choose which username to ban.

**Change Title/Game/Tags**  
Allows you to load the Stream's title, game, and tags from a file. Works along with the Text File Tools to dynamically modify what game/title you want shown.
    - Adding multiple lines in the "Title" file will cause the plugin to randomly choose one (allowing you to generate multiple similar titles for the same game).

**Clip-To-Chat**  
Allows you to clip the last few seconds of your stream and automatically post it in your chat.

### Current Chat Pager features
- Shows you live information on your stream, including number of viewers and streaming time
- Starts flashing when someone writes the !page command in the chatroom
- !page command can be limited to only work for specific people in the chat
- Supports adding a short text after the command, such as *"!page Behind You!"*
- Support for listening to pages in multiple streamers chat rooms (great for Mods that are modding multiple streamers)
- Now supports customizing the command word to be other than`!page`
- Page message can now be written to text file (and shown on stream)
- Full-Screen Alert can now be shown even if you don't have the plugin on the current profile (as long as the plugin was shown on a different plugin, at least once, since you started streaming)
- Configure your own personal chat message to show in chat when you're paged
- Added option to disable going to the Twitch Dashboard on button click
- Multiple full-screen modes include choosing 1 letter or 2 letters per key during an alert

### Download

[Download plugin](https://github.com/BarRaider/streamdeck-chatpager/releases)

## I found a bug, who do I contact?
For support please contact the developer. Contact information is available at https://barraider.com

## I have a feature request, who do I contact?
Please contact the developer. Contact information is available at https://barraider.com

## Dependencies
* Uses StreamDeck-Tools by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider - Provides seamless integration with the Stream Deck PI (Property Inspector) 

