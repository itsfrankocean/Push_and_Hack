using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerSpriteLightingRefresh : MonoBehaviour
{
    [Header("Projected Shadow")]
    [SerializeField] private bool disableShadowCaster2D = true;
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.62f);
    [SerializeField] private Vector2 footAnchorOffset = new Vector2(0f, 0.02f);
    [SerializeField] private float widthScale = 1.35f;
    [SerializeField] private float baseLength = 0.58f;
    [SerializeField] private float maxLength = 1.45f;
    [SerializeField] private float lightDistanceForMaxLength = 4.6f;
    [SerializeField] private float shearStrength = 0.78f;
    [SerializeField] private float verticalFlatten = 0.28f;
    [SerializeField] private float sideHeightLift = 0.24f;
    [SerializeField] private float upwardLengthBoost = 1.12f;
    [SerializeField, Range(1, 8)] private int maxShadowLights = 4;
    [SerializeField] private float minLightContribution = 0.04f;
    [SerializeField] private float lightOcclusionInset = 0.08f;
    [SerializeField] private LayerMask floorReceiverMask;
    [SerializeField] private LayerMask wallCollisionMask;
    [SerializeField] private float upperWallMinimumUpDot = 0.35f;
    [SerializeField] private int sortingOrderOffset = -3;

    private SpriteRenderer spriteRenderer;
    private ShadowCaster2D shadowCaster;
    private Vector3[] sourceVertices;
    private Vector3[] projectedVertices;
    private int[] sourceTriangles;
    private Vector2[] sourceUvs;
    private Sprite lastSprite;
    private bool lastFlipX;
    private bool lastFlipY;
    private Light2D[] lightCache;
    private Tilemap[] receiverTilemaps;
    private Vector3 groundedLocalPosition;
    private bool hasGroundedLocalPosition;
    private float nextLightRefreshTime;

    private readonly List<ShadowInstance> shadowInstances = new List<ShadowInstance>();
    private readonly List<LightContribution> lightContributions = new List<LightContribution>();
    private readonly List<Vector3> clippedVertices = new List<Vector3>();
    private readonly List<Vector2> clippedUvs = new List<Vector2>();
    private readonly List<int> clippedTriangles = new List<int>();
    private readonly List<ShadowVertex> clipPolygon = new List<ShadowVertex>(4);
    private readonly List<ShadowVertex> clippedPolygon = new List<ShadowVertex>(4);
    private readonly List<ShadowVertex> cellClipBuffer = new List<ShadowVertex>(8);
    private readonly RaycastHit2D[] lightBlockHits = new RaycastHit2D[8];

    private void Awake()
    {
        CacheComponents();
        CaptureGroundedPosition();
        InitializeLayerMasksIfNeeded();
        RefreshReceiverTilemaps();
        EnsureShadowCapacity();
        DisableRealShadowCaster();
        RefreshShadowMesh(force: true);
    }

    private void OnEnable()
    {
        CacheComponents();
        CaptureGroundedPosition();
        InitializeLayerMasksIfNeeded();
        RefreshReceiverTilemaps();
        EnsureShadowCapacity();
        DisableRealShadowCaster();
    }

    private void OnDisable()
    {
        SetAllShadowsActive(false);
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        DisableRealShadowCaster();
        EnsureShadowCapacity();
        RefreshShadowMesh(force: false);
        UpdateShadowProjection();
    }

    private void CacheComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (shadowCaster == null)
        {
            shadowCaster = GetComponent<ShadowCaster2D>();
        }
    }

    private void DisableRealShadowCaster()
    {
        if (!disableShadowCaster2D || shadowCaster == null)
        {
            return;
        }

        if (shadowCaster.enabled)
        {
            shadowCaster.enabled = false;
        }
    }

    private void EnsureShadowCapacity()
    {
        int desiredCount = Mathf.Max(1, maxShadowLights);
        Transform shadowParent = transform.parent != null ? transform.parent : transform;

        while (shadowInstances.Count < desiredCount)
        {
            int index = shadowInstances.Count;
            string objectName = index == 0 ? "ProjectedShadow" : $"ProjectedShadow_{index}";
            Transform existing = shadowParent.Find(objectName);
            GameObject shadowObject = existing != null ? existing.gameObject : new GameObject(objectName);
            shadowObject.transform.SetParent(shadowParent, false);

            MeshFilter filter = shadowObject.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = shadowObject.AddComponent<MeshFilter>();
            }

            MeshRenderer renderer = shadowObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = shadowObject.AddComponent<MeshRenderer>();
            }

            Mesh mesh = new Mesh
            {
                name = $"Projected Character Shadow {index}"
            };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader)
            {
                name = $"Projected Character Shadow Material {index}",
                hideFlags = HideFlags.HideAndDontSave
            };
            material.renderQueue = 3000;

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            shadowInstances.Add(new ShadowInstance(shadowObject, renderer, mesh, material));
        }

        for (int i = desiredCount; i < shadowInstances.Count; i++)
        {
            shadowInstances[i].gameObject.SetActive(false);
        }
    }

    private void SetAllShadowsActive(bool active)
    {
        for (int i = 0; i < shadowInstances.Count; i++)
        {
            if (shadowInstances[i].gameObject != null)
            {
                shadowInstances[i].gameObject.SetActive(active);
            }
        }
    }

    private void RefreshShadowMesh(bool force)
    {
        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null)
        {
            ClearAllShadowMeshes();
            return;
        }

        if (!force && sprite == lastSprite && spriteRenderer.flipX == lastFlipX && spriteRenderer.flipY == lastFlipY)
        {
            return;
        }

        lastSprite = sprite;
        lastFlipX = spriteRenderer.flipX;
        lastFlipY = spriteRenderer.flipY;

        Vector2[] spriteVertices = sprite.vertices;
        Vector3[] vertices = new Vector3[spriteVertices.Length];
        for (int i = 0; i < spriteVertices.Length; i++)
        {
            float x = spriteRenderer.flipX ? -spriteVertices[i].x : spriteVertices[i].x;
            float y = spriteRenderer.flipY ? -spriteVertices[i].y : spriteVertices[i].y;
            vertices[i] = new Vector3(x, y, 0f);
        }

        int[] triangles = new int[sprite.triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] = sprite.triangles[i];
        }

        sourceVertices = vertices;
        sourceTriangles = triangles;
        sourceUvs = sprite.uv;
        projectedVertices = new Vector3[sourceVertices.Length];

        for (int i = 0; i < shadowInstances.Count; i++)
        {
            ShadowInstance instance = shadowInstances[i];
            if (instance.material != null)
            {
                instance.material.mainTexture = sprite.texture;
            }
        }
    }

    private void ClearAllShadowMeshes()
    {
        for (int i = 0; i < shadowInstances.Count; i++)
        {
            shadowInstances[i].mesh.Clear();
            shadowInstances[i].gameObject.SetActive(false);
        }
    }

    private void UpdateShadowProjection()
    {
        if (sourceVertices == null || sourceTriangles == null || sourceUvs == null)
        {
            ClearAllShadowMeshes();
            return;
        }

        Vector2 lightSamplePosition = GetLightSamplePosition();
        CollectLightContributions(lightSamplePosition);

        int activeCount = Mathf.Min(lightContributions.Count, Mathf.Max(1, maxShadowLights));
        if (activeCount == 0)
        {
            SetAllShadowsActive(false);
            return;
        }

        EnsureShadowCapacity();
        float strongestScore = Mathf.Max(lightContributions[0].score, 0.001f);

        for (int i = 0; i < shadowInstances.Count; i++)
        {
            ShadowInstance instance = shadowInstances[i];
            bool active = i < activeCount;
            instance.gameObject.SetActive(active);
            if (!active)
            {
                instance.mesh.Clear();
                continue;
            }

            LightContribution contribution = lightContributions[i];
            float strength01 = Mathf.Clamp01(contribution.score / strongestScore);
            float alpha = shadowColor.a * Mathf.Lerp(0.25f, 1f, strength01);
            float length = contribution.length * Mathf.Lerp(0.9f, 1.08f, strength01);
            float shear = Mathf.Clamp(Vector2.Dot(contribution.awayFromLight, Vector2.right), -1f, 1f) * shearStrength;
            Vector2 side = new Vector2(contribution.awayFromLight.y, -contribution.awayFromLight.x);

            PositionShadowInstance(instance);
            ProjectShadowMesh(instance, contribution.awayFromLight, side, length, shear, alpha);

            instance.renderer.sortingLayerID = spriteRenderer.sortingLayerID;
            instance.renderer.sortingOrder = spriteRenderer.sortingOrder + sortingOrderOffset - i;
        }
    }

    private void PositionShadowInstance(ShadowInstance instance)
    {
        if (transform.parent != null)
        {
            instance.gameObject.transform.localPosition = groundedLocalPosition + (Vector3)footAnchorOffset;
        }
        else
        {
            instance.gameObject.transform.position = transform.position + (Vector3)footAnchorOffset;
        }

        instance.gameObject.transform.localRotation = Quaternion.identity;
        instance.gameObject.transform.localScale = Vector3.one;
    }

    private void ApplyShadowMaterialProperties(ShadowInstance instance, float alpha)
    {
        if (instance.material == null)
        {
            return;
        }

        Color color = shadowColor;
        color.a = Mathf.Clamp01(alpha);

        if (instance.material.HasProperty("_Color"))
        {
            instance.material.SetColor("_Color", color);
        }

        if (instance.material.HasProperty("_BaseColor"))
        {
            instance.material.SetColor("_BaseColor", color);
        }
    }

    private void CaptureGroundedPosition()
    {
        if (hasGroundedLocalPosition)
        {
            return;
        }

        groundedLocalPosition = transform.localPosition;
        hasGroundedLocalPosition = true;
    }

    private Vector2 GetLightSamplePosition()
    {
        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds.center;
        }

        return transform.position;
    }

    private void InitializeLayerMasksIfNeeded()
    {
        if (floorReceiverMask.value == 0)
        {
            int floorsLayer = LayerMask.NameToLayer("Floors");
            if (floorsLayer >= 0)
            {
                floorReceiverMask = 1 << floorsLayer;
            }
        }

        if (wallCollisionMask.value != 0)
        {
            return;
        }

        int wallsLayer = LayerMask.NameToLayer("Walls");
        if (wallsLayer >= 0)
        {
            wallCollisionMask = 1 << wallsLayer;
        }
    }

    private void RefreshReceiverTilemaps()
    {
        receiverTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
    }

    private void ProjectShadowMesh(ShadowInstance instance, Vector2 awayFromLight, Vector2 side, float length, float shear, float alpha)
    {
        if (sourceVertices == null || projectedVertices == null || sourceTriangles == null || sourceUvs == null || instance.mesh == null)
        {
            return;
        }

        float footY = float.MaxValue;
        for (int i = 0; i < sourceVertices.Length; i++)
        {
            footY = Mathf.Min(footY, sourceVertices[i].y);
        }

        for (int i = 0; i < sourceVertices.Length; i++)
        {
            Vector3 source = sourceVertices[i];
            float heightFromFoot = Mathf.Max(0f, source.y - footY);
            float lateral = source.x;

            Vector2 projected =
                side * (lateral * widthScale) +
                awayFromLight * (heightFromFoot * length + lateral * shear);

            projected += Vector2.up * (heightFromFoot * Mathf.Abs(awayFromLight.x) * sideHeightLift);

            float directionalFlatten = verticalFlatten;
            directionalFlatten = Mathf.Lerp(directionalFlatten, 0.58f, Mathf.Abs(awayFromLight.x));
            directionalFlatten = Mathf.Lerp(directionalFlatten, 0.76f, Mathf.Clamp01(awayFromLight.y));
            projected.y *= directionalFlatten;
            projectedVertices[i] = new Vector3(projected.x, projected.y, 0f);
        }

        ApplyReceiverMaskedMesh(instance.mesh, instance.gameObject.transform, ShouldReceiveOnUpperWalls(awayFromLight));
        ApplyShadowMaterialProperties(instance, alpha);
    }

    private bool ShouldReceiveOnUpperWalls(Vector2 awayFromLight)
    {
        return Vector2.Dot(awayFromLight, Vector2.up) >= upperWallMinimumUpDot;
    }

    private void ApplyReceiverMaskedMesh(Mesh targetMesh, Transform shadowTransform, bool includeUpperWalls)
    {
        if (receiverTilemaps == null || receiverTilemaps.Length == 0)
        {
            RefreshReceiverTilemaps();
        }

        if ((floorReceiverMask.value == 0 && (!includeUpperWalls || wallCollisionMask.value == 0)) || receiverTilemaps == null || receiverTilemaps.Length == 0)
        {
            targetMesh.Clear();
            targetMesh.vertices = projectedVertices;
            targetMesh.uv = sourceUvs;
            targetMesh.triangles = sourceTriangles;
            targetMesh.RecalculateBounds();
            return;
        }

        clippedVertices.Clear();
        clippedUvs.Clear();
        clippedTriangles.Clear();

        for (int i = 0; i < sourceTriangles.Length; i += 3)
        {
            ClipTriangleToReceiverCells(shadowTransform, sourceTriangles[i], sourceTriangles[i + 1], sourceTriangles[i + 2], includeUpperWalls);
        }

        targetMesh.Clear();
        targetMesh.SetVertices(clippedVertices);
        targetMesh.SetUVs(0, clippedUvs);
        targetMesh.SetTriangles(clippedTriangles, 0);
        targetMesh.RecalculateBounds();
    }

    private void ClipTriangleToReceiverCells(Transform shadowTransform, int index0, int index1, int index2, bool includeUpperWalls)
    {
        Vector3 world0 = shadowTransform.TransformPoint(projectedVertices[index0]);
        Vector3 world1 = shadowTransform.TransformPoint(projectedVertices[index1]);
        Vector3 world2 = shadowTransform.TransformPoint(projectedVertices[index2]);

        int minCellX = Mathf.FloorToInt(Mathf.Min(world0.x, Mathf.Min(world1.x, world2.x)));
        int maxCellX = Mathf.FloorToInt(Mathf.Max(world0.x, Mathf.Max(world1.x, world2.x)));
        int minCellY = Mathf.FloorToInt(Mathf.Min(world0.y, Mathf.Min(world1.y, world2.y)));
        int maxCellY = Mathf.FloorToInt(Mathf.Max(world0.y, Mathf.Max(world1.y, world2.y)));

        for (int cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            for (int cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                Vector2 cellCenter = new Vector2(cellX + 0.5f, cellY + 0.5f);
                if (!HasReceiverTile(cellCenter, includeUpperWalls))
                {
                    continue;
                }

                clipPolygon.Clear();
                clipPolygon.Add(new ShadowVertex(projectedVertices[index0], sourceUvs[index0]));
                clipPolygon.Add(new ShadowVertex(projectedVertices[index1], sourceUvs[index1]));
                clipPolygon.Add(new ShadowVertex(projectedVertices[index2], sourceUvs[index2]));

                ClipPolygonToWorldCell(shadowTransform, cellX, cellY);
                AddClippedPolygon();
            }
        }
    }

    private void ClipPolygonToWorldCell(Transform shadowTransform, int cellX, int cellY)
    {
        Vector2 localMin = shadowTransform.InverseTransformPoint(new Vector3(cellX, cellY, 0f));
        Vector2 localMax = shadowTransform.InverseTransformPoint(new Vector3(cellX + 1f, cellY + 1f, 0f));
        float minX = Mathf.Min(localMin.x, localMax.x);
        float maxX = Mathf.Max(localMin.x, localMax.x);
        float minY = Mathf.Min(localMin.y, localMax.y);
        float maxY = Mathf.Max(localMin.y, localMax.y);

        ClipPolygonAgainstAxis(clipPolygon, clippedPolygon, 0, minX, keepGreater: true);
        ClipPolygonAgainstAxis(clippedPolygon, cellClipBuffer, 0, maxX, keepGreater: false);
        ClipPolygonAgainstAxis(cellClipBuffer, clippedPolygon, 1, minY, keepGreater: true);
        ClipPolygonAgainstAxis(clippedPolygon, cellClipBuffer, 1, maxY, keepGreater: false);

        clippedPolygon.Clear();
        clippedPolygon.AddRange(cellClipBuffer);
    }

    private bool HasReceiverTile(Vector2 worldPosition, bool includeUpperWalls)
    {
        if (receiverTilemaps == null)
        {
            return false;
        }

        for (int i = 0; i < receiverTilemaps.Length; i++)
        {
            Tilemap tilemap = receiverTilemaps[i];
            if (tilemap == null || !tilemap.isActiveAndEnabled)
            {
                continue;
            }

            int layerMask = 1 << tilemap.gameObject.layer;
            bool isFloor = (floorReceiverMask.value & layerMask) != 0;
            bool isWall = (wallCollisionMask.value & layerMask) != 0;
            if (!isFloor && (!includeUpperWalls || !isWall))
            {
                continue;
            }

            Vector3Int cell = tilemap.WorldToCell(worldPosition);
            if (!tilemap.HasTile(cell))
            {
                continue;
            }

            if (isFloor)
            {
                return true;
            }

            if (includeUpperWalls && isWall && IsUpperWallReceiverCell(tilemap, cell))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsUpperWallReceiverCell(Tilemap wallTilemap, Vector3Int cell)
    {
        Vector3 cellCenter = wallTilemap.GetCellCenterWorld(cell);
        if (cellCenter.y < GetLightSamplePosition().y)
        {
            return false;
        }

        Vector3Int belowCell = new Vector3Int(cell.x, cell.y - 1, cell.z);
        if (wallTilemap.HasTile(belowCell))
        {
            return false;
        }

        return HasFloorReceiverTile(cellCenter + Vector3.down);
    }

    private bool HasFloorReceiverTile(Vector2 worldPosition)
    {
        if (receiverTilemaps == null || floorReceiverMask.value == 0)
        {
            return false;
        }

        for (int i = 0; i < receiverTilemaps.Length; i++)
        {
            Tilemap tilemap = receiverTilemaps[i];
            if (tilemap == null || !tilemap.isActiveAndEnabled)
            {
                continue;
            }

            int layerMask = 1 << tilemap.gameObject.layer;
            if ((floorReceiverMask.value & layerMask) == 0)
            {
                continue;
            }

            if (tilemap.HasTile(tilemap.WorldToCell(worldPosition)))
            {
                return true;
            }
        }

        return false;
    }

    private void ClipPolygonAgainstAxis(List<ShadowVertex> input, List<ShadowVertex> output, int axis, float limit, bool keepGreater)
    {
        output.Clear();
        if (input.Count == 0)
        {
            return;
        }

        ShadowVertex previous = input[input.Count - 1];
        float previousValue = axis == 0 ? previous.position.x : previous.position.y;
        bool previousInside = keepGreater ? previousValue >= limit : previousValue <= limit;

        for (int i = 0; i < input.Count; i++)
        {
            ShadowVertex current = input[i];
            float currentValue = axis == 0 ? current.position.x : current.position.y;
            bool currentInside = keepGreater ? currentValue >= limit : currentValue <= limit;
            if (currentInside != previousInside)
            {
                float t = Mathf.InverseLerp(previousValue, currentValue, limit);
                output.Add(ShadowVertex.Lerp(previous, current, Mathf.Clamp01(t)));
            }

            if (currentInside)
            {
                output.Add(current);
            }

            previous = current;
            previousValue = currentValue;
            previousInside = currentInside;
        }
    }

    private void AddClippedPolygon()
    {
        if (clippedPolygon.Count < 3)
        {
            return;
        }

        int startIndex = clippedVertices.Count;
        for (int i = 0; i < clippedPolygon.Count; i++)
        {
            clippedVertices.Add(clippedPolygon[i].position);
            clippedUvs.Add(clippedPolygon[i].uv);
        }

        for (int i = 1; i < clippedPolygon.Count - 1; i++)
        {
            clippedTriangles.Add(startIndex);
            clippedTriangles.Add(startIndex + i);
            clippedTriangles.Add(startIndex + i + 1);
        }
    }

    private void CollectLightContributions(Vector2 lightSamplePosition)
    {
        RefreshLightCacheIfNeeded();
        lightContributions.Clear();

        if (lightCache == null)
        {
            return;
        }

        for (int i = 0; i < lightCache.Length; i++)
        {
            Light2D candidate = lightCache[i];
            if (candidate == null || !candidate.isActiveAndEnabled || candidate.lightType == Light2D.LightType.Global)
            {
                continue;
            }

            Vector2 fromLight = lightSamplePosition - (Vector2)candidate.transform.position;
            float distance = Mathf.Max(0.01f, fromLight.magnitude);
            float radius = GetUsableLightRadius(candidate);
            if (distance > radius)
            {
                continue;
            }

            if (IsLightBlocked(candidate.transform.position, lightSamplePosition))
            {
                continue;
            }

            float range01 = Mathf.Clamp01(1f - distance / Mathf.Max(radius, 0.01f));
            float score = candidate.intensity * Mathf.SmoothStep(0f, 1f, range01) / (distance + 0.35f);
            if (score < minLightContribution)
            {
                continue;
            }

            Vector2 awayFromLight = fromLight.sqrMagnitude > 0.0001f ? fromLight.normalized : Vector2.down;
            float lengthT = Mathf.Clamp01(distance / Mathf.Max(0.01f, lightDistanceForMaxLength));
            float length = Mathf.Lerp(maxLength, baseLength, lengthT);
            length *= Mathf.Lerp(1f, upwardLengthBoost, Mathf.Clamp01(awayFromLight.y));

            lightContributions.Add(new LightContribution(candidate, awayFromLight, distance, score, length));
        }

        lightContributions.Sort((a, b) => b.score.CompareTo(a.score));
        if (lightContributions.Count > maxShadowLights)
        {
            lightContributions.RemoveRange(maxShadowLights, lightContributions.Count - maxShadowLights);
        }
    }

    private void RefreshLightCacheIfNeeded()
    {
        if (Time.time < nextLightRefreshTime && lightCache != null)
        {
            return;
        }

        lightCache = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        nextLightRefreshTime = Time.time + 0.25f;
    }

    private float GetUsableLightRadius(Light2D light)
    {
        float radius = Mathf.Max(light.pointLightOuterRadius, 0.75f);
        return Mathf.Max(radius, lightDistanceForMaxLength);
    }

    private bool IsLightBlocked(Vector2 lightPosition, Vector2 samplePosition)
    {
        if (wallCollisionMask.value == 0)
        {
            return false;
        }

        Vector2 delta = samplePosition - lightPosition;
        float distance = delta.magnitude;
        if (distance <= lightOcclusionInset * 2f)
        {
            return false;
        }

        int hitCount = Physics2D.LinecastNonAlloc(lightPosition, samplePosition, lightBlockHits, wallCollisionMask);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = lightBlockHits[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.distance <= lightOcclusionInset || hit.distance >= distance - lightOcclusionInset)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private readonly struct ShadowVertex
    {
        public readonly Vector3 position;
        public readonly Vector2 uv;

        public ShadowVertex(Vector3 position, Vector2 uv)
        {
            this.position = position;
            this.uv = uv;
        }

        public static ShadowVertex Lerp(ShadowVertex from, ShadowVertex to, float t)
        {
            return new ShadowVertex(
                Vector3.LerpUnclamped(from.position, to.position, t),
                Vector2.LerpUnclamped(from.uv, to.uv, t));
        }
    }

    private readonly struct LightContribution
    {
        public readonly Light2D light;
        public readonly Vector2 awayFromLight;
        public readonly float distance;
        public readonly float score;
        public readonly float length;

        public LightContribution(Light2D light, Vector2 awayFromLight, float distance, float score, float length)
        {
            this.light = light;
            this.awayFromLight = awayFromLight;
            this.distance = distance;
            this.score = score;
            this.length = length;
        }
    }

    private sealed class ShadowInstance
    {
        public readonly GameObject gameObject;
        public readonly MeshRenderer renderer;
        public readonly Mesh mesh;
        public readonly Material material;

        public ShadowInstance(GameObject gameObject, MeshRenderer renderer, Mesh mesh, Material material)
        {
            this.gameObject = gameObject;
            this.renderer = renderer;
            this.mesh = mesh;
            this.material = material;
        }
    }
}
