using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleController : MonoBehaviour {

    private AudioManager audioManager;

    void Start() {
        audioManager = FindObjectOfType<AudioManager>();
        audioManager.PlayMenuBGM();        
    }
}
