using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;


namespace Wintek.Graph.Node
{
    [System.Serializable, NodeMenuItem("저작도구/StringListNode")]
    public class StringListNode : BaseNode
    {
        [Output(name = "TextList"), SerializeField]
        public List<string> stringDatas;
        public override string name => "StringListNode";
        public override bool isRenamable => true;
        protected override void Enable()
        {
            if (stringDatas == null)
                stringDatas = new List<string>();
        }

    }
}