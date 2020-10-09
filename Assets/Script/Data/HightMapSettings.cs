using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class HightMapSettings : UpdateData
{
    public NoiseSettings noiseSettings;
    public ErossionSettings erossionSettings;

    public bool useFalloff;
    [Range(-10, 10)]
    public float FalloffBlur;
    [Range(0, 10)]
    public float FalloffSize;

    public float heightMultiplayer;
    public AnimationCurve animationCurve;
    public ComputeShader erosionShader;

    public float minH
    {
        get
        {
            return heightMultiplayer * animationCurve.Evaluate(0);
        }
    }
    public float maxH
    {
        get
        {
            return heightMultiplayer * animationCurve.Evaluate(1);
        }
    }


    #if UNITY_EDITOR

    protected override void OnValidate()
    {
        noiseSettings.ValidateV();
        base.OnValidate();
    }

    #endif

}
