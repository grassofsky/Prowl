// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Reflection;

using Prowl.Echo;
using Prowl.Editor.Docking;
using Prowl.Runtime;

namespace Prowl.Editor.Assets;

public static class EditorStateSerializer
{
    public static byte[]? SerializeState()
    {
        try
        {
            var state = new EditorState
            {
                DockLayout = SerializeDockLayout(),
                WindowStates = SerializeWindowStates()
            };

            var tag = Serializer.Serialize(state);
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            tag.WriteToBinary(writer);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EditorStateSerializer] Failed to serialize editor state: {ex.Message}");
            return null;
        }
    }

    public static void DeserializeState(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return;

        try
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            var tag = EchoObject.ReadFromBinary(reader);
            var state = Serializer.Deserialize<EditorState>(tag);

            if (state?.DockLayout != null)
                DeserializeDockLayout(state.DockLayout);

            if (state?.WindowStates != null)
                DeserializeWindowStates(state.WindowStates);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EditorStateSerializer] Failed to deserialize editor state: {ex.Message}");
        }
    }

    private static DockNodeState? SerializeDockLayout()
    {
        var container = EditorGuiManager.Container;
        if (container?.Root == null)
            return null;

        return SerializeDockNode(container.Root);
    }

    private static DockNodeState SerializeDockNode(DockNode node)
    {
        var state = new DockNodeState
        {
            Type = (int)node.Type,
            SplitDistance = node.SplitDistance,
            WindowNum = node.WindowNum
        };

        if (node.Type == DockNode.NodeType.Leaf)
        {
            state.WindowTypes = new List<string>();
            foreach (var window in node.LeafWindows)
            {
                state.WindowTypes.Add(window.GetType().AssemblyQualifiedName ?? window.GetType().FullName!);
            }
        }
        else
        {
            if (node.Child[0] != null)
                state.Child0 = SerializeDockNode(node.Child[0]);
            if (node.Child[1] != null)
                state.Child1 = SerializeDockNode(node.Child[1]);
        }

        return state;
    }

    private static void DeserializeDockLayout(DockNodeState state)
    {
        var container = EditorGuiManager.Container;
        if (container?.Root == null)
            return;

        DeserializeDockNode(container.Root, state);
    }

    private static void DeserializeDockNode(DockNode node, DockNodeState state)
    {
        node.Type = (DockNode.NodeType)state.Type;
        node.SplitDistance = state.SplitDistance;
        node.WindowNum = state.WindowNum;

        if (node.Type == DockNode.NodeType.Leaf)
        {
            node.LeafWindows.Clear();

            if (state.WindowTypes != null)
            {
                foreach (var windowTypeAssemblyName in state.WindowTypes)
                {
                    var windowType = FindWindowType(windowTypeAssemblyName);
                    if (windowType == null) continue;

                    var existingWindow = FindExistingWindow(windowType);
                    if (existingWindow != null)
                    {
                        node.LeafWindows.Add(existingWindow);
                    }
                }
            }
        }
        else
        {
            if (state.Child0 != null)
            {
                node.Child[0] ??= new DockNode();
                DeserializeDockNode(node.Child[0], state.Child0);
            }
            if (state.Child1 != null)
            {
                node.Child[1] ??= new DockNode();
                DeserializeDockNode(node.Child[1], state.Child1);
            }
        }
    }

    private static Type? FindWindowType(string assemblyQualifiedName)
    {
        var type = Type.GetType(assemblyQualifiedName);
        if (type != null) return type;

        var fullName = assemblyQualifiedName.Contains(",")
            ? assemblyQualifiedName.Substring(0, assemblyQualifiedName.IndexOf(',')).Trim()
            : assemblyQualifiedName;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(fullName);
            if (type != null) return type;
        }

        return null;
    }

    private static EditorWindow? FindExistingWindow(Type windowType)
    {
        foreach (var window in EditorGuiManager.Windows)
        {
            if (window.GetType() == windowType)
                return window;
        }
        return null;
    }

    private static List<WindowState> SerializeWindowStates()
    {
        var states = new List<WindowState>();

        foreach (var window in EditorGuiManager.Windows)
        {
            try
            {
                var state = new WindowState
                {
                    WindowType = window.GetType().AssemblyQualifiedName ?? window.GetType().FullName!,
                    X = window.X,
                    Y = window.Y,
                    IsDocked = window.IsDocked
                };

                window.OnSerializeState();
                states.Add(state);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EditorStateSerializer] Failed to serialize window {window.GetType().Name}: {ex.Message}");
            }
        }

        return states;
    }

    private static void DeserializeWindowStates(List<WindowState> states)
    {
        foreach (var state in states)
        {
            var windowType = FindWindowType(state.WindowType);
            if (windowType == null) continue;

            var window = FindExistingWindow(windowType);
            if (window == null) continue;

            try
            {
                window.X = state.X;
                window.Y = state.Y;
                window.OnDeserializeState();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EditorStateSerializer] Failed to deserialize window {windowType.Name}: {ex.Message}");
            }
        }
    }
}

internal class EditorState
{
    public DockNodeState? DockLayout;
    public List<WindowState>? WindowStates;
}

internal class DockNodeState
{
    public int Type;
    public double SplitDistance;
    public int WindowNum;
    public DockNodeState? Child0;
    public DockNodeState? Child1;
    public List<string>? WindowTypes;
}

internal class WindowState
{
    public string WindowType = "";
    public double X;
    public double Y;
    public bool IsDocked;
}
