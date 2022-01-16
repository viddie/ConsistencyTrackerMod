let settings = {};
let defaultSettings = {
    "rate": "20",
    "text-format": "{cpName}-{cpRoomNumber}: {rate}% ({successes}/{attempts})",
    "text-nan-replacement": "-",
    "color": "white",
    "font-size": "80px",
    "outline-size": "2px",
    "outline-color": "black",
    "refresh-time-ms": 1000,
    "green-cutoff": 0.8,
    "yellow-cutoff": 0.5,
    "hide-chapter-bar": true,
}

let countRefreshs = 99999;
let intervalHandle = null;

let currentRoomName = null;
let previousRoomName = null;
let previousRoomRaw = null;
let previousChapterName = null;
let currentChapterRoomObjs = {};
let currentChapterElements = {};
let currentChapterPath = null;
let currentChapterOverallRate = null;

let currentCheckpointObj = null;
let currentCheckpointRoomIndex = null;

document.addEventListener('DOMContentLoaded', function() {
    //Call updateOverlay once per second
    fetchSettings();
});


function applySettings(){
    // bodyLog("Applying settings...");
    let hideBar = getSettingValueOrDefault("hide-chapter-bar");
    if(hideBar){
        document.getElementById("chapter-container").style.display = "none";
    }

    var element = document.getElementById("stats-display");
    element.style.color = getSettingValueOrDefault("color");
    element.style.fontSize = getSettingValueOrDefault("font-size");

    var chapterElement = document.getElementById("chapter-container");
    chapterElement.style.color = getSettingValueOrDefault("color");
    chapterElement.style.fontSize = getSettingValueOrDefault("font-size");

    let size = getSettingValueOrDefault("outline-size");
    let color = getSettingValueOrDefault("outline-color");
    let textShadow = "";
    for(var i = 0; i < 6; i++){
        textShadow += color+" 0px 0px "+size+", ";
    }
    textShadow = textShadow.substring(0, textShadow.length-2);
    element.style.textShadow = textShadow;
    chapterElement.style.textShadow = textShadow;

}


function fetchSettings(){ //Called once per second
    // bodyLog("Fetching settings...");
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './ChapterOverlaySettings.json', true);
    xhr.onreadystatechange = function() {
        //Get content of file
        if (xhr.readyState == 4) {
            if(xhr.status === 404){
                settings = defaultSettings;
            } else if((xhr.status === 200 || xhr.status == 0) && xhr.responseText != "") {
                bodyLog("State == 4, status == 200 || 0 -> "+xhr.responseText, "stats-display");
                settings = JSON.parse(xhr.responseText);
                bodyLog("Settings: "+JSON.stringify(settings), "stats-display");
            } else {
                settings = defaultSettings;
            }
            
            applySettings();
            intervalHandle = setInterval(fetchCurrentChapter, getSettingValueOrDefault("refresh-time-ms"));
        }
    };
    xhr.send();
}

function fetchCurrentChapter(){ //Called once per second
    // bodyLog("Fetching settings...");
    countRefreshs--;
    if(countRefreshs <= 0){
        clearInterval(intervalHandle);
        intervalHandle = null;
    }

    var xhr = new XMLHttpRequest();
    xhr.open('GET', './stats/current_room.txt', true);
    xhr.onreadystatechange = function() {
        //Get content of file
        if (xhr.readyState == 4) {
            if((xhr.status === 200 || xhr.status == 0) && xhr.responseText != "") {
                bodyLog("./stats/current_room.txt -> "+xhr.responseText);

                previousRoomName = currentRoomName;

                var newCurrentRoom = parseRoomData(xhr.responseText, true, "stats-display");

                if(currentRoomName != null)
                    previousRoomRaw = getCurrentRoom();
                setCurrentRoom(newCurrentRoom, newCurrentRoom.name);

                updateStatsText(getCurrentRoom());

                if((previousRoomName != null && previousChapterName != getCurrentRoom().chapterName) || (previousRoomName == null && currentRoomName != null) || currentChapterPath == null){
                    //Update the chapter layout
                    previousChapterName = getCurrentRoom().chapterName;
                    updateChapterLayout(getCurrentRoom().chapterName);
                    
                } else if(previousRoomName != null && !areRoomsEqual(previousRoomRaw, getCurrentRoom())){
                    //Update only one room
                    updateRoomInLayout(getPreviousRoom(), getCurrentRoom());
                }


            }
        }
    };
    xhr.send();
}

