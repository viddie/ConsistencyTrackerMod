let settings = {};
let defaultSettings = {
    "rate": "max",
    "text-format": "Room '{room}': {rate}% ({successes}/{attempts})",
    "text-nan-replacement": "-",
    "color": "white",
    "font-size": "80px",
    "outline-size": "10px",
    "outline-color": "black",
    "refresh-time-ms": 1000
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
                bodyLog("State == 4, status == 200 || 0 -> "+xhr.responseText, "body");
                settings = JSON.parse(xhr.responseText);
                bodyLog("Settings: "+JSON.stringify(settings), "body");
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

function updateOverlay(){ //Called once per second
    // bodyLog("Updating overlay...");
    var xhr = new XMLHttpRequest();
    xhr.open('GET', './stats/current_room.txt', true);
    xhr.onreadystatechange = function() {
        //Get content of file
        if (xhr.readyState == 4) {
            if(xhr.status === 200 || xhr.status == 0)
            {
                bodyLog("State == 4, status == 200 || 0", "body");
                var room = parseRoomData(xhr.responseText, true, "body");
                var text = getSettingValueOrDefault("text-format");
                text = text.replace("{room}", room.name);
                text = text.replace("{checkpoint}", currentRoom.chapterName);
                
                var selectedRate = getSettingValueOrDefault("rate");
                if(selectedRate == "5"){
                    text = text.replace("{rate}", (currentRoom.rate5*100).toFixed(2));
                    text = text.replace("{successes}", currentRoom.successes5);
                    text = text.replace("{attempts}", currentRoom.totalAttempts5);
                    text = text.replace("{failures}", currentRoom.failures5);
                } else if(selectedRate == "10"){
                    text = text.replace("{rate}", (currentRoom.rate10*100).toFixed(2));
                    text = text.replace("{successes}", currentRoom.successes10);
                    text = text.replace("{attempts}", currentRoom.totalAttempts10);
                    text = text.replace("{failures}", currentRoom.failures10);
                } else if(selectedRate == "20"){    
                    text = text.replace("{rate}", (currentRoom.rate20*100).toFixed(2));
                    text = text.replace("{successes}", currentRoom.successes20);
                    text = text.replace("{attempts}", currentRoom.totalAttempts20);
                    text = text.replace("{failures}", currentRoom.failures20);
                } else {
                    text = text.replace("{rate}", (currentRoom.rateMax*100).toFixed(2));
                    text = text.replace("{successes}", currentRoom.successesMax);
                    text = text.replace("{attempts}", currentRoom.totalAttemptsMax);
                    text = text.replace("{failures}", currentRoom.failuresMax);
                }

                text = text.replace("{test}", room.test);
                text = text.replace("NaN", getSettingValueOrDefault("text-nan-replacement"));
                document.body.innerText = text;
            }
        }
    };
    xhr.send();
}
