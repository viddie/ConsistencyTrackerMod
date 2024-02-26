//#region Constants
const DisplayMode ={
    Classic: {
        name: "Classic",
        description: "Shows multiple frames at once, hover over a frame to see movable entities and more details."
    },
    Replay: {
        name: "Replay",
        description: "Shows a single frame at a time, with the ability to play and pause the recording."
    }
};
const PointLabelNone = "None";
const PointLabels= {
  None: "None",

  DragX: "Drag X",
  DragY: "Drag Y",

  PositionX: "Position X",
  PositionY: "Position Y",
  PositionCombined: "Position X & Y",
  SpeedX: "Speed X",
  SpeedY: "Speed Y",
  SpeedCombined: "Speed X & Y",
  AccelerationX: "Acceleration X",
  AccelerationY: "Acceleration Y",
  AccelerationCombined: "Acceleration X & Y",
  AbsoluteSpeed: "Absolute Speed",
  VelocityX: "Velocity X",
  VelocityY: "Velocity Y",
  VelocityCombined: "Velocity X & Y",
  VelocityDifferenceX: "Velocity Diff X",
  VelocityDifferenceY: "Velocity Diff Y",
  VelocityDifferenceCombined: "Velocity Diff X & Y",
  LiftBoostX: "LiftBoost X",
  LiftBoostY: "LiftBoost Y",
  LiftBoostCombined: "LiftBoost X & Y",
  RetainedSpeed: "Retained Speed",
  Stamina: "Stamina",
  Inputs: "Inputs",
};
//#endregion

//#region Settings
let selectedRecordingType = RecordingTypes.Recent;
let selectedRecording = 0; //When FileTypes.Recent, this is the index of the file in the list. When FileTypes.Saved, this is the ID of the recording.
let settings = {
  alwaysShowFollowLine: false,
  showRoomNames: true,
  showSpinnerRectangle: true,
  showOnlyRelevantRooms: true,
  rasterizeMovement: false,
  debugEntities: false,

  displayMode: DisplayMode.Classic.name,
  
  frameStepSize: 500,
  frameMin: 0,
  
  replaySpeed: 1,
  replayPlaying: false,
  replayIgnoreIdleFrames: false,
  replayCenterCamera: true,

  decimals: 2,

  menuHidden: false,
  pointLabels: "Inputs",

  layerVisibleRoomLayout: true,
  layerVisibleRoomEntities: true,
  layerVisibleMaddyHitbox: true,
  layerVisibleTooltip: true,
  layerVisiblePosition: true,

  tooltipInfo: {
    frame: true,
    frameRTA: false,
    position: true,
    speed: true,
    acceleration: false,
    absoluteSpeed: false,
    velocity: true,
    velocityDifference: false,
    liftboost: true,
    retainedSpeed: true,
    stamina: true,
    inputs: true,
    flags: true,
    subpixelDisplay: true,
    analogDisplay: false,
  },
};
//#endregion

//#region Settings Elements
let settingsElements = {
  showRoomNames: "Show Room Names",
  rasterizeMovement: "Rasterize Movement",
  debugEntities: "Debug Entities",
  replayCenterCamera: "Camera: Follow Frames",
  replayIgnoreIdleFrames: "Replay: Skip Idle Frames",
};
let layerVisibilityElements = {
  layerVisibleRoomLayout: "Room Layout",
  layerVisibleRoomEntities: "Room Entities",
  layerVisibleTooltip: "Tooltip",
  layerVisibleMaddyHitbox: "Hit-/Hurtbox",
  layerVisiblePosition: "Position",
};
let tooltipsInfoElements = {
  frame: "Frame",
  frameRTA: "RTA Frame",
  position: "Position",
  stamina: "Stamina",
  speed: "Speed",
  inputs: "Inputs",
  acceleration: "Acceleration",
  absoluteSpeed: "Abs. Speed",
  velocity: "Velocity",
  flags: "Flags",
  velocityDifference: "Velocity Difference",
  liftboost: "Liftboost",
  retainedSpeed: "Retained Speed",
  subpixelDisplay: "Subpixel Display",
  analogDisplay: "Analog Display",
};

let settingsInited = false;
//#endregion

