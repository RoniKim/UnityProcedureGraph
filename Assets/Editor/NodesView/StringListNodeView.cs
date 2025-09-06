using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using GraphProcessor;
using UnityEditorInternal;
using Roni.Graph.Node;

namespace Roni.Graph.Editor.NodeView
{
    [NodeCustomEditor(typeof(Roni.Graph.Node.StringListNode))]
    public class StringListNodeView : BaseNodeView
    {
        ReorderableList list;

        public override void Enable()
        {
            // base.Enable();
            controlsContainer.Add(new IMGUIContainer(DrawArray));
            var array = nodeTarget as Roni.Graph.Node.StringListNode;
            list = new ReorderableList(array.stringDatas, typeof(string));
            style.width = 400;
            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Texts");
            };
            list.onAddCallback += AddCallBack;
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                // 현재 값을 저장
                string oldValue = array.stringDatas[index];

                // ObjectField를 그리고 새 값을 받아옴                
                string newValue = EditorGUI.TextField(rect, oldValue);

                // 값이 변경되었는지 확인
                if (newValue != oldValue)
                {
                    // 값 업데이트                    
                    array.stringDatas[index] = newValue;
                }

            };
        }

        private void AddCallBack(ReorderableList list)
        {
            (nodeTarget as StringListNode).stringDatas.Add(string.Empty);
        }

        void DrawArray()
        {
            list.DoLayoutList();
        }
    }

}