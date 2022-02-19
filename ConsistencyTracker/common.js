


function getSettingValueOrDefault(settingName){
    if(settings.hasOwnProperty(settingName)){
        return settings[settingName];
    } else {
        return defaultSettings[settingName];
    }
}

function parseRoomData(roomString, hasState, targetLogId){
    //Example roomString: "name;rate5;rate10;rate20;rateMax;CSV,of,booleans\nchapterName;isTrackingPaused;isRecordingEnabled"

    bodyLog("Parsing room data...", targetLogId);

    var roomLine = null;
    var roomObj = {};

    if(hasState){
        var splitData = roomString.split("\n");
        roomLine = splitData[0];
        var state = splitData[1].trim();
        roomObj.state = {};

        var stateSplit = state.split(";");
        roomObj.state.chapterName = stateSplit[0];
        roomObj.state.isTrackingPaused = stateSplit[1] == "True";
        roomObj.state.isRecordingEnabled = stateSplit[2] == "True";

    } else {
        roomLine = roomString;
    }
    
    var roomData = roomLine.split(";");

    var roomName = roomData[0];
    var gbDeaths = roomData[1];
    var gbDeathsSession = roomData[2];
    var roomRate5 = roomData[3];
    var roomRate10 = roomData[4];
    var roomRate20 = roomData[5];
    var roomRateMax = roomData[6];
    var roomAttempts = roomData[7].split(",");

    bodyLog("Base fields done", targetLogId);

    roomObj.name = roomName;
    roomObj.goldenBerryDeaths = parseInt(gbDeaths);
    roomObj.goldenBerryDeathsSession = parseInt(gbDeathsSession);
    roomObj.rate5 = parseFloat(roomRate5);
    roomObj.rate10 = parseFloat(roomRate10);
    roomObj.rate20 = parseFloat(roomRate20);
    roomObj.rateMax = parseFloat(roomRateMax);
    roomObj.successes5 = 0;
    roomObj.successes10 = 0;
    roomObj.successes20 = 0;
    roomObj.successesMax = 0;
    roomObj.totalAttempts = 0;
    roomObj.totalAttempts5 = 0;
    roomObj.totalAttempts10 = 0;
    roomObj.totalAttempts20 = 0;
    roomObj.totalAttemptsMax = 0;

    bodyLog("Parsed rates", targetLogId);

    roomObj.attempts = [];
    for(var i = roomAttempts.length-1; i >= 0; i--){
        if(roomAttempts[i].trim() == "") continue;

        roomObj.totalAttempts++;
        if(roomObj.totalAttempts <= 5){
            roomObj.totalAttempts5++;
        } 
        if(roomObj.totalAttempts <= 10){
            roomObj.totalAttempts10++;
        } 
        if(roomObj.totalAttempts <= 20){
            roomObj.totalAttempts20++;
        }
        roomObj.totalAttemptsMax++;


        if(roomAttempts[i].trim() == "True"){
            if(roomObj.totalAttempts <= 5){
                roomObj.successes5++;
            } 
            if(roomObj.totalAttempts <= 10){
                roomObj.successes10++;
            } 
            if(roomObj.totalAttempts <= 20){
                roomObj.successes20++;
            }
            roomObj.successesMax++;
            roomObj.attempts.push(true);
        } else {
            roomObj.attempts.push(false);
        }   
    }
    roomObj.failures5 = roomObj.totalAttempts5 - roomObj.successes5;
    roomObj.failures10 = roomObj.totalAttempts10 - roomObj.successes10;
    roomObj.failures20 = roomObj.totalAttempts20 - roomObj.successes20;
    roomObj.failuresMax = roomObj.totalAttemptsMax - roomObj.successesMax;

    bodyLog("Parsed room data: "+roomObj.name+" "+roomObj.rate5*100+"%", targetLogId);
    return roomObj;
}

function bodyLog(message, id, important){
    if(important == undefined || important == false){
        return;
    }

    if(id == undefined || id == null){
        
    } else  if(id == "body"){
        document.body.innerText += "\n"+message;
    } else {
        document.getElementById(id).innerText += "\n"+message;
    }
}

function areRoomsEqual(roomNew, roomOld) {
    if(roomNew.name != roomOld.name){
        return false;
    }
    if(roomNew.attempts.length != roomOld.attempts.length){
        return false;
    }
    for(var i = 0; i < roomNew.attempts.length; i++){
        if(roomNew.attempts[i] != roomOld.attempts[i]){
            return false;
        }
    }

    return true;
}


function parseChapterPath(fileContent){
    /*
    Example room path file:
    Start;ST;7;a-01,a-02,a-03,a-04,a-05,a-06,a-07
    Alleyway;AW;8;b-01,b-02,b-03,b-04,b-05,b-06,c-01,c-02
    with the fields named:
    name;abbreviation;roomCount;rooms
    */
    bodyLog("parsing chapter path", "stats-display");
    fileContent = fileContent.trim();

    var chapterPath = [];

    var lines = fileContent.split("\n");
    for(var i = 0; i < lines.length; i++){
        var line = lines[i];
        var splitLine = line.split(";");
        var chapterName = splitLine[0];
        var chapterAbbreviation = splitLine[1];
        var roomCount = parseInt(splitLine[2]);
        var rooms = splitLine[3].split(",");

        var chapterObj = {};
        chapterObj.name = chapterName;
        chapterObj.abbreviation = chapterAbbreviation;
        chapterObj.rooms = [];
        chapterObj.roomCount = roomCount;

        for(var j = 0; j < roomCount; j++){
            var roomName = rooms[j];
            chapterObj.rooms.push(roomName);
        }

        chapterPath.push(chapterObj);
    }

    bodyLog(JSON.stringify(chapterPath), "stats-display");

    return chapterPath;
}