using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class CubeSettingAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Header("Cube阵列的设置")]
    public int widthNumber;
    public int heightNumber;
    [Header("运行时的数据")]
    [Tooltip("Cube追随目标位置的速度")]
    public float speed;
    [Tooltip("Cube阵列的最高高度")]
    public float maxHeight;
    [Tooltip("每个Cube之间的间隔")]
    public float spacing;
    [Tooltip("Cube渐变的模式")]
    public CommonSettingComponent.CubeTransitionTpe cubeTransitionType;

    [Space]
    [Header("音频数据样本的设置")]
    [Tooltip("音频样本数据的增益")]
    public float gain;
    [Min(64)]
    [Tooltip("获取音频样本量的长度")]
    public int audioBufferLength;
    [Tooltip("展示数据的类型，合并频段还是分开频段展示")]
    public CommonSettingComponent.ShowAudioDataType showAudioDataType;
    [Tooltip("获取音频样本的频率范围")]
    public AudioSystem.FrequencyRange frequencyRangeType;

    [Space]
    [Header("其他设置")]
    public float updateFrequency;
    public bool addURPColorComponent;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity,new CommonSettingComponent()
        {
            mapWidth = widthNumber,
            mapHeight = heightNumber,
            cubeMaxHeightInYAxis = maxHeight,
            cubeSpacing = spacing,
            cubeSpeedInYAxis = speed,
            audioBufferLength = audioBufferLength,
            frequencyRangeType = frequencyRangeType,
            showAudioDataType = showAudioDataType,
            audioDataGain = gain,
            cubeTransitionTpe = cubeTransitionType,
            updateFrequency = updateFrequency,
            addURPColorComponent = addURPColorComponent,
        });
        
    }
}
