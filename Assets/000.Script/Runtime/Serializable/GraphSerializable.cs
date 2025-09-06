using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GraphProcessor;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;


namespace Wintek.Graph.Serializable
{
    [Serializable]
    public class SerializableGraph
    {
        public List<BaseNode> nodes;
        public List<Group> groupNodes;
        public List<BaseStackNode> stackNodes;
        public List<StickyNote> stickyNotes;
        public List<SerializableEdge> edges;
    }
    [Serializable]
    public class SerializableEdge
    {
        public string inputNodeGuid;
        public string inputIdentifier;
        public string outputNodeGuid;
        public string outputIdentifier;
        public string inputFieldName;
        public string outputFieldName;
    }
    public static class GraphSerializable
    {
        public static string ConvertGraphToJson(BaseGraph graph)
        {
            var serializableGraph = new SerializableGraph
            {
                nodes = graph.nodes,
                groupNodes = graph.groups,
                stackNodes = graph.stackNodes,
                stickyNotes = graph.stickyNotes,
                edges = graph.edges.Select(e => new SerializableEdge
                {
                    inputNodeGuid = e.inputNode.GUID,
                    inputIdentifier = e.inputPortIdentifier,
                    outputNodeGuid = e.outputNode.GUID,
                    inputFieldName = e.inputFieldName,
                    outputFieldName = e.outputFieldName,
                    outputIdentifier = e.outputPortIdentifier
                }).ToList()
            };          
            string jsonString = JsonConvert.SerializeObject(serializableGraph, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            });
            return jsonString;
        }
        public static SerializableGraph LoadJsonFile(string jsonFilePath)
        {
            string content = null;
            using (StreamReader sr = new StreamReader(jsonFilePath))
            {
                content = sr.ReadToEnd();
            }
            var serializableGraph = JsonConvert.DeserializeObject<SerializableGraph>(content, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            return serializableGraph;
        }
        public static SerializableGraph LoadJson(string _json)
        {
            string content = _json;
            
            var serializableGraph = JsonConvert.DeserializeObject<SerializableGraph>(content, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            return serializableGraph;
        }
        public static BaseGraph ConvertJsonToGraph(ref BaseGraph graph, string jsonFilePath)
        {
            var serializableGraph = LoadJsonFile(jsonFilePath);

            if (graph == null)
                return null;

            // 노드 추가
            foreach (var node in serializableGraph.nodes)
            {
                graph.AddNode(node);
            }
            foreach (var node in serializableGraph.groupNodes)
            {
                graph.AddGroup(node);
            }
            foreach (var node in serializableGraph.stackNodes)
            {
                graph.AddStackNode(node);
            }
            foreach (var node in serializableGraph.stickyNotes)
            {
                graph.AddStickyNote(node);
            }

            // 엣지 재구성
            foreach (var edge in serializableGraph.edges)
            {
                var inputNode = graph.nodes.Find(n => n.GUID == edge.inputNodeGuid);
                var outputNode = graph.nodes.Find(n => n.GUID == edge.outputNodeGuid);

                if (inputNode != null && outputNode != null)
                {
                    graph.Connect(inputNode.GetPort(edge.inputFieldName, edge.inputIdentifier), outputNode.GetPort(edge.outputFieldName, edge.outputIdentifier));
                }
            }
#if UNITY_EDITOR           
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
            return graph;
        }
        public static BaseGraph ConvertJsonToGraph(string jsonFilePath, BaseGraph graph = null)
        {
            var serializableGraph = LoadJsonFile(jsonFilePath);

            if(graph == null)
                graph = ScriptableObject.CreateInstance<BaseGraph>();

            // 노드 추가
            foreach (var node in serializableGraph.nodes)
            {                
                graph.AddNode(node);
            }
            foreach (var node in serializableGraph.groupNodes)
            {
                graph.AddGroup(node);
            }
            foreach (var node in serializableGraph.stackNodes)
            {
                graph.AddStackNode(node);
            }
            foreach (var node in serializableGraph.stickyNotes)
            {
                graph.AddStickyNote(node);
            }

            // 엣지 재구성
            foreach (var edge in serializableGraph.edges)
            {
                var inputNode = graph.nodes.Find(n => n.GUID == edge.inputNodeGuid);
                var outputNode = graph.nodes.Find(n => n.GUID == edge.outputNodeGuid);

                if (inputNode != null && outputNode != null)
                {
                    graph.Connect(inputNode.GetPort(edge.inputFieldName, edge.inputIdentifier), outputNode.GetPort(edge.outputFieldName, edge.outputIdentifier));
                }
            }

            return graph;
        }
        public static BaseGraph ConvertJsonTextToGraph(string jsonFile, BaseGraph graph = null)
        {
            var serializableGraph = LoadJson(jsonFile);

            if (graph == null)
                graph = ScriptableObject.CreateInstance<BaseGraph>();

            // 노드 추가
            foreach (var node in serializableGraph.nodes)
            {
                graph.AddNode(node);
            }
            foreach (var node in serializableGraph.groupNodes)
            {
                graph.AddGroup(node);
            }
            foreach (var node in serializableGraph.stackNodes)
            {
                graph.AddStackNode(node);
            }
            foreach (var node in serializableGraph.stickyNotes)
            {
                graph.AddStickyNote(node);
            }

            // 엣지 재구성
            foreach (var edge in serializableGraph.edges)
            {
                var inputNode = graph.nodes.Find(n => n.GUID == edge.inputNodeGuid);
                var outputNode = graph.nodes.Find(n => n.GUID == edge.outputNodeGuid);

                if (inputNode != null && outputNode != null)
                {
                    graph.Connect(inputNode.GetPort(edge.inputFieldName, edge.inputIdentifier), outputNode.GetPort(edge.outputFieldName, edge.outputIdentifier));
                }
            }

            return graph;
        }

        public static void SaveJsonData(this BaseGraph graph, string _savePath = null, string chpaterName = null)
        {            
            if (_savePath == null)
            {
#if UNITY_EDITOR
                _savePath = UnityEditor.EditorUtility.SaveFilePanel("저장", "", "JsonFile", "json");
#endif
            }

            if (string.IsNullOrEmpty(_savePath)) { return; }
#if UNITY_EDITOR
            if(graph == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Export 오류", $"[ {chpaterName} ]은 Graph파일을 뽑는데 실패하였습니다.", "확인");
                return;
            }
#endif
            string content = ConvertGraphToJson(graph);
            //File.WriteAllText(@"D:\000. Project\000. VTS\Test.json", content);
            File.WriteAllText(_savePath, content);
        }
      
    }
}
