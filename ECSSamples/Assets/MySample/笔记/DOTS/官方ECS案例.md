# 官方ECS案例

https://github.com/Unity-Technologies/EntityComponentSystemSamples

## 将GameObject转换成Entity

导入Entity包后，在GameObject的Inspector面板中勾选“ConvertToEntity”。

![image-20220507103456851](D:\Users\Desktop\笔记\DOTS\image-20220507103456851-16518909065521.png)

勾选后会自动挂载ConvertToEntity组件。

![image-20220507103647479](D:\Users\Desktop\笔记\DOTS\image-20220507103647479.png)

ConversionMode两个选项为销毁或保留原来的GameObject。

Tips：使用继承自**IcomponentData**的struct也可以直接挂载到GameObject上一同转换到Entity。

## 在System中使用Foreach

继承自SystemBase的自定义系统，可以通过在OnUpdate中使用Entities字段对所有Entity进行遍历。

```c#
Entities.WithName("EntityName")
            .ForEach((ref Rotation rotation, in RotationSpeed_ForEach rotationSpeed) =>
            {
                //在括号中申请组件，只读的组件使用in
                //DoSomething
            }).ScheduleParallel();
//获取全部满足具有某个组件的Enity
Entities.WithAll<>();
//不使用Burst编译，当ForEach中具有非Entity的字段（不太明确）时会无法使用Burst（会报错），所以可以使用此API
Entities.WithoutBurst();

```

## 将MonoScript组件转换成Entity的Component

继承IConvertGameObjectToEntity。

```c#
public class RotationSpeedAuthoring_IJobEntityBatch : MonoBehaviour, IConvertGameObjectToEntity
{
    public float DegreesPerSecond = 360.0F;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new RotationSpeed_IJobEntityBatch { RadiansPerSecond = math.radians(DegreesPerSecond) };
        dstManager.AddComponentData(entity, data);
    }
}

[Serializable]
public struct RotationSpeed_IJobEntityBatch : IComponentData
{
    public float RadiansPerSecond;
}
```

## 使用IJobEntityBatch为System组织功能

使用继承自**IJobEntityBatch**的Job来实现逻辑，这样Job会根据内存块而不是实体块来遍历Entity，速度会极大提升。

IJobChunk已被IJobEntityBatch替代。

```c#
public partial class RotationSpeedSystem_IJobChunk : SystemBase
{
    EntityQuery m_Query;

    protected override void OnCreate()
    {
        //缓存具有Rotation和只读RotationSpeed_IJobEntityBatch的Entity列表
        m_Query = GetEntityQuery(typeof(Rotation), ComponentType.ReadOnly<RotationSpeed_IJobEntityBatch>());
    }

    [BurstCompile]
    struct RotationSpeedJob : IJobEntityBatch
    {
        public float DeltaTime;
        //使用ComponentTypeHandle来访问Entity的组件
        public ComponentTypeHandle<Rotation> RotationTypeHandle;
        [ReadOnly] public ComponentTypeHandle<RotationSpeed_IJobEntityBatch> RotationSpeedTypeHandle;
        
        //ArchetypeChunk为一块包含Entities的内存块
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunkRotations = batchInChunk.GetNativeArray(RotationTypeHandle);
            var chunkRotationSpeeds = batchInChunk.GetNativeArray(RotationSpeedTypeHandle);
            for (var i = 0; i < batchInChunk.Count; i++)
            {
                var rotation = chunkRotations[i];
                var rotationSpeed = chunkRotationSpeeds[i];

                //因为通过句柄进行更新，所以无需重新赋值回去
                chunkRotations[i] = new Rotation
                {
                    Value = math.mul(math.normalize(rotation.Value),
                        quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * DeltaTime))
                };
            }
        }
    }

    // OnUpdate runs on the main thread.
    protected override void OnUpdate()
    {
        // Explicitly declare:
        // - Read-Write access to Rotation
        // - Read-Only access to RotationSpeed_IJobChunk
        var rotationType = GetComponentTypeHandle<Rotation>();
        //括号内选项为isReadOnly
        var rotationSpeedType = GetComponentTypeHandle<RotationSpeed_IJobEntityBatch>(true);
        //安排Job
        var job = new RotationSpeedJob()
        {
            RotationTypeHandle = rotationType,
            RotationSpeedTypeHandle = rotationSpeedType,
            DeltaTime = Time.DeltaTime
        };
		//计划Job
        Dependency = job.ScheduleParallel(m_Query, Dependency);
    }
}
```

## 子场景

（目前在2020LTS中有Bug，创建子场景会报错）

可以通过子场景流式加载和卸载场景内容，因为子场景是从实体二进制文件中加载，内部的GameObject已经在编辑时而非运行时转换成Entity，所以性能非常高。

通过代码加载和卸载子场景：

