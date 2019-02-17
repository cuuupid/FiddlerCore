using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoogleARCore;
using GoogleARCore.Examples.Common;
using UnityEngine.Networking;

#if UNITY_EDITOR
    // Set up touch input propagation while using Instant Preview in the editor.
    using Input = GoogleARCore.InstantPreviewInput;
#endif

public class FiddlerEngine : FiddlerBehaviour {

	public Branch tree;

	/// <summary>
	/// The first-person camera being used to render the passthrough camera image (i.e. AR background).
	/// </summary>
	public Camera FPSCamera;
	/// <summary>
	/// A prefab for tracking and visualizing detected planes.
	/// </summary>
	public GameObject PlanePrefab;

	/// <summary>
	/// The save point prefab.
	/// </summary>
	public GameObject savePointPrefab;
	public GameObject savePoint;
	/// <summary>
	/// The mode the Engine is running in. This should probably be set to Detecting to start off.
	/// </summary>
	public enum Mode : byte {Tracking, Detecting, Cutscene};
	public Mode mode = Mode.Tracking;

	/// <summary>
	/// A list to hold new planes ARCore began tracking in the current frame. This object is used across
	/// the application to avoid per-frame allocations.
	/// </summary>
	private List<DetectedPlane> m_NewPlanes = new List<DetectedPlane>();

	/// <summary>
	/// The UI elements.
	/// </summary>
	public Canvas UI;
	public Text text;
	public GameObject dialogBox;
	public GameObject infoBox;
	public Text info;
	int curMsg = 0;
	int msgQueue = 0;

	Branch NPC;
	public void Start() {
		NPC = tree;
	}

	List<GameObject> planes = new List<GameObject>();

	IEnumerator _typeOut(string dialog, int id) {
		yield return new WaitUntil(() => curMsg == id);
		float ms = 0.001f;
		for (int i = 0; i < dialog.Length; i++) {
			text.text = dialog.Substring(0, i);
			yield return new WaitForSeconds(50 * ms);
		}
		yield return new WaitForSeconds(1.0f);
		curMsg += 1;
		if (curMsg == msgQueue) {
			dialogBox.SetActive(false);
		};
	}

	public float typeOut(string dialog) {
		dialogBox.SetActive(true);
		StartCoroutine(_typeOut(dialog, msgQueue));
		msgQueue += 1;
		return (50.0f * 0.001f * dialog.Length) + 1.0f;
	}

