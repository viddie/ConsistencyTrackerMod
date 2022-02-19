# Overlay Settings

## Setting Explanations

| Setting                           | Description                                                  | Default Value |
| --------------------------------- | ------------------------------------------------------------ | ------------- |
| attempts                          | The amount of attempts for which the tracking should be evaluated. Available values: 5, 10, 20, max (=100) | 20            |
| text-format                       | Format of the display. See section below for more details    |               |
| text-nan-replacement              | What should be displayed instead of NaN for calculated stats | -             |
| color                             | Color of the text. CSS property.                             | `white`       |
| font-size                         | Font size of the text. CSS property.                         | `40px`        |
| outline-size                      | Size of the text's outline. CSS property.                    | `10px`        |
| outline-color                     | Color of the text's outline. CSS property.                   | `black`       |
| refresh-time-ms                   | The time between update attempts of the overlay in milliseconds. | `1000`        |
| light-green-cutoff                | At and above which ratio a room should be marked light-green | 0.95          |
| green-cutoff                      | At and above which ratio a room should be marked green       | 0.8           |
| yellow-cutoff                     | At and above which ratio a room should be marked yellow      | 0.5           |
| chapter-bar-enabled               | If true, hides the chapter path bars at the bottom.          | true          |
| font-family                       | The font family of the displayed text. Font needs to be installed on the system in order to be found. Warning: The default font 'Renogare' (the font Celeste uses) is not a default system font! [Link to download Renogare](https://www.dafont.com/renogare.font). | Renogare      |
| golden-chance-decimals            | The amount of decimal places to show for the calculated golden chances. | 4             |
| golden-share-display-enabled      | The golden share display are the numbers below the chapter bar, showing how often you died with the golden berry in each checkpoint. Setting this value to false hides that display. | true          |
| golden-share-font-size            | Font size of the golden share display. CSS property.         | 28px          |
| golden-share-style-percent        | By default the golden shares will be in absolute numbers. Enable this setting to show percentage values instead | false         |
| golden-share-show-current-session | If enabled the golden share display will be extended to show the golden berry deaths for the current session in parenthesis behind the default death counts. | true          |
| room-attempts-display-enabled     | The room attempt display are the green/red circles above the chapter bar. They show the previous X attempts in order of occurrence. Setting this value to false hides that display. | true          |
| room-attempts-font-size           | Font size of the room attempt display. CSS property.         | 26px          |
| room-attempts-circle-size         | Circle size of the room attempt display. CSS property.       | 23px          |
| tracking-disabled-message-enabled | When death tracking is disabled through the mod, a little message appears on the chapter bar. Setting this value to false hides that message. | true          |

## Default Settings

```json
{
    "attempts": "20",
    "text-format-left": "GB Deaths<br>Room: {room:goldenDeaths}<br>Session: {chapter:goldenDeathsSession}<br>Total: {chapter:goldenDeaths}",
    "text-format-center": "{checkpoint:name}-{checkpoint:roomNumber}: {room:rate}% ({room:successes}/{room:attempts})<br>CP: {checkpoint:rate}%<br>Total: {chapter:rate}%",
    "text-format-right": "Golden Chance<br>CP: {checkpoint:goldenChance}%<br>Total: {chapter:goldenChance}%<br>Room➔End: {run:roomToEndGoldenChance}%",
    "text-nan-replacement": "-",
    "color": "white",
    "font-size-left": "32px",
    "font-size-center": "40px",
    "font-size-right": "25px",
    "outline-size": "10px",
    "outline-color": "black",
    "refresh-time-ms": 1000,
    "light-green-cutoff": 0.95,
    "green-cutoff": 0.8,
    "yellow-cutoff": 0.5,
    "hide-chapter-bar": false,
    "font-family": "Renogare",
    "golden-chance-decimals": 4,
    "golden-share-display-enabled": true,
    "golden-share-font-size": "28px",
    "golden-share-style-percent": false,
    "golden-share-show-current-session": true,
    "room-attempts-display-enabled": true,
    "room-attempts-font-size": "26px",
    "room-attempts-circle-size": "23px",
    "tracking-disabled-message-enabled": true
}
```

## Stats Display Format

The stats display strings uses placeholders. Type anything you want in there and use the placeholders explained below in curly braces to let the overlay fill in the information you want. Additionally, you can insert any HTML valid text in there as well, for example to make line breaks using `<br>`.

Placeholders are separated into categories. Available categories: `room` for all room specific data, `checkpoint` and `chapter` for aggregated stats for all rooms in the checkpoint or chapter respectively and `run` for stats for the current run.

Here is a list of all available placeholders:

| Category   | Placeholder                         | Description                                                  |
| ---------- | ----------------------------------- | ------------------------------------------------------------ |
| room       | {room:rate}                         | The success rate of the current room over the rate specified in the settings |
|            | {room:successes}                    | The amount of successes over the last X attempts             |
|            | {room:attempts}                     | The amount of attempts over the rate specified in the settings |
|            | {room:failures}                     | The amount of failures over the rate specified in the settings |
|            | {room:goldenDeaths}                 | The amount of deaths with the golden berry in the current room |
|            | {room:goldenDeathsSession}          | The amount of deaths with the golden berry in the current room in the current session |
|            | {room:name}                         | [DEBUGGING] Debug name of the current room                   |
| checkpoint | {checkpoint:name}                   | The name of the current checkpoint                           |
|            | {checkpoint:abbreviation}           | The abbreviation of the current checkpoint                   |
|            | {checkpoint:roomNumber}             | The number of the current room within the current checkpoint |
|            | {checkpoint:rate}                   | The average success rate of all rooms in the current checkpoint |
|            | {checkpoint:DPR}                    | The average deaths-per-room of all rooms in the current checkpoint |
|            | {checkpoint:goldenDeaths}           | The aggregated amount of deaths with the golden berry in all rooms of the current checkpoint |
|            | {checkpoint:goldenDeathsSession}    | The aggregated amount of deaths with the golden berry in all rooms of the current checkpoint in the current session |
|            | {checkpoint:goldenChance}           | The product of the success rates of all rooms in the current checkpoint |
|            | {checkpoint:goldenEstimateAttempts} | The expected value of attempts given the calculated `goldenChance` to achieve the golden berry |
| chapter    | {chapter:rate}                      | The average success rate of all rooms in the current chapter |
|            | {chapter:DPR}                       | The average deaths-per-room of all rooms in the current chapter |
|            | {chapter:goldenDeaths}              | The aggregated amount of deaths with the golden berry in all rooms of the current chapter |
|            | {chapter:goldenDeathsSession}       | The aggregated amount of deaths with the golden berry in all rooms of the current chapter in the current session |
|            | {chapter:goldenChance}              | The product of the success rates of all rooms in the current chapter |
|            | {chapter:goldenEstimateAttempts}    | The expected value of attempts given the calculated `goldenChance` to achieve the golden berry |
|            | {chapter:SID}                       | [DEBUGGING] The current chapters unique identifier           |
| run        | {run:roomToEndGoldenChance}         | The product of the success rates of all rooms in the current chapter, disregarding rooms before the current room |
|            | {run:startToRoomGoldenChance}       | The product of the success rates of all rooms in the current chapter, up until the current room |
