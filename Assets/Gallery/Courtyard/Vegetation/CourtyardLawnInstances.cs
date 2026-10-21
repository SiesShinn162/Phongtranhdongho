using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gallery.Courtyard
{
    /// <summary>Foot-pivot, camera-specific GPU batches; the authored placement mask is baked into instances.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CourtyardLawnInstances : MonoBehaviour
    {
        [Serializable]
        public sealed class Species
        {
            public string label;
            public Mesh mesh;
            public Mesh midMesh;
            public Mesh farMesh;
            public Material material;
            public bool isGrass;
            public bool movesWithWind;
        }

        [Serializable]
        public struct Instance
        {
            public Vector3 ground;
            public float yaw;
            public float height;
            public float phase;
            public float sinkFraction;
            public int species;
        }

        public int placementSeed = 20261003;
        public Transform player;
        public WindZone windZone;
        public float grassFullDistance = 10f;
        // Kept for serialized/editor-builder compatibility; now the MID-to-FAR threshold.
        // Grass is never distance-culled, including beyond this value.
        public float grassCullDistance = 26f;
        public float infillFullDistance = 55f;
        public float infillCullDistance = 65f;
        public float editorWindTime = 7.25f;
        public Species[] species = Array.Empty<Species>();
        public Instance[] instances = Array.Empty<Instance>();

        private int[][] buckets;
        private readonly Matrix4x4[] batch = new Matrix4x4[1023];

        private void OnEnable()
        {
            RebuildCache();
            RenderPipelineManager.beginCameraRendering -= RenderForCamera;
            RenderPipelineManager.beginCameraRendering += RenderForCamera;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= RenderForCamera;
        }

        private void OnValidate()
        {
            grassFullDistance = Mathf.Max(0.1f, grassFullDistance);
            grassCullDistance = Mathf.Max(grassFullDistance + 0.1f, grassCullDistance);
            infillCullDistance = Mathf.Max(infillFullDistance + 0.1f, infillCullDistance);
            RebuildCache();
        }

        public void RebuildCache()
        {
            if (species == null || instances == null)
            {
                buckets = null;
                return;
            }
            var counts = new int[species.Length];
            for (int i = 0; i < instances.Length; i++)
            {
                int kind = instances[i].species;
                if (kind >= 0 && kind < counts.Length) counts[kind]++;
            }
            buckets = new int[species.Length][];
            for (int s = 0; s < species.Length; s++) buckets[s] = new int[counts[s]];
            Array.Clear(counts, 0, counts.Length);
            for (int i = 0; i < instances.Length; i++)
            {
                int kind = instances[i].species;
                if (kind >= 0 && kind < counts.Length) buckets[kind][counts[kind]++] = i;
            }
        }

        private void RenderForCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!isActiveAndEnabled || camera == null || buckets == null || !SystemInfo.supportsInstancing)
                return;
            if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
            // Runtime uses one XR observer so both eyes select the same grass LOD.
            Vector3 observer = Application.isPlaying && player != null ? player.position : camera.transform.position;
            float time = Application.isPlaying ? Time.time : editorWindTime;
            Vector3 direction = windZone != null ? windZone.transform.forward : Vector3.right;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
            Vector3 swayAxis = Vector3.Cross(Vector3.up, direction);
            float main = windZone != null && windZone.gameObject.activeInHierarchy ? windZone.windMain : 0f;
            float turbulence = windZone != null && windZone.gameObject.activeInHierarchy ? windZone.windTurbulence : 0f;
            float pulseMagnitude = windZone != null && windZone.gameObject.activeInHierarchy ? windZone.windPulseMagnitude : 0f;
            float pulseFrequency = windZone != null ? windZone.windPulseFrequency : 0f;

            for (int s = 0; s < species.Length; s++)
            {
                Species item = species[s];
                if (item == null || item.mesh == null || item.material == null) continue;
                var render = new RenderParams(item.material)
                {
                    camera = camera,
                    layer = gameObject.layer,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = true,
                    lightProbeUsage = LightProbeUsage.Off
                };
                int[] selected = buckets[s];
                int tierCount = item.isGrass ? 3 : 1;
                for (int tier = 0; tier < tierCount; tier++)
                {
                Mesh drawMesh = tier == 0 ? item.mesh
                    : tier == 1 ? (item.midMesh != null ? item.midMesh : item.mesh)
                    : (item.farMesh != null ? item.farMesh : item.mesh);
                int used = 0;
                for (int j = 0; j < selected.Length; j++)
                {
                    Instance point = instances[selected[j]];
                    float dx = point.ground.x - observer.x;
                    float dz = point.ground.z - observer.z;
                    float distanceSquared = dx * dx + dz * dz;
                    float fade = 1f;
                    if (item.isGrass)
                    {
                        int selectedTier = distanceSquared < grassFullDistance * grassFullDistance ? 0
                            : distanceSquared < grassCullDistance * grassCullDistance ? 1 : 2;
                        if (tier != selectedTier) continue;
                    }
                    else
                    {
                        if (distanceSquared >= infillCullDistance * infillCullDistance) continue;
                        fade = 1f - Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(infillFullDistance, infillCullDistance, Mathf.Sqrt(distanceSquared)));
                        if (fade < 0.015f) continue;
                    }
                    float height = point.height * fade;
                    float sink = item.isGrass ? 1f / 3f : point.sinkFraction;
                    Vector3 root = point.ground - Vector3.up * (height * sink);
                    float sway = item.movesWithWind && tier < 2
                        ? (main * Mathf.Sin(time * 1.6f + point.phase)
                           + turbulence * 0.45f * Mathf.Sin(time * 2.7f + point.phase * 1.7f)
                           + pulseMagnitude * Mathf.Sin(time * pulseFrequency * Mathf.PI * 2f)) * 7f
                        : 0f;
                    Quaternion rotation = Quaternion.AngleAxis(sway, swayAxis) * Quaternion.Euler(0f, point.yaw, 0f);
                    // All grass LODs retain full height and the same buried foot pivot.
                    batch[used++] = Matrix4x4.TRS(root, rotation, Vector3.one * height);
                    if (used == batch.Length)
                    {
                        Graphics.RenderMeshInstanced(render, drawMesh, 0, batch, used);
                        used = 0;
                    }
                }
                if (used > 0) Graphics.RenderMeshInstanced(render, drawMesh, 0, batch, used);
                }
            }
        }
    }
}
