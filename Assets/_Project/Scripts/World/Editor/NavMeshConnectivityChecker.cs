using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Residuum.World.Editor
{
    /// <summary>
    /// 检查当前场景已烘焙 NavMesh 的各区域是否能从同一种子点到达。
    /// </summary>
    public static class NavMeshConnectivityChecker
    {
        private const string CheckMenuPath = "Residuum/检查地图连通性";
        private const string ClearMenuPath = "Residuum/清除连通性标记";
        private const float MinimumProbeSpacing = 2f;
        private const float SpacingGrowthMultiplier = 1.25f;
        private const float SeedSampleMaxDistance = 5f;
        private const float MarkerScale = 0.12f;
        private const float SeedMarkerScale = 0.22f;
        private const float SeedLabelHeightScale = 0.35f;
        private const int MaxProbePoints = 2000;
        private const int MaxReportedUnreachablePoints = 20;
        private const int MaxSceneMarkers = 500;

        private static readonly System.Collections.Generic.List<Vector3> PartialPoints =
            new System.Collections.Generic.List<Vector3>();

        private static readonly System.Collections.Generic.List<Vector3> InvalidPoints =
            new System.Collections.Generic.List<Vector3>();

        private static Vector3 _seedPoint;
        private static ulong _checkedSceneHandle;
        private static bool _hasMarkers;

        [MenuItem(CheckMenuPath)]
        private static void CheckConnectivity()
        {
            ClearMarkerState();

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Vector3[] vertices = triangulation.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                Debug.LogError("当前场景没有已烘焙的 NavMesh。请先烘焙 NavMesh，再执行连通性检查。");
                return;
            }

            float actualSpacing = MinimumProbeSpacing;
            System.Collections.Generic.List<Vector3> probePoints =
                DownsampleVertices(vertices, actualSpacing);
            if (probePoints.Count == 0)
            {
                Debug.LogError("已烘焙的 NavMesh 没有可用顶点。请重新烘焙 NavMesh 后再检查。");
                return;
            }

            bool spacingWasIncreased = false;
            while (probePoints.Count > MaxProbePoints)
            {
                spacingWasIncreased = true;
                float densityScale = Mathf.Sqrt((float)probePoints.Count / MaxProbePoints);
                actualSpacing *= Mathf.Max(SpacingGrowthMultiplier, densityScale);
                probePoints = DownsampleVertices(vertices, actualSpacing);
            }

            if (spacingWasIncreased)
            {
                Debug.LogWarning(
                    $"NavMesh 初次降采样后仍超过 {MaxProbePoints} 个探测点，"
                    + $"已将最小间距从 {MinimumProbeSpacing:F2} 米提高到 {actualSpacing:F2} 米，"
                    + $"最终使用 {probePoints.Count} 个探测点，以避免编辑器卡顿。");
            }

            Transform selectedTransform = Selection.activeTransform;
            bool hasHierarchySelection = selectedTransform != null
                && selectedTransform.gameObject.scene.IsValid();
            Vector3 seedSource = hasHierarchySelection
                ? selectedTransform.position
                : probePoints[0];
            if (!NavMesh.SamplePosition(
                    seedSource,
                    out NavMeshHit seedHit,
                    SeedSampleMaxDistance,
                    NavMesh.AllAreas))
            {
                Debug.LogError(
                    $"无法把种子位置 {FormatPosition(seedSource)} 吸附到 NavMesh。"
                    + $"请在距离 NavMesh {SeedSampleMaxDistance:F1} 米内选择一个场景物体，或取消选择后重试。");
                return;
            }

            int completeCount = 0;
            var unreachableResults =
                new System.Collections.Generic.List<UnreachableResult>();
            var path = new NavMeshPath();

            for (int pointIndex = 0; pointIndex < probePoints.Count; pointIndex++)
            {
                Vector3 targetPoint = probePoints[pointIndex];
                path.ClearCorners();
                NavMesh.CalculatePath(seedHit.position, targetPoint, NavMesh.AllAreas, path);

                switch (path.status)
                {
                    case NavMeshPathStatus.PathComplete:
                        completeCount++;
                        break;

                    case NavMeshPathStatus.PathPartial:
                        PartialPoints.Add(targetPoint);
                        unreachableResults.Add(
                            new UnreachableResult(targetPoint, NavMeshPathStatus.PathPartial));
                        break;

                    default:
                        InvalidPoints.Add(targetPoint);
                        unreachableResults.Add(
                            new UnreachableResult(targetPoint, NavMeshPathStatus.PathInvalid));
                        break;
                }
            }

            _seedPoint = seedHit.position;
            _checkedSceneHandle = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene()
                .handle
                .GetRawData();
            _hasMarkers = true;
            RegisterMarkerCallbacks();
            SceneView.RepaintAll();

            Debug.Log(BuildReport(
                probePoints.Count,
                completeCount,
                unreachableResults,
                actualSpacing));
        }

        [MenuItem(ClearMenuPath)]
        private static void ClearConnectivityMarkers()
        {
            bool hadMarkers = _hasMarkers;
            ClearMarkerState();
            Debug.Log(hadMarkers ? "已清除 NavMesh 连通性标记。" : "当前没有 NavMesh 连通性标记。");
        }

        private static System.Collections.Generic.List<Vector3> DownsampleVertices(
            Vector3[] vertices,
            float minimumSpacing)
        {
            var probePoints = new System.Collections.Generic.List<Vector3>();
            var spatialBuckets =
                new System.Collections.Generic.Dictionary<
                    Vector3Int,
                    System.Collections.Generic.List<Vector3>>();
            float minimumSpacingSquared = minimumSpacing * minimumSpacing;

            for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                Vector3 candidate = vertices[vertexIndex];
                if (!IsFinite(candidate))
                {
                    continue;
                }

                Vector3Int cell = GetCell(candidate, minimumSpacing);
                if (HasNearbyPoint(
                        candidate,
                        cell,
                        minimumSpacingSquared,
                        spatialBuckets))
                {
                    continue;
                }

                probePoints.Add(candidate);
                if (!spatialBuckets.TryGetValue(
                        cell,
                        out System.Collections.Generic.List<Vector3> bucket))
                {
                    bucket = new System.Collections.Generic.List<Vector3>();
                    spatialBuckets.Add(cell, bucket);
                }

                bucket.Add(candidate);
            }

            return probePoints;
        }

        private static bool HasNearbyPoint(
            Vector3 candidate,
            Vector3Int cell,
            float minimumSpacingSquared,
            System.Collections.Generic.Dictionary<
                Vector3Int,
                System.Collections.Generic.List<Vector3>> spatialBuckets)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    for (int zOffset = -1; zOffset <= 1; zOffset++)
                    {
                        var neighbourCell = new Vector3Int(
                            cell.x + xOffset,
                            cell.y + yOffset,
                            cell.z + zOffset);
                        if (!spatialBuckets.TryGetValue(
                                neighbourCell,
                                out System.Collections.Generic.List<Vector3> bucket))
                        {
                            continue;
                        }

                        for (int pointIndex = 0; pointIndex < bucket.Count; pointIndex++)
                        {
                            if ((bucket[pointIndex] - candidate).sqrMagnitude
                                < minimumSpacingSquared)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static Vector3Int GetCell(Vector3 point, float cellSize)
        {
            return new Vector3Int(
                Mathf.FloorToInt(point.x / cellSize),
                Mathf.FloorToInt(point.y / cellSize),
                Mathf.FloorToInt(point.z / cellSize));
        }

        private static bool IsFinite(Vector3 point)
        {
            return !float.IsNaN(point.x)
                && !float.IsInfinity(point.x)
                && !float.IsNaN(point.y)
                && !float.IsInfinity(point.y)
                && !float.IsNaN(point.z)
                && !float.IsInfinity(point.z);
        }

        private static string BuildReport(
            int totalCount,
            int completeCount,
            System.Collections.Generic.List<UnreachableResult> unreachableResults,
            float actualSpacing)
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("NavMesh 连通性检查完成");
            report.AppendLine($"种子点：{FormatPosition(_seedPoint)}");
            report.AppendLine($"实际降采样最小间距：{actualSpacing:F2} 米");
            report.AppendLine($"探测点总数：{totalCount}");
            report.AppendLine($"可达数：{completeCount}");
            report.AppendLine(
                $"不可达数：{unreachableResults.Count}"
                + $"（PathPartial 半通：{PartialPoints.Count}，PathInvalid 无效：{InvalidPoints.Count}）");

            if (unreachableResults.Count == 0)
            {
                report.Append("全部连通。");
                return report.ToString();
            }

            int listedCount = Mathf.Min(
                unreachableResults.Count,
                MaxReportedUnreachablePoints);
            report.AppendLine($"不可达点世界坐标（最多列出 {MaxReportedUnreachablePoints} 个）：");
            for (int resultIndex = 0; resultIndex < listedCount; resultIndex++)
            {
                UnreachableResult result = unreachableResults[resultIndex];
                string statusName = result.Status == NavMeshPathStatus.PathPartial
                    ? "PathPartial 半通"
                    : "PathInvalid 无效";
                report.AppendLine(
                    $"{resultIndex + 1}. [{statusName}] {FormatPosition(result.Position)}");
            }

            if (unreachableResults.Count > listedCount)
            {
                report.AppendLine(
                    $"另有 {unreachableResults.Count - listedCount} 个不可达点未列出；"
                    + $"不可达总数仍为 {unreachableResults.Count}。");
            }

            int visibleMarkerCount = Mathf.Min(unreachableResults.Count, MaxSceneMarkers);
            report.Append(
                $"场景视图最多绘制 {MaxSceneMarkers} 个不可达标记，"
                + $"本次绘制 {visibleMarkerCount} 个（黄色优先）。");
            return report.ToString();
        }

        private static string FormatPosition(Vector3 position)
        {
            return $"({position.x:F2}, {position.y:F2}, {position.z:F2})";
        }

        private static void RegisterMarkerCallbacks()
        {
            SceneView.duringSceneGui -= DrawSceneMarkers;
            SceneView.duringSceneGui += DrawSceneMarkers;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -=
                OnActiveSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode +=
                OnActiveSceneChanged;
        }

        private static void UnregisterMarkerCallbacks()
        {
            SceneView.duringSceneGui -= DrawSceneMarkers;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -=
                OnActiveSceneChanged;
        }

        private static void OnActiveSceneChanged(
            UnityEngine.SceneManagement.Scene previousScene,
            UnityEngine.SceneManagement.Scene nextScene)
        {
            ClearMarkerState();
        }

        private static void ClearMarkerState()
        {
            UnregisterMarkerCallbacks();
            PartialPoints.Clear();
            InvalidPoints.Clear();
            _seedPoint = Vector3.zero;
            _checkedSceneHandle = 0;
            _hasMarkers = false;
            SceneView.RepaintAll();
        }

        private static void DrawSceneMarkers(SceneView sceneView)
        {
            if (!_hasMarkers)
            {
                return;
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                    .handle
                    .GetRawData()
                != _checkedSceneHandle)
            {
                ClearMarkerState();
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color previousColor = Handles.color;
            try
            {
                DrawSeedMarker();

                int remainingMarkerCount = MaxSceneMarkers;
                remainingMarkerCount -= DrawPointMarkers(
                    PartialPoints,
                    Color.yellow,
                    remainingMarkerCount);
                DrawPointMarkers(InvalidPoints, Color.red, remainingMarkerCount);
            }
            finally
            {
                Handles.color = previousColor;
            }
        }

        private static void DrawSeedMarker()
        {
            float handleSize = HandleUtility.GetHandleSize(_seedPoint);
            float markerSize = handleSize * SeedMarkerScale;
            Handles.color = Color.cyan;
            Handles.SphereHandleCap(
                0,
                _seedPoint,
                Quaternion.identity,
                markerSize,
                EventType.Repaint);
            Handles.DrawWireDisc(_seedPoint, Vector3.up, markerSize);
            Handles.Label(
                _seedPoint + Vector3.up * handleSize * SeedLabelHeightScale,
                "连通性种子");
        }

        private static int DrawPointMarkers(
            System.Collections.Generic.List<Vector3> points,
            Color color,
            int markerLimit)
        {
            int drawCount = Mathf.Min(points.Count, markerLimit);
            Handles.color = color;

            for (int pointIndex = 0; pointIndex < drawCount; pointIndex++)
            {
                Vector3 point = points[pointIndex];
                float markerSize = HandleUtility.GetHandleSize(point) * MarkerScale;
                Handles.SphereHandleCap(
                    0,
                    point,
                    Quaternion.identity,
                    markerSize,
                    EventType.Repaint);
            }

            return drawCount;
        }

        private readonly struct UnreachableResult
        {
            public UnreachableResult(Vector3 position, NavMeshPathStatus status)
            {
                Position = position;
                Status = status;
            }

            public Vector3 Position { get; }

            public NavMeshPathStatus Status { get; }
        }
    }
}
