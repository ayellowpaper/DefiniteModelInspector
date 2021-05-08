using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZeludeEditor
{
    public abstract class ManipulationTool
    {
        private static Dictionary<System.Type, ManipulationTool> _tools = new Dictionary<System.Type, ManipulationTool>();

        public static T Get<T>() where T : ManipulationTool, new()
        {
            var type = typeof(T);
            if (!_tools.ContainsKey(type))
                _tools.Add(type, new T());
            return _tools[type] as T;
        }

        public abstract void DoTool(Vector3 position, Quaternion rotation, IEnumerable<GameObject> targets);
    }
}