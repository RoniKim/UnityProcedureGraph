using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GraphProcessor;
using UnityEngine;
using Roni.Graph.Node;

namespace Roni.Graph.Runtime
{
    public enum NodeChangeState
    {
        Nomal, End, Cancel
    }
    /// <summary>
    /// RuntimeProcess에서 EndCheck 후 만족했을때 OnEnd() or OnCancel()을 호출하기전 기다리기위한 인터페이스
    /// </summary>
    public  interface IWaitEndChecker
    {
        UniTask IsEndWaitRequired(BaseConditionNode _node, CancellationToken _cancellationToken = default);
    }
    public abstract class BaseProcedure
    {
        protected IWaitEndChecker waitEndChecker;
        internal delegate void OnChangeNodeDelegate(BaseConditionNode preNode,NodeChangeState preNodeState,BaseConditionNode currentNode);
        internal OnChangeNodeDelegate OnChangeNode;
        public delegate void OnNodeEndDelegate(NodeChangeState preNodeState, BaseConditionNode currentNode);
        public OnNodeEndDelegate OnEndNode;

        public BaseGraph graph { get; private set; }

        public void InjectWaitEndCheker(IWaitEndChecker _waitEndChecker)
        {
            this.waitEndChecker = _waitEndChecker;
        }
        /// <summary>
        /// Manage graph scheduling and processing
        /// </summary>
        /// <param name="graph">Graph to be processed</param>
        public BaseProcedure(BaseGraph graph)
        {
            this.graph = graph;
    
            UpdateComputeOrder();
        }

        internal abstract void UpdateComputeOrder();

        /// <summary>
        /// Schedule the graph into the job system
        /// </summary>
        internal abstract UniTask Run(BaseConditionNode _startNode = null);
    }
}