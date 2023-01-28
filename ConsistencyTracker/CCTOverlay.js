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
    "version": "2.0.0",
    "major": 2,
    "minor": 0,
    "patch": 0
};
let modState = null;

//Actual RoomStats objects
let currentRoomStats = null;
let previousRoomStats = null;

//Chapter unique identifiers
let currentChapterName = null;
let previousChapterName = null;

let currentChapterStats = {}; //Stats objects for each room in the chapter (independent of the path)
let currentChapterElements = {}; //HTML elements for the path
let currentChapterPath = null; //Path of the chapter
let currentSelectedRoomName = null;

//HTML elements for various displays
let currentChapterGoldenShareCheckpointElements = {};
let currentChapterGoldenPBElements = null;
let currentChapterGoldenPBPreviousElement = null;
let trackingPausedElement = null;


var fetchSettingsIntervalHandle = null;
document.addEventListener('DOMContentLoaded', function() {
    fetchSettingsIntervalHandle = setInterval(fetchSettings, 1000);
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
        if(responseObj.errorCode != 0){
            console.log("[fetchSettings] Error ("+responseObj.errorCode+") loading settings from mod: "+responseObj.errorMessage);
            applySettings(responseObj);
            displayCriticalError(responseObj.errorCode, responseObj.errorMessage);
            return;
        }

        clearInterval(fetchSettingsIntervalHandle);

        console.log("Overlay settings from mod:");
        console.log(responseObj);
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
    fetch(url, { headers: { "Accept":"application/json" }}).then((response) => {
        return response.json();
    }).then((responseObj) => {
        if(responseObj.errorCode != 0){
            displayCriticalError(responseObj.errorCode, responseObj.errorMessage);
            console.log("[fetchModState] Error ("+responseObj.errorCode+") fetching state from mod: "+responseObj.errorMessage);
            return;
        }

        modState = responseObj.modState;

        var hasUpdate = updateModState();
        if(!checkedForUpdate && hasUpdate){
            checkedForUpdate = true;
            var updateTimeMs = getSettingValueOrDefault("refresh-time-ms");
            var timeToSkip = 1 * 1000; //Change back to 5
            updateSkipCounter = Math.floor(timeToSkip / updateTimeMs);

            document.getElementById("stats-center").innerHTML = "An update is available! "+overlayVersion.version+" -> "+modState.overlayVersion;
        }
        if(updateSkipCounter > 0){
            updateSkipCounter--;
            return;
        }

        previousRoomStats = currentRoomStats;
        currentRoomStats = responseObj.currentRoom;
        currentChapterName = responseObj.chapterName;
        

        var roomStatsToDisplay = null;
        var isSelecting = false;
        // if(currentSelectedRoomName != null){ //Currently broken due to live-data fetching being restricted to the current room only
        //     roomToDisplayStats = currentChapterRoomObjs[currentSelectedRoomName];
        //     isSelecting = true;
        // }

        if(roomStatsToDisplay == null){
            roomStatsToDisplay = currentRoomStats;
            isSelecting = false;
        }

        var textLeft = getSettingValueOrDefault("text-format-left");
        var textMiddle = getSettingValueOrDefault("text-format-center");
        var textRight = getSettingValueOrDefault("text-format-right");

        var body = JSON.stringify({
            formats: [
                textLeft,
                textMiddle,
                textRight
            ]
        });

        fetch("http://localhost:32270/cct/parseFormat", {
            method: "POST",
            body: body,
        }).then((response) => response.json()).then((responseObj) => {
            // console.log(responseObj);
            if(responseObj.errorCode != 0){
                console.log("[fetchModState->parseFormat] Error ("+responseObj.errorCode+") fetching /cct/parseFormat: "+responseObj.errorMessage);
                var textFormattedLeft = "";
                var textFormattedMiddle = "";
                var textFormattedRight = "";
            } else {
                var textFormattedLeft = responseObj.formats[0];
                var textFormattedMiddle = responseObj.formats[1];
                var textFormattedRight = responseObj.formats[2];
            }
            
            updateStatsText("stats-left", textFormattedLeft);
            updateStatsText("stats-center", textFormattedMiddle);
            updateStatsText("stats-right", textFormattedRight);
        });

        updateGoldenPB();
        displayRoomAttempts(roomStatsToDisplay);


        if((previousRoomStats != null && previousChapterName != currentChapterName) || (previousRoomStats == null && currentRoomStats != null) || currentChapterPath == null){
            //Update the chapter layout
            previousChapterName = currentChapterName;
            updateChapterLayout();
            
        } else if(previousRoomStats != null && !areRoomsEqual(previousRoomStats, currentRoomStats)){
            //Update only one room
            updateRoomInLayout(previousRoomStats, currentRoomStats);
        }
    }).catch((error) => console.log(error));
}

function updateModState(){
    if(trackingPausedElement != null){
        if(modState.deathTrackingPaused && getSettingValueOrDefault("tracking-disabled-message-enabled")){
            trackingPausedElement.style.display = "block";
        } else {
            trackingPausedElement.style.display = "none";
        }
    }

    if(!checkedForUpdate){
        var modOverlayVersionObj = SplitVersionNumber(modState.overlayVersion); //Obj with version and major/minor/patch numbers
        //Check if overlay version is outdated compared to the mod version
        var isOutdated = false;
        if(modOverlayVersionObj.major > overlayVersion.major){
            isOutdated = true;
        }
        if(modOverlayVersionObj.major == overlayVersion.major && modOverlayVersionObj.minor > overlayVersion.minor){
            isOutdated = true;
        }
        if(modOverlayVersionObj.major == overlayVersion.major && modOverlayVersionObj.minor == overlayVersion.minor && modOverlayVersionObj.patch > overlayVersion.patch){
            isOutdated = true;
        }

        return isOutdated;
    }
    
    return false;
}

function updateStatsText(targetId, text){
    document.getElementById(targetId).innerHTML = text;
}



function updateChapterLayout(){ //Called once per chapter change
    var url = "http://localhost:32270/cct/currentChapterStats";
    fetch(url, { headers: { "Accept":"application/json" }}).then((response) => response.json()).then((responseObj) => {
        if(responseObj.errorCode != 0){
            console.log("[updateChapterLayout] Error ("+responseObj.errorCode+") fetching chapter stats from mod: "+responseObj.errorMessage);
            return;
        }

        currentChapterStats = responseObj.chapterStats;
        currentChapterElements = {};

        getChapterPath();
    }).catch((error) => console.log(error));
}

function getChapterPath(){ //Called once per second
    var url = "http://localhost:32270/cct/currentChapterPath";
    fetch(url, { headers: { "Accept":"application/json" }}).then((response) => response.json()).then((responseObj) => {
        if(responseObj.errorCode != 0){
            console.log("[getChapterPath] Error ("+responseObj.errorCode+") fetching chapter stats from mod: "+responseObj.errorMessage);
            displayCriticalError(responseObj.errorCode, responseObj.errorMessage);
        } else {
            displayCriticalError(responseObj.errorCode, responseObj.errorMessage);
            currentChapterPath = responseObj.path;
            displayRoomObjects();
        }
    }).catch((error) => console.log(error));
}

function displayCriticalError(errorCode, errorMessage){
    if(errorCode != 0){
        currentChapterPath = null;
        
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
function displayRoomAttempts(roomStatsToDisplay){
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
    var countCircles = 0;
    for(var i = roomStatsToDisplay.previousAttempts.length-1; i >= 0; i--){
        var attempt = roomStatsToDisplay.previousAttempts[i];

        if(countCircles >= amountAttempts){
            break;
        }
        countCircles++;

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
function displayRoomObjects(){
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


    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.checkpoints.length; checkpointIndex++){
        if(checkpointIndex != 0){ //Skip checkpoint element for first and last
            var checkpointElement = document.createElement("div");
            checkpointElement.classList.add("checkpoint-element");
            checkpointElement.style.flex = (5 * borderMult) + "";
            container.appendChild(checkpointElement);
        }
        
        var checkpointRoomsObj = currentChapterPath.checkpoints[checkpointIndex].rooms;

        for(var roomIndex = 0; roomIndex < checkpointRoomsObj.length; roomIndex++){
            var roomName = checkpointRoomsObj[roomIndex].debugRoomName;
            var roomStats = getRoomStatsByNameOrDefault(roomName);

            var roomElement = document.createElement("div");
            roomElement.classList.add("room-element");

            var classColor = getColorClass(roomStats);
            roomElement.classList.add(classColor);

            if(roomName == currentRoomStats.debugRoomName){ //Find current room and give it special styling
                roomElement.classList.add("selected");
            }
            
            roomElement.setAttribute("data-room-name", roomName);
            //On hover, set a global variable to this room name
            roomElement.onmouseover = function(){
                var roomName = this.getAttribute("data-room-name");
                currentSelectedRoomName = roomName;
            }
            roomElement.onmouseleave = function(){
                currentSelectedRoomName = null;
            }
            roomElement.onclick = function(){
                var roomName = this.getAttribute("data-room-name");
                fetch("http://localhost:32270/tp?level=" + roomName);
            };

            container.appendChild(roomElement);
            currentChapterElements[roomName] = roomElement;

            if(roomIndex != checkpointRoomsObj.length - 1){ //Skip border element for last room
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


    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.checkpoints.length; checkpointIndex++){
        var checkpointElement = null;
        if(checkpointIndex != 0){ //Skip checkpoint element for first and last
            checkpointElement = document.createElement("div");
            checkpointElement.classList.add("checkpoint-element");
            checkpointElement.classList.add("pb");
            checkpointElement.style.flex = (5 * borderMult) + "";
            container.appendChild(checkpointElement);
        }
        
        var checkpointRoomsObj = currentChapterPath.checkpoints[checkpointIndex].rooms;

        for(var roomIndex = 0; roomIndex < checkpointRoomsObj.length; roomIndex++){

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
            
            var roomName = checkpointRoomsObj[roomIndex].debugRoomName;

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

            if(roomName == currentRoomStats.debugRoomName){
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

    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.checkpoints.length; checkpointIndex++){
        if(checkpointIndex != 0){ //Skip checkpoint element for first and last
            var checkpointElement = document.createElement("div");
            checkpointElement.classList.add("golden-share-checkpoint-delim");
            checkpointElement.style.flex = (5 * borderMult) + "";
            container.appendChild(checkpointElement);
        }
        
        var checkpointObj = currentChapterPath.checkpoints[checkpointIndex];

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
    
    updateGoldenShares();
    applySettingsForGoldenShareDisplay();
}


function updateGoldenPB(text){
    if(currentChapterPath == null || currentChapterStats == null){
        return text;
    }

    var pbRoomName = null;
    var pbRoomNameSession = null;

    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.checkpoints.length; checkpointIndex++){
        var checkpointObj = currentChapterPath.checkpoints[checkpointIndex];
        var checkpointRoomsObj = checkpointObj.rooms;

        for(var roomIndex = 0; roomIndex < checkpointRoomsObj.length; roomIndex++){
            var roomName = checkpointRoomsObj[roomIndex].debugRoomName;
            var room = getRoomStatsByNameOrDefault(roomName);

            if(room.goldenBerryDeaths != 0 || (modState.playerIsHoldingGolden && roomName == currentRoomStats.debugRoomName) || pbRoomName == null){
                pbRoomName = roomName;
            }
            if(room.goldenBerryDeathsSession != 0 || (modState.playerIsHoldingGolden && roomName == currentRoomStats.debugRoomName) || pbRoomNameSession == null){
                pbRoomNameSession = roomName;
            }
        }
    }

    let foundPB = false;
    
    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.checkpoints.length; checkpointIndex++){
        var checkpointObj = currentChapterPath.checkpoints[checkpointIndex];
        var checkpointRoomsObj = checkpointObj.rooms;

        for(var roomIndex = 0; roomIndex < checkpointRoomsObj.length; roomIndex++){
            var roomName = checkpointRoomsObj[roomIndex].debugRoomName;
            var room = getRoomStatsByNameOrDefault(roomName);

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
                if(modState.playerIsHoldingGolden && roomName == currentRoomStats.debugRoomName){
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
    console.log("Updating room in layout: "+previousRoom.debugRoomName+" -> "+currentRoom.debugRoomName);

    var currentRoomElem = currentChapterElements[currentRoom.debugRoomName];
    console.log("Current room elem: "+JSON.stringify(currentRoomElem));

    if(previousRoom != null){
        var previousRoomElem = currentChapterElements[previousRoom.debugRoomName];
        if(previousRoomElem === undefined || previousRoom.debugRoomName == currentRoom.debugRoomName){ //Died in room
            
        } else {
            previousRoomElem.classList.remove("selected");
        }

        if(currentChapterGoldenPBElements != null && currentChapterGoldenPBElements[previousRoom.debugRoomName] !== undefined){
            currentChapterGoldenPBElements[previousRoom.debugRoomName].left.classList.remove("selected");
            // currentChapterGoldenPBElements[previousRoom.debugRoomName].middle.classList.remove("hidden");
            currentChapterGoldenPBElements[previousRoom.debugRoomName].right.classList.remove("selected");
        }

        updateChapterStats(currentChapterName);
    }
    
    if(currentRoomElem !== undefined){
        currentRoomElem.classList.add("selected");
    }

    if(currentChapterGoldenPBElements != null && currentChapterGoldenPBElements[currentRoom.debugRoomName] !== undefined){
        currentChapterGoldenPBElements[currentRoom.debugRoomName].left.classList.add("selected");
        currentChapterGoldenPBElements[currentRoom.debugRoomName].right.classList.add("selected");
    }
}

//Fetches the current chapter stats and calls an update with the room objects
function updateChapterStats(chapterName){
    var url = "http://localhost:32270/cct/currentChapterStats";
    fetch(url, { headers: { "Accept":"application/json" }}).then((response) => response.json()).then((responseObj) => {
        if(responseObj.errorCode != 0){
            console.log("[updateChapterStats] Error ("+responseObj.errorCode+") fetching chapter stats from mod: "+responseObj.errorMessage);
            return;
        }
        currentChapterStats = responseObj.chapterStats;
        updateRoomObjects();
    }).catch((error) => console.log(error));
}

//Updates the already existing HTML elements with new data
function updateRoomObjects(){
    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.checkpoints.length; checkpointIndex++){
        var checkpointRoomsObj = currentChapterPath.checkpoints[checkpointIndex].rooms;
        for(var roomIndex = 0; roomIndex < checkpointRoomsObj.length; roomIndex++){
            var roomName = checkpointRoomsObj[roomIndex].debugRoomName;
            var roomStats = getRoomStatsByNameOrDefault(roomName);
            var roomElement = currentChapterElements[roomName];
            if(roomElement == null){
                continue;
            }
            var classColor = getColorClass(roomStats);
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
        }
    }

    updateGoldenShares();
}



//Updates the already existing HTML elements with new data
function updateGoldenShares(){
    var totalGoldenDeaths = 0;
    var totalGoldenDeathsSession = 0;
    var checkpointDeathsObj = {};
    var checkpointDeathsSessionObj = {};
    
    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.checkpoints.length; checkpointIndex++){
        var checkpointObj = currentChapterPath.checkpoints[checkpointIndex];
        var checkpointRoomsObj = checkpointObj.rooms;

        checkpointDeathsObj[checkpointObj.name] = 0;
        checkpointDeathsSessionObj[checkpointObj.name] = 0;

        for(var roomIndex = 0; roomIndex < checkpointRoomsObj.length; roomIndex++){
            var roomName = checkpointRoomsObj[roomIndex].debugRoomName;
            var roomStats = getRoomStatsByNameOrDefault(roomName);
            totalGoldenDeaths += roomStats.goldenBerryDeaths;
            totalGoldenDeathsSession += roomStats.goldenBerryDeathsSession;
            checkpointDeathsObj[checkpointObj.name] += roomStats.goldenBerryDeaths;
            checkpointDeathsSessionObj[checkpointObj.name] += roomStats.goldenBerryDeathsSession;
        }
    }


    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.checkpoints.length; checkpointIndex++){
        var checkpointObj = currentChapterPath.checkpoints[checkpointIndex];
        var goldenShareElement = currentChapterGoldenShareCheckpointElements[checkpointObj.name];

        var checkpointRoomsObj = checkpointObj.rooms;
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


function getRoomStatsByNameOrDefault(roomDebugName){
    //If roomObjs has a key with the same name as roomDebugName, return that room
    if(currentChapterStats.rooms[roomDebugName]){
        return currentChapterStats.rooms[roomDebugName];
    } else {
        console.log("[getRoomStatsByNameOrDefault] Room "+roomDebugName+" not found in stats, returning default.");
    }

    return {
        debugRoomName: roomDebugName,
        goldenBerryDeaths: 0,
        goldenBerryDeathsSession: 0,
        previousAttempts: [],
        lastFiveRate: 0,
        last10Rate: 0,
        lastTwentyRate: 0,
        maxRate: 0,
        successStreak: 0,
        deathsInCurrentRun: 0,
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
        return room.lastFiveRate;
    } else if(selectedRate == "10"){
        return room.lastTenRate;
    } else if(selectedRate == "20"){
        return room.lastTwentyRate;
    } else {
        return room.maxRate;
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
