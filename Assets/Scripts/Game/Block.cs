using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour {

    public bool Moving = false, Removing = false, Unplaced = true;
    public BlockModel model;

    public void Awake() {
        GameController.Instance.TileLayer.AllBlocks.Add(this);
    }

}
