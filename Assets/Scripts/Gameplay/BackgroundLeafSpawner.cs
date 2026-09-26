using System.Collections.Generic;
using UnityEngine;

namespace LeafGame
{
    public sealed class BackgroundLeafSpawner : MonoBehaviour
    {
        public SeasonManager seasons;
        public DecorativeLeaf prefab;
        public Transform[] branchPoints;
        [Min(1)] public int maximumAlive = 18;
        public Vector2 scaleRange = new Vector2(0.4f, 0.75f);
        private readonly List<DecorativeLeaf> active = new List<DecorativeLeaf>();
        private float nextSpawn;
        private void Update()
        {
            if (!seasons || !seasons.Running || !seasons.Current || seasons.Current.fallingLeafInterval <= 0 || !prefab || branchPoints.Length == 0) return;
            if (Time.unscaledTime < nextSpawn) return;
            nextSpawn = Time.unscaledTime + seasons.Current.fallingLeafInterval;
            active.RemoveAll(item => !item);
            if (active.Count >= maximumAlive) return;
            var point = branchPoints[Random.Range(0, branchPoints.Length)];
            var leaf = Instantiate(prefab, point.position, Quaternion.Euler(0, 0, Random.Range(0, 360)), transform);
            leaf.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
            leaf.spriteRenderer.color = seasons.Current.leafColor * new Color(0.85f, 0.85f, 0.85f, 0.7f);
            active.Add(leaf);
        }
        public void Clear()
        {
            foreach (var leaf in active) if (leaf) Destroy(leaf.gameObject);
            active.Clear();
        }
        private void OnDisable() { Clear(); }
    }
}