```c#
public partial class SubSceneLoader : SystemBase
{
    private SceneSystem sceneSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        sceneSystem = World.GetOrCreateSystem<SceneSystem>();
    }

    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            sceneSystem.LoadSceneAsync(SubSceneLoaderReference.Instance.map.SceneGUID);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            sceneSystem.UnloadScene(SubSceneLoaderReference.Instance.map.SceneGUID);
        }
    }
}

public class SubSceneLoaderReference : MonoBehaviour
{
    //在Inspector中挂载子场景
    public SubScene map;
    
    public static SubSceneLoaderReference Instance{get;private set;}
    
    private void Awake()
    {
        Instance = this;
    }
}

```

## 通过预制体在Mono中生成Entity

```c#
public class Spawner_FromMonoBehaviour : MonoBehaviour
{
    public GameObject Prefab;
    public int CountX = 100;
    public int CountY = 100;

    void Start()
    {
        // Create entity prefab from the game object hierarchy once
        var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null);
        var prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(Prefab, settings);
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        for (var x = 0; x < CountX; x++)
        {
            for (var y = 0; y < CountY; y++)
            {
                // 从已经转换成Entity的Prefab中复制
                var instance = entityManager.Instantiate(prefab);
                // Place the instantiated entity in a grid with some noise
                var position = transform.TransformPoint(new float3(x * 1.3F, noise.cnoise(new float2(x, y) * 0.21F) * 2, y * 1.3F));
                entityManager.SetComponentData(instance, new Translation {Value = position});
            }
        }
    }
}
```

## 通过预制体在System中生成Entity

```c#
//先定义一个用于数据传输的Component
public struct Spawner_FromEntity : IComponentData
{
    public int CountX;
    public int CountY;
    public Entity Prefab;
}

public class SpawnerAuthoring_FromEntity : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject Prefab;
    public int CountX;
    public int CountY;

    //必须声明引用的预制件，以便转换系统提前知道它们
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(Prefab);
    }

    // Lets you convert the editor data representation to the entity optimal runtime representation
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new Spawner_FromEntity
        {
            // 因为之前声明过引用的预制体，这里可以直接使用转换系统去获取预制体转换后的Entity
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
            CountX = CountX,
            CountY = CountY
        };
        dstManager.AddComponentData(entity, spawnerData);
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class SpawnerSystem_FromEntity : SystemBase
{
    //使用Job去平行处理生成的Entity的位置
    [BurstCompile]
    struct SetSpawnedTranslation : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;

        public NativeArray<Entity> Entities;
        public float4x4 LocalToWorld;
        public int Stride;

        public void Execute(int i)
        {
            var entity = Entities[i];
            var y = i / Stride;
            var x = i - (y * Stride);

            TranslationFromEntity[entity] = new Translation()
            {
                Value = math.transform(LocalToWorld, new float3(x * 1.3F, noise.cnoise(new float2(x, y) * 0.21F) * 2, y * 1.3F))
            };
        }
    }

    protected override void OnUpdate()
    {
        //暂时不明白为什么是结构变更时再进行操作
        Entities.WithStructuralChanges().ForEach((Entity entity, int entityInQueryIndex,
            in Spawner_FromEntity spawnerFromEntity, in LocalToWorld spawnerLocalToWorld) =>
        {
            Dependency.Complete();

            var spawnedCount = spawnerFromEntity.CountX * spawnerFromEntity.CountY;
            //对于声明数组后立刻写入整个数组而不读取内容的情况可以使用UninitializedMemory来提高性能
            var spawnedEntities =
                new NativeArray<Entity>(spawnedCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			//使用预制体的Entity来生成和注入数组
            EntityManager.Instantiate(spawnerFromEntity.Prefab, spawnedEntities);
            //将spawner进行销毁，不然每帧都会进行生成操作
            EntityManager.DestroyEntity(entity);

            var translationFromEntity = GetComponentDataFromEntity<Translation>();
            var setSpawnedTranslationJob = new SetSpawnedTranslation
            {
                TranslationFromEntity = translationFromEntity,
                Entities = spawnedEntities,
                LocalToWorld = spawnerLocalToWorld.Value,
                Stride = spawnerFromEntity.CountX
            };
            Dependency = setSpawnedTranslationJob.Schedule(spawnedCount, 64, Dependency);
            Dependency = spawnedEntities.Dispose(Dependency);
        }).Run(); //注意创建和删除实体需要在主线程上运行，以免出现竞态条件
    }
}
```

## 使用EntityCommandBuffer执行无法在Job中进行的操作

对于Entity的创建和销毁等操作必须在主线程上执行，想要在Job中实现相关逻辑，可以使用EntityCommandBuffer将命令缓存起来，等待Job执行结束后再从Buffer中提取命令然后在主线程中执行。需要注意的是，使用CommandBuffer通常会有一帧的滞后（Job完成的下一帧）。

Buffer的创建：