//#region Settings Load/Update
function loadSettings() {
  let settingsStr = localStorage.getItem("settings");
  if (settingsStr !== null) {
    settings = JSON.parse(settingsStr);
  }

  settings.frameMin = 0;

  createOnOffSettings();
  createLayerVisibilitySettings();
  createTooltipInfoSettings();
  createPointLabelOptions();

  //Fix the initial state of newly added settings
  if(settings.replayPlaying === undefined){
    settings.replayPlaying = false;
  }
  if(settings.replaySpeed === undefined){
    settings.replaySpeed = 1;
  }
  if(settings.replayIgnoreIdleFrames === undefined){
    settings.replayIgnoreIdleFrames = false;
  }
  if (settings.replayCenterCamera === undefined){
    settings.replayCenterCamera = true;
  }
  if(settings.displayMode === undefined){
    settings.displayMode = DisplayMode.Classic.name;
  }
  
  updateSettings();

  settingsInited = true;
}
function saveSettings(redraw = true) {
  localStorage.setItem("settings", JSON.stringify(settings));
  updateSettings();
  if(redraw) redrawCanvas();
}

function updateSettings(){
  refreshSidebarMenuVisibility();
  
  Elements.PointLabels.value = settings.pointLabels;
  Elements.SelectDecimals.value = settings.decimals+"";
  Elements.FrameStepSize.value = settings.frameStepSize+"";
  Elements.ReplaySpeed.value = settings.replaySpeed+"";
  Elements.CheckReplayPlaying.checked = settings.replayPlaying;
  
  Elements.DisplayModeButton.innerText = "Mode: " + settings.displayMode;
  
  updateLayerVisibilities();
  updateFrameButtonStates();
  updateRecordingInfo();
  updateRecordingActionButtonStates();
  updateJumpToRoomSelect();
}
function refreshSidebarMenuVisibility() {
  //Slide the menu out to the left by setting transform: translateX(-100%)
  if (settings.menuHidden) {
    Elements.SidebarMenu.style.transform = "translateX(-100%)";
  } else {
    Elements.SidebarMenu.style.transform = "translateX(0%)";
  }
}
function updateLayerVisibilities(){
  if(konvaRoomLayoutLayer === null) return; //Not ready yet
  
  if (!settings.layerVisibleRoomLayout) konvaRoomLayoutLayer.hide();
  else konvaRoomLayoutLayer.show();

  if (!settings.layerVisibleRoomEntities) konvaRoomEntitiesLayer.hide();
  else konvaRoomEntitiesLayer.show();

  if (!settings.layerVisibleMaddyHitbox) konvaMaddyHitboxLayer.hide();
  else konvaMaddyHitboxLayer.show();

  if (!settings.layerVisibleTooltip) {
    konvaTooltipLayer.hide();
    konvaLowPrioTooltipLayer.hide();
  } else {
    konvaTooltipLayer.show();
    konvaLowPrioTooltipLayer.show();
  }

  if (!settings.layerVisiblePosition) konvaPositionLayer.hide();
  else konvaPositionLayer.show();
    
  
  if(roomLayoutRecording.usesMovableEntities && settings.frameStepSize === 1){
    konvaRoomMovableEntitiesInitialLayer.visible(false);
  } else {
    konvaRoomMovableEntitiesInitialLayer.visible(true);
  }
}
function updateFrameButtonStates() {
  if(physicsLogFrames === null) return; //Not ready yet
  
  let stepSize = settings.displayMode === DisplayMode.Replay.name ? 1 : settings.frameStepSize;
  
  if (settings.frameMin === 0) {
    Elements.PreviousFramesButton.setAttribute("disabled", true);
  } else {
    Elements.PreviousFramesButton.removeAttribute("disabled");
  }
  if (settings.frameMin + stepSize >= physicsLogFrames.length) {
    Elements.NextFramesButton.setAttribute("disabled", true);
    Elements.FinalFramesButton.setAttribute("disabled", true);
  } else {
    Elements.NextFramesButton.removeAttribute("disabled");
    Elements.FinalFramesButton.removeAttribute("disabled");
  }
}
function updateRecordingInfo() {
  if(recentPhysicsLogFilesList === null || savedPhysicsRecordingsList === null) return; //Not ready yet
  
  let isRecent = selectedRecordingType === RecordingTypes.Recent;

  let recordingTypeText = "Type: " + (isRecent ? "Recent" : "Saved");
  let recordingNameText;

  if (isRecent) {
    let inRecordingString = isRecording ? " (Recording...)" : "";
    let fileOffset = isRecording ? 1 : 0;
    recordingNameText =
      "Recording: (" +
      (selectedRecording + 1) +
      "/" +
      (recentPhysicsLogFilesList.length + fileOffset) +
      ")" +
      inRecordingString;
  } else {
    //search through savedPhysicsRecordingsList for the id, and get the name
    for (let i = 0; i < savedPhysicsRecordingsList.length; i++) {
      if (savedPhysicsRecordingsList[i].id === selectedRecording) {
        recordingNameText = "Recording: " + savedPhysicsRecordingsList[i].name;
        break;
      }
    }
  }
  
  let frameCountText = physicsLogFrames.length + " frames";
  let frameAddition = "";
  if(settings.displayMode === DisplayMode.Replay.name && !settings.replayIgnoreIdleFrames){
    frameAddition = " (+"+(replayData.idleFrameIndex+1)+")";
  }
  let frameToAddition = "";
  if(settings.displayMode !== DisplayMode.Replay.name){
    frameToAddition = " - "+Math.min(settings.frameMin + settings.frameStepSize, physicsLogFrames.length);
  }
  
  let showingFramesText =
    "(Showing: " + settings.frameMin + frameAddition + frameToAddition + ")";

  let sideAddition = "";
  if (roomLayoutRecording.sideName !== "A-Side") {
    sideAddition = " [" + roomLayoutRecording.sideName + "]";
  }
  let mapText = "Map: " + roomLayoutRecording.chapterName + sideAddition;
  
  let roomText = "Room (debug name): ";
  let currentRoom = getRoomFromFrame(physicsLogFrames[settings.frameMin]);
  if(currentRoom !== null){
    roomText += currentRoom.debugRoomName; 
  } else {
    roomText += "---";
  }

  //parse roomLayoutRecording.recordingStarted from "2020-05-01T20:00:00.0000000+02:00" to "2020-05-01 20:00:00"
  let date = new Date(roomLayoutRecording.recordingStarted);
  let dateString =
    date.getFullYear() +
    "-" +
    zeroPad(date.getMonth() + 1, 2) +
    "-" +
    zeroPad(date.getDate(), 2) +
    " " +
    zeroPad(date.getHours(), 2) +
    ":" +
    zeroPad(date.getMinutes(), 2) +
    ":" +
    zeroPad(date.getSeconds(), 2);
  let timeRecordedText = "Time recorded: " + dateString;

  Elements.RecordingDetails.innerText =
    recordingNameText +
    "\n" +
    frameCountText +
    " " +
    showingFramesText +
    "\n" +
    mapText +
    "\n" +
    roomText +
    "\n" +
    timeRecordedText;
}
function updateRecordingActionButtonStates() {
  if (selectedRecordingType === RecordingTypes.Recent) {
    Elements.SaveRecordingButton.removeAttribute("disabled");
    Elements.RenameRecordingButton.setAttribute("disabled", true);
    Elements.DeleteRecordingButton.setAttribute("disabled", true);
  } else {
    Elements.SaveRecordingButton.setAttribute("disabled", true);
    Elements.RenameRecordingButton.removeAttribute("disabled");
    Elements.DeleteRecordingButton.removeAttribute("disabled");
  }
}
function updateJumpToRoomSelect(){
  if (roomLayouts === null) return; //Not ready yet

  let select = Elements.SelectJumpToRoom;
  select.innerHTML = "";

  let option = document.createElement("option");
  option.value = -1;
  option.text = "-- Jump to Room --";
  select.appendChild(option);

  for (let i = 0; i < roomLayouts.length; i++) {
    let room = roomLayouts[i];
    option = document.createElement("option");
    option.value = i;
    option.text = room.debugRoomName;
    select.appendChild(option);
  }

  select.value = -1;
}
//#endregion

