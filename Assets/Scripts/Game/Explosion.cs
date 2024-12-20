using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour {

    public static Explosion Create(Vector3 position) {
        GameObject obj = Instantiate(PrefabsManager.Instance.ExplosionPrefab, position, Quaternion.identity);
        Explosion exp = obj.GetComponent<Explosion>();
        return exp;
    }

    private const float TimeToLive = 3f;

    private float startTime;

    public float Radius = 2;
    public ParticleSystem ExplosionParticleSystem;

    void Start() { 
        startTime = Time.time;
        ExplosionParticleSystem.Play();
    }

    void Update() { 
        if (Time.time-startTime > TimeToLive) Destroy(gameObject);
    }

}
