function SplitVersionNumber(version){
    var split = version.split(".");

    return {
        "major": parseInt(split[0]),
        "minor": parseInt(split[1]),
        "patch": parseInt(split[2])
    };
}

function areRoomsEqual(roomNew, roomOld) {
    if(roomNew.debugRoomName != roomOld.debugRoomName){
        return false;
    }
    if(roomNew.previousAttempts.length != roomOld.previousAttempts.length){
        return false;
    }
    for(var i = 0; i < roomNew.previousAttempts.length; i++){
        if(roomNew.previousAttempts[i] != roomOld.previousAttempts[i]){
            return false;
        }
    }

    return true;
}


//Helper for replaceAll
function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); // $& means the whole matched string
}
function replaceAll(str, find, replace) {
    return str.replace(new RegExp(escapeRegExp(find), 'g'), replace);
}