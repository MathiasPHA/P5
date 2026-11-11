using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SlidingSound : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float minSpeed = 0.03f; //Alt under denne fart er muted
    [SerializeField] private float maxSpeed = 0.5f; //Når denne fart rammes spilles lyden på fuld styrke

    [Header("Sound")]
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private float fadeSpeed = 8f; // Hvor hurtigt følger lyden med
    [SerializeField] private float minPitch = 0.9f; //Langsomste pitch
    [SerializeField] private float maxPitch = 1.1f; // Hurtigste pitch

    private AudioSource source;
    private Vector3 lastPosition;
    private float smoothedSpeed;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        lastPosition = transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Time.deltaTime <= 0f) return;

        //Hvor hurtigt bevægede objektet sig siden sidste frame?
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        //Gør det smooth for at counter hand tracking jitternes
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 10f*Time.deltaTime);

        // Jo hurtigere, desto højere
        float targetVolume = 0f;
        if (smoothedSpeed > minSpeed)
        {
            float t = Mathf.InverseLerp(minSpeed, maxSpeed, smoothedSpeed);
            targetVolume = t * maxVolume;
            source.pitch = Mathf.Lerp(minPitch, maxPitch, t);
        }

        source.volume = Mathf.MoveTowards(source.volume, targetVolume, fadeSpeed * Time.deltaTime);

        //Spil kun når der er noget at høre, altså minvolume
        if (source.volume > 0.001f && !source.isPlaying) source.Play();
        else if (source.volume <= 0.001f && source.isPlaying) source.Pause();
    }
}