using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AlignAndDistributeWindow : EditorWindow
{

    private GameObject[] selectedObjects = new GameObject[0];

    [SerializeField]
    private class ConfigurationSettings
    {
        public enum CalculationMethodType
        {
            Origin,
            Collider,
            Renderer
        }

        public CalculationMethodType CalculationMethod;
        public bool MatchRotation;
        public Vector3 Direction;
        public float MovementAmount;

        public ConfigurationSettings(CalculationMethodType calculationMethod = CalculationMethodType.Origin, bool matDirectionRot = false, Vector3 customDir = default(Vector3), float movementAmount = 0.0f)
        {
            CalculationMethod = calculationMethod;
            MatchRotation = matDirectionRot;
            Direction = customDir;
            MovementAmount = movementAmount;
        }
    }

    [SerializeField]
    private ConfigurationSettings alignSettings = new ConfigurationSettings();

    [SerializeField]
    private ConfigurationSettings distributeSettings = new ConfigurationSettings();

    [SerializeField]
    private ConfigurationSettings incrementSettings = new ConfigurationSettings();

    [SerializeField]
    private bool useCustomAlignDirection = false;

    [SerializeField]
    private bool useCustomDistributeDirection = false;

    [SerializeField]
    private bool useCustomIncrementDirection = false;




    [MenuItem("Mixed Reality Toolkit/Utilities/Aign and Distribute")]
    public static void ShowWindow()
    {
        GetWindow<AlignAndDistributeWindow>(false, "Aign and Distribute", true);
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Align", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        alignSettings.CalculationMethod = (ConfigurationSettings.CalculationMethodType)EditorGUILayout.EnumPopup("Calculation method: ", alignSettings.CalculationMethod);
        EditorGUILayout.Space();

        useCustomAlignDirection = GUILayout.Toggle(useCustomAlignDirection, "Use Custom Direction");
        EditorGUILayout.Space();

        if (useCustomAlignDirection)
        {
            alignSettings.Direction = EditorGUILayout.Vector3Field("CustomDirection", alignSettings.Direction);
            EditorGUILayout.Space();
            if (GUILayout.Button("Align objects"))
            {
                AlignObjects(selectedObjects, alignSettings);
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+X"))
            {
                alignSettings.Direction = Vector3.right;
                AlignObjects(selectedObjects, alignSettings);
            }

            if (GUILayout.Button("-X"))
            {
                alignSettings.Direction = Vector3.left;
                AlignObjects(selectedObjects, alignSettings);
            }

            if (GUILayout.Button("+Y"))
            {
                alignSettings.Direction = Vector3.up;
                AlignObjects(selectedObjects, alignSettings);
            }

            if (GUILayout.Button("-Y"))
            {
                alignSettings.Direction = Vector3.down;
                AlignObjects(selectedObjects, alignSettings);
            }

            if (GUILayout.Button("+Z"))
            {
                alignSettings.Direction = Vector3.forward;
                AlignObjects(selectedObjects, alignSettings);
            }

            if (GUILayout.Button("-Z"))
            {
                alignSettings.Direction = Vector3.back;
                AlignObjects(selectedObjects, alignSettings);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        EditorGUILayout.LabelField("Distribute", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        distributeSettings.CalculationMethod = (ConfigurationSettings.CalculationMethodType)EditorGUILayout.EnumPopup("Calculation method: ", distributeSettings.CalculationMethod);
        EditorGUILayout.Space();

        useCustomDistributeDirection = GUILayout.Toggle(useCustomDistributeDirection, "Use Custom Direction");
        EditorGUILayout.Space();

        if (useCustomDistributeDirection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.Vector3Field("Custom Direction", distributeSettings.Direction);
                EditorGUILayout.Space();
                if (GUILayout.Button("Distribute objects"))
                {
                    DistributeObjects(selectedObjects, distributeSettings);
                }
            }
        }
        else
        {
            using (new EditorGUI.IndentLevelScope())
            {
                if (GUILayout.Button("X"))
                {
                    distributeSettings.Direction = Vector3.right;
                    DistributeObjects(selectedObjects, distributeSettings);
                }

                if (GUILayout.Button("Y"))
                {
                    distributeSettings.Direction = Vector3.up;
                    DistributeObjects(selectedObjects, distributeSettings);
                }

                if (GUILayout.Button("Z"))
                {
                    distributeSettings.Direction = Vector3.forward;
                    DistributeObjects(selectedObjects, distributeSettings);
                }
            }
        }

        EditorGUILayout.LabelField("Increment", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        useCustomIncrementDirection = GUILayout.Toggle(useCustomIncrementDirection, "Use Custom Direction");
        EditorGUILayout.Space();

        EditorGUILayout.FloatField("Increment Amount", incrementSettings.MovementAmount);
        EditorGUILayout.Space();

        if (useCustomIncrementDirection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.Vector3Field("Custom Direction", incrementSettings.Direction);
                EditorGUILayout.Space();
                if (GUILayout.Button("Increment objects"))
                {
                    IncrementObjects(selectedObjects, incrementSettings);
                }
            }
        }
        else
        {
            using (new EditorGUI.IndentLevelScope())
            {
                if (GUILayout.Button("Along X"))
                {
                    incrementSettings.Direction = Vector3.right;
                    IncrementObjects(selectedObjects, incrementSettings);
                }

                if (GUILayout.Button("Along Y"))
                {
                    incrementSettings.Direction = Vector3.up;
                    IncrementObjects(selectedObjects, incrementSettings);
                }

                if (GUILayout.Button("Along Z"))
                {
                    incrementSettings.Direction = Vector3.forward;
                    IncrementObjects(selectedObjects, incrementSettings);
                }
            }
        }
    }

    private void AlignObjects(GameObject[] gameObjects, ConfigurationSettings settings)
    {
        if (gameObjects.Length <= 0) { return; }


        Tuple<Vector3, Vector3> MinMax = FindMinMax(gameObjects, settings.CalculationMethod);
        Vector3 ActiveMinMax = (Vector3.Dot(settings.Direction, Vector3.one) > 0) ? MinMax.Item2 : MinMax.Item1;
        Vector3 PlaneOrigin = Vector3.Scale(ActiveMinMax, VectAbs(settings.Direction));
        Plane AlignmentPlane = new Plane(settings.Direction, PlaneOrigin);

        foreach (var gameObj in gameObjects)
        {
            switch (settings.CalculationMethod)
            {
                case ConfigurationSettings.CalculationMethodType.Origin:
                    gameObj.transform.position = AlignmentPlane.ClosestPointOnPlane(gameObj.transform.position);

                    break;
                case ConfigurationSettings.CalculationMethodType.Collider:


                    break;
                case ConfigurationSettings.CalculationMethodType.Renderer:
                    break;
                default:
                    break;
            }
        }

    }

    private void DistributeObjects(GameObject[] gameObjects, ConfigurationSettings settings)
    {
        if (gameObjects.Length <= 0) { return; }

        foreach (var gameObj in gameObjects)
        {
            Debug.Log(gameObj.name);
        }

    }

    private void IncrementObjects(GameObject[] gameObjects, ConfigurationSettings settings)
    {
        if (gameObjects.Length <= 0) { return; }

        foreach (var gameObj in gameObjects)
        {
            Debug.Log(gameObj.name);
        }

    }

    private Tuple<Vector3, Vector3> FindMinMax(GameObject[] gameObjects, ConfigurationSettings.CalculationMethodType calculationMethod)
    {
        if (gameObjects.Length <= 0) { return null; }

        //we need to initialize our vectors, don't compare against zero
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
        bool initialPositionValid = false;
        bool fallbackToOrigin = false;
        List<GameObject> fallbackObjects = new List<GameObject>();

        if (calculationMethod == ConfigurationSettings.CalculationMethodType.Collider || calculationMethod == ConfigurationSettings.CalculationMethodType.Renderer)
        {
            foreach (GameObject gameObj in gameObjects)
            {
                Bounds bounds = new Bounds();

                if (calculationMethod == ConfigurationSettings.CalculationMethodType.Collider)
                {
                    bounds = gameObj.GetComponent<Collider>().bounds;
                }
                else if (calculationMethod == ConfigurationSettings.CalculationMethodType.Renderer)
                {
                    bounds = gameObj.GetComponent<Renderer>().bounds;
                }

                if (bounds == null)
                {
                    //invalid comparison, add it to the list and compare the gameobject origin
                    fallbackToOrigin = true;
                    fallbackObjects.Add(gameObj);
                    continue;
                }

                if (initialPositionValid == false)
                {
                    //Since zero is a position, we need to set a default within the range of the object group
                    min = max = bounds.center;
                    initialPositionValid = true;
                }

                for (int i = 0; i < 2; i++)
                {
                    //find the min-most or max-most value
                    min[i] = (bounds.center[i] + bounds.min[i] < min[i]) ? bounds.center[i] + bounds.min[i] : min[i];
                    max[i] = (bounds.center[i] + bounds.max[i] > max[i]) ? bounds.center[i] + bounds.max[i] : max[i];
                }
            }
        }

        if (calculationMethod == ConfigurationSettings.CalculationMethodType.Origin || fallbackToOrigin == true)
        {
            if (initialPositionValid == false)
            {
                //Since zero is a position, we need to set a default within the range of the object group
                min = max = gameObjects[0].transform.position;
                initialPositionValid = true;
            }

            GameObject[] GameObjectQueue = (fallbackToOrigin == true) ? fallbackObjects.ToArray() : gameObjects;
            foreach (GameObject gameObj in GameObjectQueue)
            {
                for (int i = 0; i < 2; i++)
                {
                    //find the min-most or max-most value
                    min[i] = (gameObj.transform.position[i] < min[i]) ? gameObj.transform.position[i] : min[i];
                    max[i] = (gameObj.transform.position[i] > max[i]) ? gameObj.transform.position[i] : max[i];
                }
            }
        }

        return new Tuple<Vector3, Vector3>(min, max);
    }

    private Vector3 VectAbs(Vector3 vector3)
    {
        return new Vector3(Mathf.Abs(vector3.x), Mathf.Abs(vector3.y), Mathf.Abs(vector3.z));
    }

    private void OnSelectionChange()
    {
        selectedObjects = Selection.gameObjects;
        Debug.Log(Selection.gameObjects.Length);
    }

}