function updateStatsText(room){
    var text = getSettingValueOrDefault("text-format");
    text = text.replace("{room}", room.name);
    text = text.replace("{chapterSID}", room.chapterName);

    var selectedRate = getSettingValueOrDefault("rate");
    if(selectedRate == "5"){
        text = text.replace("{rate}", (room.rate5*100).toFixed(2));
        text = text.replace("{successes}", room.successes5);
        text = text.replace("{attempts}", room.totalAttempts5);
        text = text.replace("{failures}", room.failures5);
    } else if(selectedRate == "10"){
        text = text.replace("{rate}", (room.rate10*100).toFixed(2));
        text = text.replace("{successes}", room.successes10);
        text = text.replace("{attempts}", room.totalAttempts10);
        text = text.replace("{failures}", room.failures10);
    } else if(selectedRate == "20"){    
        text = text.replace("{rate}", (room.rate20*100).toFixed(2));
        text = text.replace("{successes}", room.successes20);
        text = text.replace("{attempts}", room.totalAttempts20);
        text = text.replace("{failures}", room.failures20);
    } else {
        text = text.replace("{rate}", (room.rateMax*100).toFixed(2));
        text = text.replace("{successes}", room.successesMax);
        text = text.replace("{attempts}", room.totalAttemptsMax);
        text = text.replace("{failures}", room.failuresMax);
    }
    
    if(currentCheckpointObj == null){
        text = text.replace("{cpName}", "-");
        text = text.replace("{cpAbbreviation}", "-");
    } else {
        text = text.replace("{cpName}", currentCheckpointObj.name);
        text = text.replace("{cpAbbreviation}", currentCheckpointObj.abbreviation);
    }
    text = text.replace("{cpRoomNumber}", currentCheckpointRoomIndex+1);

    var textAddition = "";
    if(currentChapterRoomObjs != null){
        var countAttempts = 0;
        var countSuccesses = 0;
        var countRooms = 0;

        var rateNumber = getSelectedRateNumber();

        var roomId = 2;
        //Iterate the object currentChapterRoomObjs
        for(var key in currentChapterRoomObjs){
            if(currentChapterRoomObjs.hasOwnProperty(key)){
                var roomObj = currentChapterRoomObjs[key];

                if(roomObj.attempts.length >= 1){
                    countRooms++;
                }

                var maxIndex = Math.min(rateNumber, roomObj.attempts.length-1);
                var minIndex = 0;
                // textAddition += roomObj.name+"(min="+minIndex+", max="+maxIndex+"): "+roomObj.attempts.slice(minIndex, maxIndex+1).join(", ")+"\n";
                var max = Math.min(rateNumber, roomObj.attempts.length);
                for(var i = 0; i < max; i++){
                    countAttempts++;
                    if(roomObj.attempts[i]){
                        countSuccesses++;
                    }
                }
            }
        }

        // textAddition += " | A: "+countAttempts+", S: "+countSuccesses+", R: "+countRooms;

        var overallSuccessRate = countAttempts == 0 ? 0 : countSuccesses/countAttempts;
        var overallDeathsPerRoom = countAttempts == 0 ? 0 : ((countAttempts - countSuccesses) / countRooms);

        text = text.replace("{overallRate}", (overallSuccessRate*100).toFixed(2));
        text = text.replace("{dpr}", (overallDeathsPerRoom).toFixed(2));
    } else {
        text = text.replace("{overallRate}", "-");
    }

    text = text.replace("{test}", room.test);
    text = text.replace("NaN", getSettingValueOrDefault("text-nan-replacement"));
    document.getElementById("stats-display").innerText = text+textAddition;
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
                } else {
                    currentChapterPath = parseChapterPath(xhr.responseText);
                    displayRoomObjects(roomObjects);
                }
            }
        }
    };
    xhr.send();
}



