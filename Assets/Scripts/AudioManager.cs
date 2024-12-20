using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour {

    private static AudioManager _instance;
    public static AudioManager Instance { get { return _instance; } }

    private AudioSource audioSource;

    public AudioMixer mixer;
    public AudioMixerSnapshot[] snapshots;
    public MusicAttributionUIController musicAttributionUIController;

    public AudioClip move, select, rotate, attach, clear, ding, barf, ready, special, struck, rumble, explode;

    public BGMTrack menuBgm;
    public BGMTrack[] gameBgms;
    public bool playGameBgm = false;

    private int currentGameBgm = -1;
    private float volume = 1;
    
    void Awake() {
        if (_instance != null && _instance != this) Destroy(gameObject);
        else _instance = this;

        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();        
        StartCoroutine(playGameBgmInternal());
    }

    public void PlayMenuBGM() {
        SetGameMusicPlaying(false);
        audioSource.clip = menuBgm.Clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void HideMusicAttributionUI() { musicAttributionUIController.Hide(); }

    public void SetGameMusicPlaying(bool playGameBgm) { 
        this.playGameBgm = playGameBgm; 
        audioSource.loop = false;
        audioSource.Stop();
    }

    private IEnumerator playGameBgmInternal() {
        while (true) {
            if (playGameBgm && !audioSource.isPlaying) {
                int newBgm = Random.Range(0, gameBgms.Length);
                while (newBgm == currentGameBgm) newBgm = Random.Range(0, gameBgms.Length);
                currentGameBgm = newBgm;

                audioSource.clip  = gameBgms[currentGameBgm].Clip;
                audioSource.Play();

                musicAttributionUIController.Show(gameBgms[currentGameBgm].TrackName, 
                    gameBgms[currentGameBgm].ArtistName, 
                    gameBgms[currentGameBgm].ProductionCredit);
            }
            yield return new WaitForEndOfFrame();
        }    
    }

    public float GetVolume() { return volume; }
    public void ChangeMasterVolume(float newVolume) {
        if (newVolume == 0) newVolume = 0.0001f;
        mixer.SetFloat("MasterGroupVolume", Mathf.Log(newVolume) * 20);
        volume = newVolume;
    }

    public void SetLowpassEffectEnabled(bool enabled) {
        float[] weights = enabled ? new float[]{ 0f, 1f } : new float[]{ 1f, 0f };
        mixer.TransitionToSnapshots(snapshots, weights, 0.25f);
    }

    public AudioSource PlaySFX(AudioClip clip) {
        GameObject obj = new GameObject();
        obj.transform.SetParent(transform);
        obj.name = clip.name;
        AudioSource audioSource = obj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.Play();
        StartCoroutine(DestroyAfter(audioSource.gameObject, clip.length));

        return audioSource;
    }

    private IEnumerator DestroyAfter(GameObject obj, float timeToDestroy) {
        float startTime = Time.time;
        while (Time.time-startTime < timeToDestroy) yield return new WaitForEndOfFrame(); //give it a chance to get going
        Destroy(obj);
    }
}