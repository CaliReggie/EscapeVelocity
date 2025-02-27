using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// this script hides the mesh of an object while leaving the shadows
public class HideMeshOnStart : MonoBehaviour
{
    public bool hideMesh = true;
    private void Start()
    {
        if (hideMesh)
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            
            // hide the mesh
            meshRenderer.enabled = false;
            
            // leave the shadows
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}
