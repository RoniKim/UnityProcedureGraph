using GraphProcessor;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Roni.Graph.Node;
using Roni.Graph.Runtime;
namespace Roni.Graph.Node
{
    public enum WaitState
    {
        OR, AND
    }
    public class BaseMultipleConditionNode : BaseConditionNode
    {
        [Output(allowMultiple = false)]
        public List<ConditionalLink> execute;

        [Input]
        public List<ConditionalLink> inputs;

        [SerializeField]
        public  WaitState state;

        /// <summary>
        /// Forward호출 시 OR상태이면 몇 번째로 진행할 것인지 지정
        /// </summary>
        [Setting]
        public int forWardinitValue = -1;
        /**Input으로 연결된 node*/
        protected List<BaseConditionNode> combineMultiNodes;
        protected List<BaseConditionNode> outputNodes;

        [SerializeField, HideInInspector]
        protected int orStateOutputIndex = -1;

        //public override string name => "MultipleConditionNode";
        public override bool isRenamable => true;
        // We keep the max port count so it doesn't cause binding issues
        [SerializeField, HideInInspector]
        protected int portCount = 1;

        [JsonIgnore]
        protected List<GameObject> combineNodeTargets = new List<GameObject>();

        [CustomPortBehavior(nameof(inputs))]
        protected virtual IEnumerable<PortData> ListPortBehavior(List<SerializableEdge> edges)
        {
            portCount = edges.Count + 1;

            for (int i = 0; i < portCount; i++)
            {
                yield return new PortData
                {
                    displayName = "In " + i,
                    displayType = typeof(ConditionalLink),
                    identifier = i.ToString(), // Must be unique
                };
            }
        }
        [CustomPortBehavior(nameof(execute))]
        protected virtual IEnumerable<PortData> ListOutputBehavior(List<SerializableEdge> edges)
        {

            for (int i = 0; i < portCount - 1; i++)
            {
                yield return new PortData
                {
                    displayName = "Out " + i,
                    displayType = typeof(ConditionalLink),
                    identifier = i.ToString(), // Must be unique
                };
            }

        }
     
        [CustomPortInput(nameof(inputs), typeof(ConditionalLink))]
        protected void PullInputs(List<SerializableEdge> inputEdges)
        {
            if (combineMultiNodes == null)
                combineMultiNodes = new List<BaseConditionNode>();

            foreach (var edge in inputEdges)
            {
                if (combineMultiNodes.Find(x => x.GUID.Equals(edge.outputNode.GUID)) != null)
                    continue;
                combineMultiNodes.Add(edge.outputNode as BaseConditionNode);
            }
        }
        [CustomPortOutput(nameof(execute), typeof(ConditionalLink))]
        protected void PullOutputs(List<SerializableEdge> outputEdges)
        {
            if (outputNodes == null)
                outputNodes = new List<BaseConditionNode>();

            foreach (var edge in outputEdges)
            {
                if (outputNodes.Find(x => x.GUID.Equals(edge.inputNode.GUID)) != null)
                    continue;
                outputNodes.Add(edge.inputNode as BaseConditionNode);
            }
        }

        public override BaseConditionNode GetExecutedNode()
        {
            if (outputNodes == null || outputNodes.Count < 1)
                return null;
            else if (state == WaitState.AND)
                return outputNodes[0];
            else if (state == WaitState.OR)
            {                
                if (orStateOutputIndex == -1)
                    return outputNodes.First();

                orStateOutputIndex = orStateOutputIndex >= outputNodes.Count ? outputNodes.Count - 1 : orStateOutputIndex;
                return outputNodes[orStateOutputIndex];
            }
            return null;
        }
        public List<BaseConditionNode> GetCombinNodes()
        {
            if (combineMultiNodes == null)
                OnProcess();

            return combineMultiNodes;
        }
        public void InjectOutPutIndex(int index)
        {
            forWardinitValue = index;
            orStateOutputIndex = index;
        }
        public override void OnInit(InitCallbackState _changeState)
        {

        }

        public override bool OnEndCheck()
        {
            return true;
        }

        public override void OnUpdate()
        {

        }

        public override void OnEnd()
        {
            
        }


        public override void OnBackWard()
        {
         
        }

        public override void OnFastForWard()
        {
            
        }

        public override void OnCancel()
        {
          
        }

        public override GameObject[] GetTargets()
        {
            if (combineNodeTargets == null || combineNodeTargets.Count < 1)
            {
                combineNodeTargets = new List<GameObject>();

                if (combineMultiNodes == null)
                {
                    return null;
                }

                foreach (var nodeTargets in GetCombinNodes())
                {
                    if (nodeTargets.GetTargets() == null || nodeTargets.GetTargets().Length < 1)
                        continue;
                    combineNodeTargets.AddRange(nodeTargets.GetTargets());
                }
            }
            return combineNodeTargets.ToArray();
            //return null;
        }

       
        protected void ShowMessageDebug(Color color, string header, string message)
        {
#if UNITY_EDITOR
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            // 리치 텍스트 형식으로 변환
            string richText = $"<color=#{colorHex}>[ {header} ]</color> <color=white>{message}</color>";

            Debug.Log(richText);
#endif
        }
    }
}