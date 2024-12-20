using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MusicAttributionUIController : MonoBehaviour {

    public const float PopTime = 0.2f;
    public const float ShowTime = 3f;

    public GameObject trackName, artistName, productionCredit;

    void Start() {
        trackName.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0f);
        artistName.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0f);
        productionCredit.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0f);
    }

    public void Show(string trackName, string artistName, string productionCredit) { StartCoroutine(showInternal(trackName, artistName, productionCredit)); }
    private IEnumerator showInternal(string track, string artist, string credit) {
        trackName.GetComponent<TextMeshProUGUI>().text = track;
        artistName.GetComponent<TextMeshProUGUI>().text = artist;
        productionCredit.GetComponent<TextMeshProUGUI>().text = credit;

        float popTime = Time.time;
        while(Time.time - popTime < PopTime) {
            float ratio = (Time.time-popTime)/PopTime;
            trackName.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, ratio);
            artistName.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, ratio);
            productionCredit.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, ratio);
            yield return new WaitForEndOfFrame();
        }
        trackName.GetComponent<TextMeshProUGUI>().color = Color.white;
        artistName.GetComponent<TextMeshProUGUI>().color = Color.white;
        productionCredit.GetComponent<TextMeshProUGUI>().color = Color.white;

        yield return new WaitForSeconds(ShowTime);

        yield return hideInternal();
    }

    public void Hide() { StartCoroutine(hideInternal()); }
    private IEnumerator hideInternal() {
        float popTime = Time.time;
        while(Time.time - popTime < PopTime) {
            float ratio = (Time.time-popTime)/PopTime;
            ratio = 1-ratio;
            trackName.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, ratio);
            artistName.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, ratio);
            productionCredit.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, ratio);
            yield return new WaitForEndOfFrame();
        }
        trackName.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0f);
        artistName.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0f);
        productionCredit.GetComponent<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0f);
    }

}