//#region Create Settings Elements
function createOnOffSettings(){
  for (const key in settingsElements) {
    const label = settingsElements[key];
    let div = createSettingsCheckbox(key, label, settings[key], changedBoolSetting);
    Elements.OptionsContainer.appendChild(div);
  }
}

function createLayerVisibilitySettings() {
  for (const key in layerVisibilityElements) {
    const label = layerVisibilityElements[key];
    let div = createSettingsCheckbox(key, label, settings[key], changedLayerVisibility);
    Elements.LayersContainer.appendChild(div);
  }
}

function createTooltipInfoSettings(){
  let combinedRows = 0;
  let storedKey = null;
  for (const key in tooltipsInfoElements) {
    if (combinedRows < 5 && storedKey == null) {
      storedKey = key;
    } else if (storedKey != null) {
      const storedLabel = tooltipsInfoElements[storedKey];
      let storedDiv = createSettingsCheckbox(
          storedKey,
          storedLabel,
          settings.tooltipInfo[storedKey],
          changedTooltipInfo
      );
      storedDiv.style.width = "50%";

      const label = tooltipsInfoElements[key];
      let div = createSettingsCheckbox(key, label, settings.tooltipInfo[key], changedTooltipInfo);

      //Combine the two divs into one row
      let container = document.createElement("div");
      container.classList.add("flex-center");
      container.appendChild(storedDiv);
      container.appendChild(div);
      container.style.justifyContent = "start";
      Elements.TooltipsContainer.appendChild(container);

      storedKey = null;
      combinedRows++;
    } else {
      const label = tooltipsInfoElements[key];
      let div = createSettingsCheckbox(key, label, settings.tooltipInfo[key], changedTooltipInfo);
      Elements.TooltipsContainer.appendChild(div);
    }
  }
}

