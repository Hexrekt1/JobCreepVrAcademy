using UnityEngine;

public class Whiteboard : MonoBehaviour
{
    public Texture2D texture;
    public Vector2 textureSize = new Vector2(2048, 2048);

    void Start()
    {
        var r = GetComponent<Renderer>();
        texture = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] colors = new Color[(int)(textureSize.x * textureSize.y)];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;

        texture.SetPixels(colors);
        texture.Apply();

        r.material.mainTexture = texture;
    }
}
