using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DongHo.Courtyard
{
    /// <summary>CPU-culled pack meshes; no compute or custom vegetation shader.</summary>
    [ExecuteAlways]
    public sealed class CourtyardVegetation : MonoBehaviour
    {
        [Serializable]
        public sealed class Species
        {
            public string source;
            public Mesh mesh;
            public Material[] materials;
            public bool grass;
            public float cullDistance = 22f;
        }

        [Serializable]
        public struct Placement
        {
            public int species;
            public Vector3 foot;
            public float yaw;
            public float scale;
        }

        public int seed = 20261002;
        public Species[] species = Array.Empty<Species>();
        public Placement[] placements = Array.Empty<Placement>();
        public Material nearGrass;
        public Material farGrass;
        public Texture2D densityMask;
        public Vector4 maskBounds = new Vector4(30, -38, 80, 38);
        public WindZone wind;
        public float grassCullDistance = 9.5f;
        public float nearRingDistance = 3.5f;
        [NonSerialized] public int lastGrassVisible;
        [NonSerialized] public int lastDecorationVisible;
        [NonSerialized] public int lastDrawSubmissions;

        private sealed class Group
        {
            public Mesh animatedMesh;
            public Vector3[] originalVertices;
            public Vector3[] bentVertices;
            public Placement[] placements;
            public Matrix4x4[] matrices;
        }

        private Group[] groups;
        private readonly Matrix4x4[] batch = new Matrix4x4[1023];
        private float lastWindTime = float.NegativeInfinity;
        private Bounds worldBounds;

        private void OnEnable()
        {
            RebuildCache();
            RenderPipelineManager.beginCameraRendering += RenderForCamera;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= RenderForCamera;
            ReleaseMeshes();
        }

        public void RebuildCache()
        {
            ReleaseMeshes();
            groups = new Group[species.Length];
            worldBounds = new Bounds(new Vector3(55, 7, 0), new Vector3(52, 16, 78));
            for (int i = 0; i < species.Length; i++)
            {
                var list = new List<Placement>();
                foreach (var placement in placements)
                    if (placement.species == i) list.Add(placement);
                var group = new Group { placements = list.ToArray() };
                group.matrices = new Matrix4x4[group.placements.Length];
                for (int j = 0; j < group.placements.Length; j++)
                {
                    var p = group.placements[j];
                    group.matrices[j] = Matrix4x4.TRS(p.foot,
                        Quaternion.Euler(0, p.yaw, 0), Vector3.one * p.scale);
                }
                if (species[i].grass && species[i].mesh)
                {
                    group.animatedMesh = Instantiate(species[i].mesh);
                    group.animatedMesh.name = species[i].mesh.name + "_WindRuntime";
                    group.animatedMesh.hideFlags = HideFlags.HideAndDontSave;
                    group.animatedMesh.MarkDynamic();
                    group.originalVertices = species[i].mesh.vertices;
                    group.bentVertices = new Vector3[group.originalVertices.Length];
                }
                groups[i] = group;
            }
            lastWindTime = float.NegativeInfinity;
        }

        private void ReleaseMeshes()
        {
            if (groups == null) return;
            foreach (var group in groups)
            {
                if (group == null || !group.animatedMesh) continue;
                if (Application.isPlaying) Destroy(group.animatedMesh);
                else DestroyImmediate(group.animatedMesh);
            }
            groups = null;
        }

        private void UpdateWind(float time)
        {
            // URP Lit/Simple Lit do not consume WindZone automatically. Bend the
            // four shared grass meshes on the CPU, using the actual Unity zone.
            // The buried bottom third stays anchored; only exposed tips sway.
            if (Mathf.Abs(time - lastWindTime) < 1f / 30f) return;
            lastWindTime = time;
            bool activeWind = wind && wind.gameObject.activeInHierarchy;
            float main = activeWind ? wind.windMain : 0;
            float turbulence = activeWind ? wind.windTurbulence : 0;
            float pulse = activeWind
                ? Mathf.Sin(time * wind.windPulseFrequency * Mathf.PI * 2) * wind.windPulseMagnitude
                : 0;
            Vector3 direction = activeWind ? wind.transform.forward : Vector3.zero;
            direction.y = 0;
            direction.Normalize();
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                if (!group.animatedMesh) continue;
                float height = Mathf.Max(0.01f, species[i].mesh.bounds.size.y);
                for (int v = 0; v < group.originalVertices.Length; v++)
                {
                    Vector3 original = group.originalVertices[v];
                    float weight = Mathf.Clamp01((original.y / height - 1f / 3f) * 1.5f);
                    float gust = main * (0.65f + 0.35f * Mathf.Sin(time * 1.8f + i * 1.7f))
                        + pulse + turbulence * Mathf.Sin(time * 3.1f + original.x * 6 + i);
                    group.bentVertices[v] = original + direction * (gust * 0.09f * weight * weight);
                }
                group.animatedMesh.vertices = group.bentVertices;
                Bounds bounds = species[i].mesh.bounds;
                bounds.Expand(0.15f);
                group.animatedMesh.bounds = bounds;
            }
        }

        private void RenderForCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!camera || camera.cameraType == CameraType.Preview
                || camera.cameraType == CameraType.Reflection) return;
            if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
            if (groups == null || groups.Length != species.Length) RebuildCache();
            UpdateWind(Time.realtimeSinceStartup);
            Vector3 viewer = camera.transform.position;
            // In a running game the viewing camera is the player's head. For
            // Scene View/documentation renders the same horizontal-distance rule
            // follows that camera, without disabling culling for screenshots.
            lastGrassVisible = lastDecorationVisible = lastDrawSubmissions = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                var type = species[i];
                if (!type.mesh) continue;
                var group = groups[i];
                if (type.grass)
                {
                    DrawRing(camera, viewer, group, type, nearGrass, 0, nearRingDistance);
                    DrawRing(camera, viewer, group, type, farGrass, nearRingDistance, grassCullDistance);
                }
                else
                {
                    for (int submesh = 0; submesh < type.mesh.subMeshCount; submesh++)
                    {
                        if (submesh >= type.materials.Length) continue;
                        DrawDecoration(camera, viewer, group, type, submesh, type.materials[submesh]);
                    }
                }
            }
        }

        private void DrawRing(Camera camera, Vector3 viewer, Group group, Species type,
            Material material, float minimum, float maximum)
        {
            if (!material) return;
            int count = 0;
            for (int i = 0; i < group.placements.Length; i++)
            {
                Vector3 delta = group.placements[i].foot - viewer;
                float distanceSquared = delta.x * delta.x + delta.z * delta.z;
                if (distanceSquared >= maximum * maximum || distanceSquared < minimum * minimum) continue;
                // Shrink over the outer metre, reducing the hard cull pop.
                float factor = Mathf.Clamp01((grassCullDistance - Mathf.Sqrt(distanceSquared)) / 1f);
                Matrix4x4 matrix = group.matrices[i];
                if (factor < 1)
                {
                    // Keep the buried foot fixed, rather than floating on fade.
                    matrix *= Matrix4x4.Scale(Vector3.one * factor);
                }
                batch[count++] = matrix;
                lastGrassVisible++;
                if (count == batch.Length)
                {
                    Submit(camera, group.animatedMesh, 0, material, count, false);
                    count = 0;
                }
            }
            if (count > 0) Submit(camera, group.animatedMesh, 0, material, count, false);
        }

        private void DrawDecoration(Camera camera, Vector3 viewer, Group group, Species type,
            int submesh, Material material)
        {
            if (!material) return;
            int count = 0;
            for (int i = 0; i < group.placements.Length; i++)
            {
                Vector3 delta = group.placements[i].foot - viewer;
                float distanceSquared = delta.x * delta.x + delta.z * delta.z;
                if (distanceSquared >= type.cullDistance * type.cullDistance) continue;
                batch[count++] = group.matrices[i];
                if (submesh == 0) lastDecorationVisible++;
                if (count == batch.Length)
                {
                    Submit(camera, type.mesh, submesh, material, count, type.cullDistance > 50);
                    count = 0;
                }
            }
            if (count > 0) Submit(camera, type.mesh, submesh, material, count, type.cullDistance > 50);
        }

        private void Submit(Camera camera, Mesh mesh, int submesh, Material material, int count, bool shadows)
        {
            var parameters = new RenderParams(material)
            {
                camera = camera,
                layer = gameObject.layer,
                worldBounds = worldBounds,
                shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows = true,
                lightProbeUsage = LightProbeUsage.BlendProbes
            };
            Graphics.RenderMeshInstanced(parameters, mesh, submesh, batch, count);
            lastDrawSubmissions++;
        }
    }
}
