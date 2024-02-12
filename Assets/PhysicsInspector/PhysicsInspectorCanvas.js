//#region Constants
let spinnerRadius = 6;
let entitiesOffsetX = 0;
let entitiesOffsetY = 0.5;
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
  "#a200ff": ["MoveBlock", "ConnectedMoveBlock", "VitMoveBlock"],
  "#fa7ded": ["BouncySpikes"],
  Special: ["Strawberry", "Refill", "RefillWall", "Cloud", "CassetteBlock", "WonkyCassetteBlock"],
};
let hitcircleEntityNames = {
  "#0f58d9": ["Bumper", "StaticBumper"],
  white: ["Shield"],
  "#33c3ff": ["BlueBooster"],
  Special: ["Booster", "VortexBumper"],
};
let specialEntityColorFunctions = {
  Strawberry: (entity) => {
    return entity.properties.golden ? "#ffd700" : "#bb0000";
  },
  Refill: (entity) => {
    return entity.properties.twoDashes ? "#fa7ded" : "#aedc5e";
  },
  RefillWall: (entity) => {
    return entity.properties.twoDashes ? "#fa7ded" : "#aedc5e";
  },
  Cloud: (entity) => {
    return entity.properties.fragile ? "#ffa5ff" : "#77cde3";
  },
  Booster: (entity) => {
    return entity.properties.red ? "#d3170a" : "#219974";
  },
  CassetteBlock: (entity) => {
    return entity.properties.color;
  },
  WonkyCassetteBlock: (entity) => {
    return entity.properties.color;
  },
  VortexBumper: (entity) => {
    return entity.properties.twoDashes ? "#fa7ded" : entity.properties.oneUse ? "#d3170a" : "#0f58d9";
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

  SwapBlock: [0.8, "S"],
  ToggleSwapBlock: [0.8, "S"],

  BounceBlock: [0.2, "Core\nBlock"],
  CrushBlock: [0.25, "Kevin"],

  Refill: [0.2, (entity) => (entity.properties.oneUse ? "" : "Refill")],
  RefillWall: [0.25, (entity) => (entity.properties.oneUse ? "" : "Refill\nWall")],

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
    (entity) => (entity.properties.oneUse ? 0.125 : 0.175),
    (entity) => (entity.properties.oneUse ? "Vortex\nBumper\n(One Use)" : "Vortex\nBumper"),
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
let konvaRoomOtherEntitiesLayer = null;
let konvaMaddyHitboxLayer = null;
let konvaTooltipLayer = null;
let konvaLowPrioTooltipLayer = null;
let konvaPositionLayer = null;
//#endregion

//#region Canvas Init
function redrawCanvas() {
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
  konvaRoomOtherEntitiesLayer = new Konva.Layer({ listening: false });
  konvaMaddyHitboxLayer = new Konva.Layer({ listening: false });
  konvaPositionLayer = new Konva.Layer({ listening: true });
  konvaLowPrioTooltipLayer = new Konva.Layer({ listening: false });
  konvaTooltipLayer = new Konva.Layer({ listening: false });

  // add the layer to the stage
  konvaStage.add(konvaRoomLayoutLayer);
  konvaStage.add(konvaRoomEntitiesLayer);
  konvaStage.add(konvaRoomOtherEntitiesLayer);
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

function createAllElements() {
  findRelevantRooms();

  drawRoomBounds();
  drawStaticEntities();
  drawPhysicsLog();

  // draw the image
  konvaPositionLayer.draw();
}

let relevantRoomNames = [];
function findRelevantRooms() {
  relevantRoomNames = [];

  //Go through all frames
  for (let i = 0; i < physicsLogFrames.length; i++) {
    let frame = physicsLogFrames[i];

    if (
      (settings.frameMin != -1 && frame.frameNumber < settings.frameMin) ||
      (settings.frameMax != -1 && frame.frameNumber > settings.frameMax)
    ) {
      continue;
    }

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
        frame.positionX <= levelBounds.x + levelBounds.width &&
        frame.positionY >= levelBounds.y &&
        frame.positionY <= levelBounds.y + levelBounds.height
      ) {
        relevantRoomNames.push(roomLayout.debugRoomName);
        break;
      }
    }
  }
}
//#endregion

//#region Canvas Drawing
function drawRoomBounds() {
  let tileSize = 8;
  let tileOffsetX = 0;
  let tileOffsetY = 0.5;

  roomLayouts.forEach((roomLayout) => {
    let debugRoomName = roomLayout.debugRoomName;

    if (settings.showOnlyRelevantRooms && !relevantRoomNames.includes(debugRoomName)) {
      return;
    }

    let levelBounds = roomLayout.levelBounds;
    let solidTiles = roomLayout.solidTiles; //2d array of bools, whether tiles are solid or not

    konvaRoomLayoutLayer.add(
      new Konva.Rect({
        x: levelBounds.x + tileOffsetX,
        y: levelBounds.y + tileOffsetY,
        width: levelBounds.width,
        height: levelBounds.height,
        stroke: "white",
        strokeWidth: 0.5,
      })
    );

    if (settings.showRoomNames) {
      konvaRoomLayoutLayer.add(
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

    drawSolidTileOutlines(solidTiles, levelBounds);
  });
}


let entityCounts = {};
function drawStaticEntities() {
  entityCounts = {};
  roomLayouts.forEach((roomLayout) => {
    let debugRoomName = roomLayout.debugRoomName;

    if (settings.showOnlyRelevantRooms && !relevantRoomNames.includes(debugRoomName)) {
      return;
    }

    let levelBounds = roomLayout.levelBounds;
    let entities = roomLayout.entities;

    entities.forEach((entity) => {
      let entityX = entity.position.x + entitiesOffsetX;
      let entityY = entity.position.y + entitiesOffsetY;

      if (
        entity.type === "CrystalStaticSpinner" ||
        entity.type === "DustStaticSpinner" ||
        entity.type === "CustomSpinner"
      ) {
        //Cull offscreen spinners
        if (
          entityX < levelBounds.x - spinnerRadius ||
          entityX > levelBounds.x + levelBounds.width + spinnerRadius ||
          entityY < levelBounds.y - spinnerRadius ||
          entityY > levelBounds.y + levelBounds.height + spinnerRadius
        ) {
          return;
        }

        //Draw white circle with width 6 on entity position
        let hitcircle = { x: 0, y: 0, radius: 6 };
        konvaRoomEntitiesLayer.add(createHitCircle(entityX, entityY, hitcircle, "white", 0.5));

        if (settings.showSpinnerRectangle) {
          //Draw white rectangle with width 16, height 4, x offset -8 and y offset -3 on entity position
          let hitbox = { x: -8, y: -3, width: 16, height: 4 };
          konvaRoomEntitiesLayer.add(createHitbox(entityX, entityY, hitbox, "white", 0.5));
        }
      }
    });

    entities.forEach((entity) => {
      //add type to entityCounts
      if (entityCounts[entity.type] === undefined) {
        entityCounts[entity.type] = 0;
      }
      entityCounts[entity.type]++;

      let entityX = entity.position.x + entitiesOffsetX;
      let entityY = entity.position.y + entitiesOffsetY;

      //If the entity type is in any of the value arrays in the hitboxEntityNames map
      let entityColor = Object.keys(hitboxEntityNames).find((color) =>
        hitboxEntityNames[color].includes(entity.type)
      );
      if (entityColor !== undefined) {
        let hitbox = entity.properties.hitbox;

        if (entityColor === "Special") {
          entityColor = specialEntityColorFunctions[entity.type](entity);
        }

        let dash = [];
        if (entity.type in entityNamesDashedOutline) {
          let dashValue = entityNamesDashedOutline[entity.type];
          dash = [dashValue, dashValue];
        }

        //Draw hitbox
        konvaRoomEntitiesLayer.add(createHitbox(entityX, entityY, hitbox, entityColor, 0.25, dash));
        drawSimpleHitboxAdditionalShape(entityColor, entityX, entityY, entity);
      }

      //If the entity type is in any of the value arrays in the hitcircleEntityNames map
      entityColor = Object.keys(hitcircleEntityNames).find((color) =>
        hitcircleEntityNames[color].includes(entity.type)
      );
      if (entityColor !== undefined) {
        let hitcircle = entity.properties.hitcircle;

        if (entityColor === "Special") {
          entityColor = specialEntityColorFunctions[entity.type](entity);
        }

        let dash = [];
        if (entity.type in entityNamesDashedOutline) {
          let dashValue = entityNamesDashedOutline[entity.type];
          dash = [dashValue, dashValue];
        }

        //Draw hitcircle
        konvaRoomEntitiesLayer.add(createHitCircle(entityX, entityY, hitcircle, entityColor, 0.25, dash));
        drawSimpleHitcircleAdditionalShape(entityColor, entityX, entityY, entity);
      }

      //Entity Type: FinalBoos
      if (entity.type === "FinalBoss" || entity.type === "BadelineBoost" || entity.type === "FlingBird") {
        let hitcircle = entity.properties.hitcircle;
        let color = entity.type === "FlingBird" ? "cyan" : "#ff00ff";

        //draw the initial position
        konvaRoomEntitiesLayer.add(createHitCircle(entityX, entityY, hitcircle, color));

        //loop through properties.nodes, and draw the circle at each node, and a line between each node
        let nodes = entity.properties.nodes;
        let previousNode = null;
        for (let i = 0; i < nodes.length; i++) {
          if (entity.type === "FinalBoss" && i === nodes.length - 1) continue;

          let node = nodes[i];
          let nodeX = node.x + hitcircle.x;
          let nodeY = node.y + hitcircle.y;

          //Draw circle on node position
          konvaRoomEntitiesLayer.add(createHitCircle(node.x, node.y, hitcircle, color));

          //Draw arrow to previous node
          if (previousNode !== null) {
            konvaRoomEntitiesLayer.add(
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
            konvaRoomEntitiesLayer.add(
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
      }
    });

    let otherEntities = roomLayout.otherEntities;
    otherEntities.forEach((entity) => {
      if (!entity.properties.hasCollider) return;

      let entityColor = entity.properties.isSolid ? "red" : "white";
      let entityX = entity.position.x + entitiesOffsetX;
      let entityY = entity.position.y + entitiesOffsetY;

      let properties = entity.properties;

      let textSplit = entity.type.split(/(?=[A-Z])/);
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
      if ("hitbox" in properties) {
        konvaRoomOtherEntitiesLayer.add(createHitbox(entityX, entityY, properties.hitbox, entityColor));
        //Text
        let fontSize = Math.min(properties.hitbox.width, properties.hitbox.height) * size;
        let offsetY = fontSize * 0.1;
        konvaRoomOtherEntitiesLayer.add(
          createLetterEntityText(entityX, entityY + offsetY, properties.hitbox, text, fontSize, entityColor)
        );
      }

      //Draw hitcircle
      if ("hitcircle" in properties) {
        konvaRoomOtherEntitiesLayer.add(createHitCircle(entityX, entityY, properties.hitcircle, entityColor));
        //Text
        let fontSize = properties.hitcircle.radius * 2 * size * 0.8;
        let offsetY = fontSize * 0.1;
        konvaRoomOtherEntitiesLayer.add(
          createLetterEntityTextCircle(
            entityX,
            entityY + offsetY,
            properties.hitcircle,
            text,
            fontSize,
            entityColor
          )
        );
      }

      //Draw colliderList
      if ("colliderList" in properties) {
        properties.colliderList.forEach((collider) => {
          if (collider.type === "hitbox") {
            konvaRoomOtherEntitiesLayer.add(createHitbox(entityX, entityY, collider.hitbox, entityColor));
            if (entity.type.indexOf("Spinner") !== -1) return;
            //Text
            let fontSize = Math.min(collider.hitbox.width, collider.hitbox.height) * size;
            let offsetY = fontSize * 0.1;
            konvaRoomOtherEntitiesLayer.add(
              createLetterEntityText(entityX, entityY + offsetY, collider.hitbox, text, fontSize, entityColor)
            );
          } else if (collider.type === "hitcircle") {
            konvaRoomOtherEntitiesLayer.add(
              createHitCircle(entityX, entityY, collider.hitcircle, entityColor)
            );
            //Text
            let fontSize = collider.hitcircle.radius * 2 * size * 0.8;
            let offsetY = fontSize * 0.1;
            konvaRoomOtherEntitiesLayer.add(
              createLetterEntityTextCircle(
                entityX,
                entityY + offsetY,
                collider.hitcircle,
                text,
                fontSize,
                entityColor
              )
            );
          }
        });
      }
    });
  });
}

function createHitbox(posX, posY, hitbox, entityColor, strokeWidth = 0.25, dash = []) {
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

function createHitCircle(posX, posY, hitcircle, entityColor, strokeWidth = 0.25, dash = []) {
  return new Konva.Circle({
    x: posX + hitcircle.x,
    y: posY + hitcircle.y,
    radius: hitcircle.radius,
    stroke: entityColor,
    strokeWidth: strokeWidth,
    dash: dash,
  });
}

function drawSimpleHitboxAdditionalShape(entityColor, entityX, entityY, entity) {
  let hitbox = entity.properties.hitbox;

  //if entity.type is in entityNamesText, use the text properties from there
  if (entity.type in entityNamesText) {
    let textProperties = entityNamesText[entity.type];
    let size = textProperties[0];
    if (typeof size === "function") {
      size = size(entity);
    }
    let text = textProperties[1];
    if (typeof text === "function") {
      text = text(entity);
    }

    if (text !== "") {
      let fontSize = Math.min(hitbox.width, hitbox.height) * size;
      let offsetY = fontSize * 0.1;

      konvaRoomEntitiesLayer.add(
        createLetterEntityText(entityX, entityY + offsetY, hitbox, text, fontSize, entityColor)
      );
    }
  }

  if (entity.type === "Strawberry" || entity.type === "SilverBerry") {
    konvaRoomEntitiesLayer.add(
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
      entity.type
    )
  ) {
    let offsetX = 0,
      offsetY = 0;
    if (entity.type === "SwitchGate" || entity.type === "FlagSwitchGate") {
      offsetX = hitbox.width / 2;
      offsetY = hitbox.height / 2;
    }
    konvaRoomEntitiesLayer.add(
      new Konva.Circle({
        x: entityX + offsetX,
        y: entityY + offsetY,
        radius: 6,
        stroke: entityColor,
        strokeWidth: 1,
      })
    );
  }
  if (entity.type === "MovingTouchSwitch") {
    let nodes = entity.properties.nodes;
    //Draw the additional nodes
    let previousNode = null;
    nodes.forEach((node) => {
      konvaRoomEntitiesLayer.add(
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

      konvaRoomEntitiesLayer.add(
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

  if ((entity.type === "Refill" || entity.type === "RefillWall") && entity.properties.oneUse === true) {
    //Draw a cross on the refill
    let offset = Math.min(hitbox.width, hitbox.height) * 0.1;
    konvaRoomEntitiesLayer.add(
      new Konva.Line({
        points: [
          entityX + hitbox.x + offset,
          entityY + hitbox.y + offset,
          entityX + hitbox.x + hitbox.width - offset,
          entityY + hitbox.y + hitbox.height - offset,
        ],
        stroke: entityColor,
        strokeWidth: 0.25,
      })
    );
    konvaRoomEntitiesLayer.add(
      new Konva.Line({
        points: [
          entityX + hitbox.x + hitbox.width - offset,
          entityY + hitbox.y + offset,
          entityX + hitbox.x + offset,
          entityY + hitbox.y + hitbox.height - offset,
        ],
        stroke: entityColor,
        strokeWidth: 0.25,
      })
    );
  }

  if (
    entity.type === "MoveBlock" ||
    entity.type === "ConnectedMoveBlock" ||
    entity.type === "DreamMoveBlock"
  ) {
    let direction = entity.properties.direction; //Left, Right, Up, Down
    //Draw an arrow pointing in the direction, in center of the hitbox
    let strokeWidth = Math.min(hitbox.width, hitbox.height) * 0.2;
    let pointerSize = strokeWidth * 0.45;

    let offset = Math.min(hitbox.width, hitbox.height) * 0.15;
    let arrowOffset = pointerSize * 3.5;

    let points = [];
    if (direction === "Left") {
      points = [
        entityX + hitbox.x + hitbox.width - offset,
        entityY + hitbox.y + hitbox.height / 2,
        entityX + hitbox.x + offset + arrowOffset,
        entityY + hitbox.y + hitbox.height / 2,
      ];
    } else if (direction === "Right") {
      points = [
        entityX + hitbox.x + offset,
        entityY + hitbox.y + hitbox.height / 2,
        entityX + hitbox.x + hitbox.width - offset - arrowOffset,
        entityY + hitbox.y + hitbox.height / 2,
      ];
    } else if (direction === "Up") {
      points = [
        entityX + hitbox.x + hitbox.width / 2,
        entityY + hitbox.y + hitbox.height - offset,
        entityX + hitbox.x + hitbox.width / 2,
        entityY + hitbox.y + offset + arrowOffset,
      ];
    } else if (direction === "Down") {
      points = [
        entityX + hitbox.x + hitbox.width / 2,
        entityY + hitbox.y + offset,
        entityX + hitbox.x + hitbox.width / 2,
        entityY + hitbox.y + hitbox.height - offset - arrowOffset,
      ];
    }

    konvaRoomEntitiesLayer.add(
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

  if (entity.type == "Puffer" || entity.type == "StaticPuffer" || entity.type == "SpeedPreservePuffer") {
    let circleRadius = 32;
    //Draw a top half circle on the entity position using svg Path
    konvaRoomEntitiesLayer.add(
      new Konva.Path({
        x: entityX,
        y: entityY + hitbox.y + hitbox.height,
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

  if (entity.type == "ClutterBlockBase" || entity.type == "ClutterSwitch") {
    let fontSize = Math.min(hitbox.width, hitbox.height) * 0.15;
    let offsetY = fontSize * 0.1;
    let clutterName;
    if (entity.properties.color === "Red") {
      clutterName = "Towels";
    } else if (entity.properties.color === "Yellow") {
      clutterName = "Boxes";
    } else if (entity.properties.color === "Green") {
      clutterName = "Books";
    }

    if (entity.type == "ClutterSwitch") {
      clutterName = "Switch:\n" + clutterName;
    }

    konvaRoomEntitiesLayer.add(
      createLetterEntityText(entityX, entityY + offsetY, hitbox, clutterName, fontSize, entityColor)
    );
  }
}

function drawSimpleHitcircleAdditionalShape(entityColor, entityX, entityY, entity) {
  let hitcircle = entity.properties.hitcircle;

  if (entity.type in entityNamesText) {
    let textProperties = entityNamesText[entity.type];
    let size = textProperties[0];
    if (typeof size === "function") {
      size = size(entity);
    }
    let text = textProperties[1];
    if (typeof text === "function") {
      text = text(entity);
    }

    if (text !== "") {
      let fontSize = hitcircle.radius * 2 * size;
      let offsetY = fontSize * 0.1;

      konvaRoomEntitiesLayer.add(
        createLetterEntityTextCircle(entityX, entityY + offsetY, hitcircle, text, fontSize, entityColor)
      );
    }
  }
}

function createLetterEntityText(entityX, entityY, hitbox, text, fontSize, entityColor) {
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

function createLetterEntityTextCircle(entityX, entityY, hitcircle, text, fontSize, entityColor) {
  let hitbox = {
    x: hitcircle.x,
    y: hitcircle.y,
    width: hitcircle.radius * 2,
    height: hitcircle.radius * 2,
  };
  return createLetterEntityText(
    entityX - hitcircle.radius,
    entityY - hitcircle.radius,
    hitbox,
    text,
    fontSize,
    entityColor
  );
}

function drawPhysicsLog() {
  let previousFrame = null;
  pointLabelPreviousValue = null;

  for (let i = 0; i < physicsLogFrames.length; i++) {
    let frame = physicsLogFrames[i];

    if (settings.frameMin != -1 && frame.frameNumber < settings.frameMin) continue;
    if (settings.frameMax != -1 && frame.frameNumber > settings.frameMax) break;

    let rasterizedPos = getRasterziedPosition(frame);
    let posX = rasterizedPos.positionX;
    let posY = rasterizedPos.positionY;

    //Draw circle on position
    let posCircle = new Konva.Circle({
      x: posX,
      y: posY,
      radius: 1.25,
      fill: getFramePointColor(frame),
      stroke: "black",
      strokeWidth: 0,
    });
    konvaPositionLayer.add(posCircle);

    let nextFrame = null;
    if (i < physicsLogFrames.length - 1) {
      nextFrame = physicsLogFrames[i + 1];
    }

    createPhysicsTooltip(posCircle, frame, previousFrame, nextFrame);

    drawAdditionalFrameData(frame, previousFrame);
    previousFrame = frame;
  }
}

function drawAdditionalFrameData(frame, previousFrame) {
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
      konvaLowPrioTooltipLayer.add(
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
    drawPointLabels(frame, previousFrame);
  }
}

function drawPointLabels(frame, previousFrame) {
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
    return;
  }

  let rasterizedPos = getRasterziedPosition(frame);
  let posX = rasterizedPos.positionX;
  let posY = rasterizedPos.positionY;
  let boxWidth = 30;
  let fontSize = 1.5;
  konvaLowPrioTooltipLayer.add(
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
}
//#endregion

//#region Tooltips
function createPhysicsTooltip(shape, frame, previousFrame, nextFrame) {
  let rasterizedPos = getRasterziedPosition(frame);
  let posX = rasterizedPos.positionX;
  let posY = rasterizedPos.positionY;

  let maddyWidth = 7;
  let maddyHeight = 10;

  if (frame.flags.includes("Ducking")) {
    maddyHeight = 5;
  }

  //Draw maddy's hitbox as rectangle
  let maddyHitbox = new Konva.Rect({
    x: posX - maddyWidth / 2,
    y: posY - maddyHeight,
    width: maddyWidth,
    height: maddyHeight,
    stroke: "red",
    strokeWidth: 0.125,
    visible: false,
  });
  konvaMaddyHitboxLayer.add(maddyHitbox);

  //Draw maddy's hurtbox as green rectangle
  let maddyHurtbox = new Konva.Rect({
    x: posX - maddyWidth / 2,
    y: posY - maddyHeight,
    width: maddyWidth,
    height: maddyHeight - 2,
    stroke: "green",
    strokeWidth: 0.125,
    visible: false,
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
  tooltipRect.width(tooltipText.width() + tooltipMargin * 2);
  tooltipRect.height(tooltipText.height() + subpixelDisplayHeight + analogDisplayHeight + tooltipMargin * 2);

  konvaGroupTooltip.add(tooltipRect);
  konvaGroupTooltip.add(konvaGroupTooltipInfo);

  konvaGroupTooltip.y(konvaGroupTooltip.y() - tooltipRect.height());

  shape.keepTooltipOpen = false;

  shape.on("click", function () {
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
    maddyHitbox.visible(true);
    maddyHurtbox.visible(true);
    konvaGroupTooltip.visible(true);
  });
  shape.on("mouseleave", function () {
    if (!shape.keepTooltipOpen) {
      shape.strokeWidth(0);
      maddyHitbox.visible(false);
      maddyHurtbox.visible(false);
      konvaGroupTooltip.visible(false);
    }
  });
}
//#endregion

//#region Solid Tiles
function drawSolidTileOutlines(solidTiles, levelBounds) {
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
        konvaRoomLayoutLayer.add(createEdgeLine([topleftX, topleftY, topleftX + tileSize, topleftY]));
      }
      if (edges & Edge.Right) {
        konvaRoomLayoutLayer.add(
          createEdgeLine([topleftX + tileSize, topleftY, topleftX + tileSize, topleftY + tileSize])
        );
      }
      if (edges & Edge.Bottom) {
        konvaRoomLayoutLayer.add(
          createEdgeLine([topleftX, topleftY + tileSize, topleftX + tileSize, topleftY + tileSize])
        );
      }
      if (edges & Edge.Left) {
        konvaRoomLayoutLayer.add(createEdgeLine([topleftX, topleftY, topleftX, topleftY + tileSize]));
      }
    }
  }
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