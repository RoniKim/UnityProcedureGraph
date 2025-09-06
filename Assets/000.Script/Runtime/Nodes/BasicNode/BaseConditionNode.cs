using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GraphProcessor;
using Newtonsoft.Json;
using UnityEngine;
using Wintek.Graph.Runtime;

namespace Wintek.Graph.Node
{
       
    public enum InitCallbackState
    {
        Update,
        Fastforward,
        Backworad
    }
    public struct ConditionalLink
    {
    }
    public interface IConditionalNode
    {
        BaseConditionNode GetExecutedNode();

        FieldInfo[] GetNodeFields(); // Provide a custom order for fields (so conditional links are always at the top of the node)
    }
    [System.Serializable]
    public abstract class BaseConditionNode : BaseNode, IConditionalNode
    {
        [Input(name = "Input", allowMultiple = true)]
        public ConditionalLink executed;

        [JsonIgnore]
        internal RuntimeProcessor owner;
        public RuntimeProcessor Owner => owner;

        public override bool isRenamable => true;

        [HideInInspector]
        public bool isEndCheckStop = false;
        public abstract BaseConditionNode GetExecutedNode();

        public override FieldInfo[] GetNodeFields()
        {
            var fields = base.GetNodeFields();
            Array.Sort(fields, (f1, f2) => f1.Name == nameof(executed) ? -1 : 1);
            return fields;
        }

        /// <summary>
        /// 노드를 동기화할 때, 노드를 실행할지 여부 (무조건 Cancel로 종료됨)
        /// </summary>
        public virtual bool isNodeSyncRun => false;
        public abstract GameObject[] GetTargets();
        /// <summary>
        /// 노드의 초기값을 지정한다.
        /// </summary>
        protected override void Process()
        {
            base.Process();
        }
        /// <summary>
        /// 해당 노드의 종료 조건을 지정한다.
        /// </summary>
        /// <returns></returns>
        public abstract bool OnEndCheck();
        /// <summary>
        /// 해당 노드가 매 프레임마다 해야할 행동을 지정한다.
        /// </summary>
        public abstract void OnUpdate();
        /// <summary>
        /// 노드가 종료되었을때 호출할 이벤트를 지정한다.
        /// </summary>
        public abstract void OnEnd();

        /// <summary>        
        /// BackWrad, FastForward 등 node가 시작될 때 처음 한번 호출        
        /// </summary>
        /// <param name="isUpdate">Update가 호출될때만 True 호출</param>
        public abstract void OnInit(InitCallbackState _state);
        /// <summary>
        /// 노드가 되돌려질때 행동을 지정한다.
        /// </summary>
        public abstract void OnBackWard();
        /// <summary>
        /// 노드가 자동으로 진행될때의 행동을 지정한다.
        /// </summary>
        public abstract void OnFastForWard();

        /// <summary>
        /// 노드가 캔슬되었을때 호출할 이벤트를 지정한다.
        /// </summary>
        public abstract void OnCancel();
    }


}