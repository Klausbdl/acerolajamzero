using UnityEngine;

public class AudioLibrary : MonoBehaviour
{
    [Header("Ambient")]
    public AudioClip computerHumming;

    [Header("SFX")]
    public AudioClip transition;
    public AudioClip[] footsteps;
    public AudioClip[] whoosh;
    public AudioClip[] hit;
    public AudioClip[] hurt;
    public AudioClip jump;
    public AudioClip dash;
    public AudioClip healthUp;
    public AudioClip enemyDie;

    [Header("UI")]
    public AudioClip mouseClick;
    public AudioClip buttonClick;
    public AudioClip buttonBackClick;
    public AudioClip buttonPlayClick;
    public AudioClip buttonTickClick;
    public AudioClip equipModule;
    public AudioClip unequipModule;
    public AudioClip buyModule;
    public AudioClip menuAppear;

    [Header("Music")]
    public AudioClip[] mainMenu;
    public AudioClip[] intermissionMenu;
    public AudioClip[] gameplay;
}
