let settings = {};
let defaultSettings = {
    "base": {
        "attempts": "20",
        "refresh-time-ms": 1000,
        "text-format-left": "Attempts: {chapter:goldenDeaths} ({chapter:goldenDeathsSession}) [{room:goldenDeaths}]<br>Choke Rate: {room:chokeRate}<br>PB: {pb:best} (Session: {pb:bestSession})",
        "text-format-center": "{room:name}: {room:successRate} ({room:successes}/{room:attempts})<br>CP: {checkpoint:successRate}<br>Total: {chapter:successRate}",
        "text-format-right": "Low Death: {list:checkpointDeaths}<br>Current Run: #{run:currentPbStatus} (Session: #{run:currentPbStatusSession})",
        "text-nan-replacement": "-",
        "color": "white",
        "font-family": "Renogare",
        "font-size-left": "32px",
        "font-size-center": "40px",
        "font-size-right": "25px",
        "outline-size": "10px",
        "outline-color": "black",
        "golden-chance-decimals": 2,
        "chapter-bar-enabled": true,
        "light-green-cutoff": 0.95,
        "green-cutoff": 0.8,
        "yellow-cutoff": 0.5,
        "chapter-bar-border-width-multiplier": 1,
        "golden-share-display-enabled": true,
        "golden-share-font-size": "28px",
        "golden-share-style-percent": false,
        "golden-share-show-current-session": true,
        "room-attempts-display-enabled": true,
        "room-attempts-font-size": "26px",
        "room-attempts-circle-size": "23px",
        "room-attempts-new-text": "New -",
        "room-attempts-old-text": "- Old",
        "tracking-disabled-message-enabled": true,
        "golden-pb-display-enabled": true,
        "colorblind-mode-enabled": true
    },
    "selected-override": "",
    "overrides": {
        "Low Death": {
            "text-format-left": "Attempts: {chapter:goldenDeaths} ({chapter:goldenDeathsSession}) [{room:goldenDeaths}]<br>Room Streak: {room:currentStreak} (CP: {checkpoint:currentStreak})",
            "text-format-center": "{room:name}: {room:successRate} ({room:successes}/{room:attempts})<br>CP: {checkpoint:successRate}<br>Total: {chapter:successRate}",
            "text-format-right": "Current Low Death: {list:checkpointDeaths}<br>Red: {chapter:color-red}, Yellow: {chapter:color-yellow}, Green: {chapter:color-green} ({chapter:color-lightGreen})",
        },
        "Golden Attempts": {
            "text-format-left": "Attempts: {chapter:goldenDeaths} ({chapter:goldenDeathsSession}) [{room:goldenDeaths}]<br>PB: {pb:best} ({pb:bestRoomNumber}/{chapter:roomCount})<br>Session PB: {pb:bestSession} ({pb:bestRoomNumberSession}/{chapter:roomCount})",
            "text-format-center": "{room:name}: {room:successRate} ({room:successes}/{room:attempts})<br>CP: {checkpoint:successRate}<br>Total: {chapter:successRate}",
            "text-format-right": "Current Run: #{run:currentPbStatus} (Session: #{run:currentPbStatusSession})<br>Choke Rate: {room:chokeRate} (CP: {checkpoint:chokeRate})<br>CP Golden Chance: {checkpoint:goldenChance}<br> Chapter Golden Chance: {chapter:goldenChance}",
        }
    }
}

let intervalHandle = null;

let overlayVersion = {
    "version": "1.1.1",
    "major": 1,
    "minor": 1,
    "patch": 1
};
let modState = null;

let currentRoomName = null;
let previousRoomName = null;
let previousRoomRaw = null;
let previousChapterName = null;
let currentChapterRoomObjs = {};
let currentChapterElements = {};
let currentChapterPath = null;
let currentChapterOverallRate = null;
let currentSelectedRoomName = null;
let currentChapterRoomCheckpoints = {};

let currentChapterGoldenShareCheckpointElements = {};
let currentChapterGoldenPBElements = null;
let currentChapterGoldenPBPreviousElement = null;

let currentCheckpointObj = null;
let currentCheckpointRoomIndex = null;

let trackingPausedElement = null;

document.addEventListener('DOMContentLoaded', function() {
    fetchSettings();
});