	void _OnImageAvailable(int width, int height, IntPtr YBuffer, IntPtr UBuffer, IntPtr VBuffer, int bufferSize)
	{
		Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
		try {
			byte[] yBytes = new byte[width * height];
			byte[] uBytes = new byte[width * height / 2];
			byte[] vBytes = new byte[width * height / 2];
			System.Runtime.InteropServices.Marshal.Copy(YBuffer, yBytes, 0, width * height);
			System.Runtime.InteropServices.Marshal.Copy(UBuffer, uBytes, 0, width * height / 2);
			System.Runtime.InteropServices.Marshal.Copy(VBuffer, vBytes, 0, width * height / 2);

		    Color c = new Color();
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					float Y = yBytes[y * width + x];
					float U = uBytes[(y/2) * width + x];
					float V = vBytes[(y/2) * width + x];
					c.r = Y + 1.370705f * (V - 128.0f);
					c.g = Y - 0.698001f * (V - 128.0f) - 0.337633f * (U - 128.0f);
					c.b = Y + 1.732446f * (U - 128.0f);
					//c.r = 1.164f * (Y - 16.0f) + 1.596f * (V - 128.0f);
					//c.g = 1.164f * (Y - 16.0f) - 0.813f * (V - 128.0f) - 0.391f * (U - 128.0f);
					//c.b = 1.164f * (Y - 16.0f) + 2.018f * (U - 128.0f);
					//c.r = Y + (1.370705f * (V-128.0f));
					//c.g = Y - (0.698001f * (V-128.0f)) - (0.337633f * (U-128.0f));
					//c.b = Y + (1.732446f * (U-128.0f));
					c.r /= 255.0f;
					c.g /= 255.0f;
					c.b /= 255.0f;
					c.a = 1.0f;

					tex.SetPixel(width-1-x, y, c);
				}
			}
			StartCoroutine(UploadPNG(tex)); // owo
		}
		catch (Exception e) {
			Toast(e.ToString());
		}
	}

	IEnumerator UploadPNG(Texture2D tex)
    {
        yield return new WaitForEndOfFrame();
		byte[] bytes = tex.EncodeToPNG();
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", bytes, "image.png", "image/png");
        using (var w = UnityWebRequest.Post("https://api.findthefiddler.com/predict", form))
        {
            yield return w.SendWebRequest();
            if (w.isNetworkError || w.isHttpError) {
				Toast(w.error);
				Debug.LogError(w.error);
            }
            else {
				Debug.Log(w.downloadHandler.text);
				Toast(w.downloadHandler.text);
				string[] classes = w.downloadHandler.text.Split(',');
				NPCController controls = NPC.NPC.GetComponent<NPCController>();
				int nextNPC = controls.next(classes);
				if (nextNPC < 0) {
					NPC.dialog = NPC.redoDialog;
				} else {
					NPC = NPC.choices[nextNPC].branch;
				}
				mode = Mode.Tracking;
            }
        }
    }

	// Update is called once per frame
	void Update () {
		Lifecycle();
		Touch touch;
		if (mode == Mode.Detecting) {
			infoBox.SetActive(true);
			info.text = "Tap anywhere to interact with things!";
			if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
			{
				// the player didn't touch the screen
				return;
			}
			mode = Mode.Cutscene;
			Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24,false);
			screenShot.ReadPixels(new Rect(0,0,Screen.width,Screen.height), 0, 0);
			screenShot.Apply();
			info.text = "Loading... please wait!";
			StartCoroutine(UploadPNG(screenShot));
			/*
			using (var image = Frame.CameraImage.AcquireCameraImageBytes())
            {
                if (!image.IsAvailable)
                {
                    return;
                }
                _OnImageAvailable(image.Width, image.Height, image.Y, image.U, image.V, 0);
            }
			*/
		}
		if (mode == Mode.Tracking) {
			infoBox.SetActive(true);
			info.text = "Find a spot for your Save Post.";
			// Looking to place a save point
            // Show detected planes
            Session.GetTrackables<DetectedPlane>(m_NewPlanes, TrackableQueryFilter.New);
            for (int i = 0; i < m_NewPlanes.Count; i++)
            {
                GameObject planeObject = Instantiate(PlanePrefab, Vector3.zero, Quaternion.identity, transform);
                planeObject.GetComponent<DetectedPlaneVisualizer>().Initialize(m_NewPlanes[i]);
				planes.Add(planeObject);
            }


			if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
			{
				// the player didn't touch the screen
				return;
			}

			TrackableHit hit;
			// raycast against the plane
			TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon | TrackableHitFlags.FeaturePointWithSurfaceNormal;

			if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
			{
				// if an object is found

				if ((hit.Trackable is DetectedPlane) &&
					Vector3.Dot(FPSCamera.transform.position - hit.Pose.position,
						hit.Pose.rotation * Vector3.up) < 0)
				{
					// if hitting back of plane
					Debug.Log("Hit at back of the current DetectedPlane");
				}
				else
				{
					// place a save point
					savePoint = Instantiate(savePointPrefab, hit.Pose.position, hit.Pose.rotation);

					// Create an anchor to allow ARCore to track the save post
					var anchor = hit.Trackable.CreateAnchor(hit.Pose);

					// Make the save point a child of the anchor.
					savePoint.transform.parent = anchor.transform;

					// Turn off Tracking cuz we done bois
					mode = Mode.Cutscene;
					infoBox.SetActive(false);
					//typeOut("Saving...");
					// destroyAllPlanes();
					//typeOut("You have saved your game successfully."); // careful, this is async
					foreach (Transform child in savePoint.transform)
					{
						if (child.tag == "Respawn")
						{
							NPC.NPC = Instantiate(NPC.NPC, child.transform.position, child.transform.rotation);
							NPC.NPC.SetActive(true);
							NPCController controls = NPC.NPC.GetComponent<NPCController>();
							controls.gm = this;
							controls.dialog = NPC.dialog;
							controls.choices = NPC.choices;
							controls.playCutscene();
						}
					}
				}
			} else {
				Debug.Log("You hit nothing");
			}
		}
	}

	public void destroyAllPlanes() {
		foreach (GameObject plane in planes) {
			GameObject.Destroy(plane);
		}
	}

	/// <summary>
	/// Check and update the application lifecycle.
	/// </summary>
	private void Lifecycle()
	{
		// Exit the app when the 'back' button is pressed.
		if (Input.GetKey(KeyCode.Escape))
		{
			Application.Quit();
		}

		// Only allow the screen to sleep when not tracking.
		if (Session.Status != SessionStatus.Tracking)
		{
			const int lostTrackingSleepTimeout = 15;
			Screen.sleepTimeout = lostTrackingSleepTimeout;
		}
		else
		{
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
		}

		// Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
		if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
		{
			Toast("Camera permission is needed to run this application.");
			Application.Quit();
		}
		else if (Session.Status.IsError())
		{
			Toast("ARCore encountered a problem connecting.  Please start the app again.");
			Application.Quit();
		}
	}

	/// <summary>
	/// Show an Android toast message.
	/// </summary>
	/// <param name="message">Message string to show in the toast.</param>
	public void Toast(string message)
	{
		AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

		if (unityActivity != null)
		{
			AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
			unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
			{
				AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
					message, 0);
				toastObject.Call("show");
			}));
		}
	}


}
