// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    /// <summary>
    /// An editor game service that puts controls of its own (buttons, drop-downs, ...) in the scene editor's viewport
    /// toolbar, after the tool buttons. Declare it with <c>[EditorGameService]</c>, from a plugin too.
    /// </summary>
    /// <remarks>
    /// The scene editor offers three ways to add to its viewport toolbar, from the simplest to the most open:
    /// <list type="bullet">
    /// <item><see cref="IEditorGameToolService"/>: a toggle button per tool, kept in step with the tool (for modes).</item>
    /// <item><see cref="IEditorGameOverlayService"/>: a checkbox per overlay in the Overlays popup (for things a service draws);
    /// it can't follow a change made other than through its checkbox.</item>
    /// <item>This: any WPF controls, which the service makes and keeps up to date itself.</item>
    /// </list>
    /// </remarks>
    public interface IEditorGameToolbarService : IEditorGameViewModelService
    {
        /// <summary>
        /// Makes the controls to show in the toolbar. Called once, on the UI thread, once the editor's game content has loaded;
        /// they stay until the editor closes. Changes they make to the game go through the service, which marshals to the game
        /// thread itself.
        /// </summary>
        [NotNull, ItemNotNull]
        IEnumerable<FrameworkElement> CreateToolbarItems();
    }
}
