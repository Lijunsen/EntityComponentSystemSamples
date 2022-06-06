using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
[UpdateAfter(typeof(AudioSystem))]
public partial class CubeTranslationTransitionSystem : SystemBase
{
    private AudioSystem audioSystem;
    private EntityQuery cubeEntityQuery;
    private EntityQuery settingQuery;
    protected override void OnCreate()
    {
        base.OnCreate();
        audioSystem = World.GetExistingSystem<AudioSystem>();
        cubeEntityQuery = GetEntityQuery(typeof(Translation),ComponentType.ReadOnly<Tag_Cube>());
        settingQuery = GetEntityQuery(ComponentType.ReadOnly<CommonSettingComponent>());
    }

    protected override void OnUpdate()
    {
        var data = audioSystem.audioDataArray;
        var settings = settingQuery.GetSingleton<CommonSettingComponent>();
        var job = new EvaluateCubeHeightJob()
        {
            settings = settings,
            dataArray = data,
            deltaTime = Time.DeltaTime
        };
        Dependency = job.ScheduleParallel(cubeEntityQuery, Dependency);
    }
    
    private partial struct EvaluateCubeHeightJob : IJobEntity
    {
        // public NativeArray<Entity> cubes;
        [ReadOnly]
        public CommonSettingComponent settings;
        [ReadOnly]
        public NativeArray<float> dataArray;
        [ReadOnly]
        public float deltaTime;
        public void Execute(ref Translation translation, [EntityInQueryIndex] int entityInQueryIndex)
        {
            var width = settings.mapWidth;
            var height = settings.mapHeight;
            var heightIndex = entityInQueryIndex % height;
            var columnIndex = entityInQueryIndex/ height % width;
            var rowIndex = entityInQueryIndex / height / width % width;
            // Debug.Log($"index:{entityInQueryIndex},({rowIndex},{columnIndex},{heightIndex})");

            //获取对应的数据
            var dataArrayIndex = rowIndex * width + columnIndex;
            if (dataArrayIndex >= 0 && dataArrayIndex < dataArray.Length)
            {
                var data = Mathf.Clamp01(dataArray[dataArrayIndex] * settings.audioDataGain);
                //获取这一个格子上的最高高度
                var totalHeight = math.lerp(0, settings.cubeMaxHeightInYAxis, data);
                if (settings.cubeTransitionTpe == CommonSettingComponent.CubeTransitionTpe.Discrete)
                {
                    var newHeight = 0f;
                    if (totalHeight > heightIndex)
                    {
                        newHeight = heightIndex + heightIndex * settings.cubeSpacing;
                    }
                    var position = translation.Value;
                    var oldHeight = position.y;
                    var targetHeight = newHeight;
                    position.y = targetHeight;
                    translation.Value = position;
                }

                if (settings.cubeTransitionTpe == CommonSettingComponent.CubeTransitionTpe.Continuous)
                {
                    var newHeight = totalHeight * (float)heightIndex / settings.mapHeight;
                    
                    var position = translation.Value;
                    var oldHeight = position.y;
                    var targetHeight = oldHeight + (newHeight - oldHeight) * settings.cubeSpeedInYAxis;
                    position.y = targetHeight;
                    translation.Value = position;

                }
            }
            else
            {
                Debug.LogWarning("算法出错");
            }

            // if (rowIndex == 0)
            // {
            //     translation.Value += new float3(0, deltaTime, 0);
            // }
        }
    }
}
