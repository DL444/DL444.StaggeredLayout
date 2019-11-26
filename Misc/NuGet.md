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
