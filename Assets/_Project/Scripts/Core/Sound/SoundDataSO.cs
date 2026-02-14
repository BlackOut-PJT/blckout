using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoundDataSO", menuName = "ScriptableObjects/SoundData")]
public class SoundDataSO : ScriptableObject
{
    [System.Serializable]
    public struct SoundEntry
    {
        public string name;
        public AudioClip clip;
    }

    public List<SoundEntry> soundEntries = new List<SoundEntry>();
}
