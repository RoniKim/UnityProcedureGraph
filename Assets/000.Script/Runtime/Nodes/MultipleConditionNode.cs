using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using System.CodeDom.Compiler;
using Newtonsoft.Json;
using System;
using Cysharp.Threading.Tasks;
namespace Wintek.Graph.Node
{  
    [Serializable, NodeMenuItem("저작도구/MultipleConditionNode")]
    public class MultipleConditionNode : BaseMultipleConditionNode, IConditionalNode
    {
        public override string name => "MultipleConditionNode";
        protected override void Process()
        {
            base.Process();

            foreach (var item in GetCombinNodes())
            {
                item.OnProcess();
            }
        }

        public override void OnBackWard()
        {
            foreach (var item in GetCombinNodes())
            {
                ShowMessageDebug(Color.green, "MultiNodeBackWard", item.GetCustomName());
                item.OnBackWard();
            }
        }

        public override void OnFastForWard()
        {
            if (state == WaitState.AND)
                foreach (var node in GetCombinNodes())
                {
                    ShowMessageDebug(Color.green, "MultiNodeFastForward(AND)", node.GetCustomName());
                    node.OnInit(InitCallbackState.Fastforward);
                    node.OnFastForWard();
                }
            else
            {
                for (int i = 0; i < GetCombinNodes().Count; i++)
                {
                    ShowMessageDebug(Color.green, "MultiNodeFastForward(OR)", GetCombinNodes()[i].GetCustomName());
                    if (i == forWardinitValue)
                    {
                        GetCombinNodes()[i].OnInit(InitCallbackState.Fastforward);
                        GetCombinNodes()[i].OnFastForWard();
                    }
                    else
                        GetCombinNodes()[i].OnCancel();
                }
            }
        }

        public override void OnCancel()
        {
         
        }
      
    }
}