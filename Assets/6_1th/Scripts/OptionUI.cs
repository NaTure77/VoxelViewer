using Dummiesman;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Generation_6_1
{
    public class OptionUI : MonoBehaviour
    {
        public GameObject optionPanel;

        public GameObject savePanel;
        public InputField inputField;
        public Button captureButton;

        public Button LoadObjFile;
        public Button LoadTextureFile;

        public Text stateIndicator;
        public Text mapSizeText;

        public static OptionUI instance;
        private void Awake()
        {
            instance = this;
        }
        private void Start()
        {
            if (captureButton != null)
            {
                captureButton.onClick.AddListener(() =>
                {
                    savePanel.SetActive(false);
                    if (inputField.text.Length == 0) return;

                    RenderTexture screen = VisionController.instance.RenderTexture;
                    RenderTexture.active = screen;

                    Texture2D tex = new Texture2D(screen.width, screen.height, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, screen.width, screen.height), 0, 0);
                    tex.Apply();

                    RenderTexture.active = null;
                    byte[] byteArr = tex.EncodeToPNG();

                    DirectoryInfo di = new DirectoryInfo(Application.persistentDataPath + "/captured/");
                    if (!di.Exists) di.Create();
                    File.WriteAllBytes(Application.persistentDataPath + "/captured/" + inputField.text + ".png", byteArr);
                });
            }


            if (LoadObjFile != null)
                LoadObjFile.onClick.AddListener(() =>
                {
                    SimpleFileBrowser.FileBrowser.OnSuccess success = (string[] s) =>
                    {
                        if (!File.Exists(s[0])) return;
                        filePath_obj = s[0];
                        LoadObjFile.GetComponentInChildren<Text>().text = Path.GetFileName(filePath_obj);
                        StartLoadButton.interactable = true;
                    };
                    SimpleFileBrowser.FileBrowser.OnCancel cancel = () => 
                    { 
                        filePath_obj = null; 
                        LoadObjFile.GetComponentInChildren<Text>().text = "Select File";
                        StartLoadButton.interactable = false;
                    };
                    SimpleFileBrowser.FileBrowser.SetFilters(true, ".obj", ".fbx");
                    SimpleFileBrowser.FileBrowser.ShowLoadDialog
                        (success, cancel, initialPath: Application.persistentDataPath, title: "Import Obj file", loadButtonText: "Select");
                });

            if (LoadTextureFile != null)
                LoadTextureFile.onClick.AddListener(() =>
                {
                    SimpleFileBrowser.FileBrowser.OnSuccess success = (string[] s) =>
                    {
                        if (!File.Exists(s[0])) return;
                        filePath_texture = s[0];
                        LoadTextureFile.GetComponentInChildren<Text>().text = Path.GetFileName(filePath_texture);
                    };
                    SimpleFileBrowser.FileBrowser.OnCancel cancel = () => 
                    { 
                        filePath_texture = null; LoadTextureFile.GetComponentInChildren<Text>().text = "Select File"; 
                    };
                    SimpleFileBrowser.FileBrowser.SetFilters(false, ".jpg",".png");
                    SimpleFileBrowser.FileBrowser.ShowLoadDialog
                        (success, cancel, initialPath: Application.persistentDataPath, title: "Import Texture file", loadButtonText: "Select");
                });

            StartLoadButton.interactable = false;
            if (StartLoadButton != null)
                StartLoadButton.onClick.AddListener(LoadMap);
        }

        public void ActivateUI(bool activate)
        {
            optionPanel.SetActive(activate); 
        }

        int mapScale = 8;
        public void SetMapScale(float s)
        {
            mapScale = (int)s;
            mapSizeText.text = "MapSize: " + ((int)Mathf.Pow(2, mapScale)).ToString() + "^3";
        }

        public Button StartLoadButton;
        string filePath_obj = null;
        string filePath_texture = null;
        public void LoadMap()
        {
            inputField.text = Path.GetFileNameWithoutExtension(filePath_obj);
            StartLoadButton.interactable = false;
            VisionController.instance.UpdateOctree(filePath_obj, filePath_texture, (text) => stateIndicator.text = text, mapScale);
        }
    }

}