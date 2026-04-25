using UnityEngine;

namespace StockAlert
{
    public static class Utils
    {
        public static Texture2D SpriteToTexture(Sprite sprite)
        {
            if (sprite == null) return null;

            var rect = sprite.textureRect;
            var tex = new Texture2D((int)rect.width, (int)rect.height);
            var pixels = sprite.texture.GetPixels(
                (int)rect.x, (int)rect.y,
                (int)rect.width, (int)rect.height
            );
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
