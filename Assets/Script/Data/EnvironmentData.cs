using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class EnvironmentData : UpdateData {

    public bool usingHeightMapSeed;
    public int seed;
    [Range(0, 10000)]
    public int prngPrecision = 1000;

    [Space]
    public Types[] types;

    [System.Serializable]
    public class Types
    {
        public GameObject Object;
        public AnimationCurve curve;
        public Vector3 move;
        public Vector3 rotation;
        public Vector3 scale;
        public HightMapSettings noiseData;
        [Range(0, MeshSettings.numberOfSuportedLODs - 1)]
        public int LOD;
        [Space]
        [Range(0, 1)]
        public float randPos;
        public bool randRotX;
        public bool randRotY;
        public bool randRotZ;
        

        
    }

}
