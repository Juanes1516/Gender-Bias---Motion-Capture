using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class TrajectoryPlayer : MonoBehaviour
{
    [Header("Configuraci�n de Archivo")]
    [Tooltip("Arrastra aqu� el archivo .txt generado por el script de Python")]
    public TextAsset trajectoryFile;

    [Header("Configuraci�n de Animaci�n")]
    [Tooltip("El segundo del archivo desde el cual comenzar� la animaci�n.")]
    public float animationStartTime = 0.0f;
    [Tooltip("Controla la velocidad de la reproducci�n. 1 = velocidad normal.")]
    public float playbackSpeed = 1.0f;

    [Header("Configuraci�n de F�sica y Rotaci�n")]
    [Tooltip("El radio de la esfera en unidades de Unity. Ayuda a calcular la rotaci�n correctamente.")]
    public float ballRadius = 0.5f;
    [Tooltip("La magnitud de movimiento m�nima para que el bal�n rote. Aumenta este valor si el bal�n rota estando quieto.")]
    public float minMovementThreshold = 0.01f;
    [Tooltip("Controla qu� tan suavemente reacciona la rotaci�n a los cambios de movimiento. Valores m�s altos = reacci�n m�s r�pida.")]
    public float rotationSmoothing = 5.0f;

    // Componente Rigidbody para el movimiento f�sico
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

    // Variables para el movimiento y rotaci�n suavizados
    private Vector3 initialUnityPosition;
    private Vector3 initialDataPosition;
    private Vector3 targetPosition; // La posici�n a la que queremos mover el bal�n
    private Vector3 lastPosition;   // La posici�n en el frame de f�sica anterior
    private Vector3 smoothedMovement; // El vector de movimiento suavizado para la rotaci�n

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("�Este objeto necesita un componente Rigidbody para funcionar correctamente!");
            enabled = false; // Desactiva el script si no hay Rigidbody
            return;
        }

        // Importante: Hacemos el Rigidbody cinem�tico para controlarlo por script sin que le afecten fuerzas externas inesperadas.
        rb.isKinematic = true;

        LoadAndInitializeTrajectory();

        if (isInitialized)
        {
            // Inicializamos las posiciones para el c�lculo de movimiento
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

        // (El resto de este m�todo es id�ntico al tuyo, funciona bien)
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
                    Debug.LogWarning($"No se pudo parsear la l�nea {i}: '{line}'. Error: {e.Message}");
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
            Debug.Log($"Trayectoria cargada. Iniciando en el segundo {animationStartTime:F2} que corresponde al �ndice {currentIndex}.");
        }
        else
        {
            Debug.LogWarning("El archivo de trayectoria est� vac�o o no se pudo leer correctamente.");
        }
    }

    // Update se usa solo para calcular la posici�n objetivo en cada frame.
    void Update()
    {
        if (!isInitialized || trajectory.Count < 2) return;

        // Calculamos el tiempo actual de la animaci�n
        float elapsedTime = animationStartTime + (Time.time - playbackStartTime) * playbackSpeed;

        // Buscamos el �ndice correcto en los datos
        while (currentIndex < trajectory.Count - 2 && trajectory[currentIndex + 1].time <= elapsedTime)
        {
            currentIndex++;
        }

        // Calculamos la posici�n interpolada a partir de los datos
        Vector3 interpolatedDataPosition = GetInterpolatedPositionAtTime(elapsedTime);
        Vector3 displacement = interpolatedDataPosition - initialDataPosition;

        // En lugar de moverlo directamente, guardamos la posici�n a la que queremos llegar.
        targetPosition = initialUnityPosition + displacement;
    }

    // FixedUpdate se usa para aplicar el movimiento y la rotaci�n al Rigidbody.
    void FixedUpdate()
    {
        if (!isInitialized) return;

        // 1. Mover el Rigidbody
        // Usamos rb.MovePosition para un movimiento suave que respeta la f�sica.
        rb.MovePosition(targetPosition);

        // 2. Calcular y aplicar la rotaci�n
        // Calculamos el movimiento real desde el �ltimo frame de f�sica
        Vector3 currentMovement = rb.position - lastPosition;

        // Suavizamos el vector de movimiento usando Lerp para evitar cambios bruscos
        smoothedMovement = Vector3.Lerp(smoothedMovement, currentMovement, Time.fixedDeltaTime * rotationSmoothing);

        // Solo rotamos si el movimiento suavizado supera nuestro umbral
        if (smoothedMovement.magnitude > minMovementThreshold)
        {
            // La l�gica de rotaci�n es la misma, pero ahora usa el movimiento suavizado
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, smoothedMovement.normalized);
            float rotationAngle = (smoothedMovement.magnitude / (2 * Mathf.PI * ballRadius)) * 360f;

            // Creamos un cuaterni�n de rotaci�n y lo aplicamos a la rotaci�n actual
            Quaternion rotationDelta = Quaternion.AngleAxis(rotationAngle, rotationAxis);
            rb.MoveRotation(rb.rotation * rotationDelta);
        }

        // Actualizamos la �ltima posici�n para el siguiente ciclo de f�sica
        lastPosition = rb.position;
    }

    // (El resto de m�todos GetInterpolatedPositionAtTime y GetPositionFromData son id�nticos)
    private Vector3 GetInterpolatedPositionAtTime(float time)
    {
        if (trajectory.Count == 0) return Vector3.zero;
        if (time <= trajectory[0].time) return GetPositionFromData(trajectory[0]);
        if (time >= trajectory[trajectory.Count - 1].time) return GetPositionFromData(trajectory[trajectory.Count - 1]);

        // Una b�squeda binaria podr�a ser m�s eficiente aqu� para archivos muy grandes, pero la b�squeda lineal est� bien.
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