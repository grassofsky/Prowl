// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

namespace Prowl.Runtime.Rendering;

public static class MotionVectorTracker
{
    private static readonly ConcurrentDictionary<int, Matrix4x4> s_prevModelMatrices = new();
    private static readonly ConcurrentBag<int> s_activeObjectIds = new();
    private const int CLEANUP_INTERVAL_FRAMES = 120;
    private static int s_framesSinceLastCleanup = 0;
    private static readonly object s_cleanupLock = new();

    public static void TrackModelMatrix(CommandBuffer buffer, int objectId, Matrix4x4 currentModel)
    {
        s_activeObjectIds.Add(objectId);

        if (s_prevModelMatrices.TryGetValue(objectId, out Matrix4x4 prevModel))
            buffer.SetMatrix("prowl_PrevObjectToWorld", prevModel.ToFloat());
        else
            buffer.SetMatrix("prowl_PrevObjectToWorld", currentModel.ToFloat());

        s_prevModelMatrices[objectId] = currentModel;
    }

    public static void CleanupUnusedModelMatrices()
    {
        lock (s_cleanupLock)
        {
            s_framesSinceLastCleanup++;

            if (s_framesSinceLastCleanup < CLEANUP_INTERVAL_FRAMES)
                return;

            s_framesSinceLastCleanup = 0;

            var activeSet = new HashSet<int>(s_activeObjectIds);
            var unusedKeys = new List<int>();
            foreach (var key in s_prevModelMatrices.Keys)
            {
                if (!activeSet.Contains(key))
                    unusedKeys.Add(key);
            }

            foreach (int key in unusedKeys)
                s_prevModelMatrices.TryRemove(key, out _);

            s_activeObjectIds.Clear();
        }
    }
}
