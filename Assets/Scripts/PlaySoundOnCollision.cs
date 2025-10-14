using UnityEngine;

public class PlaySoundOnCollision : MonoBehaviour
{
    // ... (Variables de audio iguales)
    public AudioClip collisionSound;
    [Range(0.0f, 1.0f)]
    public float volume = 0.5f;
    [Range(-3.0f, 3.0f)]
    public float pitch = 1.0f;
    [Range(0.0f, 1.0f)]
    public float spatialBlend = 1.0f;

    private AudioSource audioSource;

    void Start()
    {
        // ... (Inicialización igual)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = spatialBlend;
    }

    // ¡CAMBIO CLAVE! Usar OnTriggerEnter en lugar de OnCollisionEnter
    void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto con el que colisionó tiene el tag "Ball"
        // 'other.gameObject' es el objeto con el que ha hecho el trigger (la pelota)
        if (other.gameObject.CompareTag("Ball"))
        {
            // Reproduce el sonido una sola vez
            audioSource.PlayOneShot(collisionSound);
        }
    }
}