# Features

This mod+overlay provides an automated way of tracking and displaying a lot of data.  

The data that is tracked:

- Attempts at completing every room (failures and successes)
- Deaths with the golden berry

The data that can displayed:

- Basic information (checkpoint name/abbreviation, room number in the checkpoint)
- Success Rate for every room, averaged for the checkpoints and chapter
- Average deaths-per-room for the checkpoints and chapter
- Total and session-only deaths with the golden berry for every room, summed up for the checkpoints and chapter
- Golden Chance (explained below) for the checkpoints and chapter, or from the start to the current room or the current room to the end
- Choke Rate (explained below) in total and session-only

## Display

Below is shown how the display CAN look like when displaying a lot of information. The display can be customized to display only the information you want to see (check out the [Usage](/tracker-usage) page).

![Base](https://gbt.vi-home.de/src/img/tracker/base-tracker-large-marked.png)

- 1: The formatted text portion of the display
- 2: Room Attempt Display
- 3: Chapter Bar
- 4: Golden Share Display

### Formatted Text

The formatted text portion of the display can be customized via the settings. It uses placeholder and plain text to display as much or as little information as you want. Check out the [Usage](/tracker-usage) and [Settings](/tracker-settings) page for more detail on what can be displayed here.

### Room Attempt Display

The room attempt display shows the last few attempts in the room you are currently in. The green and red circles represent successful attempts and deaths respectively. When a new attempt is added to the room (either by dying or completing the room) a new circle will appear on the left and when the limit of tracked attempts is exceeded a circle from the right will disappear. The room attempt display can be disabled through the settings.

### Chapter Bar

The chapter bar visualizes the Success Rate of all rooms in the current chapter. The colors of the bars are determined by cutoff-values specified in the settings file, by default:

| Success Rate in a room | Color       |
| ---------------------- | ----------- |
| >= 95%                 | light green |
| >= 80%                 | green       |
| >= 50%                 | yellow      |
| < 50%                  | red         |

To display this the overlay needs information about the path through the chapter. I've included a path for all base game chapters in the overlay but if you take different paths through a chapter (like 3A demo or Power Source key skip in Farewell) or play modded maps, you might want to record your own path once (See [Usage](/tracker-usage) page for details on how to record a path).

The chapter bar can be disabled in the settings.

### Golden Share Display

For easier tracking of golden berry deaths this bar shows you for each checkpoint how often you have died in the checkpoint with the golden berry. In parentheses behind each number the amount of golden berry deaths in the checkpoint in the current session is displayed. The deaths of the current session as well as the entire bar can be disabled through the settings.

## Stats Explanations

### Golden Chance

The `Golden Chance` is the empirical probability of playing a checkpoint or the chapter deathless. This value is calculated by the product of the success rates of all rooms in the checkpoint or the entire chapter.

### Choke Rate

The `Choke Rate` is calculated through the golden berry deaths. At any given room or checkpoint, the `Choke Rate` is the quotient of the golden berry deaths in the current room/checkpoint over all golden berry attempts that got to the current room/checkpoint. With this value rooms can be compared against each other and you can easily see to which room you lose a lot of runs, relative to the amount of attempts you get there.

## Further Info

- Through the ingame menu you can temporarily disable tracking of room attempts, wipe room/chapter data and start/end path recordings.
- IMPORTANT: Currently the mod does not correctly reflect changing the room via save states of the Speedrun Tool.

## Useful Links

- [Mod.zip Download via GitHub Release](https://github.com/viddie/ConsistencyTrackerMod/releases)
- [Overlay Download](https://github.com/viddie/ConsistencyTrackerMod/releases)
- [GitHub Repository](https://github.com/viddie/ConsistencyTrackerMod)