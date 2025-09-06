using System.Collections;
using System.Collections.Generic;
using GraphProcessor;
using UnityEditor;
using UnityEngine;
using Wintek.Graph.Serializable.Editor;
using Status = UnityEngine.UIElements.DropdownMenuAction.Status;

namespace Wintek.Graph.Editor
{
    public class ProcessCreatorWindow : BaseGraphWindow
    {
        BaseGraph tmpGraph;
        ProcessCreatorCustomToolBarView toolbarView;
        
       //[MenuItem("Wintek/ProcessCreatorWindow")]
       // public static BaseGraphWindow OpenWithTmpGraph()
       // {
       //    var graphWindow = CreateWindow<ProcessCreatorWindow>();

       //     // When the graph is opened from the window, we don't save the graph to disk
       //     graphWindow.tmpGraph = ScriptableObject.CreateInstance<BaseGraph>();
       //     graphWindow.tmpGraph.hideFlags = HideFlags.HideAndDontSave;     
       //     graphWindow.InitializeGraph(graphWindow.tmpGraph);
       //     graphWindow.Show();            

       //     return graphWindow;
       // }

        protected override void OnDestroy()
        {
            graphView?.Dispose();
            graphView?.SaveGraphToDisk();
            //DestroyImmediate(tmpGraph);
        }

        protected override void InitializeWindow(BaseGraph graph)
        {
            titleContent = new GUIContent($"[{graph.name}]절차 저작도구");

            if (graphView == null)
            {
                graphView = new ProcessCreatorView(this);
                toolbarView = new ProcessCreatorCustomToolBarView(graphView);
                //graphView.Add(new MiniMapView(graphView));
                graphView.Add(toolbarView);
            }
            
            rootView.Add(graphView);
        }
    }
}
