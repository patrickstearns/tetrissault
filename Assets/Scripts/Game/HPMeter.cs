using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPMeter : MonoBehaviour {

    public Renderer[] ChildLights;

    public int value;

    public void SetValue(int value) {
        this.value = value;

        for (int i = 0; i < ChildLights.Length; i++) {
            ChildLights[i].material = (i < value) ? 
                PrefabsManager.Instance.HPMeterOnMaterial : 
                PrefabsManager.Instance.HPMeterOffMaterial;
        }
    }

}
