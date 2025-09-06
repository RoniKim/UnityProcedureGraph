using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GraphProcessor;
using Newtonsoft.Json;
using Wintek.Graph.Editor;
using UnityEditor;
using UnityEngine;


namespace Wintek.Graph.Serializable.Editor
{
   
    public static class EditorGraphSerializable
    {
        [MenuItem("Wintek/ProcessCreatorWindow")]
        public static void OpenWithGraph()
        {
            BaseGraph graph = CreateGraphAsset("TempGraph");            
            EditorWindow.GetWindow<ProcessCreatorWindow>(true).InitializeGraph(graph as BaseGraph);            
        }        
        public static BaseGraph CreateGraphAsset(string _assetName, string _path = null)
        {
            BaseGraph graph = ScriptableObject.CreateInstance<BaseGraph>();
            if (string.IsNullOrEmpty(_path))
            {
                string savePath = EditorUtility.SaveFilePanelInProject("Save Asset", _assetName, "asset", "");
                if (!string.IsNullOrEmpty(savePath))
                    AssetDatabase.CreateAsset(graph, savePath);
            }
            else
            {

               _path = Path.Combine(_path, _assetName + ".asset");
                AssetDatabase.CreateAsset(graph, _path);                
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return graph;
        }
  
        public static void CreateAssetToJsonFile(string _assetName, string _jsonPath = null, string _assetSavePath = null)
        {
            //Debug.Log(_assetSavePath + "\\" + _assetName + ".asset");
           var baseGraph =  CreateGraphAsset(_assetName, _assetSavePath);
            if(string.IsNullOrEmpty(_jsonPath))
                _jsonPath = UnityEditor.EditorUtility.OpenFilePanel("불러오기", "", "json");

            BaseGraph createAsset =  AssetDatabase.LoadAssetAtPath(_assetSavePath+"\\"+_assetName+".asset" , typeof(BaseGraph)) as BaseGraph;
            //Debug.Log(createAsset);
            try
            {                
             
               GraphSerializable.ConvertJsonToGraph(ref createAsset ,_jsonPath);             

            }
            catch (Exception ex)
            {
                Debug.LogError($"Error converting JSON to graph: {ex.Message}");
              //  Debug.LogError(ex.StackTrace);
            }
        }
        public static void LoadGraphAsset()
        {
            string path = EditorUtility.OpenFilePanel("Load Asset", "Assets", "asset");

            if (string.IsNullOrEmpty(path)) return;

            path = "Assets" + path.Substring(Application.dataPath.Length);
            var loadGraph = AssetDatabase.LoadAssetAtPath<BaseGraph>(path);

            if (loadGraph != null)
                EditorWindow.GetWindow<ProcessCreatorWindow>(true).InitializeGraph(loadGraph as BaseGraph);
        }
        public static void LoadGraphAsset(string _path)
        {   
            if (string.IsNullOrEmpty(_path)) return;

            var loadGraph = AssetDatabase.LoadAssetAtPath<BaseGraph>(_path);

            if (loadGraph != null)
                EditorWindow.GetWindow<ProcessCreatorWindow>(true).InitializeGraph(loadGraph as BaseGraph);
        }
    }
}
