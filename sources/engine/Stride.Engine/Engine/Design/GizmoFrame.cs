// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Engine.Design
{
    /// <summary>
    /// What an entity's rotation is relative to, where a processor reads it as relative to more than its parent: such
    /// as a point a road runs through, rotated from the way the road would run there on its own.
    /// </summary>
    /// <remarks>
    /// A processor running in the editor sets <see cref="BaseRotationKey"/> on such an entity, a rotation in its parent's
    /// space applied after its own. The scene editor's transform gizmos, in local space, then show and turn the entity in
    /// that frame (its own rotation, then this), so the gizmo lines up with what the rotation does.
    /// </remarks>
    public static class GizmoFrame
    {
        /// <summary>The rotation, in its parent's space, an entity's own rotation is relative to.</summary>
        public static readonly PropertyKey<Quaternion> BaseRotationKey = new PropertyKey<Quaternion>("GizmoFrame.BaseRotation", typeof(GizmoFrame), DefaultValueMetadata.Static(Quaternion.Identity));

        /// <summary>The rotation an entity's own rotation is relative to: identity unless a processor set one.</summary>
        public static Quaternion BaseRotation(Entity entity) => entity.Tags.Get(BaseRotationKey);
    }
}
