# Setup

## Installation

### Mod

#### Through Olympus

Just search for `Celeste Consistency Tracker` and install it.

#### Manually

- Download the mod from [here]() (add link)
- Go into the Celeste installation folder, usually found in `C:\Program Files (x86)\Steam\steamapps\common\Celeste`
- Place the `ConsistencyTrackerMod.zip` in the `Mods` folder of Celeste.

#### After Installation

After the mod is installed:

- Start Celeste
- If it doesn't crash, you should see a new folder `ConsistencyTracker` in the root of the Celeste folder.
- In that folder 3 new folders should appear: `logs`, `paths` and `stats`
  - `stats` is where the mod will output the data to
  - `paths` is where all chapter's paths go (further explained in a later section)
  - `logs` just for my debugging logs, these self delete over time, so don't worry about those
- All done!

### Overlay

- Download the overlay from [here]() (add link)
- Make sure you have started Celeste once with the mod installed!
- Go into Celeste's root folder, usually found in `C:\Program Files (x86)\Steam\steamapps\common\Celeste`
- Go into the newly created folder `ConsistencyTracker`
- In that folder, place all the files and folders from the overlay

If everything is setup correctly, the folder structure should look something like this:

```
.../Celeste/Mods/ConsistencyTracker.zip
.../Celeste/ConsistencyTracker/paths/...
.../Celeste/ConsistencyTracker/stats/
.../Celeste/ConsistencyTracker/logs/
.../Celeste/ConsistencyTracker/ChapterOverlay.css
.../Celeste/ConsistencyTracker/ChapterOverlay.html
.../Celeste/ConsistencyTracker/ChapterOverlay.js
.../Celeste/ConsistencyTracker/ChapterOverlaySettings.json
.../Celeste/ConsistencyTracker/common.js
```

### OBS

- Add the `ChapterOverlay.html` file as browser source
- Set the width to 1920
- Set the height based on how much info you display (usually somewhere around 200-300)

### Browser

Browsers are a bit weird for this. The website will try to access the files from the stats and paths folder, which is not allowed usually in modern Browser. I did find a workaround for Firefox, but I'm not yet sure what to do on Chrome, so I hope you use Firefox :D

#### Firefox

1. Write `about:config` in the address bar
2. Accept the 'risk'
3. Search for `security.fileuri.strict_origin_policy`
4. Set the value to `false`
5. Start the `ChapterOverlay.html`

Reminder: This is not really unsafe, since it only allows local files to access other local files. Online websites still can't access anything locally.

#### Chrome

Chrome doesn't have a setting that can easily allow local sites to read local files, but Chrome can be started with additional start parameters to allow this:

1. Find your `chrome.exe`, usually located somewhere like `C:\Program Files (x86)\Google\Chrome\Application\chrome.exe`
2. In the folder with `chrome.exe`, shift right-click somewhere that is not a file/folder and select `Open PowerShell window here`
3. In the PowerShell, write `./chrome.exe --allow-file-access-from-files`
4. Chrome should open now
5. Start the `ChapterOverlay.html`

Once you close and reopen Chrome, everything will be back to normal. Every time you want to use the overlay, Chrome needs to be started with the `--allow-file-access-from-files` flag explicitly.
