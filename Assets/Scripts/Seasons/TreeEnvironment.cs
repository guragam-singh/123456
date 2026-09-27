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
            if (transition != null)
                StopCoroutine(transition);

            transition = StartCoroutine(Blend(data, seconds));
        }

        private IEnumerator Blend(SeasonData data, float seconds)
        {
            // IMPORTANT:
            // The main background is intentionally NOT modified here.
            // This allows the current placeholder/final background to remain visible.
            //
            // Seasonal background changes can be re-enabled later when the
            // final seasonal background assets are ready.

            Color fromLeaf = playerLeaf ? playerLeaf.color : Color.white;

            var colors = new Color[foliage.Length];

            for (int i = 0; i < foliage.Length; i++)
            {
                if (foliage[i])
                    colors[i] = foliage[i].color;
            }

            // Keep the crossfade disabled for now.
            if (backgroundCrossfade)
            {
                backgroundCrossfade.color = Color.clear;
            }

            for (float t = 0; t <= seconds; t += Time.unscaledDeltaTime)
            {
                float u = Mathf.Clamp01(t / Mathf.Max(seconds, 0.01f));

                // Player leaf seasonal color
                if (playerLeaf)
                {
                    Color targetLeaf = data.leafColor * data.lightingColor;
                    playerLeaf.color = Color.Lerp(fromLeaf, targetLeaf, u);
                }

                // Foliage seasonal colors
                for (int i = 0; i < foliage.Length; i++)
                {
                    if (!foliage[i])
                        continue;

                    Color target = data.foliageColor * data.lightingColor;

                    target.a =
                        i < Mathf.CeilToInt(foliage.Length * data.foliageDensity)
                        ? target.a
                        : 0f;

                    foliage[i].color = Color.Lerp(colors[i], target, u);
                }

                yield return null;
            }

            // Final player leaf color
            if (playerLeaf)
                playerLeaf.color = data.leafColor * data.lightingColor;

            // Final foliage setup
            for (int i = 0; i < foliage.Length; i++)
            {
                if (!foliage[i])
                    continue;

                // Apply seasonal foliage sprites if available.
                if (data.foliageSprites != null &&
                    data.foliageSprites.Length > 0 &&
                    data.foliageSprites[i % data.foliageSprites.Length])
                {
                    foliage[i].sprite =
                        data.foliageSprites[i % data.foliageSprites.Length];
                }

                Color target = data.foliageColor * data.lightingColor;

                target.a =
                    i < Mathf.CeilToInt(foliage.Length * data.foliageDensity)
                    ? target.a
                    : 0f;

                foliage[i].color = target;
            }

            // Make absolutely sure the crossfade remains invisible.
            if (backgroundCrossfade)
                backgroundCrossfade.color = Color.clear;

            transition = null;
        }
    }
}