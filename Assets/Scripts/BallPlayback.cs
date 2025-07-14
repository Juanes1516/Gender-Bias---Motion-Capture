using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class BallPlayback : MonoBehaviour
{
    [Header("CSV Settings")]
    public string csvFileName = "ball_positions.csv";
    public float scaleFactor = 0.005f;   // px → unidades Unity
    public float fps = 30f;      // fps del video

    [Header("Outlier Filter")]
    public float maxPixelX = 1920f;    // ancho original del video
    public float maxPixelY = 1080f;    // alto original del video
    public float maxJumpPixels = 200f;     // salto máximo permitido (px)

    private Vector3 startPosition;
    private float fixedHeight;
    private List<Vector2> pixels = new List<Vector2>();
    private float startTime;
    private Vector2 lastValid;          // última posición valida

    void Start()
    {
        // Guardamos posición inicial y altura
        startPosition = transform.position;
        fixedHeight = startPosition.y;

        // Cargamos datos antes de iniciar Update()
        LoadPositions();
        startTime = Time.time;
    }

    void Update()
    {
        float elapsed = Time.time - startTime;
        float exact = elapsed * fps;
        int i0 = Mathf.FloorToInt(exact);
        int i1 = i0 + 1;
        float t = exact - i0;

        if (i0 < 0 || i0 >= pixels.Count)
            return;

        Vector2 p0 = Filter(pixels[i0]);
        Vector2 p1 = (i1 < pixels.Count)
                     ? Filter(pixels[i1])
                     : p0;

        // Convertimos px→metros (u) en XZ
        Vector3 from = new Vector3(
            startPosition.x + p0.x * scaleFactor,
            fixedHeight,
            startPosition.z + p0.y * scaleFactor
        );
        Vector3 to = new Vector3(
            startPosition.x + p1.x * scaleFactor,
            fixedHeight,
            startPosition.z + p1.y * scaleFactor
        );

        transform.position = Vector3.Lerp(from, to, t);
    }

    Vector2 Filter(Vector2 candidate)
    {
        bool outOfRange = candidate.x < 0 || candidate.x > maxPixelX
                       || candidate.y < 0 || candidate.y > maxPixelY;
        bool tooBigJump = Vector2.Distance(candidate, lastValid) > maxJumpPixels;

        if (outOfRange || tooBigJump)
        {
            // descartamos y devolvemos la última válida
            return lastValid;
        }
        else
        {
            // aceptamos y actualizamos
            lastValid = candidate;
            return candidate;
        }
    }

    void LoadPositions()
    {
        pixels.Clear();
        lastValid = Vector2.zero;

        string path = Path.Combine(Application.streamingAssetsPath, csvFileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"No encontré CSV en {path}");
            return;
        }

        string[] lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');
            if (cols.Length >= 4
                && float.TryParse(cols[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(cols[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                Vector2 p = new Vector2(x, y);
                pixels.Add(p);
                lastValid = p;             // actualizamos aquí
            }
            else
            {
                pixels.Add(lastValid);     // repetimos la última buena
            }
        }

        Debug.Log($"Cargadas {pixels.Count} posiciones desde '{csvFileName}'");
    }
}
