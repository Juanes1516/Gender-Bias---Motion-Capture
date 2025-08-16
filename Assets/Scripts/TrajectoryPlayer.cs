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

    // --- NUEVO: Radio del balón para una rotación realista ---
    [Tooltip("El radio de la esfera en unidades de Unity. Ayuda a calcular la rotación correctamente.")]
    public float ballRadius = 0.5f;

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

    // --- NUEVO: Variable para guardar la posición del fotograma anterior ---
    private Vector3 lastPosition;

    void Start()
    {
        LoadAndInitializeTrajectory();
        // Inicializamos lastPosition con la posición de inicio del objeto.
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
        transform.position = initialUnityPosition + displacement;

        // --- NUEVO: Lógica para calcular y aplicar la rotación ---
        // 1. Calculamos el vector de movimiento de este fotograma.
        Vector3 frameMovement = transform.position - lastPosition;

        // 2. Si el balón se movió, calculamos la rotación.
        if (frameMovement.magnitude > 0.001f)
        {
            // 3. El eje de rotación es perpendicular al movimiento y al eje vertical (Vector3.up).
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, frameMovement.normalized);

            // 4. La cantidad de rotación depende de la distancia recorrida y del radio del balón.
            float rotationAngle = (frameMovement.magnitude / (2 * Mathf.PI * ballRadius)) * 360f;

            // 5. Aplicamos la rotación. Usamos Space.World para que gire correctamente sin importar su orientación actual.
            transform.Rotate(rotationAxis, rotationAngle, Space.World);
        }

        // 6. Actualizamos la última posición para el siguiente fotograma.
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
        return new Vector3(-data.z, 0, -data.x);
    }
}