function applySettings(overlaySettingsObj){
    if(overlaySettingsObj.errorCode === 0){
        console.log("Settings loaded successfully from mod");
        let modOverlaySettings = overlaySettingsObj.settings;

        //General
        settings.base["colorblind-mode-enabled"] = modOverlaySettings.general.colorblindMode;
        settings.base["attempts"] = modOverlaySettings.general.attemptsCount+"";
        settings.base["refresh-time-ms"] = modOverlaySettings.general.refreshTimeSeconds*1000;
        settings.base["outline-size"] = modOverlaySettings.general.textOutlineSize+"px";
        settings.base["font-family"] = modOverlaySettings.general.fontFamily;

        //Chapter Bar
        settings.base["chapter-bar-enabled"] = modOverlaySettings.chapterBarDisplay.enabled;
        settings.base["chapter-bar-border-width-multiplier"] = modOverlaySettings.chapterBarDisplay.borderWidthMultiplier;
        settings.base["light-green-cutoff"] = modOverlaySettings.chapterBarDisplay.lightGreenCutoff;
        settings.base["green-cutoff"] = modOverlaySettings.chapterBarDisplay.greenCutoff;
        settings.base["yellow-cutoff"] = modOverlaySettings.chapterBarDisplay.yellowCutoff;

        //Text Stats
        if(modOverlaySettings.textStatsDisplay.enabled === false){
            settings.base["text-format-left"] = "";
            settings.base["text-format-center"] = "";
            settings.base["text-format-right"] = "";
        }
        if(modOverlaySettings.textStatsDisplay.leftEnabled === false){
            settings.base["text-format-left"] = "";
        }
        if(modOverlaySettings.textStatsDisplay.middleEnabled === false){
            settings.base["text-format-center"] = "";
        }
        if(modOverlaySettings.textStatsDisplay.rightEnabled === false){
            settings.base["text-format-right"] = "";
        }

        settings["selected-override"] = modOverlaySettings.textStatsDisplay.preset;

        //Golden Share
        settings.base["golden-share-display-enabled"] = modOverlaySettings.goldenShareDisplay.enabled;
        settings.base["golden-share-show-current-session"] = modOverlaySettings.goldenShareDisplay.showSession;

        //Other
        settings.base["golden-pb-display-enabled"] = modOverlaySettings.goldenPBDisplay.enabled;
        settings.base["room-attempts-display-enabled"] = modOverlaySettings.roomAttemptsDisplay.enabled;
    } else {
        let errorMessage = overlaySettingsObj.errorMessage;
        let errorCode = overlaySettingsObj.errorCode;
        console.log("Error ("+errorCode+") loading settings from mod: "+errorMessage);
    }
    


    let hideBar = !getSettingValueOrDefault("chapter-bar-enabled");
    if(hideBar){
        document.getElementById("chapter-container").style.display = "none";
    }

    let size = getSettingValueOrDefault("outline-size");
    let outlineColor = getSettingValueOrDefault("outline-color");
    let textShadow = "";
    for(var i = 0; i < 6; i++){
        textShadow += outlineColor+" 0px 0px "+size+", ";
    }
    textShadow = textShadow.substring(0, textShadow.length-2);

    var textColor = getSettingValueOrDefault("color");
    applyToElement("stats-left", textColor, getSettingValueOrDefault("font-size-left"), textShadow);
    applyToElement("stats-center", textColor, getSettingValueOrDefault("font-size-center"), textShadow);
    applyToElement("stats-right", textColor, getSettingValueOrDefault("font-size-right"), textShadow);
    
    applyToElement("chapter-container", textColor, getSettingValueOrDefault("font-size-center"), textShadow);

    document.body.style.fontFamily = getSettingValueOrDefault("font-family");
}

// hide/show GOLDEN SHARE DISPLAY
function hideGoldenShareDisplay(){
    var goldenShareContainer = document.getElementById("golden-share-container");
    goldenShareContainer.style.display = "none";
}
function applySettingsForGoldenShareDisplay(){
    var goldenShareContainer = document.getElementById("golden-share-container");
    var doShow = getSettingValueOrDefault("golden-share-display-enabled");
    if(doShow){
        goldenShareContainer.style.display = "flex";
    } else {
        goldenShareContainer.style.display = "none";
    }

    goldenShareContainer.style.fontSize = getSettingValueOrDefault("golden-share-font-size");
}
//===============================

// hide/show ATTEMPT DISPLAY
function hideRoomAttemptDisplay(){
    var elemContainer = document.getElementById("room-attempts-container");
    elemContainer.style.display = "none";
}
function applySettingsForRoomAttemptDisplay(){
    if(getSettingValueOrDefault("room-attempts-display-enabled")){
        document.getElementById("room-attempts-container").style.display = "flex";
    }
}
//=====================

// hide/show PB DISPLAY
function applySettingsForGoldenPBDisplay(){
    if(getSettingValueOrDefault("golden-pb-display-enabled")){
        document.getElementById("pb-container").style.display = "flex";
    }
}
function hideGoldenPBDisplay(){
    var pbContainer = document.getElementById("pb-container");
    pbContainer.style.display = "none";
}

function applyToElement(id, color, fontSize, textShadow){
    var element = document.getElementById(id);
    element.style.color = color;
    element.style.fontSize = fontSize;
    element.style.textShadow = textShadow;
}
//======================

