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


//Helper for loading elements by ids
function loadElements(elementIdsObject){
    for(var key in elementIdsObject){
        if(!elementIdsObject.hasOwnProperty(key)){
            continue;
        }
        elementIdsObject[key] = document.getElementById(elementIdsObject[key]);
    }
}


function formatBigNumber(number, precision=1){
    //K for 1000, M for 1000000, B for 1000000000, T for 1000000000000
    //Round to precision decimal places
    if(number >= 1000000000000){
        return (number/1000000000000).toFixed(precision) + "T";
    }
    if(number >= 1000000000){
        return (number/1000000000).toFixed(precision) + "B";
    }
    if(number >= 1000000){
        return (number/1000000).toFixed(precision) + "M";
    }
    if(number >= 1000){
        return (number/1000).toFixed(precision) + "K";
    }
    return number;
}