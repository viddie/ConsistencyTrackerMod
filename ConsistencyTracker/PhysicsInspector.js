
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
};

//#region Properties

let roomLayouts = null; //Array
let physicsLogFrames = null; //Array

let konvaStage = null;
let konvaRoomLayoutLayer = null;
let konvaRoomEntitiesLayer = null;
let konvaTooltipLayer = null;
let konvaPositionLayer = null;

//#endregion

//#region Settings
let settings = {
    frameFollowLineAlwaysEnabled: false,
    roomNameEnabled: false,
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
        fetchRoomLayout(afterFetchRoomLayout);
    }, 700);
}

function fetchRoomLayout(then){
    let url = "http://localhost:32270/cct/getFileContent?folder=logs&file=room-layout&extension=json";
    fetch(url)
        .then(response => response.json())
        .then(responseObj => {
            console.log(responseObj);
            if(responseObj.errorCode !== 0){
                showError(responseObj.errorCode, responseObj.errorMessage);
                return;
            }

            let fileContentStr = responseObj.fileContent;

            roomLayouts = JSON.parse(fileContentStr);

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
    let url = "http://localhost:32270/cct/getFileContent?folder=logs&file=position_log&extension=txt";
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
        flags: values[9],
        inputs: values[10],
    };
    return frame;
}
//#endregion


//#region EditView
function OnShowInspectorView() {
    //Display data here
    createCanvas();
    addMouseHandlers();

    drawRoomBounds();
    drawStaticEntities();
    drawPhysicsLog();
    
    // draw the image
    konvaPositionLayer.draw();

    let firstRoomBounds = roomLayouts[0].levelBounds;
    let centerX = firstRoomBounds.x + firstRoomBounds.width / 2;
    let centerY = firstRoomBounds.y + firstRoomBounds.height / 2;
    centerOnPosition(centerX, centerY);
}

function createCanvas(){
    // first we need to create a stage
    konvaStage = new Konva.Stage({
        container: 'canvas-container',
        width: Elements.CanvasContainer.offsetWidth,
        height: Elements.CanvasContainer.offsetHeight,
        draggable: true,
    });
    
    // then create layer
    konvaRoomLayoutLayer = new Konva.Layer({listening: false});
    konvaRoomEntitiesLayer = new Konva.Layer({listening: false});
    konvaPositionLayer = new Konva.Layer({listening: true});
    konvaTooltipLayer = new Konva.Layer({listening: false});
    
    // add the layer to the stage
    konvaStage.add(konvaRoomLayoutLayer);
    konvaStage.add(konvaRoomEntitiesLayer);
    konvaStage.add(konvaPositionLayer);
    konvaStage.add(konvaTooltipLayer);
}

function addMouseHandlers(){
    var scaleBy = 1.3;
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
    "red": ["Solid", "FloatySpaceBlock"],
    "white": ["Spikes", "Lightning"],
    "#666666": ["TriggerSpikes", "GroupedTriggerSpikes", "GroupedDustTriggerSpikes"],
    "orange": ["JumpThru", "JumpthruPlatform", "AttachedJumpThru", "SidewaysJumpThru"],
    "#ff4d00": ["BounceBlock", "SwapBlock", "ZipMover", "SwitchGate", "FlagSwitchGate"],
    "green": ["Spring", "CustomSpring"],
    "cyan": ["FallingBlock", "GroupedFallingBlock", "CrumblePlatform", "CrumbleBlock", "CrumbleBlockOnTouch", "DashBlock"],
    "yellow": ["Portal", "Glider", "TheoCrystal", "LightningBreakerBox", "Lookout"],
    "#FFD700": ["Strawberry"],
    "#C0C0C0": ["SilverBerry"],
    "pink": ["Refill"],
    "black": ["DreamBlock"],
    "blue": ["TouchSwitch", "FlagTouchSwitch", "CrushBlock"],
};
function drawStaticEntities(){
    roomLayouts.forEach(roomLayout => {
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
                konvaRoomEntitiesLayer.add(new Konva.Circle({
                    x: entityX,
                    y: entityY,
                    radius: spinnerRadius,
                    stroke: 'white',
                    strokeWidth: 0.5,
                }));

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

            //If the entity type is in any of the value arrays in the hitboxEntityNames map
            let entityColor = Object.keys(hitboxEntityNames).find(color => hitboxEntityNames[color].includes(entity.type));
            if(entityColor !== undefined){
                let hitbox = entity.properties.hitbox;

                if(entity.type === "Strawberry"){
                    entityColor = entity.properties.golden ? "#FFD700" : "#BB0000";
                }
                if(entity.type === "Refill"){
                    entityColor = entity.properties.twoDashes === true ? "#fa7ded" : "#aedc5e";
                }

                //Draw hitbox
                konvaRoomEntitiesLayer.add(new Konva.Rect({
                    x: entityX + hitbox.x,
                    y: entityY + hitbox.y,
                    width: hitbox.width,
                    height: hitbox.height,
                    stroke: entityColor,
                    strokeWidth: 0.25,
                }));

                if(entity.type === "Strawberry" || entity.type === "SilverBerry"){
                    konvaRoomEntitiesLayer.add(new Konva.Circle({
                        x: entityX,
                        y: entityY,
                        radius: 3,
                        stroke: entityColor,
                        strokeWidth: 0.25,
                    }));
                }
                if(entity.type === "TouchSwitch" || entity.type === "FlagTouchSwitch"){
                    konvaRoomEntitiesLayer.add(new Konva.Circle({
                        x: entityX,
                        y: entityY,
                        radius: 6,
                        stroke: entityColor,
                        strokeWidth: 1,
                    }));
                }

                if(entity.type === "Refill" && entity.properties.oneUse === true){
                    //Draw a cross on the refill
                    konvaRoomEntitiesLayer.add(new Konva.Line({
                        points: [
                            entityX + hitbox.x, entityY + hitbox.y,
                            entityX + hitbox.x + hitbox.width, entityY + hitbox.y + hitbox.height
                        ],
                        stroke: entityColor,
                        strokeWidth: 0.25,
                    }));
                    konvaRoomEntitiesLayer.add(new Konva.Line({
                        points: [
                            entityX + hitbox.x + hitbox.width, entityY + hitbox.y,
                            entityX + hitbox.x, entityY + hitbox.y + hitbox.height
                        ],
                        stroke: entityColor,
                        strokeWidth: 0.25,
                    }));
                }
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
                nodes.forEach(node => {
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
                });
            }
        });
    });
}


