let settings = {};
let defaultSettings = {
    "text-format": "Room '{name}': {rateMax}% ({successesMax}/{attemptsMax})", //{name} = room name, {rate5} = rate of 5s, {rate10} = rate of 10s, {rate20} = rate of 20s, {rateMax} = max rate
    "text-nan-replacement": "-",
    "color": "white",
    "font-size": "80px",
    "outline-size": "10px",
    "outline-color": "black",
    "refresh-time-ms": 1000,
}

document.addEventListener('DOMContentLoaded', function() {
    //Call updateOverlay once per second
    fetchSettings();
});


function fetchSettings(){ //Called once per second
    // bodyLog("Fetching settings...");
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './PlainOverlaySettings.json', true);
    xhr.onreadystatechange = function() {
        //Get content of file
        if (xhr.readyState == 4) {
            if(xhr.status === 404){
                settings = defaultSettings;
            } else if((xhr.status === 200 || xhr.status == 0) && xhr.responseText != "") {
                bodyLog("State == 4, status == 200 || 0 -> "+xhr.responseText);
                settings = JSON.parse(xhr.responseText);
                bodyLog("Settings: "+JSON.stringify(settings));
            } else {
                settings = defaultSettings;
            }
            
            applySettings();
            setInterval(updateOverlay, getSettingValueOrDefault("refresh-time-ms"));
        }
    };
    xhr.send();
}

function applySettings(){
    // bodyLog("Applying settings...");
    document.body.style.color = getSettingValueOrDefault("color");
    document.body.style.fontSize = getSettingValueOrDefault("font-size");

    let size = getSettingValueOrDefault("outline-size");
    let color = getSettingValueOrDefault("outline-color");
    let textShadow = "";
    for(var i = 0; i < 6; i++){
        textShadow += color+" 0px 0px "+size+", ";
    }
    textShadow = textShadow.substring(0, textShadow.length-2);
    document.body.style.textShadow = textShadow;
}

function getSettingValueOrDefault(settingName){
    if(settings.hasOwnProperty(settingName)){
        return settings[settingName];
    } else {
        return defaultSettings[settingName];
    }
}

function updateOverlay(){ //Called once per second
    // bodyLog("Updating overlay...");
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './stats/current_room.txt', true);
    xhr.onreadystatechange = function() {
        //Get content of file
        if (xhr.readyState == 4) {
            if(xhr.status === 200 || xhr.status == 0)
            {
                bodyLog("State == 4, status == 200 || 0");
                var room = parseRoomData(xhr.responseText);
                var text = getSettingValueOrDefault("text-format");
                text = text.replace("{name}", room.name);
                text = text.replace("{successes5}", room.successes5);
                text = text.replace("{successes10}", room.successes10);
                text = text.replace("{successes20}", room.successes20);
                text = text.replace("{successesMax}", room.successesMax);
                text = text.replace("{attempts5}", room.totalAttempts5);
                text = text.replace("{attempts10}", room.totalAttempts10);
                text = text.replace("{attempts20}", room.totalAttempts20);
                text = text.replace("{attemptsMax}", room.totalAttemptsMax);
                text = text.replace("{failures5}", room.failures5);
                text = text.replace("{failures10}", room.failures10);
                text = text.replace("{failures20}", room.failures20);
                text = text.replace("{failuresMax}", room.failuresMax);

                text = text.replace("{rate5}", (room.rate5*100).toFixed(2));
                text = text.replace("{rate10}", (room.rate10*100).toFixed(2));
                text = text.replace("{rate20}", (room.rate20*100).toFixed(2));
                text = text.replace("{rateMax}", (room.rateMax*100).toFixed(2));
                text = text.replace("{test}", room.test);
                text = text.replace("NaN", getSettingValueOrDefault("text-nan-replacement"));
                document.body.innerText = text;
            }
        }
    };
    xhr.send();
}

function parseRoomData(roomString){
    //Example roomString: "name;rate5;rate10;rate20;rateMax;CSV,of,booleans"
    bodyLog("Parsing room data...");
    
    var roomData = roomString.split(";");
    var roomName = roomData[0];
    var roomRate5 = roomData[1];
    var roomRate10 = roomData[2];
    var roomRate20 = roomData[3];
    var roomRateMax = roomData[4];
    var roomAttempts = roomData[5].split(",");

    bodyLog("Base fields done");

    var roomObj = {};
    roomObj.name = roomName;
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

    bodyLog("Parsed rates");

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

    bodyLog("Parsed room data: "+roomObj.name+" "+roomObj.rate5+"%");
    return roomObj;
}

function bodyLog(message){
    document.body.innerText += "\n"+message;
}