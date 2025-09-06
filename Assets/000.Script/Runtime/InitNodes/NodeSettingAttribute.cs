using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Roni.Graph.Node;
namespace Roni.Graph.Runtime.NodeInit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NodeSettingAttribute : Attribute
    {
        public Type BuilderType { get; }

        public NodeSettingAttribute(Type builderType)
        {
            if (!typeof(BaseConditionNode).IsAssignableFrom(builderType))
            {
                throw new ArgumentException($"Builder type must be a subclass of {typeof(BaseConditionNode)}", nameof(builderType));
            }
            BuilderType = builderType;
        }
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class NodeInitValidator
    {
        static NodeInitValidator()
        {
            ValidateNodeInitMethods();
        }

        private static void ValidateNodeInitMethods()
        {
            var methodsWithAttribute = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(method => method.GetCustomAttributes(typeof(NodeSettingAttribute), false).Length > 0);

            foreach (var method in methodsWithAttribute)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 2 || parameters[0].ParameterType != typeof(GameObject[]) || parameters[1].ParameterType != typeof(BaseConditionNode))
                {                    
                    Debug.LogError($"'{method.DeclaringType}'클래스의  '{method.Name}' 함수는 'GameObject[]'와 Node를 매게변수로 갖고있어야합니다.");
                }
            }
        }
    }
#endif
}