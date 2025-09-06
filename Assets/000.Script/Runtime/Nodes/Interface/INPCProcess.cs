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
        /// NPC 함수는 Node의 Cancle혹은 End호출 후 종료                    
        /// </summary>     
        /// @details 순서 MultiNode 시작 -> ChildeNode 시작 ->ChildeNode 종료 -> MultiNode 종료 -> NPCRun함수 종료
        /// <param name="disposeToken"></param>     
        /// <returns></returns>
        public UniTask NPCRun(CancellationToken disposeToken);

    }
}
