using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialScroller : MonoBehaviour {

    public Vector2 ScrollSpeed;

    private Material material;

    void Start() {
        material = GetComponent<Renderer>().material;        
    }

    void Update(){
        material.mainTextureOffset += ScrollSpeed * Time.deltaTime;
    }

}
