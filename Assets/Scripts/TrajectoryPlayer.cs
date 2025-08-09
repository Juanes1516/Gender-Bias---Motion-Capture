using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class TrajectoryPlayer : MonoBehaviour
{
    public TextAsset trajectoryFile;
    [Tooltip("Controla la velocidad de la reproducción. 1 = velocidad normal.")]
    public float playbackSpeed = 1.0f;

    [System.Serializable]
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

    // MEJORA: Variables para guardar el estado inicial
    private Vector3 initialUnityPosition;
    private Vector3 initialDataPosition;
    private bool isInitialized = false;

    void Start()
    {
        LoadAndInitializeTrajectory();
    }

    void LoadAndInitializeTrajectory()
    {
        if (trajectoryFile == null)
        {
            Debug.LogError("No se ha asignado un archivo de trayectoria (trajectoryFile).");
            return;
        }

        string[] lines = trajectoryFile.text.Split('\n');
        foreach (string line in lines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                trajectory.Add(JsonUtility.FromJson<PositionData>(line));
            }
        }

        if (trajectory.Count > 0)
        {
            // MEJORA: Guardamos la posición inicial del objeto en la escena de Unity.
            initialUnityPosition = transform.position;

            // MEJORA: Guardamos el primer punto de la grabación como nuestro "origen" de datos.
            initialDataPosition = GetPositionFromData(trajectory[0]);

            playbackStartTime = Time.time;
            isInitialized = true;
            Debug.Log($"Trayectoria cargada con {trajectory.Count} puntos. Animación relativa a {initialUnityPosition}.");
        }
        else
        {
            Debug.LogWarning("El archivo de trayectoria está vacío o no se pudo leer.");
        }
    }

    void Update()
    {
        if (!isInitialized || trajectory.Count < 2) return;

        float elapsedTime = (Time.time - playbackStartTime) * playbackSpeed;

        while (currentIndex < trajectory.Count - 1 && trajectory[currentIndex + 1].time <= elapsedTime)
        {
            currentIndex++;
        }

        if (currentIndex >= trajectory.Count - 1)
        {
            // Al finalizar, aplicamos el último desplazamiento
            Vector3 lastDisplacement = GetPositionFromData(trajectory[trajectory.Count - 1]) - initialDataPosition;
            transform.position = initialUnityPosition + lastDisplacement;
            return;
        }

        // Interpolamos para obtener la posición actual según los datos
        PositionData startPoint = trajectory[currentIndex];
        PositionData endPoint = trajectory[currentIndex + 1];
        float segmentDuration = endPoint.time - startPoint.time;
        float timeIntoSegment = elapsedTime - startPoint.time;
        float interpolationFactor = (segmentDuration > 0) ? (timeIntoSegment / segmentDuration) : 0;

        Vector3 interpolatedDataPosition = Vector3.Lerp(GetPositionFromData(startPoint), GetPositionFromData(endPoint), interpolationFactor);

        // MEJORA: Calculamos el desplazamiento desde el origen de los datos
        Vector3 displacement = interpolatedDataPosition - initialDataPosition;

        // MEJORA: Aplicamos ese desplazamiento a la posición inicial del objeto en Unity
        transform.position = initialUnityPosition + displacement;
    }

    private Vector3 GetPositionFromData(PositionData data)
    {
        // Recordar invertir el eje Y para que coincida con el sistema de coordenadas de Unity
        return new Vector3(-data.x, -data.y, -data.z);
    }
}