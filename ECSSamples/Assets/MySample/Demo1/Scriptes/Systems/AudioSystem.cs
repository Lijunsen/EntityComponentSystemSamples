using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
public partial class AudioSystem : SystemBase
{
    public NativeArray<float> audioDataArray;
    private EntityQuery settingQuery;
    // private float[] outputData;
    private float fMax;// = (float)AudioSettings.outputSampleRate/2;
    private float time;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<CommonSettingComponent>();
        RequireSingletonForUpdate<AudioSource>();
        settingQuery = GetEntityQuery(ComponentType.ReadOnly<CommonSettingComponent>());
        audioDataArray = new NativeArray<float>(64, Allocator.Persistent);
        // outputData = new float[64];

        fMax = (float)AudioSettings.outputSampleRate / 2;
    }

    protected override void OnUpdate()
    {
        var settings = settingQuery.GetSingleton<CommonSettingComponent>();
        // if (audioDataArray.Length != settings.audioBufferLength && settings.audioBufferLength >= 64)
        // {
        //     outputData = new float[settings.audioBufferLength];
        // }

        if (audioDataArray.Length != settings.mapWidth * settings.mapWidth)
        {
            audioDataArray.Dispose();
            audioDataArray = new NativeArray<float>(settings.mapWidth * settings.mapWidth, Allocator.Persistent);
        }
        
        Entities.WithoutBurst().ForEach((in AudioSource audiosource) =>
        {
            var timeCheck = false;
            if (settings.updateFrequency == 0)
            {
                timeCheck = true;
            }
            else if(settings.updateFrequency > 0)
            {
                time += Time.DeltaTime;
                var updateFrameTime = 1 / settings.updateFrequency;
                if (time >= updateFrameTime)
                {
                    timeCheck = true;
                    time -= updateFrameTime;
                }
            }
            
            if (timeCheck)
            {
                switch (settings.showAudioDataType)
                {
                    case CommonSettingComponent.ShowAudioDataType.Combine:
                        var dataLength = settings.mapWidth / Enum.GetValues(typeof(FrequencyRange)).Length;
                        if (settings.mapWidth % Enum.GetValues(typeof(FrequencyRange)).Length != 0)
                        {
                            Debug.LogWarning("Cube的横向数量必须是6的倍数");
                            return;
                        }
                        var resultData = GetAudioData(audiosource, 0, settings.audioBufferLength, dataLength, DataType.Max);
                        var oldData = audioDataArray.ToList().GetRange(0, audioDataArray.Length - settings.mapWidth);
                        var newData =new List<float>(settings.audioBufferLength);
                        foreach (var resultDataValue in resultData.Values)
                        {
                            newData.AddRange(resultDataValue);
                        }
                        var combineData = new List<float>();
                        combineData.AddRange(newData);
                        combineData.AddRange(oldData);
                        audioDataArray.CopyFrom(combineData.ToArray());
                        
                        break;
                    case CommonSettingComponent.ShowAudioDataType.Split:
                        resultData = GetAudioData(audiosource, 0, settings.audioBufferLength, settings.mapWidth, DataType.Max);
                        if (resultData.TryGetValue(settings.frequencyRangeType, out var outputData))
                        {
                            oldData = audioDataArray.ToList().GetRange(0, audioDataArray.Length - settings.mapWidth);
                            combineData = new List<float>();
                            combineData.AddRange(outputData);
                            combineData.AddRange(oldData);
                            audioDataArray.CopyFrom(combineData.ToArray());
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }).Run();
    }
    
    protected override void OnDestroy()
    {
        audioDataArray.Dispose();
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="dataLength">每个频段的数据量</param>
    /// <param name="dataType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Dictionary<FrequencyRange, float[]> GetAudioData(AudioSource audioSource,int channel,int sampleLength, int dataLength, DataType dataType)
    {
        var result = new Dictionary<FrequencyRange, float[]>();
        var enumLength = Enum.GetValues(typeof(FrequencyRange)).Length;
        var enumArray = Enum.GetValues(typeof(FrequencyRange));
        for (int i = 0; i < enumLength; i++)
        {
            result.Add((FrequencyRange)enumArray.GetValue(i), new float[dataLength]);
        }
        if (!audioSource.isPlaying)
        {
            return result;
        }

        var freqData = new float[sampleLength];
        audioSource.GetSpectrumData(freqData, channel, FFTWindow.BlackmanHarris);

        for (int i = 0; i < enumLength; i++)
        {
            Vector2 range = GetFreqForRange((FrequencyRange)enumArray.GetValue(i));
            float fLow = range.x;//Mathf.Clamp (range.x, 20, fMax); // limit low...
            float fHigh = range.y;//Mathf.Clamp (range.y, fLow, fMax); // and high frequencies
            int n1 = Mathf.RoundToInt(fLow * sampleLength / fMax);
            int n2 = Mathf.RoundToInt(fHigh * sampleLength / fMax);
            
            List<float> validData = new List<float>();
            for (int j = n1; j <= n2; j++)
            {
                float frequency = freqData[j];
                validData.Add(frequency);
            }

            var resultData = new float[dataLength];
            //样本量不足，进行拉伸填充
            if (dataLength >= validData.Count)
            {
                // Debug.Log($"type:{i},n1:{n1},n2:{n2},count:{n2-n1}");
                for (int j = 0; j < dataLength; j++)
                {
                    resultData[j] = validData[Mathf.FloorToInt(j / dataLength * validData.Count)];
                }
            }
            else
            {
                var samplesPerPack = validData.Count / dataLength;
                for (int j = 0; j < dataLength; j++)
                {
                    float sumPerPack = 0f;
                    float min = 1f;
                    float max = -1f;

                    for (int k = j * samplesPerPack; k < (j + 1) * samplesPerPack; k++)
                    {
                        if (k > validData.Count) break;
                        float b = validData[k];
                        sumPerPack += b * b;
                        min = Mathf.Min(min, b);
                        max = Mathf.Max(max, b);
                    }

                    switch (dataType)
                    {
                        case DataType.Min:
                            resultData[j] = min;
                            break;
                        case DataType.Max:
                            resultData[j] = max;
                            break;
                        case DataType.Rms:
                            resultData[j] = Mathf.Sqrt(sumPerPack / samplesPerPack);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
                    }
                }
            }

            result[(FrequencyRange)enumArray.GetValue(i)] = resultData;
        }

        return result;
    }

    /// <summary>
    /// Return the bounds of a frequency range.
    /// </summary>
    /// <param name="freqRange"></param>
    /// <returns></returns>
    public static Vector2 GetFreqForRange(FrequencyRange freqRange)
    {
        switch (freqRange)
        {
            // case FrequencyRange.SubBase:
            //     return new Vector2(0, 60);
            case FrequencyRange.Bass:
                return new Vector2(0, 250);
            case FrequencyRange.LowMidrange:
                return new Vector2(250, 500);
            case FrequencyRange.Midrange:
                return new Vector2(0, 2000);
            case FrequencyRange.UpperMidrange:
                return new Vector2(2000, 4000);
            case FrequencyRange.High:
                return new Vector2(4000, 6000);
            case FrequencyRange.VeryHigh:
                return new Vector2(6000, 20000);
            // case FrequencyRange.Decibal:
            //     return new Vector2(0, 20000);
            default:
                break;
        }

        return Vector2.zero;
    }

    /// <summary>
    /// Frequency Ranges that we can sample audio from
    /// </summary>
    public enum FrequencyRange
    {
        // SubBase, // 20-60 Hz
        Bass, // 60-250 Hz
        LowMidrange, //250-500 Hz
        Midrange, //500-2,000 Hz
        UpperMidrange, //2,000-4,000 Hz
        High, //4,000-6000 Hz
        VeryHigh, //6,000-20,000 Hz
        // Decibal, // use output data instead of frequency data
    }

    public enum DataType
    {
        Min,
        Max,
        Rms
    }
}
