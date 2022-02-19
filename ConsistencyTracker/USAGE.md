# Usage

## Examples

Here are a few examples of how the tracker can be made to look through customizing the settings. All examples are available in the settings and can be used by setting the `selected-override` settings to the desired theme.  Depending on how much information you display, you will need to adjust the height of the Browser Source in OBS as the overlay will always try to fill in the available space.

#### Base (no theme)

The base is how the tracker looks when the `selected-override` setting is left empty. All base values will be used with no overrides.

![Base Overlay](https://gbt.vi-home.de/src/img/tracker/base.png)

#### only-room-rate

This override only displays the rooms Success Rate with successes/attempts in parentheses.

![](https://gbt.vi-home.de/src/img/tracker/only-room-rate.png)

#### only-rates

This override displays the rooms Success Rate with success/attempts in parentheses, as well as the average success rate.

![](https://gbt.vi-home.de/src/img/tracker/only-rates.png)

#### only-bar

This override displays only the chapter bar. Nice for minimalistically displaying the consistency in the entire chapter.

![](https://gbt.vi-home.de/src/img/tracker/only-bar-2.png)

#### bar-and-rates

Chapter bar + Success Rate stats.

![](https://gbt.vi-home.de/src/img/tracker/bar-and-rates.png)

#### golden-berry-tracking-simple

An override if you are mostly interested in tracking your golden berry deaths in each checkpoint.

![](https://gbt.vi-home.de/src/img/tracker/golden-berry-tracking-simple.png)

#### golden-berry-tracking-with-session

Extended version of the golden berry tracking override adding data of the current session, as well as room-wise data.

![](https://gbt.vi-home.de/src/img/tracker/golden-berry-tracking-session.png)

A better explanation for the "override" stuff can be found in the [Settings](/tracker-settings) page.

## Paths

Only most of the room specific stats work when no path is provided. In order to calculate and display data for the checkpoint or chapter, the path information for that chapter needs to be present in the `paths` folder. I have pre packaged a few paths: All base game A/B/C sides & Farewell, as well as a few D sides. The path info sadly can't be generated automatically, but I added a feature to make recording your own path easy.

### Record a path

When you are in the first room you want to see on the overlay, in Celeste go into `Menu -> Mod Settings -> Consistency Tracker` and enable `Record Path`. Then, play through the chapter until the last room, either as normal or with Assist Mode invincibility/dashes/stuff. It doesn't matter if you die or repeat rooms, all rooms will only be counted once. In the last room, go into the Mod Settings again and disable `Record Path`. If you have the overlay open for a chapter which didn't previously have path information, you should immediately see the recorded path. If not, refresh the overlay. Once you see the path in the overlay, everything should work properly and you can start tracking. The checkpoint names and abbreviations are just numbered through, so if you want to change those, do so in the file in the `paths` folder. The paths always look something like this (Celeste chapter: 1A, Filename: "Celeste\_1-ForsakenCity\_Normal"):

```
Start;ST;6;1,2,3,4,3b,5
Crossing;CR;8;6,6a,6b,6c,7,8,8b,9
Chasm;CH;6;9b,10a,11,12,12a,end

```

With each line representing a checkpoint and the first part being the checkpoint's full name, the second the checkpoint abbreviation, the third the count of rooms and the last the debug names of the rooms visited throughout the path.
