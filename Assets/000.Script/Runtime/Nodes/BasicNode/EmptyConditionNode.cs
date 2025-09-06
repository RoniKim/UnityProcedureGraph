using GraphProcessor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Roni.Graph.Node
{
    /// <summary>
    /// (Condition) �ƹ��� ����� ���� ����ִ� ���
    /// </summary>
    [System.Serializable, NodeMenuItem("��Ÿ/EmptyNode")]
    public class EmptyConditionNode : BaseConditionNode, IConditionalNode
    {
        [Output("Execute", allowMultiple = false)]
        public ConditionalLink execute;

        public override string name => "EmptyNode";
        public override BaseConditionNode GetExecutedNode()
        {
            return outputPorts.FirstOrDefault(n => n.fieldName == nameof(execute))
                    .GetEdges().Select(e => e.inputNode as BaseConditionNode).FirstOrDefault();
        }


        public override GameObject[] GetTargets()
        {
            return null;
        }

        public override void OnBackWard()
        {
            
        }

        public override void OnCancel()
        {
            
        }

        public override void OnEnd()
        {
            
        }

        public override bool OnEndCheck()
        {
            return true;
        }

        public override void OnFastForWard()
        {
            
        }

        public override void OnInit(InitCallbackState _state)
        {
            
        }

        public override void OnUpdate()
        {
            
        }
    }
}
