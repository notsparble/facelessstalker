# The Faceless Stalker [v69]

![Slender Image 1](https://i.imgur.com/jLTNNjM.png)

Adds the famous Slenderman as well as collectable pages to Lethal Company!

Mysterious pages have appeared inside the facility... They depict what seems like a tall humanoid entity - Weirdly enough, the creature appears to be lacking any facial features.
According to the employer, these pages are highly valuable, but any employees collecting them are advised to 'regularily check behind their backs'.

Why? No one knows, but there seems to be more to them than meets the eye.

Anyways, what could possibly go wrong?


# Config

Note that the Faceless Stalker has many config options regarding his behaviour as well as the spawn rarity of pages and various other elements.

![Slender Image 2](https://i.imgur.com/l7Jvqxc.png)

# Enemy AI (Spoilers!)

!!! It is highly recommended to NOT check this section and rather explore this mod by yourselves, as the pages already hint at the behaviour of the Faceless Stalker !!!


The enemy AI has four behaviour stages, and, in short, works like this:
As soon as any player picks up a page, the Faceless Stalker (Slenderman) will spawn on the map.
First of all, it will choose a player to haunt, after which it will regularily search for spots on the map with eye contact to the haunted player, but outside of their FOV.
As soon as it has found a spot, a deep bass sound may or may not be heard, indicating the presence of the Stalker. 
During its stalking phase, only the haunted player will be able to see the Stalker; while everybody will be able to see it during a chase.
If not looked at, it will creep closer towards the haunted player in intervals - when looking at the Stalker, depending on the distance to the player, it will either vanish, or start chasing the player.
The closer the Stalker is to the player, the higher the chances of initiating a chase are, so regularily check your back to keep him distant.
During a chase, any non-haunted player fast enough can try to intercept the Stalker, which will result in the entity disappearing and haunting the intercepting player instead.

![Slender Image 3](https://i.imgur.com/XASWwjj.png)

# Credits

The slenderman sounds are from [Parsec Productions' Slender: The Eight Pages](http://www.parsecproductions.net/).


Special Thanks to:

Max - for the [LC Modding Wiki](https://lethal.wiki/).

Evaisa - for [LethalLib](https://thunderstore.io/c/lethal-company/p/Evaisa/LethalLib/) and [HookGenPatcher](https://thunderstore.io/c/lethal-company/p/Evaisa/HookGenPatcher/).

Hamunii - for the [LC Enemy mod template](https://github.com/Hamunii/LC-ExampleEnemy) as well as helping me fixing most bugs.

DarthFigo - for helping me understand how the Server - Client System works as well helping me fixing most bugs.

Nat - for making the new and improved page textures.

As well as the Lethal Company Modding Discord for helping me with my questions.

![Slender Image 4](https://i.imgur.com/7ccz0Ej.png)

## CHANGELOG 🕗

Join the Lethal Company Modding Discord or [my Modding Discord](https://discord.gg/jkTY5z9RKE) for questions and to report bugs or incompatibilities.

- ***v1.2.0:***
     - Updated to v69
     - Added four new Page Items
     - Updated textures for the old four pages (thanks Nat!)
     - Pages can be inspected now
- ***v1.1.9:***
     - Updated to v64
     - Tweaked First-Time Spawning Sound
- ***v1.1.8:***
     - Updated to v61
- ***v1.1.7:***
     - Updated to v60
- ***v1.1.6:***
     - Updated to v56
- ***v1.1.5:***
     - New config option: Custom Moon + Weight spawning options for pages (finally)
     - Bugfix: No more infinite switches when all players inside ship w/ doors closed (will stop switching after 10 times now)
- ***v1.1.4:***
     - New config option: Switch Targets with closed doors
     - Slenderman can now switch targets when the haunted player is inside the ship and its doors are closed during a chase
- ***v1.1.3:***
     - Updated to v50
     - New config option: Chance for stalker's spot found-sound
     - Jumpscare Sound now decreases w/ distance when spotted for the first time
- ***v1.1.2:***
     - Removed unnecessary console logs
     - Updated Readme
- ***v1.1.1:***
     - Fixed bug where Slenderman would spawn twice
- ***v1.1.0:***
     - Additional Slenderman sounds added
     - When Slenderman first spawns in, there's an audible noise for every player now, alerting them of the Stalker's presence (configurable)
     - Added Chance for Slenderman to make a static noise when approaching the player (configurable)
     - If spotted more than 8 times, Slenderman now has a chance to switch targets
     - Decreased maximum distance for flickering lights & fear level increase from 80m to 70m
     - Bugfix: Fixed bug where jumpscare sound would be cut off by disppearing and chase Sounds
     - Bugfix: Fixed a bug where Slenderman wouldn't spawn when a page was picked up another day during which he spawned
     - Config:
	     - Decreased Absent State Cooldown default value from 45 seconds to 35 seconds
	     - Increased Chase Duration default value from 15  seconds to 20  seconds
- ***v1.0.9:***
     - Slenderman will now always vanish the first time he's seen (for balancing reasons)
     - Increased minimum spawn distance to the player inside the facility by 10 meters (previously 15, now 25)
     - Fixed a bug where Slenderman's chance to start a chase with the player wouldn't increase with the times seen by the player
- ***v1.0.8:***
     - Added config option to toggle if Slenderman flips the lights breaker after being seen the first time
- ***v1.0.7:***
     - New improved Slenderman model
     - Four new Slenderman animations
     - Slightly decreased creeping closer-timer when there's no LOS to the haunted player
     - New config option: Chase Duration
     - New config option: Spawn Cooldown Duration
     - Slenderman now flips the light breaker when he's seen the first time
     - Bugfix: The chase noise now stops correctly after the chase has ended
- ***v1.0.6:***
     - Decreased interval time of haunting position searches inside the facility by 50%
- ***v1.0.5:***
     - New config options   
- ***v1.0.4:***
     - New config option: Stalking Interval Duration
     - Slightly decreased the cooldown after the Vanishing state (before: 50 seconds, now: 45 seconds)
     - Slightly increased chances of Slenderman spawning inside the facility
     - Fixed a bug where picking up pages would not spawn the Slenderman    
- ***v1.0.3:***
     - Decreased base spawn interval duration from 25 to 20 seconds
     - Added a config option to allow custom interval duration
     - Added a config option to allow natural spawning of Slenderman
     - Pages will be "depleted" upon collection, removing the ability to spawn Slenderman with them on later days
     - Bugfix: Chase audio won't be heard by players far away from Slenderman anymore
     - Bugfix: Slenderman won't spawn on Gordion anymore
- ***v1.0.2:***
     - Increased Slenderman chase speed & acceleration during chases by a bit
     - Added a way for Slenderman to change targets outside of a chase
     - Fixed a bug where you could not collect pages from the ship locker due to their grab-colliders being too small.
     - Removed unnecessary console debug logs
     - Fixed a bug where multiple Slenderman enemies could spawn after pages had been picked up
- ***v1.0.1:*** 
     - Description Fix (duh...)
- ***v1.0.0:*** 
     - Initial release


