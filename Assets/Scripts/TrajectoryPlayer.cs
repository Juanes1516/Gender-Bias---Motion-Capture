using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class TrajectoryPlayer : MonoBehaviour
{
    [Tooltip("Arrastra aquí el archivo .txt generado por el script de Python")]
    public TextAsset trajectoryFile;

    [Tooltip("El segundo del archivo desde el cual comenzará la animación.")]
    public float animationStartTime = 0.0f;

    [Tooltip("Controla la velocidad de la reproducción. 1 = velocidad normal.")]
    public float playbackSpeed = 1.0f;

    [Tooltip("El radio de la esfera en unidades de Unity. Ayuda a calcular la rotación correctamente.")]
    public float ballRadius = 0.5f;

    // --- NUEVO: Variable para el Rigidbody ---
    private Rigidbody rb;

    private class PositionData
    {
        public float time;
        public float x;
        public float y;
        public float z;
    }

    private List<PositionData> trajectory = new List<PositionData>();
    private int currentIndex = 0;
    private float playbackStartTime;

    private Vector3 initialUnityPosition;
    private Vector3 initialDataPosition;
    private bool isInitialized = false;
    private Vector3 lastPosition;

    void Start()
    {
        // --- NUEVO: Obtenemos el componente Rigidbody ---
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("¡Este objeto necesita un componente Rigidbody para funcionar correctamente!");
            return;
        }

        LoadAndInitializeTrajectory();

        if (isInitialized)
        {
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

    void Update()
    {
        if (!isInitialized || trajectory.Count < 2) return;

        float elapsedTime = animationStartTime + (Time.time - playbackStartTime) * playbackSpeed;

        while (currentIndex < trajectory.Count - 2 && trajectory[currentIndex + 1].time <= elapsedTime)
        {
            currentIndex++;
        }

        Vector3 interpolatedDataPosition = GetInterpolatedPositionAtTime(elapsedTime);
        Vector3 displacement = interpolatedDataPosition - initialDataPosition;

        // --- MODIFICADO: Usamos rb.MovePosition en lugar de transform.position ---
        // Esto mueve el objeto respetando las colisiones.
        rb.MovePosition(initialUnityPosition + displacement);

        // La lógica para la rotación puede seguir usando transform.position porque
        // MovePosition actualizará la posición antes de que este código se ejecute.
        Vector3 frameMovement = transform.position - lastPosition;

        if (frameMovement.magnitude > 0.001f)
        {
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, frameMovement.normalized);
            float rotationAngle = (frameMovement.magnitude / (2 * Mathf.PI * ballRadius)) * 360f;
            transform.Rotate(rotationAxis, rotationAngle, Space.World);
        }

        lastPosition = transform.position;
    }

    private Vector3 GetInterpolatedPositionAtTime(float time)
    {
        if (time <= trajectory[0].time)
        {
            return GetPositionFromData(trajectory[0]);
        }

        if (time >= trajectory[trajectory.Count - 1].time)
        {
            return GetPositionFromData(trajectory[trajectory.Count - 1]);
        }

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
        // Se mantiene tu mapeo de coordenadas original
        return new Vector3(-data.z, 0, -data.x);
    }
}