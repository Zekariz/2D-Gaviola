using UnityEngine;

namespace YourGame.Gameplay.Level
{
    public class LevelBoundsManager : MonoBehaviour
    {
        public static LevelBoundsManager Instance { get; private set; }

        public Bounds CurrentBounds { get; private set; }
        public bool HasBounds { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ComputeBounds();
            DestroyOldWalls(); // Cleanup in case they were accidentally saved in the scene
        }

        private void ComputeBounds()
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            bool found = false;

            Collider2D[] all = FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude);

            foreach (Collider2D col in all)
            {
                if (col.gameObject.name.StartsWith("LevelBound_")) continue;
                if (col.isTrigger) continue;

                Bounds b = col.bounds;
                if (b.size.sqrMagnitude == 0f) continue;

                found = true;
                if (b.min.x < minX) minX = b.min.x;
                if (b.max.x > maxX) maxX = b.max.x;
            }

            if (!found) return;

            Bounds cb = new Bounds();
            cb.SetMinMax(new Vector3(minX, 0f, 0f), new Vector3(maxX, 1f, 0f));
            CurrentBounds = cb;
            HasBounds = true;
        }

        private void DestroyOldWalls()
        {
            GameObject left = GameObject.Find("LevelBound_Left");
            if (left) Destroy(left);
            GameObject right = GameObject.Find("LevelBound_Right");
            if (right) Destroy(right);
        }
    }
}
