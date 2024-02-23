//#region Constants
let spinnerRadius = 6;
let entitiesOffsetX = 0;
let entitiesOffsetY = 0.5;

const MaddyWidth = 7;
const MaddyHeight = 10

const TooltipMargin = 2;
const TooltipBoxOffsetX = 5;
const TooltipBoxOffsetY = 0 - MaddyHeight - 3;


let spinnerEntityNames = [
    "CrystalStaticSpinner",
    "DustStaticSpinner",
    "CustomSpinner",
    "DustTrackSpinner",
    "DustRotateSpinner",
];
let hitboxEntityNames = {
  red: [
    "Solid",
    "FakeWall",
    "FloatySpaceBlock",
    "FancyFloatySpaceBlock",
    "FloatierSpaceBlock",
    "StarJumpBlock",
    "BounceBlock",
    "LockBlock",
    "ClutterDoor",
    "ClutterBlockBase",
    "TempleCrackedBlock",
    "InvisibleBarrier",
    "CustomInvisibleBarrier",
    "SinkingPlatform",
  ],
  white: ["Spikes", "Lightning", "SeekerBarrier", "CrystalBombDetonator"],
  "#666666": [
    "TriggerSpikes",
    "GroupedTriggerSpikes",
    "GroupedDustTriggerSpikes",
    "RainbowTriggerSpikes",
    "TimedTriggerSpikes",
  ],
  orange: [
    "JumpThru",
    "JumpthruPlatform",
    "AttachedJumpThru",
    "SidewaysJumpThru",
    "UpsideDownJumpThru",
    "Puffer",
    "StaticPuffer",
    "SpeedPreservePuffer",
    "ClutterSwitch",
    "HoldableBarrier",
  ],
  "#ff4d00": [
    "SwapBlock",
    "ToggleSwapBlock",
    "ReskinnableSwapBlock",
    "ZipMover",
    "LinkedZipMover",
    "LinkedZipMoverNoReturn",
    "SwitchGate",
    "FlagSwitchGate",
  ],
  green: ["Spring", "CustomSpring", "DashSpring", "SpringGreen"],
  cyan: [
    "FallingBlock",
    "GroupedFallingBlock",
    "RisingBlock",
    "CrumblePlatform",
    "CrumbleBlock",
    "CrumbleBlockOnTouch",
    "FloatyBreakBlock",
    "DashBlock",
    "WallBooster",
    "IcyFloor",
  ],
  yellow: [
    "Portal",
    "LightningBreakerBox",
    "Lookout",
    "CustomPlaybackWatchtower",
    "FlyFeather",
    "Key",
    "Glider",
    "CustomGlider",
    "RespawningJellyfish",
    "TheoCrystal",
    "CrystalBomb",
  ],
  "#C0C0C0": ["SilverBerry"],
  black: [
    "DreamBlock",
    "DreamMoveBlock",
    "CustomDreamBlock",
    "CustomDreamBlockV2",
    "DashThroughSpikes",
    "ConnectedDreamBlock",
  ],
  blue: ["TouchSwitch", "MovingTouchSwitch", "FlagTouchSwitch", "CrushBlock"],
  "#85e340": ["DashSwitch", "TempleGate"],
  "#a200ff": ["MoveBlock", "ConnectedMoveBlock", "VitMoveBlock", "MoveBlockCustomSpeed"],
  "#fa7ded": ["BouncySpikes"],
  Special: ["Strawberry", "Refill", "RefillWall", "Cloud", "CassetteBlock", "WonkyCassetteBlock"],
};
let hitcircleEntityNames = {
  "#0f58d9": ["Bumper", "StaticBumper"],
  white: ["Shield"],
  "#33c3ff": ["BlueBooster"],
  Special: ["Booster", "VortexBumper"],
};
let otherEntityNames = {
  green: ["AngryOshiro"],
};
let specialEntityColorFunctions = {
  Strawberry: (entity) => {
    return entity.r.golden ? "#ffd700" : "#bb0000";
  },
  Refill: (entity) => {
    return entity.r.twoDashes ? "#fa7ded" : "#aedc5e";
  },
  RefillWall: (entity) => {
    return entity.r.twoDashes ? "#fa7ded" : "#aedc5e";
  },
  Cloud: (entity) => {
    return entity.r.fragile ? "#ffa5ff" : "#77cde3";
  },
  Booster: (entity) => {
    return entity.r.red ? "#d3170a" : "#219974";
  },
  CassetteBlock: (entity) => {
    return entity.r.color;
  },
  WonkyCassetteBlock: (entity) => {
    return entity.r.color;
  },
  VortexBumper: (entity) => {
    return entity.r.twoDashes ? "#fa7ded" : entity.r.oneUse ? "#d3170a" : "#0f58d9";
  },
};

let entityNamesDashedOutline = {
  FakeWall: 3.5,
  SeekerBarrier: 0.5,
  HoldableBarrier: 0.5,
  CrystalBombDetonator: 0.5,
  IcyFloor: 1,
  WallBooster: 1,
  Shield: 1,
  CassetteBlock: 3,
  WonkyCassetteBlock: 3,
  InvisibleBarrier: 5,
  CustomInvisibleBarrier: 5,
};

//Objects that map entity names to text properties
//Text properties: fontSize multiplier and text
let entityNamesText = {
  ZipMover: [0.8, "Z"],
  LinkedZipMover: [0.8, "Z"],
  LinkedZipMoverNoReturn: [0.8, "Z"],

  SinkingPlatform: [0.2, "Sinking\nPlatform"],
  
  SwapBlock: [0.8, "S"],
  ToggleSwapBlock: [0.8, "S"],

  BounceBlock: [0.2, "Core\nBlock"],
  CrushBlock: [0.25, "Kevin"],

  Refill: [0.2, (entity) => (entity.r.oneUse ? "" : "Refill")],
  RefillWall: [0.25, (entity) => (entity.r.oneUse ? "" : "Refill\nWall")],

  CrumblePlatform: [0.8, "C"],
  CrumbleBlock: [0.8, "C"],
  CrumbleBlockOnTouch: [0.8, "C"],
  FallingBlock: [0.8, "F"],
  RisingBlock: [0.8, "R"],
  GroupedFallingBlock: [0.8, "F"],
  DashBlock: [0.8, "D"],
  FloatyBreakBlock: [0.2, "Floaty\nBreakBlock"],

  DashSwitch: [0.2, "Switch"],
  TempleGate: [0.2, "Gate"],
  LightningBreakerBox: [0.15, "Breaker\nBox"],
  FlyFeather: [0.8, "F"],
  DashSpring: [0.25, "Dash\nSpring"],
  SpringGreen: [0.225, "Green\nSpring"],
  Cloud: [0.4, "Cloud"],
  Puffer: [0.25, "Puffer"],
  StaticPuffer: [0.25, "Puffer"],
  SpeedPreservePuffer: [0.25, "Speed\nPuffer"],
  Key: [0.4, "Key"],
  LockBlock: [0.15, "LockBlock"],
  ClutterDoor: [0.1, "ClutterDoor"],
  CassetteBlock: [0.15, "Cassette"],
  WonkyCassetteBlock: [0.15, "Cassette"],
  TempleCrackedBlock: [0.15, "Cracked\nBlock"],

  Glider: [0.25, "Jelly"],
  CustomGlider: [0.25, "Jelly"],
  RespawningJellyfish: [0.25, "Jelly"],
  TheoCrystal: [0.25, "Theo"],
  CrystalBomb: [0.2, "Crystal"],

  DashThroughSpikes: [0.25, "Dash\nSpikes"],

  Bumper: [0.2, "Bumper"],
  StaticBumper: [0.175, "Static\nBumper"],
  VortexBumper: [
    (entity) => (entity.r.oneUse ? 0.125 : 0.175),
    (entity) => (entity.r.oneUse ? "Vortex\nBumper\n(One Use)" : "Vortex\nBumper"),
  ],
  Booster: [0.15, "Bubble"],
  BlueBooster: [0.15, "Blue\nBubble"],
};

//Point Label Stuff
let pointLabelPreviousValue = null;
let frameDiffPointLabelFields = {
  PositionX: [(frame) => frame.positionX.toFixed(settings.decimals), false],
  PositionY: [(frame) => frame.positionY.toFixed(settings.decimals), false],
  PositionCombined: [
    (frame) =>
      "(" +
      frame.positionX.toFixed(settings.decimals) +
      "|" +
      frame.positionY.toFixed(settings.decimals) +
      ")",
    false,
  ],
  SpeedX: [(frame) => frame.speedX.toFixed(settings.decimals), false],
  SpeedY: [(frame) => frame.speedY.toFixed(settings.decimals), false],
  SpeedCombined: [
    (frame) =>
      "(" + frame.speedX.toFixed(settings.decimals) + "|" + frame.speedY.toFixed(settings.decimals) + ")",
    false,
  ],

  AbsoluteSpeed: [
    (frame) =>
      Math.sqrt(frame.speedX * frame.speedX + frame.speedY * frame.speedY).toFixed(settings.decimals),
    false,
  ],

  VelocityX: [(frame) => frame.velocityX.toFixed(settings.decimals), false],
  VelocityY: [(frame) => frame.velocityY.toFixed(settings.decimals), false],
  VelocityCombined: [
    (frame) =>
      "(" +
      frame.velocityX.toFixed(settings.decimals) +
      "|" +
      frame.velocityY.toFixed(settings.decimals) +
      ")",
    false,
  ],

  VelocityDifferenceX: [
    (frame) =>
      Math.abs(frame.velocityX - frame.speedX / 60) >= 0.005
        ? (frame.velocityX - frame.speedX / 60).toFixed(settings.decimals)
        : "",
    true,
  ],
  VelocityDifferenceY: [
    (frame) =>
      Math.abs(frame.velocityY - frame.speedY / 60) >= 0.005
        ? (frame.velocityY - frame.speedY / 60).toFixed(settings.decimals)
        : "",
    true,
  ],
  VelocityDifferenceCombined: [
    (frame) =>
      Math.abs(frame.velocityX - frame.speedX / 60) >= 0.005 ||
      Math.abs(frame.velocityY - frame.speedY / 60) >= 0.005
        ? "(" +
          (frame.velocityX - frame.speedX / 60).toFixed(settings.decimals) +
          "|" +
          (frame.velocityY - frame.speedY / 60).toFixed(settings.decimals) +
          ")"
        : "",
    true,
  ],

  LiftBoostX: [(frame) => frame.liftBoostX.toFixed(settings.decimals), false],
  LiftBoostY: [(frame) => frame.liftBoostY.toFixed(settings.decimals), false],
  LiftBoostCombined: [
    (frame) =>
      "(" +
      frame.liftBoostX.toFixed(settings.decimals) +
      "|" +
      frame.liftBoostY.toFixed(settings.decimals) +
      ")",
    false,
  ],
  RetainedSpeed: [(frame) => frame.speedRetention.toFixed(settings.decimals), false],
  Stamina: [(frame) => frame.stamina.toFixed(settings.decimals), false],
  Inputs: [(frame) => frame.inputs, false],
};
let frameDiffDiffPointLabelFields = {
  AccelerationX: (frame, previousFrame) => (frame.speedX - previousFrame.speedX).toFixed(settings.decimals),
  AccelerationY: (frame, previousFrame) => (frame.speedY - previousFrame.speedY).toFixed(settings.decimals),
  AccelerationCombined: (frame, previousFrame) =>
    "(" +
    (frame.speedX - previousFrame.speedX).toFixed(settings.decimals) +
    "|" +
    (frame.speedY - previousFrame.speedY).toFixed(settings.decimals) +
    ")",
};

