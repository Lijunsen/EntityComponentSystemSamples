using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class CubeLightSettingAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float minAlpha;
    public float maxAlpha;

    [ColorUsage(true, true)]
    public Color maxEmissionColor;

    [ColorUsage(true, true)]
    public Color minEmissionColor;

    [ColorUsage(false,true)]
    public Color lowFrequencyColor;

    [ColorUsage(false, true)]
    public Color heightFrequencyColor;


    public CubeLightSettingComponent.LightTranslationType lightTranslationType;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CubeLightSettingComponent()
        {
            minAlpha = minAlpha,
            maxAlpha = maxAlpha,
            maxEmissionColor = maxEmissionColor,
            minEmissionColor = minEmissionColor,
            lowFrequencyColor = lowFrequencyColor,
            heightFrequencyColor = heightFrequencyColor,
            lightTranslationType = lightTranslationType,
        });
    }
}
