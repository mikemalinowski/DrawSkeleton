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
using System.Collections.Generic;
using UnityEditor;

public class DrawSkeleton : MonoBehaviour
{
    // These are variables we cache and dont expose
    private List<Transform> bones = new List<Transform>();
    private Mesh boneMesh;
    private float dynamicBoneSize = 1.0f;
    
    // These are user facing variables which will show in the inspector
    public bool drawBones = true;
    public bool drawAxis = false;    
    public float boneSize = 0.0f;
    public bool setBoneSizeDynamically = true;
    public Color color = Color.greenYellow;

    /// <summary>
    /// When the component is reset we request a complete reinitialisation
    /// of the component.
    /// </summary>
    void Reset()
    {
        CacheSkeleton();
    }
    
    /// <summary>
    /// When the component is first initialised we trigger a full cache
    /// of all teh required details
    /// </summary>
    public void Awake()
    {
        CacheSkeleton();
    }

    /// <summary>
    /// When requested to draw gizmo's we cycle through the cached bones and
    /// providing the bone has a valid parent we draw it and the axis if
    /// requested.
    /// </summary>
    void OnDrawGizmos()
    {
        foreach (Transform bone in bones)
        {
            if (bone.parent && drawBones)
            {
                DrawBone(bone, bone.parent, Selection.Contains(bone.parent.gameObject));
            }
            if (drawAxis)
            {
                DrawAxis(bone);
            }    
        }
    }

    /// <summary>
    /// This function will re-evaluate the skeleton and recache all
    /// the values which go into drawing the bones in an optimal way
    /// </summary>
    public void CacheSkeleton()
    {
        CacheBones();
        CalculateBoneSize();
        
        // Note that we do not automatically rebuild the bone
        // mesh, as this will never change. Therefore we only 
        // rebuild it if it has not yet been built.
        if (!boneMesh)
        {
            boneMesh = ConstructBoneMesh();
        }

    }
    
    /// <summary>
    /// To make the usage of this component as simple as possible we dont
    /// want to have the user need to list all the bones. Instead we look
    /// at all the children for skinned meshes and take the bone lists from
    /// them.
    /// Because this is a heavy operation is definitely not something we
    /// want to do all the time - therefore we cache the bone list.
    /// </summary>
    private void CacheBones()
    {
        bones = new List<Transform>();
        
        SkinnedMeshRenderer[] skinnedMeshComponents = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: false);
        
        
        foreach (SkinnedMeshRenderer mesh in skinnedMeshComponents)
        {
            foreach (Transform bone in mesh.bones)
            {
                bones.Add(bone);
            }
        }
    }
    
    /// <summary>
    /// This function specifically manages the drawing of the axis of a
    /// transform. This allows a user to see exactly what the axis looks
    /// like.
    /// </summary>
    /// <param name="bone"></param>
    private void DrawAxis(Transform bone)
    {
        
        Vector3 position = bone.position;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + bone.right * dynamicBoneSize);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(position, position + bone.up * dynamicBoneSize);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(position, position + bone.forward * dynamicBoneSize);
    }

    /// <summary>
    /// This function draws our custom bone mesh - this helps visualise the
    /// hierarchy of a skeleton.
    /// </summary>
    /// <param name="bone"></param>
    /// <param name="parent"></param>
    private void DrawBone(Transform bone, Transform parent, bool highlight)
    {
        // If we're selected we always show as white otherwise
        // set the colour to the user defined colour
        bool isSelected = Selection.activeTransform == parent;
        Gizmos.color = highlight ? Color.white : color;
        
        // Get the bone and the parent positions
        Vector3 bonePosition = bone.position;
        Vector3 parentPosition = parent.position;
        
        // Determine the length of this bone
        float boneLength = (parentPosition -  bonePosition).magnitude;
        
        // Create a scale vector. Our width is the bone size either set
        // by the user or defined by the bounds of the skinned mesh. The
        // length is then simply the length between the bone and the
        // parent.
        Vector3 scale = new Vector3(
            dynamicBoneSize, 
            dynamicBoneSize, 
            boneLength
        );
        
        // Calculate the rotation of the bone by taking the look direction
        // between the two positions
        Vector3 direction = bonePosition - parentPosition;
		if (direction == Vector3.zero)
		{
			direction.z = 1.0f;
		}
        Quaternion rotation = Quaternion.LookRotation(direction);
		
        
        // Finally we draw the mesh
        Gizmos.DrawMesh(
            boneMesh, 
            parentPosition, 
            rotation, 
            scale
        );
    }
    
    /// <summary>
    /// Here we determine the size our bones should be. If a user has disabled
    /// the dynamic bone size option then we simply apply the bone size they
    /// request.
    /// If they have dynamic size on then we read the overall bounds of all the
    /// bones we're going to draw and take the size as being 1% of the bounds
    /// size.
    /// </summary>
    private void CalculateBoneSize()
    {
        // If the user has turned off dynamic bone size then we do not have
        // to calculate anything. Instead we just take the value they have 
        // given us.
        if (!setBoneSizeDynamically)
        {
            dynamicBoneSize = boneSize;
            return;
        }
        
        // If there are no bones to draw we just set a base value of 1. But
        // note that even though we have set it to one, nothing will actually
        // draw because there are no bones.
        if (bones == null || bones.Count == 0)
        {
            dynamicBoneSize = 1.0f;
            return;
        }
        
        // Instance a bounds object and start adding all the bone positions
        // into it
        Bounds bounds = new Bounds(bones[0].position, Vector3.zero);
        for (int i = 1; i < bones.Count; i++)
        {
            bounds.Encapsulate(bones[i].position);
        }
        
        // Finally we calculate the bone size as being 1% of the bounds
        // size. 
        dynamicBoneSize = bounds.size.magnitude * 0.01f;
    }
    
    /// <summary>
    /// This function will generate a mesh which we will use to draw
    /// a bone. 
    /// </summary>
    /// <returns></returns>
    private static Mesh ConstructBoneMesh()
    {
        // Instance a new mesh object
        Mesh mesh = new Mesh(); 
        
        // Define our vertex positions
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0.2f), // 0
            new Vector3(0.5f, -0.5f, 0.2f),  // 1
            new Vector3(-0.5f, 0.5f, 0.2f),  // 2
            new Vector3(0.5f, 0.5f, 0.2f),   // 3
            new Vector3(0.0f, 0.0f, 0.0f),   // 4
            new Vector3(0.0f, 0.0f, 1.0f),   // 5
        };

        // Now we give our triangle list, where each set of three
        // indices represent a triangle and each indice is a the 
        // index of the position in the vertices list.
        int[] triangles = new int[]
        {
            2, 3, 4,
            4, 1, 0,
            1, 4, 3,
            4, 0, 2,
            0, 1, 5,
            1, 3, 5,
            3, 2, 5,
            2, 0, 5,
        };
        
        // Apply our two lists
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        // Ask the mesh to update its normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
