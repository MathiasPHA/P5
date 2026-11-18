using System;
using UnityEngine;

// Plays sounds from a list, called from Animation Events.
// Put it on the same GameObject as the Animator.
[RequireComponent(typeof(AudioSource))]
public class AnimationSounds : MonoBehaviour
{
    [Serializable]
    public class Sound
    {
        public string name = "New Sound";
        [Tooltip("One is picked at random each time")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Random pitch between these values (1 = normal)")]
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    }

    [SerializeField] private Sound[] sounds;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    // Unity fills new list entries with zeros; give them usable defaults instead
    void OnValidate()
    {
        if (sounds == null) return;

        foreach (var s in sounds)
        {
            if (s == null) continue;
            if (s.pitchRange == Vector2.zero) s.pitchRange = new Vector2(0.95f, 1.05f);
            if (s.volume == 0f && (s.clips == null || s.clips.Length == 0)) s.volume = 1f;
            if (string.IsNullOrEmpty(s.name)) s.name = "New Sound";
        }
    }

    // Animation Event: string parameter = the sound's name
    public void PlaySound(string soundName)
    {
        var sound = Array.Find(sounds, s => s.name == soundName);
        if (sound == null)
        {
            Debug.LogWarning($"[AnimationSounds] No sound named '{soundName}' on {name}.", this);
            return;
        }
        Play(sound);
    }

    // Animation Event: int parameter = position in the list (0 = first)
    public void PlaySoundIndex(int index)
    {
        if (sounds == null || index < 0 || index >= sounds.Length)
        {
            Debug.LogWarning($"[AnimationSounds] No sound at index {index} on {name}.", this);
            return;
        }
        Play(sounds[index]);
    }

    private void Play(Sound sound)
    {
        if (sound.clips == null || sound.clips.Length == 0) return;

        var clip = sound.clips[UnityEngine.Random.Range(0, sound.clips.Length)];
        if (clip == null) return;

        // Fall back to normal pitch if the range is unset or invalid
        bool validRange = sound.pitchRange.x > 0f && sound.pitchRange.y > 0f;
        source.pitch = validRange ? UnityEngine.Random.Range(sound.pitchRange.x, sound.pitchRange.y) : 1f;

        source.PlayOneShot(clip, sound.volume);
    }
}