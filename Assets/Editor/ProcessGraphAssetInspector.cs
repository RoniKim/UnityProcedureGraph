using System.Collections;
using System.Collections.Generic;
using GraphProcessor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Roni.Graph.Editor
{
    [CustomEditor(typeof(BaseGraph), true)]
    public class ProcessGraphAssetInspector : GraphInspector
    {
        protected override void CreateInspector()
        {
            base.CreateInspector();

            root.Add(new Button(() => EditorWindow.GetWindow<ProcessCreatorWindow>().InitializeGraph(target as BaseGraph))
            {
                text = "Open Process Graph Window"
            });      
        }
    }
}