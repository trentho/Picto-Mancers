using UnityEngine;
using System;
[RequireComponent(typeof(Camera))]
public class TriggerCapture : MonoBehaviour
{
	void OnEnable()
	{
        var cam = GetComponent<Camera>();
		string filename = string.Format("Screenshots/capture_{0}.png", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
		// int width = Screen.width;
		// int height = Screen.height;
		int width = cam.pixelWidth;
		int height = cam.pixelHeight;

		RenderTexture screenTexture = new RenderTexture(width, height, 16);
        var prevTargetTexture = cam.targetTexture;
		cam.targetTexture = screenTexture;
		RenderTexture.active = screenTexture;
		cam.Render();
        cam.targetTexture = prevTargetTexture;
		Texture2D renderedTexture = new Texture2D(width, height);
		renderedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		RenderTexture.active = null;
		byte[] byteArray = renderedTexture.EncodeToPNG();
		System.IO.File.WriteAllBytes(filename, byteArray);
	}
}