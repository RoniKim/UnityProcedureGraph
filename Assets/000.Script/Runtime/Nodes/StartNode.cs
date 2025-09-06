using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphProcessor;
using UnityEngine;

namespace Roni.Graph.Node
{
    [System.Serializable, NodeMenuItem("StartNode")]
    public class StartNode : BaseNode, IConditionalNode
    {
        [Output("Executed", allowMultiple = false)]
        public ConditionalLink outPutcondition;
        public override string name => "StartNode";

        public BaseConditionNode GetExecutedNode()
        {
             return GetOutputNodes().Where(n => n is BaseConditionNode).Select(n => n as BaseConditionNode).FirstOrDefault();
        }

        public override FieldInfo[] GetNodeFields() => base.GetNodeFields();
    }
}