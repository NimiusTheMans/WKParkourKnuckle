## Description
This mod adds various movement types that attempt to aid you in your attempt to escape.

*Clarification Note: AI was used in partial to help with the development of the code. The other parts of the code were created by me. All others assets (art, visuals, descriptions, etc.) and ideas were completely created by myself (and with some help from the community).*

To access the settings, press the right arrow button at the top of the skill tree. This will take you to the options page. Every 100 meters you climb adds 10 credits to your total amount of credits. A bonus is rewarded if you pass your highest climb that was set during your exit or death, which is also given every 100 meters. You can check "parkourprogress.cfg" in your BepInEx config to check and change your progress data.

**Report any bugs or request changes in the official White Knuckle Discord server or in the GitHub repository.**

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

# Donation
Donate to help support me and my making of mods!

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/K3N62124UD)

## Version 1.4.8 SP
### Main Changes
(+)
* Sliding now throws objects based on the player's velocity.
* Changed the descriptions of toggles to better reflect what they do.
* Fixed a bug where abilities never restarted when restarting a scene.
* Fixed a bug where the visual loading screen would pop up while in a session.

### UI Changes
(+)
* Fixed a bug where the screen glow would show up even when wall running wasn't activated.

### Code Changes
(+)
* Added null-conditional operators to prevent potential NullReferenceExceptions when setting GameObject states.
* Replaced some direct assignments with null-conditional operators to make code more concise.
* Changed some SetActive calls to use null-conditional operators to prevent potential NullReferenceExceptions.
* Added UpdateCurrencyDisplay call when the player object is present and the currency icon is active.
* Updated plugin version from 1.2.1 to 1.4.8.
* Changed beginning log messages to better reflect what is happening in the background.
* Replaced a mismatch between functions to use Postfix instead of Prefix and Postfix.
* Replaced null checks before calling SetActive on GameObjects with easier to read versions.

(-)
* Removed unused variables code blocks.
* Removed duplicate code blocks for handling skill tree panel and options content visibility. 
