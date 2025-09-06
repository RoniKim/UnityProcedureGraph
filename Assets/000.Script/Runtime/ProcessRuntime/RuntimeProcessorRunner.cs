using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using GraphProcessor;
using Unity.Linq;
using UnityEngine;
using Wintek.Graph.Node;
using Wintek.Graph.Serializable;

namespace Wintek.Graph.Runtime
{
    public class RuntimeProcessorRunner : MonoBehaviour 
    {
        private static RuntimeProcessorRunner _instance;
        public static RuntimeProcessorRunner Instance => _instance;

        private void Awake()
        {
            if (_instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
       
        }
    

        public delegate void OnChangeProcessorDelegate(KeyValuePair<string, RuntimeProcessor> prevProcessor, KeyValuePair<string, RuntimeProcessor> currentProcessor);
        /// <summary>
        /// 해당 이벤트 사용 시 prevProcessor, currentProcessor가 비어있는지 체크하려면 key가 empty인지 확인하세요.
        /// </summary>
        public OnChangeProcessorDelegate OnChangeProcessor;
        public delegate void OnChangeNodeTargetDelegate(GameObject[] prevTargets, GameObject[] currentTargets);
        public OnChangeNodeTargetDelegate OnChangeTargets;
        public delegate void OnChangeNodeDelegate(BaseConditionNode preNode, NodeChangeState preNodeState, BaseConditionNode currentNode);
        public OnChangeNodeDelegate OnChangeNode;

        public delegate void OnProcesFastForwardDelegate(LinkedListNode<RuntimeProcessor> currentProcess, LinkedListNode<RuntimeProcessor> targetProcess, string targetNodeGuid, string startNodeGuid);
        public OnProcesFastForwardDelegate OnProcesFastForward;
        public delegate void OnProcesBackwardDelegate(LinkedListNode<RuntimeProcessor> currentProcess, LinkedListNode<RuntimeProcessor> targetProcess);
        public OnProcesBackwardDelegate OnProcesBackward;

        public string GetCurrentProcessFileName => GetProcessorName(CurrentRuntimeProcess);

        private bool isSingleProcessAwait = false;
        public LinkedList<RuntimeProcessor> runtimeProcessors
        {
            get; private set;
        }
        private Dictionary<string, LinkedListNode<RuntimeProcessor>> processorCache;
        private LinkedListNode<RuntimeProcessor> currentRuntimeProcess;
        public LinkedListNode<RuntimeProcessor> CurrentRuntimeProcess
        {
            get { return currentRuntimeProcess; }
            private set
            {
                if (currentRuntimeProcess != value)
                {
                    KeyValuePair<string, RuntimeProcessor> prevProcessor = new KeyValuePair<string, RuntimeProcessor>(GetProcessorName(currentRuntimeProcess), currentRuntimeProcess?.Value);
                    KeyValuePair<string, RuntimeProcessor> currentProcessor = new KeyValuePair<string, RuntimeProcessor>(GetProcessorName(value), value?.Value);
                    OnChangeProcessor?.Invoke(prevProcessor, currentProcessor);
                    currentRuntimeProcess = value;
                }
            }
        }

        public bool isProcessing { get; private set; }
        private CancellationTokenSource mainCts;

        public void InitializeProcessors(string[] _paths, IWaitEndChecker waitEndChecker)
        {
            isProcessing = false;

            mainCts = new CancellationTokenSource();
            runtimeProcessors = new LinkedList<RuntimeProcessor>();
            processorCache = new Dictionary<string, LinkedListNode<RuntimeProcessor>>();

            foreach (var path in _paths)
            {
                var processGraph = GraphSerializable.ConvertJsonToGraph(path);
                var processor = new RuntimeProcessor(processGraph);
                processor.InjectWaitEndCheker(waitEndChecker);
                processor.OnChangeNode += GetNodeChange;
                var node = runtimeProcessors.AddLast(processor);
                processorCache[Path.GetFileNameWithoutExtension(path)] = node;
            }
        }
        public void InitializeProcessors(string[] _paths)
        {
            isProcessing = false;

            mainCts = new CancellationTokenSource();
            runtimeProcessors = new LinkedList<RuntimeProcessor>();
            processorCache = new Dictionary<string, LinkedListNode<RuntimeProcessor>>();

            foreach (var path in _paths)
            {
                var processGraph = GraphSerializable.ConvertJsonToGraph(path);
                var processor = new RuntimeProcessor(processGraph);
                processor.OnChangeNode += GetNodeChange;
                var node = runtimeProcessors.AddLast(processor);
                processorCache[Path.GetFileNameWithoutExtension(path)] = node;
            }
        }
        public void InitializeProcessors(Dictionary<string,string> jsons)
        {
            isProcessing = false;

            mainCts = new CancellationTokenSource();
            runtimeProcessors = new LinkedList<RuntimeProcessor>();
            processorCache = new Dictionary<string, LinkedListNode<RuntimeProcessor>>();

            foreach (var path in jsons)
            {
                var processGraph = GraphSerializable.ConvertJsonTextToGraph(path.Value);
                var processor = new RuntimeProcessor(processGraph);
                processor.OnChangeNode += GetNodeChange;
                var node = runtimeProcessors.AddLast(processor);
                processorCache[path.Key] = node;
            }
        }
        public List<BaseNode> GetAllProcessorNodes()
        {
            List<BaseNode> nodes = new List<BaseNode>();
            for (var processor = runtimeProcessors.First; processor != null; processor = processor.Next)
            {
                nodes.AddRange(processor.Value.graph.nodes.Where(x => x is BaseConditionNode));
            }
            return nodes;
        }

        public List<string> GetTargetGameObjectPathList()
        {
            List<string> targetPaths = new List<string>();

            foreach (var node in GetAllProcessorNodes())
            {
                if(node is BaseConditionNode conditionNode)
                {
                    node.OnProcess();

                    var targets = conditionNode.GetTargets();
                    if(targets!= null)
                    {
                        foreach (var target in targets)
                        {
                            targetPaths.Add(GetGameObjectPath(target));
                        }
                    }
                }
              
            }
            return targetPaths;
        }
        public string GetGameObjectPath(GameObject _obj)
        {
            if (_obj == null)
                return string.Empty;

            string returnValue = string.Empty;
            var parents = _obj.AncestorsAndSelf().ToList();
            for (int i = parents.Count - 1; i > -1; i--)
            {
                returnValue += parents[i].name;
                if (i != 0)
                    returnValue += "/";
            }
            return returnValue;
        }
        //public List<string> GetTargetGameObjectDataPathList()
        //{
        //    List<string> targetPaths = new List<string>();

        //    foreach (var node in GetAllProcessorNodes())
        //    {
        //        // 리플렉션을 사용하여 GameObjectData의 targetPath 가져오기
        //        Type nodeType = node.GetType();

        //        // GameObjectData 타입의 필드 찾기
        //        FieldInfo gameObjectDataField = nodeType.GetFields(
        //            BindingFlags.Public |
        //            BindingFlags.NonPublic |
        //            BindingFlags.Instance)
        //            .FirstOrDefault(field =>
        //                field.FieldType == typeof(GameObjectData) ||
        //                field.FieldType.IsSubclassOf(typeof(GameObjectData)));

        //        if (gameObjectDataField != null)
        //        {
        //            // 해당 필드의 값 가져오기
        //            object gameObjectData = gameObjectDataField.GetValue(node);

        //            if (gameObjectData != null)
        //            {
        //                // targetPath 필드 가져오기
        //                FieldInfo targetPathField = gameObjectData.GetType().GetField("targetPath");

        //                if (targetPathField != null)
        //                {
        //                    // targetPath 값 추가
        //                    string targetPath = (string)targetPathField.GetValue(gameObjectData);
        //                    if (!string.IsNullOrEmpty(targetPath))
        //                    {
        //                        targetPaths.Add(targetPath);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return targetPaths;
        //}
        private void GetNodeChange(BaseConditionNode preNode, NodeChangeState preNodeState, BaseConditionNode currentNode)
        {
            OnChangeTargets?.Invoke(preNode?.GetTargets(), currentNode?.GetTargets());
            OnChangeNode?.Invoke(preNode, preNodeState, currentNode);
        }

        public void OnDispose()
        {
            ShutdownAllProcessors();
            if (mainCts != null && !mainCts.IsCancellationRequested)
            {
                mainCts?.Cancel();
                mainCts?.Dispose();
            }
            AllEventDispoe();
            if (runtimeProcessors != null)
            {
                foreach (var processor in runtimeProcessors)
                {
                    processor.AllEventClear();

                }
            }
        }

        private void OnApplicationQuit()
        {
            ShutdownAllProcessors();
        }

        private void OnDestroy()
        {
            if (mainCts != null && !mainCts.IsCancellationRequested)
            {
                mainCts?.Cancel();
                mainCts?.Dispose();
            }
            AllEventDispoe();
            if (runtimeProcessors != null)
            {
                foreach (var processor in runtimeProcessors)
                {
                    processor.AllEventClear();
                }
            }
        }
        private void AllEventDispoe()
        {
            if (OnChangeProcessor != null)
                foreach (var _invoke in OnChangeProcessor?.GetInvocationList())
                {
                    OnChangeProcessor -= _invoke as OnChangeProcessorDelegate;
                }
            if (OnChangeTargets != null)
                foreach (var _invoke in OnChangeTargets?.GetInvocationList())
                {
                    OnChangeTargets -= _invoke as OnChangeNodeTargetDelegate;
                }
            if (OnChangeNode != null)
                foreach (var _invoke in OnChangeNode?.GetInvocationList())
                {
                    OnChangeNode -= _invoke as OnChangeNodeDelegate;
                }
            if (OnProcesFastForward != null)
                foreach (var _invoke in OnProcesFastForward?.GetInvocationList())
                {
                    OnProcesFastForward -= _invoke as OnProcesFastForwardDelegate;
                }
            if (OnProcesBackward != null)
                foreach (var _invoke in OnProcesBackward?.GetInvocationList())
                {
                    OnProcesBackward -= _invoke as OnProcesBackwardDelegate;
                }
        }
        private void ShutdownCurrentProcessor()
        {
            CurrentRuntimeProcess?.Value.ShutDownProcess();
        }

        private void ShutdownAllProcessors()
        {
            if (runtimeProcessors != null)
            {
                foreach (var processor in runtimeProcessors)
                {
                    processor.ShutDownProcess();
                }
            }

        }

        public async UniTask RunSingleProcess(LinkedListNode<RuntimeProcessor> startProcessor = null)
        {
            if (runtimeProcessors == null || runtimeProcessors.Count == 0)
            {
                Debug.LogError($"InitializeProcessors()를 먼저 호출해주세요. {runtimeProcessors} is null");
                return;
            }
            if (isProcessing && !isSingleProcessAwait)
            {
                isSingleProcessAwait = true;
                await UniTask.WaitUntil(() => !isProcessing);
                isSingleProcessAwait = false;
            }
            else if (isProcessing && isSingleProcessAwait)
                return;

            isProcessing = true;
            startProcessor ??= runtimeProcessors.First;
            CurrentRuntimeProcess = startProcessor;
            try
            {
                ShowMessageDebug(Color.cyan, "Running Single processor", GetProcessorName(CurrentRuntimeProcess));
                await CurrentRuntimeProcess.Value.Run();
            }
            catch (OperationCanceledException)
            {
                //if (CurrentRuntimeProcess.Value != null)
                //{
                //    var node = CurrentRuntimeProcess.Value.GetNextNode(CurrentRuntimeProcess.Value.PreNode);
                //    while (node != null)
                //    {
                //        if (node is BaseMultipleConditionNode multiNode)
                //        {
                //            foreach (var item in multiNode.GetCombinNodes())
                //            {
                //                item.OnEnd();
                //            }
                //        }
                //        else
                //        {
                //            node.OnEnd();
                //        }
                //        node = CurrentRuntimeProcess.Value.GetNextNode(node);
                //    }
                //}
                //Debug.Log($"CurrentNODE {CurrentRuntimeProcess.Value.PreNode}");
                //Debug.Log($"Process execution cancelled. {currentRuntimeProcess.Value.PreNode}");
            }
            finally
            {
                isProcessing = false;
            }
        }

 
        public async UniTask RunProcess(LinkedListNode<RuntimeProcessor> startProcessor = null, BaseConditionNode startNode = null)
        {
            if (runtimeProcessors == null || runtimeProcessors.Count == 0)
            {
                Debug.LogError($"InitializeProcessors()를 먼저 호출해주세요. {runtimeProcessors} is null");
                return;
            }
            if (isProcessing) return;

            isProcessing = true;

            await UniTask.NextFrame();
            await UniTask.NextFrame();
            await UniTask.NextFrame();

            startProcessor ??= runtimeProcessors.First;

            try
            {
                for (CurrentRuntimeProcess = startProcessor;
                     CurrentRuntimeProcess != null && !mainCts.IsCancellationRequested;
                     CurrentRuntimeProcess = CurrentRuntimeProcess.Next)
                {

                    ShowMessageDebug(Color.cyan, "Running processor", GetProcessorName(CurrentRuntimeProcess));
                    await CurrentRuntimeProcess.Value.Run(startNode);
                    startNode = null;
                    await UniTask.NextFrame();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Process execution cancelled.");
            }
            finally
            {
                isProcessing = false;
            }

        }



        #region Processor Controll   
        private void ProcessControllBasic(LinkedListNode<RuntimeProcessor> targetProcess, bool isAutoRun = true)
        {
            if (targetProcess == null) return;

            if (CurrentRuntimeProcess == null)
            {
                CurrentRuntimeProcess = runtimeProcessors.Last;
            }
            if (IsNodeAfter(CurrentRuntimeProcess, targetProcess))
            {
                FastForWardProcess(targetProcess, isAutoRun).Forget();
            }
            else
                BackWardNodeProcess(targetProcess, isAutoRun).Forget();
        }
        public async UniTask NextProcess()
        {
            if (CurrentRuntimeProcess.Value == null || CurrentRuntimeProcess.Next == null) return;
            var nextProcessor = CurrentRuntimeProcess.Next;
            await FastForWardProcess(nextProcessor, false);
            await RunSingleProcess(nextProcessor);
        }
    
        public void ProcessorControll(int _Index)
        {
            var findProcess = FindProcessor(_Index);
            ProcessControllBasic(findProcess);
        }
        public void ProcessorControll(string _key)
        {
            if (GetCurrentProcessorName().Equals(_key))
                return;
            var findProcess = FindProcessor(_key);
            ProcessControllBasic(findProcess);
        }
        private async UniTask FastForWardProcess(LinkedListNode<RuntimeProcessor> targetProcess, bool isAutoRun = true)
        {
            if (targetProcess == null || CurrentRuntimeProcess == null) return;

            BaseConditionNode startNode = CurrentRuntimeProcess.Value.CurrentNode;

            ShutdownCurrentProcessor();
            await UniTask.WaitUntil(() => !isProcessing && CurrentRuntimeProcess.Value.CurrentNode == null);
            await UniTask.NextFrame();


            for (var processor = CurrentRuntimeProcess; processor != null && processor != targetProcess; processor = processor.Next)
            {
                ShowMessageDebug(Color.green, "Fast forwarding", GetProcessorName(processor));

                if (processor == CurrentRuntimeProcess)
                    processor.Value.ProcessFastForward(startNode);
                else
                    processor.Value.ProcessFastForward();
            }
            OnProcesFastForward?.Invoke(CurrentRuntimeProcess, targetProcess, string.Empty, startNode != null ? startNode.GUID : string.Empty);

            CurrentRuntimeProcess = targetProcess;
            if (isAutoRun)
                RunProcess(CurrentRuntimeProcess).Forget();
        }
        private async UniTask FastForWardProcess(LinkedListNode<RuntimeProcessor> targetProcessNode, string targetNodeGUID, bool isAutoRun = true)
        {
            if (targetProcessNode == null || CurrentRuntimeProcess == null) return;
            BaseConditionNode startNode = CurrentRuntimeProcess.Value.CurrentNode;
            ShutdownCurrentProcessor();
            await UniTask.WaitUntil(() => !isProcessing && CurrentRuntimeProcess.Value.CurrentNode == null);
            await UniTask.NextFrame();
            BaseConditionNode nextStartNode = null;
            for (var processor = CurrentRuntimeProcess; processor != null && processor != targetProcessNode.Next; processor = processor.Next)
            {
                ShowMessageDebug(Color.green, "Fast forwarding", GetProcessorName(processor));
                nextStartNode = processor.Value.ProcessFastForwardToTargetNode(targetNodeGUID);
                //await UniTask.NextFrame();
            }
            OnProcesFastForward?.Invoke(CurrentRuntimeProcess, targetProcessNode, targetNodeGUID, startNode != null ? startNode.GUID : targetNodeGUID);

            CurrentRuntimeProcess = targetProcessNode;
            if (isAutoRun)
            {
                if (nextStartNode.GUID.Equals(targetNodeGUID))
                {
                    RunProcess(CurrentRuntimeProcess, nextStartNode).Forget();
                }
                else
                    RunProcess(CurrentRuntimeProcess).Forget();
            }


        }
        public void CurrentToTargetFastForward(int _targetindex, bool isAutoRun = true)
        {
            var findNode = FindProcessor(_targetindex);
            FastForWardProcess(findNode, isAutoRun).Forget();
        }

        public void CurrentToTargetFastForward(string _targetkey, bool isAutoRun = true)
        {
            var findProcess = FindProcessor(_targetkey);
            FastForWardProcess(findProcess, isAutoRun).Forget();
        }
        public void CurrentToTargetFastForward(int _targetindex, string targetNnodeGUID, bool isAutoRun = true)
        {
            var findNode = FindProcessor(_targetindex);
            FastForWardProcess(findNode, targetNnodeGUID, isAutoRun).Forget();
        }

        public void CurrentToTargetFastForward(string _targetkey, string targetNnodeGUID, bool isAutoRun = true)
        {
            var findProcess = FindProcessor(_targetkey);
            FastForWardProcess(findProcess, targetNnodeGUID, isAutoRun).Forget();
        }
        private async UniTask BackWardNodeProcess(LinkedListNode<RuntimeProcessor> targetNode, bool isAutoRun = true)
        {
            if (targetNode == null || CurrentRuntimeProcess == null || IsNodeAfter(CurrentRuntimeProcess, targetNode)) return;

            ShutdownCurrentProcessor();
            await UniTask.WaitUntil(() => !isProcessing && CurrentRuntimeProcess.Value.CurrentNode == null);
            await UniTask.NextFrame();

            OnProcesBackward?.Invoke(CurrentRuntimeProcess, targetNode);
            for (var processor = CurrentRuntimeProcess; processor != null && processor != targetNode.Previous; processor = processor.Previous)
            {
                ShowMessageDebug(Color.green, "Moving backward", GetProcessorName(processor));
                processor.Value.ProcessBackward();
                //await UniTask.NextFrame();
            }


            CurrentRuntimeProcess = targetNode;
            if (isAutoRun)
            {
                RunProcess(CurrentRuntimeProcess).Forget();
            }
        }

        #endregion
        #region Finder
        /// <summary>
        /// Process의 Name으로 노드를 찾습니다
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LinkedListNode<RuntimeProcessor> FindProcessor(string key)
        {
            return processorCache.TryGetValue(key, out var processor) ? processor : null;
        }
        /// <summary>
        /// Index를 기준으로 노드를 찾습니다 속도가 느립니다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public LinkedListNode<RuntimeProcessor> FindProcessor(int index)
        {
            if (index < 0 || processorCache.Count - 1 < index)
                return null;

            return processorCache.ElementAt(index).Value;
        }
        public LinkedListNode<RuntimeProcessor> FindProcessor(RuntimeProcessor _processor)
        {
            var findprocessor = processorCache.Values.Where(x => x.Value.Equals(_processor)).FirstOrDefault();
            return findprocessor;
            //return processorCache.TryGetValue(key, out var processor) ? processor : null;
        }
        public int GetCurrentProcessorIndex()
        {
            if (processorCache == null) return 0;
            return processorCache.Keys.ToList().IndexOf(GetCurrentProcessFileName);
        }
        #endregion
        #region Utility
        public void InjectCurrentProcessor(LinkedListNode<RuntimeProcessor> processor)
        {
            CurrentRuntimeProcess = processor;
        }
        public string GetCurrentProcessorName()
        {
            return processorCache.FirstOrDefault(x => x.Value == CurrentRuntimeProcess).Key ?? string.Empty;
        }
        public string GetProcessorName(LinkedListNode<RuntimeProcessor> processor)
        {
            return processorCache.FirstOrDefault(x => x.Value == processor).Key ?? string.Empty;
        }
        public string GetProcessorName(RuntimeProcessor processor)
        {
            return processorCache.FirstOrDefault(x => x.Value.Value == processor).Key ?? string.Empty;
        }
        /// <summary>
        /// 두 노드를 비교하여 TargetNode가 더 뒤에있는지 확인한다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="standardNode">기준이 되는 노드</param>
        /// <param name="targetNode">뒤에 있는지 확인할 노드</param>
        /// <returns></returns>
        internal bool IsNodeAfter<T>(LinkedListNode<T> standardNode, LinkedListNode<T> targetNode)
        {
            if (standardNode == targetNode) return false;

            for (var current = standardNode.Next; current != null; current = current.Next)
            {
                if (current == targetNode) return true;
            }

            return false;
        }
        internal void ShowMessageDebug(Color color, string header, string message)
        {
#if UNITY_EDITOR
            string colorHex = UnityEngine.ColorUtility.ToHtmlStringRGB(color);
            // 리치 텍스트 형식으로 변환
            string richText = $"<color=#{colorHex}>[ {header} ]</color> <color=white>{message}</color>";

            Debug.Log(richText);
#endif
        }
        #endregion
    }
}
