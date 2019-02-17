using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public abstract class FiddlerBehaviour : MonoBehaviour
{
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

}
