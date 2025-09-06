
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
            // ��� �޼��带 �˻�
            var methods = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                // �޼����� ��� NodeInitAttribute�� ã��
                var attributes = method.GetCustomAttributes<NodeSettingAttribute>();
                foreach (var attribute in attributes)
                {
                    // builderType�� Ű�� ����Ͽ� �޼��带 ĳ��
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
                //3�ʰ� ������ Ȯ�� 
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


            // ��� Ÿ�Կ� �ش��ϴ� �޼��尡 �ִ��� Ȯ�� �� ȣ��
            if (methodsCache.TryGetValue(nodeType, out var methodInfos) && methodInfos.Count > 0)
            {
                foreach (var methodInfo in methodInfos)
                {
                    // �Ű������� gameObjects�� �����Ͽ� �޼��� ȣ��
                    methodInfo.Invoke(this, new object[] { _targetNode.GetTargets(), _targetNode });
                }
            }
            else
            {
                // ȣ��� �޼��尡 ���� ��� �⺻ �޼��� ȣ��
                Debug.Log($"�ش� ���[{nodeType} ']�� ������ Method�� ���ǵǾ����� �ʽ��ϴ�.");
                //BasicInitNode(_targetNode.GetTargets());

            }
        }
        public async UniTask AllInitNodesProgress(Action<float> onProgress = null)
        {
            if (methodsCache == null || methodsCache.Count == 0)
            {
                //5�ʰ� ������ Ȯ�� 
                try
                {                  
                    var waitTask = UniTask.WaitUntil(() => methodsCache != null && methodsCache.Count > 0, cancellationToken: this.destroyCancellationToken);
                    var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: this.destroyCancellationToken);

                    // ����� ������Ʈ Ÿ��ũ (0.1�ʸ���)
                    var progressTask = UniTask.Create(async () =>
                    {
                        var startTime = Time.time;
                        const float maxWaitTime = 5f;

                        while (!this.destroyCancellationToken.IsCancellationRequested)
                        {
                            var elapsed = Time.time - startTime;
                            var progress = Mathf.Clamp01(elapsed / maxWaitTime) * 0.3f; // ��ü ������� 30%����
                            onProgress?.Invoke(progress);

                            if (methodsCache != null && methodsCache.Count > 0)
                                break;

                            if (elapsed >= maxWaitTime)
                                break;

                            await UniTask.Delay(100, cancellationToken: this.destroyCancellationToken);
                        }
                    });

                    // �� ���� �½�ũ �� �ϳ��� �Ϸ�Ǹ� ����
                    await UniTask.WhenAny(waitTask, timeoutTask, progressTask);
                }
                catch
                {
                    onProgress?.Invoke(0f);
                    return;
                }
            }

            // ��� ���μ��� ��� ��������
            var allNodes = RuntimeProcessorRunner.Instance.GetAllProcessorNodes()
                .Where(item => item is BaseConditionNode)
                .Cast<BaseConditionNode>()
                .ToList();

            int totalNodes = allNodes.Count;
            int processedNodes = 0;

           
            onProgress?.Invoke(0.3f);

            // ĳ�õ� �޼��带 ����Ͽ� ȣ��
            foreach (var node in allNodes)
            {
                Type nodeType = node.GetType();
                node.Owner.ChildDataNodesOnProcess(node);
                node.OnProcess();

                // ��� Ÿ�Կ� �ش��ϴ� �޼��尡 �ִ��� Ȯ�� �� ȣ��
                if (methodsCache.TryGetValue(nodeType, out var methodInfos) && methodInfos.Count > 0)
                {
                    foreach (var methodInfo in methodInfos)
                    {
                        // �Ű������� gameObjects�� �����Ͽ� �޼��� ȣ��
                        methodInfo.Invoke(this, new object[] { node.GetTargets(), node });
                    }
                }
                else
                {
                    // ȣ��� �޼��尡 ���� ��� �⺻ �޼��� ȣ��
                    BasicInitNode(node.GetTargets(), node);
                }

                // ����� ������Ʈ (30% ���ĺ��� 100%����)
                processedNodes++;
                float nodeProgress = totalNodes > 0 ? (float)processedNodes / totalNodes : 1f;
                float totalProgress = 0.3f + (nodeProgress * 0.7f); // 30% + (��� ó�� ����� * 70%)
                onProgress?.Invoke(totalProgress);
                              
                await UniTask.NextFrame();
            }

            // �Ϸ� ����� ����
            onProgress?.Invoke(1f);
        }
        public async UniTask AllInitNodes()
        {
            if (methodsCache == null || methodsCache.Count == 0)
            {
                //3�ʰ� ������ Ȯ�� 
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

            // ĳ�õ� �޼��带 ����Ͽ� ȣ��
            foreach (var item in RuntimeProcessorRunner.Instance.GetAllProcessorNodes())
            {
                if (item is BaseConditionNode node)
                {
                    Type nodeType = node.GetType();

                    node.Owner.ChildDataNodesOnProcess(node);

                    node.OnProcess();


                    // ��� Ÿ�Կ� �ش��ϴ� �޼��尡 �ִ��� Ȯ�� �� ȣ��
                    if (methodsCache.TryGetValue(nodeType, out var methodInfos) && methodInfos.Count > 0)
                    {

                        foreach (var methodInfo in methodInfos)
                        {
                            // �Ű������� gameObjects�� �����Ͽ� �޼��� ȣ��
                            methodInfo.Invoke(this, new object[] { node.GetTargets(), node });
                        }
                    }
                    else
                    {
                        // ȣ��� �޼��尡 ���� ��� �⺻ �޼��� ȣ��
                        BasicInitNode(node.GetTargets(), node);
                    }
                }
            }

            await UniTask.NextFrame();
        }
    }
}
