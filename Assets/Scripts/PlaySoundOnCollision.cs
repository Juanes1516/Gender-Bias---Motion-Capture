using UnityEngine;

public class PlaySoundOnCollision : MonoBehaviour
{
    // Arrastra el clip de audio desde la carpeta "Assets" aquí en el Inspector
    public AudioClip collisionSound;

    // Variable para controlar el volumen desde el Inspector (0.0 a 1.0)
    [Range(0.0f, 1.0f)]
    public float volume = 0.5f;

    // Variable para controlar el tono (Pitch) del sonido (-3.0 a 3.0)
    // Un valor menor a 1.0 lo hace más grave, mayor a 1.0 lo hace más agudo.
    [Range(-3.0f, 3.0f)]
    public float pitch = 1.0f;

    // Variable para controlar la espacialización (Spatial Blend) del sonido (0.0 a 1.0)
    // 0.0 = 2D (seco, sin espacialización), 1.0 = 3D (se espacializa en el espacio 3D).
    [Range(0.0f, 1.0f)]
    public float spatialBlend = 1.0f;

    private AudioSource audioSource;

    void Start()
    {
        // Agrega el componente AudioSource al objeto si no lo tiene
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Asigna las propiedades de audio que definimos en el Inspector
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.spatialBlend = spatialBlend;
    }

    // Este método se llama cuando el objeto colisiona con otro
    void OnCollisionEnter(Collision collision)
    {
        // Verifica si el objeto con el que colisionó tiene el tag "Ball"
        if (collision.gameObject.CompareTag("Ball"))
        {
            // Reproduce el sonido una sola vez con los ajustes de volumen y tono
            audioSource.PlayOneShot(collisionSound);
        }
    }
}