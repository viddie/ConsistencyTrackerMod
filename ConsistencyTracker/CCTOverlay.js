let settings = {};
let defaultSettings = {
    "base": {
        "attempts": "20",
        "refresh-time-ms": 1000,
        "text-format-left": "GB Total: {chapter:goldenDeaths} ({chapter:goldenDeathsSession}) [{room:goldenDeaths}]<br>Choke Rate: {room:goldenChokeRate}%<br>PB: {pb:checkpointAbbreviation}-{pb:checkpointRoomNumber} (Session: {pb:checkpointAbbreviationSession}-{pb:checkpointRoomNumberSession})",
        "text-format-center": "{checkpoint:abbreviation}-{checkpoint:roomNumber}: {room:rate}% ({room:successes}/{room:attempts})<br>CP: {checkpoint:rate}%<br>Total: {chapter:rate}%",
        "text-format-right": "Golden Chance<br>CP: {checkpoint:goldenChance}%<br>Total: {chapter:goldenChance}%<br>Room➔End: {run:roomToEndGoldenChance}%",
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
        "room-attempts-new-text": "New ➔",
        "room-attempts-old-text": "➔ Old",
        "tracking-disabled-message-enabled": true,
        "golden-pb-display-enabled": true
    },
    "selected-override": "",
    "overrides": {
        "only-room-rate": {
            "chapter-bar-enabled": false,
            "golden-share-display-enabled": false,
            "room-attempts-display-enabled": false,
            "text-format-left": "",
            "text-format-center": "{room:rate}% ({room:successes}/{room:attempts})",
            "text-format-right": ""
        },
        "only-rates": {
            "chapter-bar-enabled": false,
            "golden-share-display-enabled": false,
            "room-attempts-display-enabled": false,
            "text-format-left": "",
            "text-format-right": ""
        },
        "only-bar": {
            "chapter-bar-enabled": true,
            "golden-share-display-enabled": false,
            "room-attempts-display-enabled": false,
            "text-format-left": "",
            "text-format-center": "",
            "text-format-right": ""
        },
        "bar-and-rates": {
            "chapter-bar-enabled": true,
            "golden-share-display-enabled": false,
            "room-attempts-display-enabled": false,
            "text-format-left": "",
            "text-format-right": ""
        },
        "some-grinding-info": {
            "text-format-left": "GB Deaths Room: {room:goldenDeaths}<br>Choke Rate: {room:goldenChokeRate}%",
            "text-format-right": "",
            "room-attempts-display-enabled": false,
            "golden-share-show-current-session": false
        },
        "more-grinding-info": {
            "text-format-left": "GB Deaths: {chapter:goldenDeaths} ({chapter:goldenDeathsSession})<br>Room: {room:goldenDeaths}<br>Choke Rate: {room:goldenChokeRate}%",
            "room-attempts-new-text": "➔",
            "room-attempts-old-text": "➔"
        },
        "golden-berry-tracking-simple": {
            "text-format-left": "GB Deaths: {chapter:goldenDeaths}<br>PB: {pb:checkpointAbbreviation}-{pb:checkpointRoomNumber}",
            "text-format-center": "",
            "text-format-right": "",
            "room-attempts-display-enabled": false
        },
        "golden-berry-tracking-with-session": {
            "text-format-left": "GB Deaths: {chapter:goldenDeaths} ({chapter:goldenDeathsSession}) [{room:goldenDeaths}]<br>PB: {pb:checkpointAbbreviation}-{pb:checkpointRoomNumber} (Session: {pb:checkpointAbbreviationSession}-{pb:checkpointRoomNumberSession})",
            "text-format-center": "{checkpoint:abbreviation}-{checkpoint:roomNumber}: {room:rate}% ({room:successes}/{room:attempts})",
            "text-format-right": "",
            "room-attempts-display-enabled": false,
            "font-size-left": "30px",
            "font-size-center": "30px",
            "font-size-right": "30px"
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
    //Call updateOverlay once per second
    fetchSettings();
});


function applySettings(){
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

// show ATTEMPT DISPLAY
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


function fetchSettings(){ //Called once per second
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './CCTOverlaySettings.json', true);
    xhr.onreadystatechange = function() {
        //Get content of file
        if (xhr.readyState == 4) {
            if(xhr.status === 404){
                settings = defaultSettings;
            } else if((xhr.status === 200 || xhr.status == 0) && xhr.responseText != "") {
                settings = JSON.parse(xhr.responseText);
            } else {
                settings = defaultSettings;
            }
            
            applySettings();
            intervalHandle = setInterval(fetchModState, getSettingValueOrDefault("refresh-time-ms"));
        }
    };
    xhr.send();
}

var checkedForUpdate = false; //Flag to prevent multiple update checks
var updateSkipCounter = -1; //Counter to delay start of usual overlay stuff
function fetchModState(){ //Called once per second
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './stats/modState.txt', true);
    xhr.onreadystatechange = function() {
        if (xhr.readyState == 4) {
            if((xhr.status === 200 || xhr.status == 0) && xhr.responseText != "") {
                previousRoomName = currentRoomName;

                var newCurrentRoom = parseRoomData(xhr.responseText, true, "stats-display");
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
                updateStatsText("stats-left", textLeft, roomToDisplayStats, isSelecting);
                
                var textMiddle = getSettingValueOrDefault("text-format-center");
                updateStatsText("stats-center", textMiddle, roomToDisplayStats, isSelecting);
                
                var textRight = getSettingValueOrDefault("text-format-right");
                updateStatsText("stats-right", textRight, roomToDisplayStats, isSelecting);

                displayRoomAttempts(roomToDisplayStats);


                if((previousRoomName != null && previousChapterName != modState.chapterName) || (previousRoomName == null && currentRoomName != null) || currentChapterPath == null){
                    //Update the chapter layout
                    previousChapterName = modState.chapterName;
                    updateChapterLayout(modState.chapterName);
                    
                } else if(previousRoomName != null && !areRoomsEqual(previousRoomRaw, getCurrentRoom())){
                    //Update only one room
                    updateRoomInLayout(getPreviousRoom(), getCurrentRoom());
                }
            }
        }
    };
    xhr.send();
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
    text = replaceAll(text, "{room:name}", room.name);
    text = replaceAll(text, "{chapter:SID}", modState.chapterName);
    text = replaceAll(text, "{state:trackingPaused}", modState.isTrackingPaused == true ? "Yes" : "No");
    text = replaceAll(text, "{state:recordingPath}", modState.isRecordingEnabled == true ? "Yes" : "No");

    var selectedRate = getSettingValueOrDefault("attempts");
    if(selectedRate == "5"){
        text = replaceAll(text, "{room:rate}", (room.rate5*100).toFixed(2));
        text = replaceAll(text, "{room:successes}", room.successes5);
        text = replaceAll(text, "{room:attempts}", room.totalAttempts5);
        text = replaceAll(text, "{room:failures}", room.failures5);
    } else if(selectedRate == "10"){
        text = replaceAll(text, "{room:rate}", (room.rate10*100).toFixed(2));
        text = replaceAll(text, "{room:successes}", room.successes10);
        text = replaceAll(text, "{room:attempts}", room.totalAttempts10);
        text = replaceAll(text, "{room:failures}", room.failures10);
    } else if(selectedRate == "20"){    
        text = replaceAll(text, "{room:rate}", (room.rate20*100).toFixed(2));
        text = replaceAll(text, "{room:successes}", room.successes20);
        text = replaceAll(text, "{room:attempts}", room.totalAttempts20);
        text = replaceAll(text, "{room:failures}", room.failures20);
    } else {
        text = replaceAll(text, "{room:rate}", (room.rateMax*100).toFixed(2));
        text = replaceAll(text, "{room:successes}", room.successesMax);
        text = replaceAll(text, "{room:attempts}", room.totalAttemptsMax);
        text = replaceAll(text, "{room:failures}", room.failuresMax);
    }

    text = replaceAll(text, "{room:goldenDeaths}", room.goldenBerryDeaths);
    text = replaceAll(text, "{room:goldenDeathsSession}", room.goldenBerryDeathsSession);
    
    var roomCP = currentChapterRoomCheckpoints[room.name];
    if(roomCP === undefined){
        text = replaceAll(text, "{checkpoint:name}", "-");
        text = replaceAll(text, "{checkpoint:abbreviation}", "-");
        text = replaceAll(text, "{checkpoint:roomNumber}",  "-");
    } else {
        text = replaceAll(text, "{checkpoint:name}", roomCP.checkpoint.name);
        text = replaceAll(text, "{checkpoint:abbreviation}", roomCP.checkpoint.abbreviation);
        text = replaceAll(text, "{checkpoint:roomNumber}",  roomCP.roomIndex+1);
    }

    if(roomCP !== undefined){
        var countAttemptsChapter = 0;
        var countSuccessesChapter = 0;
        var countRoomsChapter = 0;

        var countAttemptsCheckpoint = 0;
        var countSuccessesCheckpoint = 0;
        var countRoomsCheckpoint = 0;

        var gbDeathsChapter = 0;
        var gbDeathsCheckpoint = 0;
        var gbDeathsChapterSession = 0;
        var gbDeathsCheckpointSession = 0;

        var chapterGoldenChance = 1;
        var checkpointGoldenChance = 1;
        var fromNowGoldenChance = calculateRemainingGoldenChance(room);
        var toNowGoldenChance = calculateRemainingGoldenChance(room, true);

        var deathsBeforeRoom = 0;
        var deathsBeforeRoomSession = 0;
        var deathsBeforeCheckpoint = 0;
        var deathsBeforeCheckpointSession = 0;
        var foundRoom = false;
        var foundCheckpoint = false;
        var currentCheckpointObj = null;

        var rateNumber = getSelectedRateNumber();

        //Iterate the object currentChapterRoomObjs
        for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
            var roomsObj = currentChapterPath[checkpointIndex].rooms;
            for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){
                var roomName = roomsObj[roomIndex];

                //if roomName doesnt exist in currentChapterRoomObjs, continue
                if(currentChapterRoomObjs[roomName] === undefined){
                    continue;
                }

                var roomObj = currentChapterRoomObjs[roomName];

                //Choke Rate
                if(!foundRoom && roomObj.name == room.name){
                    foundRoom = true;
                    deathsBeforeRoom = gbDeathsChapter;
                    deathsBeforeRoomSession = gbDeathsChapterSession;
                }
                if(!foundCheckpoint && roomsInSameCheckpoint(roomObj, room)){
                    foundCheckpoint = true;
                    deathsBeforeCheckpoint = gbDeathsChapter;
                    deathsBeforeCheckpointSession = gbDeathsChapterSession;
                    currentCheckpointObj = currentChapterPath[checkpointIndex];
                }



                //Golden Berry Deaths
                gbDeathsChapter += roomObj.goldenBerryDeaths;
                gbDeathsChapterSession += roomObj.goldenBerryDeathsSession;
                if(roomsInSameCheckpoint(room, roomObj)){
                    gbDeathsCheckpoint += roomObj.goldenBerryDeaths;
                    gbDeathsCheckpointSession += roomObj.goldenBerryDeathsSession;
                }



                //Calculate Golden Chances
                chapterGoldenChance *= getSelectedRateOfRoom(roomObj);
                if(roomsInSameCheckpoint(room, roomObj)){
                    checkpointGoldenChance *= getSelectedRateOfRoom(roomObj);
                }

                //Count rooms
                if(roomObj.attempts.length >= 1){
                    countRoomsChapter++;
                    if(roomsInSameCheckpoint(room, roomObj)){
                        countRoomsCheckpoint++;
                    }
                }

                //Count attempts and successes
                var max = Math.min(rateNumber, roomObj.attempts.length);
                for(var i = 0; i < max; i++){
                    countAttemptsChapter++;
                    if(roomsInSameCheckpoint(room, roomObj)){
                        countAttemptsCheckpoint++;
                    }

                    if(roomObj.attempts[i]){
                        countSuccessesChapter++;
                        if(roomsInSameCheckpoint(room, roomObj)){
                            countSuccessesCheckpoint++;
                        }
                    }
                }
            }
        }

        var chapterSuccessRate = countAttemptsChapter == 0 ? 0 : countSuccessesChapter/countAttemptsChapter;
        var checkpointSuccessRate = countAttemptsCheckpoint == 0 ? 0 : countSuccessesCheckpoint/countAttemptsCheckpoint;

        var chapterGoldenEstimateAttempts = 1 / chapterGoldenChance;
        var checkpointGoldenEstimateAttempts = 1 / checkpointGoldenChance;

        var goldenChanceDecimals = getSettingValueOrDefault("golden-chance-decimals");

        var roomChokeRate = room.goldenBerryDeaths / (gbDeathsChapter - deathsBeforeRoom);
        var roomChokeRateSession = room.goldenBerryDeathsSession / (gbDeathsChapterSession - deathsBeforeRoomSession);

        var checkpointChokeRate = gbDeathsCheckpoint / (gbDeathsChapter - deathsBeforeCheckpoint);
        var checkpointChokeRateSession = gbDeathsCheckpointSession / (gbDeathsChapterSession - deathsBeforeCheckpointSession);

        text = replaceAll(text, "{chapter:rate}", (chapterSuccessRate*100).toFixed(2));
        text = replaceAll(text, "{chapter:DPR}", ((1/chapterSuccessRate)-1).toFixed(2));
        text = replaceAll(text, "{chapter:countRooms}", getChapterRoomCount());
        text = replaceAll(text, "{chapter:goldenDeaths}", gbDeathsChapter);
        text = replaceAll(text, "{chapter:goldenDeathsSession}", gbDeathsChapterSession);
        text = replaceAll(text, "{chapter:goldenChance}", (chapterGoldenChance*100).toFixed(goldenChanceDecimals));
        text = replaceAll(text, "{chapter:goldenEstimateAttempts}", chapterGoldenEstimateAttempts.toFixed(0));

        text = replaceAll(text, "{checkpoint:rate}", (checkpointSuccessRate*100).toFixed(2));
        text = replaceAll(text, "{checkpoint:DPR}", ((1/checkpointSuccessRate)-1).toFixed(2));
        if(currentCheckpointObj != null){
            text = replaceAll(text, "{checkpoint:countRooms}", currentCheckpointObj.rooms.length);
        }
        text = replaceAll(text, "{checkpoint:goldenDeaths}", gbDeathsCheckpoint);
        text = replaceAll(text, "{checkpoint:goldenDeathsSession}", gbDeathsCheckpointSession);
        text = replaceAll(text, "{checkpoint:goldenChance}", (checkpointGoldenChance*100).toFixed(goldenChanceDecimals));
        text = replaceAll(text, "{checkpoint:goldenEstimateAttempts}", checkpointGoldenEstimateAttempts.toFixed(0));
        text = replaceAll(text, "{checkpoint:goldenChokeRate}", (checkpointChokeRate*100).toFixed(2));
        text = replaceAll(text, "{checkpoint:goldenChokeRateSession}", (checkpointChokeRateSession*100).toFixed(2));

        text = replaceAll(text, "{room:goldenChokeRate}", (roomChokeRate*100).toFixed(2));
        text = replaceAll(text, "{room:goldenChokeRateSession}", (roomChokeRateSession*100).toFixed(2));

        text = replaceAll(text, "{run:roomToEndGoldenChance}", (fromNowGoldenChance*100).toFixed(goldenChanceDecimals));
        text = replaceAll(text, "{run:startToRoomGoldenChance}", (toNowGoldenChance*100).toFixed(goldenChanceDecimals));

    } else {
        var loadingReplacement = "...";
        text = replaceAll(text, "{chapter:rate}", loadingReplacement);
        text = replaceAll(text, "{chapter:DPR}", loadingReplacement);
        text = replaceAll(text, "{chapter:countRooms}", loadingReplacement);
        text = replaceAll(text, "{chapter:goldenDeaths}", loadingReplacement);
        text = replaceAll(text, "{chapter:goldenDeathsSession}", loadingReplacement);
        text = replaceAll(text, "{chapter:goldenChance}", loadingReplacement);
        text = replaceAll(text, "{chapter:goldenEstimateAttempts}", loadingReplacement);

        text = replaceAll(text, "{checkpoint:rate}", loadingReplacement);
        text = replaceAll(text, "{checkpoint:DPR}", loadingReplacement);
        text = replaceAll(text, "{checkpoint:countRooms}", loadingReplacement);
        text = replaceAll(text, "{checkpoint:goldenDeaths}", loadingReplacement);
        text = replaceAll(text, "{checkpoint:goldenDeathsSession}", loadingReplacement);
        text = replaceAll(text, "{checkpoint:goldenChance}", loadingReplacement);
        text = replaceAll(text, "{checkpoint:goldenEstimateAttempts}", loadingReplacement);
        text = replaceAll(text, "{checkpoint:goldenChokeRate}", loadingReplacement);
        text = replaceAll(text, "{checkpoint:goldenChokeRateSession}", loadingReplacement);
        
        text = replaceAll(text, "{room:goldenChokeRate}", loadingReplacement);
        text = replaceAll(text, "{room:goldenChokeRateSession}", loadingReplacement);

        text = replaceAll(text, "{run:roomToEndGoldenChance}", loadingReplacement);
        text = replaceAll(text, "{run:startToRoomGoldenChance}", loadingReplacement);

        text = replaceAll(text, "{pb:checkpointName}", loadingReplacement);
        text = replaceAll(text, "{pb:checkpointAbbreviation}", loadingReplacement);
        text = replaceAll(text, "{pb:checkpointRoomNumber}", loadingReplacement);
        text = replaceAll(text, "{pb:checkpointNameSession}", loadingReplacement);
        text = replaceAll(text, "{pb:checkpointAbbreviationSession}", loadingReplacement);
        text = replaceAll(text, "{pb:checkpointRoomNumberSession}", loadingReplacement);

        
    }

    text = updateGoldenPB(text);

    text = replaceAll(text, "{test}", room.test);
    text = replaceAll(text, "NaN", getSettingValueOrDefault("text-nan-replacement"));
    document.getElementById(targetId).innerHTML = text;
}