function fetchSettings(){ //Called once on startup
    fetch('http://localhost:32270/cct/info').then((response) => response.json()).then((data) => console.log(data));
    settings = defaultSettings;

    var url = "http://localhost:32270/cct/overlaySettings";
    fetch(url, { headers: { "Accept":"application/json" }}).then((response) => {
        return response.json();
    }).then((responseObj) => {
        applySettings(responseObj);
        intervalHandle = setInterval(fetchModState, getSettingValueOrDefault("refresh-time-ms"));
        
    }).catch((error) => {
        console.log("Failed to fetch settings from mod. Is it running?");
        console.log(error);
        applySettings({ errorCode: 99, errorMessage: "Failed to fetch settings from mod. Is it running?"});
        displayCriticalError(99, "Failed to fetch settings from mod. Is it running?");
    });
}

var checkedForUpdate = false; //Flag to prevent multiple update checks
var updateSkipCounter = -1; //Counter to delay start of usual overlay stuff
function fetchModState(){ //Called once per second
    var url = "http://localhost:32270/cct/state";
    fetch(url, { headers: { "Accept":"text/plain" }}).then((response) => {
        return response.text();
    }).then((responseText) => {
        previousRoomName = currentRoomName;

        //split the response into lines
        var lines = responseText.split("\n");
        
        //ignore the first line, join the rest
        var responseText = lines.slice(1).join("\n");
        

        var newCurrentRoom = parseRoomData(responseText, true, "stats-display");
        modState = newCurrentRoom.state;

        var hasUpdate = updateModState();
        if(!checkedForUpdate && hasUpdate){
            checkedForUpdate = true;
            var updateTimeMs = getSettingValueOrDefault("refresh-time-ms");
            var timeToSkip = 5 * 1000;
            updateSkipCounter = Math.floor(timeToSkip / updateTimeMs);

            document.getElementById("stats-center").innerHTML = "An update is available! "+overlayVersion.version+" -> "+modState.modVersion.version;
        }
        if(updateSkipCounter > 0){
            updateSkipCounter--;
            return;
        }


        if(currentRoomName != null)
            previousRoomRaw = getCurrentRoom();
        setCurrentRoom(newCurrentRoom, newCurrentRoom.name);

        var roomToDisplayStats = null;
        var isSelecting = false;
        if(currentSelectedRoomName != null){
            roomToDisplayStats = currentChapterRoomObjs[currentSelectedRoomName];
            isSelecting = true;
        }

        if(roomToDisplayStats == null || roomToDisplayStats == undefined){
            roomToDisplayStats = getCurrentRoom();
            isSelecting = false;
        }

        var textLeft = getSettingValueOrDefault("text-format-left");
        var textMiddle = getSettingValueOrDefault("text-format-center");
        var textRight = getSettingValueOrDefault("text-format-right");

        //combine the three text into one string, separated by newlines and ignoring empty texts
        var combinedBody = "";
        if(textLeft != null && textLeft != ""){
            combinedBody += textLeft + "\n";
        } else {
            combinedBody += " \n";
        }
        if(textMiddle != null && textMiddle != ""){
            combinedBody += textMiddle + "\n";
        } else {
            combinedBody += " \n";
        }
        if(textRight != null && textRight != ""){
            combinedBody += textRight + "\n";
        } else {
            combinedBody += " \n";
        }

        fetch("http://localhost:32270/cct/parseFormat", {
            method: "POST",
            body: combinedBody,
        }).then((response) => response.json()).then((responseObj) => {
            if(responseObj.errorCode != 0){
                var textFormattedLeft = "";
                var textFormattedMiddle = "";
                var textFormattedRight = "";
            } else {
                var textFormattedLeft = responseObj.formats[0];
                var textFormattedMiddle = responseObj.formats[1];
                var textFormattedRight = responseObj.formats[2];
            }
            
            updateStatsText("stats-left", textFormattedLeft, roomToDisplayStats, isSelecting);
            updateStatsText("stats-center", textFormattedMiddle, roomToDisplayStats, isSelecting);
            updateStatsText("stats-right", textFormattedRight, roomToDisplayStats, isSelecting);
        });

        updateGoldenPB();
        displayRoomAttempts(roomToDisplayStats);


        if((previousRoomName != null && previousChapterName != modState.chapterName) || (previousRoomName == null && currentRoomName != null) || currentChapterPath == null){
            //Update the chapter layout
            previousChapterName = modState.chapterName;
            updateChapterLayout(modState.chapterName);
            
        } else if(previousRoomName != null && !areRoomsEqual(previousRoomRaw, getCurrentRoom())){
            //Update only one room
            updateRoomInLayout(getPreviousRoom(), getCurrentRoom());
        }
    }).catch((error) => console.log(error));
}

function updateModState(){
    if(trackingPausedElement != null){
        if(modState.isTrackingPaused && getSettingValueOrDefault("tracking-disabled-message-enabled")){
            trackingPausedElement.style.display = "block";
        } else {
            trackingPausedElement.style.display = "none";
        }
    }

    if(!checkedForUpdate){
        var modVersionObj = modState.modVersion; //Obj with version and major/minor/patch numbers
        //Check if overlay version is outdated compared to the mod version
        var isOutdated = false;
        if(modVersionObj.major > overlayVersion.major){
            isOutdated = true;
        }
        if(modVersionObj.major == overlayVersion.major && modVersionObj.minor > overlayVersion.minor){
            isOutdated = true;
        }
        if(modVersionObj.major == overlayVersion.major && modVersionObj.minor == overlayVersion.minor && modVersionObj.patch > overlayVersion.patch){
            isOutdated = true;
        }

        return isOutdated;
    }
    
    return false;
}

