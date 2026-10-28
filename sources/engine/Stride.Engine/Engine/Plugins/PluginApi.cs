// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

namespace Stride.Engine.Plugins;

/// <summary>
/// Functions plugins offer each other by name, so one plugin can use another's when it is there, without referencing it.
/// A plugin registers its functions as delegates (of engine and .NET types) when it loads; another looks one up when it
/// needs it, and does without when it is not found (its plugin not in the project, or not loaded). A plugin's functions
/// go when its assembly is unregistered.
/// </summary>
/// <example>
/// <code>
/// // In the terrain plugin, as it loads:
/// PluginApi.Register("Terrain.HeightAt", (Func&lt;EntityManager, Vector3, float&gt;)HeightAt);
///
/// // In the roads plugin:
/// if (PluginApi.TryGet&lt;Func&lt;EntityManager, Vector3, float&gt;&gt;("Terrain.HeightAt", out var heightAt))
///     point.Y = heightAt(EntityManager, point);
/// </code>
/// </example>
public static class PluginApi
{
    private static readonly Logger Log = GlobalLogger.GetLogger(nameof(PluginApi));
    private static readonly Lock FunctionsLock = new();
    private static readonly Dictionary<string, (Delegate Function, Assembly? Owner)> Functions = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Mismatched = new(StringComparer.Ordinal);

    static PluginApi()
    {
        AssemblyRegistry.AssemblyUnregistered += (_, e) => UnregisterAll(e.Assembly);
    }

    /// <summary>Raised when a function is registered or goes (with its name), for those holding on to one to look it up again.</summary>
    public static event Action<string>? Changed;

    /// <summary>
    /// Offers a function by name (by convention "Plugin.Function"), in place of any registered by that name before.
    /// It goes when the assembly that defines it is unregistered.
    /// </summary>
    public static void Register(string name, Delegate function)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(function);
        lock (FunctionsLock)
        {
            Functions[name] = (function, function.Method.DeclaringType?.Assembly);
            Mismatched.Remove(name);
        }
        Changed?.Invoke(name);
    }

    /// <summary>Takes a function back.</summary>
    public static bool Unregister(string name)
    {
        bool removed;
        lock (FunctionsLock)
            removed = Functions.Remove(name);
        if (removed)
            Changed?.Invoke(name);
        return removed;
    }

    /// <summary>
    /// The function registered by this name, if there is one of this type. One by the name but of another type is not
    /// given, and is told of (once) as a warning naming both, as its plugin and its user disagree on what it is.
    /// </summary>
    public static bool TryGet<T>(string name, out T function) where T : Delegate
    {
        (Delegate Function, Assembly? Owner) found;
        lock (FunctionsLock)
        {
            if (!Functions.TryGetValue(name, out found))
            {
                function = null!;
                return false;
            }
        }
        if (found.Function is T typed)
        {
            function = typed;
            return true;
        }
        bool first;
        lock (FunctionsLock)
            first = Mismatched.Add(name);
        if (first)
            Log.Warning($"Plugin function [{name}] is a {found.Function.GetType()}, not the {typeof(T)} asked for: not used.");
        function = null!;
        return false;
    }

    /// <summary>Whether a function is registered by this name (of any type).</summary>
    public static bool Has(string name)
    {
        lock (FunctionsLock)
            return Functions.ContainsKey(name);
    }

    /// <summary>The names of the functions registered, as of now.</summary>
    public static IReadOnlyList<string> Names
    {
        get
        {
            lock (FunctionsLock)
                return Functions.Keys.OrderBy(n => n, StringComparer.Ordinal).ToList();
        }
    }

    private static void UnregisterAll(Assembly assembly)
    {
        List<string> gone;
        lock (FunctionsLock)
        {
            gone = Functions.Where(f => f.Value.Owner == assembly).Select(f => f.Key).ToList();
            foreach (var name in gone)
                Functions.Remove(name);
        }
        foreach (var name in gone)
            Changed?.Invoke(name);
    }
}
