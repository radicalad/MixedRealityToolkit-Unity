// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AlignAndDistributeWindow : EditorWindow
{
    [SerializeField]
    private class ConfigurationSettings
    {
        public enum CalculationMethodType
        {
            Origin,
            Collider,
            Renderer
        }

        public CalculationMethodType CalculationMethod = CalculationMethodType.Origin;
        public Dictionary<GameObject, Bounds> CachedBounds
        {
            get;
            private set;
        }
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

        public void GenerateCachedBounds(GameObject[] gameobjects)
        {
            CachedBounds = new Dictionary<GameObject, Bounds>();

            foreach (GameObject gameObject in gameobjects)
            {
                Bounds bounds = new Bounds();

                if (CalculationMethod == CalculationMethodType.Collider)
                {
                    List<Vector3> boundsPoints = new List<Vector3>();
                    BoundsExtensions.GetColliderBoundsPoints(gameObject, boundsPoints, 0);

                    bounds.center = boundsPoints[0];
                    foreach (Vector3 point in boundsPoints)
                    {
                        bounds.Encapsulate(point);
                    }
                }
                else if (CalculationMethod == CalculationMethodType.Renderer)
                {
                    List<Vector3> boundsPoints = new List<Vector3>();
                    BoundsExtensions.GetRenderBoundsPoints(gameObject, boundsPoints, 0);

                    bounds.center = boundsPoints[0];
                    foreach (Vector3 point in boundsPoints)
                    {
                        bounds.Encapsulate(point);
                    }
                }
                
                CachedBounds.Add(gameObject, bounds);
            }
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
                AlignObjects(Selection.gameObjects, alignSettings);
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+X"))
            {
                alignSettings.Direction = Vector3.right;
                AlignObjects(Selection.gameObjects, alignSettings);
            }

            if (GUILayout.Button("-X"))
            {
                alignSettings.Direction = Vector3.left;
                AlignObjects(Selection.gameObjects, alignSettings);
            }

            if (GUILayout.Button("+Y"))
            {
                alignSettings.Direction = Vector3.up;
                AlignObjects(Selection.gameObjects, alignSettings);
            }

            if (GUILayout.Button("-Y"))
            {
                alignSettings.Direction = Vector3.down;
                AlignObjects(Selection.gameObjects, alignSettings);
            }

            if (GUILayout.Button("+Z"))
            {
                alignSettings.Direction = Vector3.forward;
                AlignObjects(Selection.gameObjects, alignSettings);
            }

            if (GUILayout.Button("-Z"))
            {
                alignSettings.Direction = Vector3.back;
                AlignObjects(Selection.gameObjects, alignSettings);
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
                    DistributeObjects(Selection.gameObjects, distributeSettings);
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
                    DistributeObjects(Selection.gameObjects, distributeSettings);
                }

                if (GUILayout.Button("Y"))
                {
                    distributeSettings.Direction = Vector3.up;
                    DistributeObjects(Selection.gameObjects, distributeSettings);
                }

                if (GUILayout.Button("Z"))
                {
                    distributeSettings.Direction = Vector3.forward;
                    DistributeObjects(Selection.gameObjects, distributeSettings);
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
                    IncrementObjects(Selection.gameObjects, incrementSettings);
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
                    IncrementObjects(Selection.gameObjects, incrementSettings);
                }

                if (GUILayout.Button("Along Y"))
                {
                    incrementSettings.Direction = Vector3.up;
                    IncrementObjects(Selection.gameObjects, incrementSettings);
                }

                if (GUILayout.Button("Along Z"))
                {
                    incrementSettings.Direction = Vector3.forward;
                    IncrementObjects(Selection.gameObjects, incrementSettings);
                }
            }
        }
    }

    private void AlignObjects(GameObject[] gameObjects, ConfigurationSettings settings)
    {
        if (gameObjects.Length <= 0) { return; }

        //Grab all the min max values per axis.
        Tuple<Vector3, Vector3> MinMax = FindMinMax(gameObjects, settings.CalculationMethod);

        //Check the direction - it will determine min or max
        Vector3 ActiveMinMax;// = Vector3.zero;// = (Vector3.Dot(settings.Direction, Vector3.one) > 0) ? MinMax.Item2 : MinMax.Item1;

        if (Vector3.Dot(settings.Direction, Vector3.one) > 0)
        {
            Debug.Log("we're out maxin");
            ActiveMinMax = MinMax.Item2;
        }
        else
        {
            ActiveMinMax = MinMax.Item1;
            Debug.Log("MIN");
        }
        Debug.Log("active plane origin: " + Vector3.Scale(ActiveMinMax, VectAbs(settings.Direction)).ToString("F5"));

        //Generate the plane to project against
        Vector3 PlaneOrigin = Vector3.Scale(ActiveMinMax, VectAbs(settings.Direction));
        Plane AlignmentPlane = new Plane(VectAbs(settings.Direction), PlaneOrigin);

        foreach (var gameObj in gameObjects)
        {
            //use AABB if Collider or Renderer
            Bounds bounds = new Bounds();
            Vector3 additionalOffset = Vector3.zero;

            if (settings.CalculationMethod == ConfigurationSettings.CalculationMethodType.Collider)
            {
                bounds = gameObj.GetComponent<Collider>().bounds;
            }
            else if (settings.CalculationMethod == ConfigurationSettings.CalculationMethodType.Renderer)
            {
                bounds = gameObj.GetComponent<Renderer>().bounds;
            }

            if (bounds.size != Vector3.zero)
            {
                //get the offset from the origin and AABB
                Vector3 offset = Vector3.Scale(bounds.center - gameObj.transform.position, VectAbs(settings.Direction));
                additionalOffset = offset - Vector3.Scale(bounds.extents, -settings.Direction);
            }

            //project the position on the plane and apply any additional offset
            gameObj.transform.position = AlignmentPlane.ClosestPointOnPlane(gameObj.transform.position) - additionalOffset;
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

        //we need to initialize vectors, but don't compare against zero
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
                    List<Vector3> boundsPoints = new List<Vector3>();
                    BoundsExtensions.GetColliderBoundsPoints(gameObj, boundsPoints, 0);
                    
                    bounds.center = boundsPoints[0];
                    foreach (Vector3 point in boundsPoints)
                    {
                        bounds.Encapsulate(point);
                    }

                }
                else if (calculationMethod == ConfigurationSettings.CalculationMethodType.Renderer)
                {
                    List<Vector3> boundsPoints = new List<Vector3>();
                    BoundsExtensions.GetRenderBoundsPoints(gameObj, boundsPoints, 0);

                    bounds.center = boundsPoints[0];
                    foreach (Vector3 point in boundsPoints)
                    {
                        bounds.Encapsulate(point);
                    }
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

                Debug.Log("------");
                Debug.Log("Name: " + gameObj.name);
                Debug.Log("Object center: " + bounds.center);
                Debug.Log("Object min: " + bounds.min);
                Debug.Log("Object max: " + bounds.max);

                for (int i = 0; i < 3; i++)
                {
                    //find the min-most or max-most value
                    min[i] = (bounds.min[i] < min[i]) ? bounds.min[i] : min[i];
                    max[i] = (bounds.max[i] > max[i]) ? bounds.max[i] : max[i];
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
                for (int i = 0; i < 3; i++)
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
}