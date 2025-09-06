using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using GraphProcessor;
using UnityEngine;
using Wintek.Graph.Node;

namespace Wintek.Graph.Runtime
{

    public class RuntimeProcessor : BaseRuntimeProcessor
    {
        private StartNode startNode;
        public StartNode StartNode
        {
            get
            {
                if (startNode == null)
                    startNode = graph.nodes.Where(n => n is StartNode).Select(n => n as StartNode).FirstOrDefault();
                return startNode;
            }
        }
        public BaseConditionNode EndNode
        {
            get
            {
                return graph.nodes.Where(n => n is BaseConditionNode).Select(n => n as BaseConditionNode).Where(x =>
                {
                    if (x.GetExecutedNode() == null) return true;
                    return false;
                }).FirstOrDefault();
            }
        }
        public BaseConditionNode PreNode { get; private set; }
        private BaseConditionNode currentNode;
        public BaseConditionNode CurrentNode
        {
            get
            {
                return currentNode;
            }
            private set
            {
                if (currentNode != value)
                {
                    if (value != null)
                    {
                        ChildDataNodesOnProcess(value);
                        value.OnProcess();
                    }
                    PreNode = currentNode;
                    currentNodeChangeState = NodeChangeState.Nomal;
                    currentNode = value;
                    OnChangeNode?.Invoke(PreNode, currentNodeChangeState, value);
                }
            }
        }
        private NodeChangeState currentNodeChangeState;
        private CancellationTokenSource mainCts;
        public bool IsProcessing => mainCts != null && !mainCts.Token.IsCancellationRequested;

        public RuntimeProcessor(BaseGraph graph) : base(graph)
        {
            var baseconditionNodes = graph.nodes.OfType<BaseConditionNode>();
            foreach (var node in baseconditionNodes)
            {
                node.owner = this;
            }
        }
        /// <summary>
        /// 진행한 Nodes
        /// </summary>
        public Stack<string> processedNodeStackGUID
        {
            get; private set;
        }

