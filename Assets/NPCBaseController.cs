using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class NPCBaseController : FiddlerBehaviour
{
    public FiddlerEngine gm;

    public IEnumerator kill(float wait) {
        yield return new WaitForSeconds(wait);
        gm.mode = FiddlerEngine.Mode.Detecting;
        while (transform.localScale.x > 0) {
            transform.localScale -= new Vector3(0.02f, 0.02f, 0.02f);
            yield return new WaitForEndOfFrame();
        }
        Destroy(gm.savePoint);
        gameObject.SetActive(false);
    }

    public abstract int next(string[] detections);

    public abstract void playCutscene();

}
