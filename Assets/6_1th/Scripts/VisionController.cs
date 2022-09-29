using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Generation_6_1
{
    public class VisionController : MonoBehaviour
    {
        public ComputeShader visionShader;
        public ComputeShader octreeShader;
        public ComputeShader temporalAA = null;
        public ComputeShader gridShader = null;
        public RenderTexture RenderTexture => renderTexture;
        RenderTexture renderTexture = null;
        RenderTexture renderTexture_history = null;
        public RawImage rawImage;
        public Texture2D backgroundImage;

        public GameObject crossHead;
        Vector2Int resolution = new Vector2Int();

        [Range(4,12)]
        public int mapScale = 8;

        public int currentMapScale;

        int kernelID_normal;
        int kernelID_rtx;
        int KernelID_taa;
        int KernelID_grid;
        int KernelID_focus;

        public bool lightEnabled = false;
        public bool gridEnabled = false;
        public bool fogEnabled = false;

        Vector3 lightPos = new Vector3();
        IEnumerator coroutineVar;

        Vector3 pos = new Vector3(128, 128, 128);
        Vector3 rot = new Vector3(128, 128, 128);

        public static VisionController instance;

        OctreeManager octreeManager = new OctreeManager();
        ComputeBuffer focusPosBuffer;

        Vector2[] randomVectors = new Vector2[16];
        int randomVectorIndex = 0;
        private void Awake()
        {
            instance = this;
        }
        private void OnValidate()
        {
            visionShader.SetVector("skyColor", skyColor);
            visionShader.SetVector("groundColor", groundColor);
            visionShader.SetVector("lightColor", lightColor);
            visionShader.SetFloat("lightIntensity", lightIntensity);
            SetFieldOfView((int)fieldOfView);
            SetLightDirection();

            SetFuzz(fuzz);
            SetLens_radiuds(lens_radius);
            SetMaterialType(materialType);
            Setfocus_dist(focus_dist);
        }


        public void ReleaseBuffers()
        {
            renderTexture?.Release();

            recs?.Release();
            colorSum?.Release();
            traced_cnt?.Release();
            seeds?.Release();
            focusPosBuffer?.Release();
            octreeManager.ReleaseBuffers();
        }

        public void SetOctreeBuffer(ComputeBuffer octreeBuffer)
        {
            visionShader.SetBuffer(kernelID_normal, CSPARAMS.OCTREE, octreeBuffer);
            visionShader.SetBuffer(kernelID_rtx, CSPARAMS.OCTREE, octreeBuffer);
        }

        public void Init()
        {
            currentMapScale = mapScale;
            kernelID_normal = visionShader.FindKernel("Kernel2");
            kernelID_rtx = visionShader.FindKernel("Kernel3");
            KernelID_taa = temporalAA.FindKernel("CSMain");
            KernelID_grid = gridShader.FindKernel(CSPARAMS.KERNELID);
            KernelID_focus = visionShader.FindKernel("Focus");

            visionShader.SetInt(CSPARAMS.VOXELLVL, VoxelLevel);
            visionShader.SetFloat(CSPARAMS.MAX_STEPS, 700);
            visionShader.SetFloat(CSPARAMS.MAPSIZE, Mathf.Pow(2, currentMapScale));
            visionShader.SetVector(CSPARAMS.POSITION, pos);

            octreeManager.Init(octreeShader, currentMapScale);


        Vector4[] childDirections =
        {
                new Vector4(-1,-1,-1),
                new Vector4(-1,-1,1),
                new Vector4(-1,1,-1),
                new Vector4(-1,1,1),
                new Vector4(1,-1,-1),
                new Vector4(1,-1,1),
                new Vector4(1,1,-1),
                new Vector4(1,1,1)
        };
        visionShader.SetVectorArray(CSPARAMS.CHILDDIR, childDirections);
            SetOctreeBuffer(octreeManager.octreeBuffer);

            focusPosBuffer = new ComputeBuffer(1, sizeof(float) * 3);
            visionShader.SetBuffer(KernelID_focus, "focus_pos", focusPosBuffer);



            randomVectors[00] = new Vector2(0.500000f, 0.333333f);
            randomVectors[01] = new Vector2(0.250000f, 0.666667f);
            randomVectors[02] = new Vector2(0.750000f, 0.111111f);
            randomVectors[03] = new Vector2(0.125000f, 0.444444f);
            randomVectors[04] = new Vector2(0.625000f, 0.777778f);
            randomVectors[05] = new Vector2(0.375000f, 0.222222f);
            randomVectors[06] = new Vector2(0.875000f, 0.555556f);
            randomVectors[07] = new Vector2(0.062500f, 0.888889f);
            randomVectors[08] = new Vector2(0.562500f, 0.037037f);
            randomVectors[09] = new Vector2(0.312500f, 0.370370f);
            randomVectors[10] = new Vector2(0.812500f, 0.703704f);
            randomVectors[11] = new Vector2(0.187500f, 0.148148f);
            randomVectors[12] = new Vector2(0.687500f, 0.481481f);
            randomVectors[13] = new Vector2(0.437500f, 0.814815f);
            randomVectors[14] = new Vector2(0.937500f, 0.259259f);
            randomVectors[15] = new Vector2(0.031250f, 0.592593f);


            visionShader.SetVector("skyColor", skyColor);
            visionShader.SetVector("groundColor", groundColor);
            visionShader.SetVector("lightColor", lightColor);
            visionShader.SetFloat("lightIntensity", lightIntensity);
            SetLightDirection();


            ShowShadow(false);
            ShowGrid(false);
            ShowOctree(false);

            ShowRtx(false);

            SetFieldOfView((int)fieldOfView);
            SetFuzz(fuzz);
            SetLens_radiuds(lens_radius);
            Setfocus_dist(focus_dist);
            SetMaterialType(0);
            visionShader.SetFloat(CSPARAMS.MAX_STEPS, 800);

            Refresh();
        }


        ComputeBuffer recs;
        ComputeBuffer colorSum;
        ComputeBuffer traced_cnt;
        ComputeBuffer seeds;
        public void Init2()
        {
            recs?.Release();
            colorSum?.Release();
            traced_cnt?.Release();
            seeds?.Release();

            recs = new ComputeBuffer(resolution.x * resolution.y, sizeof(float) * 17);
            colorSum = new ComputeBuffer(resolution.x * resolution.y, sizeof(float) * 3);
            traced_cnt = new ComputeBuffer(resolution.x * resolution.y, sizeof(int) * 2);
            seeds = new ComputeBuffer(resolution.x * resolution.y, sizeof(int));

            uint[] s = new uint[resolution.x * resolution.y];
            System.Random rand = new System.Random();
            for (int i = 0; i < s.Length; i++)
                s[i] = (uint)rand.Next(-int.MaxValue, int.MaxValue);
            seeds.SetData(s);

            visionShader.SetBuffer(kernelID_rtx, "recs", recs);
            visionShader.SetBuffer(kernelID_normal, "recs", recs);
            visionShader.SetBuffer(KernelID_focus, "recs", recs);
            visionShader.SetBuffer(kernelID_rtx, "color_sum", colorSum);
            visionShader.SetBuffer(kernelID_rtx, "traced_cnt", traced_cnt);
            visionShader.SetBuffer(kernelID_rtx, "seeds", seeds);

            visionShader.SetVector("skyColor", skyColor);
            visionShader.SetVector("groundColor", groundColor);

            gridShader.SetBuffer(KernelID_grid, "recs", recs);
        }
        public void Refresh()
        {
            //해상도 설정
            resolution.x = (int)(Screen.currentResolution.width * qualityLevels[currentQLevel]);
            resolution.y = (int)(Screen.currentResolution.height * qualityLevels[currentQLevel]);

            //renderTexture 생성
            renderTexture?.Release();
            renderTexture = new RenderTexture(resolution.x, resolution.y, 32);
            renderTexture.enableRandomWrite = true;
            //renderTexture.filterMode = FilterMode.Point;
            renderTexture.Create();

            renderTexture_history?.Release();
            renderTexture_history = new RenderTexture(resolution.x, resolution.y, 32);
            renderTexture_history.enableRandomWrite = true;
            //renderTexture.filterMode = FilterMode.Point;
            renderTexture_history.Create();

            rawImage.texture = renderTexture;

            RenderTexture bgTexture = new RenderTexture(backgroundImage.width, backgroundImage.height, 32);
            bgTexture.enableRandomWrite = true;
            bgTexture.Create();
            Graphics.Blit(backgroundImage, bgTexture);

            visionShader.SetTexture(kernelID_normal, "backgroundImage", bgTexture);
            visionShader.SetTexture(kernelID_rtx, "backgroundImage", bgTexture);

            //computeShader 변수 세팅
            visionShader.SetVector(CSPARAMS.RESOLUTION, (Vector2)resolution);
            visionShader.SetVector(CSPARAMS.SCREEN_CENTER, new Vector2(Mathf.FloorToInt(resolution.x * 0.5f), Mathf.FloorToInt(resolution.y * 0.5f)));
            visionShader.SetTexture(kernelID_normal, CSPARAMS.RESULT, renderTexture);
            visionShader.SetTexture(kernelID_rtx, CSPARAMS.RESULT, renderTexture);

            temporalAA.SetTexture(KernelID_taa, "history", renderTexture_history);
            temporalAA.SetTexture(KernelID_taa, "current", renderTexture);
            temporalAA.SetVector(CSPARAMS.RESOLUTION, (Vector2)resolution);

            gridShader.SetVector(CSPARAMS.RESOLUTION, (Vector2)resolution);
            gridShader.SetTexture(KernelID_grid, CSPARAMS.RESULT, renderTexture);

            Init2();
            updated = true;
        }

        Vector3 pos_absolute;
        public void DispatchComputeShader(Vector3 position, Vector3 rotation)
        {
            float pos_delta = (pos - position).magnitude;
            float rot_delta = (rot - rotation).magnitude;
            temporalAA.SetBool("updated", pos_delta > 0.00001f || rot_delta > 0.0001f);
            if (pos != position || rot != rotation)
            {
                updated = true;
            }
            pos = position;
            rot = rotation;

            pos_absolute = position * Mathf.Pow(2, currentMapScale);
            visionShader.SetVector(CSPARAMS.ROTATION, rotation);
            visionShader.SetVector(CSPARAMS.POSITION, pos_absolute);

            gridShader.SetVector(CSPARAMS.POSITION, pos_absolute);

            /*if(octreeManager.triangleCount != 0)
            {
                octreeManager.Dispatch_ResetMap();
                octreeManager.RotateMap();
                octreeManager.Dispatch_Update();
            }*/


            visionShader.SetBool("updated", updated);
            if (!rtxOn)
            {
                
                if (!updated && randomVectorIndex == randomVectors.Length - 1) return;
                
                visionShader.Dispatch(kernelID_normal,
                    Mathf.CeilToInt(1.0f * resolution.x / CSPARAMS.THREAD_NUMBER_X),
                    Mathf.CeilToInt(1.0f * resolution.y / CSPARAMS.THREAD_NUMBER_Y), 
                    1);

                gridShader.Dispatch(KernelID_grid,
                    Mathf.CeilToInt(1.0f * resolution.x / CSPARAMS.THREAD_NUMBER_X),
                    Mathf.CeilToInt(1.0f * resolution.y / CSPARAMS.THREAD_NUMBER_Y), 1);


                 if(updated) randomVectorIndex = 0;
                 else randomVectorIndex = (randomVectorIndex + 1) % randomVectors.Length;

                visionShader.SetVector("randomVector", randomVectors[randomVectorIndex]);
                temporalAA.Dispatch(KernelID_taa,
                    Mathf.CeilToInt(1.0f * resolution.x / 8),
                    Mathf.CeilToInt(1.0f * resolution.y / 8), 1);
            }
            else
            {
                visionShader.Dispatch(kernelID_rtx,
                    Mathf.CeilToInt(1.0f * resolution.x / CSPARAMS.THREAD_NUMBER_X),
                    Mathf.CeilToInt(1.0f * resolution.y / CSPARAMS.THREAD_NUMBER_Y), 1);

                if (updated)
                {
                    updated = false;
                    visionShader.SetBool("updated", updated);
                    visionShader.Dispatch(kernelID_rtx,
                        Mathf.CeilToInt(1.0f * resolution.x / 8),
                        Mathf.CeilToInt(1.0f * resolution.y / 8), 1);
                }
                temporalAA.Dispatch(KernelID_taa,
                    Mathf.CeilToInt(1.0f * resolution.x / 8),
                    Mathf.CeilToInt(1.0f * resolution.y / 8), 1);
            }


            updated = false;
        }

        public bool updated = false;
        public bool rtxOn = false;
        public bool showOctree = false;

        public Color skyColor;
        public Color groundColor;
        public Color lightColor;
        [Range(0, 1)]
        public float lightIntensity = 1;
        public Vector2 lightAngle;
        public Vector3 lightDirection;

        [Range(0, 20)]
        public float fuzz = 0.01f;

        [Range(0, 1)]
        public float lens_radius = 0.1f;

        [Range(0, 100)]
        public float focus_dist = 100f;

        [Range(0, 180)]
        public float fieldOfView = 60;

        int materialType = 0;

        public void EnableSetSkyColor3()
        {
            Slider3Controller.instance.SetSliderFunction("Set Sky Color", SetSkyColor, Vector3.zero, Vector3.one, skyColor);
        }
        public void EnableSetGroundColor3()
        {
            Slider3Controller.instance.SetSliderFunction("Set Ground Color", SetGroundColor, Vector3.zero, Vector3.one, groundColor);
        }
        public void EnableSetLens_radiuds()
        {
            Slider3Controller.instance.SetSliderFunction("Set Lens Radius", SetLens_radiuds, 0, 2, lens_radius);
        }
        public void EnableSeFocusDist()
        {
            Slider3Controller.instance.SetSliderFunction("Set Focus Distance", Setfocus_dist, 1, 50, focus_dist);
        }

        public void EnableSetLightColor()
        {
            Slider3Controller.instance.SetSliderFunction("Set Light Color", SetLightColor, Vector3.zero, Vector3.one, lightColor);
        }
        public void EnableSetLightAngle()
        {
            Slider3Controller.instance.SetSliderFunction("Set Light Angle", SetLightAngle, Vector2.zero, Vector2.one * 360, lightAngle);
        }
        public void EnableSetLightIntensity()
        {
            Slider3Controller.instance.SetSliderFunction("Set Light Intensity", SetLightIntensity, 0, 1, lightIntensity);
        }
        public void EnableSetQuality()
        {
            Slider3Controller.instance.SetSliderFunction("Set Resolution Quality", SetQuality, 0, 4, currentQLevel);
        }
        public void EnableSetFoV()
        {
            Slider3Controller.instance.SetSliderFunction("Set Field Of View", SetFieldOfView, 1, 179, (int)fieldOfView);
        }

        void SetFieldOfView(int i)
        {
            fieldOfView = i;
            visionShader.SetFloat("viewport_height", Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f) * 2);
            updated = true;
        }
        public void ToggleGrid()
        {
            gridEnabled = !gridEnabled;
            ShowGrid(gridEnabled);
        }

        void SetLightDirection()
        {
            float sx, cx, sy, cy;
            //sincos(a.x, sx, cx);
            //sincos(-a.y, sy, cy);

            sx = Mathf.Sin(lightAngle.x * Mathf.Deg2Rad);
            cx = Mathf.Cos(lightAngle.x * Mathf.Deg2Rad);

            sy = Mathf.Sin(-lightAngle.y * Mathf.Deg2Rad);
            cy = Mathf.Cos(-lightAngle.y * Mathf.Deg2Rad);

            lightDirection = new Vector3(-sx * sy, cx, sx * cy);
            visionShader.SetVector("lightDirection", lightDirection);
        }
        void SetSkyColor(Color c)
        {
            skyColor = c;
            visionShader.SetVector("skyColor", skyColor);
            updated = true;
        }
        void SetGroundColor(Color c)
        {
            groundColor = c;
            visionShader.SetVector("groundColor", groundColor);
            updated = true;
        }
        void SetLightColor(Color c)
        {
            lightColor = c;
            visionShader.SetVector("lightColor", lightColor);
            updated = true;
        }
        void SetLightAngle(Vector2 v)
        {
            lightAngle = v;
            SetLightDirection();
            updated = true;
        }
        void SetLightIntensity(float f)
        {
            lightIntensity = f;
            visionShader.SetFloat("lightIntensity", lightIntensity);
            updated = true;
        }
        public void ShowGrid(bool b)
        {
            visionShader.SetBool(CSPARAMS.FLAG_GRID, b);
            gridShader.SetBool(CSPARAMS.FLAG_GRID, b);
            //crossHead.SetActive(b);
            updated = true;
        }
        public void ShowOctree(bool b)
        {
            showOctree = b;
            visionShader.SetBool("showOctree", showOctree);
            updated = true;
        }

        public void ShowRtx(bool b)
        {
            rtxOn = b;
            visionShader.SetBool("rtxOn", rtxOn);
            updated = true;
        }

        /// <summary>
        /// 0: lambertian, 1: metal
        /// </summary>
        /// <param name="i"></param>
        public void SetMaterialType(int i)
        {
            materialType = i;
            visionShader.SetInt("materialType", materialType);
            updated = true;
        }

        public void SetMaterial_Metal()
        {
            SetMaterialType(1);
            Slider3Controller.instance.SetSliderFunction("Set Metal Fuzz", SetFuzz, 0, 2, fuzz);
        }
        public void SetMaterial_Lambertian()
        {
            SetMaterialType(0);
        }


        public void SetFuzz(float f)
        {
            fuzz = f;
            visionShader.SetFloat("fuzz", fuzz);
            updated = true;
        }

        public void SetLens_radiuds(float f)
        {
            lens_radius = f;
            visionShader.SetFloat("lens_radius", lens_radius);
            updated = true;
        }
        public void Setfocus_dist(float f)
        {
            focus_dist = f;
            visionShader.SetFloat("focus_dist", focus_dist);
            updated = true;
        }
        public void SetFocusPosition()
        {
            //blockToolShader.Dispatch(KernelID_blockTool7, 1, 1, 1);

            visionShader.Dispatch(KernelID_focus, 1, 1, 1);
            Vector3[] focusPosResult = { Vector3.zero };

            focusPosBuffer.GetData(focusPosResult);
            Setfocus_dist((PlayerControl.instance.Pos * Mathf.Pow(2, currentMapScale) - focusPosResult[0]).magnitude);
        }

        public void UpdateOctree(string filePath_obj, string filePaht_texture, UnityAction<string> loadingEffect, int _mapScale)
        {
            //octreeManager.UpdateOctree(filePath_obj, filePaht_texture, loadingEffect, _mapScale);
            //octreeManager.UpdateOctree2(filePath_obj, filePaht_texture, loadingEffect, _mapScale);

            MeshVoxelizer.instance.Voxelize(filePath_obj, _mapScale, octreeManager.GenerateOctree, filePaht_texture);
            updated = true;
        }
        //public void CreateTexture3D() => StartCoroutine(octreeManager.CreateTexture3D());

        public void ToggleShadow()
        {
            ShowShadow(!lightEnabled);
        }
        public void ShowShadow(bool b)
        {
            lightEnabled = b;
            visionShader.SetBool(CSPARAMS.FLAG_LIGHT, b);
            updated = true;
        }

        public void ToggleFog()
        {
            fogEnabled = !fogEnabled;
            ShowFog(fogEnabled);
        }
        public void ShowFog(bool b)
        {
            visionShader.SetBool(CSPARAMS.FLAG_FOG, b);
        }

        public int VoxelLevel = 0;
        // public Text sText;
        public void SetVoxelLevel(int v)
        {
            VoxelLevel = (int)Mathf.Clamp(v, 0, currentMapScale - 1);
            visionShader.SetInt(CSPARAMS.VOXELLVL, VoxelLevel);
            // sText.text = VoxelLevel.ToString();
            updated = true;
        }


        float[] qualityLevels = { 0.0625f, 0.125f, 0.25f, 0.5f, 1 };
        int currentQLevel = 3;
        public void SetQuality(int i)
        {
            currentQLevel = (int)Mathf.Clamp(i, 0, 4);
            Refresh();
            //updated = true;
        }
    }
}