//Solid tiles
const Edge = {
  Top: 1,
  Right: 2,
  Bottom: 4,
  Left: 8,
};

//#endregion

//#region Properties
let konvaStage = null;
let konvaRoomLayoutLayer = null;
let konvaRoomEntitiesLayer = null;
let konvaRoomMovableEntitiesInitialLayer = null;
let konvaRoomMovableEntitiesLayer = null;
let konvaMaddyHitboxLayer = null;
let konvaTooltipLayer = null;
let konvaLowPrioTooltipLayer = null;
let konvaPositionLayer = null;
//#endregion

//#region Canvas Init

function renderCanvas() {
  if(settings.displayMode === DisplayMode.Classic.name){
    redrawCanvas();
  } else if(settings.displayMode === DisplayMode.Replay.name){
    console.log("Not implemented yet.");
  }
}

function redrawCanvas(frameEntityData = null) {
  //Clear konva layers
  konvaRoomLayoutLayer.destroyChildren();
  konvaRoomEntitiesLayer.destroyChildren();
  konvaRoomMovableEntitiesInitialLayer.destroyChildren();
  konvaRoomMovableEntitiesLayer.destroyChildren();
  konvaMaddyHitboxLayer.destroyChildren();
  konvaPositionLayer.destroyChildren();
  konvaLowPrioTooltipLayer.destroyChildren();
  konvaTooltipLayer.destroyChildren();

  if(settings.displayMode === DisplayMode.Classic.name){
    createAllElements(frameEntityData);
  } else if(settings.displayMode === DisplayMode.Replay.name){
    replayModeCreateInitialState();
  }
}

function createCanvas() {
  // first we need to create a stage
  konvaStage = new Konva.Stage({
    container: "canvas-container",
    width: Elements.CanvasContainer.offsetWidth,
    height: Elements.CanvasContainer.offsetHeight,
    draggable: true,
  });
}

function createLayers() {
  // then create layer
  konvaRoomLayoutLayer = new Konva.Layer({ listening: false });
  konvaRoomEntitiesLayer = new Konva.Layer({ listening: false });
  konvaRoomMovableEntitiesInitialLayer = new Konva.Layer({ listening: false });
  konvaRoomMovableEntitiesLayer = new Konva.Layer({ listening: false });
  konvaMaddyHitboxLayer = new Konva.Layer({ listening: false });
  konvaPositionLayer = new Konva.Layer({ listening: true });
  konvaLowPrioTooltipLayer = new Konva.Layer({ listening: false });
  konvaTooltipLayer = new Konva.Layer({ listening: false });

  // add the layer to the stage
  konvaStage.add(konvaRoomLayoutLayer);
  konvaStage.add(konvaRoomEntitiesLayer);
  konvaStage.add(konvaRoomMovableEntitiesInitialLayer);
  konvaStage.add(konvaRoomMovableEntitiesLayer);
  konvaStage.add(konvaMaddyHitboxLayer);
  konvaStage.add(konvaPositionLayer);
  konvaStage.add(konvaLowPrioTooltipLayer);
  konvaStage.add(konvaTooltipLayer);
}

