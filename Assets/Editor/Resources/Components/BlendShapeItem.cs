using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Editor.Resources.Components
{
    public class BlendShapeItem : VisualElement
    {
        DropdownField _dropdown;

        public BlendShapeItem(List<string> blendShapeKeys, Action<string, string> onShapeKeySelected)
        {
            AddToClassList("blend-shape-item");

            // dropdown
            _dropdown = new DropdownField("Shape Key", blendShapeKeys, 0);
            _dropdown.RegisterValueChangedCallback(evt =>
            {
                onShapeKeySelected?.Invoke(evt.previousValue, evt.newValue);
            });
            Add(_dropdown);
        }

        public void SetSelectedIndex(int index)
        {
            _dropdown.index = index;
        }
    }
}