function updateStatsText(targetId, text, room, isSelecting){
    document.getElementById(targetId).innerHTML = text;
}



function updateChapterLayout(chapterName){ //Called once per second
    var url = "http://localhost:32270/cct/currentChapterStats";
    fetch(url, { headers: { "Accept":"text/plain" }}).then((response) => response.text()).then((responseText) => {
        var roomStrings = responseText.split("\n");
        currentChapterRoomObjs = {};
        currentChapterElements = {};
        for(var i = 2; i < roomStrings.length; i++){ //Start at 2 because the error code is row 0 and the current room is row 1
            if(roomStrings[i].trim() == "") continue;
            var room = parseRoomData(roomStrings[i], false);
            currentChapterRoomObjs[room.name] = room;
        }
        var currentRoom = parseRoomData(roomStrings[1], false);
        setCurrentRoom(currentRoom, currentRoom.name);

        getChapterPath(chapterName, currentChapterRoomObjs);
    }).catch((error) => console.log(error));
}

function getChapterPath(chapterName, roomObjects){ //Called once per second
    var url = "http://localhost:32270/cct/currentChapterPath";
    fetch(url, { headers: { "Accept":"text/plain" }}).then((response) => response.text()).then((responseText) => {
        var lines = responseText.split("\n");
        var errorCodeLine = lines[0];
        var splitErrorCodeLine = errorCodeLine.split(";");
        var errorCode = splitErrorCodeLine[0];
        var errorMessage = splitErrorCodeLine[1];

        if(errorCode != "0"){
            displayCriticalError(errorCode, errorMessage);
        } else {
            displayCriticalError(errorCode, errorMessage);
            responseText = lines.slice(1).join("\n");
            currentChapterPath = parseChapterPath(responseText);
            displayRoomObjects(roomObjects);
        }
    }).catch((error) => console.log(error));
}

function displayCriticalError(errorCode, errorMessage){
    if(errorCode != 0){
        currentChapterPath = null;
        currentCheckpointObj = null;
        currentCheckpointRoomIndex = null;
        
        document.getElementById("chapter-container").innerHTML = errorMessage;
        hideGoldenShareDisplay();
        hideRoomAttemptDisplay();
        hideGoldenPBDisplay();
    } else {
        applySettingsForGoldenShareDisplay();
        applySettingsForRoomAttemptDisplay();
        applySettingsForGoldenPBDisplay();
    }
}



var roomAttemptsInitialized = false;
function displayRoomAttempts(roomToDisplayStats){
    if(!roomAttemptsInitialized){
        roomAttemptsInitialized = true;
        applySettingsForRoomAttemptDisplay();
    }

    var fontSize = getSettingValueOrDefault("room-attempts-font-size");
    var circleSize = getSettingValueOrDefault("room-attempts-circle-size");
    var isColorblindMode = getSettingValueOrDefault("colorblind-mode-enabled");

    var amountAttempts = getSelectedRateNumber();

    var container = document.getElementById("room-attempts-container");
    container.innerHTML = "";

    //Start element
    var startElement = document.createElement("div");
    startElement.className = "room-attempts-start-end";
    startElement.innerHTML = getSettingValueOrDefault("room-attempts-new-text");
    startElement.style.fontSize = fontSize;
    container.appendChild(startElement);

    //Iterate the attempts in roomToDisplayStats
    for(var i = 0; i < roomToDisplayStats.attempts.length; i++){
        var attempt = roomToDisplayStats.attempts[i];

        if(i >= amountAttempts){
            break;
        }

        /* Create the element:
        <div class="room-attempts-element">
			<div class="room-attempts-circle">
				<div class="room-attempts-circle-inner green"></div>
			</div>
		</div>
        */
        var attemptElement = document.createElement("div");
        attemptElement.className = "room-attempts-element";

        var circleElement = document.createElement("div");
        circleElement.className = "room-attempts-circle";
        circleElement.style.width = circleSize;
        circleElement.style.height = circleSize;

        var circleInnerElement = document.createElement("div");
        circleInnerElement.className = "room-attempts-circle-inner";
        circleInnerElement.style.width = circleSize;
        circleInnerElement.style.height = circleSize;

        if(attempt){
            circleInnerElement.classList.add(isColorblindMode ? "green-colorblind" : "green");
        } else {
            circleInnerElement.classList.add(isColorblindMode ? "red-colorblind" : "red");
        }

        circleElement.appendChild(circleInnerElement);
        attemptElement.appendChild(circleElement);

        container.appendChild(attemptElement);
    }

    //End element
    var endElement = document.createElement("div");
    endElement.className = "room-attempts-start-end";
    endElement.innerHTML = getSettingValueOrDefault("room-attempts-old-text");
    endElement.style.fontSize = fontSize;
    container.appendChild(endElement);
}