function updateRoomInLayout(previousRoom, currentRoom){
    var currentRoomElem = currentChapterElements[currentRoom.name];
    // bodyLog("currentRoom.element: "+currentRoomElem, "stats-display", true);
    if(previousRoom != null){
        var previousRoomElem = currentChapterElements[previousRoom.name];
        // bodyLog("previousRoom.element: "+previousRoomElem, "stats-display", true);
        if(previousRoom.name == currentRoom.name){ //Died in room
            // bodyLog("Player died...", "stats-display", true);
        } else {
            // bodyLog("Player moved to "+currentRoom.name, "stats-display", true);
            previousRoomElem.classList.remove("selected");
        }
        updateChapterStats(getCurrentRoom().chapterName);
    }
    // bodyLog("Set highlight to element of: "+JSON.stringify(currentRoom)+"\nElement: "+currentRoomElem, "stats-display", true);
    currentRoomElem.classList.add("selected");
}

//Fetches the current chapter stats and calls an update with the room objects
function updateChapterStats(chapterName){
    bodyLog('Updating chapter stats');
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './stats/'+chapterName+'.txt', true);
    xhr.onreadystatechange = function() {
        if (xhr.readyState == 4) {
            if(xhr.status === 200 || xhr.status == 0)
            {
                bodyLog('./stats/'+chapterName+'.txt -> '+xhr.responseText);
                
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


//Creates HTML elements for all room objects and saves them in currentChapterElements
function displayRoomObjects(roomObjs){
    var container = document.getElementById("chapter-container");
    container.innerHTML = "";

    //Add the start element
    var startElement = document.createElement("div");
    startElement.className = "start-end-element";
    container.appendChild(startElement);



    for(var checkpointIndex = 0; checkpointIndex < currentChapterPath.length; checkpointIndex++){
        if(checkpointIndex != 0){ //Skip checkpoint element for first and last
            var checkpointElement = document.createElement("div");
            checkpointElement.classList.add("checkpoint-element");
            container.appendChild(checkpointElement);
        }
        
        var roomsObj = currentChapterPath[checkpointIndex].rooms;

        for(var roomIndex = 0; roomIndex < roomsObj.length; roomIndex++){
            var roomName = currentChapterPath[checkpointIndex].rooms[roomIndex];
            var room = getRoomByNameOrDefault(roomObjs, roomName);
            var roomElement = document.createElement("div");
            roomElement.classList.add("room-element");
            var classColor = getColorClass(room);
            roomElement.classList.add(classColor);
            if(room.name == getCurrentRoom().name){
                roomElement.classList.add("selected");
                currentCheckpointObj = currentChapterPath[checkpointIndex];
                currentCheckpointRoomIndex = roomIndex;
            }
            container.appendChild(roomElement);
            currentChapterElements[roomName] = roomElement;

            if(roomIndex != roomsObj.length - 1){ //Skip border element for last room
                var borderElement = document.createElement("div");
                borderElement.classList.add("border-element");
                container.appendChild(borderElement);
            }
        }
    }

    //Add the end element
    var endElement = document.createElement("div");
    endElement.className = "start-end-element";
    container.appendChild(endElement);
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
        rate5: NaN,
        rate10: NaN,
        rate20: NaN,
        rateMax: NaN,
    };
}

function getColorClass(room){
    var compareAgainstRate = getSelectedRateOfRoom(room);
    
    var greenCutoff = getSettingValueOrDefault("green-cutoff");
    var yellowCutoff = getSettingValueOrDefault("yellow-cutoff");

    if(isNaN(compareAgainstRate)){
        return "gray";
    } else if(compareAgainstRate >= greenCutoff){
        return "green";
    } else if(compareAgainstRate >= yellowCutoff){
        return "yellow";
    } else {
        return "red";
    }
}

function getSelectedRateOfRoom(room){
    var selectedRate = getSettingValueOrDefault("rate");
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
    var selectedRate = getSettingValueOrDefault("rate");
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