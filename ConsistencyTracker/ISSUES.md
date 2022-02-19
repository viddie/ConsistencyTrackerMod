# Known Issues

Your issue is not listed here? Contact `viddie#4751` on Discord or open an issue on [GitHub]()

| Topic                                                        | Description                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| Roos only appear once in a path                              | Rooms can only appear once in chapter path - the first time they are encountered while recording the path. This leads to the chapter-bar jumping around a bit when HUB rooms are in a chapter, like 3A's Huge Mess or Farewell's Power Source. There is currently no good solution for this problem. |
| Successful room clears that naturally exit into the previous room don't count as success | Successful clear attempts for a room are tracked by checking whether you leave the room to enter a room that is different from the previous room. For linear chapters this works fine, but for certain spots like Farewell's Power Source where you exit back into the HUB room after getting a key the mod doesn't track a successful attempt. There is currently no solution for this problem. |
| Switching rooms with save states doesn't work                | Switching rooms with save states is not correctly reflected in the tracker. If anyone knows how to detect room chances via save states in code, please message me on Discord or open an issue/PR on GitHub |