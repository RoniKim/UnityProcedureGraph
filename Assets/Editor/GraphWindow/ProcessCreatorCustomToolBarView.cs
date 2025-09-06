using System.IO;
using GraphProcessor;
using Newtonsoft.Json;
using UnityEditor;
using Wintek.Graph.Serializable;
using Wintek.Graph.Serializable.Editor;
using Status = UnityEngine.UIElements.DropdownMenuAction.Status;
namespace Wintek.Graph.Editor
{
    public class ProcessCreatorCustomToolBarView : ToolbarView
    {
        public ProcessCreatorCustomToolBarView(BaseGraphView graphView) : base(graphView) { }

        protected override void AddButtons()
        {
            // Add the hello world button on the left of the toolbar
            //AddButton("Hello !", () => Debug.Log("Hello World"), left: false);
            //base.AddButtons();
            AddButton("Center", graphView.ResetPositionAndZoom);            
            AddButton("Load Asset", () =>
            {
                graphView.SaveGraphToDisk();
                EditorGraphSerializable.LoadGraphAsset();
            });
            AddButton("Graph SaveToJson", () =>
            {
                graphView.SaveGraphToDisk();
                GraphSerializable.SaveJsonData(graphView.graph);                
            });
            AddButton("Graph LoadToJson", () => LoadJsonFile());            
        }
        void LoadJsonFile()
        {  
            var _loadPath = UnityEditor.EditorUtility.OpenFilePanel("불러오기", "", "json");
            var serializableGraph = GraphSerializable.LoadJsonFile(_loadPath);
            //var Graph = GraphSerializable.ConvertJsonToGraph(_loadPath);

            graphView.graph.nodes.Clear();
            graphView.graph.groups.Clear();
            graphView.graph.stackNodes.Clear();
            graphView.graph.stickyNotes.Clear();
            graphView.graph.edges.Clear();
            graphView.graph.graphOutputs.Clear();
            graphView.graph.nodesPerGUID.Clear();
            graphView.graph.edgesPerGUID.Clear();
            graphView.Initialize(graphView.graph);
            foreach (var node in serializableGraph.nodes)
            {
                graphView.AddNode(node);
            }
            foreach (var node in serializableGraph.groupNodes)
            {
                graphView.AddGroup(node);
            }
            foreach (var node in serializableGraph.stackNodes)
            {
                graphView.AddStackNode(node);
            }
            foreach (var node in serializableGraph.stickyNotes)
            {
                graphView.AddStickyNote(node);
            }

            foreach (var edge in serializableGraph.edges)
            {
                var inputNode = graphView.nodeViews.Find(n => n.nodeTarget.GUID == edge.inputNodeGuid);
                var outputNode = graphView.nodeViews.Find(n => n.nodeTarget.GUID == edge.outputNodeGuid);

                if (inputNode != null && outputNode != null)
                {
                    graphView.Connect(inputNode.GetPortViewFromFieldName(edge.inputFieldName, edge.inputIdentifier), outputNode.GetPortViewFromFieldName(edge.outputFieldName, edge.outputIdentifier));
                }
            }
            
            var window = EditorWindow.GetWindow<ProcessCreatorWindow>(true);
            window.InitializeGraph(graphView.graph);
            graphView.SaveGraphToDisk();
        }
    }
}