//Creates HTML elements for all room objects and saves them in currentChapterElements
function displayRoomObjects(roomObjs){
    var container = document.getElementById("chapter-container");
    container.innerHTML = "";

    var borderMult = getSettingValueOrDefault("chapter-bar-border-width-multiplier");

    //Add the tracking paused element
    trackingPausedElement = document.createElement("div");
    trackingPausedElement.id = "tracking-paused";
    trackingPausedElement.innerText = "Tracking is paused";
    trackingPausedElement.style.display = "none";
    container.appendChild(trackingPausedElement);

    //Add the start element
    var startElement = document.createElement("div");
    startElement.className = "start-end-element";
    container.appendChild(startElement);


    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        if(checkpointIndex != 0){ //Skip checkpoint element for first and last
            var checkpointElement = document.createElement("div");
            checkpointElement.classList.add("checkpoint-element");
            checkpointElement.style.flex = (5 * borderMult) + "";
            container.appendChild(checkpointElement);
        }
        
        var roomsObj = currentChapterPath[checkpointIndex].rooms;

        for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){
            var roomName = currentChapterPath[checkpointIndex].rooms[roomIndex];
            var room = getRoomByNameOrDefault(roomObjs, roomName);

            currentChapterRoomCheckpoints[roomName] = {
                checkpoint: currentChapterPath[checkpointIndex],
                roomIndex: roomIndex,
            };

            var roomElement = document.createElement("div");
            roomElement.classList.add("room-element");

            var classColor = getColorClass(room);
            roomElement.classList.add(classColor);

            if(room.name == getCurrentRoom().name){
                roomElement.classList.add("selected");
                currentCheckpointObj = currentChapterPath[checkpointIndex];
                currentCheckpointRoomIndex = roomIndex;
            }
            
            roomElement.setAttribute("data-room-name", room.name);
            //On hover, set a global variable to this room name
            roomElement.onmouseover = function(){
                var roomName = this.getAttribute("data-room-name");
                currentSelectedRoomName = roomName;
            }
            roomElement.onmouseleave = function(){
                currentSelectedRoomName = null;
            }

            container.appendChild(roomElement);
            currentChapterElements[roomName] = roomElement;

            if(roomIndex != roomsObj.length - 1){ //Skip border element for last room
                var borderElement = document.createElement("div");
                borderElement.classList.add("border-element");
                borderElement.style.flex = (3 * borderMult) + "";
                container.appendChild(borderElement);
            }
        }
    }

    //Add the end element
    var endElement = document.createElement("div");
    endElement.className = "start-end-element";
    container.appendChild(endElement);

    displayGoldenPBs(borderMult);
    displayGoldenShares(borderMult);
}


function displayGoldenPBs(borderMult){
    var container = document.getElementById("pb-container");
    container.innerHTML = "";

    currentChapterGoldenPBElements = {};

    //Add the start element
    var startElement = document.createElement("div");
    startElement.className = "start-end-element pb";
    container.appendChild(startElement);


    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        var checkpointElement = null;
        if(checkpointIndex != 0){ //Skip checkpoint element for first and last
            checkpointElement = document.createElement("div");
            checkpointElement.classList.add("checkpoint-element");
            checkpointElement.classList.add("pb");
            checkpointElement.style.flex = (5 * borderMult) + "";
            container.appendChild(checkpointElement);
        }
        
        var roomsObj = currentChapterPath[checkpointIndex].rooms;

        for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){

            var borderElementRef = null;
            if(roomIndex != 0){ //Skip border element for first room
                var borderElement = document.createElement("div");
                borderElement.classList.add("border-element");
                borderElement.classList.add("pb");
                borderElement.style.flex = (3 * borderMult) + "";
                container.appendChild(borderElement);
                borderElementRef = borderElement;
            } else if(checkpointIndex != 0){
                borderElementRef = checkpointElement;
            }
            
            var roomName = currentChapterPath[checkpointIndex].rooms[roomIndex];

            var roomElementLeft = document.createElement("div");
            roomElementLeft.classList.add("room-element");
            roomElementLeft.classList.add("pb");
            container.appendChild(roomElementLeft);

            var roomElementMiddle = document.createElement("div");
            roomElementMiddle.classList.add("berry-element");
            roomElementMiddle.classList.add("pb");
            roomElementMiddle.classList.add("hidden");
            container.appendChild(roomElementMiddle);

            var berryImageElement = document.createElement("img");
            berryImageElement.src = "img/goldberry.gif";
            berryImageElement.classList.add("berry-image");
            roomElementMiddle.appendChild(berryImageElement);

            var roomElementRight = document.createElement("div");
            roomElementRight.classList.add("room-element");
            roomElementRight.classList.add("pb");
            container.appendChild(roomElementRight);

            if(roomName == getCurrentRoom().name){
                roomElementLeft.classList.add("selected");
                roomElementRight.classList.add("selected");
            }

            currentChapterGoldenPBElements[roomName] = {
                left: roomElementLeft,
                middle: roomElementMiddle,
                right: roomElementRight,
                leftBorder: borderElementRef,
            };
        }
    }

    //Add the end element
    var endElement = document.createElement("div");
    endElement.className = "start-end-element pb";
    container.appendChild(endElement);
}


