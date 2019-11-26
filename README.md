# DL444.StaggeredLayout
StaggeredLayout is a layout control with the same visual as [StaggeredPanel](https://docs.microsoft.com/en-us/windows/communitytoolkit/controls/staggeredpanel), but with UI virtualization support.   
It is intended to be used with [ItemsRepeater](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/items-repeater) control.  

This control is available on [nuget.org](https://www.nuget.org/packages/DL444.StaggeredLayout.Controls).

![Screenshot](https://github.com/DL444/DL444.StaggeredLayout/blob/master/Misc/Staggered-Complete.jpg?raw=true)  

[Sample](https://github.com/DL444/DL444.StaggeredLayout.Demo)

## Install
In your **Package Manager Console**:
```ps
PM> Install-Package DL444.StaggeredLayout.Controls
```
Or use GUI-based **Manage Nuget Packages**.  

## Usage
In XAML, add a `ItemsRepeater`, and set its `Layout` property. See Windows UI Library [documentation](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/items-repeater) for more info on `ItemsRepeater`.
```xml
<Page xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
      xmlns:dsc="using:DL444.StaggeredLayout.Controls">

    <muxc:ItemsRepeaterScrollHost>
        <ScrollViewer>
            <muxc:ItemsRepeater>
                <muxc:ItemsRepeater.Layout>
                    <dsc:StaggeredLayout />
                </muxc:ItemsRepeater.Layout>
            </muxc:ItemsRepeater>
        </ScrollViewer>
    </muxc:ItemsRepeaterScrollHost>

</Page>
```

This control has the same properties as `StaggeredPanel` in Windows Community Toolkit.
```xml
<dsc:StaggeredLayout Padding="16" 
                     HorizontalAlignment="Stretch" 
                     DesiredColumnWidth="240" 
                     RowSpacing="8" 
                     ColumnSpacing="12" />
```
