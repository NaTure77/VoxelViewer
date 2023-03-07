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
        public RenderTexture texture;

        public RawImage rawImage;

        int kernel_id1;


        int maxBufferSize;
        public static MeshVoxelizer instance;

        private void Awake()
        {
            instance = this;
        }
        public void Init()
        {
            kernel_id1 = voxelConverter.FindKernel("Kernel1");
            maxBufferSize = (int)(2147483648d / 12);
            voxelBuffer = new ComputeBuffer(maxBufferSize, sizeof(int) * 3, ComputeBufferType.Append);
            voxelConverter.SetBuffer(kernel_id1, "voxels", voxelBuffer);

            texture = new RenderTexture(8192, 8192, 32);
            texture.enableRandomWrite = true;
            texture.Create();
            voxelConverter.SetTexture(kernel_id1, "_Texture", texture);
            rawImage.texture = texture;
        }

        public void DrawTexture(Texture2D source)
        {
            RenderTexture.active = texture;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, 8192, 8192, 0);
            Graphics.DrawTexture(new Rect(0, 8192 - source.height, source.width, source.height), source);
            GL.PopMatrix();
            RenderTexture.active = null;
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
            Texture2D texture_imported = LoadTexture(texturePath);
            List<Texture2D> textures = new List<Texture2D>();
            List<string> texturePaths = new List<string>();

            float density = 0;
            Vector3 coord = Vector3.zero;

            stateIndicator.text = "Load Model Data";
            await Task.Run(() =>
            {
                data = MeshImporter.LoadMeshData(modelPath);
            });

            /*if (!hasTexture)
            {
                for(int i = 0; i < data.materialDatas.Count; i++)
                {
                    if (!data.materialDatas[i].HasTextureDiffuse) continue;
                    if (texturePaths.Contains(data.materialDatas[i].texturePath))
                    {
                        data.materialDatas[i].textureIndex = texturePaths.IndexOf(data.materialDatas[i].texturePath);
                        data.materialDatas[i].texture = textures[data.materialDatas[i].textureIndex];
                        continue;
                    }
                    texturePaths.Add(data.materialDatas[i].texturePath);
                    Texture2D uTexture = new Texture2D(2, 2);
                    if(data.materialDatas[i].texturePath[0] != '/')
                        texturePath = Directory.GetParent(modelPath).FullName + '/' + data.materialDatas[i].texturePath;
                    else texturePath = Directory.GetParent(modelPath).FullName + data.materialDatas[i].texturePath;//Path.Combine();
                    Debug.Log(texturePath);

                    byte[] byteArray = File.ReadAllBytes(texturePath);
                    bool isLoaded = uTexture.LoadImage(byteArray);
                    if (!isLoaded)
                    {
                        throw new Exception("Cannot find texture file: " + texturePath);
                    }
                    hasTexture = true;
                    uTexture.Apply();

                    textures.Add(uTexture);
                    data.materialDatas[i].textureIndex = textures.Count - 1;
                    data.materialDatas[i].texture = uTexture;
                }
                if(!hasTexture)
                {
                    RenderTexture t = new RenderTexture(2, 2, 32);
                    t.enableRandomWrite = true;
                    t.Create();
                    voxelConverter.SetTexture(kernel_id1, "_Texture", t);
                }
                hasTexture = false;
            }
            else
            {
                for (int i = 0; i < data.materialDatas.Count; i++)
                {
                    data.materialDatas[i].texture = texture_imported;
                    data.materialDatas[i].HasTextureDiffuse = true;
                }
            }*/

            stateIndicator.text = "Calculating Vertices for resizing";
            await Task.Run(() =>
            {
                data.meshDatas.Sort((a, b) => 
                { 
                    return 
                        data.materialDatas[a.materialIndex].textureIndex.CompareTo(
                        data.materialDatas[b.materialIndex].textureIndex); 
                });

                Vector3 minPoint = Vector3.one * float.MaxValue;
                Vector3 maxPoint = Vector3.one * float.MinValue;

                for (int j = 0; j < data.meshDatas.Count; j++)
                {
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
                coord = ((Vector3.one * longestAxisSize) - (maxPoint + minPoint)) * 0.5f;

            });
            voxelConverter.SetFloat("density", density);
            voxelConverter.SetVector("coord", coord);

            if (_loadCoroutine != null) Controller.instance.StopCoroutine(_loadCoroutine);
            _loadCoroutine = Voxelize(data.meshDatas,data.materialDatas,mapScale, callback, hasTexture);

            StartCoroutine(_loadCoroutine);
        }
        IEnumerator _loadCoroutine;
        IEnumerator Voxelize(List<MeshData> meshDatas, List<MaterialData> materialDatas, int mapScale, UnityAction<Voxel[], int> callback, bool hasTexture)
        {
            ComputeBuffer trianglesBuffer;
            ComputeBuffer verticesBuffer;
            ComputeBuffer uvsBuffer;
            ComputeBuffer calcCountBuffer;
            ComputeBuffer finishedCountBuffer;
            ComputeBuffer voxelCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

            int[] voxelCountArray = { 0 };
            voxelBuffer.SetCounterValue(0);

            int currentTextureIndex = -1;

            if (hasTexture)
            {
                DrawTexture(materialDatas[0].texture);
                Debug.Log("Has Texture");
            }
            Debug.Log("material num: " + materialDatas.Count);
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


                //Upload material or textures
                MaterialData materialData = materialDatas[meshDatas[i].materialIndex];
                voxelConverter.SetBool("materialMode", !materialData.HasTextureDiffuse);
                voxelConverter.SetVector("currentMatColor", new Vector4(materialData.color.r, materialData.color.g, materialData.color.b, materialData.color.a));
                
                
                

               /* if (!hasTexture && materialData.HasTextureDiffuse && currentTextureIndex != meshDatas[i].materialIndex)
                {
                    currentTextureIndex = meshDatas[i].materialIndex;
                    DrawTexture(materialData.texture);
                    Debug.Log("dont have texture");
                    voxelConverter.SetVector("textureSize", new Vector2(materialData.texture.width, materialData.texture.height));
                }*/

                //Set VoxelCounters
                finishedCountBuffer = new ComputeBuffer(1, sizeof(int));
                int[] tCount = new int[1] { 0 };
                finishedCountBuffer.SetData(tCount);

                int triangleCount = meshDatas[i].triangles.Length / 3;
                voxelConverter.SetInt("triangleCount", triangleCount);
                voxelConverter.SetBuffer(kernel_id1, "finishedCount", finishedCountBuffer);
                
                //Calculate voxels position on triangle surfaces
                while (tCount[0] < triangleCount)
                {
                    stateIndicator.text = ("Fill Meshes" + (int)(tCount[0] * 100f / triangleCount) + "%");

                    voxelConverter.Dispatch(kernel_id1, Mathf.CeilToInt((triangleCount) / 512f), 1, 1);
                    finishedCountBuffer.GetData(tCount);

                    ComputeBuffer.CopyCount(voxelBuffer, voxelCount, 0);
                    voxelCount.GetData(voxelCountArray);
                    if (voxelCountArray[0] > maxBufferSize) break;
                    yield return null;
                }
                finishedCountBuffer.Release();
                verticesBuffer.Release();
                uvsBuffer.Release();
                trianglesBuffer.Release();
                calcCountBuffer.Release();

                if (voxelCountArray[0] > maxBufferSize) break;
                yield return null;
            }
            voxelCount.Release();

            int numVoxels = voxelCountArray[0];
            if (numVoxels > maxBufferSize) numVoxels = maxBufferSize;
            
            stateIndicator.text = ("Get voxel Data from GPU");
            yield return null;
            Voxel[] voxels = new Voxel[numVoxels];
            voxelBuffer.GetData(voxels, 0, 0, numVoxels);

            //Generate Octree
            callback(voxels, mapScale);
        }

        public void ReleaseBuffers()
        {
            voxelBuffer?.Release();
        }
    }

}
