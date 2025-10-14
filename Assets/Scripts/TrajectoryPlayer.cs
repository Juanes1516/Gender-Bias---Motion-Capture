using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class TrajectoryPlayer : MonoBehaviour
{
    [Header("Configuración de Archivo")]
    [Tooltip("Arrastra aquí el archivo .txt generado por el script de Python")]
    public TextAsset trajectoryFile;

    [Header("Configuración de Animación")]
    [Tooltip("El segundo del archivo desde el cual comenzará la animación.")]
    public float animationStartTime = 0.0f;
    [Tooltip("Controla la velocidad de la reproducción. 1 = velocidad normal.")]
    public float playbackSpeed = 1.0f;

    [Header("Configuración de Física y Rotación")]
    [Tooltip("El radio de la esfera en unidades de Unity. Ayuda a calcular la rotación correctamente.")]
    public float ballRadius = 0.5f;
    [Tooltip("La magnitud de movimiento mínima para que el balón rote. Aumenta este valor si el balón rota estando quieto.")]
    public float minMovementThreshold = 0.01f;
    [Tooltip("Controla qué tan suavemente reacciona la rotación a los cambios de movimiento. Valores más altos = reacción más rápida.")]
    public float rotationSmoothing = 5.0f;

    // Componente Rigidbody para el movimiento físico
    private Rigidbody rb;

    // Estructura de datos para la trayectoria
    private class PositionData
    {
        public float time;
        public float x, y, z;
    }

    private List<PositionData> trajectory = new List<PositionData>();
    private int currentIndex = 0;
    private float playbackStartTime;
    private bool isInitialized = false;

    // Variables para el movimiento y rotación suavizados
    private Vector3 initialUnityPosition;
    private Vector3 initialDataPosition;
    private Vector3 targetPosition; // La posición a la que queremos mover el balón
    private Vector3 lastPosition;   // La posición en el frame de física anterior
    private Vector3 smoothedMovement; // El vector de movimiento suavizado para la rotación

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("¡Este objeto necesita un componente Rigidbody para funcionar correctamente!");
            enabled = false; // Desactiva el script si no hay Rigidbody
            return;
        }

        // Importante: Hacemos el Rigidbody cinemático para controlarlo por script sin que le afecten fuerzas externas inesperadas.
        rb.isKinematic = true;

        LoadAndInitializeTrajectory();

        if (isInitialized)
        {
            // Inicializamos las posiciones para el cálculo de movimiento
            targetPosition = transform.position;
            lastPosition = transform.position;
        }
    }

    void LoadAndInitializeTrajectory()
    {
        if (trajectoryFile == null)
        {
            Debug.LogError("No se ha asignado un archivo de trayectoria (trajectoryFile).");
            return;
        }

        // (El resto de este método es idéntico al tuyo, funciona bien)
        trajectory = new List<PositionData>();
        string[] lines = trajectoryFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = line.Split(',');
            if (values.Length == 4)
            {
                try
                {
                    PositionData data = new PositionData
                    {
                        time = float.Parse(values[0], CultureInfo.InvariantCulture),
                        x = float.Parse(values[1], CultureInfo.InvariantCulture),
                        y = float.Parse(values[2], CultureInfo.InvariantCulture),
                        z = float.Parse(values[3], CultureInfo.InvariantCulture)
                    };
                    trajectory.Add(data);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"No se pudo parsear la línea {i}: '{line}'. Error: {e.Message}");
                }
            }
        }

        if (trajectory.Count > 0)
        {
            initialUnityPosition = transform.position;
            while (currentIndex < trajectory.Count - 1 && trajectory[currentIndex + 1].time < animationStartTime)
            {
                currentIndex++;
            }
            initialDataPosition = GetInterpolatedPositionAtTime(animationStartTime);
            playbackStartTime = Time.time;
            isInitialized = true;
            Debug.Log($"Trayectoria cargada. Iniciando en el segundo {animationStartTime:F2} que corresponde al índice {currentIndex}.");
        }
        else
        {
            Debug.LogWarning("El archivo de trayectoria está vacío o no se pudo leer correctamente.");
        }
    }

    // Update se usa solo para calcular la posición objetivo en cada frame.
    void Update()
    {
        if (!isInitialized || trajectory.Count < 2) return;

        // Calculamos el tiempo actual de la animación
        float elapsedTime = animationStartTime + (Time.time - playbackStartTime) * playbackSpeed;

        // Buscamos el índice correcto en los datos
        while (currentIndex < trajectory.Count - 2 && trajectory[currentIndex + 1].time <= elapsedTime)
        {
            currentIndex++;
        }

        // Calculamos la posición interpolada a partir de los datos
        Vector3 interpolatedDataPosition = GetInterpolatedPositionAtTime(elapsedTime);
        Vector3 displacement = interpolatedDataPosition - initialDataPosition;

        // En lugar de moverlo directamente, guardamos la posición a la que queremos llegar.
        targetPosition = initialUnityPosition + displacement;
    }

    // FixedUpdate se usa para aplicar el movimiento y la rotación al Rigidbody.
    void FixedUpdate()
    {
        if (!isInitialized) return;

        // 1. Mover el Rigidbody
        // Usamos rb.MovePosition para un movimiento suave que respeta la física.
        rb.MovePosition(targetPosition);

        // 2. Calcular y aplicar la rotación
        // Calculamos el movimiento real desde el último frame de física
        Vector3 currentMovement = rb.position - lastPosition;

        // Suavizamos el vector de movimiento usando Lerp para evitar cambios bruscos
        smoothedMovement = Vector3.Lerp(smoothedMovement, currentMovement, Time.fixedDeltaTime * rotationSmoothing);

        // Solo rotamos si el movimiento suavizado supera nuestro umbral
        if (smoothedMovement.magnitude > minMovementThreshold)
        {
            // La lógica de rotación es la misma, pero ahora usa el movimiento suavizado
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, smoothedMovement.normalized);
            float rotationAngle = (smoothedMovement.magnitude / (2 * Mathf.PI * ballRadius)) * 360f;

            // Creamos un cuaternión de rotación y lo aplicamos a la rotación actual
            Quaternion rotationDelta = Quaternion.AngleAxis(rotationAngle, rotationAxis);
            rb.MoveRotation(rb.rotation * rotationDelta);
        }

        // Actualizamos la última posición para el siguiente ciclo de física
        lastPosition = rb.position;
    }

    // (El resto de métodos GetInterpolatedPositionAtTime y GetPositionFromData son idénticos)
    private Vector3 GetInterpolatedPositionAtTime(float time)
    {
        if (trajectory.Count == 0) return Vector3.zero;
        if (time <= trajectory[0].time) return GetPositionFromData(trajectory[0]);
        if (time >= trajectory[trajectory.Count - 1].time) return GetPositionFromData(trajectory[trajectory.Count - 1]);

        // Una búsqueda binaria podría ser más eficiente aquí para archivos muy grandes, pero la búsqueda lineal está bien.
        int searchIndex = 0;
        while (searchIndex < trajectory.Count - 2 && trajectory[searchIndex + 1].time < time)
        {
            searchIndex++;
        }

        PositionData startPoint = trajectory[searchIndex];
        PositionData endPoint = trajectory[searchIndex + 1];

        float segmentDuration = endPoint.time - startPoint.time;
        float timeIntoSegment = time - startPoint.time;
        float interpolationFactor = (segmentDuration > 0) ? (timeIntoSegment / segmentDuration) : 0;

        return Vector3.Lerp(GetPositionFromData(startPoint), GetPositionFromData(endPoint), interpolationFactor);
    }

    private Vector3 GetPositionFromData(PositionData data)
    {
        // Mapeo de coordenadas original
        // OpenCV (x,y,z) -> Unity (x,y,z)
        // x -> -z
        // y -> y (altura, no usada)
        // z -> -x
        return new Vector3(-data.z, 0, -data.x); // Tuve que invertir X y Z para que coincida con tu mapeo
    }
}