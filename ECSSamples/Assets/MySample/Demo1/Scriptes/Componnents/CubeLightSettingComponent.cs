using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct CubeLightSettingComponent : IComponentData
{
    public float minAlpha;
    public float maxAlpha;

    [ColorUsage(true, true)]
    public Color maxEmissionColor;

    [ColorUsage(true, true)]
    public Color minEmissionColor;

    [ColorUsage(true, true)]
    public Color lowFrequencyColor;

    [ColorUsage(true, true)]
    public Color heightFrequencyColor;

    public LightTranslationType lightTranslationType;

    public enum LightTranslationType
    {
        Frequency,
        Height
    }
}
