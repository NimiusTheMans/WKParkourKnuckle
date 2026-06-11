## Description
This mod adds various movement types that attempt to aid you in your attempt to escape.

*Developer note: AI was used in partial to help with the development of the code.*

To access the settings, press the right arrow button at the top of the skill tree. This will take you to the options page. Every 100 meters you climb adds 10 credits to your total amount of credits. A bonus is rewarded if you pass your highest climb that was set during your exit or death, which is also given every 100 meters. You can check "parkourprogress.cfg" in your BepInEx config to check and change your progress data.

## Unlock Abilities
**Use Height Currency to Buy Skills**

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Pictures/CurrencyPicture.png?raw=true)

**Buy Skills in the Skill Tree**

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Pictures/SkillTreePicture.png?raw=true)

## Wall Running
**Horizontal Wall Running**

You can run on the side of walls by standing close to a wall and double tapping and holding the spacebar and holding A or D depending on which side of the wall you are standing near. This consumes very little hand stamina.

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Gifs/WallRunHoriz.gif?raw=true)

**Vertical Wall Running**

You have the ability to run up to walls and climb them without needing any tools whatsoever. Double tap and hold spacebar and hold W at the same time while facing a wall to begin a vertical wall run. This consumes a moderate amount of hand stamina.

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Gifs/WallRunVert.gif?raw=true)

**Wall Run Boost**

While vertically wall running, you can release W and quickly tap spacebar to kick off of the wall and give yourself a backwards boost. This does not use up your stamina, however, the power of the boost is determined by your hand stamina.

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Gifs/WallRunVertJump.gif?raw=true)

## Monster Leap
Holding G will cause you to charge up a leap. The longer you charge your leap, the further your leap will go, but your stamina will be consumed the more you charge your leap.

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Gifs/MonsterLeap.gif?raw=true)

## Quick Turning
You can quick turn 180 degrees at any point with no debuff by tapping X. This may help save time when you need to turn around at fast intervals.

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Gifs/QuickTurn.gif?raw=true)

## Wall Kick
While your back is turned away from a wall, hold S and press the spacebar to kick off of that wall. holding down S after the kick will allow you to control your distance while in the air.

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Gifs/WallKick.gif?raw=true)

## Sliding
While running (or moving at a high velocity), hold down the crouch key to slide.

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Gifs/Slide.gif?raw=true)

**Slide Jumping**

During a slide, you can jump to gain a boost. This boost does consume hand stamina. As the duration of your slide grows, less power will be put into the boost, however, less stamina will be taken.

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Gifs/SlideJump.gif?raw=true)

## Rolling
While you are falling from great height, hold down your crouch button to initiate a roll when you land. This takes no stamina and prevents fall damage from the landing impact.

![](https://github.com/NimiusTheMans/WKParkourKnuckle/blob/main/Assets/Gifs/Roll.gif?raw=true)

## Version 1.4.7 SP
### Main Changes
(+)
* Added new camera options for parkour: “Use Parkour FOV” and “Use Parkour Shake”.
* Added a new option group with a new option: "Use Wall Run Helper" - this will create a glow on the sides of your screen when your wall runs are ready.
* Added multipliers to each skill that change depending on the amount of injectors are in effect. (maximum of 5)
* Added a reset to the leap's charging state when the player leaves the ground while generating charge.
* Fixed the bug where stamina is lost even though the player has used pills and injectors. (Wall running still drains stamina, but with less intensity)

(-)
* Replaced separate currency and skill config maps with a single shared skill config map for all types of values instead of individual ones per config.
* Removed the ability to wall run while holding on to objects to prevent accidental wall running while climbing.

### UI Changes
(+)
* Added on-screen toggles for camera FOV and camera shake in the options menu.
* Added ability cooldown UI elements to help track ability cooldown states during a session.
* Added a height display text element to help track the highest climb from previous sessions.
* Added the ability to check current amount of credits and highest climb in the pause menu and death screen menu.
* Added new separators in the options menu for Camera Settings and UI Settings.
* Fixed the bug where F4 would re-enable the open button during a session.

(-)
* Replaced periodic player lookup with cached player presence checks to reduce lag. (difference on my pc went from a maximum of 50 fps to 144 fps with VSync enabled on both tests)