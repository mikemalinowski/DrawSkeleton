// MIT License
//
// Copyright (c) 2026 Michael Malinowski
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// We implement a custom inspector in for the draw skeleton behaviour. This
/// allows us to expose the ability for a user to re-cache the skeleton
/// </summary>
[CustomEditor(typeof(DrawSkeleton))]
public class DrawSkeletonEditor : Editor
{
    /// <summary>
    /// This is a list of properties which we know will require a recaching
    /// of data when they are changed.
    /// </summary>
    private List<string> propertiesRequiringCallbackRecaching = new List<string>
    {
        "boneSize",
        "setBoneSizeDynamically"
    };

    public override VisualElement CreateInspectorGUI()
    {
        // Access the DrawSkeleton component
        DrawSkeleton drawSkeleton = (DrawSkeleton)target;

        // Root container
        var root = new VisualElement();

        // Draw default inspector (UIElements version)
        InspectorElement.FillDefaultInspector(
            root,
            serializedObject,
            this
        );

        // Create the group containing the input properties
        Foldout inputsGroup = new Foldout();
        inputsGroup.text = "Visual Properties";
        inputsGroup.value = true;
        root.Add(inputsGroup);

        // Create a button and link it to the cache function
        // of the draw skeleton
        Button button = new Button { text = "Update The Skeleton" };
        button.clicked += drawSkeleton.CacheSkeleton;

        // Add the button to the visual element
        root.Add(button);

        // Hook up our callbacks for properties we know we need to
        // trigger re-caches for on change
        foreach (var field in root.Query<PropertyField>().ToList())
        {
            if (field.bindingPath == "m_Script")
            {
                continue;
            }
            if (propertiesRequiringCallbackRecaching.Contains(field.bindingPath))
            {
                field.RegisterValueChangeCallback(ReCacheSkeleton);
            }
            inputsGroup.Add(field);
        }
        return root;
    }

    /// <summary>
    /// This is specifically here to trigger a recache of the draw skeleton component
    /// it represents.
    /// </summary>
    /// <param name="property"></param>
    private void ReCacheSkeleton(SerializedPropertyChangeEvent property)
    {
        DrawSkeleton drawSkeleton = (DrawSkeleton)target;
        drawSkeleton.CacheSkeleton();

    }

    /// <summary>
    /// This is where we build scene (3d) ui elements. In this case we show each bone
    /// as a sphere - allowing them to be selectable.
    /// </summary>
    private void OnSceneGUI()
    {
        // Read our component
        DrawSkeleton skeleton = (DrawSkeleton)target;

        if (!skeleton.enabled)
        {
            return;
        }

        // Set the attribute which are common to all the handles
        // we will create
        Handles.color = skeleton.color;
        float handleSize = skeleton.GetDynamicBoneSize();

        // Now we cycle the bones and create a handle for each
        foreach (Transform bone in skeleton.Bones())
        {
            bool handleSelected = Handles.Button(
                bone.position,
                Quaternion.identity,
                handleSize,
                handleSize,
                Handles.SphereHandleCap
            );

            if (handleSelected)
            {
                Selection.activeGameObject = bone.gameObject;
            }
        }

    }
}
