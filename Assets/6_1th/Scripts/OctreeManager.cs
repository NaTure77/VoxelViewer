using Dummiesman;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityMeshImporter;

namespace Generation_6_1
{
    public class OctreeManager
    {
        public ComputeBuffer octreeBuffer;
        
        int kernel_id1;
        int kernel_id2;
        struct VertUVs
        {
            public VertUVs(Vector3 vert, Vector2 uv)
            {
                x = vert.x;
                y = vert.y;
                z = vert.z;
                u = uv.x;
                v = uv.y;
            }
            public float x;
            public float y;
            public float z;

            public float u;
            public float v;
        };

        public  void ReleaseBuffers()
        {
            //vert_uvsBuffer?.Release();
            //verticesBuffer?.Release();
            //uvsBuffer?.Release();
            //trianglesBuffer?.Release();
            octreeBuffer?.Release();
            
            //calcCountBuffer?.Release();
            //finishedCountBuffer?.Release();
        }
        public int octreeDepth = 0;
        

        int dataNodeSize;
        uint dataLength = 1;
        DataNode[] dataNodes;
        public void Init(ComputeShader s, int mapScale)
        {
            //voxelConverter = s;
            
            //kernel_id2 = voxelConverter.FindKernel("Kernel2");

            octreeDepth = mapScale;
            //32 * 16

            // octreeBitLength = 2147483648d / 64;
            
            octreeBuffer = new ComputeBuffer((int)(2147483648d / 36), sizeof(int) * 8);
            dataNodeSize =  (int)(Mathf.Pow(2, 26));
            dataNodes = new DataNode[dataNodeSize];

        }

