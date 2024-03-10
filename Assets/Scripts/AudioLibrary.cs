using UnityEngine;

public class AudioLibrary : MonoBehaviour
{
    [Header("Ambient")]
    public AudioClip computerHumming;

    [Header("SFX")]
    public AudioClip jump;
    public AudioClip transition;

    [Header("UI")]
    public AudioClip mouseClick;
    public AudioClip buttonClick;
    public AudioClip buttonBackClick;
    public AudioClip buttonPlayClick;
    public AudioClip buttonTickClick;
    public AudioClip equipModule;
    public AudioClip unequipModule;
    public AudioClip buyModule;

    [Header("Music")]
    public AudioClip[] mainMenu;
    public AudioClip[] intermissionMenu;
    public AudioClip[] gameplay;

    [Header("SFX")]
    public AudioClip[] footsteps;
}
