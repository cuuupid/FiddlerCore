using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NPCController : NPCBaseController
{

    public string[] dialog;
    public Choice[] choices;

    public override void playCutscene() {
        float wait = 0.0f;
        for (int i = 0; i < dialog.Length; i++) {
            wait += gm.typeOut(dialog[i]);
        }
        StartCoroutine(kill(wait + 2.0f));
    }

    public string reduceOne(string detection) {
        switch (detection) {
            case "apple":
            case "banana":
            case "pomegranate":
            case "pineapple":
            case "lemon":
            case "orange":
            case "strawberry":
                return "fruit";
            case "menu":
            case "candle":
            case "bed":
                return "health";
            case "oven":
            case "tv":
                return "dumbbell";
            default:
                return detection;
        }
        return detection;
    }

    // glorified switch statement
    public override int next(string[] detections) {
        foreach (string detection in detections) {
            Debug.Log("I detected " + detection);
            string det = reduceOne(detection);
            for (int i = 0; i < choices.Length; i++) {
                if (string.Compare(det, choices[i].detection) == 0 || string.Compare("any", choices[i].detection) == 0) {
                    Debug.Log("Matched " + choices[i].detection);
                    return i;
                }
            }
        }
        Debug.Log("Couldn't match against anything, returning default.");
        return -1;
    }
}
