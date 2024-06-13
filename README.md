![](logo.png)
# Killfeed - PvP Announcer and Statistics

## Features
- Announce PvP kills in chat
- Kill Leaderboard
- Killstreak Leaderboard
- Death Leaderboard
- Announce kill streaks
- Optionally include current level or maximum level
 
## Upcoming features
- Announce multi-kills
- Track by clan

## Setttings

This is a preview of the settings file, the plugin will generate a file with the default values when run.

```ini
[General]

## Announce kills in chat
# Setting type: Boolean
# Default value: true
AnnounceKills = true

## Minimum killstreak count that must be lost to be announced.
# Setting type: Int32
# Default value: 3
AnnounceKillstreakLostMinimum = 3

## Announce killstreaks in chat
# Setting type: Boolean
# Default value: true
AnnounceKillstreak = true

## Include player gear levels in announcements.
# Setting type: Boolean
# Default value: true
IncludeLevel = true

## Use max gear level instead of current gear level.
# Setting type: Boolean
# Default value: false
UseMaxLevel = true

```

## https://vrisingmods.com/discord

# Changelog
- 0.3.1
	- Added new options to show level, and if so to show current or maximum level
- 0.2.1
	- Fixed confusing plugin ID, **important:** the config file was `com.deca.Bloodstone.cfg` it will now use `gg.deca.Killfeed.cfg`
- 0.2.0
	- Update for 1.0
- 0.1.0
	- Initial release
