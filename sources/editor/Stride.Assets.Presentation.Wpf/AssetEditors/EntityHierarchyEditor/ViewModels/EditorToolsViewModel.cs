// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    /// <summary>
    /// What services add to the scene editor's viewport toolbar: a button per tool of each <see cref="IEditorGameToolService"/>,
    /// then the controls of each <see cref="IEditorGameToolbarService"/>.
    /// </summary>
    public class EditorToolsViewModel : DispatcherViewModel
    {
        private static readonly ILogger Logger = GlobalLogger.GetLogger("Scene");

        private readonly ObservableList<EditorToolViewModel> tools = new ObservableList<EditorToolViewModel>();
        private readonly ObservableList<FrameworkElement> toolbarItems = new ObservableList<FrameworkElement>();

        public EditorToolsViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller)
            : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            Dispatcher.InvokeTask(async () =>
            {
                await controller.GameContentLoaded;
                foreach (var service in controller.GetServices<IEditorGameToolService>())
                {
                    foreach (var tool in service.Tools)
                        tools.Add(new EditorToolViewModel(ServiceProvider, tool));
                }
                foreach (var service in controller.GetServices<IEditorGameToolbarService>())
                {
                    // A plugin's controls failing to build must not take the toolbar with them
                    try
                    {
                        toolbarItems.AddRange(service.CreateToolbarItems());
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Unable to create the toolbar controls of [{service.GetType().Name}]", e);
                    }
                }
            });
        }

        public IReadOnlyObservableList<EditorToolViewModel> Tools => tools;

        /// <summary>
        /// The controls of the <see cref="IEditorGameToolbarService"/>s, shown as they are.
        /// </summary>
        public IReadOnlyObservableList<FrameworkElement> ToolbarItems => toolbarItems;
    }

    /// <summary>
    /// One tool button.
    /// </summary>
    public class EditorToolViewModel : DispatcherViewModel
    {
        private readonly EditorTool tool;
        private bool isActive;
        private bool isAvailable;

        public EditorToolViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] EditorTool tool)
            : base(serviceProvider)
        {
            this.tool = tool ?? throw new ArgumentNullException(nameof(tool));
            isActive = tool.IsActive;
            isAvailable = tool.IsAvailable;
            Icon = LoadIcon(tool.Icon);
            // Turned on or off elsewhere (a shortcut), or now (un)available: show it, without turning it again
            tool.Changed += (sender, e) => Dispatcher.InvokeAsync(() =>
            {
                SetValue(ref isActive, tool.IsActive, nameof(IsActive));
                SetValue(ref isAvailable, tool.IsAvailable, nameof(IsAvailable));
            });
        }

        /// <summary>
        /// Whether the tool can be used now: its button shows only then.
        /// </summary>
        public bool IsAvailable => isAvailable;

        public string DisplayName => tool.DisplayName;

        [CanBeNull]
        public ImageSource Icon { get; }

        public bool HasIcon => Icon != null;

        public bool IsActive
        {
            get => isActive;
            set => SetValue(ref isActive, value, () => tool.SetActive(value));
        }

        private static ImageSource LoadIcon(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = new MemoryStream(data);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
