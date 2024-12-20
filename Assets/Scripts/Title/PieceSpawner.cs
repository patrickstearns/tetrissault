using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceSpawner : MonoBehaviour {

    private float SpawnProbability = 0.01f;

    public GameObject[] PiecePrefabs;

    private BoxCollider boxCollider;

    void Start() { 
        boxCollider = GetComponent<BoxCollider>();
    }

    void Update() { 
        if (Random.value < SpawnProbability) {
            Vector3 pos = new Vector3(
                Random.Range(0, boxCollider.bounds.size.x),
                Random.Range(0, boxCollider.bounds.size.y),
                Random.Range(0, boxCollider.bounds.size.z)
                ) + boxCollider.bounds.min;

            GameObject newObject = Instantiate(PiecePrefabs[Random.Range(0, PiecePrefabs.Length)]);
            newObject.transform.position = pos;

            Rigidbody rigidbody = newObject.AddComponent<Rigidbody>();
            rigidbody.angularVelocity = Random.insideUnitSphere;

            newObject.AddComponent<MeshCollider>();

            StartCoroutine(killAfterFiveSeconds(newObject));
        }
    }

    private IEnumerator killAfterFiveSeconds(GameObject toKill) {
        yield return new WaitForSeconds(5f);
        Destroy(toKill);
    }

}
