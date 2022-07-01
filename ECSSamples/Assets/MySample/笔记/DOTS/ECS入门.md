# ECS入门

ECS分为Entity、Component、System，Entity作为组件实体概念类似于GameObject，Component则作为组件挂载在Entity上实现所需功能，System则作为系统以整体层面对Entity进行操作。

## Component

继承自IComponentData接口的Struct可以作为Entity的Component，用于记录数据等。

官方提供的组件有：

| Unity.Transforms.Translation | 具有一个float3属性，用于更新Entity的Transform |
| ---------------------------- | :-------------------------------------------- |
| Unity.Rendering.RenderMesh   | 具有网格，材质等属性，用于渲染实体            |
| LocalToWorld                 | 为了能看见实体，需要添加此组件                |

## Entity

### Entity在Mono中的创建

使用EntityMananger来创建Entity：

```c#
EntityArchetype antityArchetype = entityManager.CreateArchetype(typeof(需要的组件),…);//创建想要的Entity原型
NativeArray<Entity> entityArray = new NativeArray<Entity>(number,Allocator.Temp); //创建想要数量的Enitity数组
entityManager.CreateEntity(entityArchetype,entityArray); //使用原型创建Entity填充数组
Foreach(var entity in entityArray){
	enitityManager.SetComponentData(entity,new Component{
        //初始化数据
    }); //对每个Entity中的组件进行数据初始化
}
entityArray.Dispose(); //显式注销数组
```

## System

继承自ComponentSystem的类作为System对Entity进行操作：

```c#
Override void OnUpdate()｛
	Entities.Foreach((ref 想要的组件类型 component, ….)=>{
		//对所有拥有对应组件的Entity进行操作
	})
｝
```

## 对于2020版本中ECS的更改

以上内容已在Unity2020LTS版本中有了较大变化，有以下几点：

1. System继承的类名修改为**SystemBase**，且需要使用partial
2. 