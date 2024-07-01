using System;
using Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Resources.Components
{
    public class ItemButton: Button
    {
        public ItemButton(ItemInfo item, Action onClick)
        {
            AddToClassList("item-button");
            var preview = new Image { image = item.preview, scaleMode = ScaleMode.ScaleToFit, style = { width = 64, height = 64 } };
            Add(preview);

            var labelContainer = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.FlexStart,
                    justifyContent = Justify.Center,
                }
            };

            var label = new Label(item.modelName)
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            labelContainer.Add(label);

            // vrc sdk check
            // UnityEditor.EditorApplication.delayCall += async () =>
            // {
            //     try
            //     {
            //         var a
            //     }
            //     catch (Exception e)
            //     {
            //         Debug.LogError(e);
            //     }
            // };

            Add(labelContainer);


            clicked += onClick;
            clicked += () =>
            {
                EnableInClassList("selected", true);
                foreach (var child in parent.Children())
                {
                    if (child != this)
                    {
                        child.EnableInClassList("selected", false);
                    }
                }
            };
        }
    }
}