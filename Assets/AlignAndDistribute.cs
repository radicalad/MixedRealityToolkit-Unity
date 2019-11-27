// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
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
            set;
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

    [MenuItem("Mixed Reality Toolkit/Utilities/Align and Distribute")]
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

        //Grab the min or max value
        Vector3 ActiveValue = ActiveMinMaxValue(gameObjects, settings.CalculationMethod, settings.Direction);

        //Generate the plane to project against
        Vector3 PlaneOrigin = Vector3.Scale(ActiveValue, VectAbs(settings.Direction));
        Plane AlignmentPlane = new Plane(VectAbs(settings.Direction), PlaneOrigin);

        foreach (var gameObj in gameObjects)
        {
            //use AABB if Collider or Renderer
            Bounds bounds = GenerateBoundsFromPoints(gameObj, settings.CalculationMethod);
            Vector3 additionalOffset = Vector3.zero;

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

        GameObject[] sortedGameObjects = gameObjects.OrderBy((x) => Vector3.Scale(x.transform.position, VectAbs(settings.Direction)).sqrMagnitude).ToArray();

        //Grab the min or max value
        //Since positioning happens from Min -> Max, remove the last item size to make the math work
        Tuple<Vector3, Vector3> MinMax = FindAndMinMax(sortedGameObjects, settings.CalculationMethod);

        Vector3 TotalMargin = MinMax.Item2;

        for (int i = 0; i < sortedGameObjects.Length; i++)
        {
            TotalMargin -= Vector3.Scale(GenerateBoundsFromPoints(sortedGameObjects[i], settings.CalculationMethod).size, VectAbs(settings.Direction));
        }


        //Since positioning happens from Min -> Max, remove the last item size, otherwise the last item will begin at the edge of where it should end
        Vector3 MaxMinusOne = MinMax.Item2 - Vector3.Scale(GenerateBoundsFromPoints(sortedGameObjects[sortedGameObjects.Length - 1], settings.CalculationMethod).size, VectAbs(settings.Direction));
        Vector3 MinMinusOne = MinMax.Item1 + Vector3.Scale(GenerateBoundsFromPoints(sortedGameObjects[0], settings.CalculationMethod).size, VectAbs(settings.Direction));

        // ensure the distribution area is bigger than 0
        for (int i = 0; i < 3; i++)
        {
            if (VectAbs(settings.Direction)[i] != 0.0f)
            {
                //A valid direction to distribute against
                if(MaxMinusOne[i] - MinMinusOne[i] < 0.0f)
                {
                    return;
                }
            }
        }

        Bounds b = new Bounds();
        b.center = MinMax.Item1;
        b.Encapsulate(MinMax.Item2);
        DrawBox(b, Color.blue, 5f);

        Bounds margin = new Bounds();
        margin.center = MinMax.Item1;
        margin.Encapsulate(TotalMargin);
        DrawBox(margin, Color.red, 5f);

        Vector3 distStep = new Vector3(TotalMargin.x / (sortedGameObjects.Length - 1), TotalMargin.y / (sortedGameObjects.Length - 1), TotalMargin.z / (sortedGameObjects.Length - 1));
        Vector3 distStepOnAxis = Vector3.Scale(distStep, VectAbs(settings.Direction));

        for (int i = 0; i < sortedGameObjects.Length; i++)
        {
            Vector3 newPos = sortedGameObjects[i].transform.position;

            Vector3 additionalOffset = Vector3.zero;

            for (int j = 0; j < 3; j++)
            {
                newPos[j] = (distStepOnAxis[j] != 0.0f) ? distStepOnAxis[j] * i : newPos[j];
            }

            //use AABB if Collider or Renderer
            Bounds bounds = GenerateBoundsFromPoints(sortedGameObjects[i], settings.CalculationMethod);

            if (bounds.size != Vector3.zero)
            {


            }
            sortedGameObjects[i].transform.position = newPos + additionalOffset;
        }
            /*

            Vector3 distStep = new Vector3((MaxMinusOne - MinMinusOne).x / (sortedGameObjects.Length-1), (MaxMinusOne - MinMinusOne).y / (sortedGameObjects.Length-1), (MaxMinusOne - MinMinusOne).z / (sortedGameObjects.Length-1));

            for (int i = 0; i < sortedGameObjects.Length; i++)
            {
                Vector3 newPos = sortedGameObjects[i].transform.position;
                Vector3 additionalOffset = Vector3.zero;

                //use AABB if Collider or Renderer
                Bounds bounds = GenerateBoundsFromPoints(sortedGameObjects[i], settings.CalculationMethod);

                for (int j = 0; j < 3; j++)
                {
                    newPos[j] = (distStepOnAxis[j] != 0.0f) ? distStepOnAxis[j] * i : newPos[j];
                }

                if (bounds.size != Vector3.zero)
                {
                    Vector3 offset = newPos - Vector3.Scale(bounds.center, settings.Direction);
                    Debug.Log("newPos: " + newPos.ToString("F5") + ", " + bounds.center);
                    additionalOffset = offset;
                    //additionalOffset = Vector3.Scale(MinMinusOne, VectAbs(settings.Direction)) + Vector3.Scale(GenerateBoundsFromPoints(sortedGameObjects[0], settings.CalculationMethod).size, VectAbs(settings.Direction));
                    //additionalOffset = additionalOffset - offset;// + Vector3.Scale(bounds.extents, settings.Direction);
                }
                //sortedGameObjects[i].transform.position = newPos + additionalOffset;
                //Debug.DrawRay(bounds.center, Vector3.right, Color.black, 5f);
                }
            }*/
        }

        private void IncrementObjects(GameObject[] gameObjects, ConfigurationSettings settings)
    {
        if (gameObjects.Length <= 0) { return; }

        foreach (var gameObj in gameObjects)
        {
            Debug.Log(gameObj.name);
        }
    }

    private Bounds GenerateBoundsFromPoints(GameObject gameObj, ConfigurationSettings.CalculationMethodType calculationMethod)
    {
        Bounds bounds = new Bounds();
        List<Vector3> boundsPoints = new List<Vector3>();

        if (calculationMethod == ConfigurationSettings.CalculationMethodType.Collider)
        {
            BoundsExtensions.GetColliderBoundsPoints(gameObj, boundsPoints, 0);
        }
        else if (calculationMethod == ConfigurationSettings.CalculationMethodType.Renderer)
        {
            BoundsExtensions.GetRenderBoundsPoints(gameObj, boundsPoints, 0);
        }
        else
        {
            //bail - wrong type
            return bounds;
        }

        bounds.center = boundsPoints[0];
        foreach (Vector3 point in boundsPoints)
        {
            bounds.Encapsulate(point);
        }

        return bounds;
    }

    private Vector3 ActiveMinMaxValue(GameObject[] gameObjects, ConfigurationSettings.CalculationMethodType calculationMethod, Vector3 direction)
    {
        //Grab all the min max values per axis.
        Tuple<Vector3, Vector3> MinMax = FindAndMinMax(gameObjects, calculationMethod);
        /*
        if (Vector3.Dot(direction, Vector3.one) > 0)
        {
            Debug.Log("we're out maxin");
            ActiveMinMax = MinMax.Item2;
        }
        else
        {
            ActiveMinMax = MinMax.Item1;
            Debug.Log("MIN");
        }
        */

        //Check the direction - it will determine min or max
        return (Vector3.Dot(direction, Vector3.one) > 0) ? MinMax.Item2 : MinMax.Item1;
    }

    private Tuple<Vector3, Vector3> FindAndMinMax(GameObject[] gameObjects, ConfigurationSettings.CalculationMethodType calculationMethod)
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
                Bounds bounds = GenerateBoundsFromPoints(gameObj, calculationMethod);
                
                DrawBox(bounds, Color.yellow, 5f);

                if (bounds.size == Vector3.zero)
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

    void DrawBox(Bounds bounds, Color color, float duration)
    {
        
        Vector3 v3Center = bounds.center;
        Vector3 v3Extents = bounds.extents;

        Vector3 v3FrontTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top left corner
        Vector3 v3FrontTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top right corner
        Vector3 v3FrontBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom left corner
        Vector3 v3FrontBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom right corner
        Vector3 v3BackTopLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top left corner
        Vector3 v3BackTopRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top right corner
        Vector3 v3BackBottomLeft = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom left corner
        Vector3 v3BackBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom right corner

        Debug.DrawLine(v3FrontTopLeft, v3FrontTopRight, color, duration);
        Debug.DrawLine(v3FrontTopRight, v3FrontBottomRight, color, duration);
        Debug.DrawLine(v3FrontBottomRight, v3FrontBottomLeft, color, duration);
        Debug.DrawLine(v3FrontBottomLeft, v3FrontTopLeft, color, duration);

        Debug.DrawLine(v3BackTopLeft, v3BackTopRight, color, duration);
        Debug.DrawLine(v3BackTopRight, v3BackBottomRight, color, duration);
        Debug.DrawLine(v3BackBottomRight, v3BackBottomLeft, color, duration);
        Debug.DrawLine(v3BackBottomLeft, v3BackTopLeft, color, duration);

        Debug.DrawLine(v3FrontTopLeft, v3BackTopLeft, color, duration);
        Debug.DrawLine(v3FrontTopRight, v3BackTopRight, color, duration);
        Debug.DrawLine(v3FrontBottomRight, v3BackBottomRight, color, duration);
        Debug.DrawLine(v3FrontBottomLeft, v3BackBottomLeft, color, duration);
    }


}