function displayGoldenShares(borderMult){
    currentChapterGoldenShareCheckpointElements = {};

    var container = document.getElementById("golden-share-container");
    container.innerHTML = "";

    //Add the start element
    var startElement = document.createElement("div");
    startElement.className = "golden-share-start-end";
    container.appendChild(startElement);

    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        if(checkpointIndex != 0){ //Skip checkpoint element for first and last
            var checkpointElement = document.createElement("div");
            checkpointElement.classList.add("golden-share-checkpoint-delim");
            checkpointElement.style.flex = (5 * borderMult) + "";
            container.appendChild(checkpointElement);
        }
        
        var checkpointObj = currentChapterPath[checkpointIndex];

        var checkpointElement = document.createElement("div");
        checkpointElement.classList.add("golden-share-checkpoint");
        checkpointElement.style.flex = checkpointObj.rooms.length * 50 + (checkpointObj.rooms.length-1) * (3 * borderMult);
        container.appendChild(checkpointElement);

        var checkpointName = checkpointObj.name;
        currentChapterGoldenShareCheckpointElements[checkpointName] = checkpointElement;
    }

    
    //Add the end element
    var endElement = document.createElement("div");
    endElement.className = "golden-share-start-end";
    container.appendChild(endElement);
    
    updateGoldenShares(currentChapterRoomObjs);
    applySettingsForGoldenShareDisplay();
}


function updateGoldenPB(text){
    if(currentChapterPath == null || currentChapterRoomObjs == null){
        return text;
    }

    var pbRoomName = null;
    var pbRoomNameSession = null;

    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        var checkpointObj = currentChapterPath[checkpointIndex];
        var roomsObj = checkpointObj.rooms;

        for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){
            var roomName = roomsObj[roomIndex];
            var room = getRoomByNameOrDefault(currentChapterRoomObjs, roomName);

            if(room.goldenBerryDeaths != 0 || (modState.holdingGolden && roomName == currentRoomName) || pbRoomName == null){
                pbRoomName = roomName;
            }
            if(room.goldenBerryDeathsSession != 0 || (modState.holdingGolden && roomName == currentRoomName) || pbRoomNameSession == null){
                pbRoomNameSession = roomName;
            }
        }
    }

    let foundPB = false;
    
    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        var checkpointObj = currentChapterPath[checkpointIndex];
        var roomsObj = checkpointObj.rooms;

        for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){
            var roomName = roomsObj[roomIndex];
            var room = getRoomByNameOrDefault(currentChapterRoomObjs, roomName);
            var roomCP = currentChapterRoomCheckpoints[roomName];

            var roomElements = currentChapterGoldenPBElements[roomName];

            if(roomName == pbRoomName){ //At PB
                foundPB = true;
                roomElements.left.classList.add("gold");
                roomElements.middle.classList.remove("hidden");
                roomElements.right.classList.remove("gold");
                if(roomElements.leftBorder != null){
                    roomElements.leftBorder.classList.add("gold");
                }

                continue;
            }

            if(!foundPB){ //Before PB
                roomElements.left.classList.add("gold");
                if(modState.holdingGolden && roomName == currentRoomName){
                    roomElements.middle.classList.remove("hidden");
                } else {
                    roomElements.middle.classList.add("hidden");
                }
                roomElements.right.classList.add("gold");
                
                if(roomElements.leftBorder != null){
                    roomElements.leftBorder.classList.add("gold");
                }

            } else { //After PB
                roomElements.left.classList.remove("gold");
                roomElements.middle.classList.add("hidden");
                roomElements.right.classList.remove("gold");
                if(roomElements.leftBorder != null){
                    roomElements.leftBorder.classList.remove("gold");
                }
            }
        }
    }

    return text;
}


function updateRoomInLayout(previousRoom, currentRoom){
    console.log("Updating room in layout: "+previousRoom.name+" -> "+currentRoom.name);

    var currentRoomElem = currentChapterElements[currentRoom.name];
    console.log("Current room elem: "+JSON.stringify(currentRoomElem));

    if(previousRoom != null){
        var previousRoomElem = currentChapterElements[previousRoom.name];
        if(previousRoomElem === undefined || previousRoom.name == currentRoom.name){ //Died in room
            
        } else {
            previousRoomElem.classList.remove("selected");
        }

        if(currentChapterGoldenPBElements != null && currentChapterGoldenPBElements[previousRoom.name] !== undefined){
            currentChapterGoldenPBElements[previousRoom.name].left.classList.remove("selected");
            // currentChapterGoldenPBElements[previousRoom.name].middle.classList.remove("hidden");
            currentChapterGoldenPBElements[previousRoom.name].right.classList.remove("selected");
        }

        updateChapterStats(modState.chapterName);
    }
    
    if(currentRoomElem !== undefined){
        currentRoomElem.classList.add("selected");
    }

    if(currentChapterGoldenPBElements != null && currentChapterGoldenPBElements[currentRoom.name] !== undefined){
        currentChapterGoldenPBElements[currentRoom.name].left.classList.add("selected");
        currentChapterGoldenPBElements[currentRoom.name].right.classList.add("selected");
    }
}

