using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepSceneViewActive : MonoBehaviour{

    public bool KeepSceneView;

    void Awake(){

        if (this.KeepSceneView && Application.isEditor){

            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
    }
}
