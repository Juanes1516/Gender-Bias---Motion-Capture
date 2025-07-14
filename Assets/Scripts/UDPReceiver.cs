using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UDPReceiver : MonoBehaviour
{
    [Tooltip("Puerto UDP donde escucha Unity (coincide con el puerto de Python)")]
    public int listenPort = 5065;

    [Tooltip("Número de muestras para la media móvil")]
    public int windowSize = 5;

    private UdpClient client;
    private bool isRunning = false;

    // Estructura para el filtro de media móvil
    private Queue<Vector3> window = new Queue<Vector3>();
    private Vector3 sum = Vector3.zero;

    void Start()
    {
        // Iniciar socket UDP
        client = new UdpClient(listenPort);
        isRunning = true;
        client.BeginReceive(OnReceived, null);
        Debug.Log($"[UDPReceiver] Escuchando en puerto {listenPort}");
    }

    private void OnReceived(IAsyncResult ar)
    {
        if (!isRunning) return;

        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);
        byte[] bytes;

        try
        {
            bytes = client.EndReceive(ar, ref remoteEP);
        }
        catch (ObjectDisposedException)
        {
            // El socket ya fue cerrado
            return;
        }

        // Decodificar JSON recibido
        string json = Encoding.UTF8.GetString(bytes);
        try
        {
            Position data = JsonUtility.FromJson<Position>(json);

            // Convertir a Vector3 (e invertir eje Y si hiciste esa corrección)
            Vector3 rawPos = new Vector3(
                (float)data.x,
                -(float)data.y,
                (float)data.z
            );

            // Encolar al hilo principal el filtrado y la asignación de posición
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                ApplyMovingAverage(rawPos);
            });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UDPReceiver] Error parseando JSON: {e.Message}");
        }
        finally
        {
            // Volver a escuchar
            if (isRunning)
                client.BeginReceive(OnReceived, null);
        }
    }

    /// <summary>
    /// Aplica un filtro de media móvil sobre rawPos y actualiza transform.position.
    /// </summary>
    private void ApplyMovingAverage(Vector3 rawPos)
    {
        // 1) Añadir nueva muestra
        window.Enqueue(rawPos);
        sum += rawPos;

        // 2) Si excede windowSize, retirar la más antigua
        if (window.Count > windowSize)
        {
            sum -= window.Dequeue();
        }

        // 3) Calcular promedio y asignar
        Vector3 avgPos = sum / window.Count;
        transform.position = avgPos;
    }

    void OnApplicationQuit()
    {
        // Detener el ciclo de recepción y cerrar socket
        isRunning = false;
        if (client != null)
        {
            client.Close();
            client = null;
        }
    }

    [Serializable]
    private class Position
    {
        public double x;
        public double y;
        public double z;
    }
}
