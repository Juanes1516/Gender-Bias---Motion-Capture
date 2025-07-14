using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un despachador para ejecutar código en el hilo principal de Unity.
/// Esencial para interactuar con la API de Unity desde otros hilos (como los de red/OSC).
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            throw new Exception("No se ha encontrado una instancia de UnityMainThreadDispatcher. Por favor, asegúrese de que haya uno en la escena.");
        }
        return _instance;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void OnDestroy()
    {
        _instance = null;
    }

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// Pone una acción en la cola para ser ejecutada en el hilo principal.
    /// </summary>
    /// <param name="action">La acción a ejecutar.</param>
    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}