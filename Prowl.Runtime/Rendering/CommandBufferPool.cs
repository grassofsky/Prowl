// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Concurrent;

namespace Prowl.Runtime.Rendering;

public static class CommandBufferPool
{
    private static readonly ConcurrentBag<CommandBuffer> bufferPool = new();

    /// <summary>Get a clean Command Buffer.</summary>
    public static CommandBuffer Get()
    {
        return Get("New Command Buffer");
    }

    /// <summary>Get a clean, named Command Buffer.</summary>
    public static CommandBuffer Get(string name)
    {
        CommandBuffer cmd;
        
        if (!bufferPool.TryTake(out cmd))
        {
            cmd = new CommandBuffer();
        }

        cmd.Name = name;
        cmd.BeginRecording();

        return cmd;
    }

    /// <summary>Release a Command Buffer.</summary>
    public static void Release(CommandBuffer buffer)
    {
        if (buffer == null)
            return;

        // Reset state without calling End() again (it was already called in SubmitCommandBuffer)
        buffer._isRecording = false;
        buffer.ResetState();
        bufferPool.Add(buffer);
    }
}
