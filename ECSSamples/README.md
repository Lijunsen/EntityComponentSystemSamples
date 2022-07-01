# Audio Effect In ECS Demo

这是探究在DOTS框架下表现音频可视化效果的分支仓库。

## 版本要求

此演示基于Unity官方仓库`EntityComponentSystemSamples`，版本为2020.3.34f1，升级Unity版本将造成Job相关API修改导致报错和其他未知错误。请基于此版本打开工程。

## 演示简介

本分支修改内容可在Assets/MySample中找到。演示场景中通过CommandBufferSystem和BurstCompile异步生成48x48x10=23040个Cube阵列来组成效果方阵，使用AudioSystem在ECS环境下读取AudioSource的频谱数据保存于AudioDataArray中，然后使用多个CubeSystem读取AudioData对Cube进行属性上的修改。

### Demo1

<div align = center>
   <img src="Res\CommonCube1.gif" alt="CommonCube1"/>
</div>

此Demo演示场景位于Assets/MySample/Demo1/Scene/CommonCube.unity中。通过控制Cube的Transform中的Position来达到频谱的可视化效果（参考CubeTranslationTransitionSystem.cs）。靠近视角端为低频，远离视角端为高频，频谱由左侧更新向右侧传递，可通过查看名为“Settings”的GameObject修改各项数值。

<div align = center>
   <img src="Res\Settings.png" alt="Settings"/>
</div>



### Demo2

此Demo演示场景位于Assets/MySample/Demo1/Scene/LightingCube.unity中。此场景通过添加CubeLightTransitionSystem对Cube的材质属性等进行数值更新，表现出更多样的风格。具体设置可查看名为“Light Setting”的GameObject。

<div align = center>
   <img src="Res\LightSetting.png" alt="LightSettings"/>
</div>

#### 效果1

在Demo1的基础上使用URP表现半透明和Bloom后处理效果，营造出黑客帝国的荧光数字雨的效果。

<div align = center>
   <img src="Res\LightingCube1.gif" alt="LightingCube1"/>
</div>

#### 效果2

从侧面营造层峦叠嶂的山峰感，为了避免遮挡，在低频和高频的过渡上进行了颜色区分。

<div align = center>
   <img src="Res\LightingCube2.gif" alt="LightingCube2"/>
</div>

### Demo3

表现半透明灰色的冷科技感，未完成。