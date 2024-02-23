//#region Constants
const RecordingTypes = {
  Recent: 0,
  Saved: 1,
};

const apiBaseUrl = "http://localhost:32270/cct";
//#endregion

//#region Properties
let isOffline = false;
let isRecording = false;
let recentPhysicsLogFilesList = null;
let savedPhysicsRecordingsList = null;
let roomLayouts = null; //Array
let physicsLogFrames = null; //Array
//#endregion

//#region Log Frame Parsing
function parsePhysicsLogFrames(fileContent) {
  let lines = fileContent.split("\n");

  //First line can be the header
  let header = lines[0];
  let beginIndex = header.indexOf("Frame,") !== -1 ? 1 : 0;

  //All other lines have the format:
  //FrameNumber, PositionX, PositionY, SpeedX, SpeedY, VelocityX, VelocityY, LiftBoostX, LiftBoostY, Flags, Inputs
  let frames = [];
  for (let i = beginIndex; i < lines.length; i++) {
    const line = lines[i].trim();
    if (line.length === 0) continue;
    let frame = parsePhysicsLogFrame(line);
    frames.push(frame);
  }

  return frames;
}

function filterPhysicsLogFrames() {
  //Filter out the consecutive frames with the same position
  let filteredFrames = [];
  let lastFrame = null;
  for (let i = 0; i < physicsLogFrames.length; i++) {
    const frame = physicsLogFrames[i];
    if (lastFrame === null) {
      filteredFrames.push(frame);
    } else {
      if (lastFrame.positionX !== frame.positionX || lastFrame.positionY !== frame.positionY || isFrameFirstInRoom(frame)) {
        filteredFrames.push(frame);
      } else {
        // add the frame to the last frame's idleFrames
        filteredFrames[filteredFrames.length - 1].idleFrames.push(frame);
      }
    }
    lastFrame = frame;
  }
  physicsLogFrames = filteredFrames;
}

function parsePhysicsLogFrame(line) {
  let values = line.split(",");
  let valuesLength = values.length;
  let frame = {
    frameNumber: parseInt(values[0]),
    frameNumberRTA: parseInt(values[1]),
    positionX: parseFloat(values[2]),
    positionY: parseFloat(values[3]),
    speedX: parseFloat(values[4]),
    speedY: parseFloat(values[5]),
    velocityX: parseFloat(values[6]),
    velocityY: parseFloat(values[7]),
    liftBoostX: parseFloat(values[8]),
    liftBoostY: parseFloat(values[9]),
    speedRetention: parseFloat(values[10]),
    stamina: parseFloat(values[11]),
    flags: values[12],
    inputs: values[13],
    analogAimX: valuesLength > 15 ? parseFloat(values[14]) : -1,
    analogAimY: valuesLength > 15 ? parseFloat(values[15]) : -1,
    entities: valuesLength > 16 ? joinArray(values.slice(16), ",") : "",
    idleFrames: [],
  };
  
  if(frame.entities !== ""){
    frame.entities = JSON.parse(frame.entities);
  }
  
  return frame;
}

function joinArray(arr, separator) {
    let str = "";
    for (let i = 0; i < arr.length; i++) {
        if (i > 0) {
            str += separator;
        }
        str += arr[i];
    }
    return str;
}


let relevantRoomNames = [];
function findRelevantRooms() {
  relevantRoomNames = [];

  //Go through all frames
  for (let i = 0; i < settings.frameStepSize; i++) {
    let frameIndex = settings.frameMin != -1 ? settings.frameMin + i : i;
    frameIndex = Math.min(frameIndex, physicsLogFrames.length - 1);
    let frame = physicsLogFrames[frameIndex];

    //Go through all roomLayouts
    for (let j = 0; j < roomLayouts.length; j++) {
      //Check if frame is in the room, if yes, add room to relevantRoomNames and break
      if (relevantRoomNames.includes(roomLayouts[j].debugRoomName)) {
        continue;
      }

      let roomLayout = roomLayouts[j];
      let levelBounds = roomLayout.levelBounds;

      if (
        frame.positionX >= levelBounds.x &&
        frame.positionX <= levelBounds.x + levelBounds.w &&
        frame.positionY >= levelBounds.y &&
        frame.positionY <= levelBounds.y + levelBounds.h
      ) {
        relevantRoomNames.push(roomLayout.debugRoomName);
        break;
      }
    }
  }
}

