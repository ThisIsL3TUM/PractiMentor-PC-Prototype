using UnityEngine;

public enum EmbroideryMachinePartType
{
    Spool,
    Knob,
    MachineHead,
    SpringLever,
    PresserFoot,
    Needle
}
public class IndustrialEmbroideryMachineTool : MonoBehaviour
{
    [SerializeField] private EmbroideryMachinePartType partType;
    public EmbroideryMachinePartType PartType => partType;
}