function updateChapterLayout(chapterName){ //Called once per second
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './stats/'+chapterName+'.txt', true);
    xhr.onreadystatechange = function() {
        if (xhr.readyState == 4) {
            if(xhr.status === 200 || xhr.status == 0)
            {
                var roomStrings = xhr.responseText.split("\n");
                currentChapterRoomObjs = {};
                currentChapterElements = {};
                for(var i = 1; i < roomStrings.length; i++){ //Start at 1 because the current room is always row 0
                    if(roomStrings[i].trim() == "") continue;
                    var room = parseRoomData(roomStrings[i], false);
                    currentChapterRoomObjs[room.name] = room;
                }
                var currentRoom = parseRoomData(roomStrings[0], false);
                setCurrentRoom(currentRoom, currentRoom.name);

                getChapterPath(chapterName, currentChapterRoomObjs);
            }
        }
    };
    xhr.send();
}

function getChapterPath(chapterName, roomObjects){ //Called once per second
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './paths/'+chapterName+'.txt', true);
    xhr.onreadystatechange = function() {
        if (xhr.readyState == 4) {
            if(xhr.status === 200 || xhr.status == 0)
            {
                if(xhr.responseText == ""){ //File was not found or was empty
                    currentChapterPath = null;
                    currentCheckpointObj = null;
                    currentCheckpointRoomIndex = null;
                    
                    document.getElementById("chapter-container").innerHTML = "Path info not found";
                    hideGoldenShareDisplay();
                    hideGoldenPBDisplay();
                } else {
                    currentChapterPath = parseChapterPath(xhr.responseText);
                    displayRoomObjects(roomObjects);
                    applySettingsForGoldenShareDisplay();
                    applySettingsForGoldenPBDisplay();
                }
            }
        }
    };
    xhr.send();
}



