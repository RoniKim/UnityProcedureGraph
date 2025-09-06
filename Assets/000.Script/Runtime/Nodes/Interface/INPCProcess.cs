using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Wintek.Graph.Node.Interface
{
    public interface INPCProcess
    {
        /// <summary>             
        /// NPC �Լ��� Node�� CancleȤ�� Endȣ�� �� ����                    
        /// </summary>     
        /// @details ���� MultiNode ���� -> ChildeNode ���� ->ChildeNode ���� -> MultiNode ���� -> NPCRun�Լ� ����
        /// <param name="disposeToken"></param>     
        /// <returns></returns>
        public UniTask NPCRun(CancellationToken disposeToken);

    }
}
