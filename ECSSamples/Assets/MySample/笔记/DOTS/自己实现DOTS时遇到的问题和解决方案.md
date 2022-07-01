# 遇到的问题和解决方案

## 如何在Entity层级中获取音频数据

在ConvertToEntity组件选择ConvertAndInjectGameObject，然后在System中通过Entities.WithoutBurst().ForEach((in AudioSource audiosource) =>{}.Run进行组件获取，因为需要访问Mono组件，所以只能在主线程上使用。

## 如何在特定场景运行特定系统

方法一：

在System上添加[DisableAutoCreation]特性，然后在继承自ICustomBootSrap接口的类中进行自定义的世界创建：

```c#
public class CustomReserveVirtualMemory : ICustomBootstrap
{
    //Entity会通过反射执行此方法
    public bool Initialize(string defaultWorldName)
    {
        var world = new World(defaultWorldName, WorldFlags.Game);
        World.DefaultGameObjectInjectionWorld = world;
        //获取默认的系统列表
        var systemList = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default).ToList();
        //跟对场景添加特定系统       
        if (SceneManager.GetActiveScene().name == "LightingCube")
        {
            systemList.Add(typeof(AudioSystem));
            systemList.Add(typeof(SpawnerSystem));
            systemList.Add(typeof(CubeTranslationTransitionSystem));
            systemList.Add(typeof(CubeLightTransitionSystem));
        }
        
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world,systemList);
        ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);
		//返回true告诉底层我们自己创建了默认世界
        return true;
    }
}
```

TIps:可以通过WorldSystemFilter和WorldSystemFilterFlags特性来设置系统的分类

方法二：

直接在系统的OnCreate()方法中调用方法:

```c#
 protected override void OnCreate()
    {
     	 //这会寻找单例的Component，如果没有则不执行OnUpdate方法
         RequireSingletonForUpdate<CubeLightSettingComponent>();
    }
```

## 如何在system中修改材质等属性

需要为Entity添加相应的属性组件，在Unity.Rendering库中。

| 相应组件类                       |
| -------------------------------- |
| URPMaterialPropertyBaseColor     |
| URPMaterialPropertyEmissionColor |

## 如何修改Entity的Scale

为Entity添加Unity.Transform.CompositeScale组件。

