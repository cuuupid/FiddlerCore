# FiddlerCore

Fiddler is an AR + AI Choose Your Own Adventure Game. Use Object Detection and create AR experiences!

![Demo](img/demo.gif)

## Purpose

While AR has been around for some time it still lacks its "killer app." While it has boundless potential for becoming a platform to encourage better lifestyles, augmenting our lives, and connecting to others in new ways, it has yet to be used in full efficiency.

Fiddler aims to solve that by creating an engine and proof of concept for a Choose Your Own Adventure game run with a combination of artificial intelligence and augmented reality. By combining these two technologies, we hope to encourage healthier lifestyles both physically and mentally.

## AI

One of the major drawbacks with AR is lack of interactibility. Intuitively users want to touch, feel, and move things around, but this is hard without complex depth sensors. Current standards in AR interaction have been widely criticized and lack intuitive feel. To immprove the user experience, we drew on a basic human interaction--seeing.

Since seeing is believing, we have users look at objects and see those objects through their camera lens. Using Object Detection (Inception v4 with [a proxy through IBM's MAX Object Detector Proxy](https://github.com/pshah123/MAX-Object-Detector-Proxy)) we are able to detect what they are looking at and allow interactions to occur at this level.

We then created an engine around detections powered by a reducer to reduce certain classes to a superclass (as Inception contains thousands of labels). From there, we created a custom, extensible Choose Your Own Adventure engine.

## Fiddler Engine

The Fiddler Engine consists mostly of a GameManager to control the scene. This is because in AR, we cannot create large levels as in most Unity games, and instead must rely on procedural generation as much as possible.

The Fiddler Engine uses Augmented Reality through Google's ARCore to accomplish a variety of tasks:

- placing "save posts" which advance the game and signal the end of an interaction (the "handshake")

- augmenting faces to achieve the initial face filter-esque effect (depends on ARCore 1.7.0 for Augmented Faces)

![face demo](img/face.gif)

- delivering a camera feed (we have methods to process both the YUV-420-888 color space that can be read into byte channels from `Frame.AcquireCameraImageBytes()` and to capture full RGB images using the screencapture API)

These AR pieces are used in "Tracking" mode. The Engine also has two other modes, the "Cutscene" and "Detecting" modes.

In the "Cutscene" mode, almost everything else is disabled and dialog becomes readily available. It is used as a sort of interim mode to allow for game-level interactions.

In the "Detecting" mode, the AR objects are cleared to allow for user interaction. Upon a user interaction, the detected image is fed back into our branch engine and the game then progresses.

# Branching

While we initially thought of representing the game as a Decision Tree using a Node-based graph, we quickly found several limitations; primarily, there was no standardized way to implement the choice system in CYOA.

To allow for this we developed our engine around "branches" which each consist of two Serializable structs:
``` csharp
[Serializable]
public struct Choice {
    public string detection;
    public Branch branch;
}

[Serializable]
public struct Branch {
    public GameObject NPC;
		public Choice[] choices;
		public string[] dialog;
		public string[] redoDialog;
}
```

The "Choice" struct represents a dictionary-style pair of detection and branch. The detected classes are filtered at a threshold of 40% and then matches against detection strings; if a Choice's detection string matches, the accompanying branch is selected in the progression.

The "Branch" struct represents a recursive structure that allows for the actual CYOA aspect to be added. Each Branch consists of:

- an NPC, which is spawned next to the save post and should provide a cutscene through the `playCutscene` method

- a series of choices that the user can make from there

- some dialog to play during the cutscene

- a "redo" dialog that is played if no choices are matched by the user's next action

# Known Limitations

To allow maximum usability, we serialized both classes. If used internally they would be completely effective, however since they are serialized for use in the Inspector as part of the Engine they are limited by Unity's Serialization depth (7 cycles).

In addition, the interactions are limited by the choice-cutscene pattern above. While this is common in the CYOA genre, the Engine should be improved in the future to allow for microinteractions.

# Demo

The provided demo project consists of 2 scenes. The plot involves a nefarious antagonist, the "Fiddler," who has stolen the protagonist's identity! The protagonist must go on a journey, led by a fragment of themselves, to (quite literally) find themselves.

The protagonist encounters characters along the way who give clues and choices for the protagonist, who is penalized with increasingly worse clues for choosing incorrectly.

As a proof of concept we illustrate the usability of this mechanic to encourage:

- a healthier lifestyle through a gamified diet

- leaving home and travelling, slowly breaking out of their shell, encouraged by their curiosity and willingness to progress the game

- which then builds up to higher grade social interaction, keeping up with friends and making new ones, and ultimately encouraging large-scale social interaction

Through this manner we can gamify _life_ itself by allowing them to see their lives through AR lens, and encourage both physically and mentally healthier lifestyles.


# Credits

We used the Low Poly v2 pack by AxeyWorks for our 3d models with some edits.

The AR library powering this project is ARCore 1.7.0 by GoogleAR.

The Object Detection model, Inception v4, comes trained from the TensorFlow Model Zoo. The IBM MAX Object Detector was used to API-fy this to reduce friction points as MAX is containerized.

The container is run through a proxy maintained by us, which can be found [here](https://github.com/pshah123/MAX-Object-Detector-Proxy).


This project was hacked together at [HackNYU 2019](www.hacknyu.org).


