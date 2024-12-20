using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonusCounter : MonoBehaviour {

    public GameObject TimerForeground;
    public GameObject ReadyLabel;

    public float Value = 0f;

    void Start() {
        Value = 0f;
    }

    void Update() {
        TimerForeground.transform.localScale = new Vector3(3.5f * Value, 0.51f, 3.5f * Value);

        ReadyLabel.SetActive(Value == 1);

        if (Value == 1) {
            float time = Time.time;
            float dec = time - (int)time;
            TimerForeground.GetComponent<Renderer>().material = (dec < 0.5f) ? 
                PrefabsManager.Instance.SpecialButtonMaterial : 
                PrefabsManager.Instance.SpecialButtonLitMaterial;
        }
        else TimerForeground.GetComponent<Renderer>().material = PrefabsManager.Instance.SpecialButtonMaterial;
    }

}
