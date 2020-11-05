// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

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
        public bool UseWorldSpace;
        public Vector3 Direction;
        public Vector3 MovementAmount;

        public ConfigurationSettings(CalculationMethodType calculationMethod = CalculationMethodType.Origin,
                                     bool matDirectionRot = false,
                                     bool useWorldSpace = true,
                                     Vector3 customDir = default(Vector3),
                                     Vector3 movementAmount = default(Vector3))
        {
            CalculationMethod = calculationMethod;
            MatchRotation = matDirectionRot;
            UseWorldSpace = useWorldSpace;
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
    private bool showInEditorWindow = true;

    [SerializeField]
    Tuple<Vector3, Vector3> alignMinMax = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);

    [SerializeField]
    Tuple<Vector3, Vector3> distributeMinMax = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);


    private readonly float sectionSpace = 24.0f;

    [MenuItem("Mixed Reality Toolkit/Utilities/Align and Distribute")]
    public static void ShowWindow()
    {
        GetWindow<AlignAndDistributeWindow>(false, "Aign and Distribute", true);
    }

    private void OnFocus()
    {
        SceneView.duringSceneGui -= SceneView_duringSceneGui;
        SceneView.duringSceneGui += SceneView_duringSceneGui;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= this.SceneView_duringSceneGui;
    }

    private void OnInspectorUpdate()
    {
        if(Selection.gameObjects.Length <= 0) { return; }
        alignMinMax = FindAndMinMax(Selection.gameObjects, alignSettings.CalculationMethod);
        distributeMinMax = FindAndMinMax(Selection.gameObjects, distributeSettings.CalculationMethod);
    }
 
    private void SceneView_duringSceneGui(SceneView obj)
    {
        if (Selection.gameObjects.Length <= 1) { return; }


        Bounds selectionB = GenerateBoundsFromPoints(Selection.gameObjects, ConfigurationSettings.CalculationMethodType.Collider);

        DrawAlignButton("+X", Vector3.right, selectionB.center + Vector3.right * selectionB.extents.x);
        DrawAlignButton("-X", Vector3.left, selectionB.center - Vector3.right * selectionB.extents.x);

        DrawAlignButton("+Y", Vector3.up, selectionB.center + Vector3.up * selectionB.extents.y);
        DrawAlignButton("-Y", Vector3.down, selectionB.center - Vector3.up * selectionB.extents.y);

        DrawAlignButton("+Z", Vector3.forward, selectionB.center + Vector3.forward * selectionB.extents.z);
        DrawAlignButton("-Z", Vector3.back, selectionB.center - Vector3.forward * selectionB.extents.z);
    }

    private void DrawAlignButton(string label, Vector3 direction, Vector3 position)
    {
        Handles.Label(position, "Align " + label);
        if(Handles.Button(position, Quaternion.LookRotation(direction, Vector3.up), HandleUtility.GetHandleSize(Vector3.zero), HandleUtility.GetHandleSize(Vector3.zero), Handles.RectangleHandleCap))
        {
            alignSettings.Direction = direction;
            AlignObjects(Selection.gameObjects, alignSettings);
        }
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        showInEditorWindow = EditorGUILayout.Toggle(showInEditorWindow, "Show in scene view");
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Align", EditorStyles.boldLabel);

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
            EditorGUILayout.Space(sectionSpace);
        }

        EditorGUILayout.LabelField("Distribute", EditorStyles.boldLabel);
        distributeSettings.CalculationMethod = (ConfigurationSettings.CalculationMethodType)EditorGUILayout.EnumPopup("Calculation method: ", distributeSettings.CalculationMethod);
        EditorGUILayout.Space();

        useCustomDistributeDirection = GUILayout.Toggle(useCustomDistributeDirection, "Use Custom Direction");
        EditorGUILayout.Space();

        if (useCustomDistributeDirection)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                distributeSettings.Direction = EditorGUILayout.Vector3Field("Custom Direction", distributeSettings.Direction);
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
        EditorGUILayout.Space(sectionSpace);

        EditorGUILayout.LabelField("Increment", EditorStyles.boldLabel);

        incrementSettings.MovementAmount = EditorGUILayout.Vector3Field("Increment Amount", incrementSettings.MovementAmount);
        EditorGUILayout.Space();

        using (new EditorGUI.IndentLevelScope())
        {
            if (GUILayout.Button("Increment objects"))
            {
                IncrementObjects(Selection.gameObjects, incrementSettings);
            }
        }
    }

    private void DisplayAlign(GameObject[] gameObjects, ConfigurationSettings settings)
    {
        if (gameObjects.Length <= 0) { return; }
        
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
            Bounds bounds = new Bounds();
            Vector3 additionalOffset = Vector3.zero;

            if (settings.CalculationMethod != ConfigurationSettings.CalculationMethodType.Origin)
            {
            bounds = GenerateBoundsFromPoints(gameObj, settings.CalculationMethod);
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

        //Grab the min or max value
        Tuple<Vector3, Vector3> MinMax = FindAndMinMax(gameObjects, settings.CalculationMethod);

        GameObject[] sortedGameObjects = gameObjects.OrderBy((x) => Vector3.Scale(MinMax.Item1 - x.transform.position, VectAbs(settings.Direction)).sqrMagnitude).ToArray();

        Vector3 TotalMargin = MinMax.Item1 - MinMax.Item2;

        // ensure the distribution area is bigger than 0
        for (int i = 0; i < 3; i++)
        {
            if (VectAbs(settings.Direction)[i] != 0.0f)
            {
                if (Mathf.Abs(TotalMargin[i]) < Mathf.Epsilon)
                {
                    //Bail, the delta between min and max on axis is too small
                    Debug.Assert((TotalMargin[i] < Mathf.Epsilon), "Align & Distribute: The distance along the specified direction is too small.");
                    return;
                }
            }
        }

        Vector3 distStep = new Vector3(TotalMargin.x / (sortedGameObjects.Length - 1), TotalMargin.y / (sortedGameObjects.Length - 1), TotalMargin.z / (sortedGameObjects.Length - 1));
        Vector3 distStepOnAxis = Vector3.Scale(distStep, VectAbs(settings.Direction));

        for (int i = 0; i < sortedGameObjects.Length; i++)
        {
            Debug.Log("DistanceStep: " + distStep.ToString("F5") + ", OriginalPosition" + sortedGameObjects[i].transform.position.ToString("F5"));

            Vector3 newPos = MinMax.Item1;

            Vector3 additionalOffset = Vector3.zero;

            for (int j = 0; j < 3; j++)
            {
                newPos[j] = (distStepOnAxis[j] != 0.0f) ? newPos[j] - (distStepOnAxis[j] * i) : sortedGameObjects[i].transform.position[j];
            }

            //use AABB if Collider or Renderer
            Bounds bounds = GenerateBoundsFromPoints(sortedGameObjects[i], settings.CalculationMethod);

            if (bounds.size != Vector3.zero)
            {
                //get the offset from the origin and AABB
                additionalOffset = Vector3.Scale(bounds.center - sortedGameObjects[i].transform.position, VectAbs(settings.Direction));
            }

            sortedGameObjects[i].transform.position = newPos + additionalOffset;
        }

    }

    private void IncrementObjects(GameObject[] gameObjects, ConfigurationSettings settings)
    {
        if (gameObjects.Length <= 0) { return; }

        foreach (var gameObj in gameObjects)
        {
            Debug.Log("Movement Amount: " + settings.MovementAmount);
            gameObj.transform.position += settings.MovementAmount;
        }
    }

    private static void SetPosition(Transform transform, bool useWorldSpace, Vector3 newValue)
    {
        if (useWorldSpace)
        {
            transform.position = newValue;
        }
        else
        {
            transform.localPosition = newValue;
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
        
        if (boundsPoints.Count <= 0)
        {
            boundsPoints.Add(gameObj.transform.position);
        }

        bounds.center = boundsPoints[0];
        foreach (Vector3 point in boundsPoints)
        {
            bounds.Encapsulate(point);
        }

        return bounds;
    }

    private Bounds GenerateBoundsFromPoints(GameObject[] gameObjs, ConfigurationSettings.CalculationMethodType calculationMethod)
    {
        Bounds bounds = new Bounds();
        List<Vector3> boundsPoints = new List<Vector3>();

        foreach (GameObject gameObj in gameObjs)
        {
            List<Vector3> objPoints = new List<Vector3>();

            if (calculationMethod == ConfigurationSettings.CalculationMethodType.Collider)
            {
                BoundsExtensions.GetColliderBoundsPoints(gameObj, objPoints, 0);
            }
            else if (calculationMethod == ConfigurationSettings.CalculationMethodType.Renderer)
            {
                BoundsExtensions.GetRenderBoundsPoints(gameObj, objPoints, 0);
            }

            if(objPoints.Count <= 0)
            {
                boundsPoints.Add(gameObj.transform.position);
            }
            boundsPoints.AddRange(objPoints);
        }

        bounds.center = boundsPoints[0];
        foreach (Vector3 point in boundsPoints)
        {
            bounds.Encapsulate(point);
        }

        return bounds;
    }


    private Bounds GenerateBoundsFromPositions(GameObject[] gameObjects)
    {
        Bounds bounds = new Bounds();
        List<Vector3> boundsPoints = new List<Vector3>();

        if (gameObjects.Length <= 1) { return bounds; }

        foreach (GameObject gameObject in gameObjects)
        {
            boundsPoints.Add(gameObject.transform.position);
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

        //Check the direction - it will determine min or max
        return (Vector3.Dot(direction, Vector3.one) > 0) ? MinMax.Item2 : MinMax.Item1;
    }

    private Vector3 ActiveMinMaxValue(Tuple<Vector3, Vector3> MinMax, Vector3 direction)
    {
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
                    min[i] = (bounds.center[i] < min[i]) ? bounds.center[i] : min[i];
                    max[i] = (bounds.center[i] > max[i]) ? bounds.center[i] : max[i];
                }
            }
        }

        if (calculationMethod == ConfigurationSettings.CalculationMethodType.Origin || fallbackObjects.Count > 0)
        {
            if (initialPositionValid == false)
            {
                //Since zero is a position, we need to set a default within the range of the object group
                min = max = gameObjects[0].transform.position;
                initialPositionValid = true;
            }

            GameObject[] gameObjectQueue = (fallbackObjects.Count > 0) ? fallbackObjects.ToArray() : gameObjects;
            foreach (GameObject gameObj in gameObjectQueue)
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