function createPointLabelOptions(){
  for (const key in PointLabels) {
    const label = PointLabels[key];
    let option = document.createElement("option");
    option.value = key;
    option.text = label;
    Elements.PointLabels.appendChild(option);
  }
}

function createSettingsCheckbox(settingName, settingLabel, currentValue, onChangeFunction) {
  let checkbox = document.createElement("input");
  checkbox.type = "checkbox";
  checkbox.id = "check-" + settingName;
  checkbox.checked = currentValue;
  checkbox.onchange = function () {
    onChangeFunction(settingName, this.checked);
  };

  let label = document.createElement("label");
  label.htmlFor = "check-" + settingName;
  label.appendChild(document.createTextNode(settingLabel));

  let div = document.createElement("div");
  div.classList.add("flex-center");
  div.classList.add("flex-start");
  div.appendChild(checkbox);
  div.appendChild(label);

  return div;
}
//#endregion

//#region Settings Change Functions
function changedBoolSetting(settingName, value) {
  settings[settingName] = value;
  saveSettings();
}
function changedLayerVisibility(layerName, value) {
  let layerToSet = null;
  switch (layerName) {
    case "layerVisibleRoomLayout":
      layerToSet = konvaRoomLayoutLayer;
      break;
    case "layerVisibleRoomEntities":
      layerToSet = konvaRoomEntitiesLayer;
      break;
    case "layerVisibleMaddyHitbox":
      layerToSet = konvaMaddyHitboxLayer;
      break;
    case "layerVisibleTooltip":
      layerToSet = konvaTooltipLayer;
      break;
    case "layerVisiblePosition":
      layerToSet = konvaPositionLayer;
      break;
  }

  settings[layerName] = value;
  saveSettings();
}
function changedTooltipInfo(settingName, value) {
  settings.tooltipInfo[settingName] = value;
  saveSettings();
}
function changedPointLabel(pointLabel) {
  if (!settingsInited) return;
  settings.pointLabels = pointLabel;
  saveSettings();
}
function changedDecimals(decimals) {
  if (!settingsInited) return;
  settings.decimals = parseInt(decimals);
  saveSettings();
}
function changedFrameStepSize(value){
  settings.frameStepSize = parseInt(value);
  saveSettings();
}
function changedReplaySpeed(value){
  settings.replaySpeed = parseFloat(value);
  saveSettings();
}

