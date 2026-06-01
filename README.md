# This Reminded Me Of You
Code and documentation for a Unity3D project by Jo Suk and Mashi Zaman for Hypercinema Fall 2022 at ITP.

> **Note:** Third-party assets (models, textures, fonts, plugins) are excluded from git tracking.
> See [Setup & Dependencies](#setup--dependencies) below for what to download.

## Setup & Dependencies

The following third-party content is **required** for the V2 scene to work.
Install from the Unity Asset Store or Package Manager as needed:

| Asset | Source | Role |
|-------|--------|------|
| `Assets/FurnitureAssets/` | Unity Asset Store | Room furniture FBX models + textures (tables, shelves, kiosks, display) |
| `Assets/3dizart Books Pack/` | Unity Asset Store | Book 3D models + textures |
| `Assets/DOTween/` | Unity Asset Store (free) | Tween animations (book animate-in/out, panel transitions, bobbing, hovers) |
| `Assets/TextMesh Pro/` | Unity Package Manager | Text rendering for book UI and submission form |
| `Assets/Gwangju_3D asset/` | Custom-commissioned | Korean storefront environments (Gungjeon Bakery, Dong-A Silk, Jeonbyungwon Tailor Shop) — see `credits.txt` |
| `Assets/Customizable Skybox/` | Unity Asset Store | Skybox material |
| `Assets/ReadingRoom/` | Unity Asset Store | Room environment mesh + textures (walls, floor, ceiling, windows, lights) |
| `Assets/ShelfObjects/` | Unity Asset Store | Shelf decoration items (plants, frames, etc.) |
| `Assets/TextureHaven/` | [texturehaven.com](https://texturehaven.com) (CC0) | PBR textures (bricks) |
| `Assets/Fonts/` | Google Fonts | Display fonts: [Noto Serif KR](https://fonts.google.com/noto/specimen/Noto+Serif+KR) (Korean), [Lora](https://fonts.google.com/specimen/Lora), [IM Fell English](https://fonts.google.com/specimen/IM+Fell+English) |
| `Assets/Materials/YughuesFreeFlooringMaterials/` | Unity Asset Store (free) | Flooring material textures |

### Optional / reference content

| Asset | Source | Notes |
|-------|--------|-------|
| `Assets/KoreanInspo/` | Original photography | Reference photos of Korean storefronts (not used in builds) |
| `Assets/ImageAssets/TheWorldsBorough/` | Original photography | Large photo textures |
| `Assets/Screenshots/` | Generated | Testing / presentation screenshots |
| `Assets/URPDefaultResources/` | Unity generated | URP default resources; recreated on package re-import |

---

Take a break in your living room filled with books we've shared with you throughout years of friendship. Use mouse or scroll wheel to search the room, and click to read our dedications to you. <a href="https://youtu.be/eiOH1X2wOLE">Watch a demo here</a>, or play on your computer's web browser <a href="https://mashiyatz.github.io/ThisRemindedMeOfYou/">through here</a>.   

<p align="center">
<img src="https://user-images.githubusercontent.com/43973044/208791162-5f12d3ce-8a2e-4ffc-8486-3ea03391741c.png" alt="A still image of a living room that is the setting of this game and the title This Reminded Me Of You. The room, depicted in an isometric perspective, has a couch on one wall facing a TV and shelves on the opposite side. The wall between them has a pair of wall length windows with a big blue curtain covering them. On the shelves, and on a coffee table in front of the couch, are many books, some neatly arranged, others scattered.">
</p>

### Introduction

This Reminded Me of You is an interactive scene where the player is a mutual friend of the two creators, Mashi and Jo. Scattered across the player’s living room are myriad books that Mashi and Jo have gifted the player over the course of years. Each book contains an inscription from either Mashi or Jo explaining why they thought to give the book to their friend. As the player pores through the covers, recurring themes emerge around gender roles, the weight of responsibility, and the joy of friendship. The aggregate effect is a quiet sense of kinship between the player and each of the creators that can reveal just as much about the hypothetical recipient as the givers.

### Gameplay

To navigate the scene, the player will begin at a start screen that provides a bit of context. After clicking through, the player will enter their own living room, where they can mouse over the books positioned on the table, shelves, and TV console. Upon clicking, a close-up of the book will appear alongside a handwritten note from Mashi or Jo. An audio recording of the note will play at the same time. The player need only click once to exit the pop-up and continue exploring more books. 

Players are free to engage with the room for as long as it takes them to find all of the books, or they can leave it as is for the next player to explore. Hopefully, they're interested in the books we recommend. 

### Takeaways and Ideas for Future Projects
* Most people expect to be able to interact with other objects in the room, beyond just the set of books we chose. Part of this is a consequence of using a cursor to navigate the scene. We can limit people's actions further using a rotary button, for example. Alternatively, we can make the other objects in the room interactive. 
* The space in which one plays this game is important for creating a relaxed atmosphere for the player, just like the ambient music and desaturated color tones. 
* This scene is like a more interactive Goodreads - we could help others create such spaces for themselves, or alternatively, allow a way to develop the room with artifacts and recommendations over time. Consider generating books retrieved from a database that is updated online. We could export this scene to a web browser as well. 

<p align="center">
<img src="https://user-images.githubusercontent.com/43973044/208799840-6e7dcdb2-668a-4cc1-be4d-bc2ec1781853.jpg" alt="A player reads the note written for Convenience Store Woman at the ITP Winter Show/">
</p>

<p align="center">
<img src="https://user-images.githubusercontent.com/43973044/208799855-d07214ae-8966-4081-85a5-32fc6ddf2ca6.jpg" alt="Another player reads the note written for Persepolis.">
</p>
