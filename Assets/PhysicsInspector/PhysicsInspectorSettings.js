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

//#region Settings
let selectedRecordingType = RecordingTypes.Recent;
let selectedRecording = 0; //When FileTypes.Recent, this is the index of the file in the list. When FileTypes.Saved, this is the ID of the recording.
let settings = {
  alwaysShowFollowLine: false,
  showRoomNames: true,
  showSpinnerRectangle: true,
  showOnlyRelevantRooms: true,
  rasterizeMovement: false,

  frameStepSize: 500,
  frameMin: 0,
  
  replaySpeed: 1,
  replayPlaying: false,

  decimals: 2,

  menuHidden: false,
  pointLabels: "None",

  layerVisibleRoomLayout: true,
  layerVisibleRoomEntities: true,
  layerVisibleRoomOtherEntities: true,
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

//#region Settings
let settingsElements = {
  showRoomNames: "Show Room Names",
  rasterizeMovement: "Rasterize Movement",
  replayPlaying: "Enable Replay",
};
let layerVisibilityElements = {
  layerVisibleRoomLayout: "Room Layout",
  layerVisibleRoomEntities: "Room Entities",
  layerVisibleRoomOtherEntities: "Other Entities",
  layerVisibleTooltip: "Tooltip",
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
function loadSettings() {
  let settingsStr = localStorage.getItem("settings");
  if (settingsStr !== null) {
    settings = JSON.parse(settingsStr);
  }

  settings.frameMin = 0;

  for (const key in settingsElements) {
    if (settingsElements.hasOwnProperty(key)) {
      const label = settingsElements[key];
      let div = createSettingsCheckbox(key, label, settings[key], changedBoolSetting);
      Elements.OptionsContainer.appendChild(div);
    }
  }

  let skipRows = 3;
  let combinedRows = 0;
  let storedKey = null;
  for (const key in layerVisibilityElements) {
    if (!layerVisibilityElements.hasOwnProperty(key)) {
      return;
    }

    if (combinedRows < 1 && storedKey == null && skipRows <= 0) {
      storedKey = key;
      continue;
    } else if (storedKey != null) {
      const storedLabel = layerVisibilityElements[storedKey];
      let storedDiv = createSettingsCheckbox(storedKey, storedLabel, settings[key], changedLayerVisibility);
      storedDiv.style.width = "50%";

      const label = layerVisibilityElements[key];
      let div = createSettingsCheckbox(key, label, settings[key], changedLayerVisibility);
      div.style.width = "50%";

      //Combine the two
      let container = document.createElement("div");
      container.classList.add("flex-center");
      container.appendChild(storedDiv);
      container.appendChild(div);
      container.style.justifyContent = "start";
      Elements.LayersContainer.appendChild(container);
    } else {
      skipRows--;

      const label = layerVisibilityElements[key];
      let div = createSettingsCheckbox(key, label, settings[key], changedLayerVisibility);
      Elements.LayersContainer.appendChild(div);
    }
  }

  combinedRows = 0;
  storedKey = null;
  for (const key in tooltipsInfoElements) {
    if (!tooltipsInfoElements.hasOwnProperty(key)) {
      continue;
    }

    if (combinedRows < 5 && storedKey == null) {
      storedKey = key;
      continue;
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

  if (settings.menuHidden) {
    refreshSidebarMenuVisibility();
  }

  //Add PointLabelNames to the combobox in Elements.PointLabels
  for (const key in PointLabels) {
    if (PointLabels.hasOwnProperty(key)) {
      const label = PointLabels[key];
      let option = document.createElement("option");
      option.value = key;
      option.text = label;
      Elements.PointLabels.appendChild(option);
    }
  }

  Elements.PointLabels.value = settings.pointLabels;
  Elements.FrameStepSize.value = settings.frameStepSize+"";
  
  //Fix the initial state of newly added settings
  if(settings.replayPlaying === undefined){
    settings.replayPlaying = false;
  }
  if(settings.replaySpeed === undefined){
    settings.replaySpeed = 1;
  }
  
  settings.replaySpeed = 1;
  

  settingsInited = true;
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

function changedBoolSetting(settingName, value) {
  settings[settingName] = value;
  saveSettings();
  redrawCanvas();
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
    case "layerVisibleRoomOtherEntities":
      layerToSet = konvaRoomOtherEntitiesLayer;
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

  if (value) {
    layerToSet.show();
  } else {
    layerToSet.hide();
  }

  if (layerName === "layerVisibleTooltip") {
    if (value) {
      konvaLowPrioTooltipLayer.show();
    } else {
      konvaLowPrioTooltipLayer.hide();
    }
  }

  settings[layerName] = value;

  saveSettings();
  redrawCanvas();
}
function changedTooltipInfo(settingName, value) {
  settings.tooltipInfo[settingName] = value;
  saveSettings();
  redrawCanvas();
}
function changedPointLabel(pointLabel) {
  if (!settingsInited) return;

  settings.pointLabels = pointLabel;
  saveSettings();
  redrawCanvas();
}

function saveSettings() {
  localStorage.setItem("settings", JSON.stringify(settings));
}

function applySettings() {
  if (!settings.layerVisibleRoomLayout) {
    konvaRoomLayoutLayer.hide();
  }
  if (!settings.layerVisibleRoomEntities) {
    konvaRoomEntitiesLayer.hide();
  }
  if (!settings.layerVisibleRoomOtherEntities) {
    konvaRoomOtherEntitiesLayer.hide();
  }
  if (!settings.layerVisibleMaddyHitbox) {
    konvaMaddyHitboxLayer.hide();
  }
  if (!settings.layerVisibleTooltip) {
    konvaTooltipLayer.hide();
    konvaLowPrioTooltipLayer.hide();
  }
  if (!settings.layerVisiblePosition) {
    konvaPositionLayer.hide();
  }
  
  if(roomLayoutRecording.usesMovableEntities && settings.frameStepSize === 1){
    konvaRoomMovableEntitiesInitialLayer.visible(false);
  } else {
    konvaRoomMovableEntitiesInitialLayer.visible(true);
  }
}

//#region Button Actions
function changedFrameStepSize(value){
  settings.frameStepSize = parseInt(value);
  
  if(settings.frameStepSize === 1 && roomLayoutRecording.usesMovableEntities){
    konvaRoomMovableEntitiesInitialLayer.visible(false);
  } else {
    konvaRoomMovableEntitiesInitialLayer.visible(true);
  }
  
  saveSettings();
  updateRecordingInfo();
  redrawCanvas();
}

function framePageUp(mult = 1, event) {
  let stepSize = event.ctrlKey ? 1 : event.shiftKey ? settings.frameStepSize * 5 : settings.frameStepSize;
  if (settings.frameMin == -1) {
    settings.frameMin = stepSize * (mult - 1);
  } else {
    settings.frameMin += stepSize * mult;
  }

  if (settings.frameMin < 0) {
    settings.frameMin = 0;
  }
  
  const centerFrame = physicsLogFrames[Math.min(settings.frameMin + Math.floor(settings.frameStepSize / 2), physicsLogFrames.length - 1)];

  updateRecordingInfo();
  redrawCanvas();
  if(!areModelCoordinatesOnScreen(centerFrame.positionX, centerFrame.positionY)){
    centerOnPositionReal(centerFrame.positionX, centerFrame.positionY);
  }
}
function frameEnd(event) {
  if(event.ctrlKey){
    settings.frameMin = 0;
  } else {
    settings.frameMin = physicsLogFrames.length - settings.frameStepSize;
  }
  
  const centerFrame = physicsLogFrames[Math.min(settings.frameMin + Math.floor(settings.frameStepSize / 2), physicsLogFrames.length - 1)];

  updateRecordingInfo();
  redrawCanvas();
  centerOnPositionReal(centerFrame.positionX, centerFrame.positionY);
}
function resetFramePage() {
  settings.frameMin = 0;
  updateRecordingInfo();
}
function updateFrameButtonStates() {
  if (settings.frameMin == 0) {
    Elements.PreviousFramesButton.setAttribute("disabled", true);
  } else {
    Elements.PreviousFramesButton.removeAttribute("disabled");
  }
  if (settings.frameMin + settings.frameStepSize >= physicsLogFrames.length) {
    Elements.NextFramesButton.setAttribute("disabled", true);
    Elements.FinalFramesButton.setAttribute("disabled", true);
  } else {
    Elements.NextFramesButton.removeAttribute("disabled");
    Elements.FinalFramesButton.removeAttribute("disabled");
  }
}

function ChangeRecording(selected) {
  //selected is in the form of "recent-0" or "saved-0"
  let split = selected.split("-");
  let selectedRecordingTypeStr = split[0];
  let selectedRecordingId = parseInt(split[1]);

  selectedRecordingType = selectedRecordingTypeStr == "recent" ? RecordingTypes.Recent : RecordingTypes.Saved;
  selectedRecording = selectedRecordingId;

  updateRecordingActionButtonStates();

  console.log("ChangeRecording -> Selected: ", selected);

  fetchRoomLayout(afterFetchRoomLayout);
  resetFramePage();
  if (roomLayoutRecording.usesMovableEntities && settings.frameStepSize === 1){
    konvaRoomMovableEntitiesInitialLayer.visible(false);
  } else {
    konvaRoomMovableEntitiesInitialLayer.visible(true);
  }
}
function showSavedRecording(id) {
  selectedRecording = id;
  selectedRecordingType = RecordingTypes.Saved;
  fetchRoomLayout(afterFetchRoomLayout);
  resetFramePage();
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

function toggleSidebarMenuSetting() {
  settings.menuHidden = !settings.menuHidden;
  refreshSidebarMenuVisibility();
  saveSettings();
}
function refreshSidebarMenuVisibility() {
  //Slide the menu out to the left by setting transform: translateX(-100%)
  if (settings.menuHidden) {
    Elements.SidebarMenu.style.transform = "translateX(-100%)";
  } else {
    Elements.SidebarMenu.style.transform = "translateX(0%)";
  }
}
//#endregion

//#endregion
