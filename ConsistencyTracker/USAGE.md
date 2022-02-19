## Usage

The stats display works immediately for the room, and always the current room only. In order to get data for the checkpoint or chapter, the path information for that chapter needs to be present in the `paths` folder. I have pre packaged a few paths: All base game A/B/C sides & Farewell, as well as a few D sides. The path info sadly can't be generated automatically, but I added a feature to make recording your own path easy. For the settings, consult the [Tracker Settings](/tracker-settings).

### Record a path

When you are in the first room you want to see on the overlay, in Celeste go into `Menu -> Mod Settings -> Consistency Tracker` and enable `Record Path`. Then, play through the chapter until the last room, either as normal or with Assist Mode invincibility/dashes/stuff. It doesn't matter if you die or repeat rooms, all rooms will only be counted once. In the last room, go into the Mod Settings again and disable `Record Path`. If you have the overlay open for a chapter which didn't previously have path information, you should immediately see the recorded path. If not, refresh the overlay. Once you see the path in the overlay, everything should work properly and you can start tracking. The checkpoint names and abbreviations are just numbered through, so if you want to change those, do so in the file in the `paths` folder. The paths always look something like this (Celeste chapter: 1A, Filename: "Celeste\_1-ForsakenCity\_Normal"):

```
Start;ST;6;1,2,3,4,3b,5
Crossing;CR;8;6,6a,6b,6c,7,8,8b,9
Chasm;CH;6;9b,10a,11,12,12a,end

```

With each line representing a checkpoint and the first part being the checkpoint's full name, the second the checkpoint abbreviation, the third the count of rooms and the last the debug names of the rooms visited throughout the path.

## WIP Ideas
- Room streaks
  - How often in a row did you complete the room without dying?
  - Two new fields: Current Streak and Highest Streak OR deduct it from the room attempts