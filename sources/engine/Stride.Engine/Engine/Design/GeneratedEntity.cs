// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Engine.Design
{
    /// <summary>
    /// Entities a processor makes for another entity, such as a mesh it builds, rather than ones loaded from a scene.
    /// </summary>
    /// <remarks>
    /// A processor running in the editor (<see cref="ExecutionMode.Editor"/>) must not add entities under an entity of
    /// the scene being edited: the editor keeps that scene's entities and children in step with the asset by position,
    /// so an extra child or root entity puts them out of step. It adds them to <see cref="SceneInstance.RootScene"/>
    /// instead, and sets <see cref="OwnerKey"/> on them so the editor selects the owner when one is clicked.
    /// </remarks>
    public static class GeneratedEntity
    {
        /// <summary>
        /// The entity a generated entity was made for and stands for: clicking it in the scene editor selects this one.
        /// </summary>
        public static readonly PropertyKey<Entity> OwnerKey = new PropertyKey<Entity>("GeneratedEntity.Owner", typeof(GeneratedEntity));
    }
}