function addMouseHandlers() {
  var scaleBy = 1.2;
  konvaStage.on("wheel", (e) => {
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


function createAllElements(frameEntityData = null) {
  findRelevantRooms();

  staticEntityCounts = {};
  movableEntityCounts = {};
  
  drawAllRoomBounds();
  roomLayouts.forEach((roomLayout) => {
    let debugRoomName = roomLayout.debugRoomName;

    if (settings.showOnlyRelevantRooms && !relevantRoomNames.includes(debugRoomName)) {
      return;
    }

    let levelBounds = roomLayout.levelBounds;
    //roomLayout.entities is an object with the entity ID as key and the entity as value
    for (const entity of Object.values(roomLayout.entities)) {
      let shapes = createEntityShapes(entity, levelBounds);
      konvaRoomEntitiesLayer.add(...shapes);
    }
    countEntities(roomLayout.entities, staticEntityCounts);
  });
  
  drawMovableEntitiesInitialPositions();
  
  currentRoomEntities = null;
  drawPhysicsLog(frameEntityData);

  // draw the image
  konvaPositionLayer.draw();
}

let staticEntityCounts = {};
let movableEntityCounts = {};
function countEntities(entities, counterObject){
  for (const entity of Object.values(entities)) {
    if (entity.t in counterObject) {
      counterObject[entity.t]++;
    } else {
      counterObject[entity.t] = 1;
    }
  }
}
//#endregion

//#region Canvas Drawing
function drawAllRoomBounds() {
  roomLayouts.forEach((roomLayout) => {
    let debugRoomName = roomLayout.debugRoomName;
    if (settings.showOnlyRelevantRooms && !relevantRoomNames.includes(debugRoomName)) {
      return;
    }
    
    let shapes = createRoomBoundsShapes(roomLayout);
    konvaRoomLayoutLayer.add(...shapes);
  });
}

let SolidTileOffsetX = 0;
let SolidTileOffsetY = 0.5;
function createRoomBoundsShapes(roomLayout){
  let shapes = [];
  let debugRoomName = roomLayout.debugRoomName;

  let levelBounds = roomLayout.levelBounds;
  let solidTiles = roomLayout.solidTiles; //2d array of bools, whether tiles are solid or not

  shapes.push(
      new Konva.Rect({
        x: levelBounds.x + SolidTileOffsetX,
        y: levelBounds.y + SolidTileOffsetY,
        width: levelBounds.w,
        height: levelBounds.h,
        stroke: "white",
        strokeWidth: 0.5,
      })
  );

  if (settings.showRoomNames) {
    shapes.push(
        new Konva.Text({
          x: levelBounds.x + 3,
          y: levelBounds.y + 3,
          text: debugRoomName,
          fontSize: 20,
          fontFamily: "Renogare",
          fill: "white",
          stroke: "black",
          strokeWidth: 1,
        })
    );
  }

  shapes.push(...createSolidTilesOutlines(solidTiles, levelBounds));
  return shapes;
}

function drawMovableEntitiesInitialPositions(){
  let frameStart = settings.frameMin;
  let frameEnd = frameStart + settings.frameStepSize;
  
  //Find all frames that have the flag FirstFrameInRoom
  let firstFrames = [];
  for (let i = frameStart; i <= frameEnd; i++) {
    let frame = physicsLogFrames[i];
    if (frame === undefined) continue;
    if (frame.flags.indexOf("FirstFrameInRoom") !== -1) {
      firstFrames.push(frame);
    }
  }
  
  if(firstFrames.length === 0){
    firstFrames.push(getFirstFrameInRoom(frameStart));
  }
  
  //Draw the initial position of all entities in the first frame of the rooms
  firstFrames.forEach((frame) => {
    let entities = frame.entities;
    for (const entity of Object.values(entities)) {
      let shapes = createEntityShapes(entity, null);
      konvaRoomMovableEntitiesInitialLayer.add(...shapes);
    }
    
    countEntities(entities, movableEntityCounts);
  });
}

function createEntityShapes(entity, levelBounds) {
  const shapes = [];
  
  //Basic entities (Spinners)
  let entityX = entity.p.x + entitiesOffsetX;
  let entityY = entity.p.y + entitiesOffsetY;
  if (spinnerEntityNames.includes(entity.t)){
    //Cull offscreen spinners
    if (
        levelBounds !== null && (
            entityX < levelBounds.x - spinnerRadius ||
            entityX > levelBounds.x + levelBounds.w + spinnerRadius ||
            entityY < levelBounds.y - spinnerRadius ||
            entityY > levelBounds.y + levelBounds.h + spinnerRadius
        )
    ) {
      return shapes;
    }

    shapes.push(...createSpinnerEntityShapes(entity));
  }

  //Non-Spinner entities
  let isOther = true;

  //If the entity type is in any of the value arrays in the hitboxEntityNames map
  let entityColor = Object.keys(hitboxEntityNames).find((color) =>
      hitboxEntityNames[color].includes(entity.t)
  );
  if (entityColor !== undefined) {
    shapes.push(...createHitBoxEntityShapes(entity, entityColor));
    isOther = false;
  }

  //If the entity type is in any of the value arrays in the hitcircleEntityNames map
  entityColor = Object.keys(hitcircleEntityNames).find((color) =>
      hitcircleEntityNames[color].includes(entity.t)
  );
  if (entityColor !== undefined) {
    shapes.push(...createHitCircleEntityShapes(entity, entityColor));
    isOther = false;
  }

  //If the entity type is in any of the value arrays in the otherEntityNames map
  entityColor = Object.keys(otherEntityNames).find((color) =>
      otherEntityNames[color].includes(entity.t)
  );
  if (entityColor !== undefined) {
    shapes.push(...createOtherEntityShape(entity, entityColor));
    isOther = false;
  }

  //Special entities
  //Entity Type: FinalBoos
  if (entity.t === "FinalBoss" || entity.t === "BadelineBoost" || entity.t === "FlingBird") {
    shapes.push(...createBadelineBossShapes(entity));
    isOther = false;
  }

  if (isOther) {
    shapes.push(...createOtherEntityShape(entity));
  }
  
  return shapes;
}

//#region Entity Shapes
function createSpinnerEntityShapes(entity){
  let shapes = [];
  let entityX = entity.p.x + entitiesOffsetX;
  let entityY = entity.p.y + entitiesOffsetY;
  
  //Draw white circle with width 6 on entity position
  let hitcircle = entity.r?.c ?? { x: 0, y: 0, r: 6 };
  shapes.push(createHitCircle(entityX, entityY, hitcircle, "white", 0.5));

  if (settings.showSpinnerRectangle) {
    //Draw white rectangle with width 16, height 4, x offset -8 and y offset -3 on entity position
    let hitbox = entity.r?.b ?? { x: -8, y: -3, w: 16, h: 4 };
    shapes.push(createHitbox(entityX, entityY, hitbox, "white", 0.5));
  }
  return shapes;
}

function createHitBoxEntityShapes(entity, color){
  let shapes = [];
  let hitbox = entity.r.b;
  let entityX = entity.p.x + entitiesOffsetX;
    let entityY = entity.p.y + entitiesOffsetY;

  if (color === "Special") {
    color = specialEntityColorFunctions[entity.t](entity);
  }

  let dash = [];
  if (entity.t in entityNamesDashedOutline) {
    let dashValue = entityNamesDashedOutline[entity.t];
    dash = [dashValue, dashValue];
  }

  //Draw hitbox
  shapes.push(createHitbox(entityX, entityY, hitbox, color, 0.25, dash));
  let additionalShapes = createSimpleHitboxAdditionalShape(color, entityX, entityY, entity);
  shapes.push(...additionalShapes);
  
  return shapes;
}

function createHitCircleEntityShapes(entity, color){
  let shapes = [];
  let hitcircle = entity.r.c;
  let entityX = entity.p.x + entitiesOffsetX;
  let entityY = entity.p.y + entitiesOffsetY;

  if (color === "Special") {
    color = specialEntityColorFunctions[entity.t](entity);
  }

  let dash = [];
  if (entity.t in entityNamesDashedOutline) {
    let dashValue = entityNamesDashedOutline[entity.t];
    dash = [dashValue, dashValue];
  }

  //Draw hitcircle
  shapes.push(createHitCircle(entityX, entityY, hitcircle, color, 0.25, dash));
  let additionalShapes = drawSimpleHitcircleAdditionalShape(color, entityX, entityY, entity);
  shapes.push(...additionalShapes);
  return shapes;
}

let commonColorNames = [
    "white", "black", "red", "green", "blue", "cyan", "yellow", "orange"
];
function createOtherEntityShape(entity, color){
  let shapes = [];
  if (!entity.r.bc) return shapes;
  
  if(color === undefined){
    //Check if the entity.t contains a colors name from CSS_COLORS
    //CSS_COLORS is an object with key => value of colorName => colorHex
    //Ignore case
    let entityTLower = entity.t.toLowerCase();
    let colorName = Object.keys(CSS_COLOR_NAMES).find((color) => entityTLower.includes(color.toLowerCase()));
    if(colorName !== undefined){
      color = CSS_COLOR_NAMES[colorName];
    }
  }

  let entityColor = color ?? (entity.r.bs ? "red" : "white");
  let entityX = entity.p.x + entitiesOffsetX;
  let entityY = entity.p.y + entitiesOffsetY;

  let properties = entity.r;

  let textSplit = entity.t.split(/(?=[A-Z])/);
  let maxWidth = 0;
  //find the longest string in the array
  textSplit.forEach((text) => {
    if (text.length > maxWidth) {
      maxWidth = text.length;
    }
  });
  let maxDim = Math.max(textSplit.length, maxWidth);
  let size = 1 / maxDim;
  let text = textSplit.join("\n");

  //Draw hitbox
  if ("b" in properties) {
    shapes.push(createHitbox(entityX, entityY, properties.b, entityColor));
    //Text
    let fontSize = Math.min(properties.b.w, properties.b.h) * size;
    let offsetY = fontSize * 0.1;
    shapes.push(
        createLetterEntityText(entityX, entityY + offsetY, properties.b, text, fontSize, entityColor)
    );
  }

  //Draw hitcircle
  if ("c" in properties) {
    shapes.push(createHitCircle(entityX, entityY, properties.c, entityColor));
    //Text
    let fontSize = properties.c.r * 2 * size * 0.8;
    let offsetY = fontSize * 0.1;
    shapes.push(
        createLetterEntityTextCircle(
            entityX,
            entityY + offsetY,
            properties.c,
            text,
            fontSize,
            entityColor
        )
    );
  }

  //Draw colliderList
  if ("cl" in properties) {
    properties.cl.forEach((collider) => {
      if (collider.t === "b") {
        shapes.push(createHitbox(entityX, entityY, collider.b, entityColor));
        if (entity.t.indexOf("Spinner") !== -1) return;
        //Text
        let fontSize = Math.min(collider.b.w, collider.b.h) * size;
        let offsetY = fontSize * 0.1;
        shapes.push(
            createLetterEntityText(entityX, entityY + offsetY, collider.b, text, fontSize, entityColor)
        );
      } else if (collider.t === "c") {
        shapes.push(
            createHitCircle(entityX, entityY, collider.c, entityColor)
        );
        //Text
        let fontSize = collider.c.r * 2 * size * 0.8;
        let offsetY = fontSize * 0.1;
        shapes.push(
            createLetterEntityTextCircle(
                entityX,
                entityY + offsetY,
                collider.c,
                text,
                fontSize,
                entityColor
            )
        );
      }
    });
  }
  return shapes;
}

function createBadelineBossShapes(entity){
  let shapes = [];
  let hitcircle = entity.r.c;
  let entityX = entity.p.x + entitiesOffsetX;
  let entityY = entity.p.y + entitiesOffsetY;
  let color = entity.t === "FlingBird" ? "cyan" : "#ff00ff";

  //draw the initial position
  shapes.push(createHitCircle(entityX, entityY, hitcircle, color));

  //loop through properties.nodes, and draw the circle at each node, and a line between each node
  let nodes = entity.r.nodes;
  let previousNode = null;
  for (let i = 0; i < nodes.length; i++) {
    if (entity.t === "FinalBoss" && i === nodes.length - 1) continue;

    let node = nodes[i];
    let nodeX = node.x + hitcircle.x;
    let nodeY = node.y + hitcircle.y;

    //Draw circle on node position
    shapes.push(createHitCircle(node.x, node.y, hitcircle, color));

    //Draw arrow to previous node
    if (previousNode !== null) {
      shapes.push(
          new Konva.Arrow({
            points: [previousNode.x, previousNode.y + hitcircle.y, nodeX, nodeY],
            pointerLength: 2,
            pointerWidth: 2,
            fill: color,
            stroke: color,
            strokeWidth: 0.25,
            lineJoin: "round",
          })
      );
    } else {
      //Draw arrow from initial position to first node
      shapes.push(
          new Konva.Arrow({
            points: [entityX + hitcircle.x, entityY + hitcircle.y, nodeX, nodeY],
            pointerLength: 2,
            pointerWidth: 2,
            fill: color,
            stroke: color,
            strokeWidth: 0.25,
            lineJoin: "round",
          })
      );
    }

    previousNode = node;
  }
  return shapes;
}
//#endregion

//#region Simple Shapes
function createHitbox(posX, posY, hitbox, entityColor, strokeWidth = 0.25, dash = []) {
  return new Konva.Rect({
    x: posX + hitbox.x,
    y: posY + hitbox.y,
    width: hitbox.w,
    height: hitbox.h,
    stroke: entityColor,
    strokeWidth: strokeWidth,
    dash: dash,
  });
}

function createHitCircle(posX, posY, hitcircle, entityColor, strokeWidth = 0.25, dash = []) {
  return new Konva.Circle({
    x: posX + hitcircle.x,
    y: posY + hitcircle.y,
    radius: hitcircle.r,
    stroke: entityColor,
    strokeWidth: strokeWidth,
    dash: dash,
  });
}

function createSimpleHitboxAdditionalShape(entityColor, entityX, entityY, entity) {
  let shapes = [];
  let hitbox = entity.r.b;

  //if entity.t is in entityNamesText, use the text properties from there
  if (entity.t in entityNamesText) {
    let textProperties = entityNamesText[entity.t];
    let size = textProperties[0];
    if (typeof size === "function") {
      size = size(entity);
    }
    let text = textProperties[1];
    if (typeof text === "function") {
      text = text(entity);
    }

    if (text !== "") {
      let fontSize = Math.min(hitbox.w, hitbox.h) * size;
      let offsetY = fontSize * 0.1;

      shapes.push(
        createLetterEntityText(entityX, entityY + offsetY, hitbox, text, fontSize, entityColor)
      );
    }
  }

  if (entity.t === "Strawberry" || entity.t === "SilverBerry") {
    shapes.push(
      new Konva.Circle({
        x: entityX,
        y: entityY,
        radius: 3,
        stroke: entityColor,
        strokeWidth: 0.25,
      })
    );
  }
  if (
    ["TouchSwitch", "FlagTouchSwitch", "SwitchGate", "FlagSwitchGate", "MovingTouchSwitch"].includes(
      entity.t
    )
  ) {
    let offsetX = 0,
      offsetY = 0;
    if (entity.t === "SwitchGate" || entity.t === "FlagSwitchGate") {
      offsetX = hitbox.w / 2;
      offsetY = hitbox.h / 2;
    }
    shapes.push(
      new Konva.Circle({
        x: entityX + offsetX,
        y: entityY + offsetY,
        radius: 6,
        stroke: entityColor,
        strokeWidth: 1,
      })
    );
  }
  if (entity.t === "MovingTouchSwitch") {
    let nodes = entity.r.nodes;
    //Draw the additional nodes
    let previousNode = null;
    nodes.forEach((node) => {
      shapes.push(
        new Konva.Circle({
          x: node.x,
          y: node.y,
          radius: 6,
          stroke: entityColor,
          strokeWidth: 1,
        })
      );

      //Draw an arrow between the nodes
      let drawFromX = previousNode === null ? entityX : previousNode.x;
      let drawFromY = previousNode === null ? entityY : previousNode.y;

      shapes.push(
        new Konva.Arrow({
          points: [drawFromX, drawFromY, node.x, node.y],
          pointerLength: 3,
          pointerWidth: 3,
          fill: entityColor,
          stroke: entityColor,
          strokeWidth: 0.25,
        })
      );

      previousNode = node;
    });
  }

  if ((entity.t === "Refill" || entity.t === "RefillWall") && entity.r.oneUse === true) {
    //Draw a cross on the refill
    let offset = Math.min(hitbox.w, hitbox.h) * 0.1;
    shapes.push(
      new Konva.Line({
        points: [
          entityX + hitbox.x + offset,
          entityY + hitbox.y + offset,
          entityX + hitbox.x + hitbox.w - offset,
          entityY + hitbox.y + hitbox.h - offset,
        ],
        stroke: entityColor,
        strokeWidth: 0.25,
      })
    );
    shapes.push(
      new Konva.Line({
        points: [
          entityX + hitbox.x + hitbox.w - offset,
          entityY + hitbox.y + offset,
          entityX + hitbox.x + offset,
          entityY + hitbox.y + hitbox.h - offset,
        ],
        stroke: entityColor,
        strokeWidth: 0.25,
      })
    );
  }

  if (
    entity.t === "MoveBlock" ||
    entity.t === "ConnectedMoveBlock" ||
    entity.t === "DreamMoveBlock" || 
    entity.t === "MoveBlockCustomSpeed" || 
    entity.t === "VitMoveBlock"
  ) {
    let direction = entity.r.direction; //Left, Right, Up, Down
    //Draw an arrow pointing in the direction, in center of the hitbox
    let strokeWidth = Math.min(hitbox.w, hitbox.h) * 0.2;
    let pointerSize = strokeWidth * 0.45;

    let offset = Math.min(hitbox.w, hitbox.h) * 0.15;
    let arrowOffset = pointerSize * 3.5;

    let points = [];
    if (direction === "Left") {
      points = [
        entityX + hitbox.x + hitbox.w - offset,
        entityY + hitbox.y + hitbox.h / 2,
        entityX + hitbox.x + offset + arrowOffset,
        entityY + hitbox.y + hitbox.h / 2,
      ];
    } else if (direction === "Right") {
      points = [
        entityX + hitbox.x + offset,
        entityY + hitbox.y + hitbox.h / 2,
        entityX + hitbox.x + hitbox.w - offset - arrowOffset,
        entityY + hitbox.y + hitbox.h / 2,
      ];
    } else if (direction === "Up") {
      points = [
        entityX + hitbox.x + hitbox.w / 2,
        entityY + hitbox.y + hitbox.h - offset,
        entityX + hitbox.x + hitbox.w / 2,
        entityY + hitbox.y + offset + arrowOffset,
      ];
    } else if (direction === "Down") {
      points = [
        entityX + hitbox.x + hitbox.w / 2,
        entityY + hitbox.y + offset,
        entityX + hitbox.x + hitbox.w / 2,
        entityY + hitbox.y + hitbox.h - offset - arrowOffset,
      ];
    }

    shapes.push(
      new Konva.Arrow({
        points: points,
        pointerLength: pointerSize,
        pointerWidth: pointerSize,
        fill: entityColor,
        stroke: entityColor,
        strokeWidth: strokeWidth,
      })
    );
  }

  if (entity.t == "Puffer" || entity.t == "StaticPuffer" || entity.t == "SpeedPreservePuffer") {
    let circleRadius = 32;
    //Draw a top half circle on the entity position using svg Path
    shapes.push(
      new Konva.Path({
        x: entityX,
        y: entityY + hitbox.y + hitbox.h,
        data:
          "M " +
          -circleRadius +
          " 0 A " +
          circleRadius +
          " " +
          circleRadius +
          " 0 0 0 " +
          circleRadius +
          " 0 Z",
        stroke: entityColor,
        strokeWidth: 0.25,
      })
    );
  }

  if (entity.t == "ClutterBlockBase" || entity.t == "ClutterSwitch") {
    let fontSize = Math.min(hitbox.w, hitbox.h) * 0.15;
    let offsetY = fontSize * 0.1;
    let clutterName;
    if (entity.r.color === "Red") {
      clutterName = "Towels";
    } else if (entity.r.color === "Yellow") {
      clutterName = "Boxes";
    } else if (entity.r.color === "Green") {
      clutterName = "Books";
    }

    if (entity.t == "ClutterSwitch") {
      clutterName = "Switch:\n" + clutterName;
    }

    shapes.push(
      createLetterEntityText(entityX, entityY + offsetY, hitbox, clutterName, fontSize, entityColor)
    );
  }
  
  return shapes;
}

function drawSimpleHitcircleAdditionalShape(entityColor, entityX, entityY, entity) {
  let shapes = [];
  let hitcircle = entity.r.c;

  if (entity.t in entityNamesText) {
    let textProperties = entityNamesText[entity.t];
    let size = textProperties[0];
    if (typeof size === "function") {
      size = size(entity);
    }
    let text = textProperties[1];
    if (typeof text === "function") {
      text = text(entity);
    }

    if (text !== "") {
      let fontSize = hitcircle.r * 2 * size;
      let offsetY = fontSize * 0.1;

      shapes.push(
        createLetterEntityTextCircle(entityX, entityY + offsetY, hitcircle, text, fontSize, entityColor)
      );
    }
  }
  return shapes;
}

function createLetterEntityText(entityX, entityY, hitbox, text, fontSize, entityColor) {
  return new Konva.Text({
    x: entityX + hitbox.x,
    y: entityY + hitbox.y,
    width: hitbox.w,
    height: hitbox.h,
    text: text,
    fontSize: fontSize,
    fontFamily: "Renogare",
    fill: entityColor,
    align: "center",
    verticalAlign: "middle",
  });
}

function createLetterEntityTextCircle(entityX, entityY, hitcircle, text, fontSize, entityColor) {
  let hitbox = {
    x: hitcircle.x,
    y: hitcircle.y,
    w: hitcircle.r * 2,
    h: hitcircle.r * 2,
  };
  return createLetterEntityText(
    entityX - hitcircle.r,
    entityY - hitcircle.r,
    hitbox,
    text,
    fontSize,
    entityColor
  );
}
//#endregion

//#region Physics Log
function drawPhysicsLog(frameEntityData = null) {
  pointLabelPreviousValue = null;

  for (let i = 0; i < settings.frameStepSize; i++) {
    let frameIndex = settings.frameMin != -1 ? settings.frameMin + i : i;
    if(frameIndex >= physicsLogFrames.length) break;

    let physicsLogShapes = createPhysicsLogShapes(frameIndex);
    konvaPositionLayer.add(...physicsLogShapes.point);
    konvaMaddyHitboxLayer.add(...physicsLogShapes.tooltip.hitbox);
    konvaTooltipLayer.add(...physicsLogShapes.tooltip.tooltip);
    Object.keys(physicsLogShapes.tooltip.movables).forEach((key) => {
      let entityShapes = physicsLogShapes.tooltip.movables[key];
      konvaRoomMovableEntitiesLayer.add(...entityShapes);
    });

    let shapes = createAdditionalFrameData(frameIndex);
    konvaLowPrioTooltipLayer.add(...shapes.followLine);
    konvaLowPrioTooltipLayer.add(...shapes.pointLabel);
  }
}

function createPhysicsLogShapes(frameIndex){
  let shapes = {
    point: [],
    tooltip: {},
  };

  let frame = physicsLogFrames[frameIndex];
  let rasterizedPos = getRasterziedPosition(frame);
  let posX = rasterizedPos.positionX;
  let posY = rasterizedPos.positionY;
  
  let nextFrame = null;
  if (frameIndex < physicsLogFrames.length - 1) {
    nextFrame = physicsLogFrames[frameIndex + 1];
  }
  let previousFrame = null;
  if (frameIndex > 0) {
    previousFrame = physicsLogFrames[frameIndex - 1];
  }

  //Draw circle on position
  let posCircle = new Konva.Circle({
    x: posX,
    y: posY,
    radius: 1.25,
    fill: getFramePointColor(frame),
    stroke: "black",
    strokeWidth: 0,
  });
  shapes.point.push(posCircle);
  shapes.tooltip = createPhysicsTooltip(posCircle, frameIndex, frame, previousFrame, nextFrame);
  return shapes;
}

function createAdditionalFrameData(frameIndex) {
  let shapes = {
    followLine: [],
    pointLabel: [],
  };

  let frame = physicsLogFrames[frameIndex];
  let previousFrame = null;
  if (frameIndex > 0) {
    previousFrame = physicsLogFrames[frameIndex - 1];
  }
  
  let rasterizedPos = getRasterziedPosition(frame);
  let posX = rasterizedPos.positionX;
  let posY = rasterizedPos.positionY;

  if (
    settings.alwaysShowFollowLine ||
    (previousFrame !== null && previousFrame.flags.includes("Dead")) ||
    frame.velocityX > 20 ||
    frame.velocityY > 20 ||
    frame.velocityX < -20 ||
    frame.velocityY < -20
  ) {
    //Draw line to previous position
    if (previousFrame !== null) {
      let rasterizedPreviousPos = getRasterziedPosition(previousFrame);
      shapes.followLine.push(
        new Konva.Line({
          points: [rasterizedPreviousPos.positionX, rasterizedPreviousPos.positionY, posX, posY],
          stroke: "white",
          strokeWidth: 0.05,
          lineCap: "round",
          lineJoin: "round",
        })
      );
    }
  }

  if (settings.pointLabels !== PointLabelNone) {
    shapes.pointLabel.push(...createPointLabel(frame, previousFrame));
  }
  
  return shapes;
}

function createPointLabel(frame, previousFrame) {
  let shapes = [];
  let text = "";

  if (settings.pointLabels === "DragX" && previousFrame !== null) {
    let dragX = frame.speedX - previousFrame.speedX;
    if (
      dragX < -15 ||
      dragX > 15 ||
      dragX === 0 ||
      previousFrame.speedX === 0 ||
      /Retained(.)/.test(frame.flags)
    ) {
    } else {
      text = dragX.toFixed(settings.decimals);
    }
  } else if (settings.pointLabels === "DragY" && previousFrame !== null) {
    let dragY = frame.speedY - previousFrame.speedY;
    if (dragY < -16 || dragY > 16 || dragY === 0 || previousFrame.speedY === 0) {
    } else {
      text = dragY.toFixed(settings.decimals);
    }
  }

  //if settings.pointLabels is in frameDiffPointLabelFields, then add the value of that field to text
  if (settings.pointLabels in frameDiffPointLabelFields) {
    let func = frameDiffPointLabelFields[settings.pointLabels][0];
    let showRepeatValues = frameDiffPointLabelFields[settings.pointLabels][1];
    let valueThisFrame = func(frame);
    if (previousFrame === null || showRepeatValues || func(previousFrame) !== valueThisFrame) {
      text = valueThisFrame;
    }
  }

  //if settings.pointLabels is in frameDiffDiffPointLabelFields, then add the value of that field to text
  if (settings.pointLabels in frameDiffDiffPointLabelFields) {
    if (previousFrame === null) return;
    let func = frameDiffDiffPointLabelFields[settings.pointLabels];
    let valueThisFrame = func(frame, previousFrame);
    if (valueThisFrame !== "0.00" && valueThisFrame !== "-0.00") {
      text = valueThisFrame;
    }
  }

  if (text === "") {
    return shapes;
  }

  let rasterizedPos = getRasterziedPosition(frame);
  let posX = rasterizedPos.positionX;
  let posY = rasterizedPos.positionY;
  let boxWidth = 30;
  let fontSize = 1.5;
  shapes.push(
    new Konva.Text({
      x: posX - boxWidth / 2,
      y: posY - 1.5 - fontSize,
      width: boxWidth,
      height: fontSize,
      align: "center",
      verticalAlign: "middle",
      text: text,
      fontSize: fontSize,
      fontFamily: "Renogare",
      fill: "white",
      stroke: "black",
      strokeWidth: fontSize * 0.08,
    })
  );
  
  return shapes;
}
//#endregion

//#region Tooltips
let currentRoomEntities = null; //{}
function createPhysicsTooltip(shape, frameIndex, frame, previousFrame, nextFrame, frameEntityData = null) {
  let shapes = {
    hitbox: [],
    tooltip: [],
    movables: [],
  };
  
  let rasterizedPos = getRasterziedPosition(frame);
  let posX = rasterizedPos.positionX;
  let posY = rasterizedPos.positionY;


  let maddyHeight = MaddyHeight;
  if (frame.flags.includes("Ducking")) {
    maddyHeight = 5;
  }

  //Draw maddy's hitbox as rectangle
  let maddyHitbox = new Konva.Rect({
    x: posX - MaddyWidth / 2,
    y: posY - maddyHeight,
    width: MaddyWidth,
    height: maddyHeight,
    stroke: "red",
    strokeWidth: 0.125,
    visible: settings.frameStepSize === 1,
  });
  shapes.hitbox.push(maddyHitbox);

  //Draw maddy's hurtbox as green rectangle
  let maddyHurtbox = new Konva.Rect({
    x: posX - MaddyWidth / 2,
    y: posY - maddyHeight,
    width: MaddyWidth,
    height: maddyHeight - 2,
    stroke: "green",
    strokeWidth: 0.125,
    visible: settings.frameStepSize === 1,
  });
  shapes.hitbox.push(maddyHurtbox);

  //Create a group for the tooltip
  let konvaGroupTooltip = createTooltipGroup(posX, posY, frame, nextFrame, previousFrame);
  shapes.tooltip.push(konvaGroupTooltip);
  
  //Render movable shapes
  if (currentRoomEntities === null){
    currentRoomEntities = getEntitiesForFrame(frameIndex);
  } else if (isFrameFirstInRoom(frame)){
    currentRoomEntities = frame.entities;
  } else {
    currentRoomEntities = applyEntityChanges(currentRoomEntities, frame.entities);
  }
  
  //currentRoomEntities is an object with entity ID as key and entity as value
  for (let entityID in currentRoomEntities) {
    let entity = currentRoomEntities[entityID];
    let entityShapes = createEntityShapes(entity, null);
    entityShapes.forEach((shape) => {
      shape.visible(settings.frameStepSize === 1);
    });
    shapes.movables[entityID] = entityShapes;
  }

  shape.keepTooltipOpen = false;

  shape.on("click", function () {
    console.log("Movable entities: ", currentRoomEntities);
    shape.keepTooltipOpen = !shape.keepTooltipOpen;
    if (shape.keepTooltipOpen) {
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
  shape.on("mouseenter", function () {
    shape.strokeWidth(0.2);
    konvaGroupTooltip.visible(true);
    if(settings.frameStepSize !== 1){
      maddyHitbox.visible(true);
      maddyHurtbox.visible(true);
      Object.keys(shapes.movables).forEach((id) => {
        shapes.movables[id].forEach((shape) => {
          shape.visible(true);
        });
      });
      konvaRoomMovableEntitiesInitialLayer.visible(false);
    }
  });
  shape.on("mouseleave", function () {
    if (!shape.keepTooltipOpen) {
      shape.strokeWidth(0);
      konvaGroupTooltip.visible(false);
      if(settings.frameStepSize !== 1){
        maddyHitbox.visible(false);
        maddyHurtbox.visible(false);
        Object.keys(shapes.movables).forEach((id) => {
          shapes.movables[id].forEach((shape) => {
            shape.visible(false);
          });
        });
        konvaRoomMovableEntitiesInitialLayer.visible(true);
      }
    }
  });
  
  return shapes;
}

function createTooltipGroup(posX, posY, frame, nextFrame, previousFrame){
  let konvaGroupTooltip = new Konva.Group({
    x: posX + TooltipBoxOffsetX,
    y: posY + TooltipBoxOffsetY,
    visible: false,
  });

  let konvaGroupTooltipInfo = new Konva.Group({
    x: TooltipMargin,
    y: TooltipMargin,
  });

  //Create a tooltip text with additional info about the frame
  let tooltipFontSize = 2.5;
  let tooltipTextContent = formatTooltipText(frame, previousFrame, nextFrame);
  let tooltipText = new Konva.Text({
    x: 0,
    y: 0,
    text: tooltipTextContent,
    fontSize: tooltipFontSize,
    fontFamily: "Courier New",
    fill: "black",
    align: "left",
  });
  konvaGroupTooltipInfo.add(tooltipText);

  //Subpixel display
  let subpixelDisplayHeight = 0;
  if (settings.tooltipInfo.subpixelDisplay) {
    let subpixelPos = getSubpixelDistances(frame.positionX, frame.positionY);

    let containerWidth = tooltipText.width();
    let subpixelOffsetY = tooltipText.height() + 1;
    let squareMarginBottom = 0.5;
    let squareMultiplier = 3;

    //Place a centered text with subpixelPos.up info
    let subpixelUpText = new Konva.Text({
      x: 0,
      y: subpixelOffsetY,
      width: containerWidth,
      text: subpixelPos.up.toFixed(settings.decimals),
      fontSize: tooltipFontSize,
      fontFamily: "Courier New",
      fill: "black",
      align: "center",
    });
    konvaGroupTooltipInfo.add(subpixelUpText);

    //Draw a square of size tooltipFontSize * 3 below the subpixelUpText
    let subpixelSquare = new Konva.Rect({
      x: containerWidth / 2 - tooltipFontSize * 1.5,
      y: subpixelOffsetY + tooltipFontSize,
      width: tooltipFontSize * squareMultiplier,
      height: tooltipFontSize * squareMultiplier,
      stroke: "black",
      strokeWidth: 0.125,
    });
    konvaGroupTooltipInfo.add(subpixelSquare);

    let squareSize = tooltipFontSize / 4;
    //Draw a filled red square in the subpixelUpSquare, based on the subpixelPos.up and subpixelPos.left values
    let subpixelSquareFill = new Konva.Rect({
      x:
          containerWidth / 2 -
          tooltipFontSize * 1.5 +
          tooltipFontSize * squareMultiplier * subpixelPos.left -
          squareSize / 2,
      y:
          subpixelOffsetY +
          tooltipFontSize +
          tooltipFontSize * squareMultiplier * subpixelPos.up -
          squareSize / 2,
      width: squareSize,
      height: squareSize,
      fill: "red",
    });
    konvaGroupTooltipInfo.add(subpixelSquareFill);

    //Place a centered text with subpixelPos.down info below the subpixelUpSquare
    let subpixelDownText = new Konva.Text({
      x: 0,
      y: subpixelOffsetY + tooltipFontSize + tooltipFontSize * squareMultiplier + squareMarginBottom,
      width: containerWidth,
      text: subpixelPos.down.toFixed(settings.decimals),
      fontSize: tooltipFontSize,
      fontFamily: "Courier New",
      fill: "black",
      align: "center",
    });
    konvaGroupTooltipInfo.add(subpixelDownText);

    //Place a right-aligned text with subpixelPos.left info on the left side of the subpixelUpSquare
    let subpixelLeftText = new Konva.Text({
      x: 0,
      y: subpixelOffsetY + tooltipFontSize / 2 + (tooltipFontSize * squareMultiplier) / 2,
      width: containerWidth / 2 - (tooltipFontSize * squareMultiplier) / 2,
      text: subpixelPos.left.toFixed(settings.decimals),
      fontSize: tooltipFontSize,
      fontFamily: "Courier New",
      fill: "black",
      align: "right",
    });
    konvaGroupTooltipInfo.add(subpixelLeftText);

    //Place a left-aligned text with subpixelPos.right info on the right side of the subpixelUpSquare
    let subpixelRightText = new Konva.Text({
      x: containerWidth / 2 + (tooltipFontSize * squareMultiplier) / 2 + squareMarginBottom,
      y: subpixelOffsetY + tooltipFontSize / 2 + (tooltipFontSize * squareMultiplier) / 2,
      width: containerWidth / 2 - (tooltipFontSize * squareMultiplier) / 2,
      text: subpixelPos.right.toFixed(settings.decimals),
      fontSize: tooltipFontSize,
      fontFamily: "Courier New",
      fill: "black",
      align: "left",
    });
    konvaGroupTooltipInfo.add(subpixelRightText);

    subpixelDisplayHeight = 1 + tooltipFontSize * 5 + squareMarginBottom;
  }

  let analogDisplayHeight = 0;
  if (settings.tooltipInfo.analogDisplay) {
    let aim = {
      x: frame.analogAimX,
      y: frame.analogAimY,
    };

    let analogOffsetY = tooltipText.height() + subpixelDisplayHeight + 1;
    let containerWidth = tooltipText.width();

    let analogCircle = {
      radius: 1,
      radiusDeadzone: 0.25,
      scale: 8, //15
    };
    analogCircle.x = containerWidth / 2;
    analogCircle.y = analogOffsetY + analogCircle.radius * analogCircle.scale;

    //Draw a centered circle
    let circle = new Konva.Circle({
      x: analogCircle.x,
      y: analogCircle.y,
      radius: analogCircle.radius * analogCircle.scale,
      stroke: "black",
      strokeWidth: 0.125,
    });
    konvaGroupTooltipInfo.add(circle);

    //Draw a circle for the deadzone
    let circleDeadzone = new Konva.Circle({
      x: analogCircle.x,
      y: analogCircle.y,
      radius: analogCircle.radius * analogCircle.scale * analogCircle.radiusDeadzone,
      stroke: "red",
      strokeWidth: 0.125,
      opacity: 0.5,
    });
    konvaGroupTooltipInfo.add(circleDeadzone);

    //Draw one line for each axis
    let lineX = new Konva.Line({
      points: [
        analogCircle.x - analogCircle.radius * analogCircle.scale,
        analogCircle.y,
        analogCircle.x + analogCircle.radius * analogCircle.scale,
        analogCircle.y,
      ],
      stroke: "black",
      strokeWidth: 0.125,
      opacity: 0.5,
    });
    konvaGroupTooltipInfo.add(lineX);

    let lineY = new Konva.Line({
      points: [
        analogCircle.x,
        analogCircle.y - analogCircle.radius * analogCircle.scale,
        analogCircle.x,
        analogCircle.y + analogCircle.radius * analogCircle.scale,
      ],
      stroke: "black",
      strokeWidth: 0.125,
      opacity: 0.5,
    });
    konvaGroupTooltipInfo.add(lineY);

    //Draw one line from the circles origin for each direction border point
    let directionBorders = [
      { x: -0.953695, y: -0.300715 },
      { x: -0.300715, y: -0.953695 },
      { x: 0.300715, y: -0.953695 },
      { x: 0.953695, y: -0.300715 },

      { x: 0.9239, y: 0.382683 },
      { x: 0.382683, y: 0.9239 },
      { x: -0.382683, y: 0.9239 },
      { x: -0.9239, y: 0.382683 },
    ];
    directionBorders.forEach((border) => {
      let line = new Konva.Line({
        points: [
          analogCircle.x,
          analogCircle.y,
          analogCircle.x + border.x * analogCircle.radius * analogCircle.scale,
          analogCircle.y + border.y * analogCircle.radius * analogCircle.scale,
        ],
        stroke: "green",
        strokeWidth: 0.125,
        opacity: 0.25,
      });
      konvaGroupTooltipInfo.add(line);
    });

    //Draw one line for all of the rectangular deadzones edges to the circles perimeter
    let deadzoneLines = [
      { x1: -0.7141, y1: 0.7, x2: 0.7141, y2: 0.7 },
      { x1: -0.7141, y1: -0.7, x2: 0.7141, y2: -0.7 },
      { x1: -0.3, y1: 0.953695, x2: -0.3, y2: -0.953695 },
      { x1: 0.3, y1: 0.953695, x2: 0.3, y2: -0.953695 },
    ];
    deadzoneLines.forEach((line) => {
      let lineObj = new Konva.Line({
        points: [
          analogCircle.x + line.x1 * analogCircle.radius * analogCircle.scale,
          analogCircle.y + line.y1 * analogCircle.radius * analogCircle.scale,
          analogCircle.x + line.x2 * analogCircle.radius * analogCircle.scale,
          analogCircle.y + line.y2 * analogCircle.radius * analogCircle.scale,
        ],
        stroke: "orange",
        strokeWidth: 0.125,
        opacity: 0.25,
      });
      konvaGroupTooltipInfo.add(lineObj);
    });

    //Draw a filled rectangle for a different deadzone: 0.3 in X, 0.7 in Y
    let rectDeadzone = new Konva.Rect({
      x: analogCircle.x - analogCircle.radius * analogCircle.scale * 0.3,
      y: analogCircle.y - analogCircle.radius * analogCircle.scale * 0.7,
      width: analogCircle.radius * analogCircle.scale * 0.6,
      height: analogCircle.radius * analogCircle.scale * 1.4,
      fill: "red",
      opacity: 0.25,
    });
    konvaGroupTooltipInfo.add(rectDeadzone);

    //Draw a red square on the circle, based on the aim
    let squareSize = analogCircle.radius * analogCircle.scale * 0.05;
    let square = new Konva.Rect({
      x: analogCircle.x - squareSize / 2 + aim.x * analogCircle.radius * analogCircle.scale,
      y: analogCircle.y - squareSize / 2 + -aim.y * analogCircle.radius * analogCircle.scale,
      width: squareSize,
      height: squareSize,
      fill: "red",
    });
    konvaGroupTooltipInfo.add(square);

    //Draw a text below the circle saying Angle: <angle>\nAmp.: <amplitude>
    let angle = Math.atan2(aim.y, aim.x) * (180 / Math.PI); //CAV angle
    angle = VectorAngleToTAS(angle); //TAS angle
    let amplitude = Math.sqrt(aim.x * aim.x + aim.y * aim.y);
    let angleText = new Konva.Text({
      x: 0,
      y: analogOffsetY + analogCircle.radius * analogCircle.scale * 2 + 1,
      width: containerWidth,
      text:
          "Angle: " +
          angle.toFixed(settings.decimals) +
          "°\nAmplitude: " +
          amplitude.toFixed(settings.decimals),
      fontSize: tooltipFontSize,
      fontFamily: "Courier New",
      fill: "black",
      align: "center",
    });
    konvaGroupTooltipInfo.add(angleText);

    analogDisplayHeight = analogCircle.radius * 2 * analogCircle.scale + 1;
    analogDisplayHeight += angleText.height() + 1;
  }

  //Create a tooltip rectangle with additional info about the frame
  let tooltipRect = new Konva.Rect({
    x: 0,
    y: 0,
    fill: "white",
    stroke: "black",
    strokeWidth: 0.125,
  });
  tooltipRect.width(tooltipText.width() + TooltipMargin * 2);
  tooltipRect.height(tooltipText.height() + subpixelDisplayHeight + analogDisplayHeight + TooltipMargin * 2);

  konvaGroupTooltip.add(tooltipRect);
  konvaGroupTooltip.add(konvaGroupTooltipInfo);

  konvaGroupTooltip.y(konvaGroupTooltip.y() - tooltipRect.height());
  
  return konvaGroupTooltip;
}
//#endregion

//#region Solid Tiles
function createSolidTilesOutlines(solidTiles, levelBounds) {
  let shapes = [];
  let tileSize = 8;
  let tileOffsetX = 0;
  let tileOffsetY = 0.5;

  for (let y = 0; y < solidTiles.length; y++) {
    for (let x = 0; x < solidTiles[y].length; x++) {
      if (!solidTiles[y][x]) continue;

      let edges = getEmptyTileEdges(solidTiles, x, y);
      if (edges === 0) continue;

      let topleftX = levelBounds.x + x * tileSize + tileOffsetX;
      let topleftY = levelBounds.y + y * tileSize + tileOffsetY;

      //Create konva line for each edge
      if (edges & Edge.Top) {
        shapes.push(createEdgeLine([topleftX, topleftY, topleftX + tileSize, topleftY]));
      }
      if (edges & Edge.Right) {
        shapes.push(
          createEdgeLine([topleftX + tileSize, topleftY, topleftX + tileSize, topleftY + tileSize])
        );
      }
      if (edges & Edge.Bottom) {
        shapes.push(
          createEdgeLine([topleftX, topleftY + tileSize, topleftX + tileSize, topleftY + tileSize])
        );
      }
      if (edges & Edge.Left) {
        shapes.push(createEdgeLine([topleftX, topleftY, topleftX, topleftY + tileSize]));
      }
    }
  }
  return shapes;
}

function createEdgeLine(points) {
  return new Konva.Line({
    points: points,
    stroke: "red",
    strokeWidth: 0.25,
    lineCap: "square",
    // lineJoin: 'round'
  });
}

//Get all edges that lead to empty tiles
function getEmptyTileEdges(solidTiles, x, y) {
  let edges = 0;

  if (y > 0 && !solidTiles[y - 1][x]) {
    edges |= Edge.Top;
  }
  if (x < solidTiles[y].length - 1 && !solidTiles[y][x + 1]) {
    edges |= Edge.Right;
  }
  if (y < solidTiles.length - 1 && !solidTiles[y + 1][x]) {
    edges |= Edge.Bottom;
  }
  if (x > 0 && !solidTiles[y][x - 1]) {
    edges |= Edge.Left;
  }

  return edges;
}
//#endregion

//#endregion

//#region Drawing Replay Mode
const replayData = {
  idleFrameIndex: -1,
  roomBoundsShapes: [],
  staticEntityShapes: {},
  movableEntities: {},
  movableEntityShapes: {},
  physicsLog: {},
  additionalFrameData: {},
};
function resetReplayData(){
  replayData.idleFrameIndex = -1;
  replayData.roomBoundsShapes = [];
  replayData.staticEntityShapes = {};
  replayData.movableEntities = {};
  replayData.movableEntityShapes = {};
  replayData.physicsLog = {};
  replayData.additionalFrameData = {};
}
function replayModeCreateInitialState(){
  resetReplayData();
  
  const frameIndex = settings.frameMin;
  const currentFrame = physicsLogFrames[frameIndex];
  const room = getRoomFromFrame(currentFrame);
  replayData.roomBoundsShapes = createRoomBoundsShapes(room);

  let levelBounds = room.levelBounds;
  //roomLayout.entities is an object with the entity ID as key and the entity as value
  for (const entity of Object.values(room.entities)) {
    let shapes = createEntityShapes(entity, levelBounds);
    replayData.staticEntityShapes[entity.i] = shapes;
  }
  
  //Movable Entities
  replayData.movableEntities = getEntitiesForFrame(frameIndex);
  for (let entityID in replayData.movableEntities) {
    let entity = replayData.movableEntities[entityID];
    replayData.movableEntityShapes[entityID] = createEntityShapes(entity, null);
  }
  
  
  replayData.physicsLog = createPhysicsLogShapes(frameIndex);
  replayData.additionalFrameData = createAdditionalFrameData(frameIndex);

  
  //Draw everything to their respective layers
  replayData.roomBoundsShapes.forEach((shape) => konvaRoomLayoutLayer.add(shape));
  for (let entityID in replayData.staticEntityShapes) {
    replayData.staticEntityShapes[entityID].forEach((shape) => konvaRoomEntitiesLayer.add(shape));
  }
  
  konvaPositionLayer.add(...replayData.physicsLog.point);
  konvaMaddyHitboxLayer.add(...replayData.physicsLog.tooltip.hitbox);
  konvaTooltipLayer.add(...replayData.physicsLog.tooltip.tooltip);
  Object.keys(replayData.movableEntityShapes).forEach((key) => {
    let entityShapes = replayData.movableEntityShapes[key];
    konvaRoomMovableEntitiesLayer.add(...entityShapes);
  });
  konvaLowPrioTooltipLayer.add(...replayData.additionalFrameData.pointLabel);
}

function replayModeNextFrame(){
  //Increase frameMin
  const previousFrame = physicsLogFrames[settings.frameMin];
  //Go to next frame, if:
  //  - There are no idle frames
  //  - We already handled all idle frames
  //  - We are ignoring idle frames
  if(previousFrame.idleFrames.length === 0 || (replayData.idleFrameIndex + 1 >= previousFrame.idleFrames.length) || settings.replayIgnoreIdleFrames){
    replayData.idleFrameIndex = -1;
    settings.frameMin += 1;
    if(settings.frameMin >= physicsLogFrames.length) {
      settings.frameMin = 0;
      redrawCanvas();
      return;
    }
  } else {
    replayData.idleFrameIndex += 1;
  }

  //Get the current frame
  const frameIndex = settings.frameMin;
  const currentFrame = physicsLogFrames[settings.frameMin];
  
  if(replayData.idleFrameIndex === -1){
    //Check if the frame is the first frame in a new room
    if(isFrameFirstInRoom(currentFrame)){
      replayModeNewRoom(currentFrame);
      return;
    }
    
    if(replayData.physicsLog.point === undefined){
      console.log("Physics log is undefined");
      return;
    }
    
    //If its not, then update the movable entities
    let pos = getRasterziedPosition(currentFrame);
    let posDiff = null;
    replayData.physicsLog.point.forEach((shape) => {
      if(posDiff === null){
        posDiff = {
          positionX: shape.x() - pos.positionX,
          positionY: shape.y() - pos.positionY,
        };
      }
      shape.position({
        x: pos.positionX,
        y: pos.positionY,
      });
      shape.fill(getFramePointColor(currentFrame));
    });
    //Destroy and recreate tooltip and additional data
    replayData.physicsLog.tooltip.hitbox.forEach((shape) => {
      shape.destroy();
    });
    replayData.physicsLog.tooltip.tooltip.forEach((shape) => {
      shape.destroy();
    });
    let physicsLog = createPhysicsLogShapes(frameIndex);
    replayData.physicsLog.tooltip.hitbox = physicsLog.tooltip.hitbox;
    replayData.physicsLog.tooltip.tooltip = physicsLog.tooltip.tooltip;
    konvaMaddyHitboxLayer.add(...replayData.physicsLog.tooltip.hitbox);
    konvaTooltipLayer.add(...replayData.physicsLog.tooltip.tooltip);

    replayData.additionalFrameData.pointLabel.forEach((shape) => {
      shape.destroy();
    });
    let additionalFrameData = createAdditionalFrameData(frameIndex);
    replayData.additionalFrameData.pointLabel = additionalFrameData.pointLabel;
    konvaLowPrioTooltipLayer.add(...replayData.additionalFrameData.pointLabel);
  }
    
  //Update movable entities
  let frameEntities = replayData.idleFrameIndex !== -1 ? currentFrame.idleFrames[replayData.idleFrameIndex].entities : currentFrame.entities;
  replayModeUpdateEntities(frameEntities);
  
  if(settings.replayIgnoreIdleFrames && currentFrame.idleFrames.length > 0){
    for (let i = 0; i < currentFrame.idleFrames.length; i++){
      let frameEntities = currentFrame.idleFrames[i].entities;
      replayModeUpdateEntities(frameEntities);
    }
  }
}

function replayModeUpdateEntities(frameEntities){
  for (let entityID in frameEntities) {
    let changes = frameEntities[entityID];
    let entity = replayData.movableEntities[entityID];
    if(changes.r.added === true){
      let entityShapes = createEntityShapes(changes, null);
      replayData.movableEntityShapes[entityID] = entityShapes;
      konvaRoomMovableEntitiesLayer.add(...entityShapes);
    } else if(changes.r.removed === true){
      let entityShapes = replayData.movableEntityShapes[entityID];
      entityShapes.forEach((shape) => {
        shape.destroy();
      });
      delete replayData.movableEntityShapes[entityID];
    } else {
      let entityShapes = replayData.movableEntityShapes[entityID];
      entityShapes.forEach((shape) => {
        shape.position({
          x: shape.x() + changes.p.x,
          y: shape.y() + changes.p.y,
        });
      });
      //Find attached entities
      for (let attachedID in replayData.movableEntities){
        let attachedEntity = replayData.movableEntities[attachedID];
        if(attachedEntity.a === entity.i){
          let attachedEntityShapes = replayData.movableEntityShapes[attachedID];
          attachedEntityShapes.forEach((shape) => {
            shape.position({
              x: shape.x() + changes.p.x,
              y: shape.y() + changes.p.y,
            });
          });
        }
      }
    }
  }
  replayData.movableEntities = applyEntityChanges(replayData.movableEntities, frameEntities); 
}

function replayModeNewRoom(frame){
  //Delete all old shapes
  replayData.roomBoundsShapes.forEach((shape) => shape.destroy());
  for (let entityID in replayData.staticEntityShapes) {
    replayData.staticEntityShapes[entityID].forEach((shape) => shape.destroy());
  }
  for (let entityID in replayData.movableEntityShapes) {
    replayData.movableEntityShapes[entityID].forEach((shape) => shape.destroy());
  }
  replayData.physicsLog.point.forEach((shape) => shape.destroy());
  replayData.physicsLog.tooltip.hitbox.forEach((shape) => shape.destroy());
  replayData.physicsLog.tooltip.tooltip.forEach((shape) => shape.destroy());
  replayData.additionalFrameData.pointLabel.forEach((shape) => shape.destroy());
  
  replayModeCreateInitialState(); 
}
//#endregion

//#region Canvas Utils
function getFramePointColor(frame) {
  //if flags contains StDash, then return 'red'
  if (frame.flags.includes("Dead")) {
    return "black";
  } else if (frame.flags.includes("StDash")) {
    return "red";
  } else if (frame.flags.includes("StStarFly")) {
    return "yellow";
  }

  //if inputs contains the jump input "J", then return 'green'
  if (frame.inputs.includes("J") || frame.inputs.includes("K")) {
    return "green";
  }

  //Default color is white
  return "white";
}

function getSubpixelDistances(x, y, precision = 2) {
  function round(n, precision) {
    precision = precision || 0;
    var factor = Math.pow(10, precision);
    return Math.round(n * factor) / factor;
  }

  let xHigher = Math.round(x) + 0.5;
  let xLower = xHigher - 1;

  let yHigher = Math.round(y) + 0.5;
  let yLower = yHigher - 1;

  return {
    up: round(y - yLower, precision),
    down: round(yHigher - y, precision),
    left: round(x - xLower, precision),
    right: round(xHigher - x, precision),
  };
}

function formatTooltipText(frame, previousFrame, nextFrame) {
  if (previousFrame == null) {
    previousFrame = {
      speedX: 0,
      speedY: 0,
    };
  }

  let accelerationX = frame.speedX - previousFrame.speedX;
  let accelerationY = frame.speedY - previousFrame.speedY;

  let speedXInPixels = frame.speedX / 60;
  let speedYInPixels = frame.speedY / 60;

  let velocityDiffX = frame.velocityX - speedXInPixels;
  let velocityDiffY = frame.velocityY - speedYInPixels;

  let xySeparator = "|";
  let posText =
    "(" +
    frame.positionX.toFixed(settings.decimals) +
    xySeparator +
    frame.positionY.toFixed(settings.decimals) +
    ")";
  let speedText =
    "(" +
    frame.speedX.toFixed(settings.decimals) +
    xySeparator +
    frame.speedY.toFixed(settings.decimals) +
    ")";
  let accelerationText =
    "(" +
    accelerationX.toFixed(settings.decimals) +
    xySeparator +
    accelerationY.toFixed(settings.decimals) +
    ")";
  let absSpeed = Math.sqrt(frame.speedX * frame.speedX + frame.speedY * frame.speedY);
  let velocityText =
    "(" +
    frame.velocityX.toFixed(settings.decimals) +
    xySeparator +
    frame.velocityY.toFixed(settings.decimals) +
    ")";
  let velocityDiffText =
    "(" +
    velocityDiffX.toFixed(settings.decimals) +
    xySeparator +
    velocityDiffY.toFixed(settings.decimals) +
    ")";
  let liftBoostText =
    "(" +
    frame.liftBoostX.toFixed(settings.decimals) +
    xySeparator +
    frame.liftBoostY.toFixed(settings.decimals) +
    ")";

  //flags are space separated
  //split the flags into lines of max 3 flags each
  let flags = frame.flags.split(" ");
  let flagsText = "";
  for (let i = 0; i < flags.length; i++) {
    flagsText += flags[i];
    if (i % 3 === 2) {
      flagsText += "\n";
    } else {
      flagsText += " ";
    }
  }
  //If the flags are a multiple of 3, then remove the last newline
  if (flags.length % 3 === 0) {
    flagsText = flagsText.slice(0, -1);
  }

  let lines = [];
  if (settings.tooltipInfo.frame) {
    let frameAddition = "";
    if (frame.idleFrames.length > 0) {
      frameAddition = " (+" + frame.idleFrames.length + " idle)";
    }
    lines.push("    Frame: " + frame.frameNumber + frameAddition);
  }
  if (settings.tooltipInfo.frameRTA) {
    let frameAddition = "";
    if (nextFrame !== null && nextFrame.frameNumberRTA - frame.frameNumberRTA > 1) {
      frameAddition = " (+" + (nextFrame.frameNumberRTA - frame.frameNumberRTA - 1) + " idle)";
    }
    lines.push("RTA Frame: " + frame.frameNumberRTA + frameAddition);
  }
  if (settings.tooltipInfo.position) {
    lines.push("      Pos: " + posText);
  }
  if (settings.tooltipInfo.speed) {
    lines.push("    Speed: " + speedText);
  }
  if (settings.tooltipInfo.acceleration) {
    lines.push("   Accel.: " + accelerationText);
  }
  if (settings.tooltipInfo.absoluteSpeed) {
    lines.push("Abs.Speed: " + absSpeed.toFixed(settings.decimals));
  }
  if (settings.tooltipInfo.velocity) {
    lines.push(" Velocity: " + velocityText);
  }
  if (settings.tooltipInfo.velocityDifference) {
    lines.push("Vel.Diff.: " + velocityDiffText);
  }
  if (settings.tooltipInfo.liftboost) {
    lines.push("LiftBoost: " + liftBoostText);
  }
  if (settings.tooltipInfo.retainedSpeed) {
    lines.push(" Retained: " + frame.speedRetention.toFixed(settings.decimals));
  }
  if (settings.tooltipInfo.stamina) {
    lines.push("  Stamina: " + frame.stamina.toFixed(settings.decimals));
  }
  if (settings.tooltipInfo.inputs) {
    lines.push("   Inputs: " + frame.inputs);
  }
  if (settings.tooltipInfo.flags) {
    lines.push("    Flags: ");
    lines.push(flagsText);
  }

  if (frame.idleFrames.length > 0) {
    lines.push("Idle Inp.| ");
    for (let i = 0; i < frame.idleFrames.length; i++) {
      let idleFrameNumber = frame.idleFrames[i].frameNumber;
      //space padded to 9 characters, right aligned
      let idleFrameNumberStr = idleFrameNumber.toString().padStart(8, " ");
      let analogAddition = "";
      if (settings.tooltipInfo.analogDisplay) {
        let aim = {
          x: frame.idleFrames[i].analogAimX,
          y: frame.idleFrames[i].analogAimY,
        };
        let angle = Math.atan2(aim.y, aim.x) * (180 / Math.PI); //CAV angle
        angle = VectorAngleToTAS(angle); //TAS angle
        let amplitude = Math.sqrt(aim.x * aim.x + aim.y * aim.y);
        analogAddition =
          " (" + angle.toFixed(settings.decimals) + "°," + amplitude.toFixed(settings.decimals) + ")";
      }
      lines.push(idleFrameNumberStr + " | " + frame.idleFrames[i].inputs + analogAddition);
    }
  }

  return lines.join("\n");
}
//#endregion

//#region CSS Colors
const CSS_COLOR_NAMES = {
  AliceBlue: '#F0F8FF',
  AntiqueWhite: '#FAEBD7',
  Aqua: '#00FFFF',
  Aquamarine: '#7FFFD4',
  Azure: '#F0FFFF',
  Beige: '#F5F5DC',
  Bisque: '#FFE4C4',
  Black: '#000000',
  BlanchedAlmond: '#FFEBCD',
  Blue: '#0000FF',
  BlueViolet: '#8A2BE2',
  Brown: '#A52A2A',
  BurlyWood: '#DEB887',
  CadetBlue: '#5F9EA0',
  Chartreuse: '#7FFF00',
  Chocolate: '#D2691E',
  Coral: '#FF7F50',
  CornflowerBlue: '#6495ED',
  Cornsilk: '#FFF8DC',
  Crimson: '#DC143C',
  Cyan: '#00FFFF',
  DarkBlue: '#00008B',
  DarkCyan: '#008B8B',
  DarkGoldenRod: '#B8860B',
  DarkGray: '#A9A9A9',
  DarkGrey: '#A9A9A9',
  DarkGreen: '#006400',
  DarkKhaki: '#BDB76B',
  DarkMagenta: '#8B008B',
  DarkOliveGreen: '#556B2F',
  DarkOrange: '#FF8C00',
  DarkOrchid: '#9932CC',
  DarkRed: '#8B0000',
  DarkSalmon: '#E9967A',
  DarkSeaGreen: '#8FBC8F',
  DarkSlateBlue: '#483D8B',
  DarkSlateGray: '#2F4F4F',
  DarkSlateGrey: '#2F4F4F',
  DarkTurquoise: '#00CED1',
  DarkViolet: '#9400D3',
  DeepPink: '#FF1493',
  DeepSkyBlue: '#00BFFF',
  DimGray: '#696969',
  DimGrey: '#696969',
  DodgerBlue: '#1E90FF',
  FireBrick: '#B22222',
  FloralWhite: '#FFFAF0',
  ForestGreen: '#228B22',
  Fuchsia: '#FF00FF',
  Gainsboro: '#DCDCDC',
  GhostWhite: '#F8F8FF',
  Gold: '#FFD700',
  GoldenRod: '#DAA520',
  Gray: '#808080',
  Grey: '#808080',
  Green: '#008000',
  GreenYellow: '#ADFF2F',
  HoneyDew: '#F0FFF0',
  HotPink: '#FF69B4',
  IndianRed: '#CD5C5C',
  Indigo: '#4B0082',
  Ivory: '#FFFFF0',
  Khaki: '#F0E68C',
  Lavender: '#E6E6FA',
  LavenderBlush: '#FFF0F5',
  LawnGreen: '#7CFC00',
  LemonChiffon: '#FFFACD',
  LightBlue: '#ADD8E6',
  LightCoral: '#F08080',
  LightCyan: '#E0FFFF',
  LightGoldenRodYellow: '#FAFAD2',
  LightGray: '#D3D3D3',
  LightGrey: '#D3D3D3',
  LightGreen: '#90EE90',
  LightPink: '#FFB6C1',
  LightSalmon: '#FFA07A',
  LightSeaGreen: '#20B2AA',
  LightSkyBlue: '#87CEFA',
  LightSlateGray: '#778899',
  LightSlateGrey: '#778899',
  LightSteelBlue: '#B0C4DE',
  LightYellow: '#FFFFE0',
  Lime: '#00FF00',
  LimeGreen: '#32CD32',
  Linen: '#FAF0E6',
  Magenta: '#FF00FF',
  Maroon: '#800000',
  MediumAquaMarine: '#66CDAA',
  MediumBlue: '#0000CD',
  MediumOrchid: '#BA55D3',
  MediumPurple: '#9370DB',
  MediumSeaGreen: '#3CB371',
  MediumSlateBlue: '#7B68EE',
  MediumSpringGreen: '#00FA9A',
  MediumTurquoise: '#48D1CC',
  MediumVioletRed: '#C71585',
  MidnightBlue: '#191970',
  MintCream: '#F5FFFA',
  MistyRose: '#FFE4E1',
  Moccasin: '#FFE4B5',
  NavajoWhite: '#FFDEAD',
  Navy: '#000080',
  OldLace: '#FDF5E6',
  Olive: '#808000',
  OliveDrab: '#6B8E23',
  Orange: '#FFA500',
  OrangeRed: '#FF4500',
  Orchid: '#DA70D6',
  PaleGoldenRod: '#EEE8AA',
  PaleGreen: '#98FB98',
  PaleTurquoise: '#AFEEEE',
  PaleVioletRed: '#DB7093',
  PapayaWhip: '#FFEFD5',
  PeachPuff: '#FFDAB9',
  Peru: '#CD853F',
  Pink: '#FFC0CB',
  Plum: '#DDA0DD',
  PowderBlue: '#B0E0E6',
  Purple: '#800080',
  RebeccaPurple: '#663399',
  Red: '#FF0000',
  RosyBrown: '#BC8F8F',
  RoyalBlue: '#4169E1',
  SaddleBrown: '#8B4513',
  Salmon: '#FA8072',
  SandyBrown: '#F4A460',
  SeaGreen: '#2E8B57',
  SeaShell: '#FFF5EE',
  Sienna: '#A0522D',
  Silver: '#C0C0C0',
  SkyBlue: '#87CEEB',
  SlateBlue: '#6A5ACD',
  SlateGray: '#708090',
  SlateGrey: '#708090',
  Snow: '#FFFAFA',
  SpringGreen: '#00FF7F',
  SteelBlue: '#4682B4',
  Tan: '#D2B48C',
  Teal: '#008080',
  Thistle: '#D8BFD8',
  Tomato: '#FF6347',
  Turquoise: '#40E0D0',
  Violet: '#EE82EE',
  Wheat: '#F5DEB3',
  White: '#FFFFFF',
  WhiteSmoke: '#F5F5F5',
  Yellow: '#FFFF00',
  YellowGreen: '#9ACD32',
  
  //Custom colors
  Dream: '#000000',
};
//#endregion
