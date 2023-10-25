
const ViewStates = {
    MainView: 0,
    InspectorView: 1,
};
let CurrentState = null;

const RecordingTypes = {
    Recent: 0,
    Saved: 1,
};

const PointLabels = {
    None: "None",

    DragX: "DragX",
    DragY: "DragY",

    PositionX: "PositionX",
    PositionY: "PositionY",
    PositionCombined: "PositionCombined",
    SpeedX: "SpeedX",
    SpeedY: "SpeedY",
    SpeedCombined: "SpeedCombined",
    AccelerationX: "AccelerationX",
    AccelerationY: "AccelerationY",
    AccelerationCombined: "AccelerationCombined",
    AbsoluteSpeed: "AbsoluteSpeed",
    VelocityX: "VelocityX",
    VelocityY: "VelocityY",
    VelocityCombined: "VelocityCombined",
    VelocityDifferenceX: "VelocityDifferenceX",
    VelocityDifferenceY: "VelocityDifferenceY",
    VelocityDifferenceCombined: "VelocityDifferenceCombined",
    LiftBoostX: "LiftBoostX",
    LiftBoostY: "LiftBoostY",
    LiftBoostCombined: "LiftBoostCombined",
    RetainedSpeed: "RetainedSpeed",
    Stamina: "Stamina",
    Inputs: "Inputs",
};
const PointLabelNames = {
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

const Elements = {
    MainContainer: "main-view",
    LoadingText: "loading-text",

    InspectorContainer: "inspector-view",
    CanvasContainer: "canvas-container",
    SidebarMenu: "sidebar-menu",

    NextFramesButton: "next-frames-button",
    PreviousFramesButton: "previous-frames-button",
    FinalFramesButton: "final-frames-button",

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
    PointLabels: "point-labels",
    LayersContainer: "layers-container",
    TooltipsContainer: "tooltips-container",


    OtherToolsButton: "other-tools-button",
    OtherToolsModal: "other-tools-modal",
    EntitiesCountModal: "entities-count-modal",
    OtherEntitiesModal: "other-entities-modal",
};

const apiBaseUrl = "http://localhost:32270/cct";

//#region Properties

let isOffline = false;
let isRecording = false;
let recentPhysicsLogFilesList = null;
let savedPhysicsRecordingsList = null;
let roomLayouts = null; //Array
let physicsLogFrames = null; //Array

let konvaStage = null;
let konvaRoomLayoutLayer = null;
let konvaRoomEntitiesLayer = null;
let konvaRoomOtherEntitiesLayer = null;
let konvaMaddyHitboxLayer = null;
let konvaTooltipLayer = null;
let konvaLowPrioTooltipLayer = null;
let konvaPositionLayer = null;

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

    frameStepSize: 1000,
    frameMin: 0,
    frameMax: 1000,

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
    }
};
//#endregion


//#region Startup
document.addEventListener("DOMContentLoaded", function () {
    loadElements(Elements);
    loadSettings();
    ShowState(ViewStates.MainView);
});


//#region Settings
let settingsElements = {
    showRoomNames: "Show Room Names",
    showSpinnerRectangle: "Show Spinner Rectangle",
    showOnlyRelevantRooms: "Show Only Relevant Rooms",
    rasterizeMovement: "Rasterize Movement",
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
};

