using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisableAutoCreation]
public partial class CubeLightTransitionSystem : SystemBase
{
    private EntityQuery commonSettingQuery;
    private EntityQuery lightSettingQuery;
    private EntityQuery CubeEntityQuery;

    protected override void OnCreate()
    {
        commonSettingQuery = GetEntityQuery(ComponentType.ReadOnly(typeof(CommonSettingComponent)));
        lightSettingQuery = GetEntityQuery(ComponentType.ReadOnly(typeof(CubeLightSettingComponent)));
        CubeEntityQuery = GetEntityQuery(ComponentType.ReadOnly(typeof(Tag_Cube)), typeof(URPMaterialPropertyBaseColor),
            typeof(URPMaterialPropertyEmissionColor));

        RequireSingletonForUpdate<CubeLightSettingComponent>();
    }

    protected override void OnUpdate()
    {
        var commonSetting = commonSettingQuery.GetSingleton<CommonSettingComponent>();
        var lightSetting = lightSettingQuery.GetSingleton<CubeLightSettingComponent>();

        new EvaluateCubeLightJob()
        {
            commonSetting = commonSetting,
            lightSetting = lightSetting
        }.ScheduleParallel(CubeEntityQuery);
    }

    private partial struct EvaluateCubeLightJob : IJobEntity
    {
        [ReadOnly]
        public CommonSettingComponent commonSetting;
        [ReadOnly]
        public CubeLightSettingComponent lightSetting;

        public void Execute(in Translation translation,ref URPMaterialPropertyBaseColor baseColor,ref URPMaterialPropertyEmissionColor emissionColor, [EntityInQueryIndex] int entityInQueryIndex)
        {
            var width = commonSetting.mapWidth;
            var height = commonSetting.mapHeight;
            var heightIndex = entityInQueryIndex % height;
            var columnIndex = entityInQueryIndex / height % width;
            var rowIndex = entityInQueryIndex / height / width % width;

            var t =Mathf.Clamp01(translation.Value.y / commonSetting.cubeMaxHeightInYAxis);
            baseColor.Value.w = Mathf.Clamp01(Mathf.Lerp(lightSetting.minAlpha,lightSetting.maxAlpha,t));
            switch (lightSetting.lightTranslationType)
            {
                case CubeLightSettingComponent.LightTranslationType.Frequency:
                    t = Mathf.Clamp01((float)columnIndex / width);
                    var newEmissionColor = Color.Lerp(lightSetting.lowFrequencyColor, lightSetting.heightFrequencyColor, t);
                    emissionColor.Value = newEmissionColor.ToFloat4();
                    break;
                case CubeLightSettingComponent.LightTranslationType.Height:
                    newEmissionColor = Color.Lerp(lightSetting.minEmissionColor, lightSetting.maxEmissionColor, t);
                    emissionColor.Value = newEmissionColor.ToFloat4();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

public static class ColorExtension
{
    public static float4 ToFloat4(this Color color)
    {
        return new float4(color.r, color.g, color.b, color.a);
    }
}
