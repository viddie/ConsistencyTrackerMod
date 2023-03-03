
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

    NewerRecordingButton: "newer-recording-button",
    OlderRecordingButton: "older-recording-button",
    RecordingDetails: "recording-details",
    EntityCounts: "entity-counts",
};

//#region Properties

let physicsLogFilesList = null;
let roomLayouts = null; //Array
let physicsLogFrames = null; //Array

let konvaStage = null;
let konvaRoomLayoutLayer = null;
let konvaRoomEntitiesLayer = null;
let konvaTooltipLayer = null;
let konvaLowPrioTooltipLayer = null;
let konvaPositionLayer = null;

//#endregion

//#region Settings
let settings = {
    frameFollowLineAlwaysEnabled: false,
    roomNameEnabled: true,
    showDragLabels: false,
    spinnerDrawRectangle: true,
    frameStepSize: 1000,
    frameMin: -1,
    frameMax: -1,

    selectedFile: 0,
};
//#endregion


//#region Startup
document.addEventListener("DOMContentLoaded", function () {
    loadElements(Elements);
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
    setTimeout(() => {
        fetchPhysicsLogFileList(afterFetchPhysicsLogFileList);
    }, 700);
}

function fetchPhysicsLogFileList(then){
    let url = "http://localhost:32270/cct/getPhysicsLogList";
    fetch(url)
        .then(response => response.json())
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                showError(responseObj.errorCode, responseObj.errorMessage);
                return;
            }

            physicsLogFilesList = responseObj.physicsLogFiles;
            then();
        })
        .catch(error => {
            showError(-1, "Could not fetch physics log files list (is CCT running?)");
            console.error(error);
        });
}

function afterFetchPhysicsLogFileList(){
    fetchRoomLayout(afterFetchRoomLayout);
}

function fetchRoomLayout(then){
    let url = "http://localhost:32270/cct/getFileContent?folder=logs&file="+settings.selectedFile+"_room-layout&extension=json";
    fetch(url)
        .then(response => response.json())
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                showError(responseObj.errorCode, responseObj.errorMessage);
                return;
            }

            let fileContentStr = responseObj.fileContent;

            roomLayoutRecording = JSON.parse(fileContentStr);
            roomLayouts = roomLayoutRecording.rooms;

            then();
        })
        .catch(error => {
            showError(-1, "Could not fetch room layouts (is CCT running?)");
            console.error(error);
        });
}
function afterFetchRoomLayout(){
    fetchPhysicsLog(goToInspectorView);
}

function fetchPhysicsLog(then){
    let url = "http://localhost:32270/cct/getFileContent?folder=logs&file="+settings.selectedFile+"_position-log&extension=txt";
    fetch(url)
        .then(response => {
            //log response
            console.log(response);
            return response.json();
        })
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                showError(responseObj.errorCode, responseObj.errorMessage);
                return;
            }

            let fileContentStr = responseObj.fileContent;
            physicsLogFrames = parsePhysicsLogFrames(fileContentStr);
            filterPhysicsLogFrames();
            
            then();
        })
        .catch(error => {
            showError(-1, "Could not fetch physics log (is CCT running?)");
            console.error(error);
        });
}
function goToInspectorView(){
    ShowState(ViewStates.InspectorView);
}

