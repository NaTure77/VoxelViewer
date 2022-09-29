using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Generation_6: Convert Mesh to Voxel octree. No texture but huge scale of octree
/// </summary>
namespace Generation_6_1
{
    public class Controller : MonoBehaviour
    {
        public static Controller instance;
        PlayerControl playerControl;
        InputManager inputManager;

        bool rtxOn = false;
        bool menuEnabled = false;

        public void Awake()
        {
            instance = this;
            Application.targetFrameRate = 60;
        }

        public void Start()
        {
            playerControl = GetComponent<PlayerControl>();
            if (playerControl == null) playerControl = gameObject.AddComponent<PlayerControl>();

            InitInputmanager();
            VisionController.instance.Init();

            MeshVoxelizer.instance.Init();

            OptionUI.instance.SetMapScale(VisionController.instance.currentMapScale);

            menuEnabled = false;
            OptionUI.instance.ActivateUI(menuEnabled);

            StartCoroutine(Loop());
        }

        IEnumerator Loop()
        {
            Vector3 moveDirection = Vector3.zero;
            while (true)
            {
                Vector2 delta = new Vector2();
                if (!(menuEnabled || rtxOn) || inputManager.Player.MouseMiddle.ReadValue<float>() > 0)
                {
                    delta = inputManager.Player.Look.ReadValue<Vector2>();
                }
                Vector3 rot = playerControl.Rotate(delta);
                
                if(!OptionUI.instance.savePanel.activeSelf)
                {
                    Vector2 dir = inputManager.Player.Move.ReadValue<Vector2>();
                    moveDirection.x = dir.x;
                    moveDirection.z = dir.y;
                    moveDirection.y = inputManager.Player.UPDOWN.ReadValue<float>();
                }
                Vector3 pos = playerControl.Move(moveDirection);
                yield return null;

                VisionController.instance.DispatchComputeShader(pos, rot * Mathf.Deg2Rad);
            }

        }


        void InitInputmanager()
        {
            inputManager = new InputManager();
            inputManager.Enable();

            
            inputManager.Player.Esc.performed += val =>
            {
                menuEnabled = !menuEnabled;
                OptionUI.instance.ActivateUI(menuEnabled);
            };

            inputManager.Player.VoxelLevel.performed += val =>
            {
                if (val.ReadValue<float>() > 0)
                    ++VisionController.instance.VoxelLevel;

                else --VisionController.instance.VoxelLevel;

                VisionController.instance.SetVoxelLevel(VisionController.instance.VoxelLevel);
            };
            inputManager.Player.Pause.performed += val =>
            {
                if (val.ReadValue<float>() > 0)
                {
                    rtxOn = !rtxOn;
                    VisionController.instance.ShowRtx(rtxOn);
                }
            };
        }
        public void LoadMap(string filePath, string filePath_texture, UnityAction<string> loadingEffect, int mapScale)
        {
            VisionController.instance.UpdateOctree(filePath, filePath_texture, loadingEffect, mapScale);
        }

        void OnDestroy()
        {
            inputManager.Dispose();
            VisionController.instance.ReleaseBuffers();
            MeshVoxelizer.instance.ReleaseBuffers();
            VisionController.instance = null;
            instance = null;
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }
}