//Find the room layout that the frame is in
function getRoomFromFrame(frame = null){
  frame = frame ?? physicsLogFrames[settings.frameMin];
  for (let i = 0; i < roomLayouts.length; i++) {
    let roomLayout = roomLayouts[i];
    let levelBounds = roomLayout.levelBounds;

    if (
        frame.positionX >= levelBounds.x &&
        frame.positionX <= levelBounds.x + levelBounds.w &&
        frame.positionY >= levelBounds.y &&
        frame.positionY <= levelBounds.y + levelBounds.h
    ) {
      return roomLayout;
    }
  }
  return null;
}
//#endregion

//#region Display Utils
function getRecordingDisplayNameByID(recordingType, recordingID) {
  let toSearch =
    recordingType === RecordingTypes.Recent ? recentPhysicsLogFilesList : savedPhysicsRecordingsList;
  for (let i = 0; i < toSearch.length; i++) {
    let recording = toSearch[i];
    if (recording.id === recordingID) {
      return getRecordingDisplayName(recordingType, recording);
    }
  }
}
function getRecordingDisplayName(recordingType, recordingObj) {
  if (recordingType === RecordingTypes.Recent) {
    let chapterName = recordingObj.chapterName;
    let sideName = recordingObj.sideName;
    let recordingStarted = new Date(recordingObj.recordingStarted);
    let frameCount = recordingObj.frameCount;
    let id = recordingObj.id;

    //format the date in: "day/month hour:minute", zero padded using the function zeroPad(number, zeros)
    let date =
      zeroPad(recordingStarted.getDate(), 2) +
      "/" +
      zeroPad(recordingStarted.getMonth() + 1, 2) +
      " " +
      zeroPad(recordingStarted.getHours(), 2) +
      ":" +
      zeroPad(recordingStarted.getMinutes(), 2);
    let mapName = sideName === "A-Side" ? chapterName : chapterName + " [" + sideName[0] + "]";

    return "(" + (id + 1) + ") " + mapName + " - " + formatBigNumber(frameCount) + "f";
  } else {
    let chapterName = recordingObj.chapterName;
    let sideName = recordingObj.sideName;
    let recordingStarted = new Date(recordingObj.recordingStarted);
    let frameCount = recordingObj.frameCount;
    let id = recordingObj.id;
    let name = recordingObj.name;

    let date =
      zeroPad(recordingStarted.getDate(), 2) +
      "/" +
      zeroPad(recordingStarted.getMonth() + 1, 2) +
      " " +
      zeroPad(recordingStarted.getHours(), 2) +
      ":" +
      zeroPad(recordingStarted.getMinutes(), 2);
    let mapName = sideName === "A-Side" ? chapterName : chapterName + " [" + sideName[0] + "]";

    return name + ": " + mapName + " - " + formatBigNumber(frameCount) + "f";
  }
}
//#endregion

//#region New Data Format Stuff
function isFrameFirstInRoom(frame){
  return frame.flags.indexOf("FirstFrameInRoom") !== -1;
}
function getFirstFrameInRoom(frameIndex){
  frameIndex = frameIndex ?? settings.frameMin;
  
  //Walk backwards from settings.frameMin through the physicsLogFrames and find the first frame with the "FirstFrameInRoom" flag
  for (let i = frameIndex; i >= 0; i--) {
    let frame = physicsLogFrames[i];
    if (isFrameFirstInRoom(frame)) {
      return frame;
    }
  }
  return null;
}

function getFrameIndexFromFrame(frame){
  for (let i = 0; i < physicsLogFrames.length; i++) {
    if (physicsLogFrames[i].frameNumber === frame.frameNumber) {
      return i;
    }
  }
  return -1;
}

function getEntitiesForFrame(frameIndex){
  let firstFrameInRoom = getFirstFrameInRoom(frameIndex);
  let firstFrameIndex = getFrameIndexFromFrame(firstFrameInRoom);
  //The first frame in each room holds the entities initial positions
  //All subsequent frames hold the CHANGES to the entities, such as position difference, removal and addition
  if(firstFrameInRoom === null){
    return {};
  }
  
  let entities = JSON.parse(JSON.stringify(firstFrameInRoom.entities));
  if(firstFrameIndex === frameIndex){
    return entities;
  }
  
  for (let i = firstFrameIndex+1; i <= frameIndex; i++) {
    let frame = physicsLogFrames[i];
    let frameEntities = frame.entities;
    entities = applyEntityChanges(entities, frameEntities);
    if(frame.idleFrames.length > 0){
      for (let j = 0; j < frame.idleFrames.length; j++) {
        let idleFrame = frame.idleFrames[j];
        let idleFrameEntities = idleFrame.entities;
        entities = applyEntityChanges(entities, idleFrameEntities);
      }
    }
  }
  
  return entities;
}

