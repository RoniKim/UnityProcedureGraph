
using GraphProcessor;

using System.Collections.Generic;
using UnityEngine;
using Roni.Graph.Runtime;
using Roni.Graph.Serializable;
using Cysharp.Threading.Tasks;

namespace Roni.Graph.Example
{
    [RequireComponent(typeof(UnityProcedureGraphManager))]
    public class ProcessorExample : MonoBehaviour
    {
        public List<BaseGraph> testGraphs = new List<BaseGraph>();

        private UnityProcedureGraphManager runtimeProcessor;

        private void Awake()
        {
            runtimeProcessor = GetComponent<UnityProcedureGraphManager>();
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
            
            UnityProcedureGraphManager.Instance.InitializeProcessors(contentJsons);
            UnityProcedureGraphManager.Instance.RunProcess().Forget();      
            
        }

        
    }
}