function drawPhysicsLog(){
    let previousFrame = null;
    physicsLogFrames.forEach(frame => {
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

        if(settings.frameFollowLineAlwaysEnabled
            || (previousFrame !== null && previousFrame.flags.includes('Dead'))
            || frame.velocityX > 20 || frame.velocityY > 20){
            //Draw line to previous position
            if(previousFrame !== null){
                konvaRoomLayoutLayer.add(new Konva.Line({
                    points: [previousFrame.positionX, previousFrame.positionY, posX, posY],
                    stroke: 'white',
                    strokeWidth: 0.05,
                    lineCap: 'round',
                    lineJoin: 'round'
                }));
            }
        }

        previousFrame = frame;
    });
}

function getFramePointColor(frame){
    //if flags contains StDash, then return 'red'
    if(frame.flags.includes('StDash')){
        return 'red';
    } else if(frame.flags.includes('Dead')){
        return 'black';
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

    let tooltipBoxWidth = 65;
    let tooltipBoxHeight = 23;
    let tooltipBoxOffsetX = 5;
    let tooltipBoxOffsetY = 0 - maddyHeight - tooltipBoxHeight - 3;

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

    //Create a tooltip rectangle with additional info about the frame
    let tooltipRect = new Konva.Rect({
        x: posX + tooltipBoxOffsetX,
        y: posY + tooltipBoxOffsetY,
        width: tooltipBoxWidth,
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
        text: formatTooltipText(frame),
        fontSize: 2.5,
        fontFamily: 'Courier New',
        fill: 'black',
        align: 'left',
        visible: false
    });

    konvaTooltipLayer.add(tooltipRect);
    konvaTooltipLayer.add(tooltipText);


    shape.on("mouseenter", function(){
        shape.strokeWidth(0.2);
        maddyHitbox.visible(true);
        maddyHurtbox.visible(true);
        tooltipRect.visible(true);
        tooltipText.visible(true);
    });
    shape.on("mouseleave", function(){
        shape.strokeWidth(0);
        maddyHitbox.visible(false);
        maddyHurtbox.visible(false);
        tooltipRect.visible(false);
        tooltipText.visible(false);
    });
}

function formatTooltipText(frame){
    let xySeparator = "|";

    let posText = "(" + frame.positionX.toFixed(2) + xySeparator + frame.positionY.toFixed(2) + ")";
    let speedText = "(" + frame.speedX.toFixed(2) + xySeparator + frame.speedY.toFixed(2) + ")";
    let velocityText = "(" + frame.velocityX.toFixed(2) + xySeparator + frame.velocityY.toFixed(2) + ")";
    let liftBoostText = "(" + frame.liftBoostX.toFixed(2) + xySeparator + frame.liftBoostY.toFixed(2) + ")";

    return "    Frame: " + frame.frameNumber + "\n" +
    "      Pos: " + posText + "\n" +
    "    Speed: " + speedText + "\n" +
    " Velocity: " + velocityText + "\n" +
    "LiftBoost: " + liftBoostText + "\n" +
    "   Inputs: " + frame.inputs + "\n" +
    "    Flags: \n" + frame.flags;
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
//#endregion