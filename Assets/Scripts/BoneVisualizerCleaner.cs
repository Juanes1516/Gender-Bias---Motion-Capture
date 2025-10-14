using UnityEngine;
using System.Collections.Generic;

public class BoneVisualizerCleaner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Arrastra aquí la raíz del esqueleto (ej: mixamorig:Hips). Si está vacío se usará este objeto.")]
    public Transform skeletonRoot;

    [Tooltip("Prefab de la esfera/cubo que se instanciará por cada hueso.")]
    public GameObject spherePrefab;

    [Header("Options")]
    [Tooltip("Prefijo que tendrán los objetos generados (para poder identificarlos y borrarlos).")]
    public string createdPrefix = "BV_Sphere_";

    [Tooltip("Tamaño local final de cada esfera.")]
    public float sphereScale = 0.08f;

    [Tooltip("Si true, borra previamente cualquier objeto con el prefijo antes de crear nuevos.")]
    public bool cleanupBeforeCreate = true;

    [Tooltip("Si no está vacío, sólo crea esferas en huesos cuyo nombre contenga este texto (ej: mixamorig).")]
    public string onlyIfNameContains = "mixamorig";

    void Start()
    {
        if (spherePrefab == null)
        {
            Debug.LogError("[BoneVisualizerCleaner] Asigna un Sphere Prefab en el inspector.");
            return;
        }

        if (skeletonRoot == null) skeletonRoot = transform;

        if (cleanupBeforeCreate) CleanupExisting();

        CreateSpheres();
    }

    void CleanupExisting()
    {
        // Recolectamos los objetos previos con el prefijo y los destruimos
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform t in skeletonRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith(createdPrefix))
                toDestroy.Add(t.gameObject);
        }

        foreach (var go in toDestroy)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    void CreateSpheres()
    {
        Transform[] bones = skeletonRoot.GetComponentsInChildren<Transform>(true);
        foreach (var bone in bones)
        {
            if (bone == skeletonRoot) continue;

            if (!string.IsNullOrEmpty(onlyIfNameContains) && !bone.name.Contains(onlyIfNameContains)) continue;

            // Evitar duplicar si ya existe una esfera con ese nombre como hijo
            if (bone.Find(createdPrefix + bone.name) != null) continue;

            GameObject s = Instantiate(spherePrefab, bone);
            s.name = createdPrefix + bone.name;
            s.transform.localPosition = Vector3.zero;
            s.transform.localRotation = Quaternion.identity;
            s.transform.localScale = Vector3.one * sphereScale;

            // Opcional: quitar cualquier componente que haga que el prefab se desparentice
            // (por ejemplo si el prefab tiene scripts que lo mueven en Awake)
            // foreach (var c in s.GetComponents<MonoBehaviour>()) Destroy(c);
        }
    }
}
