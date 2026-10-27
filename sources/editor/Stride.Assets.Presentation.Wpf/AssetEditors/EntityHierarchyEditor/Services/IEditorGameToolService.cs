// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    /// <summary>
    /// An editor game service with tools that can be turned on and off, shown as toggle buttons in the scene editor's
    /// viewport toolbar (as a road drawing mode). Declare it with <c>[EditorGameService]</c>, from a plugin too.
    /// </summary>
    public interface IEditorGameToolService : IEditorGameViewModelService
    {
        /// <summary>
        /// The tools of this service, each one button.
        /// </summary>
        IReadOnlyList<EditorTool> Tools { get; }
    }

    /// <summary>
    /// One tool of an <see cref="IEditorGameToolService"/>: a toggle button in the viewport toolbar.
    /// </summary>
    public sealed class EditorTool
    {
        private readonly Func<bool> isActive;
        private readonly Action<bool> setActive;
        private readonly Func<bool> isAvailable;

        /// <param name="key">Identifies the tool; unique across services.</param>
        /// <param name="displayName">The button's tooltip: the tool's name, with its shortcut if it has one.</param>
        /// <param name="icon">The button's image, as PNG data, or null for its name instead.</param>
        /// <param name="isActive">Whether the tool is on.</param>
        /// <param name="setActive">Turns the tool on or off. Called from the UI thread: the service marshals to the game thread itself.</param>
        /// <param name="isAvailable">Whether the tool can be used now (its button shows only then), or null for always. Called from the UI thread.</param>
        public EditorTool([NotNull] string key, [NotNull] string displayName, [CanBeNull] byte[] icon, [NotNull] Func<bool> isActive, [NotNull] Action<bool> setActive,
            [CanBeNull] Func<bool> isAvailable = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Icon = icon;
            this.isActive = isActive ?? throw new ArgumentNullException(nameof(isActive));
            this.setActive = setActive ?? throw new ArgumentNullException(nameof(setActive));
            this.isAvailable = isAvailable;
        }

        public string Key { get; }

        public string DisplayName { get; }

        [CanBeNull]
        public byte[] Icon { get; }

        /// <summary>
        /// Whether the tool is on.
        /// </summary>
        public bool IsActive => isActive();

        /// <summary>
        /// Whether the tool can be used now: its button shows only then (as a tool that needs something selected).
        /// </summary>
        public bool IsAvailable => isAvailable?.Invoke() ?? true;

        /// <summary>
        /// Raised when the tool turns on or off other than through its button (a shortcut, say), or becomes available or
        /// not, from any thread.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Turns the tool on or off. Called from the UI thread.
        /// </summary>
        public void SetActive(bool active) => setActive(active);

        /// <summary>
        /// Tells the toolbar the tool has turned on or off, or become available or not. Safe from any thread.
        /// </summary>
        public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
