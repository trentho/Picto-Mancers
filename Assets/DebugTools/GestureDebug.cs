using System.Linq;
using UnityEngine;
using TMPro;

public class GestureDebug : MonoBehaviour
{
	[SerializeField] XRPlayerController xr;
	public XRPlayerController.Hand hand;

    [SerializeField] TMP_Text predictionText;
    [SerializeField] Renderer rasterRenderer;

	[SerializeField] LineRenderer nLine;
	[SerializeField] LineRenderer xLine;
	[SerializeField] LineRenderer yLine;

	void Start()
	{
		xr ??= XRPlayerController.Main;
        xr.GetHand(hand).Caster.OnGestureDrawn.AddListener(spell => {
            string spellName = spell < 0 ? "garbage" : SpellManager.Instance.spellTypes[spell].spellName;
            predictionText.text = "Prediction: " + spellName;
        });
    }

    // Update is called once per frame
    void Update()
    {
        var canvas = xr.GetHand(hand).Caster.CurrentCanvas;
        if (canvas != null && rasterRenderer.gameObject.activeInHierarchy)
        {
            var drawing = canvas.GetDrawing2D();
		    var raster = drawing.Normalized(23).Translated(2.5f, 2.5f).Rasterized().Transposed();
			Texture2D tex = new Texture2D(28, 28, TextureFormat.RGB24, false);
			for (int x = 0; x < 28; x++)
			{
				for (int y = 0; y < 28; y++)
				{
					float val = raster[x, 28 - y - 1];
					tex.SetPixel(x, y, new Color(val, val, val));
				}
			}
			tex.Apply();
			tex.filterMode = FilterMode.Point;
			rasterRenderer.material.mainTexture = tex;

			if (drawing.Count > 5)
			{
				var (centroid, normal) = canvas.ComputeCentroidAndNormal();
				normal.Normalize();
				Vector3 yAxis = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
				Vector3 xAxis = Vector3.Cross(yAxis, normal).normalized;
				Debug.Log(centroid + ", " + normal + ", " + yAxis + ", " + xAxis);
				nLine.SetPosition(0, centroid);
				yLine.SetPosition(0, centroid);
				xLine.SetPosition(0, centroid);
				nLine.SetPosition(1, centroid+normal*0.5f);
				yLine.SetPosition(1, centroid+yAxis);
				xLine.SetPosition(1, centroid+xAxis);
			}
        }
    }
}