//Fetches the current chapter stats and calls an update with the room objects
function updateChapterStats(chapterName){
    var url = "http://localhost:32270/cct/currentChapterStats";
    fetch(url, { headers: { "Accept":"text/plain" }}).then((response) => response.text()).then((responseText) => {
        var roomStrings = responseText.split("\n");
        currentChapterRoomObjs = {};

        for(var i = 1; i < roomStrings.length; i++){ //Start at 1 because the current room is always row 0
            if(roomStrings[i].trim() == "") continue;
            var room = parseRoomData(roomStrings[i], false);
            currentChapterRoomObjs[room.name] = room;
        }

        updateRoomObjects(currentChapterRoomObjs);
    }).catch((error) => console.log(error));
}

//Updates the already existing HTML elements with new data
function updateRoomObjects(roomObjs){
    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        var roomsObj = currentChapterPath[checkpointIndex].rooms;
        for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){
            var roomName = roomsObj[roomIndex];
            var room = getRoomByNameOrDefault(roomObjs, roomName);
            var roomElement = currentChapterElements[roomName];
            var classColor = getColorClass(room);
            roomElement.classList.remove("light-green");
            roomElement.classList.remove("light-green-colorblind");
            roomElement.classList.remove("green");
            roomElement.classList.remove("green-colorblind");
            roomElement.classList.remove("yellow");
            roomElement.classList.remove("yellow-colorblind");
            roomElement.classList.remove("red");
            roomElement.classList.remove("red-colorblind");
            roomElement.classList.remove("gray");
            roomElement.classList.add(classColor);

            if(room.name == getCurrentRoom().name){
                currentCheckpointObj = currentChapterPath[checkpointIndex];
                currentCheckpointRoomIndex = roomIndex;
            }
        }
    }

    updateGoldenShares(currentChapterRoomObjs);
}



//Updates the already existing HTML elements with new data
function updateGoldenShares(roomObjs){
    var totalGoldenDeaths = 0;
    var totalGoldenDeathsSession = 0;
    var checkpointDeathsObj = {};
    var checkpointDeathsSessionObj = {};
    
    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        var checkpointObj = currentChapterPath[checkpointIndex];
        var roomsObj = checkpointObj.rooms;

        checkpointDeathsObj[checkpointObj.name] = 0;
        checkpointDeathsSessionObj[checkpointObj.name] = 0;

        for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){
            var roomName = roomsObj[roomIndex];
            var room = getRoomByNameOrDefault(roomObjs, roomName);
            totalGoldenDeaths += room.goldenBerryDeaths;
            totalGoldenDeathsSession += room.goldenBerryDeathsSession;
            checkpointDeathsObj[checkpointObj.name] += room.goldenBerryDeaths;
            checkpointDeathsSessionObj[checkpointObj.name] += room.goldenBerryDeathsSession;
        }
    }


    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        var checkpointObj = currentChapterPath[checkpointIndex];
        var goldenShareElement = currentChapterGoldenShareCheckpointElements[checkpointObj.name];

        var roomsObj = checkpointObj.rooms;
        var checkpointDeaths = checkpointDeathsObj[checkpointObj.name];
        var checkpointDeathsSession = checkpointDeathsSessionObj[checkpointObj.name];

        var goldenDisplay = 0;
        var addition = "";

        if(getSettingValueOrDefault("golden-share-style-percent")){
            if(totalGoldenDeaths == 0){
                goldenDisplay = 0;
            } else {
                goldenDisplay = ((checkpointDeaths / totalGoldenDeaths) * 100).toFixed(0);
            }
            addition = "%";
        } else {
            goldenDisplay = checkpointDeaths+"";
        }

        var goldenDisplaySession = 0;
        var additionSession = "";
        if(getSettingValueOrDefault("golden-share-style-percent")){
            if(totalGoldenDeathsSession == 0){
                goldenDisplaySession = 0;
            } else {
                goldenDisplaySession = ((checkpointDeathsSession / totalGoldenDeathsSession) * 100).toFixed(0);
            }
            additionSession = "%";
        } else {
            goldenDisplaySession = checkpointDeathsSession+"";
        }

        var combined = "";
        if(getSettingValueOrDefault("golden-share-show-current-session")){
            combined = goldenDisplay+addition+" ("+goldenDisplaySession+additionSession+")";
        } else {
            combined = goldenDisplay+addition;
        }

        goldenShareElement.innerHTML = combined;
    }
}



