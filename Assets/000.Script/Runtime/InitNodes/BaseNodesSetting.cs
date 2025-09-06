
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Wintek.Graph.Node;

namespace Wintek.Graph.Runtime.NodeInit
{
    public abstract class BaseNodesSetting : MonoBehaviour
    {
        Dictionary<Type, List<MethodInfo>> methodsCache = new Dictionary<Type, List<MethodInfo>>();

        protected virtual void Awake()
        {
            SetMethodCaching();
        }

        protected void SetMethodCaching()
        {
            // 모든 메서드를 검색
            var methods = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                // 메서드의 모든 NodeInitAttribute를 찾기
                var attributes = method.GetCustomAttributes<NodeSettingAttribute>();
                foreach (var attribute in attributes)
                {
                    // builderType을 키로 사용하여 메서드를 캐시
                    if (!methodsCache.ContainsKey(attribute.BuilderType))
                    {
                        methodsCache[attribute.BuilderType] = new List<MethodInfo>();
                    }

                    methodsCache[attribute.BuilderType].Add(method);
                }
            }
        }
        public abstract void BasicInitNode(GameObject[] _targetObj, BaseConditionNode _node);

        public async UniTask NodeSetting(BaseConditionNode _targetNode)
        {
            if (methodsCache == null || methodsCache.Count == 0)
            {
                //3초간 데이터 확인 
                try
                {
                    await UniTask.WhenAny(
                UniTask.WaitUntil(() => methodsCache != null && methodsCache.Count > 0, cancellationToken: this.destroyCancellationToken),
                UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: this.destroyCancellationToken));
                }
                catch
                {
                    return;
                }
            }


            Type nodeType = _targetNode.GetType();


            _targetNode.Owner.ChildDataNodesOnProcess(_targetNode);
            _targetNode.OnProcess();


            // 노드 타입에 해당하는 메서드가 있는지 확인 후 호출
            if (methodsCache.TryGetValue(nodeType, out var methodInfos) && methodInfos.Count > 0)
            {
                foreach (var methodInfo in methodInfos)
                {
                    // 매개변수로 gameObjects를 전달하여 메서드 호출
                    methodInfo.Invoke(this, new object[] { _targetNode.GetTargets(), _targetNode });
                }
            }
            else
            {
                // 호출된 메서드가 없을 경우 기본 메서드 호출
                Debug.Log($"해당 노드[{nodeType} ']는 적절한 Method가 정의되어있지 않습니다.");
                //BasicInitNode(_targetNode.GetTargets());

            }
        }
        public async UniTask AllInitNodesProgress(Action<float> onProgress = null)
        {
            if (methodsCache == null || methodsCache.Count == 0)
            {
                //5초간 데이터 확인 
                try
                {                  
                    var waitTask = UniTask.WaitUntil(() => methodsCache != null && methodsCache.Count > 0, cancellationToken: this.destroyCancellationToken);
                    var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: this.destroyCancellationToken);

                    // 진행률 업데이트 타스크 (0.1초마다)
                    var progressTask = UniTask.Create(async () =>
                    {
                        var startTime = Time.time;
                        const float maxWaitTime = 5f;

                        while (!this.destroyCancellationToken.IsCancellationRequested)
                        {
                            var elapsed = Time.time - startTime;
                            var progress = Mathf.Clamp01(elapsed / maxWaitTime) * 0.3f; // 전체 진행률의 30%까지
                            onProgress?.Invoke(progress);

                            if (methodsCache != null && methodsCache.Count > 0)
                                break;

                            if (elapsed >= maxWaitTime)
                                break;

                            await UniTask.Delay(100, cancellationToken: this.destroyCancellationToken);
                        }
                    });

                    // 세 개의 태스크 중 하나라도 완료되면 진행
                    await UniTask.WhenAny(waitTask, timeoutTask, progressTask);
                }
                catch
                {
                    onProgress?.Invoke(0f);
                    return;
                }
            }

            // 모든 프로세서 노드 가져오기
            var allNodes = RuntimeProcessorRunner.Instance.GetAllProcessorNodes()
                .Where(item => item is BaseConditionNode)
                .Cast<BaseConditionNode>()
                .ToList();

            int totalNodes = allNodes.Count;
            int processedNodes = 0;

           
            onProgress?.Invoke(0.3f);

            // 캐시된 메서드를 사용하여 호출
            foreach (var node in allNodes)
            {
                Type nodeType = node.GetType();
                node.Owner.ChildDataNodesOnProcess(node);
                node.OnProcess();

                // 노드 타입에 해당하는 메서드가 있는지 확인 후 호출
                if (methodsCache.TryGetValue(nodeType, out var methodInfos) && methodInfos.Count > 0)
                {
                    foreach (var methodInfo in methodInfos)
                    {
                        // 매개변수로 gameObjects를 전달하여 메서드 호출
                        methodInfo.Invoke(this, new object[] { node.GetTargets(), node });
                    }
                }
                else
                {
                    // 호출된 메서드가 없을 경우 기본 메서드 호출
                    BasicInitNode(node.GetTargets(), node);
                }

                // 진행률 업데이트 (30% 이후부터 100%까지)
                processedNodes++;
                float nodeProgress = totalNodes > 0 ? (float)processedNodes / totalNodes : 1f;
                float totalProgress = 0.3f + (nodeProgress * 0.7f); // 30% + (노드 처리 진행률 * 70%)
                onProgress?.Invoke(totalProgress);
                              
                await UniTask.NextFrame();
            }

            // 완료 진행률 보고
            onProgress?.Invoke(1f);
        }
        public async UniTask AllInitNodes()
        {
            if (methodsCache == null || methodsCache.Count == 0)
            {
                //3초간 데이터 확인 
                try
                {
                    await UniTask.WhenAny(
                UniTask.WaitUntil(() => methodsCache != null && methodsCache.Count > 0, cancellationToken: this.destroyCancellationToken),
                UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: this.destroyCancellationToken));
                }
                catch
                {
                    return;
                }
            }

            // 캐시된 메서드를 사용하여 호출
            foreach (var item in RuntimeProcessorRunner.Instance.GetAllProcessorNodes())
            {
                if (item is BaseConditionNode node)
                {
                    Type nodeType = node.GetType();

                    node.Owner.ChildDataNodesOnProcess(node);

                    node.OnProcess();


                    // 노드 타입에 해당하는 메서드가 있는지 확인 후 호출
                    if (methodsCache.TryGetValue(nodeType, out var methodInfos) && methodInfos.Count > 0)
                    {

                        foreach (var methodInfo in methodInfos)
                        {
                            // 매개변수로 gameObjects를 전달하여 메서드 호출
                            methodInfo.Invoke(this, new object[] { node.GetTargets(), node });
                        }
                    }
                    else
                    {
                        // 호출된 메서드가 없을 경우 기본 메서드 호출
                        BasicInitNode(node.GetTargets(), node);
                    }
                }
            }

            await UniTask.NextFrame();
        }
    }
}
