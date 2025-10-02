using UnityEngine;

public enum InstrumentType
{
    Bass,
    Drums,
    Instrumental
}

[CreateAssetMenu(fileName = "InstrumentDataSO", menuName = "ScriptableObjects/InstrumentData")]

public class InstrumentData : ScriptableObject
{
    public int instrumentID;
    public string song;
    public InstrumentType instrumentType;
    public AudioClip previewClip;
}