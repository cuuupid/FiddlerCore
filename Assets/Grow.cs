using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grow : MonoBehaviour
{
    bool growing = true;
    // Update is called once per frame
    void Update()
    {
        if (transform.localScale.x < 1 && growing) {
            transform.localScale += new Vector3(0.02f, 0.02f, 0.02f);
        } else {
            growing = false;
        }
    }
}