function applyEntityChanges(entities, changes){
  let entitiesCopy = JSON.parse(JSON.stringify(entities));
  for (const key in changes) {
    const entity = changes[key];
    if (entity.r.removed === true){
      delete entitiesCopy[key];
    } else if (entity.r.added === true){
      entitiesCopy[key] = entity;
    } else {
      entitiesCopy[key].p.x += entity.p.x;
      entitiesCopy[key].p.y += entity.p.y;
      
      //Loop through all entities and move the ones that are attached to this entity
      for (const entityKey in entitiesCopy) {
        const childEntity = entitiesCopy[entityKey];
        if(childEntity.a === entity.i){
          childEntity.p.x += entity.p.x;
          childEntity.p.y += entity.p.y;
        }
      }
    }
  }
  return entitiesCopy;
}
//#endregion

//#region Main API Calls
function performRequest(url, then, errorMessage, errorFunction = null) {
  fetch(url)
    .then((response) => response.json())
    .then((responseObj) => {
      then(responseObj);
    })
    .catch((error) => {
      showError(-1, errorMessage);
      console.error(error);
      if (errorFunction !== null) {
        errorFunction();
      }
    });
}

function fetchPhysicsLogFileList(then) {
  let url = apiBaseUrl + "/getPhysicsLogList";
  function afterFetch(responseObj) {
    console.log(responseObj);
    if (responseObj.errorCode !== 0) {
      showError(responseObj.errorCode, responseObj.errorMessage);
      return;
    }

    recentPhysicsLogFilesList = responseObj.recentPhysicsLogFiles;
    savedPhysicsRecordingsList = responseObj.savedPhysicsRecordings;
    isRecording = responseObj.isRecording;

    if (recentPhysicsLogFilesList.length > 0) {
      selectedRecording = 0;
      if (isRecording) {
        selectedRecording = 1;
      }
      selectedRecordingType = RecordingTypes.Recent;
    } else if (recentPhysicsLogFilesList.length === 0 && savedPhysicsRecordingsList.length > 0) {
      selectedRecording = savedPhysicsRecordingsList[0].id;
      selectedRecordingType = RecordingTypes.Saved;
    } else {
      showError(-1, "No recordings found. Please start a recording through CCT.");
      return;
    }

    showRecordingList();
    updateRecordingActionButtonStates();

    then();
  }
  function onError() {
    // isOffline = true;
    // OnShowMainView();
  }

  performRequest(url, afterFetch, "Failed to fetch physics log file list (is CCT running?)", onError);
}

function afterFetchPhysicsLogFileList() {
  fetchRoomLayout(afterFetchRoomLayout);
}

function fetchRoomLayout(then) {
  let subfolderName =
    selectedRecordingType === RecordingTypes.Recent ? "recent-recordings" : "saved-recordings";

  let url =
    apiBaseUrl +
    "/getFileContent?folder=physics-recordings&subfolder=" +
    subfolderName +
    "&file=" +
    selectedRecording +
    "_room-layout&extension=json";
  function afterFetch(responseObj) {
    console.log(responseObj);
    if (responseObj.errorCode !== 0) {
      showError(responseObj.errorCode, responseObj.errorMessage);
      return;
    }
    let fileContentStr = responseObj.fileContent;

    roomLayoutRecording = JSON.parse(fileContentStr);
    roomLayouts = roomLayoutRecording.rooms;

    //Find the id selectedRecording in the list of savedPhysicsRecordingsList
    if (selectedRecordingType === RecordingTypes.Saved) {
      let recording = null;
      for (let i = 0; i < savedPhysicsRecordingsList.length; i++) {
        if (savedPhysicsRecordingsList[i].id === selectedRecording) {
          recording = savedPhysicsRecordingsList[i];
          break;
        }
      }
      if (recording !== null) {
        roomLayoutRecording.name = recording.name;
      }
    }

    then();
  }

  performRequest(url, afterFetch, "Failed to fetch room layout (is CCT running?)");
}
function afterFetchRoomLayout() {
  fetchPhysicsLog(goToInspectorView);
}