function parsePhysicsLogFrames(fileContent){
    //First line is the header
    let lines = fileContent.split("\n");
    let header = lines[0];

    //All other lines have the format:
    //FrameNumber, PositionX, PositionY, SpeedX, SpeedY, VelocityX, VelocityY, LiftBoostX, LiftBoostY, Flags, Inputs
    let frames = [];
    for (let i = 1; i < lines.length; i++) {
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
        positionX: parseFloat(values[1]),
        positionY: parseFloat(values[2]),
        speedX: parseFloat(values[3]),
        speedY: parseFloat(values[4]),
        velocityX: parseFloat(values[5]),
        velocityY: parseFloat(values[6]),
        liftBoostX: parseFloat(values[7]),
        liftBoostY: parseFloat(values[8]),
        speedRetention: parseFloat(values[9]),
        stamina: parseFloat(values[10]),
        flags: values[11],
        inputs: values[12],
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
    let recordingNumberText = "Recording: "+(settings.selectedFile+1);
    let frameCountText = roomLayoutRecording.frameCount+" frames";

    let sideAddition = "";
    if(roomLayoutRecording.sideName !== "A-Side"){
        sideAddition = " ["+roomLayoutRecording.sideName+"]";
    }
    let mapText = "Map: "+roomLayoutRecording.chapterName+sideAddition;

    //parse roomLayoutRecording.recordingStarted from "2020-05-01T20:00:00.0000000+02:00" to "2020-05-01 20:00:00"
    let date = new Date(roomLayoutRecording.recordingStarted);
    let dateString = date.getFullYear()+"-"+zeroPad(date.getMonth()+1, 2)+"-"+zeroPad(date.getDate(), 2)+" "+zeroPad(date.getHours(), 2)+":"+zeroPad(date.getMinutes(), 2)+":"+zeroPad(date.getSeconds(), 2);
    let timeRecordedText = "Time recorded: "+dateString;

    Elements.RecordingDetails.innerText = recordingNumberText+"\n"+frameCountText+"\n"+mapText+"\n"+timeRecordedText;

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
}

function redrawCanvas(){
    //Clear konva layers
    konvaRoomLayoutLayer.destroyChildren();
    konvaRoomEntitiesLayer.destroyChildren();
    konvaPositionLayer.destroyChildren();
    konvaLowPrioTooltipLayer.destroyChildren();
    konvaTooltipLayer.destroyChildren();

    createAllElements();
}

function createAllElements(){
    drawRoomBounds();
    drawStaticEntities();
    drawPhysicsLog();
    
    // draw the image
    konvaPositionLayer.draw();
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
    konvaPositionLayer = new Konva.Layer({listening: true});
    konvaLowPrioTooltipLayer = new Konva.Layer({listening: false});
    konvaTooltipLayer = new Konva.Layer({listening: false});

    
    // add the layer to the stage
    konvaStage.add(konvaRoomLayoutLayer);
    konvaStage.add(konvaRoomEntitiesLayer);
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

function drawRoomBounds(){
    let tileSize = 8;
    let tileOffsetX = 0;
    let tileOffsetY = 0.5;

    roomLayouts.forEach(roomLayout => {
        let debugRoomName = roomLayout.debugRoomName;
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

        if(settings.roomNameEnabled){
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

        // for(let y = 0; y < solidTiles.length; y++){
        //     for(let x = 0; x < solidTiles[y].length; x++){
        //         if(solidTiles[y][x]){
        //             konvaRoomLayoutLayer.add(new Konva.Rect({
        //                 x: levelBounds.x + x * tileSize + tileOffsetX,
        //                 y: levelBounds.y + y * tileSize + tileOffsetY,
        //                 width: tileSize,
        //                 height: tileSize,
        //                 stroke: (y+x) % 2 === 0 ? 'red' : 'red',
        //                 strokeWidth: 0.25
        //             }));
        //         }
        //     }
        // }

        drawSolidTileOutlines(solidTiles, levelBounds);
    });
}

let spinnerRadius = 6;
let entitiesOffsetX = 0;
let entitiesOffsetY = 0.5;
let hitboxEntityNames = {
    "red": ["Solid", "FloatySpaceBlock", "FloatierSpaceBlock", "StarJumpBlock", "BounceBlock", "LockBlock", "ClutterDoor", "ClutterBlockBase", "TempleCrackedBlock"],
    "white": ["Spikes", "Lightning", "SeekerBarrier", "CrystalBombDetonator"],
    "#666666": ["TriggerSpikes", "GroupedTriggerSpikes", "GroupedDustTriggerSpikes"],
    "orange": ["JumpThru", "JumpthruPlatform", "AttachedJumpThru", "SidewaysJumpThru", "Puffer", "StaticPuffer", "ClutterSwitch"],
    "#ff4d00": ["SwapBlock", "ToggleSwapBlock", "ZipMover", "LinkedZipMover", "LinkedZipMoverNoReturn", "SwitchGate", "FlagSwitchGate"],
    "green": ["Spring", "CustomSpring", "DashSpring"],
    "cyan": ["FallingBlock", "GroupedFallingBlock", "RisingBlock", "CrumblePlatform", "CrumbleBlock", "CrumbleBlockOnTouch", "DashBlock", "WallBooster", "IcyFloor"],
    "yellow": ["Portal", "Glider", "RespawningJellyfish", "TheoCrystal", "CrystalBomb", "LightningBreakerBox", "Lookout", "FlyFeather", "Key"],
    "#C0C0C0": ["SilverBerry"],
    "black": ["DreamBlock", "DreamMoveBlock"],
    "blue": ["TouchSwitch", "MovingTouchSwitch", "FlagTouchSwitch", "CrushBlock"],
    "#85e340": ["DashSwitch", "TempleGate"],
    "#a200ff": ["MoveBlock", "ConnectedMoveBlock"],
    "Special": ["Refill", "Cloud", "CassetteBlock", "WonkyCassetteBlock"],
};
let hitcircleEntityNames = {
    "#0f58d9": ["Bumper"],
    "white": ["Shield"],
    "Special": ["Booster"],
};
let specialEntityColorFunctions = {
    "Strawberry": (entity) => { return entity.properties.golden ? "#ffd700" : "#bb0000"; },
    "Refill": (entity) => { return entity.properties.twoDashes === true ? "#fa7ded" : "#aedc5e"; },
    "Cloud": (entity) => { return entity.properties.fragile === true ? "#ffa5ff" : "#77cde3"; },
    "Booster": (entity) => { return entity.properties.red === true ? "#d3170a" : "#219974"; },
    "CassetteBlock": (entity) => { return entity.properties.color; },
    "WonkyCassetteBlock": (entity) => { return entity.properties.color; },
};

let entityNamesDashedOutline = {
    "SeekerBarrier": 0.5,
    "CrystalBombDetonator": 0.5,
    "IcyFloor": 1,
    "WallBooster": 1,
    "Shield": 1,
    "CassetteBlock": 3,
    "WonkyCassetteBlock": 3,
};

let entityCounts = {};
function drawStaticEntities(){
    entityCounts = {};
    roomLayouts.forEach(roomLayout => {
        let levelBounds = roomLayout.levelBounds;
        let entities = roomLayout.entities;
        entities.forEach(entity => {
            //add type to entityCounts
            if(entityCounts[entity.type] === undefined){
                entityCounts[entity.type] = 0;
            }
            entityCounts[entity.type]++;

            let entityX = entity.position.x + entitiesOffsetX;
            let entityY = entity.position.y + entitiesOffsetY;

            if(entity.type === "CrystalStaticSpinner" || entity.type === "DustStaticSpinner" || entity.type === "CustomSpinner"){
                //Cull offscreen spinners
                if(entityX < levelBounds.x - spinnerRadius || entityX > levelBounds.x + levelBounds.width + spinnerRadius ||
                    entityY < levelBounds.y - spinnerRadius || entityY > levelBounds.y + levelBounds.height + spinnerRadius){
                    return;
                }

                //Draw white circle with width 6 on entity position
                konvaRoomEntitiesLayer.add(new Konva.Circle({
                    x: entityX,
                    y: entityY,
                    radius: spinnerRadius,
                    stroke: 'white',
                    strokeWidth: 0.5,
                }));

                if(settings.spinnerDrawRectangle){
                    //Draw white rectangle with width 16, height 4, x offset -8 and y offset -3 on entity position
                    konvaRoomEntitiesLayer.add(new Konva.Rect({
                        x: entityX - 8,
                        y: entityY - 3,
                        width: 16,
                        height: 4,
                        stroke: 'white',
                        strokeWidth: 0.5,
                    }));
                }
            }

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
                konvaRoomEntitiesLayer.add(new Konva.Rect({
                    x: entityX + hitbox.x,
                    y: entityY + hitbox.y,
                    width: hitbox.width,
                    height: hitbox.height,
                    stroke: entityColor,
                    strokeWidth: 0.25,
                    dash: dash,
                }));

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
                konvaRoomEntitiesLayer.add(new Konva.Circle({
                    x: entityX + hitcircle.x,
                    y: entityY + hitcircle.y,
                    radius: hitcircle.radius,
                    stroke: entityColor,
                    strokeWidth: 0.25,
                    dash: dash,
                }));

                drawSimpleHitcircleAdditionalShape(entityColor, entityX, entityY, entity);
            }

            //Entity Type: FinalBoos
            if(entity.type === "FinalBoss" || entity.type === "BadelineBoost" || entity.type === "FlingBird"){
                let hitcircle = entity.properties.hitcircle;
                let color = entity.type === "FlingBird" ? "cyan" : "#ff00ff";

                //draw the initial position
                konvaRoomEntitiesLayer.add(new Konva.Circle({
                    x: entityX + hitcircle.x,
                    y: entityY + hitcircle.y,
                    radius: hitcircle.radius,
                    stroke: color,
                    strokeWidth: 0.25,
                }));

                //loop through properties.nodes, and draw the circle at each node, and a line between each node
                let nodes = entity.properties.nodes;
                let previousNode = null;
                for(let i = 0; i < nodes.length; i++){
                    if(entity.type === "FinalBoss" && i === nodes.length-1) continue;

                    let node = nodes[i];
                    let nodeX = node.x + hitcircle.x;
                    let nodeY = node.y + hitcircle.y;

                    //Draw circle on node position
                    konvaRoomEntitiesLayer.add(new Konva.Circle({
                        x: nodeX,
                        y: nodeY,
                        radius: hitcircle.radius,
                        stroke: color,
                        strokeWidth: 0.25,
                    }));

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
    });

    //Write the entity counts to the paragraph element at Elements.EntityCounts
    //entityCounts is a map of entity type to count and exists already
    //Sorting the keys of the map by the count, and then by the entity type
    Elements.EntityCounts.innerHTML = "";
    let entityCountsKeys = Object.keys(entityCounts);
    entityCountsKeys.sort((a, b) => {
        if(entityCounts[a] === entityCounts[b]){
            return a.localeCompare(b);
        }
        return entityCounts[b] - entityCounts[a];
    });
    let total = 0;
    entityCountsKeys.forEach(entityType => {
        total += entityCounts[entityType];
        Elements.EntityCounts.innerHTML += `${entityType}: ${entityCounts[entityType]}<br>`;
    });
    Elements.EntityCounts.innerHTML = `Total: ${total}<br>` + Elements.EntityCounts.innerHTML;

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

    "CrumblePlatform": [0.8, "C"],
    "CrumbleBlock": [0.8, "C"],
    "CrumbleBlockOnTouch": [0.8, "C"],
    "FallingBlock": [0.8, "F"],
    "RisingBlock": [0.8, "R"],
    "GroupedFallingBlock": [0.8, "F"],
    "DashBlock": [0.8, "D"],

    "DashSwitch": [0.2, "Switch"],
    "TempleGate": [0.2, "Gate"],
    "LightningBreakerBox": [0.15, "Breaker\nBox"],
    "FlyFeather": [0.8, "F"],
    "DashSpring": [0.25, "Dash"],
    "TheoCrystal": [0.25, "Theo"],
    "CrystalBomb": [0.2, "Crystal"],
    "Cloud": [0.4, "Cloud"],
    "Puffer": [0.25, "Puffer"],
    "StaticPuffer": [0.25, "Puffer"],
    "Key": [0.4, "Key"],
    "LockBlock": [0.15, "LockBlock"],
    "ClutterDoor": [0.1, "ClutterDoor"],
    "CassetteBlock": [0.15, "Cassette"],
    "WonkyCassetteBlock": [0.15, "Cassette"],
    "TempleCrackedBlock": [0.15, "Cracked\nBlock"],

    "Bumper": [0.8, "B"],
    "Booster": [0.25, "Bubble"],
};

function drawSimpleHitboxAdditionalShape(entityColor, entityX, entityY, entity){
    let hitbox = entity.properties.hitbox;

    //if entity.type is in entityNamesText, use the text properties from there
    if(entity.type in entityNamesText){
        let textProperties = entityNamesText[entity.type];
        let fontSize = Math.min(hitbox.width, hitbox.height) * textProperties[0];
        let offsetY = fontSize * 0.1;
        konvaRoomEntitiesLayer.add(createLetterEntityText(entityX, entityY + offsetY, hitbox, textProperties[1], fontSize, entityColor));
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

    if(entity.type === "Refill" && entity.properties.oneUse === true){
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


    if(entity.type == "Puffer" || entity.type == "StaticPuffer"){
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

    if(entity.type == "Bumper"){
        let fontSize = hitcircle.radius * 0.8;
        let offsetY = fontSize * 0.1;
        konvaRoomEntitiesLayer.add(createLetterEntityTextCircle(entityX, entityY + offsetY, hitcircle, "B", fontSize, entityColor));
    }

    if(entity.type == "Booster"){
        let fontSize = hitcircle.radius * 0.25;
        let offsetY = fontSize * 0.1;
        konvaRoomEntitiesLayer.add(createLetterEntityTextCircle(entityX, entityY + offsetY, hitcircle, "Bubble", fontSize, entityColor));
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


function drawPhysicsLog(){
    let previousFrame = null;
    for(let i = 0; i < physicsLogFrames.length; i++){
        let frame = physicsLogFrames[i];

        if(settings.frameMin != -1 && frame.frameNumber < settings.frameMin) continue;
        if(settings.frameMax != -1 && frame.frameNumber > settings.frameMax) break;

        let posX = frame.positionX;
        let posY = frame.positionY;

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

        createPhysicsTooltip(posCircle, frame);

        drawAdditionalFrameData(frame, previousFrame);
        previousFrame = frame;
    }
}

function drawAdditionalFrameData(frame, previousFrame){
    let posX = frame.positionX;
    let posY = frame.positionY;

    if(settings.frameFollowLineAlwaysEnabled
        || (previousFrame !== null && previousFrame.flags.includes('Dead'))
        || frame.velocityX > 20 || frame.velocityY > 20){
        //Draw line to previous position
        if(previousFrame !== null){
            konvaTooltipLayer.add(new Konva.Line({
                points: [previousFrame.positionX, previousFrame.positionY, posX, posY],
                stroke: 'white',
                strokeWidth: 0.05,
                lineCap: 'round',
                lineJoin: 'round'
            }));
        }
    }

    if(settings.showDragLabels && previousFrame !== null){
        let dragX = frame.speedX - previousFrame.speedX;
        if(dragX < -15 || dragX > 15 || dragX === 0 || previousFrame.speedX === 0 || /Retained(.)/.test(frame.flags)) {

        } else {
            let fontSize = 1.5;
            //Draw text over position according to dragX
            konvaLowPrioTooltipLayer.add(new Konva.Text({
                x: posX - 0.5 - fontSize,
                y: posY - 1.5 - fontSize,
                text: dragX.toFixed(2),
                fontSize: fontSize,
                fontFamily: 'Renogare',
                fill: 'white',
                stroke: 'black',
                strokeWidth: fontSize * 0.08,
            }));
        }
    }
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

function createPhysicsTooltip(shape, frame){
    let posX = frame.positionX;
    let posY = frame.positionY;

    let maddyWidth = 7;
    let maddyHeight = 10;

    if(frame.flags.includes('Ducking')){
        maddyHeight = 5;
    }

    // let tooltipBoxWidth = 70;
    let tooltipBoxHeight = 6;
    let tooltipBoxOffsetX = 5;
    let tooltipBoxOffsetY = 0 - maddyHeight - tooltipBoxHeight - 3;
    let tooltipFontSize = 2.5;

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
    konvaTooltipLayer.add(maddyHitbox);

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
    konvaTooltipLayer.add(maddyHurtbox);


    let tooltipTextContent = formatTooltipText(frame);
    //For every line in the tooltipTextContent, add some height to the tooltip box
    let additionalHeight = (tooltipTextContent.split("\n").length - 1) * tooltipFontSize;
    tooltipBoxHeight = tooltipBoxHeight + additionalHeight;
    tooltipBoxOffsetY = tooltipBoxOffsetY - additionalHeight;

    //Create a tooltip rectangle with additional info about the frame
    let tooltipRect = new Konva.Rect({
        x: posX + tooltipBoxOffsetX,
        y: posY + tooltipBoxOffsetY,
        // width: tooltipBoxWidth,
        height: tooltipBoxHeight,
        fill: 'white',
        stroke: 'black',
        strokeWidth: 0.125,
        visible: false
    });

    //Create a tooltip text with additional info about the frame
    let tooltipText = new Konva.Text({
        x: posX + tooltipBoxOffsetX + 2,
        y: posY + tooltipBoxOffsetY + 2,
        text: tooltipTextContent,
        fontSize: tooltipFontSize,
        fontFamily: 'Courier New',
        fill: 'black',
        align: 'left',
        visible: false
    });

    tooltipRect.width(tooltipText.width() + 4);

    konvaTooltipLayer.add(tooltipRect);
    konvaTooltipLayer.add(tooltipText);

    shape.keepTooltipOpen = false;

    shape.on("click", function(){
        shape.keepTooltipOpen = !shape.keepTooltipOpen;
        if(shape.keepTooltipOpen){
            shape.zIndex(150);
            maddyHitbox.zIndex(0);
            maddyHurtbox.zIndex(1);
            tooltipRect.zIndex(0);
            tooltipText.zIndex(1);
        } else {
            shape.zIndex(2);
            maddyHitbox.zIndex(2);
            maddyHurtbox.zIndex(2);
            tooltipRect.zIndex(2);
            tooltipText.zIndex(2);
        }
    });
    shape.on("mouseenter", function(){
        shape.strokeWidth(0.2);
        maddyHitbox.visible(true);
        maddyHurtbox.visible(true);
        tooltipRect.visible(true);
        tooltipText.visible(true);
    });
    shape.on("mouseleave", function(){
        if(!shape.keepTooltipOpen){
            shape.strokeWidth(0);
            maddyHitbox.visible(false);
            maddyHurtbox.visible(false);
            tooltipRect.visible(false);
            tooltipText.visible(false);
        }
    });
}

function formatTooltipText(frame){
    let xySeparator = "|";

    let posText = "(" + frame.positionX.toFixed(2) + xySeparator + frame.positionY.toFixed(2) + ")";
    let speedText = "(" + frame.speedX.toFixed(2) + xySeparator + frame.speedY.toFixed(2) + ")";
    let absSpeed = Math.sqrt(frame.speedX * frame.speedX + frame.speedY * frame.speedY);
    let velocityText = "(" + frame.velocityX.toFixed(2) + xySeparator + frame.velocityY.toFixed(2) + ")";
    let liftBoostText = "(" + frame.liftBoostX.toFixed(2) + xySeparator + frame.liftBoostY.toFixed(2) + ")";

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

    return "    Frame: " + frame.frameNumber + "\n" +
           "      Pos: " + posText + "\n" +
           "    Speed: " + speedText + "\n" +
           "Abs.Speed: " + absSpeed.toFixed(2) + "\n" +
           " Velocity: " + velocityText + "\n" +
           "LiftBoost: " + liftBoostText + "\n" +
           " Retained: " + frame.speedRetention.toFixed(2) + "\n" +
           "  Stamina: " + frame.stamina.toFixed(2) + "\n" +
           "   Inputs: " + frame.inputs + "\n" +
           "    Flags: \n" + flagsText;
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

//#region Settings
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

    redrawCanvas();
}

function ChangeRecording(direction){
    if(direction == 1 && Elements.OlderRecordingButton.hasAttribute("disabled")){
        return;
    } else if(direction == -1 && Elements.NewerRecordingButton.hasAttribute("disabled")){
        return;
    }

    let selectedBefore = settings.selectedFile;
    settings.selectedFile += direction;
    if(settings.selectedFile < 0){
        settings.selectedFile = 0;
    } else if(settings.selectedFile >= 10){
        settings.selectedFile = 9;
    }

    if(selectedBefore == settings.selectedFile){
        return;
    }

    if(settings.selectedFile == 0){
        Elements.NewerRecordingButton.setAttribute("disabled", true);
    } else {
        Elements.NewerRecordingButton.removeAttribute("disabled");
    }

    if(settings.selectedFile == physicsLogFilesList.length-1){
        Elements.OlderRecordingButton.setAttribute("disabled", true);
    } else {
        Elements.OlderRecordingButton.removeAttribute("disabled");
    }

    fetchRoomLayout(afterFetchRoomLayout);
}
//#endregion