```c#
EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
//如果是为TempJob的CommandBuffer，需要显式调用Dispose()
entityCommandBuffer.Dispose();

//从BeginInitializationEntityCommandBufferSystem中创建CommandBuffer
//这样可以在Inithalizetion阶段就进行创建操作
BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
EntityCommandBuffer entityCommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
//从EndSimulationEntityCommandBufferSystem中创建CommandBuffer
//这可以在Update帧的末尾执行CommandBuffer中的指令
EntityCommandBuffer entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();

//获取Buffer的写入句柄，通过Writer中的API来写入命令缓存
var commandBufferWriter = entityCommandBuffer.AsParallelWriter();
```

| 常见的Buffer命令 |                        |
| ---------------- | ---------------------- |
| Instantiate      | 生成Entity             |
| DestroyEntity    | 销毁Entity             |
| PlayBack         | 主动回放所有记录的命令 |
| AddComponent     | 向Entity添加Component  |
| SetComponent     | 设置Component的数据    |

创建实体示例：

```c#
//分入到SimulationSystemGroup中（但是默认的自定义System都在此Group中，意义不明）
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class SpawnerSystem_SpawnAndRemove : SystemBase
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        //缓存在字段中，不必每帧都获取
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
		// 由于这个作业只运行在第一帧，我们要确保 Burst 在运行前编译它以获得最佳性能（With Burst 的第 3 个参数）
        // 实际作业一旦编译就会被缓存（它只会被 Burst 编译一次）。
        Entities
            .WithName("SpawnerSystem_SpawnAndRemove")
            .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
            .ForEach((Entity entity, int entityInQueryIndex, in Spawner_SpawnAndRemove spawner, in LocalToWorld location) =>
            {
                var random = new Random(1);

                for (var x = 0; x < spawner.CountX; x++)
                {
                    for (var y = 0; y < spawner.CountY; y++)
                    {
                        //通过buffer记录命令
                        var instance = commandBuffer.Instantiate(entityInQueryIndex, spawner.Prefab);
                        var position = math.transform(location.Value, new float3(x * 1.3F, noise.cnoise(new float2(x, y) * 0.21F) * 2, y * 1.3F));
                        commandBuffer.SetComponent(entityInQueryIndex, instance, new Translation { Value = position });
                        commandBuffer.SetComponent(entityInQueryIndex, instance, new LifeTime { Value = random.NextFloat(10.0F, 100.0F) });
                    }
                }

                commandBuffer.DestroyEntity(entityInQueryIndex, entity);
            }).ScheduleParallel();
		//这个API为了告诉系统在可以回放这些命令前需要完成哪些Job
        m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
```

销毁实体示例：

```c#
public partial class LifeTimeSystem : SystemBase
{
    EntityCommandBufferSystem m_Barrier;

    protected override void OnCreate()
    {
        //系统创建在帧末尾
        m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_Barrier.CreateCommandBuffer().AsParallelWriter();

        var deltaTime = Time.DeltaTime;
        Entities.ForEach((Entity entity, int nativeThreadIndex, ref LifeTime lifetime) =>
        {
            lifetime.Value -= deltaTime;

            if (lifetime.Value < 0.0f)
            {
                commandBuffer.DestroyEntity(nativeThreadIndex, entity);
            }
        }).ScheduleParallel();

        m_Barrier.AddJobHandleForProducer(Dependency);
    }
}
```

## 使用Isystem

使用Isystem而非SystemBase可以在主线程上使用BurstCompile，加速System中的Update操作。

```c#
//注意这里的是结构而不是类，可以使用BurstComplie加速
[BurstCompile]
public struct RotationSpeedSystem_IJobChunkStructBased : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    // OnUpdate runs on the main thread.
    // 2020.2之后可以使用BurstComplie编译系统的OnUpdate
#if UNITY_2020_2_OR_NEWER
    [BurstCompile]
#endif
    public void OnUpdate(ref SystemState state)
    {
        //SystemBase中的各种访问器都在state中
    	 state.Entities
                .WithName("RotationSpeedSystemForEachISystem")
                .ForEach((ref Rotation rotation, in RotationSpeed_ForEach_ISystem rotationSpeed) =>
                {
                    //DoSomething
                })
                .ScheduleParallel();
    	//使用Job的话这么调用
        state.Dependency = job.ScheduleSingle(m_Group, state.Dependency);
    }

    public void OnDestroy(ref SystemState state)
    {
    }
}
```

## 使用IJobEntity

IJobEntity是对IJobEntityBatch的上级封装，目的在于简化IJobBatch中对组件句柄的获取等操作。

```c#
public partial struct RotateEntityJob : IJobEntity
{
    public float DeltaTime;
	//直接在Execute中声明所需的组件
    public void Execute(ref Rotation rotation, in RotationSpeed_IJobEntity speed)
    {
        rotation.Value =
            math.mul(
                math.normalize(rotation.Value),
                quaternion.AxisAngle(math.up(), speed.RadiansPerSecond * DeltaTime));
    }
}
public partial class RotationSpeedSystem_IJobEntity : SystemBase
{
    // OnUpdate runs on the main thread.
    protected override void OnUpdate()
    {
        new RotateEntityJob {DeltaTime = Time.DeltaTime}.Schedule();
    }
}
```

