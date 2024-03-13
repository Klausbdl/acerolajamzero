using UnityEngine;

public class AudioLibrary : MonoBehaviour
{
    [Header("Ambient")]
    public AudioClip computerHumming;
    public AudioClip birds;
    [Space(8)]
    [Header("SFX")]
    public AudioClip transition;
    public AudioClip doorSound;
    [Header("Player")]
    public AudioClip[] footsteps;
    public AudioClip[] whoosh;
    public AudioClip jump;
    public AudioClip dash;
    [Header("Combat")]
    public AudioClip[] hit;
    public AudioClip[] hitMetal;
    public AudioClip[] hurt;
    public AudioClip enemyDie;
    [Header("Enemy")]
    [Header("Tower")]
    public AudioClip towerShutdown;
    [Space(8)]
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
    public AudioClip healthUp;
    [Space(8)]
    [Header("Music")]
    public AudioClip[] mainMenu;
    public AudioClip[] intermissionMenu;
    public AudioClip[] gameplay;
}
