# BlobAsset

## 什么是BlobAsset？

本质是不可修改的数据容器，可以储存数据、动画、单位状态、寻路地图等，然后在所有的Entity上使用，因为不可修改，所以在多线程中可安全快速访问。

## 如何创建BlobAsset

1. 使用GameObject转换系统构建BlobAsset。
2. 定义所有资源中要储存的容器，同时定义储存数据的结构体，一种使用BlobArray，储存几个数据实例的普通数组，一种是BlobPointer，指向单个数据实例的普通指针，一种是使用BlobString，储存普通的字符串。
3. 创建BlobAsset参照，用作在Entity组件中储存Asset的参照。
4. 储存参照在组件中。

## 代码示例

BlobAsset的创建：

```c#
public struct Waypoint{
    public float3 position;
}

public struct WaypointBlobAsset{
    public BlobArray<WayPoint> waypointArray;
}
```

构建储存BlobAsset参考的组件

```c#
public struct WaypointFollow : IComponentData
{
    public BlobAssetReference<WaypointBlobAsset> waypointBlobAssetReference;
}
```

使用转换系统构建参照：

```c#
//归入转换系统之后的Group中运行
[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
public class WaypointBlobAssetConstructor : GameObjectConversionSystem
{
    //即使是Update但是对于系统来说只运行一次
    protected override void OnUpdate()
    {
        BlobAssetReference<WaypointBlobAsset> reference;
        using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
        {
            //创建资源，记得使用ref，否则只是值引用
            ref WaypointBlobAsset waypointBlobAsset = ref blobBuilder.ConstructRoot<WaypointBlobAsset>();
            //为BlobAsset分配内存并注入数据
            BlobBuilderArray<Waypoint> waypointArray = blobBuilder.Allocate(ref waypointBlobAsset.waypointArray, 3);
            waypointArray[0] = new Waypoint() { position = new float3(0, 0, 0) };
            waypointArray[1] = new Waypoint() { position = new float3(5, 0, 0) };
            waypointArray[2] = new Waypoint() { position = new float3(2.5f, 2.5f, 0) };
            //创建参照
            reference = blobBuilder.CreateBlobAssetReference<WaypointBlobAsset>(Allocator.Persistent);

            //在EntityConversionSystem中包含两个EntityManager
            //一个是普通是EntityManager，管理待转换世界中的Entity
            //如果想访问转换后的EntityManager，则使用DstEntityManager
            //获取转换后的Entity
            EntityQuery targetEntityQuery = DstEntityManager.CreateEntityQuery(typeof(Tag_Player));//筛选具有某种组件的Entity
            Entity targetEntity = targetEntityQuery.GetSingletonEntity();

            //将数据参考注入组件中，使用Reference.value提供数据供System使用
            DstEntityManager.AddComponentData(targetEntity, new WaypointFollow()
            {
                waypointBlobAssetReference = reference
            });
        }
    }
}
```

