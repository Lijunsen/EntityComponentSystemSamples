# 如何在Mono获取DOTS的事件

目前官方并没有提供清晰的事件传递思路，以下提供两种方法。

## 方法一	使用NativeQueue

在系统中使用NativeQueue在执行时记录，最后出列调用事件

```c#

public class ASystem : SystemBase
{
    public event EventHandler<EventData> anEvent;

    public struct EventData
    {
        //在此设置传输数据
    }

    private NativeQueue<EventData> eventQueue;

    protected override void OnCreate()
    {
        base.OnCreate();
        //设置为Persistent确保系统不会多次创建Queue
        eventQueue = new NativeQueue<EventData>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        eventQueue.Dispose();
    }

    protected override void OnUpdate()
    {
        //需要使用Writer并行写入队列
        NativeQueue<EventData>.ParallelWriter eventQueueParallelWriter = eventQueue.AsParallelWriter();
        //在遍历执行中将事件入列
        var jobHandle = Entities.ForEach((() =>
        {
            eventQueueParallelWriter.Enqueue(new EventData(){
                //写入数据
                });
        })).Schedule(new JobHandle());
        //注意Job并没有实际执行需要显式调用complete，但是这会导致阻塞线程没有用到多线程的资源
        jobHandle.Complete();

        while (eventQueue.TryDequeue(out var eventData))
        {
            anEvent?.Invoke(this,eventData);
        }
    }
}
```

在Mono中订阅事件：

```c#
 void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ASystem>().anEvent += TestEvent;
    }

    private void TestEvent(object sender, ASystem.EventData e)
    {
        //DoSomething
    }
```

## 方法二 	使用IComponent组件

在同一帧触发Event，缺点是会阻塞线程

```c#

public class ASystem : SystemBase
{
    public event EventHandler<EventData> anEvent;

    public struct EventComponent :IComponentData
    {
        public EventData data;
        public double elapsedTime;
    }

    public struct EventData
    {
        //在此设置传输数据
    }
   
    protected override void OnUpdate()
    {
        //创建一块buffer来储存命令
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob); 
        //获取平行写入的writer
        EntityCommandBuffer.ParallelWriter entityCommandBufferParallelWriter = entityCommandBuffer.AsParallelWriter();
        //构建Entity原型
        EntityArchetype archetype = EntityManager.CreateArchetype(typeof(EventComponent));

        var time = Time.ElapsedTime;
        JobHandle jobHandle = Entities.ForEach((int entityInQueryIndex) =>
        {
            //满足条件创建实体
          var entity = entityCommandBufferParallelWriter.CreateEntity(entityInQueryIndex, archetype);
          entityCommandBufferParallelWriter.SetComponent(entityInQueryIndex,entity,new EventComponent()
          {
              //设置数据
              elapsedTime = time
          });
            
        }).Schedule(new JobHandle());

        //依旧需要阻塞线程，不然下面的销毁会报错
        jobHandle.Complete();
		//Job执行结束后将Buffer中的命令回滚执行，创建Entity
        entityCommandBuffer.Playback(EntityManager);
        entityCommandBuffer.Dispose();

        //发送事件，需要在主线程上执行
        //ForEach默认使用Burst，而Burst无法引用字段
        Entities.WithoutBurst().ForEach((ref EventComponent eventComponent) =>
        {
            //检测是否在同一帧触发
            Debug.Log(time + " ### "+ eventComponent.elapsedTime);
            anEvent?.Invoke(this,eventComponent.data);
        }).Run();

        //摧毁生成的Entity，要在Job完成后才能进行销毁
        EntityManager.DestroyEntity(GetEntityQuery(typeof(EventComponent)));

    }
}
```

不阻塞线程的方法，缺点是会在下一帧才触发

```c#

public class ASystem : SystemBase
{
    public event EventHandler<EventData> anEvent;

    public struct EventComponent :IComponentData
    {
        public EventData data;
        public double elapsedTime;
    }

    //帧结束时的命令缓存系统
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    public struct EventData
    {
        //在此设置传输数据
    }
    

    protected override void OnCreate()
    {
        base.OnCreate();
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        //创建一块buffer来储存命令。从endSimulationEntityCommandBufferSystem创建的Buffer会在帧结束时执行
        EntityCommandBuffer entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
        //获取平行写入的writer
        EntityCommandBuffer.ParallelWriter entityCommandBufferParallelWriter = entityCommandBuffer.AsParallelWriter();
        //构建Entity原型
        EntityArchetype archetype = EntityManager.CreateArchetype(typeof(EventComponent));

        var time = Time.ElapsedTime;
        JobHandle jobHandle = Entities.ForEach((int entityInQueryIndex) =>
        {
            //满足条件创建实体
          var entity = entityCommandBufferParallelWriter.CreateEntity(entityInQueryIndex, archetype);
          entityCommandBufferParallelWriter.SetComponent(entityInQueryIndex,entity,new EventComponent()
          {
              //设置数据
              elapsedTime = time
          });
            
        }).Schedule(new JobHandle());
        //这行功能不太明白
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
        //新建触发事件后销毁Entity的命令缓存
        EntityCommandBuffer captureEventsCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();

        //发送事件，需要在主线程上执行
        //ForEach默认使用Burst，而Burst无法引用字段
        Entities.WithoutBurst().ForEach((Entity entity,ref EventComponent eventComponent) =>
        {
            //检测是否在同一帧触发
            Debug.Log(time + " ### "+eventComponent.elapsedTime);
            anEvent?.Invoke(this,eventComponent.data);
            //摧毁生成的Entity
            captureEventsCommandBuffer.DestroyEntity(entity);
        }).Run();
    }
}
```

