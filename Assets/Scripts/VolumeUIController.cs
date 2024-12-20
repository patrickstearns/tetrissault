using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeUIController : MonoBehaviour {

    public const float ShowTime = 3f;

    private int volume = 7;
    private bool muted = false;
    private float lastKeypress = -10f; //neg so it doesn't appear on startup

    public RawImage icon, bar;

    void Start() {
        volume = PlayerPrefs.GetInt("volume");
        UpdateMasterVolume();       
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.LeftBracket)) {
            volume--;
            if (volume < 0) volume = 0;
            lastKeypress = Time.time;
            UpdateMasterVolume();
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket)) { 
            volume++; 
            if (volume > 10) volume = 10;
            lastKeypress = Time.time;
            UpdateMasterVolume();
        }
        else if (Input.GetKeyDown(KeyCode.Backslash)) { 
            muted = !muted;
            lastKeypress = Time.time;
            UpdateMasterVolume();
        }

        if (Time.time-lastKeypress < ShowTime + 0.1f) { 
            float ratio = (Time.time-lastKeypress) / 0.1f;
            if (ratio > 1) ratio = 1;
            icon.color = new Color(1f, 1f, 1f, ratio);
            bar.color = new Color(1f, 1f, 1f, ratio);
        }
        else if (Time.time-lastKeypress >= ShowTime + 0.2f) {
            float ratio = (Time.time-lastKeypress-ShowTime-0.1f) / 0.1f;
            if (ratio > 1) ratio = 1;
            ratio = 1-ratio;
            icon.color = new Color(1f, 1f, 1f, ratio);
            bar.color = new Color(1f, 1f, 1f, ratio);
        }
        if (muted) icon.color = new Color(0.3f, 0.3f, 0.3f, 1f);
    }

    private void UpdateMasterVolume() {
        Vector2 max = bar.GetComponent<RectTransform>().anchorMax;
        max.y = 0.5f * volume;
        bar.GetComponent<RectTransform>().anchorMax = max;

        int effectiveVolume = volume;
        if (muted) effectiveVolume = 0;
        AudioManager.Instance.ChangeMasterVolume(effectiveVolume/10f);

        PlayerPrefs.SetInt("volume", volume);
    }
}
