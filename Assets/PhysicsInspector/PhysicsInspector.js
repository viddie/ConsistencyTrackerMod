const ViewStates = {
  MainView: 0,
  InspectorView: 1,
};
let CurrentState = null;


const Elements = {
  MainContainer: "main-view",
  LoadingText: "loading-text",

  InspectorContainer: "inspector-view",
  CanvasContainer: "canvas-container",
  SidebarMenu: "sidebar-menu",

  NextFramesButton: "next-frames-button",
  PreviousFramesButton: "previous-frames-button",
  FinalFramesButton: "final-frames-button",
  SelectJumpToRoom: "select-jump-to-room",

  SelectedRecording: "selected-recording",
  OptgroupRecent: "optgroup-recent",
  OptgroupSaved: "optgroup-saved",

  SaveRecordingButton: "save-recording-button",
  DeleteRecordingButton: "delete-recording-button",
  RenameRecordingButton: "rename-recording-button",

  ImportRecordingButton: "import-recording-button",
  ImportModal: "import-modal",
  ExportRecordingButton: "export-recording-button",
  ExportModal: "export-modal",

  RecordingDetails: "recording-details",
  // EntityCounts: "entity-counts",

  OptionsContainer: "options-container",
  FrameStepSize: "selected-frame-step-size",
  PointLabels: "point-labels",
  SelectDecimals: "select-decimals",
  
  DisplayModeButton: "change-display-mode-button",
  CheckReplayPlaying: "check-replayPlaying",
  ReplaySpeed: "selected-replay-speed",
  
  LayersModalButton: "layers-modal-button",
  LayersModal: "layers-modal",
  LayersContainer: "layers-container",
  
  TooltipsModalButton: "tooltips-modal-button",
  TooltipsModal: "tooltips-modal",
  TooltipsContainer: "tooltips-container",

  OtherToolsButton: "other-tools-button",
  OtherToolsModal: "other-tools-modal",
  EntitiesCountModal: "entities-count-modal",
  OtherEntitiesModal: "other-entities-modal",
};



//#region Startup
document.addEventListener("DOMContentLoaded", function () {
  loadElements(Elements);
  loadSettings();
  ShowState(ViewStates.MainView);
});

function ShowState(state) {
  CurrentState = state;
  switch (state) {
    case ViewStates.MainView:
      Elements.MainContainer.style.display = "flex";
      Elements.InspectorContainer.style.display = "none";
      OnShowMainView();
      break;
    case ViewStates.InspectorView:
      Elements.MainContainer.style.display = "none";
      Elements.InspectorContainer.style.display = "flex";
      OnShowInspectorView();
      break;
  }
}

function showError(errorCode, errorMessage) {
  let message = "Error (" + errorCode + "): " + errorMessage;
  console.log(message);

  if (CurrentState === ViewStates.MainView) {
    Elements.LoadingText.innerText = message;
  }
}
//#endregion

//#region MainView
function OnShowMainView() {
  fetchPhysicsLogFileList(afterFetchPhysicsLogFileList);
}

function showRecordingList() {
  //Clear the optgroups
  Elements.OptgroupRecent.innerHTML = "";
  Elements.OptgroupSaved.innerHTML = "";

  //Add recent recordings
  if (isRecording) {
    let opt = document.createElement("option");
    opt.value = "recent-" + 0;
    opt.disabled = true;
    opt.innerText = "(1) Recording in progress...";
    Elements.OptgroupRecent.appendChild(opt);
  }

  for (let i = 0; i < recentPhysicsLogFilesList.length; i++) {
    let recording = recentPhysicsLogFilesList[i];
    let id = recording.id;

    let opt = document.createElement("option");
    opt.value = "recent-" + id;
    opt.innerText = getRecordingDisplayName(RecordingTypes.Recent, recording);
    Elements.OptgroupRecent.appendChild(opt);
  }

  //Add saved recordings
  for (let i = 0; i < savedPhysicsRecordingsList.length; i++) {
    let recording = savedPhysicsRecordingsList[i];
    let id = recording.id;
    let opt = document.createElement("option");
    opt.value = "saved-" + id;
    opt.innerText = getRecordingDisplayName(RecordingTypes.Saved, recording);
    Elements.OptgroupSaved.appendChild(opt);
  }
}

