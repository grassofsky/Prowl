// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using Prowl.Echo;
using Prowl.Runtime;
using Prowl.Runtime.SceneManagement;
using Prowl.Runtime.Utils;
using Prowl.Editor;

namespace Prowl.Editor.Assets;

public static class AssemblyReplacer
{
    private static bool _isHotReloading;
    public static bool IsHotReloading => _isHotReloading;

    private static readonly ConcurrentDictionary<string, Type> _typeCache = new();
    private static readonly Dictionary<string, Dictionary<string, object?>> _staticFields = new();
    private static readonly List<SerializedGameObject> _serializedGameObjects = new();
    private static byte[]? _serializedEditorState;

    public static bool TryHotReload()
    {
        if (_isHotReloading)
        {
            Debug.LogWarning("[HotReload] Already in progress");
            return false;
        }

        _isHotReloading = true;
        Debug.Log("[HotReload] Starting hot reload process...");

        try
        {
            Debug.Log("[HotReload] Step 1/5: Storing scene...");
            SceneManager.StoreScene();
            Debug.Log("[HotReload] Scene stored successfully");

            Debug.Log("[HotReload] Step 2/5: Capturing static fields...");
            CaptureStaticFields();
            Debug.Log("[HotReload] Static fields captured");

            Debug.Log("[HotReload] Step 3/5: Serializing editor state...");
            _serializedEditorState = EditorStateSerializer.SerializeState();
            Debug.Log("[HotReload] Editor state serialized");

            Debug.Log("[HotReload] Step 4/5: Unloading assemblies...");
            AssemblyManager.Unload();
            ClearTypeCache(); // Clear type cache after unloading old assemblies
            Debug.Log("[HotReload] Assemblies unloaded");

            Debug.Log("[HotReload] Step 5/5: Loading new assemblies...");
            if (!LoadNewAssemblies())
            {
                Debug.LogWarning("[HotReload] Failed to load new assemblies, falling back to restart");
                return false;
            }
            Debug.Log("[HotReload] New assemblies loaded");

            Debug.Log("[HotReload] Restoring scene from stored state...");
            bool sceneRestored = false;
            try
            {
                SceneManager.RestoreScene();
                sceneRestored = true;
            }
            catch (Exception sceneEx)
            {
                Debug.LogException(new Exception("[HotReload] Failed to restore scene", sceneEx));
            }
            finally
            {
                if (sceneRestored)
                {
                    SceneManager.ClearStoredScene();
                    Debug.Log("[HotReload] Scene restored");
                }
                else
                {
                    Debug.LogWarning("[HotReload] Scene restore failed, stored scene data preserved");
                }
            }

            RestoreStaticFields();
            Debug.Log("[HotReload] Static fields restored");

            if (_serializedEditorState != null)
            {
                EditorStateSerializer.DeserializeState(_serializedEditorState);
                Debug.Log("[HotReload] Editor state deserialized");
            }

            Debug.LogSuccess("[HotReload] Hot reload completed successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(new Exception("[HotReload] Hot reload failed", ex));
            return false;
        }
        finally
        {
            _isHotReloading = false;
            ClearTempData();
        }
    }

    private static void CaptureStaticFields()
    {
        _staticFields.Clear();

        foreach (var assembly in AssemblyManager.ExternalAssemblies)
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type == null) continue;

                var typeKey = type.FullName!;
                _staticFields[typeKey] = new Dictionary<string, object?>();

                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.IsLiteral) continue;
                    if (field.IsInitOnly) continue;
                    if (field.IsDefined(typeof(CompilerGeneratedAttribute))) continue;

