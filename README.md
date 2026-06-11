# The Last Door to Home

## Game Title

**The Last Door to Home**

## Short Description

**The Last Door to Home** is a 2D story-driven escape room adventure game with light horror elements and branching endings.

The player takes the role of **Kian**, a scientist and explorer who wakes up alone in an abandoned research base after a failed teleportation incident. His goal is to understand what happened, follow the remaining clues, and find the real way home.

As the player explores the base, they investigate rooms, collect clues, receive conflicting messages, and gradually discover that the most obvious exit may not be the true way out.

The game focuses on:

* room-based exploration
* environmental storytelling
* object interaction and clue collection
* conflicting messages and unreliable information
* player choices leading to different endings
* a final monster encounter

## Target Audience

**The Last Door to Home** is designed for players who enjoy short 2D narrative escape room games with light horror and science-fiction elements.

The game is aimed at players who like exploration, environmental storytelling, clue collection, room investigation, and branching endings. It is suitable for casual players, but it requires attention to dialogue, objects, and story details in order to understand the truth and reach the best ending.

The game is not focused on fast-paced combat or traditional RPG progression. Instead, it focuses on atmosphere, mystery, player interpretation, and meaningful choices.

## Controls

This game is mainly controlled by keyboard.

| Action              | Key / Input               |
| ------------------- | ------------------------- |
| Move                | WASD / Arrow Keys         |
| Interact / Confirm  | Enter                     |
| Continue dialogue   | Enter                     |
| Back / Return       | Esc                       |
| Main Menu selection | Mouse / WASD / Arrow Keys |

The mouse is mainly used in the **Main Menu**.
Most in-game interactions are completed with the keyboard.

## How to Run

### Option 1: Run the Unity Project

1. Clone or download this repository.
2. Open the project in Unity.
3. Use Unity version **2022.3.62f3**.
4. Open the main menu scene.
5. Press **Play** in the Unity Editor.

### Option 2: Run the Built Game

1. Download the latest build from the release folder or release page.
2. Unzip the build file.
3. Run the executable file.
4. Start the game from the main menu.

## Unity Version

This project was developed using:

**Unity 2022.3.62f3**

Please use the same or a compatible Unity version to avoid scene, package, or input system issues.

## Current Status

The main game content has been completed.

Implemented features include:

* player movement
* room-to-room scene transitions
* object interaction system
* item pickup system
* inventory-based progression checks
* dialogue and message triggers
* clue-based story progression
* multiple rooms
* main menu and UI screens
* save/load system
* Ending 1 routes
* Ending 2 route
* Ending 3 route
* final monster encounter
* final story events

The project is currently in the **testing and debugging stage**.

The remaining work mainly focuses on:

* fixing bugs
* testing all endings
* checking interaction triggers
* checking save/load stability
* balancing the final monster encounter
* polishing visual and UI details before submission

## Known Issues

Current known issues include:

* Some dialogue or interaction triggers may need further testing.
* Scene transitions may require final stability checks.
* Save/load behaviour needs to be tested across different scenes.
* The final monster encounter may still need balance adjustments.
* Some UI elements may need final visual polish.
* Audio and visual feedback may not be fully polished in every scene.

These issues are being tested and fixed during the final debugging stage.

## Credits

A simplified credit statement is included in the game’s **About Game** page.

Detailed credits for assets, fonts, audio, references, and other external resources are recorded in the following file:

```text
[Add the detailed credits file path here]
```

### Development Team

Lead Designer, Story Writer, Artist & Visual Designer: **Yajing Xu**

### Tools & Engine

* Programming Assistance: **Codex**
* Pixel Art Editor: **Aseprite**
* Illustration Tool: **HuashiJie Pro**
* Game Engine: **Unity 2022.3.62f3**

### Assets and Fonts

Most UI and visual design elements were created by the developer.

The game font was downloaded from **fonts.net.cn** and is used for non-commercial purposes only.

Sound effects are sourced from **Freesound**, licensed under **CC0** and **CC BY 4.0** where applicable.

Full detailed credits are listed in the detailed credits file and/or ending credits.

## AI / Tutorial / Template Use

AI tools were used during development, especially for programming support.

Codex was used to assist with most of the code implementation, including:

* player movement
* interaction logic
* scene transitions
* inventory and item checks
* dialogue and message systems
* save/load logic
* UI and menu behaviour
* ending conditions
* final monster encounter logic
* debugging and code revision

AI tools were also used for support in:

* brainstorming story ideas
* improving documentation wording
* generating and refining development plans
* writing issue and pull request descriptions
* README drafting and revision

Tutorials and online references were used to support Unity implementation, including topics such as:

* 2D player movement
* Tilemap setup
* object interaction
* scene switching
* UI and menu systems
* save/load logic
* simple enemy or monster behaviour

All AI-generated or tutorial-assisted content was reviewed, adapted, tested, and integrated by the developer to fit the final game design.

## Repository Structure

```text
.
├── Class Note/              Class notes and written materials
├── In-class Activity/       Unity practice projects completed during class
├── Prototype/               Early concept, design, feasibility, and planning documents
├── The Last Door to Home/   Main Unity game project
├── docs/                    Project documentation, images, sketches, and supporting files
├── .gitignore               Git ignore configuration
└── README.md                Project overview and running instructions
```

## Game Structure

The game contains several main playable areas:

1. **Dorm Room**
   The starting area where Kian wakes up and finds the first clues.

2. **Dorm Corridor**
   A connecting area leading to the base entrance and elevator.

3. **Base Entrance**
   A dangerous exit point that can lead to one version of Ending 1.

4. **Elevator and Lower Corridor**
   A transition area leading to deeper parts of the base.

5. **Archive Room**
   A clue-focused room containing information about the eye-flowers and the incident.

6. **Control Room**
   A key progression area containing investigation content, equipment choices, and story reveals.

7. **Teleportation Room**
   The final area where the truth is confronted through the monster encounter.

## Endings

The game contains three main endings.

### Ending 1: Death Ending

There are two possible routes to this ending.

The first route happens when the player trusts the wrong information and chooses the most obvious exit too early.

The second route happens when the player dies during the final monster encounter.

### Ending 2: Distorted Escape Ending

The player enters the teleportation portal before the monster is truly eliminated.

Although Kian reaches the portal, he sees a distorted reflection of himself in the mirror, appearing in the form of a monster. His perception of reality collapses, and he fails to truly return home.

### Ending 3: True Return Ending

The player discovers the truth, makes the correct decisions, and successfully eliminates the monster.

After resolving the final threat, Kian opens the real door and returns home.

## Notes

This project was designed as a compact but complete individual game project for **DI32002 - Games Programming**.

The goal was to balance narrative design with playable systems, including exploration, item checks, branching endings, save/load, menu systems, and a final monster encounter.