function goToInspectorView() {
  ShowState(ViewStates.InspectorView);
}

//#endregion

//#region EditView
function OnShowInspectorView() {
  //Display data here
  createCanvas();
  createLayers();
  addMouseHandlers();

  if(settings.displayMode === DisplayMode.Classic.name){
    createAllElements();
  } else if(settings.displayMode === DisplayMode.Replay.name){
    replayModeCreateInitialState();
  }

  //Sidemenu things
  let firstFrame = physicsLogFrames[0];

  let firstRoomBounds = roomLayouts[0].levelBounds;
  let centerX = firstRoomBounds.x + firstRoomBounds.w / 2;
  let centerY = firstRoomBounds.y + firstRoomBounds.h / 2;

  //If the distance of firstFrame position to center of first room is greater than 1000, then center on firstFrame position
  let distanceToCenter = Math.sqrt(
    Math.pow(centerX - firstFrame.positionX, 2) + Math.pow(centerY - firstFrame.positionY, 2)
  );
  if (distanceToCenter > 1000) {
    centerX = firstFrame.positionX;
    centerY = firstFrame.positionY;
  }

  centerOnPosition(centerX, centerY);
  updateSettings();
  
  if(replayTimeout){
    clearTimeout(replayTimeout);
  }
  replayTimeoutFunction();
}


let replayTimeout = null;
let replayIdleFrameCounter = 0;
function replayTimeoutFunction() {
  let speed = settings.replaySpeed; //In multiplier of frames, where 1 is 1 frame per 60th of a second
  let ms = 1000 / 60 / speed;

  if(settings.replayPlaying){
    let frame = physicsLogFrames[settings.frameMin];
    if(settings.displayMode === DisplayMode.Classic.name){
      if(frame.idleFrames.length > replayIdleFrameCounter && replayIdleFrameCounter < 40 && frame.flags.indexOf("NoControl") === -1 && !settings.replayIgnoreIdleFrames){
        redrawCanvas(frame.idleFrames[replayIdleFrameCounter].entities);
        replayIdleFrameCounter++;
      } else {
        replayIdleFrameCounter = 0;

        settings.frameMin += 1;
        if (settings.frameMin < 0 || settings.frameMin >= physicsLogFrames.length) {
          settings.frameMin = 0;
        }

        frame = physicsLogFrames[settings.frameMin];
        updateRecordingInfo();
        redrawCanvas();
      }

      if(settings.replayCenterCamera && !areModelCoordinatesComfortablyOnScreen(frame.positionX, frame.positionY)){
        centerOnPositionReal(frame.positionX, frame.positionY);
      }
    } else if (settings.displayMode === DisplayMode.Replay.name) {
      replayModeNextFrame();
      if(settings.replayCenterCamera && !areModelCoordinatesComfortablyOnScreen(frame.positionX, frame.positionY)){
        centerOnPositionReal(frame.positionX, frame.positionY);
      }
    }
  }
  replayTimeout = setTimeout(replayTimeoutFunction, ms);
}

function getRasterziedPosition(frame) {
  if (settings.rasterizeMovement) {
    return {
      positionX: Math.round(frame.positionX),
      positionY: Math.round(frame.positionY),
    };
  }

  return {
    positionX: frame.positionX,
    positionY: frame.positionY,
  };
}


//#endregion

//#region Util
function goToPosition(x, y) {
  konvaStage.position({
    x: -x,
    y: -y,
  });
}

function konvaStageDebug(){
  console.log("Konva Stage: ", {
    width: konvaStage.width(),
    height: konvaStage.height(),
    scale: konvaStage.scaleX(),
    x: konvaStage.x(),
    y: konvaStage.y(),
  });
}

function centerOnPosition(x, y) {
  goToPosition(x - konvaStage.width() / 2, y - konvaStage.height() / 2);
}