function fetchPhysicsLog(then) {
  let subfolderName =
    selectedRecordingType === RecordingTypes.Recent ? "recent-recordings" : "saved-recordings";

  let url =
    apiBaseUrl +
    "/getFileContent?folder=physics-recordings&subfolder=" +
    subfolderName +
    "&file=" +
    selectedRecording +
    "_position-log&extension=txt";
  function afterFetch(responseObj) {
    console.log(responseObj);
    if (responseObj.errorCode !== 0) {
      showError(responseObj.errorCode, responseObj.errorMessage);
      return;
    }

    let fileContentStr = responseObj.fileContent;
    physicsLogFrames = parsePhysicsLogFrames(fileContentStr);
    filterPhysicsLogFrames();

    then();
  }

  performRequest(url, afterFetch, "Failed to fetch physics log (is CCT running?)");
}


function getPhysicsLogAsStrings() {
  function appendToLine(line, frame, key) {
    if (line.length > 0) {
      line += ",";
    }
    line += frame[key];
    return line;
  }
  function frameToString(frame) {
    let line = "";
    line = appendToLine(line, frame, "frameNumber");
    line = appendToLine(line, frame, "frameNumberRTA");
    line = appendToLine(line, frame, "positionX");
    line = appendToLine(line, frame, "positionY");
    line = appendToLine(line, frame, "speedX");
    line = appendToLine(line, frame, "speedY");
    line = appendToLine(line, frame, "velocityX");
    line = appendToLine(line, frame, "velocityY");
    line = appendToLine(line, frame, "liftBoostX");
    line = appendToLine(line, frame, "liftBoostY");
    line = appendToLine(line, frame, "speedRetention");
    line = appendToLine(line, frame, "stamina");
    line = appendToLine(line, frame, "flags");
    line = appendToLine(line, frame, "inputs");
    line = appendToLine(line, frame, "analogAimX");
    line = appendToLine(line, frame, "analogAimY");
    line += ","+JSON.stringify(frame.entities);
    return line;
  }

  let arr = [];

  for (let i = 0; i < physicsLogFrames.length; i++) {
    let frame = physicsLogFrames[i];
    let line = frameToString(frame);
    arr.push(line);

    for (let j = 0; j < frame.idleFrames.length; j++) {
      let idleFrame = frame.idleFrames[j];
      let idleLine = frameToString(idleFrame);
      arr.push(idleLine);
    }
  }

  return arr;
}
//#endregion

//#region Interaction API Calls
function saveCurrentRecording(name, dataObj = null) {
  let request;

  if (dataObj == null) {
    request = {
      layoutFile: roomLayoutRecording,
      physicsLog: getPhysicsLogAsStrings(),
      name: name,
    };
  } else {
    request = {
      layoutFile: dataObj.layoutFile,
      physicsLog: dataObj.physicsLog,
      name: name,
    };
  }

  let url = apiBaseUrl + "/saveRecording";
  //Fetch request
  fetch(url, {
    method: "POST",
    headers: {
      Accept: "application/json",
    },
    body: JSON.stringify(request),
  })
    .then((response) => response.json())
    .then((responseObj) => {
      if (responseObj.errorCode === 0) {
        //location.reload();
      }
    })
    .catch((err) => {
      console.log(err);
    });
}

function deleteRecording(id) {
  let url = apiBaseUrl + "/deleteRecording?id=" + id;
  //Fetch request
  fetch(url, {
    method: "GET",
    headers: {
      Accept: "application/json",
    },
  })
    .then((response) => response.json())
    .then((responseObj) => {
      if (responseObj.errorCode === 0) {
        location.reload();
      }
    })
    .catch((err) => {
      console.log(err);
    });
}
function renameRecording(id, newName) {
  let request = {
    id: id,
    name: newName,
  };

  let url = apiBaseUrl + "/renameRecording";
  //Fetch request
  fetch(url, {
    method: "POST",
    headers: {
      Accept: "application/json",
    },
    body: JSON.stringify(request),
  })
    .then((response) => response.json())
    .then((responseObj) => {
      if (responseObj.errorCode === 0) {
        location.reload();
      }
    })
    .catch((err) => {
      console.log(err);
    });
}
//#endregion
