using IVSoftware.Portable;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.ViewManagement;

namespace CheckboxTest
{
    public sealed partial class MainWindow : Window
    {
        //  <PackageReference Include="IVSoftware.Portable.WatchdogTimer" Version="1.2.1" />
        private readonly WatchdogTimer _wdtOpening = new WatchdogTimer { Interval = TimeSpan.FromSeconds(0.5) };
        public MainWindow()
        {
            InitializeComponent();
            DeleteConfirmationDialog.Opened += async(sender, e) =>
            {
                CancelButton.Focus(FocusState.Keyboard);
                for (stackPanel.Opacity = 0.0; stackPanel.Opacity < 1.0; stackPanel.Opacity += 0.05)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(0.5));
                }
            };
            DeleteDontAskCheckbox.GettingFocus += (sender, e) =>
            {
                Debug.WriteLine($"Running={_wdtOpening.Running}");
                e.Cancel = e.Handled = _wdtOpening.Running;
            };
            Content.KeyUp += async(sender, e) =>
            {
                if (Equals(e.Key, VirtualKey.Delete))
                {
                    if (_doNotAsk)
                    {
                        await DoDeleteOperation();
                    }
                    else
                    {
                        if (!_isShowing)    // avoid reentry (Delete key while already open)
                        {
                            _isShowing = true;
                            _wdtOpening.StartOrRestart();
                            _wdtOpening.StartOrRestart();
                            if (Equals(await DeleteConfirmationDialog.ShowAsync(), ContentDialogResult.Primary))
                            {
                                await DoDeleteOperation();
                            }
                        }
                        _doNotAsk = DeleteDontAskCheckbox.IsChecked == true;
                        _isShowing = false;
                    }
                }
            };
        }
        bool _isShowing;
        bool _doNotAsk;
        private async Task DoDeleteOperation()
        {
            await new ContentDialog
            {
                Title = "Deleted",
                Content = "The item has been successfully deleted.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot // Required in WinUI3
            }.ShowAsync();
        }
        public Button CancelButton
        {
            get
            {
                if (_cancelButton is null)
                {
                    _cancelButton =
                        DeleteConfirmationDialog
                        .Traverse()
                        .OfType<Button>()
                        .ToArray()[2];
                }
                return _cancelButton;
            }
        }
        Button? _cancelButton = default;
    }
    static partial class Extensions
    {
        public static IEnumerable<DependencyObject> Traverse(this DependencyObject parent)
        {
            if (parent == null)
                yield break;

            yield return parent; 
            if (parent is Popup popup && popup.Child is DependencyObject popupContent)
            {
                foreach (var descendant in Traverse(popupContent))
                {
                    yield return descendant;
                }
            }
            else
            {
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    foreach (var descendant in Traverse(child))
                    {
                        yield return descendant;
                    }
                }
            }
        }
    }
}