function centerOnPositionReal(x, y){
  const scale = konvaStage.scaleX();
  const newPos = {
    x: -x * scale + konvaStage.width() / 2,
    y: -y * scale + konvaStage.height() / 2,
  };
  konvaStage.position(newPos);
}

function areModelCoordinatesOnScreen(x, y){
  const screenCoords = modelCoordinatesToScreen(x, y);
  return screenCoords.x >= 0 && screenCoords.x <= konvaStage.width() && screenCoords.y >= 0 && screenCoords.y <= konvaStage.height();
}
function areModelCoordinatesComfortablyOnScreen(x, y){
  const screenCoords = modelCoordinatesToScreen(x, y);
  //Check if the coordinates are within comfortably within the screen (100 pixels distance from the edge)
  return screenCoords.x >= 300 && screenCoords.x <= konvaStage.width() - 200 && screenCoords.y >= 100 && screenCoords.y <= konvaStage.height() - 100;
}
function modelCoordinatesToScreen(x, y){
  const scale = konvaStage.scaleX();
  return {
    x: x * scale + konvaStage.x(),
    y: y * scale + konvaStage.y(),
  };
}

function centerOnRoom(roomIndex) {
  let roomBounds = roomLayouts[roomIndex].levelBounds;
  let centerX = roomBounds.x + roomBounds.width / 2;
  let centerY = roomBounds.y + roomBounds.height / 2;
  centerOnPositionReal(centerX, centerY);
}

function zeroPad(num, size) {
  var s = num + "";
  while (s.length < size) s = "0" + s;
  return s;
}

function VectorAngleToTAS(angle) {
  return modulo(-angle + 90, 360);
}
function modulo(x, m) {
  return ((x % m) + m) % m;
}
//#endregion

//#region Dialogs
function openSaveRecordingDialog() {
  xdialog.open({
    title: "Save Recording As...",
    body: '<input type="text" id="recording-name" class="dialog-item" placeholder="Recording Name" required/>',
    buttons: { ok: "Save", cancel: "Cancel" },
    style: "width:600px",
    listenEnterKey: true,
    listenESCKey: true,
    onok: function (param) {
      let element = document.getElementById("recording-name");
      element.classList.add("validated");

      let recordingName = element.value;
      if (!recordingName) {
        return false;
      }

      console.log("Typed name:", recordingName);
      saveCurrentRecording(recordingName);
    },
  });
}

function openDeleteRecordingDialog() {
  let recordingName = getRecordingDisplayNameByID(selectedRecordingType, selectedRecording);

  xdialog.open({
    title: "Delete Recording?",
    body: "Are you sure you want to delete the recording:<br><code>" + recordingName + "</code>",
    buttons: { delete: "Delete", cancel: "Cancel" },
    style: "width:600px",
    listenESCKey: true,
    ondelete: function () {
      deleteRecording(selectedRecording);
    },
  });
}

function openRenameRecordingDialog() {
  xdialog.open({
    title: "Rename Recording...",
    body: '<input type="text" id="recording-name" class="dialog-item" placeholder="New Name" required/>',
    buttons: { ok: "Rename", cancel: "Cancel" },
    style: "width:600px",
    listenEnterKey: true,
    listenESCKey: true,
    onok: function (param) {
      let element = document.getElementById("recording-name");
      element.classList.add("validated");

      let recordingName = element.value;
      if (!recordingName) {
        return false;
      }

      console.log("Typed name:", recordingName);
      renameRecording(selectedRecording, recordingName);
    },
  });
}
function openImportRecordingDialog() {
  let dataElem = Elements.ImportModal.querySelector("#import-input");
  let nameElem = Elements.ImportModal.querySelector("#import-name");

  dataElem.value = "";
  nameElem.value = "";

  xdialog.open({
    title: "Import Recording",
    body: { element: Elements.ImportModal },
    buttons: { ok: "Import Recording", cancel: "Cancel" },
    style: "width:600px",
    listenEnterKey: true,
    listenESCKey: true,
    onok: function (param) {
      let dataElem = Elements.ImportModal.querySelector("#import-input");
      let nameElem = Elements.ImportModal.querySelector("#import-name");

      dataElem.classList.add("validated");
      nameElem.classList.add("validated");

      let data = dataElem.value;
      let name = nameElem.value;

      if (!data || !name) {
        return false;
      }

      let dataObj = JSON.parse(data);
      if (!dataObj.layoutFile || !dataObj.physicsLog) {
        return false;
      }

      saveCurrentRecording(name, dataObj);
    },
  });
}