let settingsInited = false;
function loadSettings(){
    let settingsStr = localStorage.getItem("settings");
    if(settingsStr !== null){
        settings = JSON.parse(settingsStr);
    }

    settings.frameMin = 0;
    settings.frameMax = 1000;

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

        if(combinedRows < 1 && storedKey == null && skipRows <= 0){
            storedKey = key;
            continue;
        } else if (storedKey != null){
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

        if(combinedRows < 5 && storedKey == null){
            storedKey = key;
            continue;
        } else if (storedKey != null){
            const storedLabel = tooltipsInfoElements[storedKey];
            let storedDiv = createSettingsCheckbox(storedKey, storedLabel, settings.tooltipInfo[storedKey], changedTooltipInfo);
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

    if(settings.menuHidden){
        refreshSidebarMenuVisibility();
    }

    //Add PointLabelNames to the combobox in Elements.PointLabels
    for (const key in PointLabelNames) {
        if (PointLabels.hasOwnProperty(key)) {
            const label = PointLabelNames[key];
            let option = document.createElement("option");
            option.value = key;
            option.text = label;
            Elements.PointLabels.appendChild(option);
        }
    }

    Elements.PointLabels.value = settings.pointLabels;

    settingsInited = true;
}

function createSettingsCheckbox(settingName, settingLabel, currentValue, onChangeFunction){
    let checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.id = "check-"+settingName;
    checkbox.checked = currentValue;
    checkbox.onchange = function(){
        onChangeFunction(settingName, this.checked);
    };

    let label = document.createElement("label");
    label.htmlFor = "check-"+settingName;
    label.appendChild(document.createTextNode(settingLabel));

    let div = document.createElement("div");
    div.classList.add("flex-center");
    div.classList.add("flex-start");
    div.appendChild(checkbox);
    div.appendChild(label);

    return div;
}

function changedBoolSetting(settingName, value){
    settings[settingName] = value;
    saveSettings();
    redrawCanvas();
}

function changedLayerVisibility(layerName, value){
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

    if(value){
        layerToSet.show();
    } else {
        layerToSet.hide();
    }

    if(layerName === "layerVisibleTooltip"){
        if(value){
            konvaLowPrioTooltipLayer.show();
        } else {
            konvaLowPrioTooltipLayer.hide();
        }
    }

    settings[layerName] = value;

    saveSettings();
    redrawCanvas();
}
function changedTooltipInfo(settingName, value){
    settings.tooltipInfo[settingName] = value;
    saveSettings();
    redrawCanvas();
}
function changedPointLabel(pointLabel){
    if(!settingsInited) return;

    settings.pointLabels = pointLabel;
    saveSettings();
    redrawCanvas();
}

function saveSettings(){
    localStorage.setItem("settings", JSON.stringify(settings));
}


function applySettings(){
    if(!settings.layerVisibleRoomLayout){
        konvaRoomLayoutLayer.hide();
    }
    if(!settings.layerVisibleRoomEntities){
        konvaRoomEntitiesLayer.hide();
    }
    if(!settings.layerVisibleRoomOtherEntities){
        konvaRoomOtherEntitiesLayer.hide();
    }
    if(!settings.layerVisibleMaddyHitbox){
        konvaMaddyHitboxLayer.hide();
    }
    if(!settings.layerVisibleTooltip){
        konvaTooltipLayer.hide();
        konvaLowPrioTooltipLayer.hide();
    }
    if(!settings.layerVisiblePosition){
        konvaPositionLayer.hide();
    }
}

//#region Button Actions
function framePageUp(mult=1){
    if(settings.frameMin == -1){
        settings.frameMin = 1000*(mult-1);
        settings.frameMax = 1000*mult;
    } else {
        settings.frameMin += 1000*mult;
        settings.frameMax += 1000*mult;
    }

    if(settings.frameMin < 0){
        settings.frameMin = 0;
        settings.frameMax = 1000;
    }

    updateRecordingInfo();
    redrawCanvas();
}
function frameEnd() {
    settings.frameMin = roomLayoutRecording.frameCount - 1000;
    settings.frameMax = roomLayoutRecording.frameCount;

    updateRecordingInfo();
    redrawCanvas();
}
function resetFramePage(){
    settings.frameMin = 0;
    settings.frameMax = 1000;
    updateRecordingInfo();
}
function updateFrameButtonStates(){
    if(settings.frameMin == 0){
        Elements.PreviousFramesButton.setAttribute("disabled", true);
    } else {
        Elements.PreviousFramesButton.removeAttribute("disabled");
    }
    if(settings.frameMax >= roomLayoutRecording.frameCount){
        Elements.NextFramesButton.setAttribute("disabled", true);
        Elements.FinalFramesButton.setAttribute("disabled", true);
    } else {
        Elements.NextFramesButton.removeAttribute("disabled");
        Elements.FinalFramesButton.removeAttribute("disabled");
    }
}

function ChangeRecording(selected){
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
}
function showSavedRecording(id){
    selectedRecording = id;
    selectedRecordingType = RecordingTypes.Saved;
    fetchRoomLayout(afterFetchRoomLayout);
    resetFramePage();
}
function updateRecordingActionButtonStates(){
    if(selectedRecordingType === RecordingTypes.Recent){
        Elements.SaveRecordingButton.removeAttribute("disabled");
        Elements.RenameRecordingButton.setAttribute("disabled", true);
        Elements.DeleteRecordingButton.setAttribute("disabled", true);
    } else {
        Elements.SaveRecordingButton.setAttribute("disabled", true);
        Elements.RenameRecordingButton.removeAttribute("disabled");
        Elements.DeleteRecordingButton.removeAttribute("disabled");
    }
}

function toggleSidebarMenuSetting(){
    settings.menuHidden = !settings.menuHidden;
    refreshSidebarMenuVisibility();
    saveSettings();
}
function refreshSidebarMenuVisibility(){
    //Slide the menu out to the left by setting transform: translateX(-100%)
    if(settings.menuHidden){
        Elements.SidebarMenu.style.transform = "translateX(-100%)";
    } else {
        Elements.SidebarMenu.style.transform = "translateX(0%)";
    }
}
//#endregion

//#endregion





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

function showError(errorCode, errorMessage){
    let message = "Error ("+errorCode+"): "+errorMessage;
    console.log(message);

    if(CurrentState === ViewStates.MainView){
        Elements.LoadingText.innerText = message;
    }
}
//#endregion


//#region MainView
function OnShowMainView() {
    fetchPhysicsLogFileList(afterFetchPhysicsLogFileList);
}

function performRequest(url, then, errorMessage, errorFunction=null){
    fetch(url)
    .then(response => response.json())
    .then(responseObj => {
        then(responseObj);
    })
    .catch(error => {
        showError(-1, errorMessage);
        console.error(error);
        if(errorFunction !== null){
            errorFunction();
        }
    });
}

function fetchPhysicsLogFileList(then){
    let url = apiBaseUrl + "/getPhysicsLogList";
    function afterFetch(responseObj){
        console.log(responseObj);
        if(responseObj.errorCode !== 0){
            showError(responseObj.errorCode, responseObj.errorMessage);
            return;
        }

        recentPhysicsLogFilesList = responseObj.recentPhysicsLogFiles;
        savedPhysicsRecordingsList = responseObj.savedPhysicsRecordings;
        isRecording = responseObj.isRecording;

        if(recentPhysicsLogFilesList.length > 0){
            selectedRecording = 0;
            if(isRecording){
                selectedRecording = 1;
            }
            selectedRecordingType = RecordingTypes.Recent;
        } else if(recentPhysicsLogFilesList.length === 0 && savedPhysicsRecordingsList.length > 0){
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
    function onError(){
        // isOffline = true;
        // OnShowMainView();
    }

    performRequest(url, afterFetch, "Failed to fetch physics log file list (is CCT running?)", onError);
}

function showRecordingList(){
    //Clear the optgroups
    Elements.OptgroupRecent.innerHTML = "";
    Elements.OptgroupSaved.innerHTML = "";

    //Add recent recordings
    if(isRecording){
        let opt = document.createElement("option");
        opt.value = "recent-"+0;
        opt.disabled = true;
        opt.innerText = "(1) Recording in progress...";
        Elements.OptgroupRecent.appendChild(opt);
    }

    for(let i = 0; i < recentPhysicsLogFilesList.length; i++){
        let recording = recentPhysicsLogFilesList[i];
        let id = recording.id;

        let opt = document.createElement("option");
        opt.value = "recent-"+id;
        opt.innerText = getRecordingDisplayName(RecordingTypes.Recent, recording);
        Elements.OptgroupRecent.appendChild(opt);
    }

    //Add saved recordings
    for(let i = 0; i < savedPhysicsRecordingsList.length; i++){
        let recording = savedPhysicsRecordingsList[i];
        let id = recording.id;
        let opt = document.createElement("option");
        opt.value = "saved-"+id;
        opt.innerText = getRecordingDisplayName(RecordingTypes.Saved, recording);
        Elements.OptgroupSaved.appendChild(opt);
    }
}

function getRecordingDisplayNameByID(recordingType, recordingID){
    let toSearch = recordingType === RecordingTypes.Recent ? recentPhysicsLogFilesList : savedPhysicsRecordingsList;
    for(let i = 0; i < toSearch.length; i++){
        let recording = toSearch[i];
        if(recording.id === recordingID){
            return getRecordingDisplayName(recordingType, recording);
        }
    }
}
function getRecordingDisplayName(recordingType, recordingObj){
    if(recordingType === RecordingTypes.Recent){
        let chapterName = recordingObj.chapterName;
        let sideName = recordingObj.sideName;
        let recordingStarted = new Date(recordingObj.recordingStarted);
        let frameCount = recordingObj.frameCount;
        let id = recordingObj.id;

        //format the date in: "day/month hour:minute", zero padded using the function zeroPad(number, zeros)
        let date = zeroPad(recordingStarted.getDate(), 2)+"/"+zeroPad(recordingStarted.getMonth()+1, 2)+" "+zeroPad(recordingStarted.getHours(), 2)+":"+zeroPad(recordingStarted.getMinutes(), 2);
        let mapName = sideName === "A-Side" ? chapterName : chapterName+" ["+sideName[0]+"]";

        return "("+(id+1)+") "+mapName+" - "+formatBigNumber(frameCount)+"f";

    } else {
        let chapterName = recordingObj.chapterName;
        let sideName = recordingObj.sideName;
        let recordingStarted = new Date(recordingObj.recordingStarted);
        let frameCount = recordingObj.frameCount;
        let id = recordingObj.id;
        let name = recordingObj.name;

        let date = zeroPad(recordingStarted.getDate(), 2)+"/"+zeroPad(recordingStarted.getMonth()+1, 2)+" "+zeroPad(recordingStarted.getHours(), 2)+":"+zeroPad(recordingStarted.getMinutes(), 2);
        let mapName = sideName === "A-Side" ? chapterName : chapterName+" ["+sideName[0]+"]";

        return name+": "+mapName+" - "+formatBigNumber(frameCount)+"f";
    }
}

function afterFetchPhysicsLogFileList(){
    fetchRoomLayout(afterFetchRoomLayout);
}

function fetchRoomLayout(then){
    let subfolderName = selectedRecordingType === RecordingTypes.Recent ? "recent-recordings" : "saved-recordings";

    let url = apiBaseUrl + "/getFileContent?folder=physics-recordings&subfolder="+subfolderName+"&file="+selectedRecording+"_room-layout&extension=json";
    function afterFetch(responseObj){
        console.log(responseObj);
        if(responseObj.errorCode !== 0){
            showError(responseObj.errorCode, responseObj.errorMessage);
            return;
        }
        let fileContentStr = responseObj.fileContent;

        roomLayoutRecording = JSON.parse(fileContentStr);
        roomLayouts = roomLayoutRecording.rooms;

        //Find the id selectedRecording in the list of savedPhysicsRecordingsList
        if(selectedRecordingType === RecordingTypes.Saved){
            let recording = null;
            for(let i = 0; i < savedPhysicsRecordingsList.length; i++){
                if(savedPhysicsRecordingsList[i].id === selectedRecording){
                    recording = savedPhysicsRecordingsList[i];
                    break;
                }
            }
            if(recording !== null){
                roomLayoutRecording.name = recording.name;
            }
        }

        then();
    }
    
    performRequest(url, afterFetch, "Failed to fetch room layout (is CCT running?)");
}
function afterFetchRoomLayout(){
    fetchPhysicsLog(goToInspectorView);
}

function fetchPhysicsLog(then){
    let subfolderName = selectedRecordingType === RecordingTypes.Recent ? "recent-recordings" : "saved-recordings";

    let url = apiBaseUrl + "/getFileContent?folder=physics-recordings&subfolder="+subfolderName+"&file="+selectedRecording+"_position-log&extension=txt";
    function afterFetch(responseObj){
        console.log(responseObj);
        if(responseObj.errorCode !== 0){
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
function goToInspectorView(){
    ShowState(ViewStates.InspectorView);
}

function parsePhysicsLogFrames(fileContent){
    let lines = fileContent.split("\n");
    
    //First line can be the header
    let header = lines[0];
    let beginIndex = header.indexOf("Frame") !== -1 ? 1 : 0;

    //All other lines have the format:
    //FrameNumber, PositionX, PositionY, SpeedX, SpeedY, VelocityX, VelocityY, LiftBoostX, LiftBoostY, Flags, Inputs
    let frames = [];
    for (let i = beginIndex; i < lines.length; i++) {
        const line = lines[i].trim();
        if(line.length === 0) continue;
        let frame = parsePhysicsLogFrame(line);
        frames.push(frame);
    }

    return frames;
}

function filterPhysicsLogFrames(){
    //Filter out the consecutive frames with the same position
    let filteredFrames = [];
    let lastFrame = null;
    for (let i = 0; i < physicsLogFrames.length; i++) {
        const frame = physicsLogFrames[i];
        if(lastFrame === null){
            filteredFrames.push(frame);
        }else{
            if(lastFrame.positionX !== frame.positionX || lastFrame.positionY !== frame.positionY){
                filteredFrames.push(frame);
            }
        }
        lastFrame = frame;
    }
    physicsLogFrames = filteredFrames;
}

function parsePhysicsLogFrame(line){
    let values = line.split(",");
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
    };
    return frame;
}
//#endregion


//#region EditView
function OnShowInspectorView() {
    //Display data here
    createCanvas();
    createLayers();
    addMouseHandlers();

    createAllElements();

    //Sidemenu things
    updateRecordingInfo();

    let firstFrame = physicsLogFrames[0];

    let firstRoomBounds = roomLayouts[0].levelBounds;
    let centerX = firstRoomBounds.x + firstRoomBounds.width / 2;
    let centerY = firstRoomBounds.y + firstRoomBounds.height / 2;

    //If the distance of firstFrame position to center of first room is greater than 1000, then center on firstFrame position
    let distanceToCenter = Math.sqrt(Math.pow(centerX - firstFrame.positionX, 2) + Math.pow(centerY - firstFrame.positionY, 2));
    if(distanceToCenter > 1000){
        centerX = firstFrame.positionX;
        centerY = firstFrame.positionY;
    }

    centerOnPosition(centerX, centerY);
    applySettings();
}

function redrawCanvas(){
    //Clear konva layers
    konvaRoomLayoutLayer.destroyChildren();
    konvaRoomEntitiesLayer.destroyChildren();
    konvaRoomOtherEntitiesLayer.destroyChildren();
    konvaMaddyHitboxLayer.destroyChildren();
    konvaPositionLayer.destroyChildren();
    konvaLowPrioTooltipLayer.destroyChildren();
    konvaTooltipLayer.destroyChildren();

    createAllElements();
}

function createCanvas(){
    // first we need to create a stage
    konvaStage = new Konva.Stage({
        container: 'canvas-container',
        width: Elements.CanvasContainer.offsetWidth,
        height: Elements.CanvasContainer.offsetHeight,
        draggable: true,
    });
}

function createLayers(){
    // then create layer
    konvaRoomLayoutLayer = new Konva.Layer({listening: false});
    konvaRoomEntitiesLayer = new Konva.Layer({listening: false});
    konvaRoomOtherEntitiesLayer = new Konva.Layer({listening: false});
    konvaMaddyHitboxLayer = new Konva.Layer({listening: false});
    konvaPositionLayer = new Konva.Layer({listening: true});
    konvaLowPrioTooltipLayer = new Konva.Layer({listening: false});
    konvaTooltipLayer = new Konva.Layer({listening: false});

    
    // add the layer to the stage
    konvaStage.add(konvaRoomLayoutLayer);
    konvaStage.add(konvaRoomEntitiesLayer);
    konvaStage.add(konvaRoomOtherEntitiesLayer);
    konvaStage.add(konvaMaddyHitboxLayer);
    konvaStage.add(konvaPositionLayer);
    konvaStage.add(konvaLowPrioTooltipLayer);
    konvaStage.add(konvaTooltipLayer);
}

function addMouseHandlers(){
    var scaleBy = 1.2;
    konvaStage.on('wheel', (e) => {
        // stop default scrolling
        e.evt.preventDefault();

        var oldScale = konvaStage.scaleX();
        var pointer = konvaStage.getPointerPosition();

        var mousePointTo = {
            x: (pointer.x - konvaStage.x()) / oldScale,
            y: (pointer.y - konvaStage.y()) / oldScale,
        };

        // how to scale? Zoom in? Or zoom out?
        let direction = e.evt.deltaY > 0 ? -1 : 1;

        // when we zoom on trackpad, e.evt.ctrlKey is true
        // in that case lets revert direction
        if (e.evt.ctrlKey) {
            direction = -direction;
        }

        var newScale = direction > 0 ? oldScale * scaleBy : oldScale / scaleBy;

        konvaStage.scale({ x: newScale, y: newScale });

        var newPos = {
            x: pointer.x - mousePointTo.x * newScale,
            y: pointer.y - mousePointTo.y * newScale,
        };
        konvaStage.position(newPos);
    });
}

function createAllElements(){
    findRelevantRooms();

    drawRoomBounds();
    drawStaticEntities();
    drawPhysicsLog();
    
    // draw the image
    konvaPositionLayer.draw();
}

let relevantRoomNames = [];
function findRelevantRooms(){
    relevantRoomNames = [];

    //Go through all frames
    for(let i = 0; i < physicsLogFrames.length; i++){
        let frame = physicsLogFrames[i];

        if(settings.frameMin != -1 && frame.frameNumber < settings.frameMin
            || settings.frameMax != -1 && frame.frameNumber > settings.frameMax){
            continue;
        }

        //Go through all roomLayouts
        for(let j = 0; j < roomLayouts.length; j++){
            //Check if frame is in the room, if yes, add room to relevantRoomNames and break
            if(relevantRoomNames.includes(roomLayouts[j].debugRoomName)){
                continue;
            }

            let roomLayout = roomLayouts[j];
            let levelBounds = roomLayout.levelBounds;

            if(frame.positionX >= levelBounds.x && frame.positionX <= levelBounds.x + levelBounds.width &&
                frame.positionY >= levelBounds.y && frame.positionY <= levelBounds.y + levelBounds.height){
                    relevantRoomNames.push(roomLayout.debugRoomName);
                    break;
            }
        }
    }
}

function drawRoomBounds(){
    let tileSize = 8;
    let tileOffsetX = 0;
    let tileOffsetY = 0.5;

    roomLayouts.forEach(roomLayout => {
        let debugRoomName = roomLayout.debugRoomName;
        
        if(settings.showOnlyRelevantRooms && !relevantRoomNames.includes(debugRoomName)){
            return;
        }

        let levelBounds = roomLayout.levelBounds;
        let solidTiles = roomLayout.solidTiles; //2d array of bools, whether tiles are solid or not

        konvaRoomLayoutLayer.add(new Konva.Rect({
            x: levelBounds.x + tileOffsetX,
            y: levelBounds.y + tileOffsetY,
            width: levelBounds.width,
            height: levelBounds.height,
            stroke: 'white',
            strokeWidth: 0.5
        }));

        if(settings.showRoomNames){
            konvaRoomLayoutLayer.add(new Konva.Text({
                x: levelBounds.x + 3,
                y: levelBounds.y + 3,
                text: debugRoomName,
                fontSize: 20,
                fontFamily: 'Renogare',
                fill: 'white',
                stroke: 'black',
                strokeWidth: 1
            }));
        }

        drawSolidTileOutlines(solidTiles, levelBounds);
    });
}

let spinnerRadius = 6;
let entitiesOffsetX = 0;
let entitiesOffsetY = 0.5;
let hitboxEntityNames = {
    "red": ["Solid", "FakeWall",
            "FloatySpaceBlock", "FancyFloatySpaceBlock", "FloatierSpaceBlock", "StarJumpBlock",
            "BounceBlock",
            "LockBlock", "ClutterDoor", "ClutterBlockBase",
            "TempleCrackedBlock",
            "InvisibleBarrier", "CustomInvisibleBarrier"],
    "white": ["Spikes", "Lightning",
              "SeekerBarrier", "CrystalBombDetonator"],
    "#666666": ["TriggerSpikes", "GroupedTriggerSpikes", "GroupedDustTriggerSpikes", "RainbowTriggerSpikes", "TimedTriggerSpikes"],
    "orange": ["JumpThru", "JumpthruPlatform", "AttachedJumpThru", "SidewaysJumpThru", "UpsideDownJumpThru",
               "Puffer", "StaticPuffer", "SpeedPreservePuffer",
               "ClutterSwitch", "HoldableBarrier"],
    "#ff4d00": ["SwapBlock", "ToggleSwapBlock", "ReskinnableSwapBlock",
                "ZipMover", "LinkedZipMover", "LinkedZipMoverNoReturn",
                "SwitchGate", "FlagSwitchGate"],
    "green": ["Spring", "CustomSpring", "DashSpring", "SpringGreen"],
    "cyan": ["FallingBlock", "GroupedFallingBlock", "RisingBlock",
             "CrumblePlatform", "CrumbleBlock", "CrumbleBlockOnTouch", "FloatyBreakBlock",
             "DashBlock", "WallBooster", "IcyFloor"],
    "yellow": ["Portal", "LightningBreakerBox", "Lookout", "CustomPlaybackWatchtower", "FlyFeather", "Key",
               "Glider", "CustomGlider", "RespawningJellyfish",
               "TheoCrystal", "CrystalBomb"],
    "#C0C0C0": ["SilverBerry"],
    "black": ["DreamBlock", "DreamMoveBlock", "CustomDreamBlock", "CustomDreamBlockV2", "DashThroughSpikes", "ConnectedDreamBlock"],
    "blue": ["TouchSwitch", "MovingTouchSwitch", "FlagTouchSwitch", "CrushBlock"],
    "#85e340": ["DashSwitch", "TempleGate"],
    "#a200ff": ["MoveBlock", "ConnectedMoveBlock", "VitMoveBlock"],
    "#fa7ded": ["BouncySpikes"],
    "Special": ["Strawberry", "Refill", "RefillWall", "Cloud", "CassetteBlock", "WonkyCassetteBlock"],
};
let hitcircleEntityNames = {
    "#0f58d9": ["Bumper", "StaticBumper"],
    "white": ["Shield"],
    "#33c3ff": ["BlueBooster"],
    "Special": ["Booster", "VortexBumper"],
};
let specialEntityColorFunctions = {
    "Strawberry": (entity) => { return entity.properties.golden ? "#ffd700" : "#bb0000"; },
    "Refill": (entity) => { return entity.properties.twoDashes ? "#fa7ded" : "#aedc5e"; },
    "RefillWall": (entity) => { return entity.properties.twoDashes ? "#fa7ded" : "#aedc5e"; },
    "Cloud": (entity) => { return entity.properties.fragile ? "#ffa5ff" : "#77cde3"; },
    "Booster": (entity) => { return entity.properties.red ? "#d3170a" : "#219974"; },
    "CassetteBlock": (entity) => { return entity.properties.color; },
    "WonkyCassetteBlock": (entity) => { return entity.properties.color; },
    "VortexBumper": (entity) => { return entity.properties.twoDashes ? "#fa7ded" : entity.properties.oneUse ? "#d3170a" : "#0f58d9"; },
};

let entityNamesDashedOutline = {
    "FakeWall": 3.5,
    "SeekerBarrier": 0.5,
    "HoldableBarrier": 0.5,
    "CrystalBombDetonator": 0.5,
    "IcyFloor": 1,
    "WallBooster": 1,
    "Shield": 1,
    "CassetteBlock": 3,
    "WonkyCassetteBlock": 3,
    "InvisibleBarrier": 5,
    "CustomInvisibleBarrier": 5,
};

let entityCounts = {};
function drawStaticEntities(){
    entityCounts = {};
    roomLayouts.forEach(roomLayout => {
        let debugRoomName = roomLayout.debugRoomName;

        if(settings.showOnlyRelevantRooms && !relevantRoomNames.includes(debugRoomName)){
            return;
        }

        let levelBounds = roomLayout.levelBounds;
        let entities = roomLayout.entities;

        entities.forEach(entity => {
            let entityX = entity.position.x + entitiesOffsetX;
            let entityY = entity.position.y + entitiesOffsetY;

            if(entity.type === "CrystalStaticSpinner" || entity.type === "DustStaticSpinner" || entity.type === "CustomSpinner"){
                //Cull offscreen spinners
                if(entityX < levelBounds.x - spinnerRadius || entityX > levelBounds.x + levelBounds.width + spinnerRadius ||
                    entityY < levelBounds.y - spinnerRadius || entityY > levelBounds.y + levelBounds.height + spinnerRadius){
                    return;
                }

                //Draw white circle with width 6 on entity position
                let hitcircle = { x:0, y:0, radius:6 };
                konvaRoomEntitiesLayer.add(createHitCircle(entityX, entityY, hitcircle, 'white', 0.5));

                if(settings.showSpinnerRectangle){
                    //Draw white rectangle with width 16, height 4, x offset -8 and y offset -3 on entity position
                    let hitbox = { x:-8, y:-3, width:16, height:4 };
                    konvaRoomEntitiesLayer.add(createHitbox(entityX, entityY, hitbox, 'white', 0.5));
                }
            }
        });

        entities.forEach(entity => {
            //add type to entityCounts
            if(entityCounts[entity.type] === undefined){
                entityCounts[entity.type] = 0;
            }
            entityCounts[entity.type]++;

            let entityX = entity.position.x + entitiesOffsetX;
            let entityY = entity.position.y + entitiesOffsetY;

            //If the entity type is in any of the value arrays in the hitboxEntityNames map
            let entityColor = Object.keys(hitboxEntityNames).find(color => hitboxEntityNames[color].includes(entity.type));
            if(entityColor !== undefined){
                let hitbox = entity.properties.hitbox;

                if(entityColor === "Special"){
                    entityColor = specialEntityColorFunctions[entity.type](entity);
                }

                let dash = [];
                if(entity.type in entityNamesDashedOutline){
                    let dashValue = entityNamesDashedOutline[entity.type];
                    dash = [dashValue, dashValue];
                }

                //Draw hitbox
                konvaRoomEntitiesLayer.add(createHitbox(entityX, entityY, hitbox, entityColor, 0.25, dash));
                drawSimpleHitboxAdditionalShape(entityColor, entityX, entityY, entity);
            }
            
            //If the entity type is in any of the value arrays in the hitcircleEntityNames map
            entityColor = Object.keys(hitcircleEntityNames).find(color => hitcircleEntityNames[color].includes(entity.type));
            if(entityColor !== undefined){
                let hitcircle = entity.properties.hitcircle;
                
                if(entityColor === "Special"){
                    entityColor = specialEntityColorFunctions[entity.type](entity);
                }

                let dash = [];
                if(entity.type in entityNamesDashedOutline){
                    let dashValue = entityNamesDashedOutline[entity.type];
                    dash = [dashValue, dashValue];
                }

                //Draw hitcircle
                konvaRoomEntitiesLayer.add(createHitCircle(entityX, entityY, hitcircle, entityColor, 0.25, dash));
                drawSimpleHitcircleAdditionalShape(entityColor, entityX, entityY, entity);
            }

            //Entity Type: FinalBoos
            if(entity.type === "FinalBoss" || entity.type === "BadelineBoost" || entity.type === "FlingBird"){
                let hitcircle = entity.properties.hitcircle;
                let color = entity.type === "FlingBird" ? "cyan" : "#ff00ff";

                //draw the initial position
                konvaRoomEntitiesLayer.add(createHitCircle(entityX, entityY, hitcircle, color));

                //loop through properties.nodes, and draw the circle at each node, and a line between each node
                let nodes = entity.properties.nodes;
                let previousNode = null;
                for(let i = 0; i < nodes.length; i++){
                    if(entity.type === "FinalBoss" && i === nodes.length-1) continue;

                    let node = nodes[i];
                    let nodeX = node.x + hitcircle.x;
                    let nodeY = node.y + hitcircle.y;

                    //Draw circle on node position
                    konvaRoomEntitiesLayer.add(createHitCircle(node.x, node.y, hitcircle, color));

                    //Draw arrow to previous node
                    if(previousNode !== null){
                        konvaRoomEntitiesLayer.add(new Konva.Arrow({
                            points: [previousNode.x, previousNode.y + hitcircle.y, nodeX, nodeY],
                            pointerLength: 2,
                            pointerWidth: 2,
                            fill: color,
                            stroke: color,
                            strokeWidth: 0.25,
                            lineJoin: 'round'
                        }));
                    } else {
                        //Draw arrow from initial position to first node
                        konvaRoomEntitiesLayer.add(new Konva.Arrow({
                            points: [entityX + hitcircle.x, entityY + hitcircle.y, nodeX, nodeY],
                            pointerLength: 2,
                            pointerWidth: 2,
                            fill: color,
                            stroke: color,
                            strokeWidth: 0.25,
                            lineJoin: 'round'
                        }));
                    }

                    previousNode = node;
                }
            }
        });


        
        let otherEntities = roomLayout.otherEntities;
        otherEntities.forEach(entity => {
            if(!entity.properties.hasCollider) return;

            let entityColor = entity.properties.isSolid ? "red" : "white";
            let entityX = entity.position.x + entitiesOffsetX;
            let entityY = entity.position.y + entitiesOffsetY;

            let properties = entity.properties;

            let textSplit = entity.type.split(/(?=[A-Z])/);
            let maxWidth = 0;
            //find the longest string in the array
            textSplit.forEach(text => { if(text.length > maxWidth){maxWidth = text.length;} });
            let maxDim = Math.max(textSplit.length, maxWidth);
            let size = 1 / maxDim;
            let text = textSplit.join("\n");

            //Draw hitbox
            if("hitbox" in properties){
                konvaRoomOtherEntitiesLayer.add(createHitbox(entityX, entityY, properties.hitbox, entityColor));
                //Text
                let fontSize = Math.min(properties.hitbox.width, properties.hitbox.height) * size;
                let offsetY = fontSize * 0.1;
                konvaRoomOtherEntitiesLayer.add(createLetterEntityText(entityX, entityY + offsetY, properties.hitbox, text, fontSize, entityColor));
            }

            //Draw hitcircle
            if("hitcircle" in properties){
                konvaRoomOtherEntitiesLayer.add(createHitCircle(entityX, entityY, properties.hitcircle, entityColor));
                //Text
                let fontSize = properties.hitcircle.radius*2 * size * 0.8;
                let offsetY = fontSize * 0.1;
                konvaRoomOtherEntitiesLayer.add(createLetterEntityTextCircle(entityX, entityY + offsetY, properties.hitcircle, text, fontSize, entityColor));
            }

            //Draw colliderList
            if("colliderList" in properties){
                properties.colliderList.forEach(collider => {
                    if(collider.type === "hitbox"){
                        konvaRoomOtherEntitiesLayer.add(createHitbox(entityX, entityY, collider.hitbox, entityColor));
                        if(entity.type.indexOf("Spinner") !== -1) return;
                        //Text
                        let fontSize = Math.min(collider.hitbox.width, collider.hitbox.height) * size;
                        let offsetY = fontSize * 0.1;
                        konvaRoomOtherEntitiesLayer.add(createLetterEntityText(entityX, entityY + offsetY, collider.hitbox, text, fontSize, entityColor));
                    } else if(collider.type === "hitcircle"){
                        konvaRoomOtherEntitiesLayer.add(createHitCircle(entityX, entityY, collider.hitcircle, entityColor));
                        //Text
                        let fontSize = collider.hitcircle.radius*2 * size * 0.8;
                        let offsetY = fontSize * 0.1;
                        konvaRoomOtherEntitiesLayer.add(createLetterEntityTextCircle(entityX, entityY + offsetY, collider.hitcircle, text, fontSize, entityColor));
                    }
                });
            }
        });
    });
}

function createHitbox(posX, posY, hitbox, entityColor, strokeWidth=0.25, dash=[]){
    return new Konva.Rect({
        x: posX + hitbox.x,
        y: posY + hitbox.y,
        width: hitbox.width,
        height: hitbox.height,
        stroke: entityColor,
        strokeWidth: strokeWidth,
        dash: dash,
    });
}

function createHitCircle(posX, posY, hitcircle, entityColor, strokeWidth=0.25, dash=[]){
    return new Konva.Circle({
        x: posX + hitcircle.x,
        y: posY + hitcircle.y,
        radius: hitcircle.radius,
        stroke: entityColor,
        strokeWidth: strokeWidth,
        dash: dash,
    });
}

//Objects that map entity names to text properties
//Text properties: fontSize multiplier and text
let entityNamesText = {
    "ZipMover": [0.8, "Z"],
    "LinkedZipMover": [0.8, "Z"],
    "LinkedZipMoverNoReturn": [0.8, "Z"],

    "SwapBlock": [0.8, "S"],
    "ToggleSwapBlock": [0.8, "S"],

    "BounceBlock": [0.2, "Core\nBlock"],
    "CrushBlock": [0.25, "Kevin"],

    "Refill": [0.2, (entity) => entity.properties.oneUse ? "" : "Refill"],
    "RefillWall": [0.25, (entity) => entity.properties.oneUse ? "" : "Refill\nWall"],

    "CrumblePlatform": [0.8, "C"],
    "CrumbleBlock": [0.8, "C"],
    "CrumbleBlockOnTouch": [0.8, "C"],
    "FallingBlock": [0.8, "F"],
    "RisingBlock": [0.8, "R"],
    "GroupedFallingBlock": [0.8, "F"],
    "DashBlock": [0.8, "D"],
    "FloatyBreakBlock": [0.2, "Floaty\nBreakBlock"],

    "DashSwitch": [0.2, "Switch"],
    "TempleGate": [0.2, "Gate"],
    "LightningBreakerBox": [0.15, "Breaker\nBox"],
    "FlyFeather": [0.8, "F"],
    "DashSpring": [0.25, "Dash\nSpring"],
    "SpringGreen": [0.225, "Green\nSpring"],
    "Cloud": [0.4, "Cloud"],
    "Puffer": [0.25, "Puffer"],
    "StaticPuffer": [0.25, "Puffer"],
    "SpeedPreservePuffer": [0.25, "Speed\nPuffer"],
    "Key": [0.4, "Key"],
    "LockBlock": [0.15, "LockBlock"],
    "ClutterDoor": [0.1, "ClutterDoor"],
    "CassetteBlock": [0.15, "Cassette"],
    "WonkyCassetteBlock": [0.15, "Cassette"],
    "TempleCrackedBlock": [0.15, "Cracked\nBlock"],

    "Glider": [0.25, "Jelly"],
    "CustomGlider": [0.25, "Jelly"],
    "RespawningJellyfish": [0.25, "Jelly"],
    "TheoCrystal": [0.25, "Theo"],
    "CrystalBomb": [0.2, "Crystal"],

    "DashThroughSpikes": [0.25, "Dash\nSpikes"],

    "Bumper": [0.2, "Bumper"],
    "StaticBumper": [0.175, "Static\nBumper"],
    "VortexBumper": [(entity) => entity.properties.oneUse ? 0.125 : 0.175, (entity) => entity.properties.oneUse ? "Vortex\nBumper\n(One Use)" : "Vortex\nBumper"],
    "Booster": [0.15, "Bubble"],
    "BlueBooster": [0.15, "Blue\nBubble"],
};

function drawSimpleHitboxAdditionalShape(entityColor, entityX, entityY, entity){
    let hitbox = entity.properties.hitbox;

    //if entity.type is in entityNamesText, use the text properties from there
    if(entity.type in entityNamesText){
        let textProperties = entityNamesText[entity.type];
        let size = textProperties[0];
        if(typeof(size) === "function"){
            size = size(entity);
        }
        let text = textProperties[1];
        if(typeof(text) === "function"){
            text = text(entity);
        }

        if(text !== ""){
            let fontSize = Math.min(hitbox.width, hitbox.height) * size;
            let offsetY = fontSize * 0.1;
    
            konvaRoomEntitiesLayer.add(createLetterEntityText(entityX, entityY + offsetY, hitbox, text, fontSize, entityColor));
        }
    }

    if(entity.type === "Strawberry" || entity.type === "SilverBerry"){
        konvaRoomEntitiesLayer.add(new Konva.Circle({
            x: entityX,
            y: entityY,
            radius: 3,
            stroke: entityColor,
            strokeWidth: 0.25,
        }));
    }
    if(["TouchSwitch", "FlagTouchSwitch", "SwitchGate", "FlagSwitchGate", "MovingTouchSwitch"].includes(entity.type)){
        let offsetX = 0, offsetY = 0;
        if(entity.type === "SwitchGate" || entity.type === "FlagSwitchGate"){
            offsetX = hitbox.width / 2;
            offsetY = hitbox.height / 2;
        }
        konvaRoomEntitiesLayer.add(new Konva.Circle({
            x: entityX + offsetX,
            y: entityY + offsetY,
            radius: 6,
            stroke: entityColor,
            strokeWidth: 1,
        }));
    }
    if(entity.type === "MovingTouchSwitch"){
        let nodes = entity.properties.nodes;
        //Draw the additional nodes
        let previousNode = null;
        nodes.forEach(node => {
            konvaRoomEntitiesLayer.add(new Konva.Circle({
                x: node.x,
                y: node.y,
                radius: 6,
                stroke: entityColor,
                strokeWidth: 1,
            }));

            //Draw an arrow between the nodes
            let drawFromX = previousNode === null ? entityX : previousNode.x;
            let drawFromY = previousNode === null ? entityY : previousNode.y;
            
            konvaRoomEntitiesLayer.add(new Konva.Arrow({
                points: [drawFromX, drawFromY, node.x, node.y],
                pointerLength: 3,
                pointerWidth: 3,
                fill: entityColor,
                stroke: entityColor,
                strokeWidth: 0.25,
            }));

            previousNode = node;
        });
    }

    if((entity.type === "Refill" || entity.type === "RefillWall") && entity.properties.oneUse === true){
        //Draw a cross on the refill
        let offset = Math.min(hitbox.width, hitbox.height) * 0.1;
        konvaRoomEntitiesLayer.add(new Konva.Line({
            points: [
                entityX + hitbox.x + offset, entityY + hitbox.y + offset,
                entityX + hitbox.x + hitbox.width - offset, entityY + hitbox.y + hitbox.height - offset
            ],
            stroke: entityColor,
            strokeWidth: 0.25,
        }));
        konvaRoomEntitiesLayer.add(new Konva.Line({
            points: [
                entityX + hitbox.x + hitbox.width - offset, entityY + hitbox.y + offset,
                entityX + hitbox.x + offset, entityY + hitbox.y + hitbox.height - offset
            ],
            stroke: entityColor,
            strokeWidth: 0.25,
        }));
    }

    
    if(entity.type === "MoveBlock" || entity.type === "ConnectedMoveBlock" || entity.type === "DreamMoveBlock"){
        let direction = entity.properties.direction; //Left, Right, Up, Down
        //Draw an arrow pointing in the direction, in center of the hitbox
        let strokeWidth = Math.min(hitbox.width, hitbox.height) * 0.2;
        let pointerSize = strokeWidth * 0.45;
        
        let offset = Math.min(hitbox.width, hitbox.height) * 0.15;
        let arrowOffset = pointerSize * 3.5;
        
        let points = [];
        if(direction === "Left"){
            points = [ entityX + hitbox.x + hitbox.width - offset, entityY + hitbox.y + hitbox.height / 2,
                entityX + hitbox.x + offset + arrowOffset, entityY + hitbox.y + hitbox.height / 2 ];
        } else if(direction === "Right"){
            points = [ entityX + hitbox.x + offset, entityY + hitbox.y + hitbox.height / 2,
                entityX + hitbox.x + hitbox.width - offset - arrowOffset, entityY + hitbox.y + hitbox.height / 2 ];
        } else if(direction === "Up"){
            points = [ entityX + hitbox.x + hitbox.width / 2, entityY + hitbox.y + hitbox.height - offset,
                entityX + hitbox.x + hitbox.width / 2, entityY + hitbox.y + offset + arrowOffset ];
        } else if(direction === "Down"){
            points = [ entityX + hitbox.x + hitbox.width / 2, entityY + hitbox.y + offset,
                entityX + hitbox.x + hitbox.width / 2, entityY + hitbox.y + hitbox.height - offset - arrowOffset ];
        }


        konvaRoomEntitiesLayer.add(new Konva.Arrow({
            points: points,
            pointerLength: pointerSize,
            pointerWidth: pointerSize,
            fill: entityColor,
            stroke: entityColor,
            strokeWidth: strokeWidth,
        }));
    }


    if(entity.type == "Puffer" || entity.type == "StaticPuffer" || entity.type == "SpeedPreservePuffer"){
        let circleRadius = 32;
        //Draw a top half circle on the entity position using svg Path
        konvaRoomEntitiesLayer.add(new Konva.Path({
            x: entityX,
            y: entityY + hitbox.y + hitbox.height,
            data: "M " + (-circleRadius) + " 0 A " + circleRadius + " " + circleRadius + " 0 0 0 " + circleRadius + " 0 Z",
            stroke: entityColor,
            strokeWidth: 0.25,
        }));
    }

    
    if(entity.type == "ClutterBlockBase" || entity.type == "ClutterSwitch"){
        let fontSize = Math.min(hitbox.width, hitbox.height) * 0.15;
        let offsetY = fontSize * 0.1;
        let clutterName;
        if(entity.properties.color === "Red"){
            clutterName = "Towels";
        } else if(entity.properties.color === "Yellow"){
            clutterName = "Boxes";
        } else if(entity.properties.color === "Green"){
            clutterName = "Books";
        }

        if(entity.type == "ClutterSwitch"){
            clutterName = "Switch:\n"+clutterName;
        }

        konvaRoomEntitiesLayer.add(createLetterEntityText(entityX, entityY + offsetY, hitbox, clutterName, fontSize, entityColor));
    }
}

function drawSimpleHitcircleAdditionalShape(entityColor, entityX, entityY, entity){
    let hitcircle = entity.properties.hitcircle;

    if(entity.type in entityNamesText){
        let textProperties = entityNamesText[entity.type];
        let size = textProperties[0];
        if(typeof(size) === "function"){
            size = size(entity);
        }
        let text = textProperties[1];
        if(typeof(text) === "function"){
            text = text(entity);
        }
        
        if(text !== ""){
            let fontSize = hitcircle.radius*2 * size;
            let offsetY = fontSize * 0.1;
    
            konvaRoomEntitiesLayer.add(createLetterEntityTextCircle(entityX, entityY + offsetY, hitcircle, text, fontSize, entityColor));
        }
    }
}

function createLetterEntityText(entityX, entityY, hitbox, text, fontSize, entityColor){
    return new Konva.Text({
        x: entityX + hitbox.x,
        y: entityY + hitbox.y,
        width: hitbox.width,
        height: hitbox.height,
        text: text,
        fontSize: fontSize,
        fontFamily: "Renogare",
        fill: entityColor,
        align: "center",
        verticalAlign: "middle",
    });
}

function createLetterEntityTextCircle(entityX, entityY, hitcircle, text, fontSize, entityColor){
    let hitbox = {
        x: hitcircle.x,
        y: hitcircle.y,
        width: hitcircle.radius * 2,
        height: hitcircle.radius * 2,
    };
    return createLetterEntityText(entityX - hitcircle.radius, entityY - hitcircle.radius, hitbox, text, fontSize, entityColor);
}

function getRasterziedPosition(frame){
    if(settings.rasterizeMovement){
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

function drawPhysicsLog(){
    let previousFrame = null;
    pointLabelPreviousValue = null;

    for(let i = 0; i < physicsLogFrames.length; i++){
        let frame = physicsLogFrames[i];

        if(settings.frameMin != -1 && frame.frameNumber < settings.frameMin) continue;
        if(settings.frameMax != -1 && frame.frameNumber > settings.frameMax) break;

        let rasterizedPos = getRasterziedPosition(frame);
        let posX = rasterizedPos.positionX;
        let posY = rasterizedPos.positionY;

        //Draw circle on position
        let posCircle = new Konva.Circle({
            x: posX,
            y: posY,
            radius: 1.25,
            fill: getFramePointColor(frame),
            stroke: 'black',
            strokeWidth: 0,
        });
        konvaPositionLayer.add(posCircle);

        let nextFrame = null;
        if(i < physicsLogFrames.length - 1){
            nextFrame = physicsLogFrames[i+1];
        }

        createPhysicsTooltip(posCircle, frame, previousFrame, nextFrame);

        drawAdditionalFrameData(frame, previousFrame);
        previousFrame = frame;
    }
}

function drawAdditionalFrameData(frame, previousFrame){
    let rasterizedPos = getRasterziedPosition(frame);
    let posX = rasterizedPos.positionX;
    let posY = rasterizedPos.positionY;

    if(settings.alwaysShowFollowLine
        || (previousFrame !== null && previousFrame.flags.includes('Dead'))
        || frame.velocityX > 20 || frame.velocityY > 20
        || frame.velocityX < -20 || frame.velocityY <-20){
        //Draw line to previous position
        if(previousFrame !== null){
            let rasterizedPreviousPos = getRasterziedPosition(previousFrame);
            konvaLowPrioTooltipLayer.add(new Konva.Line({
                points: [rasterizedPreviousPos.positionX, rasterizedPreviousPos.positionY, posX, posY],
                stroke: 'white',
                strokeWidth: 0.05,
                lineCap: 'round',
                lineJoin: 'round'
            }));
        }
    }

    if(settings.pointLabels !== PointLabels.None){
        drawPointLabels(frame, previousFrame);
    }
}

let pointLabelPreviousValue = null;
let frameDiffPointLabelFields = {
    PositionX: [(frame) => frame.positionX.toFixed(settings.decimals), false],
    PositionY: [(frame) => frame.positionY.toFixed(settings.decimals), false],
    PositionCombined: [(frame) => "("+frame.positionX.toFixed(settings.decimals)+"|"+frame.positionY.toFixed(settings.decimals)+")", false],
    SpeedX: [(frame) => frame.speedX.toFixed(settings.decimals), false],
    SpeedY: [(frame) => frame.speedY.toFixed(settings.decimals), false],
    SpeedCombined: [(frame) => "("+frame.speedX.toFixed(settings.decimals)+"|"+frame.speedY.toFixed(settings.decimals)+")", false],

    AbsoluteSpeed: [(frame) => (Math.sqrt(frame.speedX * frame.speedX + frame.speedY * frame.speedY)).toFixed(settings.decimals), false],

    VelocityX: [(frame) => frame.velocityX.toFixed(settings.decimals), false],
    VelocityY: [(frame) => frame.velocityY.toFixed(settings.decimals), false],
    VelocityCombined: [(frame) => "("+frame.velocityX.toFixed(settings.decimals)+"|"+frame.velocityY.toFixed(settings.decimals)+")", false],

    VelocityDifferenceX: [(frame) => Math.abs(frame.velocityX - frame.speedX/60) >= 0.005 ? (frame.velocityX - frame.speedX/60).toFixed(settings.decimals) : "", true],
    VelocityDifferenceY: [(frame) => Math.abs(frame.velocityY - frame.speedY/60) >= 0.005 ? (frame.velocityY - frame.speedY/60).toFixed(settings.decimals) : "", true],
    VelocityDifferenceCombined: [(frame) => Math.abs(frame.velocityX - frame.speedX/60) >= 0.005 || Math.abs(frame.velocityY - frame.speedY/60) >= 0.005 ? "("+(frame.velocityX - frame.speedX/60).toFixed(settings.decimals)+"|"+(frame.velocityY - frame.speedY/60).toFixed(settings.decimals)+")" : "", true],
    
    LiftBoostX: [(frame) => frame.liftBoostX.toFixed(settings.decimals), false],
    LiftBoostY: [(frame) => frame.liftBoostY.toFixed(settings.decimals), false],
    LiftBoostCombined: [(frame) => "("+frame.liftBoostX.toFixed(settings.decimals)+"|"+frame.liftBoostY.toFixed(settings.decimals)+")", false],
    RetainedSpeed: [(frame) => frame.speedRetention.toFixed(settings.decimals), false],
    Stamina: [(frame) => frame.stamina.toFixed(settings.decimals), false],
    Inputs: [(frame) => frame.inputs, false],
};
let frameDiffDiffPointLabelFields = {
    AccelerationX: (frame, previousFrame) => (frame.speedX - previousFrame.speedX).toFixed(settings.decimals),
    AccelerationY: (frame, previousFrame) => (frame.speedY - previousFrame.speedY).toFixed(settings.decimals),
    AccelerationCombined: (frame, previousFrame) => "("+(frame.speedX - previousFrame.speedX).toFixed(settings.decimals)+"|"+(frame.speedY - previousFrame.speedY).toFixed(settings.decimals)+")",
}
function drawPointLabels(frame, previousFrame){
    let text = "";
    
    if(settings.pointLabels === PointLabels.DragX && previousFrame !== null){
        let dragX = frame.speedX - previousFrame.speedX;
        if(dragX < -15 || dragX > 15 || dragX === 0 || previousFrame.speedX === 0 || /Retained(.)/.test(frame.flags)) {

        } else {
            text = dragX.toFixed(settings.decimals);
        }
    } else if (settings.pointLabels === PointLabels.DragY && previousFrame !== null){
        let dragY = frame.speedY - previousFrame.speedY;
        if(dragY < -16 || dragY > 16 || dragY === 0 || previousFrame.speedY === 0) {

        } else {
            text = dragY.toFixed(settings.decimals);
        }
    }

    //if settings.pointLabels is in frameDiffPointLabelFields, then add the value of that field to text
    if(settings.pointLabels in frameDiffPointLabelFields){
        let func = frameDiffPointLabelFields[settings.pointLabels][0];
        let showRepeatValues = frameDiffPointLabelFields[settings.pointLabels][1];
        let valueThisFrame = func(frame);
        if(previousFrame === null || showRepeatValues || func(previousFrame) !== valueThisFrame){
            text = valueThisFrame;
        }
    }

    //if settings.pointLabels is in frameDiffDiffPointLabelFields, then add the value of that field to text
    if(settings.pointLabels in frameDiffDiffPointLabelFields){
        if(previousFrame === null) return;
        let func = frameDiffDiffPointLabelFields[settings.pointLabels];
        let valueThisFrame = func(frame, previousFrame);
        if(valueThisFrame !== "0.00" && valueThisFrame !== "-0.00"){
            text = valueThisFrame;
        }
    }

    if(text === ""){
        return;
    }
    
    let rasterizedPos = getRasterziedPosition(frame);
    let posX = rasterizedPos.positionX;
    let posY = rasterizedPos.positionY;
    let boxWidth = 30;
    let fontSize = 1.5;
    konvaLowPrioTooltipLayer.add(new Konva.Text({
        x: posX - boxWidth/2,
        y: posY - 1.5 - fontSize,
        width: boxWidth,
        height: fontSize,
        align: 'center',
        verticalAlign: 'middle',
        text: text,
        fontSize: fontSize,
        fontFamily: 'Renogare',
        fill: 'white',
        stroke: 'black',
        strokeWidth: fontSize * 0.08,
    }));
}

function getFramePointColor(frame){
    //if flags contains StDash, then return 'red'
    if(frame.flags.includes('Dead')){
        return 'black';
    } else if(frame.flags.includes('StDash')){
        return 'red';
    } else if(frame.flags.includes('StStarFly')){
        return 'yellow';
    }

    //if inputs contains the jump input "J", then return 'green'
    if(frame.inputs.includes('J') || frame.inputs.includes('K')){
        return 'green';
    }

    //Default color is white
    return 'white';
}

function createPhysicsTooltip(shape, frame, previousFrame, nextFrame){
    let rasterizedPos = getRasterziedPosition(frame);
    let posX = rasterizedPos.positionX;
    let posY = rasterizedPos.positionY;

    let maddyWidth = 7;
    let maddyHeight = 10;

    if(frame.flags.includes('Ducking')){
        maddyHeight = 5;
    }

    //Draw maddy's hitbox as rectangle
    let maddyHitbox = new Konva.Rect({
        x: posX - maddyWidth / 2,
        y: posY - maddyHeight,
        width: maddyWidth,
        height: maddyHeight,
        stroke: 'red',
        strokeWidth: 0.125,
        visible: false
    });
    konvaMaddyHitboxLayer.add(maddyHitbox);

    //Draw maddy's hurtbox as green rectangle
    let maddyHurtbox = new Konva.Rect({
        x: posX - maddyWidth / 2,
        y: posY - maddyHeight,
        width: maddyWidth,
        height: maddyHeight - 2,
        stroke: 'green',
        strokeWidth: 0.125,
        visible: false
    });
    konvaMaddyHitboxLayer.add(maddyHurtbox);



    let tooltipMargin = 2;
    let tooltipBoxOffsetX = 5;
    let tooltipBoxOffsetY = 0 - maddyHeight - 3;

    let konvaGroupTooltip = new Konva.Group({
        x: posX + tooltipBoxOffsetX,
        y: posY + tooltipBoxOffsetY,
        visible: false,
    });
    konvaTooltipLayer.add(konvaGroupTooltip);

    let konvaGroupTooltipInfo = new Konva.Group({
        x: tooltipMargin,
        y: tooltipMargin,
    });


    
    //Create a tooltip text with additional info about the frame
    let tooltipFontSize = 2.5;
    let tooltipTextContent = formatTooltipText(frame, previousFrame, nextFrame);
    let tooltipText = new Konva.Text({
        x: 0,
        y: 0,
        text: tooltipTextContent,
        fontSize: tooltipFontSize,
        fontFamily: 'Courier New',
        fill: 'black',
        align: 'left',
    });
    konvaGroupTooltipInfo.add(tooltipText);

    //Subpixel display
    let subpixelDisplayHeight = 0;
    if(settings.tooltipInfo.subpixelDisplay){
        let subpixelPos = getSubpixelDistances(frame.positionX, frame.positionY);

        let containerWidth = tooltipText.width();
        let subpixelOffsetY = tooltipText.height() + 1;
        let squareMarginBottom = 0.5;
        let squareMultiplier = 3;

        //Place a left-aligned text saying "Subpixels:"
        // let subpixelText = new Konva.Text({
        //     x: 0,
        //     y: subpixelOffsetY,
        //     text: "Subpixels:",
        //     fontSize: tooltipFontSize,
        //     fontFamily: 'Courier New',
        //     fill: 'black',
        //     align: 'left',
        // });
        // konvaGroupTooltipInfo.add(subpixelText);

        //Place a centered text with subpixelPos.up info
        let subpixelUpText = new Konva.Text({
            x: 0,
            y: subpixelOffsetY,
            width: containerWidth,
            text: subpixelPos.up.toFixed(settings.decimals),
            fontSize: tooltipFontSize,
            fontFamily: 'Courier New',
            fill: 'black',
            align: 'center',
        });
        konvaGroupTooltipInfo.add(subpixelUpText);

        //Draw a square of size tooltipFontSize * 3 below the subpixelUpText
        let subpixelSquare = new Konva.Rect({
            x: containerWidth/2 - tooltipFontSize * 1.5,
            y: subpixelOffsetY + tooltipFontSize,
            width: tooltipFontSize * squareMultiplier,
            height: tooltipFontSize * squareMultiplier,
            stroke: 'black',
            strokeWidth: 0.125,
        });
        konvaGroupTooltipInfo.add(subpixelSquare);

        let squareSize = tooltipFontSize / 4;
        //Draw a filled red square in the subpixelUpSquare, based on the subpixelPos.up and subpixelPos.left values
        let subpixelSquareFill = new Konva.Rect({
            x: containerWidth/2 - tooltipFontSize * 1.5 + tooltipFontSize * squareMultiplier * subpixelPos.left - squareSize/2,
            y: subpixelOffsetY + tooltipFontSize + tooltipFontSize * squareMultiplier * subpixelPos.up - squareSize/2,
            width: squareSize,
            height: squareSize,
            fill: 'red',
        });
        konvaGroupTooltipInfo.add(subpixelSquareFill);

        //Place a centered text with subpixelPos.down info below the subpixelUpSquare
        let subpixelDownText = new Konva.Text({
            x: 0,
            y: subpixelOffsetY + tooltipFontSize + tooltipFontSize * squareMultiplier + squareMarginBottom,
            width: containerWidth,
            text: subpixelPos.down.toFixed(settings.decimals),
            fontSize: tooltipFontSize,
            fontFamily: 'Courier New',
            fill: 'black',
            align: 'center',
        });
        konvaGroupTooltipInfo.add(subpixelDownText);

        //Place a right-aligned text with subpixelPos.left info on the left side of the subpixelUpSquare
        let subpixelLeftText = new Konva.Text({
            x: 0,
            y: subpixelOffsetY + tooltipFontSize/2 + tooltipFontSize * squareMultiplier/2,
            width: containerWidth/2 - tooltipFontSize * squareMultiplier/2,
            text: subpixelPos.left.toFixed(settings.decimals),
            fontSize: tooltipFontSize,
            fontFamily: 'Courier New',
            fill: 'black',
            align: 'right',
        });
        konvaGroupTooltipInfo.add(subpixelLeftText);

        //Place a left-aligned text with subpixelPos.right info on the right side of the subpixelUpSquare
        let subpixelRightText = new Konva.Text({
            x: containerWidth/2 + tooltipFontSize * squareMultiplier/2 + squareMarginBottom,
            y: subpixelOffsetY + tooltipFontSize/2 + tooltipFontSize * squareMultiplier/2,
            width: containerWidth/2 - tooltipFontSize * squareMultiplier/2,
            text: subpixelPos.right.toFixed(settings.decimals),
            fontSize: tooltipFontSize,
            fontFamily: 'Courier New',
            fill: 'black',
            align: 'left',
        });
        konvaGroupTooltipInfo.add(subpixelRightText);

        subpixelDisplayHeight = 1 + tooltipFontSize * 5 + squareMarginBottom;
    }

    //Create a tooltip rectangle with additional info about the frame
    let tooltipRect = new Konva.Rect({
        x: 0,
        y: 0,
        fill: 'white',
        stroke: 'black',
        strokeWidth: 0.125,
    });
    tooltipRect.width(tooltipText.width() + tooltipMargin*2);
    tooltipRect.height(tooltipText.height() + subpixelDisplayHeight + tooltipMargin*2);

    konvaGroupTooltip.add(tooltipRect);
    konvaGroupTooltip.add(konvaGroupTooltipInfo);

    konvaGroupTooltip.y(konvaGroupTooltip.y() - tooltipRect.height());



    shape.keepTooltipOpen = false;

    shape.on("click", function(){
        shape.keepTooltipOpen = !shape.keepTooltipOpen;
        if(shape.keepTooltipOpen){
            shape.zIndex(150);
            maddyHitbox.zIndex(0);
            maddyHurtbox.zIndex(1);
            konvaGroupTooltip.zIndex(0);
        } else {
            shape.zIndex(2);
            maddyHurtbox.zIndex(2);
            maddyHitbox.zIndex(2);
            konvaGroupTooltip.zIndex(2);
        }
    });
    shape.on("mouseenter", function(){
        shape.strokeWidth(0.2);
        maddyHitbox.visible(true);
        maddyHurtbox.visible(true);
        konvaGroupTooltip.visible(true);
    });
    shape.on("mouseleave", function(){
        if(!shape.keepTooltipOpen){
            shape.strokeWidth(0);
            maddyHitbox.visible(false);
            maddyHurtbox.visible(false);
            konvaGroupTooltip.visible(false);
        }
    });
}

function getSubpixelDistances(x, y, precision=2){
    function round(n, precision) {
        precision = precision || 0;
        var factor = Math.pow(10, precision);
        return Math.round(n * factor) / factor;
    }

    let xHigher = Math.round(x)+0.5;
    let xLower = xHigher - 1;

    let yHigher = Math.round(y)+0.5;
    let yLower = yHigher - 1;

    return {
        up: round(y - yLower, precision),
        down: round(yHigher - y, precision),
        left: round(x - xLower, precision),
        right: round(xHigher - x, precision),
    };
}

function formatTooltipText(frame, previousFrame, nextFrame){
    if(previousFrame == null){
        previousFrame = {
            speedX: 0,
            speedY: 0,
        }
    }

    let accelerationX = frame.speedX - previousFrame.speedX;
    let accelerationY = frame.speedY - previousFrame.speedY;

    let speedXInPixels = frame.speedX / 60;
    let speedYInPixels = frame.speedY / 60;

    let velocityDiffX = frame.velocityX - speedXInPixels;
    let velocityDiffY = frame.velocityY - speedYInPixels;
    
    let xySeparator = "|";
    let posText = "(" + frame.positionX.toFixed(settings.decimals) + xySeparator + frame.positionY.toFixed(settings.decimals) + ")";
    let speedText = "(" + frame.speedX.toFixed(settings.decimals) + xySeparator + frame.speedY.toFixed(settings.decimals) + ")";
    let accelerationText = "(" + accelerationX.toFixed(settings.decimals) + xySeparator + accelerationY.toFixed(settings.decimals) + ")";
    let absSpeed = Math.sqrt(frame.speedX * frame.speedX + frame.speedY * frame.speedY);
    let velocityText = "(" + frame.velocityX.toFixed(settings.decimals) + xySeparator + frame.velocityY.toFixed(settings.decimals) + ")";
    let velocityDiffText = "(" + velocityDiffX.toFixed(settings.decimals) + xySeparator + velocityDiffY.toFixed(settings.decimals) + ")";
    let liftBoostText = "(" + frame.liftBoostX.toFixed(settings.decimals) + xySeparator + frame.liftBoostY.toFixed(settings.decimals) + ")";

    //flags are space separated
    //split the flags into lines of max 3 flags each
    let flags = frame.flags.split(" ");
    let flagsText = "";
    for(let i = 0; i < flags.length; i++){
        flagsText += flags[i];
        if(i % 3 === 2){
            flagsText += "\n";
        } else {
            flagsText += " ";
        }
    }
    //If the flags are a multiple of 3, then remove the last newline
    if(flags.length % 3 === 0){
        flagsText = flagsText.slice(0, -1);
    }

    let lines = [];
    if(settings.tooltipInfo.frame){
        let frameAddition = "";
        if(nextFrame !== null && nextFrame.frameNumber - frame.frameNumber > 1){
            frameAddition = " (+" + (nextFrame.frameNumber - frame.frameNumber - 1) + " idle)";
        }
        lines.push("    Frame: " + frame.frameNumber + frameAddition);
    }
    if(settings.tooltipInfo.frameRTA){
        let frameAddition = "";
        if(nextFrame !== null && nextFrame.frameNumberRTA - frame.frameNumberRTA > 1){
            frameAddition = " (+" + (nextFrame.frameNumberRTA - frame.frameNumberRTA - 1) + " idle)";
        }
        lines.push("RTA Frame: " + frame.frameNumberRTA + frameAddition);
    }
    if(settings.tooltipInfo.position){
        lines.push("      Pos: " + posText);
    }
    if(settings.tooltipInfo.speed){
        lines.push("    Speed: " + speedText);
    }
    if(settings.tooltipInfo.acceleration){
        lines.push("   Accel.: " + accelerationText);
    }
    if(settings.tooltipInfo.absoluteSpeed){
        lines.push("Abs.Speed: " + absSpeed.toFixed(settings.decimals));
    }
    if(settings.tooltipInfo.velocity){
        lines.push(" Velocity: " + velocityText);
    }
    if(settings.tooltipInfo.velocityDifference){
        lines.push("Vel.Diff.: " + velocityDiffText);
    }
    if(settings.tooltipInfo.liftboost){
        lines.push("LiftBoost: " + liftBoostText);
    }
    if(settings.tooltipInfo.retainedSpeed){
        lines.push(" Retained: " + frame.speedRetention.toFixed(settings.decimals));
    }
    if(settings.tooltipInfo.stamina){
        lines.push("  Stamina: " + frame.stamina.toFixed(settings.decimals));
    }
    if(settings.tooltipInfo.inputs){
        lines.push("   Inputs: " + frame.inputs);
    }
    if(settings.tooltipInfo.flags){
        lines.push("    Flags: ");
        lines.push(flagsText);
    }

    return lines.join("\n");
}

function updateRecordingInfo(){
    let isRecent = selectedRecordingType === RecordingTypes.Recent;

    let recordingTypeText = "Type: "+(isRecent ? "Recent" : "Saved");
    let recordingNameText;
    
    if(isRecent){
        let inRecordingString = isRecording ? " (Recording...)" : "";
        let fileOffset = isRecording ? 1 : 0;
        recordingNameText = "Recording: ("+(selectedRecording+1)+"/"+(recentPhysicsLogFilesList.length+fileOffset)+")"+inRecordingString;
    } else {
        let id = selectedRecording;
        //search through savedPhysicsRecordingsList for the id, and get the name
        for(let i = 0; i < savedPhysicsRecordingsList.length; i++){
            if(savedPhysicsRecordingsList[i].id === id){
                recordingNameText = "Recording: "+savedPhysicsRecordingsList[i].name;
                break;
            }
        }
    }

    let frameCountText = roomLayoutRecording.frameCount+" frames";
    let showingFramesText = "(Showing: "+settings.frameMin+" - "+Math.min(settings.frameMax, roomLayoutRecording.frameCount)+")";

    let sideAddition = "";
    if(roomLayoutRecording.sideName !== "A-Side"){
        sideAddition = " ["+roomLayoutRecording.sideName+"]";
    }
    let mapText = "Map: "+roomLayoutRecording.chapterName+sideAddition;

    //parse roomLayoutRecording.recordingStarted from "2020-05-01T20:00:00.0000000+02:00" to "2020-05-01 20:00:00"
    let date = new Date(roomLayoutRecording.recordingStarted);
    let dateString = date.getFullYear()+"-"+zeroPad(date.getMonth()+1, 2)+"-"+zeroPad(date.getDate(), 2)+" "+zeroPad(date.getHours(), 2)+":"+zeroPad(date.getMinutes(), 2)+":"+zeroPad(date.getSeconds(), 2);
    let timeRecordedText = "Time recorded: "+dateString;

    Elements.RecordingDetails.innerText = recordingNameText+"\n"+frameCountText+" "+showingFramesText+"\n"+mapText+"\n"+timeRecordedText;

    updateFrameButtonStates();
}
//#endregion

//#region Solid Tiles Drawing
const Edge = {
    Top: 1,
    Right: 2,
    Bottom: 4,
    Left: 8
};

function drawSolidTileOutlines(solidTiles, levelBounds){
    let tileSize = 8;
    let tileOffsetX = 0;
    let tileOffsetY = 0.5;

    for(let y = 0; y < solidTiles.length; y++){
        for(let x = 0; x < solidTiles[y].length; x++){
            if(!solidTiles[y][x]) continue;

            let edges = getEmptyTileEdges(solidTiles, x, y);
            if(edges === 0) continue;

            let topleftX = levelBounds.x + x * tileSize + tileOffsetX;
            let topleftY = levelBounds.y + y * tileSize + tileOffsetY;

            //Create konva line for each edge
            if(edges & Edge.Top){
                konvaRoomLayoutLayer.add(createEdgeLine([topleftX, topleftY, topleftX + tileSize, topleftY]));
            }
            if(edges & Edge.Right){
                konvaRoomLayoutLayer.add(createEdgeLine([topleftX + tileSize, topleftY, topleftX + tileSize, topleftY + tileSize]));
            }
            if(edges & Edge.Bottom){
                konvaRoomLayoutLayer.add(createEdgeLine([topleftX, topleftY + tileSize, topleftX + tileSize, topleftY + tileSize]));
            }
            if(edges & Edge.Left){
                konvaRoomLayoutLayer.add(createEdgeLine([topleftX, topleftY, topleftX, topleftY + tileSize]));
            }
        }
    }
}

function createEdgeLine(points){
    return new Konva.Line({
        points: points,
        stroke: 'red',
        strokeWidth: 0.25,
        lineCap: 'square',
        // lineJoin: 'round'
    });
}

//Get all edges that lead to empty tiles
function getEmptyTileEdges(solidTiles, x, y){
    let edges = 0;

    if(y > 0 && !solidTiles[y - 1][x]){
        edges |= Edge.Top;
    }
    if(x < solidTiles[y].length - 1 && !solidTiles[y][x + 1]){
        edges |= Edge.Right;
    }
    if(y < solidTiles.length - 1 && !solidTiles[y + 1][x]){
        edges |= Edge.Bottom;
    }
    if(x > 0 && !solidTiles[y][x - 1]){
        edges |= Edge.Left;
    }

    return edges;
}
//#endregion


//#region Util
function goToPosition(x, y){
    konvaStage.position({
        x: -x,
        y: -y
    });
}

function centerOnPosition(x, y){
    goToPosition(x - konvaStage.width() / 2, y - konvaStage.height() / 2);
}

function centerOnRoom(roomIndex){
    let roomBounds = roomLayouts[roomIndex].levelBounds;
    let centerX = roomBounds.x + roomBounds.width / 2;
    let centerY = roomBounds.y + roomBounds.height / 2;
    konvaStage.scale({x: 1, y: 1});
    centerOnPosition(centerX, centerY);
}

function zeroPad(num, size) {
    var s = num+"";
    while (s.length < size) s = "0" + s;
    return s;
}
//#endregion


//#region API Calls
function getPhysicsLogAsStrings(){
    function appendToLine(line, frame, key){
        if(line.length > 0){
            line += ",";
        }
        line += frame[key];
        return line;
    }
    
    let arr = [];

    for(let i = 0; i < physicsLogFrames.length; i++){
        let line = "";
        let frame = physicsLogFrames[i];
        
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

        arr.push(line);
    }

    return arr;
}


function openSaveRecordingDialog(){
    xdialog.open({
        title: 'Save Recording As...',
        body: '<input type="text" id="recording-name" class="dialog-item" placeholder="Recording Name" required/>',
        buttons: { ok: "Save", cancel: "Cancel"},
        style: 'width:600px',
        listenEnterKey: true,
        listenESCKey: true,
        onok: function(param) {
            let element = document.getElementById('recording-name');
            element.classList.add('validated');

            let recordingName = element.value;
            if (!recordingName) {
                return false;
            }

            console.log("Typed name:", recordingName);
            saveCurrentRecording(recordingName);
        },
    });
}
function saveCurrentRecording(name, dataObj=null){
    let request;

    if(dataObj == null){
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
            "Accept": "application/json",
        },
        body: JSON.stringify(request)
    })
        .then(response => response.json())
        .then(responseObj => {
            if(responseObj.errorCode === 0){
                location.reload();
            }
        })
        .catch(err => {
            console.log(err);
        });
}

function openDeleteRecordingDialog(){
    let recordingName = getRecordingDisplayNameByID(selectedRecordingType, selectedRecording);

    xdialog.open({
        title: 'Delete Recording?',
        body: 'Are you sure you want to delete the recording:<br><code>'+recordingName+'</code>',
        buttons: { delete: "Delete", cancel: "Cancel"},
        style: 'width:600px',
        listenESCKey: true,
        ondelete: function() {
            deleteRecording(selectedRecording);
        },
    });
}
function deleteRecording(id){
    let url = apiBaseUrl + "/deleteRecording?id=" + id;
    //Fetch request
    fetch(url, {
        method: "GET",
        headers: {
            "Accept": "application/json",
        }
    })
        .then(response => response.json())
        .then(responseObj => {
            if(responseObj.errorCode === 0){
                location.reload();
            }
        })
        .catch(err => {
            console.log(err);
        });
}

function openRenameRecordingDialog(){
    xdialog.open({
        title: 'Rename Recording...',
        body: '<input type="text" id="recording-name" class="dialog-item" placeholder="New Name" required/>',
        buttons: { ok: "Rename", cancel: "Cancel"},
        style: 'width:600px',
        listenEnterKey: true,
        listenESCKey: true,
        onok: function(param) {
            let element = document.getElementById('recording-name');
            element.classList.add('validated');

            let recordingName = element.value;
            if (!recordingName) {
                return false;
            }

            console.log("Typed name:", recordingName);
            renameRecording(selectedRecording, recordingName);
        },
    });
}
function renameRecording(id, newName){
    let request = {
        id: id,
        name: newName,
    };

    let url = apiBaseUrl + "/renameRecording";
    //Fetch request
    fetch(url, {
        method: "POST",
        headers: {
            "Accept": "application/json",
        },
        body: JSON.stringify(request)
    })
        .then(response => response.json())
        .then(responseObj => {
            if(responseObj.errorCode === 0){
                location.reload();
            }
        })
        .catch(err => {
            console.log(err);
        });
}


function openImportRecordingDialog(){
    let dataElem = Elements.ImportModal.querySelector("#import-input");
    let nameElem = Elements.ImportModal.querySelector("#import-name");

    dataElem.value = "";
    nameElem.value = "";

    xdialog.open({
        title: 'Import Recording',
        body: { element: Elements.ImportModal },
        buttons: { ok: "Import Recording", cancel: "Cancel"},
        style: 'width:600px',
        listenEnterKey: true,
        listenESCKey: true,
        onok: function(param) {
            let dataElem = Elements.ImportModal.querySelector("#import-input");
            let nameElem = Elements.ImportModal.querySelector("#import-name");

            dataElem.classList.add('validated');
            nameElem.classList.add('validated');

            let data = dataElem.value;
            let name = nameElem.value;

            if(!data || !name){
                return false;
            }

            let dataObj = JSON.parse(data);
            if(!dataObj.layoutFile || !dataObj.physicsLog){
                return false;
            }

            saveCurrentRecording(name, dataObj);
        },
    });
}

function openExportRecordingDialog(){
    let exportObj = {
        layoutFile: roomLayoutRecording,
        physicsLog: getPhysicsLogAsStrings(),
    };

    let inputElem = Elements.ExportModal.querySelector("#export-input");
    inputElem.value = JSON.stringify(exportObj);

    xdialog.open({
        title: 'Export Recording',
        body: { element: Elements.ExportModal },
        buttons: { ok: "Copy to Clipboard", cancel: "Cancel"},
        style: 'width:600px',
        listenEnterKey: true,
        listenESCKey: true,
        onok: function(param) {
            let inputElem = Elements.ExportModal.querySelector("#export-input");
            inputElem.select();
            let success = document.execCommand("copy");
            console.log("Copied to clipboard: ", success, inputElem.value, inputElem);
        },
    });
}
//#endregion

//#region Other Dialogs
function openOtherToolsDialog(){
    Elements.OtherToolsButton.setAttribute("disabled", true);
    xdialog.open({
        title: 'Other Tools',
        body: { element: Elements.OtherToolsModal },
        buttons: { ok: "Ok" },
        style: 'width:400px',
        modal: false,
        onok: function(param) {
            Elements.OtherToolsButton.removeAttribute("disabled");
        },
    });
}

function openEntitiesCountDialog(){
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
    let sortedKeys = Object.keys(entityCounts).sort(function(a,b){return entityCounts[b]-entityCounts[a]});
    let totalCount = 0;
    for(let i = 0; i < sortedKeys.length; i++){
        let key = sortedKeys[i];
        let count = entityCounts[key];
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
        title: 'Entity Counts',
        body: { element: Elements.EntitiesCountModal },
        buttons: { ok: "Ok" },
        style: 'min-width:600px;max-width:1000px;max-height:800px;',
        listenEnterKey: true,
        modal: false,
    });
}


function openOtherEntitiesDialog(){
    let tableElem = Elements.OtherEntitiesModal.querySelector("table");
    tableElem.innerHTML = "";

    //Create headers: Entity #, Entity Name, Position, Collider
    let headerRow = document.createElement("tr");
    let headerCell = document.createElement("th");
    headerCell.innerText = "Entity #";
    headerRow.appendChild(headerCell);
    headerCell = document.createElement("th");
    headerCell.innerText = "Entity Name";
    headerRow.appendChild(headerCell);
    headerCell = document.createElement("th");
    headerCell.innerText = "Position";
    headerRow.appendChild(headerCell);
    headerCell = document.createElement("th");
    headerCell.innerText = "Collider";
    headerRow.appendChild(headerCell);
    tableElem.appendChild(headerRow);

    //Loop through all roomLayouts
    let totalCount = 0;
    for(let i = 0; i < roomLayouts.length; i++){
        let roomName = roomLayouts[i].debugRoomName;
        let otherEntities = roomLayouts[i].otherEntities;
        for(let j = 0; j < otherEntities.length; j++){
            totalCount++;
            let entity = otherEntities[j];
            let row = document.createElement("tr");
            let cell = document.createElement("td");
            cell.innerText = roomName+"-"+totalCount;
            row.appendChild(cell);
            cell = document.createElement("td");
            cell.innerText = entity.type;
            row.appendChild(cell);
            cell = document.createElement("td");
            cell.classList.add("centered");
            cell.innerText = "("+entity.position.x.toFixed(2)+"|"+entity.position.y.toFixed(2)+")";
            row.appendChild(cell);
            cell = document.createElement("td");
            cell.classList.add("centered");
            let collider = entity.properties.isSolid ? "Solid" : entity.properties.hasCollider ? "Trigger" : "None";
            cell.innerText = collider;
            row.appendChild(cell);
            tableElem.appendChild(row);
        }
    }

    xdialog.open({
        title: 'Other Entities ('+totalCount+')',
        body: { element: Elements.OtherEntitiesModal },
        buttons: { ok: "Ok" },
        style: 'min-width:800px;max-width:1000px;max-height:800px;',
        listenEnterKey: true,
        modal: false,
    });
}
//#endregion