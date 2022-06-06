using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[DisableAutoCreation]
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class SpawnerSystem : SystemBase
{
    private EntityQuery settingQuery;
    private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

    protected override void OnCreate()
    {
        commandBufferSystem = World.DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        settingQuery = GetEntityQuery(ComponentType.ReadOnly<CommonSettingComponent>());
    }

    protected override void OnUpdate()
    {
        var writer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        CommonSettingComponent settings = settingQuery.GetSingleton<CommonSettingComponent>();

        Entities.WithBurst(synchronousCompilation:true).ForEach((Entity entity,int entityInQueryIndex, in CubeSpawnerComponent cubeSpawnerComponent,in LocalToWorld location) =>
        {
            var width = settings.mapWidth;
            var height = settings.mapHeight;
            if(width<=0 && height<=0) return;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        var instance = writer.Instantiate(entityInQueryIndex, cubeSpawnerComponent.cube);
                        var position = math.transform(location.Value, new float3(i + i * settings.cubeSpacing, k + k * settings.cubeSpacing, j + j * settings.cubeSpacing));
                        writer.SetComponent(entityInQueryIndex, instance, new Translation()
                        {
                            Value = position
                        });
                        writer.AddComponent<Tag_Cube>(entityInQueryIndex, instance);

                        if (settings.addURPColorComponent)
                        {
                            writer.AddComponent(entityInQueryIndex, instance,
                                new URPMaterialPropertyBaseColor() { Value = new float4(1, 1, 1, 1) });

                            writer.AddComponent(entityInQueryIndex, instance,
                                new URPMaterialPropertyEmissionColor() { Value = new float4(1, 1, 1, 1) });
                        }
                    }
                }
            }
        
            writer.DestroyEntity(entityInQueryIndex,entity);
        }).ScheduleParallel();
        
        commandBufferSystem.AddJobHandleForProducer(Dependency);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}

