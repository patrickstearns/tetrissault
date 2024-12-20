using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabsManager : MonoBehaviour {

    private static PrefabsManager _instance;
    public static PrefabsManager Instance { get { return _instance; } }

    void Awake() { 
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else { _instance = this; }
    }

    public Material HPMeterOnMaterial, HPMeterOffMaterial;
    public Material SpecialButtonMaterial, SpecialButtonLitMaterial;

    public GameObject BlockPrefab, ExplosionPrefab;
    public List<GameObject> PiecePrefabs;

}
