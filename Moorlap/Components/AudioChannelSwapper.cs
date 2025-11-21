using UnityEngine;

namespace Moorlap.Components;

/// <summary>
/// Component which, when added to an audio source or listener, swaps the left and right channels.
/// </summary>
public class AudioChannelSwapper : MonoBehaviour
{
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (channels != 2) return;
        
        for (int i = 0; i < data.Length; i += 2)
        {
            float tmp = data[i];
            data[i] = data[i + 1];
            data[i + 1] = tmp;
        }
    }
}
