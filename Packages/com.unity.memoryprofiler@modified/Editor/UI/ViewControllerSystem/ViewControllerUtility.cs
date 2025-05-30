using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.MemoryProfilerExtension.Editor.UI
{
    static class ViewControllerUtility
    {
        // Loads the specified Uxml asset and returns its root VisualElement, discarding the Template container. If the Uxml specifies multiple roots, the first will be returned.
        public static VisualElement LoadVisualTreeFromUxml(string uxmlAssetGuid)
        {
            // Load Uxml template from disk.
            var uxml = UIElementsHelper.LoadAssetByGUID(uxmlAssetGuid);
            if (uxml == null)
                return null;

#if UNITY_2020_3_OR_NEWER
            var template = uxml.Instantiate();
#else
            var template = uxml.CloneTree();
#endif

            // Retrieve first child from template container.
            VisualElement view = null;
            using (var enumerator = template.Children().GetEnumerator())
            {
                if (enumerator.MoveNext())
                    view = enumerator.Current;
            }

            return view;
        }

        public static VisualTreeAsset LoadVisualTreeAssetFromUxml(string uxmlAssetGuid)
        {
            // Load Uxml template from disk.
            return UIElementsHelper.LoadAssetByGUID(uxmlAssetGuid);
        }

        // Instantiates the Uxml asset and returns its root VisualElement, discarding the Template container. If the Uxml specifies multiple roots, the first will be returned.
        public static VisualElement Instantiate(VisualTreeAsset uxml)
        {
#if UNITY_2020_3_OR_NEWER
            var template = uxml.Instantiate();
#else
            var template = uxml.CloneTree();
#endif

            // Retrieve first child from template container.
            VisualElement view = null;
            using (var enumerator = template.Children().GetEnumerator())
            {
                if (enumerator.MoveNext())
                    view = enumerator.Current;
            }

            return view;
        }
    }
}
