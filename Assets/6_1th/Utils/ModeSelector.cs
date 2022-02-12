using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Generation_6_1
{
	public class ModeSelector : MonoBehaviour
	{
		private void Awake()
		{
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
		}
		public void LoadScene(string sceneName)
		{
			SceneManager.LoadScene(sceneName);
		}
		public void SetWindowModeScreen()
		{
			Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
			VisionController.instance.Refresh();
		}

		public void SetFullModeScreen()
		{
			Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
			VisionController.instance.Refresh();
		}
		public void ExitApplication()
		{
			Application.Quit();
		}


		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]

		public static void ImportSample()
		{
			string fileName = "/Doom combat scene.obj";
			string filePath = Application.persistentDataPath + fileName;
			if (!System.IO.File.Exists(filePath))
			{
				BetterStreamingAssets.Initialize();
				byte[] data = BetterStreamingAssets.ReadAllBytes(fileName);
				File.WriteAllBytes(filePath, data);
			}

			fileName = "/Doom combat scene_0.jpg";
			filePath = Application.persistentDataPath + fileName;
			if (!System.IO.File.Exists(filePath))
			{
				BetterStreamingAssets.Initialize();
				byte[] data = BetterStreamingAssets.ReadAllBytes(fileName);
				File.WriteAllBytes(filePath, data);
			}
		}
	}
}