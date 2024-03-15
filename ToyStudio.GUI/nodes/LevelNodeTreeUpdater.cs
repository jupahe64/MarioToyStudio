using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ToyStudio.GUI.scene;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;

namespace ToyStudio.GUI.nodes
{
    internal class LevelNodeTreeUpdater(ObjectMapping<object, ILevelNode> nodeMapping,
        Scene<SubLevelSceneContext> scene)
    {
        public TNode UpdateOrCreateNodeFor<TNode>(object levelObj, Func<TNode> createFunc)
            where TNode : class, ILevelNode
        {
            bool justCreated = false;
            if (!nodeMapping.TryGetMappedObjFor(levelObj, out var mappedNode, out bool isDirty) ||
                mappedNode is not TNode node)
            {
                var sceneObject = createFunc.Invoke();
                node = sceneObject;
                isDirty = true;
                justCreated = true;
            }

            if (!isDirty)
                return node;

            bool isValid = true;
            node.Update(this, ref isValid);
            if (!isValid && !justCreated)
            {
                node = createFunc.Invoke();
                isValid = true;
                node.Update(this, ref isValid);
            }
            Debug.Assert(isValid);

            nodeMapping.SetMappingFor(levelObj, node);
            return node;
        }

        public bool TryGetSceneObjFor<TSceneObject>(object dataObject, out TSceneObject? sceneObject)
            where TSceneObject : class, ISceneObject<SubLevelSceneContext>
        {
            if (!scene.TryGetObjFor(dataObject, out ISceneObject<SubLevelSceneContext>? obj))
            {
                sceneObject = null;
                return false;
            }
            sceneObject = obj as TSceneObject;
            Debug.Assert(sceneObject != null);
            return sceneObject != null;
        }
    }
}