var roomAttemptsInitialized = false;
function displayRoomAttempts(roomToDisplayStats){
    if(!roomAttemptsInitialized){
        roomAttemptsInitialized = true;
        applySettingsForRoomAttemptDisplay();
    }

    var fontSize = getSettingValueOrDefault("room-attempts-font-size");
    var circleSize = getSettingValueOrDefault("room-attempts-circle-size");

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
            circleInnerElement.classList.add("green");
        } else {
            circleInnerElement.classList.add("red");
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
            checkpointElement.style.flexGrow = (5 * borderMult) + "";
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
                borderElement.style.flexGrow = (3 * borderMult) + "";
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
            checkpointElement.style.flexGrow = (5 * borderMult) + "";
            container.appendChild(checkpointElement);
        }
        
        var roomsObj = currentChapterPath[checkpointIndex].rooms;

        for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){

            var borderElementRef = null;
            if(roomIndex != 0){ //Skip border element for first room
                var borderElement = document.createElement("div");
                borderElement.classList.add("border-element");
                borderElement.classList.add("pb");
                borderElement.style.flexGrow = (3 * borderMult) + "";
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
            checkpointElement.style.flexGrow = (5 * borderMult) + "";
            container.appendChild(checkpointElement);
        }
        
        var checkpointObj = currentChapterPath[checkpointIndex];

        var checkpointElement = document.createElement("div");
        checkpointElement.classList.add("golden-share-checkpoint");
        checkpointElement.style.flexGrow = checkpointObj.rooms.length * 50 + (checkpointObj.rooms.length-1) * (3 * borderMult);
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

            if(roomName == pbRoomNameSession){
                text = replaceAll(text, "{pb:checkpointNameSession}", roomCP.checkpoint.name);
                text = replaceAll(text, "{pb:checkpointAbbreviationSession}", roomCP.checkpoint.abbreviation);
                text = replaceAll(text, "{pb:checkpointRoomNumberSession}",  roomCP.roomIndex+1);
            }

            var roomElements = currentChapterGoldenPBElements[roomName];

            if(roomName == pbRoomName){ //At PB
                foundPB = true;
                roomElements.left.classList.add("gold");
                roomElements.middle.classList.remove("hidden");
                roomElements.right.classList.remove("gold");
                if(roomElements.leftBorder != null){
                    roomElements.leftBorder.classList.add("gold");
                }

                text = replaceAll(text, "{pb:checkpointName}", roomCP.checkpoint.name);
                text = replaceAll(text, "{pb:checkpointAbbreviation}", roomCP.checkpoint.abbreviation);
                text = replaceAll(text, "{pb:checkpointRoomNumber}",  roomCP.roomIndex+1);

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
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './stats/'+chapterName+'.txt', true);
    xhr.onreadystatechange = function() {
        if (xhr.readyState == 4) {
            if(xhr.status === 200 || xhr.status == 0)
            {
                
                var roomStrings = xhr.responseText.split("\n");
                currentChapterRoomObjs = {};

                for(var i = 1; i < roomStrings.length; i++){ //Start at 1 because the current room is always row 0
                    if(roomStrings[i].trim() == "") continue;
                    var room = parseRoomData(roomStrings[i], false);
                    currentChapterRoomObjs[room.name] = room;
                }

                updateRoomObjects(currentChapterRoomObjs);
            }
        }
    };
    xhr.send();
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
            roomElement.classList.remove("green");
            roomElement.classList.remove("yellow");
            roomElement.classList.remove("red");
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

    if(isNaN(compareAgainstRate)){
        return "gray";
    } else if(compareAgainstRate >= lightGreenCutoff){
        return "light-green";
    } else if(compareAgainstRate >= greenCutoff){
        return "green";
    } else if(compareAgainstRate >= yellowCutoff){
        return "yellow";
    } else {
        return "red";
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

    if(selectedOverride !== "" && selectedOverride !== null){
        var overrides = settings[overridesKey];
        if(overrides[selectedOverride] !== undefined){
            var override = overrides[selectedOverride];
            if(override[settingName] !== undefined){
                return override[settingName];
            }
        }
    }

    var baseSettings = settings[baseKey];

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