function openExportRecordingDialog() {
  let exportObj = {
    layoutFile: roomLayoutRecording,
    physicsLog: getPhysicsLogAsStrings(),
  };

  let inputElem = Elements.ExportModal.querySelector("#export-input");
  inputElem.value = JSON.stringify(exportObj);

  xdialog.open({
    title: "Export Recording",
    body: { element: Elements.ExportModal },
    buttons: { ok: "Copy to Clipboard", cancel: "Cancel" },
    style: "width:600px",
    listenEnterKey: true,
    listenESCKey: true,
    onok: function (param) {
      let inputElem = Elements.ExportModal.querySelector("#export-input");
      inputElem.select();
      let success = document.execCommand("copy");
      console.log("Copied to clipboard: ", success, inputElem.value, inputElem);
    },
  });
}
//#endregion

//#region Other Dialogs
function openLayersDialog(){
  xdialog.open({
    extraClass: "no-title",
    body: { element: Elements.LayersModal },
    buttons: { ok: "Ok" },
    style: "width:400px",
    modal: false,
    onok: function (param) {
    },
  });
}

function openTooltipsDialog() {
  xdialog.open({
    extraClass: "no-title",
    body: { element: Elements.TooltipsModal },
    buttons: { ok: "Ok" },
    style: "width:500px",
    modal: false,
    onok: function (param) {
    },
  });
}

function openOtherToolsDialog() {
  Elements.OtherToolsButton.setAttribute("disabled", true);
  xdialog.open({
    title: "Other Tools",
    body: { element: Elements.OtherToolsModal },
    buttons: { ok: "Ok" },
    style: "width:400px",
    modal: false,
    onok: function (param) {
      Elements.OtherToolsButton.removeAttribute("disabled");
    },
  });
}

function openEntitiesCountDialog() {
  let tableElem = Elements.EntitiesCountModal.querySelector("table");
  tableElem.innerHTML = "";

  //Create headers: Entity Name, Count
  let headerRow = document.createElement("tr");
  let headerCell = document.createElement("th");
  headerCell.innerText = "Entity Name";
  headerRow.appendChild(headerCell);
  headerCell = document.createElement("th");
  headerCell.innerText = "Count";
  headerRow.appendChild(headerCell);
  tableElem.appendChild(headerRow);

  //entityCounts is an object with keys being the entity type and values being the count
  //entityCounts already exists

  //Sort the keys based on the counts
  let sortedKeys = Object.keys(staticEntityCounts).sort(function (a, b) {
    return staticEntityCounts[b] - staticEntityCounts[a];
  });
  let totalCount = 0;
  for (let i = 0; i < sortedKeys.length; i++) {
    let key = sortedKeys[i];
    let count = staticEntityCounts[key];
    totalCount += count;
    let row = document.createElement("tr");
    let cell = document.createElement("td");
    cell.innerText = key;
    row.appendChild(cell);
    cell = document.createElement("td");
    cell.classList.add("centered");
    cell.innerText = count;
    row.appendChild(cell);
    tableElem.appendChild(row);
  }

  let totalElem = Elements.EntitiesCountModal.querySelector("#total-entities");
  totalElem.innerText = "Total Drawn Entities: " + totalCount + "";

  xdialog.open({
    title: "Entity Counts",
    body: { element: Elements.EntitiesCountModal },
    buttons: { ok: "Ok" },
    style: "min-width:600px;max-width:1000px;max-height:800px;",
    listenEnterKey: true,
    modal: true,
    ondrag: () => false,
  });
}
//#endregion