function getCurrentRoom(){
    return currentChapterRoomObjs[currentRoomName];
}
function setCurrentRoom(room, roomName){
    currentRoomName = roomName;
    return currentChapterRoomObjs[roomName] = room;
}
function getPreviousRoom(){
    return currentChapterRoomObjs[previousRoomName];
}
function getRoomByNameOrDefault(roomObjs, roomDebugName){
    //If roomObjs has a key with the same name as roomDebugName, return that room
    if(roomObjs[roomDebugName]){
        return roomObjs[roomDebugName];
    }

    return {
        attempts: [],
        goldenBerryDeaths: 0,
        goldenBerryDeathsSession: 0,
        rate5: NaN,
        rate10: NaN,
        rate20: NaN,
        rateMax: NaN,
    };
}

function getColorClass(room){
    var compareAgainstRate = getSelectedRateOfRoom(room);
    
    var lightGreenCutoff = getSettingValueOrDefault("light-green-cutoff");
    var greenCutoff = getSettingValueOrDefault("green-cutoff");
    var yellowCutoff = getSettingValueOrDefault("yellow-cutoff");

    var isColorblindMode = getSettingValueOrDefault("colorblind-mode-enabled");

    if(isNaN(compareAgainstRate)){
        return "gray";
    } else if(compareAgainstRate >= lightGreenCutoff){
        return isColorblindMode ? "light-green-colorblind" : "light-green";
    } else if(compareAgainstRate >= greenCutoff){
        return isColorblindMode ? "green-colorblind" : "green";
    } else if(compareAgainstRate >= yellowCutoff){
        return isColorblindMode ? "yellow-colorblind" : "yellow";
    } else {
        return isColorblindMode ? "red-colorblind" : "red";
    }
}

function getSelectedRateOfRoom(room){
    var selectedRate = getSettingValueOrDefault("attempts");
    if(selectedRate == "5"){
        return room.rate5;
    } else if(selectedRate == "10"){
        return room.rate10;
    } else if(selectedRate == "20"){
        return room.rate20;
    } else {
        return room.rateMax;
    }
}
function getSelectedRateNumber(){
    var selectedRate = getSettingValueOrDefault("attempts");
    if(selectedRate == "5"){
        return 5;
    } else if(selectedRate == "10"){
        return 10;
    } else if(selectedRate == "20"){
        return 20;
    } else {
        return 9999999;
    }
}

function roomsInSameCheckpoint(room, otherRoom){
    var roomCP = currentChapterRoomCheckpoints[room.name];
    var otherRoomCP = currentChapterRoomCheckpoints[otherRoom.name];

    if(otherRoomCP === undefined) return false;

    if(roomCP.checkpoint === undefined || otherRoomCP.checkpoint === undefined){
        return false;
    }

    return roomCP.checkpoint.name == otherRoomCP.checkpoint.name;
}

function calculateRemainingGoldenChance(roomToCalc, toNow=false){
    var remainingGoldenChance = 1;
    var skipMode = !toNow;

    if(currentChapterPath == null){
        return 0;
    }

    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        var roomsObj = currentChapterPath[checkpointIndex].rooms;
        for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){
            var roomName = roomsObj[roomIndex];
            var room = getRoomByNameOrDefault(currentChapterRoomObjs, roomName);

            if(room.name == roomToCalc.name){ //Found current room, disable skip mode and start calculating from here
                skipMode = !skipMode;
            }

            if(!skipMode){
                var roomRate = getSelectedRateOfRoom(room);
                if(!isNaN(roomRate)){
                    remainingGoldenChance *= getSelectedRateOfRoom(room);
                } else {
                    remainingGoldenChance = 0; //If there is a room that was not yet played, chances are 0%
                }
            }

            if(remainingGoldenChance == 0) break; //No need to keep calculating if chances are 0%
        }
    }

    return remainingGoldenChance;
}



let baseKey = "base";
let selOverrideKey = "selected-override";
let overridesKey = "overrides";
function getSettingValueOrDefault(settingName){
    var selectedOverride = settings[selOverrideKey];
    var baseSettings = settings[baseKey];

    var isTextFormat = settingName === "text-format-left" || settingName === "text-format-right" || settingName === "text-format-center";
    if(isTextFormat){
        if(baseSettings[settingName] === ""){
            return baseSettings[settingName];
        }
    }

    if(selectedOverride !== "" && selectedOverride !== null){
        var overrides = settings[overridesKey];
        if(overrides[selectedOverride] !== undefined){
            var override = overrides[selectedOverride];
            if(override[settingName] !== undefined){
                return override[settingName];
            }
        }
    }


    if(baseSettings.hasOwnProperty(settingName)){
        return baseSettings[settingName];
    } else {
        return defaultSettings[baseKey][settingName];
    }
}

function getChapterRoomCount(){
    var count = 0;
    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        var roomsObj = currentChapterPath[checkpointIndex].rooms;
        count += roomsObj.length;
    }
    return count;
}