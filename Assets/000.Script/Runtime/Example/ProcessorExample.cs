
using GraphProcessor;

using System.Collections.Generic;
using UnityEngine;
using Wintek.Graph.Runtime;
using Wintek.Graph.Serializable;

namespace Wintek.Graph.Example
{
    [RequireComponent(typeof(RuntimeProcessorRunner))]
    public class ProcessorExample : MonoBehaviour
    {
        public List<BaseGraph> testGraphs = new List<BaseGraph>();

        private RuntimeProcessorRunner runtimeProcessor;

        private void Awake()
        {
            runtimeProcessor = GetComponent<RuntimeProcessorRunner>();
            RunProcessor();
        }
  
        public void FastForwardChapter(int index = -1)
        {         
            if (index < 0)
            {
                runtimeProcessor.ProcessorControll(runtimeProcessor.GetCurrentProcessorIndex()+1);
                return;
            }
            runtimeProcessor.ProcessorControll(index);

        }

     
        public void RunProcessor()
        {
            Dictionary<string, string> contentJsons = new Dictionary<string, string>();
            foreach (var item in testGraphs)
            {
                BaseGraph graph = item;
                contentJsons[item.name]=GraphSerializable.ConvertGraphToJson(graph);
            }            
            
            RuntimeProcessorRunner.Instance.InitializeProcessors(contentJsons);
            RuntimeProcessorRunner.Instance.RunProcess();      
            
        }
    }
}