        Texture2D LoadTexture(string path)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.grey); texture.Apply();
            if (path == null) return texture;
            byte[] byteTexture = File.ReadAllBytes(path);
            if (byteTexture.Length > 0)
            {
                texture.LoadImage(byteTexture);
            }
            return texture;

        }

        //public async void UpdateOctree(string filePath_obj, string filePath_texture, UnityAction<string> loadingEffect, int mapScale)
        //{
        //    octreeDepth = mapScale;
        //    Vector3[] vertices = null;
        //    Vector2[] uvs = null;
        //    int[] triangles = null;
        //    Texture2D texture = LoadTexture(filePath_texture);

        //    loadingEffect("Load Into Memory");
        //    float density = 0;
        //    Vector3 coord = Vector3.zero;
        //    await Task.Run(() =>
        //    {
        //        OBJObjectBuilder builder = new OBJLoader().LoadMeshBuilder(filePath_obj);
        //        vertices = builder.GetVertices();
        //        uvs = builder.GetUVs();
        //        triangles = builder.GetTriangles();
        //        builder = null;
        //    });
        //    Controller.instance.StartCoroutine(ShowProgress("Calculating Vertices", loadingEffect));
        //    await Task.Run(() =>
        //    {
        //        Vector3 minPoint = Vector3.one * float.MaxValue;
        //        Vector3 maxPoint = Vector3.one * float.MinValue;
        //        for (int i = 0; i < vertices.Length; i++)
        //        {
        //            percentage = ((i + 1.0f) / vertices.Length);
        //            if (vertices[i].x < minPoint.x) minPoint.x = vertices[i].x;
        //            if (vertices[i].y < minPoint.y) minPoint.y = vertices[i].y;
        //            if (vertices[i].z < minPoint.z) minPoint.z = vertices[i].z;

        //            if (vertices[i].x > maxPoint.x) maxPoint.x = vertices[i].x;
        //            if (vertices[i].y > maxPoint.y) maxPoint.y = vertices[i].y;
        //            if (vertices[i].z > maxPoint.z) maxPoint.z = vertices[i].z;
        //        }
        //        Vector3 size = maxPoint - minPoint;
        //        float longestAxisSize = Mathf.Max(size.x, size.y, size.z);

        //        density = (Mathf.Pow(2, octreeDepth) - 1) / longestAxisSize; //* 0.6f;
        //        coord = ((Vector3.one * longestAxisSize) - (maxPoint + minPoint)) / 2;

        //    });
        //    if (_loadCoroutine != null) Controller.instance.StopCoroutine(_loadCoroutine);

        //    voxelConverter.SetFloat("density", density);
        //    voxelConverter.SetVector("coord", coord);
        //    _loadCoroutine = loadCoroutine(vertices, uvs, triangles, texture, loadingEffect);
        //    Controller.instance.StartCoroutine(_loadCoroutine);
        //}

        //public async void UpdateOctree2(string filePath, string filePath_texture, UnityAction<string> loadingEffect, int mapScale)
        //{
        //    octreeDepth = mapScale;
        //    Vector3[] vertices = null;
        //    Vector2[] uvs = null;
        //    int[] triangles = null;
        //    loadingEffect("Load Into Memory");
        //    float density = 0;
        //    Vector3 coord = Vector3.zero;

        //    Texture2D texture = LoadTexture(filePath_texture);
        //    await Task.Run(() =>
        //    {
        //        (List<MeshData> meshDatas, List<MaterialData> materialDatas) = MeshImporter.LoadMeshData(filePath);
        //        vertices = meshDatas[0].vertices;
        //        uvs = meshDatas[0].uvs;
        //        triangles = meshDatas[0].triangles;
        //        if(materialDatas[0].texture != null)
        //            texture = materialDatas[0].texture;

        //        Debug.Log(materialDatas.Count);
        //    });
            
        //    Controller.instance.StartCoroutine(ShowProgress("Calculating Vertices", loadingEffect));
        //    await Task.Run(() =>
        //    {
        //        Vector3 minPoint = Vector3.one * float.MaxValue;
        //        Vector3 maxPoint = Vector3.one * float.MinValue;
        //        for (int i = 0; i < vertices.Length; i++)
        //        {
        //            percentage = ((i + 1.0f) / vertices.Length);
        //            if (vertices[i].x < minPoint.x) minPoint.x = vertices[i].x;
        //            if (vertices[i].y < minPoint.y) minPoint.y = vertices[i].y;
        //            if (vertices[i].z < minPoint.z) minPoint.z = vertices[i].z;

        //            if (vertices[i].x > maxPoint.x) maxPoint.x = vertices[i].x;
        //            if (vertices[i].y > maxPoint.y) maxPoint.y = vertices[i].y;
        //            if (vertices[i].z > maxPoint.z) maxPoint.z = vertices[i].z;
        //        }
        //        Vector3 size = maxPoint - minPoint;
        //        float longestAxisSize = Mathf.Max(size.x, size.y, size.z);

        //        density = (Mathf.Pow(2, octreeDepth) - 1) / longestAxisSize; //* 0.6f;
        //        coord = ((Vector3.one * longestAxisSize) - (maxPoint + minPoint)) / 2;

        //    });
        //    if (_loadCoroutine != null) Controller.instance.StopCoroutine(_loadCoroutine);
        //    voxelConverter.SetFloat("density", density);
        //    voxelConverter.SetVector("coord", coord);
        //    _loadCoroutine = loadCoroutine(vertices, uvs, triangles, texture, loadingEffect);
        //    Controller.instance.StartCoroutine(_loadCoroutine);
        //}

        //IEnumerator _loadCoroutine;


        //IEnumerator loadCoroutine(Vector3[] vertices, Vector2[] uvs, int[] triangles, Texture2D texture, UnityAction<string> loadingEffect)
        //{
        //    verticesBuffer?.Release();
        //    uvsBuffer?.Release();
        //    trianglesBuffer?.Release();
        //    calcCountBuffer?.Release();
        //    loadingEffect("Uploading to GPU");
        //    yield return null;

        //    //Upload vertices
        //    verticesBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        //    verticesBuffer.SetData(vertices);
        //    voxelConverter.SetBuffer(kernel_id1, "vertices", verticesBuffer);

        //    //Upload triangles
        //    trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        //    voxelConverter.SetBuffer(kernel_id1, "triangles", trianglesBuffer);
        //    trianglesBuffer.SetData(triangles);

        //    calcCountBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
        //    voxelConverter.SetBuffer(kernel_id1, "calcIndex", calcCountBuffer);
        //    calcCountBuffer.SetData(new int[triangles.Length]);

        //    //Upload uvs
        //    if (uvs.Length != 0)
        //    {
        //        uvsBuffer = new ComputeBuffer(uvs.Length, sizeof(float) * 2);
        //        uvsBuffer.SetData(uvs);
        //        voxelConverter.SetBool("noUV", false);
        //    }
        //    else
        //    {
        //        uvsBuffer = new ComputeBuffer(1, sizeof(float) * 2);
        //        voxelConverter.SetBool("noUV", true);
        //    }

        //    voxelConverter.SetBuffer(kernel_id1, "uvs", uvsBuffer);
        //    //Upload texture
        //    RenderTexture t = new RenderTexture(texture.width, texture.height, 32);
        //    t.enableRandomWrite = true;
        //    t.Create();
        //    Graphics.Blit(texture, t);
        //    voxelConverter.SetVector("textureSize", new Vector2(texture.width, texture.height));
        //    voxelConverter.SetTexture(kernel_id1, "_Texture", t);


        //    //Set VoxelCounters
        //    finishedCountBuffer?.Release();
        //    finishedCountBuffer = new ComputeBuffer(1, sizeof(int));
        //    int[] tCount = new int[1] { 0 };
        //    finishedCountBuffer.SetData(tCount);

        //    int triangleCount = triangles.Length / 3;
        //    voxelConverter.SetInt("triangleCount", triangleCount);
        //    voxelConverter.SetBuffer(kernel_id1, "finishedCount", finishedCountBuffer);
        //    voxelBuffer.SetCounterValue(0);
            
        //    //Fill Meshes
        //    while (tCount[0] < triangleCount)
        //    {
        //        loadingEffect("Fill Meshes" + (int)(tCount[0] * 100f / triangleCount) + "%");
        //        voxelConverter.Dispatch(kernel_id1, Mathf.CeilToInt((triangleCount) / 512f), 1, 1);
        //        finishedCountBuffer.GetData(tCount);
        //        yield return null;
        //    }

        //    finishedCountBuffer.Release();
        //    verticesBuffer.Release();
        //    uvsBuffer.Release();
        //    trianglesBuffer.Release();
        //    calcCountBuffer.Release();

        //    //Get data size
        //    ComputeBuffer voxelCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        //    ComputeBuffer.CopyCount(voxelBuffer, voxelCount, 0);
        //    int[] voxelCountArray = { 0 };
        //    voxelCount.GetData(voxelCountArray);
        //    int numVoxels = voxelCountArray[0];
        //    if (numVoxels > voxelBufferSize) numVoxels = voxelBufferSize;
        //    voxelCount.Release();
        //    _loadCoroutine = null;

        //    loadingEffect("Get voxel Data from GPU");
        //    yield return null;
        //    GenerateOctree(numVoxels, loadingEffect);
        //}

        

        float percentage = 0;
        IEnumerator ShowProgress(string defaultStr)
        {
            percentage = 0;
            yield return new WaitWhile(()=>percentage == 0);
            while (percentage < 1)
            {
                OptionUI.instance.stateIndicator.text = (defaultStr + ((int)(percentage * 100)).ToString() + "%");
                yield return null;
            }
        }
        public async void GenerateOctree(Voxel[] voxels, int mapScale)
        {
            octreeDepth = mapScale;
            //Voxel[] voxels = new Voxel[numVoxels];
            //voxelBuffer.GetData(voxels, 0, 0, numVoxels);
            IEnumerator progressCoroutine = ShowProgress("GenerateOctree");
            Controller.instance.StartCoroutine(progressCoroutine);
            await Task.Run(() =>
            {
                uint scale = (uint)Mathf.Pow(2, octreeDepth);

                //Reset If it has before data
                for (int i = 0; i < dataLength; i++) dataNodes[i].Reset();
                dataLength = 1;

                for (int i = 0; i < voxels.Length; i++)
                {
                    percentage = ((float)i / voxels.Length);
                    Vector3 voxelPosition = Vector3.one * scale * 0.5f;
                    uint y = voxels[i].xy & ((uint)Mathf.Pow(2, 16) - 1);
                    uint x = /*scale - 1 - */(voxels[i].xy >> 16) & ((uint)Mathf.Pow(2, 16) - 1);
                    Vector3 pos = new Vector3(x, y, voxels[i].z);
                    uint color = voxels[i].color;
                    uint octreeIdx = 0;

                    float currentScale = scale;
                    for (int j = 0; j < octreeDepth; j++)
                    {
                        int childIdx = 0;
                        if (voxelPosition.x <= pos.x) childIdx |= 4;
                        if (voxelPosition.y <= pos.y) childIdx |= 2;
                        if (voxelPosition.z <= pos.z) childIdx |= 1;

                        currentScale *= 0.5f;
                        Vector3 childPosRelative = Vector3.one * 0.5f * (currentScale);
                        if (voxelPosition.x > pos.x) childPosRelative.x *= -1;
                        if (voxelPosition.y > pos.y) childPosRelative.y *= -1;
                        if (voxelPosition.z > pos.z) childPosRelative.z *= -1;

                        voxelPosition += childPosRelative;

                        //update color
                        if (j == octreeDepth - 1)
                        {
                            uint d = dataNodes[octreeIdx][childIdx];
                            if (d == 0)
                            {
                                d = color;
                                d -= 254;
                                dataNodes[octreeIdx][childIdx] = d;
                            }
                            uint colorData = color;
                            colorData >>= 8;
                            float b = colorData & 255;
                            colorData >>= 8;
                            float g = colorData & 255;
                            colorData >>= 8;
                            float r = colorData & 255;

                            colorData = d;
                            float count = colorData & 255;
                            colorData >>= 8;
                            float b2 = (colorData & 255) * count + b;
                            colorData >>= 8;
                            float g2 = (colorData & 255) * count + g;
                            colorData >>= 8;
                            float r2 = (colorData & 255) * count + r;

                            count++;
                            colorData = (uint)(r2 / count);
                            colorData <<= 8;
                            colorData |= (uint)(g2 / count);
                            colorData <<= 8;
                            colorData |= (uint)(b2 / count);
                            colorData <<= 8;
                            colorData |= (uint)(count);
                            dataNodes[octreeIdx][childIdx] = colorData;
                            break;
                        }
                        if (dataLength == dataNodeSize - 1) break;
                        if (dataNodes[octreeIdx][childIdx] == 0)
                        {
                            dataNodes[octreeIdx][childIdx] = dataLength++;
                        }
                        octreeIdx = dataNodes[octreeIdx][childIdx];
                    }
                    if (dataLength == dataNodeSize - 1) break;
                }
                System.GC.Collect();
            });
            Controller.instance.StopCoroutine(progressCoroutine);
            octreeBuffer.SetData(dataNodes, 0, 0, (int)dataLength);
            OptionUI.instance.stateIndicator.text = ("Octree Size: " + dataLength * 32 + "byte");
            
            VisionController.instance.currentMapScale = octreeDepth;
            VisionController.instance.visionShader.SetFloat(CSPARAMS.MAPSIZE, Mathf.Pow(2, octreeDepth));
            OptionUI.instance.StartLoadButton.interactable = true;
            VisionController.instance.updated = true;
        }
    }
}