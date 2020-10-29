using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AlignAndDistributeVisualizer))]
public class AlignAndDistributeVisualizerEditor : Editor
{
    void OnSceneGUI()
    {
        AlignAndDistributeVisualizer viz = target as AlignAndDistributeVisualizer;
         
        Handles.DrawLine(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f));
    }
}