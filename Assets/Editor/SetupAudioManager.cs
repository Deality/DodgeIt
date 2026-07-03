using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupAudioManager
{
    public static void Execute()
    {
        GameObject go = GameObject.Find("AudioManager");
        if (go == null) { Debug.LogError("AudioManager game object not found!"); return; }

        AudioManager mgr = go.GetComponent<AudioManager>();
        if (mgr == null) { Debug.LogError("AudioManager component not found!"); return; }

        // Ensure we have exactly 3 AudioSource components
        AudioSource[] sources = go.GetComponents<AudioSource>();
        AudioSource musicSrc  = sources.Length > 0 ? sources[0] : go.AddComponent<AudioSource>();
        AudioSource sfxSrc    = sources.Length > 1 ? sources[1] : go.AddComponent<AudioSource>();
        AudioSource engineSrc = sources.Length > 2 ? sources[2] : go.AddComponent<AudioSource>();

        // Configure each source
        musicSrc.loop        = true;
        musicSrc.playOnAwake = false;
        musicSrc.volume      = 0.4f;

        sfxSrc.loop        = false;
        sfxSrc.playOnAwake = false;
        sfxSrc.volume      = 1.0f;

        engineSrc.loop        = true;
        engineSrc.playOnAwake = false;
        engineSrc.volume      = 0.6f;

        // Wire sources into AudioManager
        mgr.musicSource  = musicSrc;
        mgr.sfxSource    = sfxSrc;
        mgr.engineSource = engineSrc;

        // Assign audio clips
        mgr.backgroundMusic  = Load("Assets/Sounds/MarketSound.wav");
        mgr.crashSound       = Load("Assets/Sounds/CarAccident_Sound.wav");
        mgr.coinSound        = Load("Assets/Sounds/Coin Sound.wav");
        mgr.powerUpSound     = Load("Assets/Sounds/SpeedReducer.wav");
        mgr.boostSound       = Load("Assets/Sounds/BoostSound.wav");
        mgr.nearMissSound    = Load("Assets/Sounds/NearMiss-Sound.wav");
        mgr.truckHornSound   = Load("Assets/Sounds/TruckHorn.wav");
        mgr.buttonClickSound = Load("Assets/Sounds/Buton-Sound-Finished.wav");

        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(go.scene);

        Debug.Log("AudioManager wired up: 3 AudioSources assigned, 8 clips loaded.");
    }

    static AudioClip Load(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogWarning($"Clip not found at: {path}");
        return clip;
    }
}