function framePageUp(mult = 1, event) {
  let stepSize = settings.displayMode === DisplayMode.Replay.name ? 1 : settings.frameStepSize;
  stepSize = event.ctrlKey ? 1 : event.shiftKey ? stepSize * 5 : stepSize;
  if (settings.frameMin === -1) {
    settings.frameMin = stepSize * (mult - 1);
  } else {
    settings.frameMin += stepSize * mult;
  }

  if (settings.frameMin < 0) {
    settings.frameMin = 0;
  } else if (settings.frameMin >= physicsLogFrames.length) {
    settings.frameMin = physicsLogFrames.length - stepSize;
  }
  
  const centerFrame = physicsLogFrames[Math.min(settings.frameMin + Math.floor(stepSize / 2), physicsLogFrames.length - 1)];

  saveSettings();
  if(settings.replayCenterCamera && !areModelCoordinatesOnScreen(centerFrame.positionX, centerFrame.positionY)){
    centerOnPositionReal(centerFrame.positionX, centerFrame.positionY);
  }
}
function frameEnd(event) {
  let stepSize = settings.displayMode === DisplayMode.Replay.name ? 1 : settings.frameStepSize;
  if(event.ctrlKey){
    settings.frameMin = 0;
  } else {
    settings.frameMin = physicsLogFrames.length - stepSize;
  }
  
  const centerFrame = physicsLogFrames[Math.min(settings.frameMin + Math.floor(stepSize / 2), physicsLogFrames.length - 1)];

  saveSettings();
  centerOnPositionReal(centerFrame.positionX, centerFrame.positionY);
}
function resetFramePage() {
  settings.frameMin = 0;
  saveSettings();
}
function jumpToRoom(index){
  if(index === -1) return;
  
  let room = roomLayouts[index];
  let bounds = room.levelBounds; //{x, y, w, h}
  
  //Find the first frame that falls into the bounds of this room
  let frameIndex = 0;
  for (let i = 0; i < physicsLogFrames.length; i++) {
    let frame = physicsLogFrames[i];
    if (frame.positionX >= bounds.x && frame.positionX <= bounds.x + bounds.w && frame.positionY >= bounds.y && frame.positionY <= bounds.y + bounds.h) {
      frameIndex = i;
      break;
    }
  }
  
  //Search frames ahead until a frame is found that does NOT have the flag "NoControl"
  for (let i = frameIndex; i < physicsLogFrames.length; i++) {
    let frame = physicsLogFrames[i];
    if (!frame.flags.includes("NoControl")) {
      frameIndex = i;
      break;
    }
  }
  
  settings.frameMin = frameIndex;
  saveSettings();
  
  let frame = physicsLogFrames[frameIndex];
  centerOnPositionReal(frame.positionX, frame.positionY);
}
function jumpToNextRoom(event, dir){
  if(roomLayouts === null) return;
  
  //Find the room that the settings.frameMin frame is in
  let currentRoomIndex = getRoomIndexFromFrameIndex(settings.frameMin);
  if(currentRoomIndex === -1) return;

  let distance = event.shiftKey ? 3 : 1;
  let nextRoomIndex = currentRoomIndex + dir * distance;
  nextRoomIndex = Math.max(0, Math.min(roomLayouts.length - 1, nextRoomIndex));

  jumpToRoom(nextRoomIndex);
}

function changeRecording(selected) {
  //selected is in the form of "recent-0" or "saved-0"
  let split = selected.split("-");
  let selectedRecordingTypeStr = split[0];
  let selectedRecordingId = parseInt(split[1]);

  selectedRecordingType = selectedRecordingTypeStr == "recent" ? RecordingTypes.Recent : RecordingTypes.Saved;
  selectedRecording = selectedRecordingId;

  console.log("ChangeRecording -> Selected: ", selected);

  fetchRoomLayout(afterFetchRoomLayout);
  resetFramePage(); //Will update settings
}
function toggleSidebarMenuSetting() {
  settings.menuHidden = !settings.menuHidden;
  saveSettings(false);
}

function toggleDisplayMode(){
  if(settings.displayMode === DisplayMode.Classic.name){
      settings.displayMode = DisplayMode.Replay.name;
  } else {
      settings.displayMode = DisplayMode.Classic.name;
  }
  
  saveSettings();
}
//#endregion

