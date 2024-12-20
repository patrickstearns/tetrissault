using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimer : MonoBehaviour {

    public float TimeToIncrement = 5f;

    public GameObject TimerForeground;

    public bool Running = false;

    private float timeSoFar;

    void Start() {
        timeSoFar = 0;
//        Running = false;
    }

    public void SetRunning(bool running) {
        this.Running = running;
    }

    void Update() {
        if (Running) {
            timeSoFar += Time.deltaTime;
            if (timeSoFar >= TimeToIncrement) {
                timeSoFar -= TimeToIncrement;
                GameController.Instance.TimerDinged();
            }
        }

        float ratio = Mathf.Clamp(timeSoFar / TimeToIncrement, 0, 1);
        TimerForeground.transform.localScale = new Vector3(3.5f * ratio, 0.01f, 3.5f * ratio);
    }

    public float TimeUntilIncrement() { return Mathf.Max(0, TimeToIncrement - timeSoFar); }

}
