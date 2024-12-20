using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/BGM Track")]
public class BGMTrack : ScriptableObject {

    public AudioClip Clip;
    public string TrackName, ArtistName, ProductionCredit;
}
