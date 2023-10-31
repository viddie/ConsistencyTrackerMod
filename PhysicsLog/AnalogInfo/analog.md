# How does analog input work?

My understanding of how the analog input pipeline works:

- User moves the stick
- Platform dependent deadzone is applied (very small around neutral)
- Gamepad circular deadzone around neutral (threshold 0.25)
- For moving (not dash): Another big rectangular deadzone is applied
- Then the player moves/dashes

## Platform Deadzone

For me the controller deadzone is dependent on FNAPlatform functions, so idk how that is exactly calculated, but its also only a very small region around the neutral point of the stick from what I could manually test with a raw readout of the `GamePad.GetState(0, GamePadDeadZone.None)`.

I think further down the pipeline this controller deadzone is subtracted from the actual stick movement value, so if Gamepad deadzone check sees an axis value of just over `0`, that means it already passed the controller deadzone check.

## Gamepad Deadzone

Tested with CelesteTAS, but it didn't allow any inputs with an amplitude of less than 0.26 (which is +0.01 over the deadzone threshold), so I am assuming this is the minimum you can go. I tried testing manually for a bit as well but didn't manage to get an amplitude lower than that either.

<img src=".\gamepad-deadzone.png" alt="image-20231030044146088" style="zoom:50%;" />

## Move Deadzone

The move deadzone looks like this. 

<img src=".\move-deadzone.png" alt="image-20231030015832270" style="zoom: 50%;" />

The use cases:

- Moving horizontally (left/right)
- Climbing (up/down)
- Fast falling (down)

Looking at the big deadzone on the vertical input, it kinda makes sense why fast falling while also holding a horizontal direction feels pretty ass on analog. You have to hit exactly the area marked in red if you want to move right while fast falling!

<img src=".\move-downright.png" alt="image-20231030043445782" style="zoom:50%;" />

## Dash Direction

Fun fact: Dash direction areas on analog are NOT vertically symmetric, only horizontally. This is how it looks:

<img src=".\dash-directions.png" alt="image-20231030043649596" style="zoom:50%;" /> 

In the top half a range of 17.5° from an axis registers as a directional input, while in the bottom half it has to be 22.5° from an axis. The following table shows all dash directions and required angles (0° is straight UP, like in CelesteTAS)

| Direction  | Angle Start* | Angle End* | Range | %      |
| ---------- | ------------ | ---------- | ----- | ------ |
| UP         | 342.5        | 17.5       | 35°   | 9.72%  |
| UP+RIGHT   | 17.5         | 72.5       | 55°   | 15.28% |
| RIGHT      | 72.5         | 112.5      | 40°   | 11.11% |
| DOWN+RIGHT | 112.5        | 157.5      | 45°   | 12.50% |
| DOWN       | 157.5        | 202.5      | 45°   | 12.50% |
| DOWN+LEFT  | 202.5        | 247.5      | 45°   | 12.50% |
| LEFT       | 247.5        | 287.5      | 40°   | 11.11% |
| UP+LEFT    | 287.5        | 342.5      | 55°   | 15.28% |

*These angles dont line up with what the mod `Celeste Analog Viewer` shows, as that mod simply uses the angle of the `Aim` vector, calculated through `Math.Atan2(vector.Y, vector.X)`. CAV's 0° angle is RIGHT, 90° is UP, 180°/-180° is LEFT, -90° is DOWN.

This means this older image that was floating around on analog dash directions is not exactly correct:

<img src=".\dash-directions-old.png" alt="image-20231030051826647" style="zoom:67%;" />

| Direction | Middle Angle (CelesteTAS) | Middle Angle (Vector.Angle) |
| --------- | ------------------------- | --------------------------- |
| UP        | 0                         | 90                          |
| RIGHT     | 90                        | 0                           |
| DOWN      | 180                       | -90                         |
| LEFT      | 270                       | 180/-180                    |

``````
CelesetTAS_Angle = (-Vector.Angle() + 90) mod 360
``````

