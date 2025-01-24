The goals of this answer are:
- Eliminate drawing artifacts
- Preserve the ability to tab between controls once the dialog is open. 
- Discover the Cancel button using an optimized localization-friendly singleton.
- Prevent reentry (and crash) e.g. when the [Del] key is pressed while the dialog is already open.

It utilizes a watchdog timer to suppress the check box focus until the dialog is completely provisioned.

___

**Code-Behind**

~~~
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
            if (!_isShowing)    // avoid reentry (Delete key while already open)
            {
                _isShowing = true;
                _wdtOpening.StartOrRestart();
                if (Equals(e.Key, VirtualKey.Delete))
                {
                    _wdtOpening.StartOrRestart();
                    if(Equals(await DeleteConfirmationDialog.ShowAsync(), ContentDialogResult.Primary))
                    {
                        DoDeleteOperation();
                    }
                }
                _isShowing = false;
            }
        };
    }
    bool _isShowing;
    private void DoDeleteOperation()
    {
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
~~~

**Enumerator**

This extension provides a way to enumerating all of the controls which works even when FindName won't.

~~~
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
~~~

___

**XAML**

~~~
<Window
    x:Class="CheckboxTest.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:CheckboxTest"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Title="ContentDialog Focus Test">

    <StackPanel 
        Orientation="Horizontal" 
        HorizontalAlignment="Center"
        VerticalAlignment="Center">
        <ContentDialog
            x:Name="DeleteConfirmationDialog"
            Title="Delete file"
            PrimaryButtonText="Move to Recycle Bin"
            CloseButtonText="Cancel"
            DefaultButton="Close">
            <StackPanel
                x:Name="stackPanel"
                Opacity="0"
                VerticalAlignment="Stretch"
                HorizontalAlignment="Stretch"
                Spacing="12">
                <TextBlock
                    TextWrapping="Wrap" 
                    Text="Are you sure you want to move file 'somefile.jpg' to the Recycle Bin?" />
                <CheckBox
                    x:Name="DeleteDontAskCheckbox"
                    Content="Don't ask me again" />
            </StackPanel>
        </ContentDialog>
    </StackPanel>
</Window>
~~~
___

![screenshot](https://i.sstatic.net/v8RziKQo.png)