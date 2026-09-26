using System.Collections;
using UnityEngine;

namespace LeafGame
{
    public sealed class TreeEnvironment : MonoBehaviour
    {
        public SpriteRenderer background;
        public SpriteRenderer backgroundCrossfade;
        public SpriteRenderer[] foliage;
        public SpriteRenderer playerLeaf;
        private Coroutine transition;
        public void Apply(SeasonData data, float seconds)
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(Blend(data, seconds));
        }
        private IEnumerator Blend(SeasonData data, float seconds)
        {
            Color fromSky = background.color, fromLeaf = playerLeaf.color;
            var colors = new Color[foliage.Length];
            for (int i = 0; i < foliage.Length; i++) colors[i] = foliage[i].color;
            if (backgroundCrossfade)
            {
                backgroundCrossfade.sprite = data.backgroundSprite ? data.backgroundSprite : background.sprite;
                backgroundCrossfade.color = Color.clear;
            }
            for (float t = 0; t <= seconds; t += Time.unscaledDeltaTime)
            {
                float u = Mathf.Clamp01(t / Mathf.Max(seconds, 0.01f));
                background.color = Color.Lerp(fromSky, data.backgroundColor * data.lightingColor, u);
                if (backgroundCrossfade) { Color sky = data.backgroundColor * data.lightingColor; sky.a *= u; backgroundCrossfade.color = sky; }
                playerLeaf.color = Color.Lerp(fromLeaf, data.leafColor * data.lightingColor, u);
                for (int i = 0; i < foliage.Length; i++)
                {
                    Color target = data.foliageColor * data.lightingColor;
                    target.a = i < Mathf.CeilToInt(foliage.Length * data.foliageDensity) ? target.a : 0f;
                    foliage[i].color = Color.Lerp(colors[i], target, u);
                }
                yield return null;
            }
            if (data.backgroundSprite) background.sprite = data.backgroundSprite;
            background.color = data.backgroundColor * data.lightingColor;
            playerLeaf.color = data.leafColor * data.lightingColor;
            if (backgroundCrossfade) backgroundCrossfade.color = Color.clear;
            for (int i = 0; i < foliage.Length; i++)
            {
                if (data.foliageSprites.Length > 0 && data.foliageSprites[i % data.foliageSprites.Length]) foliage[i].sprite = data.foliageSprites[i % data.foliageSprites.Length];
                Color target = data.foliageColor * data.lightingColor;
                target.a = i < Mathf.CeilToInt(foliage.Length * data.foliageDensity) ? target.a : 0f;
                foliage[i].color = target;
            }
            transition = null;
        }
    }
}
