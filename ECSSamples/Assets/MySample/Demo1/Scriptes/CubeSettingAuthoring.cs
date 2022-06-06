using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class CubeSettingAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Header("Cube���е�����")]
    public int widthNumber;
    public int heightNumber;
    [Header("����ʱ������")]
    [Tooltip("Cube׷��Ŀ��λ�õ��ٶ�")]
    public float speed;
    [Tooltip("Cube���е���߸߶�")]
    public float maxHeight;
    [Tooltip("ÿ��Cube֮��ļ��")]
    public float spacing;
    [Tooltip("Cube�����ģʽ")]
    public CommonSettingComponent.CubeTransitionTpe cubeTransitionType;

    [Space]
    [Header("��Ƶ��������������")]
    [Tooltip("��Ƶ�������ݵ�����")]
    public float gain;
    [Min(64)]
    [Tooltip("��ȡ��Ƶ�������ĳ���")]
    public int audioBufferLength;
    [Tooltip("չʾ���ݵ����ͣ��ϲ�Ƶ�λ��Ƿֿ�Ƶ��չʾ")]
    public CommonSettingComponent.ShowAudioDataType showAudioDataType;
    [Tooltip("��ȡ��Ƶ������Ƶ�ʷ�Χ")]
    public AudioSystem.FrequencyRange frequencyRangeType;

    [Space]
    [Header("��������")]
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
