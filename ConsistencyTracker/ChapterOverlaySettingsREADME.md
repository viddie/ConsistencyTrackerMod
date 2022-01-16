# Plain Settings

## Setting Explanations

| Settings        | Description                                                  | Default Value               |
| --------------- | ------------------------------------------------------------ | --------------------------- |
| text-format     | Format of the display. Uses placeholders to fill in the data. Available placeholders: `{name}` for the name of the current room, `{rate[5|10|20|Max]}` for the average over the 5/10/20/all attempts, `{successes[5|10|20|Max]}` for the number of successes over the 5/10/20/all tracked attempts, `{attempts[5|10|20|Max]}` for the number of attempts, `{failures[5|10|20|Max]}` for the number of failures over the 5/10/20/all tracked attempts | `Room '{name}': {rateMax}%` |
| color           | Font color of the displayed text. CSS property.              | `white`                     |
| font-size       | Font size of the displayed text. CSS property.               | `80px`                      |
| outline-size    | Size of the text outline. CSS property.                      | `1px`                       |
| outline-color   | Color of the text outline. CSS property.                     | `black`                     |
| refresh-time-ms | The time between update attempts of the overlay in milliseconds. | `1000`                      |

## Default Settings

```json
{
    "text-format": "Room '{name}': {rateMax}%",
    "color": "white",
    "font-size": "80px",
    "outline-size": "1px",
    "outline-color": "black",
    "refresh-time-ms": 1000
}
```

## Installation

Browsers don't allow pages to access local files. Since this local page needs to access the statistic files outputted by the Celeste Mod, you need to instruct your Browser to let local pages access local files.

### Firefox

1. Write `about:config` in the address bar
2. Accept the 'risk'
3. Search for `security.fileuri.strict_origin_policy`
4. Set the value to `false`
5. Start the `ChapterOverlay.html`