        internal override void UpdateComputeOrder()
        {
            startNode = graph.nodes.Where(n => n is StartNode).Select(n => n as StartNode).FirstOrDefault();
            processedNodeStackGUID = new Stack<string>();
        }
        internal override async UniTask Run(BaseConditionNode _startNode = null)
        {
            mainCts = new CancellationTokenSource();

            if (_startNode == null)
                processedNodeStackGUID.Clear();

            try
            {
                _startNode ??= startNode.GetExecutedNode();
                CurrentNode = _startNode;
                while (CurrentNode != null)
                {
                    processedNodeStackGUID.Push(CurrentNode.GUID);

                    await ProcessNode(CurrentNode);
                    if (CurrentNode != null)
                    {
                        await UniTask.NextFrame(mainCts.Token);
                        CurrentNode = GetNextNode(CurrentNode);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        public async UniTask ProcessNode(BaseConditionNode node)
        {
            ShowMessageDebug(Color.yellow, "StartNode", node.GetCustomName());

            if (mainCts == null || mainCts.IsCancellationRequested) mainCts = new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(mainCts.Token);
            try
            {
                switch (node)
                {
                    case BaseMultipleConditionNode multipleNode:
                        await ProcessMultipleNode(multipleNode, linkedCts);
                        break;
                    default:
                        await ProcessSingleNode(node, linkedCts.Token);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                if (Application.isPlaying && !HasNullTarget(node.GetTargets()))
                {
                    currentNodeChangeState = NodeChangeState.Cancel;
                    OnEndNode?.Invoke(currentNodeChangeState, node);
                    try
                    {
                        if (waitEndChecker != null)
                        {
                            await waitEndChecker.IsEndWaitRequired(node);
                        }
                    }
                    catch
                    {
                        Debug.LogError("EndWait Error");
                    }
                    ShowMessageDebug(Color.red, "CancelNode", node.GetCustomName());
                    node.OnCancel();
                }
                throw;
            }
            finally
            {
                if (Application.isPlaying && !HasNullTarget(node.GetTargets()))
                {
                    if (!linkedCts.IsCancellationRequested)
                    {
                        currentNodeChangeState = NodeChangeState.End;
                        OnEndNode?.Invoke(currentNodeChangeState, node);
                        try
                        {
                            if (waitEndChecker != null)
                                await waitEndChecker.IsEndWaitRequired(node);
                        }
                        catch
                        {
                            Debug.LogError("EndWait Error");
                        }
                        ShowMessageDebug(Color.yellow, "EndNode", node.GetCustomName());
                        node.OnEnd();
                    }

                }
            }
        }

        private async UniTask ProcessMultipleNode(BaseMultipleConditionNode node, CancellationTokenSource cts)
        {
            //멀티 노드 Init
            node.OnInit(InitCallbackState.Update);

            //var tasks = node.GetCombinNodes().Select(n => ProcessSingleNode(n, cts.Token)).ToList();
            var tasks = node.GetCombinNodes().Select(async (n, index) =>
            {
                try
                {
                    ShowMessageDebug(Color.yellow, "MultiChildStart", n.GetCustomName());
                    await ProcessSingleNode(n, cts.Token);
                    if (Application.isPlaying)
                    {

                        var childNodeChangeState = NodeChangeState.End;
                        OnEndNode?.Invoke(childNodeChangeState, n);
                        ShowMessageDebug(Color.yellow, "MultiChildEnd", n.GetCustomName());
                        n.OnEnd();
                    }
                }
                catch (OperationCanceledException)
                {
                    if (Application.isPlaying)
                    {
                        var childNodeChangeState = NodeChangeState.Cancel;
                        OnEndNode?.Invoke(childNodeChangeState, n);
                        ShowMessageDebug(Color.red, "MultiChildCancel", n.GetCustomName());
                        n.OnCancel();
                    }
                    return false; // 실패한 작업을 표시
                }
                return true; // 성공한 작업을 표시
            }).ToArray();

            if (node.state == WaitState.AND)
            {
                var results = await UniTask.WhenAll(tasks);

                // 모든 작업 실행 후 실패한 작업이 있는지 확인
                if (results.Any(r => r == false))
                {
                    throw new OperationCanceledException();// new Exception("Some child tasks failed");
                }
            }
            else if (node.state == WaitState.OR)
            {
                /*기존 코드
                var successIndex = await UniTask.WhenAny(tasks);
                Debug.Log($"{successIndex.winArgumentIndex} / {successIndex.result}" );
                node.InjectOutPutIndex(successIndex.winArgumentIndex);                
                cts.Cancel();

                //// 모든 작업 실행 후 실패한 작업이 있는지 확인
                //if (successIndex.winArgumentIndex == 0)
                //{
                //    throw new OperationCanceledException();// new Exception("Some child tasks failed");
                //}
                */

                //수정 코드
                (int winArgumentIndex, bool result) = await UniTask.WhenAny(tasks);

                //실행된 작업 결과가 실패 -> 절차 넘기기
                if (result == false)
                {
                    throw new OperationCanceledException();// new Exception("Some child tasks failed");
                }
                else
                {
                    node.InjectOutPutIndex(winArgumentIndex);
                    cts.Cancel();
                }
            }
        }

        private async UniTask ProcessSingleNode(BaseConditionNode node, CancellationToken token)
        {
            node.isEndCheckStop = false;
            node.OnInit(InitCallbackState.Update);
            while (!token.IsCancellationRequested)
            {
                node.OnUpdate();
                if (node.OnEndCheck() || node.isEndCheckStop)
                {
                    break;
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        public void ChildDataNodesOnProcess(BaseConditionNode _parnetNode)
        {
            //노드와 연결된 데이터 노드들 초기화 및 실행
            var dataNodes = GetChildDataNodes(_parnetNode);
            if (dataNodes != null && dataNodes.Count > 0)
                while (dataNodes.Count > 0)
                    dataNodes.Pop().OnProcess();
        }

        private Stack<BaseNode> GetChildDataNodes(BaseNode node, Stack<BaseNode> childStack = null)
        {
            Stack<BaseNode> returnValues = new Stack<BaseNode>();
            if (childStack == null)
                childStack = new Stack<BaseNode>();

            var notInputPorts = node.inputPorts.Where(x => !x.portData.displayName.Equals("Input"));
            foreach (var x in notInputPorts)
            {
                foreach (var edge in x.GetEdges())
                {
                    childStack.Push(edge.outputNode);
                    GetChildDataNodes(edge.outputNode, childStack);
                }
            }
            return childStack;
        }

        public BaseConditionNode GetNextNode(BaseConditionNode _node)
        {
            if (_node == null) return null;
            var nextNode = _node.GetExecutedNode();
            return nextNode == null ? null : nextNode;
        }

        public void ShutDownProcess()
        {
            try
            {
                mainCts?.Cancel();
                mainCts?.Dispose();
            }
            catch (ObjectDisposedException)
            {

            }
            finally
            {
                currentNodeChangeState = NodeChangeState.Cancel;
                CurrentNode = null;
            }
        }

        internal void NodeFastForward(BaseConditionNode _node)
        {
            if (!processedNodeStackGUID.Contains(_node.GUID))
            {
                processedNodeStackGUID.Push(_node.GUID);
                ChildDataNodesOnProcess(_node);
                _node.OnProcess();
            }
            ShowMessageDebug(Color.green, "NodeFastForward", _node.GetCustomName());
            _node.OnInit(InitCallbackState.Fastforward);
            _node.OnFastForWard();
        }

        internal void ProcessFastForward(BaseConditionNode fastStartnode = null)
        {
            BaseConditionNode tempNode = null;

            if (fastStartnode == null)
            {
                tempNode = StartNode.GetExecutedNode();
            }
            else
            {
                tempNode = GetNextNode(fastStartnode);
            }

            while (tempNode != null)
            {
                NodeFastForward(tempNode);
                tempNode = GetNextNode(tempNode);
            }
        }
        internal BaseConditionNode ProcessFastForwardToTargetNode(string targetNodeGUID)
        {
            BaseConditionNode tempNode = null;
            if (CurrentNode == null)
            {
                tempNode = startNode.GetExecutedNode();
            }
            else
            {
                tempNode = GetNextNode(CurrentNode);
            }

            while (tempNode != null && tempNode.GUID != targetNodeGUID)
            {

                NodeFastForward(tempNode);
                tempNode = GetNextNode(tempNode);
            }
            return tempNode;
        }
        internal void ProcessBackward()
        {
            foreach (var item in processedNodeStackGUID)
            {
                var node = FindNode(item);
                ShowMessageDebug(Color.magenta, "Back Node", node.GetCustomName());
            }
            if (processedNodeStackGUID != null && processedNodeStackGUID.Count > 0)
            {
                while (processedNodeStackGUID.Count > 0)
                {
                    var node = FindNode(processedNodeStackGUID.Pop());
                    BaseConditionNode backwardNode = node;
                    ShowMessageDebug(Color.green, "NodeBackard", backwardNode.GetCustomName());
                    backwardNode.OnInit(InitCallbackState.Backworad);
                    backwardNode.OnBackWard();
                }
            }
            else
            {
                ShowMessageDebug(Color.red, "ERROR", $"{processedNodeStackGUID} is Null or Zero");
            }
        }
        internal void AllEventClear()
        {
            if (OnChangeNode != null)
            {
                foreach (var _invoke in OnChangeNode?.GetInvocationList())
                {
                    OnChangeNode -= _invoke as OnChangeNodeDelegate;
                }
            }
            if (OnEndNode != null)
            {
                foreach (var _event in OnEndNode.GetInvocationList())
                {
                    OnEndNode -= (OnNodeEndDelegate)_event;
                }
            }
        }
        bool HasNullTarget(GameObject[] targets)
        {
            if (targets == null) return false;
            for (int i = 0; i < targets.Length; i++)
            {
                if (UnityEngine.Object.ReferenceEquals(targets[i], null))
                    return true;
            }
            return false;
        }

        public BaseConditionNode FindNode(string _nodeGUID)
        {
            if (string.IsNullOrEmpty(_nodeGUID))
                return null;

            return graph.nodes.Find(x => x.GUID.Equals(_nodeGUID)) as BaseConditionNode;
        }

        internal void ShowMessageDebug(Color color, string header, string message)
        {
#if UNITY_EDITOR
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            // 리치 텍스트 형식으로 변환
            string richText = $"<color=#{colorHex}>[ {header} ]</color> <color=white>{message} / {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}</color>";

            Debug.Log(richText);
#endif
        }

        public void InjectCurrentNode(BaseConditionNode _node)
        {
            CurrentNode = _node;
        }
        public void InjectProcessStack(Stack<BaseConditionNode> _processedNodeStack)
        {
            processedNodeStackGUID.Clear();
            processedNodeStackGUID = new Stack<string>(_processedNodeStack.Select(x => x.GUID));
        }
    }
}