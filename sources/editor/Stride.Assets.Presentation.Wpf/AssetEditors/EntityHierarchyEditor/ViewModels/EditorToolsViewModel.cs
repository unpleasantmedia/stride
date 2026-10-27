// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    /// <summary>
    /// The tool buttons of the scene editor's viewport toolbar: one per tool of each <see cref="IEditorGameToolService"/>.
    /// </summary>
    public class EditorToolsViewModel : DispatcherViewModel
    {
        private readonly ObservableList<EditorToolViewModel> tools = new ObservableList<EditorToolViewModel>();

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
            });
        }

        public IReadOnlyObservableList<EditorToolViewModel> Tools => tools;
    }

    /// <summary>
    /// One tool button.
    /// </summary>
    public class EditorToolViewModel : DispatcherViewModel
    {
        private readonly EditorTool tool;
        private bool isActive;

        public EditorToolViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] EditorTool tool)
            : base(serviceProvider)
        {
            this.tool = tool ?? throw new ArgumentNullException(nameof(tool));
            isActive = tool.IsActive;
            Icon = LoadIcon(tool.Icon);
            // Turned on or off elsewhere (a shortcut): show it, without turning it again
            tool.ActiveChanged += (sender, e) => Dispatcher.InvokeAsync(() => SetValue(ref isActive, tool.IsActive, nameof(IsActive)));
        }

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
