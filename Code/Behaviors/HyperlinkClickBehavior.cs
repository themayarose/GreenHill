using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI;
using FishyFlip.Models;
using GreenHill.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.Foundation;
using System.Globalization;

namespace GreenHill.Behaviors;

public partial class HyperlinkClickBehavior : BehaviorBase<RichTextBlock> {
    public readonly static DependencyProperty LinksCommandProperty =
        DependencyProperty.Register(
             "LinksCommand",
             typeof(IRelayCommand),
             typeof(HyperlinkClickBehavior),
             null
        );

    public IRelayCommand? LinksCommand {
        get => (IRelayCommand?) GetValue(LinksCommandProperty);
        set => SetValue(LinksCommandProperty, value);
    }

    public HyperlinkClickBehavior() => Initialize();

    public void RemakeLinks() {
        if (!IsAttached) return;

        var links =
            from block in AssociatedObject.Blocks
            where block is Paragraph
            from inline in (block as Paragraph)!.Inlines
            where inline is Hyperlink
            select inline as Hyperlink
        ;

        foreach (var link in links) {
            if (link is null) continue;

            if (link.GetValue(RichTextExtensions.FacetProperty) is not Facet facet) continue;
            if (link.GetValue(RichTextExtensions.FacetTextProperty) is not string text) continue;

            link.SetValue(HyperlinkExtensions.CommandProperty, LinksCommand);
            link.SetValue(HyperlinkExtensions.CommandParameterProperty, new HyperlinkClickedArgs() {
                Facet = facet,
                Text = text
            });
        }
    }

}