                    try
                    {
                        var value = field.GetValue(null);
                        _staticFields[typeKey][field.Name] = SerializeValue(value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[HotReload] Failed to capture static field {type.Name}.{field.Name}: {ex.Message}");
                    }
                }
            }
        }
    }

    private static void RestoreStaticFields()
    {
        foreach (var assembly in AssemblyManager.ExternalAssemblies)
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type == null) continue;

                var typeKey = type.FullName!;

                if (!_staticFields.TryGetValue(typeKey, out var fields))
                    continue;

                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.IsLiteral) continue;
                    if (field.IsInitOnly) continue;

                    if (!fields.TryGetValue(field.Name, out var serializedValue))
                        continue;

                    try
                    {
                        var value = DeserializeValue(serializedValue, field.FieldType);
                        field.SetValue(null, value);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[HotReload] Failed to restore static field {type.Name}.{field.Name}: {ex.Message}");
                    }
                }
            }
        }
    }

    private static bool SerializeAllGameObjects()
    {
        _serializedGameObjects.Clear();

        var sceneRef = SceneManager.Current;
        var scene = sceneRef.Res;
        if (scene == null)
        {
            Debug.LogWarning("[HotReload] No active scene to serialize");
            return true;
        }

        int count = 0;
        foreach (var go in scene.AllObjects)
        {
            try
            {
                var serializedGO = SerializeGameObject(go);
                _serializedGameObjects.Add(serializedGO);
                count++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HotReload] Failed to serialize GameObject '{go.Name}': {ex.Message}");
            }
        }

        Debug.Log($"[HotReload] Serialized {count} GameObjects");
        return true;
    }

    private static SerializedGameObject SerializeGameObject(GameObject go)
    {
        var serialized = new SerializedGameObject
        {
            Name = go.Name,
            Identifier = go.Identifier,
            ParentIdentifier = go.parent?.Identifier,
            IsActive = go.enabledInHierarchy,
            Transform = SerializeTransform(go.Transform),
            Components = new List<SerializedComponent>()
        };

        foreach (var component in go.GetComponents<MonoBehaviour>())
        {
            var serializedComp = SerializeComponent(component);
            serialized.Components.Add(serializedComp);
        }

        return serialized;
    }

    private static SerializedTransform SerializeTransform(Transform transform)
    {
        return new SerializedTransform
        {
            LocalPosition = transform.localPosition,
            LocalRotation = transform.localRotation,
            LocalScale = transform.localScale
        };
    }

    private static SerializedComponent SerializeComponent(MonoBehaviour component)
    {
        var type = component.GetType();
        var serialized = new SerializedComponent
        {
            TypeName = type.FullName!,
            Identifier = component.Identifier,
            Enabled = component.Enabled,
            FieldValues = new Dictionary<string, object?>()
        };

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            if (field.IsLiteral || field.IsInitOnly) continue;
            if (field.IsDefined(typeof(SerializeIgnoreAttribute))) continue;
            if (!field.IsPublic && !field.IsDefined(typeof(SerializeFieldAttribute))) continue;

            try
            {
                var value = field.GetValue(component);
                serialized.FieldValues[field.Name] = SerializeValue(value);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HotReload] Failed to serialize field {type.Name}.{field.Name}: {ex.Message}");
            }
        }

        return serialized;
    }

    private static bool RestoreAllGameObjects()
    {
        var sceneRef = SceneManager.Current;
        var scene = sceneRef.Res;
        if (scene == null)
        {
            Debug.LogWarning("[HotReload] No active scene to restore");
            return false;
        }

        var gameObjectMap = new Dictionary<Guid, GameObject>();

        Debug.Log($"[HotReload] Restoring {_serializedGameObjects.Count} GameObjects...");

        // First pass: Create all GameObjects
        foreach (var serializedGO in _serializedGameObjects)
        {
            var go = new GameObject(serializedGO.Name);
            SetGameObjectIdentifier(go, serializedGO.Identifier);
            SetGameObjectEnabledInHierarchy(go, serializedGO.IsActive);

            gameObjectMap[serializedGO.Identifier] = go;
        }

        int rootCount = 0;
        int childCount = 0;

        // Second pass: Set up hierarchy and add to scene
        foreach (var serializedGO in _serializedGameObjects)
        {
            if (!gameObjectMap.TryGetValue(serializedGO.Identifier, out var go))
                continue;

            if (serializedGO.ParentIdentifier.HasValue && 
                gameObjectMap.TryGetValue(serializedGO.ParentIdentifier.Value, out var parent))
            {
                go.SetParent(parent);
                childCount++;
            }
            else
            {
                scene.Add(go);
                rootCount++;
            }

            RestoreTransform(go.Transform, serializedGO.Transform);

            foreach (var serializedComp in serializedGO.Components)
            {
                RestoreComponent(go, serializedComp);
            }
        }

        Debug.Log($"[HotReload] Restored {rootCount} root and {childCount} child GameObjects");
        return true;
    }

    private static void SetGameObjectIdentifier(GameObject go, Guid identifier)
    {
        var field = typeof(GameObject).GetField("_identifier", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(go, identifier);
    }

    private static void SetGameObjectEnabledInHierarchy(GameObject go, bool enabled)
    {
        var field = typeof(GameObject).GetField("_enabledInHierarchy", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(go, enabled);
    }

    private static void RestoreTransform(Transform transform, SerializedTransform serialized)
    {
        transform.localPosition = serialized.LocalPosition;
        transform.localRotation = serialized.LocalRotation;
        transform.localScale = serialized.LocalScale;
    }

    private static void RestoreComponent(GameObject go, SerializedComponent serialized)
    {
        var type = FindTypeInExternalAssemblies(serialized.TypeName);
        if (type == null)
        {
            Debug.LogWarning($"[HotReload] Type not found: {serialized.TypeName}");
            return;
        }

        var component = Activator.CreateInstance(type) as MonoBehaviour;
        if (component == null)
        {
            Debug.LogWarning($"[HotReload] Failed to create instance of {serialized.TypeName}");
            return;
        }

        SetComponentIdentifier(component, serialized.Identifier);
        component.Enabled = serialized.Enabled;

        AddComponentToGameObject(go, component);

        foreach (var kvp in serialized.FieldValues)
        {
            var field = type.GetField(kvp.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (field == null)
            {
                field = TypeMigrationHelper.FindFieldByFormerName(type, kvp.Key);
            }

            if (field == null) continue;

            try
            {
                var value = DeserializeValue(kvp.Value, field.FieldType);
                field.SetValue(component, value);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HotReload] Failed to restore field {type.Name}.{kvp.Key}: {ex.Message}");
            }
        }
    }

    private static void SetComponentIdentifier(MonoBehaviour component, Guid identifier)
    {
        var field = typeof(MonoBehaviour).GetField("_identifier", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(component, identifier);
    }

    private static void AddComponentToGameObject(GameObject go, MonoBehaviour component)
    {
        var componentsField = typeof(GameObject).GetField("_components", BindingFlags.Instance | BindingFlags.NonPublic);
        if (componentsField != null)
        {
            var list = componentsField.GetValue(go) as List<MonoBehaviour>;
            list?.Add(component);
        }

        var attachMethod = typeof(MonoBehaviour).GetMethod("AttachToGameObject", BindingFlags.Instance | BindingFlags.NonPublic);
        attachMethod?.Invoke(component, new object[] { go });
    }

    private static bool LoadNewAssemblies()
    {
        var active = Project.Active;
        if (active == null)
        {
            Debug.LogWarning("[HotReload] No active project");
            return false;
        }

        try
        {
            DirectoryInfo temp = active.TempDirectory;
            
            // Use timestamp-based unique directory to avoid file locking issues
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            DirectoryInfo bin = new DirectoryInfo(Path.Combine(temp.FullName, "bin_hotreload_" + timestamp));
            DirectoryInfo tmpProject = new DirectoryInfo(Path.Combine(temp.FullName, "obj_hotreload_" + timestamp, Project.GameCSProjectName));
            DirectoryInfo tmpEditor = new DirectoryInfo(Path.Combine(temp.FullName, "obj_hotreload_" + timestamp, Project.EditorCSProjectName));

            string projectOutputPath = Path.Combine(bin.FullName, Project.GameCSProjectName + ".dll");
            string editorOutputPath = Path.Combine(bin.FullName, Project.EditorCSProjectName + ".dll");

            Debug.Log($"[HotReload] Game project name: {Project.GameCSProjectName}");
            Debug.Log($"[HotReload] Editor project name: {Project.EditorCSProjectName}");
            Debug.Log($"[HotReload] Compiling to: {projectOutputPath}");
            Debug.Log($"[HotReload] Editor assembly: {editorOutputPath}");

            // Create new directories (no need to delete old ones)
            bin.Create();
            tmpProject.Create();
            tmpEditor.Create();

            DotnetCompileOptions options = new DotnetCompileOptions()
            {
                isRelease = false,
                isSelfContained = false,
                outputPath = bin,
                tempPath = tmpProject
            };

            active.GenerateGameProject();
            Debug.Log("[HotReload] Generated game project");
            
            active.CompileGameAssembly(options);
            Debug.Log("[HotReload] Compiled game assembly");

            if (!File.Exists(projectOutputPath))
            {
                Debug.LogError($"[HotReload] Game assembly not found at: {projectOutputPath}");
                Debug.LogError($"[HotReload] Directory exists: {bin.Exists}");
                if (bin.Exists)
                {
                    var files = bin.GetFiles("*.dll");
                    Debug.LogError($"[HotReload] Files in directory: {files.Length}");
                    foreach (var file in files)
                        Debug.LogError($"[HotReload] File: {file.Name}");
                }
                return false;
            }
            Debug.Log("[HotReload] Found game assembly");

            Assembly? gameAssembly = AssemblyManager.LoadExternalAssembly(projectOutputPath, true);
            if (gameAssembly == null)
            {
                Debug.LogWarning("[HotReload] Failed to load game assembly");
                return false;
            }

            options.outputPath = bin;
            options.tempPath = tmpEditor;

            active.GenerateEditorProject(gameAssembly);
            active.CompileEditorAssembly(options);
            Debug.Log("[HotReload] Compiled editor assembly");

            if (!File.Exists(editorOutputPath))
            {
                Debug.LogError($"[HotReload] Editor assembly not found at: {editorOutputPath}");
                Debug.LogError($"[HotReload] Directory exists: {bin.Exists}");
                if (bin.Exists)
                {
                    var files = bin.GetFiles("*.dll");
                    Debug.LogError($"[HotReload] Files in directory: {files.Length}");
                    foreach (var file in files)
                        Debug.LogError($"[HotReload] File: {file.Name}");
                }
                return false;
            }

            Assembly? editorAssembly = AssemblyManager.LoadExternalAssembly(editorOutputPath, true);
            if (editorAssembly == null)
            {
                Debug.LogWarning("[HotReload] Failed to load editor assembly");
                return false;
            }

            AssemblyManager.Initialize();
            AssemblyMethodAttributeBase.FindAll();
            OnAssemblyLoadAttribute.Invoke();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(new Exception("[HotReload] Error loading new assemblies", ex));
            return false;
        }
    }

    private static object? SerializeValue(object? value)
    {
        if (value == null) return null;

        var type = value.GetType();

        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            return value;

        if (type == typeof(Guid))
            return ((Guid)value).ToString();

        if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
            type == typeof(Quaternion) || type == typeof(Color) || type == typeof(Matrix4x4))
            return value;

        if (value is EngineObject engineObj)
        {
            Guid identifier = Guid.Empty;
            if (engineObj is GameObject go)
                identifier = go.Identifier;
            else if (engineObj is MonoBehaviour mb)
                identifier = mb.Identifier;

            return new SerializedReference
            {
                Type = SerializedReferenceType.EngineObject,
                Identifier = identifier,
                TypeName = type.FullName!
            };
        }

        if (value is Enum)
            return Convert.ToInt32(value);

        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
        {
            return SerializeCollection(value);
        }

        return new SerializedCustomObject
        {
            TypeName = type.FullName!,
            Data = SerializeCustomObject(value)
        };
    }

    private static object? DeserializeValue(object? serialized, Type targetType)
    {
        if (serialized == null) return GetDefaultValue(targetType);

        if (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(decimal))
        {
            if (serialized.GetType() == targetType)
                return serialized;
            return Convert.ChangeType(serialized, targetType);
        }

        if (targetType == typeof(Guid) && serialized is string guidStr)
            return Guid.Parse(guidStr);

        if (targetType == typeof(Vector2) && serialized is Vector2 v2) return v2;
        if (targetType == typeof(Vector3) && serialized is Vector3 v3) return v3;
        if (targetType == typeof(Vector4) && serialized is Vector4 v4) return v4;
        if (targetType == typeof(Quaternion) && serialized is Quaternion q) return q;
        if (targetType == typeof(Color) && serialized is Color c) return c;
        if (targetType == typeof(Matrix4x4) && serialized is Matrix4x4 m) return m;

        if (targetType.IsEnum && serialized is int enumValue)
            return Enum.ToObject(targetType, enumValue);

        if (serialized is SerializedReference reference)
        {
            return DeserializeReference(reference, targetType);
        }

        if (serialized is SerializedCustomObject custom)
        {
            return DeserializeCustomObject(custom, targetType);
        }

        if (serialized is SerializedCollection collection)
        {
            return DeserializeCollection(collection, targetType);
        }

        return GetDefaultValue(targetType);
    }

    private static object? DeserializeReference(SerializedReference reference, Type targetType)
    {
        switch (reference.Type)
        {
            case SerializedReferenceType.EngineObject:
                if (reference.Identifier != Guid.Empty)
                    return EngineObject.FindObjectByIdentifier<EngineObject>(reference.Identifier);
                return null;

            case SerializedReferenceType.AssetRef:
                return null;

            default:
                return null;
        }
    }

    private static Dictionary<string, object?> SerializeCustomObject(object obj)
    {
        var result = new Dictionary<string, object?>();
        var type = obj.GetType();

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            if (field.IsLiteral || field.IsInitOnly) continue;
            if (field.IsDefined(typeof(SerializeIgnoreAttribute))) continue;

            try
            {
                result[field.Name] = SerializeValue(field.GetValue(obj));
            }
            catch { }
        }

        return result;
    }

    private static object? DeserializeCustomObject(SerializedCustomObject serialized, Type targetType)
    {
        var obj = Activator.CreateInstance(targetType);
        if (obj == null) return null;

        foreach (var kvp in serialized.Data)
        {
            var field = targetType.GetField(kvp.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) continue;

            try
            {
                var value = DeserializeValue(kvp.Value, field.FieldType);
                field.SetValue(obj, value);
            }
            catch { }
        }

        return obj;
    }

    private static SerializedCollection SerializeCollection(object collection)
    {
        var type = collection.GetType();
        var result = new SerializedCollection
        {
            IsArray = type.IsArray,
            ElementType = type.IsArray ? type.GetElementType()!.FullName! : type.GetGenericArguments()[0].FullName!,
            Items = new List<object?>()
        };

        if (collection is Array array)
        {
            foreach (var item in array)
                result.Items.Add(SerializeValue(item));
        }
        else if (collection is System.Collections.IList list)
        {
            foreach (var item in list)
                result.Items.Add(SerializeValue(item));
        }

        return result;
    }

    private static object? DeserializeCollection(SerializedCollection serialized, Type targetType)
    {
        var elementType = FindTypeInExternalAssemblies(serialized.ElementType) ?? typeof(object);

        if (serialized.IsArray)
        {
            var array = Array.CreateInstance(elementType, serialized.Items.Count);
            for (int i = 0; i < serialized.Items.Count; i++)
                array.SetValue(DeserializeValue(serialized.Items[i], elementType), i);
            return array;
        }
        else
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (System.Collections.IList?)Activator.CreateInstance(listType);
            if (list == null) return null;

            foreach (var item in serialized.Items)
                list.Add(DeserializeValue(item, elementType));

            return list;
        }
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    public static Type? FindTypeInExternalAssemblies(string typeName)
    {
        if (_typeCache.TryGetValue(typeName, out var cached))
            return cached;

        foreach (var assembly in AssemblyManager.ExternalAssemblies)
        {
            var type = assembly.GetType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }
        }

        return null;
    }

    private static IEnumerable<Type?> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }

    private static void ClearTempData()
    {
        _staticFields.Clear();
        _serializedGameObjects.Clear();
        _serializedEditorState = null;
    }

    public static void ClearTypeCache()
    {
        _typeCache.Clear();
    }
}

internal struct SerializedGameObject
{
    public string Name;
    public Guid Identifier;
    public Guid? ParentIdentifier;
    public bool IsActive;
    public SerializedTransform Transform;
    public List<SerializedComponent> Components;
}

internal struct SerializedTransform
{
    public Vector3 LocalPosition;
    public Quaternion LocalRotation;
    public Vector3 LocalScale;
}

internal struct SerializedComponent
{
    public string TypeName;
    public Guid Identifier;
    public bool Enabled;
    public Dictionary<string, object?> FieldValues;
}

internal enum SerializedReferenceType
{
    EngineObject,
    AssetRef
}

internal struct SerializedReference
{
    public SerializedReferenceType Type;
    public Guid Identifier;
    public string TypeName;
}

internal struct SerializedCustomObject
{
    public string TypeName;
    public Dictionary<string, object?> Data;
}

internal struct SerializedCollection
{
    public bool IsArray;
    public string ElementType;
    public List<object?> Items;
}
