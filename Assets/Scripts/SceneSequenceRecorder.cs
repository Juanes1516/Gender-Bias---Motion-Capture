using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder.Encoder;

public class SceneSequenceRecorder : MonoBehaviour
{
    [Tooltip("La lista de nombres de las escenas que quieres grabar, en orden.")]
    public List<string> scenesToRecord;

    [Tooltip("Cu�ntos segundos grabar de cada escena.")]
    public float recordDurationPerScene = 10f;

    [Tooltip("Tiempo de espera adicional despu�s de cargar cada escena.")]
    public float sceneLoadWaitTime = 2f;

    private RecorderController recorderController;
    private RecorderControllerSettings recorderSettings;

    void Start()
    {
        // Inicia la rutina que grabar� todas las escenas en secuencia
        StartCoroutine(StartRecordingSequence());
    }

    IEnumerator StartRecordingSequence()
    {
        Debug.Log("Iniciando secuencia de grabaci�n...");

        // Recorremos cada escena en la lista que definimos en el Inspector
        foreach (string sceneName in scenesToRecord)
        {
            // Cargar la escena
            Debug.Log($"Cargando escena: {sceneName}");

            // Usar LoadSceneAsync para mejor control
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // Esperar hasta que la escena est� completamente cargada
            yield return asyncLoad;

            // Esperar tiempo adicional para que todos los objetos se inicialicen
            yield return new WaitForSeconds(sceneLoadWaitTime);

            // Iniciar la grabaci�n de la escena actual
            StartRecording(sceneName);

            // Esperar el tiempo de grabaci�n definido
            Debug.Log($"Grabando por {recordDurationPerScene} segundos...");
            yield return new WaitForSeconds(recordDurationPerScene);

            // Detener la grabaci�n
            StopRecording();

            // Esperar un momento antes de la siguiente escena
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("�Secuencia de grabaci�n finalizada!");
    }

    void StartRecording(string sceneName)
    {
        // 1. Configuraci�n del Recorder
        recorderSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderController = new RecorderController(recorderSettings);

        // 2. Configuraci�n del formato de video (MP4) - Usando la nueva API
        var videoRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        videoRecorderSettings.name = "My Video Recorder";
        videoRecorderSettings.Enabled = true;

        // Configurar el encoder usando la nueva API
        videoRecorderSettings.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };

        // Asignar de d�nde se grabar� (la vista del juego)
        videoRecorderSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };

        // 3. Definir la ruta de salida
        string outputFolder = Path.Combine(Application.dataPath, "..", "Recordings");
        Directory.CreateDirectory(outputFolder);
        videoRecorderSettings.OutputFile = Path.Combine(outputFolder,
            $"{sceneName}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}");

        // A�adir la configuraci�n al controlador
        recorderSettings.AddRecorderSettings(videoRecorderSettings);
        recorderSettings.SetRecordModeToManual();

        // Iniciar la grabaci�n
        Debug.Log($"Iniciando grabaci�n para la escena {sceneName}...");
        recorderController.PrepareRecording();
        recorderController.StartRecording();
    }

    void StopRecording()
    {
        if (recorderController != null && recorderController.IsRecording())
        {
            Debug.Log("Deteniendo grabaci�n...");
            recorderController.StopRecording();
            recorderController = null;
        }

        // Limpiar el settings tambi�n
        if (recorderSettings != null)
        {
            DestroyImmediate(recorderSettings);
            recorderSettings = null;
        }
    }

    void OnDestroy()
    {
        // Asegurarse de limpiar recursos al destruir el objeto
        StopRecording();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // Detener grabaci�n si la aplicaci�n se pausa
        if (pauseStatus && recorderController != null && recorderController.IsRecording())
        {
            StopRecording();
        }
    }
}