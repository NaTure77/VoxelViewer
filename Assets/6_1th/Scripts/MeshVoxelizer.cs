using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityMeshImporter;
using System.Threading.Tasks;
using System.IO;
using System;

namespace Generation_6_1
{
    public class MeshVoxelizer : MonoBehaviour
    {
        public Text stateIndicator;

        public ComputeShader voxelConverter;
        ComputeBuffer voxelBuffer;

        int kernel_id1;


        int voxelBufferSize;
        public static MeshVoxelizer instance;

        private void Awake()
        {
            instance = this;
        }
        public void Init()
        {
            kernel_id1 = voxelConverter.FindKernel("Kernel1");
            voxelBufferSize = (int)(2147483648d / 12);
            voxelBuffer = new ComputeBuffer(voxelBufferSize, sizeof(int) * 3, ComputeBufferType.Append);
            voxelConverter.SetBuffer(kernel_id1, "voxels", voxelBuffer);

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

        float percentage;
        IEnumerator ShowProgress(string defaultStr)
        {
            percentage = 0;
            yield return new WaitWhile(() => percentage == 0);
            while (percentage < 1)
            {
                OptionUI.instance.stateIndicator.text = (defaultStr + ((int)(percentage * 100)).ToString() + "%");
                yield return null;
            }
        }

        public async void Voxelize(string modelPath, int mapScale, UnityAction<Voxel[], int> callback, string texturePath = null)
        {
            (List<MeshData> meshDatas, List<MaterialData> materialDatas) data = (null,null);

            bool hasTexture = texturePath != null;
            Texture2D texture = LoadTexture(texturePath);
            float density = 0;
            Vector3 coord = Vector3.zero;

            stateIndicator.text = "Load Model Data";
            await Task.Run(() =>
            {
                data = MeshImporter.LoadMeshData(modelPath);
            });

            if (!hasTexture)
            {
                for(int i = 0; i < data.materialDatas.Count; i++)
                {
                    if (!data.materialDatas[i].HasTextureDiffuse) continue;
                    Texture2D uTexture = new Texture2D(2, 2);
                    texturePath = Directory.GetParent(modelPath).FullName + data.materialDatas[i].texturePath;//Path.Combine();

                    Debug.Log(Directory.GetParent(modelPath).FullName);
                    Debug.Log(texturePath);
                    byte[] byteArray = File.ReadAllBytes(texturePath);
                    bool isLoaded = uTexture.LoadImage(byteArray);
                    if (!isLoaded)
                    {
                        throw new Exception("Cannot find texture file: " + texturePath);
                    }
                    hasTexture = true;
                    uTexture.Apply();
                    data.materialDatas[i].texture = uTexture;
                }
                if(!hasTexture)
                {
                    RenderTexture t = new RenderTexture(2, 2, 32);
                    t.enableRandomWrite = true;
                    t.Create();
                    voxelConverter.SetTexture(kernel_id1, "_Texture", t);
                }
            }
            else
            {
                for (int i = 0; i < data.materialDatas.Count; i++)
                {
                    data.materialDatas[i].texture = texture;
                    data.materialDatas[i].HasTextureDiffuse = true;
                }
            }
            //for (int i = 0; i < data.materialDatas.Count; i++)
            //    Debug.Log(data.materialDatas[i].color);

            stateIndicator.text = "Calculating Vertices for resizing";

            await Task.Run(() =>
            {
                Vector3 minPoint = Vector3.one * float.MaxValue;
                Vector3 maxPoint = Vector3.one * float.MinValue;

                for (int j = 0; j < data.meshDatas.Count; j++)
                {
                    //percentage = ((j + 1.0f) / data.meshDatas.Count);
                    Vector3[] vertices = data.meshDatas[j].vertices;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        
                        vertices[i].x *= data.meshDatas[j].localScale.x;
                        vertices[i].y *= data.meshDatas[j].localScale.y;
                        vertices[i].z *= data.meshDatas[j].localScale.z;

                        vertices[i] += data.meshDatas[j].localPosition;
                        vertices[i] = data.meshDatas[j].localRotation * vertices[i];

                        
                        if (vertices[i].x < minPoint.x) minPoint.x = vertices[i].x;
                        if (vertices[i].y < minPoint.y) minPoint.y = vertices[i].y;
                        if (vertices[i].z < minPoint.z) minPoint.z = vertices[i].z;

                        if (vertices[i].x > maxPoint.x) maxPoint.x = vertices[i].x;
                        if (vertices[i].y > maxPoint.y) maxPoint.y = vertices[i].y;
                        if (vertices[i].z > maxPoint.z) maxPoint.z = vertices[i].z;
                    }
                }
                
                Vector3 size = maxPoint - minPoint;
                float longestAxisSize = Mathf.Max(size.x, size.y, size.z);

                density = (Mathf.Pow(2, mapScale) - 1) / longestAxisSize; //* 0.6f;
                coord = ((Vector3.one * longestAxisSize) - (maxPoint + minPoint)) / 2;

            });
            voxelConverter.SetFloat("density", density);
            voxelConverter.SetVector("coord", coord);

            if (_loadCoroutine != null) Controller.instance.StopCoroutine(_loadCoroutine);
            _loadCoroutine = Voxelize(data.meshDatas,data.materialDatas,mapScale, callback);

            StartCoroutine(_loadCoroutine);
        }
        IEnumerator _loadCoroutine;
        IEnumerator Voxelize(List<MeshData> meshDatas, List<MaterialData> materialDatas, int mapScale, UnityAction<Voxel[], int> callback)
        {
            ComputeBuffer trianglesBuffer;
            ComputeBuffer verticesBuffer;
            ComputeBuffer uvsBuffer;
            ComputeBuffer calcCountBuffer;
            ComputeBuffer finishedCountBuffer;

            voxelBuffer.SetCounterValue(0);
            
            for (int i = 0; i < meshDatas.Count; i++)
            {
                stateIndicator.text = ("Uploading to GPU");
                //Upload vertices
                verticesBuffer = new ComputeBuffer(meshDatas[i].vertices.Length, sizeof(float) * 3);
                verticesBuffer.SetData(meshDatas[i].vertices);
                voxelConverter.SetBuffer(kernel_id1, "vertices", verticesBuffer);

                //Upload triangles
                trianglesBuffer = new ComputeBuffer(meshDatas[i].triangles.Length, sizeof(int));
                voxelConverter.SetBuffer(kernel_id1, "triangles", trianglesBuffer);
                trianglesBuffer.SetData(meshDatas[i].triangles);

                calcCountBuffer = new ComputeBuffer(meshDatas[i].triangles.Length, sizeof(int));
                voxelConverter.SetBuffer(kernel_id1, "calcIndex", calcCountBuffer);
                calcCountBuffer.SetData(new int[meshDatas[i].triangles.Length]);


                // localPosition, localScale, localRotation도 넘기기.
                //
                // Debug.Log(meshDatas[i].materialIndex);
                MaterialData materialData = materialDatas[meshDatas[i].materialIndex];

                voxelConverter.SetBool("materialMode", !materialData.HasTextureDiffuse);

                voxelConverter.SetVector("currentMatColor", new Vector4(materialData.color.r, materialData.color.g, materialData.color.b, materialData.color.a));
                if (materialData.HasTextureDiffuse)
                {
                    RenderTexture t = new RenderTexture(materialData.texture.width, materialData.texture.height, 32);
                    t.enableRandomWrite = true;
                    t.Create();
                    Graphics.Blit(materialData.texture, t);
                    voxelConverter.SetVector("textureSize", new Vector2(materialData.texture.width, materialData.texture.height));
                    voxelConverter.SetTexture(kernel_id1, "_Texture", t);
                }

                //Upload UVs
                if (meshDatas[i].uvs.Length != 0)
                {
                    uvsBuffer = new ComputeBuffer(meshDatas[i].uvs.Length, sizeof(float) * 2);
                    uvsBuffer.SetData(meshDatas[i].uvs);
                    voxelConverter.SetBool("noUV", false);
                }

                else
                {
                    uvsBuffer = new ComputeBuffer(1, sizeof(float) * 2);
                    voxelConverter.SetBool("noUV", true);
                }
                voxelConverter.SetBuffer(kernel_id1, "uvs", uvsBuffer);

                //Set VoxelCounters
                finishedCountBuffer = new ComputeBuffer(1, sizeof(int));
                int[] tCount = new int[1] { 0 };
                finishedCountBuffer.SetData(tCount);

                int triangleCount = meshDatas[i].triangles.Length / 3;
                voxelConverter.SetInt("triangleCount", triangleCount);
                voxelConverter.SetBuffer(kernel_id1, "finishedCount", finishedCountBuffer);
                
                while (tCount[0] < triangleCount)
                {
                    stateIndicator.text = ("Fill Meshes" + (int)(tCount[0] * 100f / triangleCount) + "%");

                    voxelConverter.Dispatch(kernel_id1, Mathf.CeilToInt((triangleCount) / 512f), 1, 1);
                    finishedCountBuffer.GetData(tCount);
                    yield return null;
                }
                finishedCountBuffer.Release();
                verticesBuffer.Release();
                uvsBuffer.Release();
                trianglesBuffer.Release();
                calcCountBuffer.Release();

                yield return null;
            }

            //Get data size
            ComputeBuffer voxelCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            ComputeBuffer.CopyCount(voxelBuffer, voxelCount, 0);
            int[] voxelCountArray = { 0 };
            voxelCount.GetData(voxelCountArray);
            int numVoxels = voxelCountArray[0];
            if (numVoxels > voxelBufferSize) numVoxels = voxelBufferSize;
            voxelCount.Release();

            stateIndicator.text = ("Get voxel Data from GPU");
            yield return null;

            Voxel[] voxels = new Voxel[numVoxels];
            voxelBuffer.GetData(voxels, 0, 0, numVoxels);

            callback(voxels, mapScale);
        }

        public void ReleaseBuffers()
        {
            voxelBuffer?.Release();
        }
    }

}
