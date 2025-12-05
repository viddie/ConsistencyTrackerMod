# Domain Knowledge

CCT, or Celeste Consistency Tracker, is a mod for the game Celeste that helps players track a bunch of data about their consistency doing goldens. A "golden" references the ingame objective of the "golden berry", which you can obtain after completion a level without dying. The mod tracks various statistics such as the number of golden berry deaths to each room, the number of golden berry collects for each map, and the success rate when practicing specific rooms.

## Terminology

### Celeste Terminology
- Golden Berry: An in-game collectible obtained by completing a level without dying. In this mod, the term "golden" is used interchangeably with "deathless run", even if you don't technically need a golden berry for that anymore.
- Campaign: A group of maps, made by the mapper.
- Map: An "Area" in the game, made by the mapper. Interchangeably called "chapter" in CCT.
- Side: Each map can have multiple sides (A, B, C), called "Area Modes" in the game files. Each side is essentially a different map for CCT's purposes.
- Session: When loading into a map, a new session is started, lasting until the map is exited again.

### CCT Terminology
- Path: A user-defined sequence of rooms, grouped into checkpoints, that are part of a "run" on a map.
- Checkpoint: A series of rooms within a map, usually defined by the mapper
- Room: A single gameplay segment of a map. Can be a "gameplay" room (default) or a "transition" room (not counted towards stats)
- Stats: The data being tracked is stored for each path. However, they are stored independent of the path and don't require a path to be defined. A path is just necessary to make sense of the information in the stats file.
- Full Game Run (FGR): A special type of path that encompasses multiple other paths across maps. Requires CCT to be in a special mode to track properly, and has a separate stats file.
- Grouped Rooms: A feature that allows multiple rooms to be treated as a single room for tracking purposes. This is done through resolving the current room name to a different room name, based on the path. Useful for maps that use multiple rooms to represent a single room, due to technical limitations.
- Custom Room Name: A custom name assigned to a Room, which can be used to override the default room name when displaying data.
- Room Difficulty: A user-defined difficulty rating for a room. Default: 10. If not defined by the user, will be calculated automatically based on the golden choke rate = golden deaths of a room / golden entries to a room.
- Run: A single try at completing a challenge. Usually accompanied by a golden berry, but not necessary (FGR mode)
- Attempt: As currently referenced in CCT, an "attempt" is purely a practice stat. If you die in the room outside a Run, no golden stats are tracked, but the attempt at clearing the room is still counted separately.
- Live-Data / Ingame overlay: All stats and calculations are done by CCT every time the stats file is saved. The ingame overlay can display all stats through a text-based templating system.
- PhysicsInspector: A tool included with CCT that allows the player to track various physics-simulation values of the player, the room and entities in the room, and saves them to a file. The external tool bundled with CCT can then visualize the data for analysis.
- Session: A session in CCTs terms is started when the game is opened and ends when the game is closed. Entering a map for the first time in a session resets the session-specific data for that map. Old sessions are compiled in the stats file for historical tracking. If the game is opened multiple times on the same day, the old session entries are merged into a single entry per day.

### Other Terminology
- SpeedRunTool SaveState/LoadState: A hook into a different mod "SpeedRunTool". A savestate allows the player to store their current position and state in a room, and load it back later at will. CCT does a lot of state tracking, so it needs to be informed when the player entirely foregoes all game events and loads into a previous state.

## Data Model

At the core of the data model is the ChapterStats and the PathInfo classes. They represent a single path and all the stats for all rooms in the current path. All tracking and calculation is done through these two classes. Besides the basic data, they store some additional information:
- Path:
  - Chapter:
    - Chapter name, Side name, SID, UID, Campaign name, Ignored rooms list
  - Checkpoint:
    - Name, Abbreviation
  - Room:
    - Custom room name, Grouped rooms list
- Stats:
  - Same chapter meta info as Path, plus:
  - Map Bin file
  - Golden type
  - Session started timestamp
  - List of last golden runs' death locations
  - Old session list:
    - Bunch of compiled data for each previous session
  - Mod state: Flag for run state, golden done, tracking paused, mod version, overlay version, and has path flag
  - Game data: Data about the current map, pulled from the save file: Completed?, Full cleared?, Total time, Total deaths

Over the years, a few systems were built around the ChapterStats and PathInfo classes: Each map can have multiple paths, called "Segments". Each path file thus stores a list of paths now, the "PathSegmentList". A stats file likewise stores a list of stats for each path, the "ChapterStatsList". The path list has a selected index, which is both the currently active path AND the currently active stats files, so both always need to be in sync. The full data hierarchy now goes:

Path Segment List -> Path Segment -> Path Info -> Checkpoints -> Rooms