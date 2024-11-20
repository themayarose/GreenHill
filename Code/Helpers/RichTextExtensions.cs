using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using FishyFlip.Models;
using Microsoft.UI.Xaml.Documents;

using Block = Microsoft.UI.Xaml.Documents.Block;
using Microsoft.UI.Composition;
using GreenHill.Services;
using System.Text.RegularExpressions;
using System.Text;
using urldetector.detection;
using Windows.Devices.Perception;
using Microsoft.Xaml.Interactivity;
using GreenHill.Behaviors;

namespace GreenHill.Helpers;

public record HyperlinkClickedArgs {
    public Facet? Facet { get; set; }
    public string? Text { get; set; }
    public Hyperlink? Link { get; set; }
}

public class FacetsChangedEventArgs : EventArgs {
    public IEnumerable<Facet>? NewFacets { get; set; }
    public IEnumerable<Facet>? OldFacets { get; set; }
}

public static partial class RichTextExtensions {
    public const string MentionType = "app.bsky.richtext.facet#mention";
    public const string LinkType = "app.bsky.richtext.facet#link";
    public const string HashtagType = "app.bsky.richtext.facet#tag";

    // public static event EventHandler<FacetsChangedEventArgs>? FacetsChangedEvent;

    public static readonly DependencyProperty TextProperty = 
        DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(RichTextBlock),
            new (defaultValue: null, propertyChangedCallback: TextChanged)
        );

    public static string? GetText(RichTextBlock rtb) => rtb.GetValue(TextProperty) as string;
    public static void SetText(RichTextBlock rtb, string? text) => rtb.SetValue(TextProperty, text);

    public static readonly DependencyProperty FacetsProperty = 
        DependencyProperty.RegisterAttached(
            "Facets",
            typeof(IEnumerable<Facet>),
            typeof(RichTextBlock),
            new (defaultValue: null, propertyChangedCallback: FacetsChanged)
        );

    public static IEnumerable<Facet>? GetFacets(RichTextBlock rtb) => rtb.GetValue(FacetsProperty) as IEnumerable<Facet>;
    public static void SetFacets(RichTextBlock rtb, IEnumerable<Facet>? facets) => rtb.SetValue(FacetsProperty, facets);

    public static readonly DependencyProperty FacetProperty =
        DependencyProperty.RegisterAttached(
            "Facet",
            typeof(Facet),
            typeof(Hyperlink),
            null
        );

    public static Facet? GetFacet(Hyperlink link) => link.GetValue(FacetProperty) as Facet;
    public static void SetFacet(Hyperlink link, Facet? facet) => link.SetValue(FacetProperty, facet);

    public static readonly DependencyProperty FacetTextProperty =
        DependencyProperty.RegisterAttached(
            "FacetText",
            typeof(string),
            typeof(Hyperlink),
            null
        );

    public static string? GetFacetText(Hyperlink link) => link.GetValue(FacetTextProperty) as string;
    public static void SetFacetText(Hyperlink link, string? text) => link.SetValue(FacetTextProperty, text);

    public static void TextChanged(object sender, DependencyPropertyChangedEventArgs args) {
        if (sender is not RichTextBlock rtb) return;

        if (args.NewValue is null) {
            rtb.Blocks.Clear();

            return;
        }

        SetFacets(rtb, []);
    }

    public static void FacetsChanged(object sender, DependencyPropertyChangedEventArgs args) {
        if (sender is not RichTextBlock rtb) return;

        string currentText = rtb.GetValue(TextProperty) as string ?? string.Empty;

        IEnumerable<Facet> facets = args.NewValue as IEnumerable<Facet> ?? [];
        IEnumerable<Facet> oldFacets = args.OldValue as IEnumerable<Facet> ?? [];

        (App.Current as App)!.MainWindow?.DispatcherQueue.TryEnqueue(async () => {
            await rtb.BuildAll(currentText, facets);

            rtb.InvalidateMeasure();

            var behaviors = (BehaviorCollection) rtb.GetValue(Interaction.BehaviorsProperty);

            foreach (var b in behaviors) {
                if (b is not HyperlinkClickBehavior behavior) continue;

                behavior.RemakeLinks();
            }
        });
    }

    private static async Task BuildAll(this RichTextBlock self, string text, IEnumerable<Facet> facets) {
        self.Blocks.Clear();

        var paragraphs = text.Split("\n");
        var offset = 0;

        facets = facets
            .OrderBy(f => f.Index?.ByteEnd ?? 0)
            .OrderBy(f => f.Index?.ByteStart ?? 0);

        foreach (var paragraphText in paragraphs) {
            var p = await self.BuildParagraph(offset, paragraphText, facets);

            offset += Encoding.UTF8.GetByteCount(paragraphText) + 1;

            self.Blocks.Add(p);
        }

        await Task.CompletedTask;
    }

    private static async Task<Paragraph> BuildParagraph(this RichTextBlock self, int offset, string text, IEnumerable<Facet> facets) {
        Paragraph p = new();
        List<Facet> validFacets = [];
        Facet? prev = null;

        var textBytes = Encoding.UTF8.GetBytes(text);

        foreach (var current in facets) {
            if ((current.Index?.ByteStart ?? 0) < offset) continue;
            if ((current.Index?.ByteEnd ?? 0) < offset) continue;
            if ((current.Index?.ByteStart ?? int.MaxValue) > offset + textBytes.Length) continue;
            if ((current.Index?.ByteEnd ?? int.MaxValue) > offset + textBytes.Length) continue;

            if ((current.Index?.ByteStart ?? 0) <= (prev?.Index?.ByteEnd ?? 0)) {
                // overlaps
                continue;
            }

            if ((current.Features?.Length ?? 0) == 0) continue;

            validFacets.Add(current);

            prev = current;
        }

        prev = null;

        foreach (var facet in validFacets) {
            if (facet.Index is null) continue;
            if (facet.Features is null) continue;

            List<FacetFeature> f = [..
                from feat in facet.Features
                where (feat.Type ?? string.Empty) is MentionType or HashtagType or LinkType
                select feat
            ];

            var feature = f.First();

            var start = Encoding.UTF8.GetString(
                textBytes[..(facet.Index.ByteStart - offset)]
            ).Length;

            var end = Encoding.UTF8.GetString(
                textBytes[..(facet.Index.ByteEnd - offset)]
            ).Length;

            // var end = Encoding.UTF8.GetBytes(text)[..facet.Index.ByteEnd].Length - offset;
            // var prevEnd = (prev?.Index?.ByteEnd ?? offset) - offset;

            var prevEnd = Encoding.UTF8.GetString(
                textBytes[..((prev?.Index?.ByteEnd ?? offset) - offset)]
            ).Length;

            var textBefore = text[prevEnd..start];

            var facetText = text[start..end];

            Hyperlink link = feature.Type switch {
                LinkType => new () {
                    NavigateUri = new (feature.Uri ?? "about:blank")
                },
                _ => new ()
            };

            if (feature.Type is MentionType or HashtagType) {
                link.SetValue(FacetProperty, facet);
                link.SetValue(FacetTextProperty, facetText);
            }

            link.Inlines.Add(new Run() { Text = facetText });

            if (textBefore != string.Empty) {
                p.Inlines.Add(new Run() { Text = textBefore });
            }

            p.Inlines.Add(link);

            prev = facet;
        }

        if (validFacets.Count > 0) {
            var lastEnd = (validFacets.Last().Index?.ByteEnd ?? int.MaxValue) - offset;

            if (lastEnd < textBytes.Length) {
                var endText = Encoding.UTF8.GetString(textBytes[lastEnd..]);

                p.Inlines.Add(new Run() { Text = endText });
            }
        }
        else {
            p.Inlines.Add(new Run() { Text = text });
        }

        await Task.CompletedTask;
        return p;
    }

    [GeneratedRegex(@"@([a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?")]
    public static partial Regex MentionRegex();

    [GeneratedRegex(@"#[-\w\d]+")]
    public static partial Regex HashtagRegex();

    public async static Task<IEnumerable<Facet>> GetFacetsAsync(this string text, SkyConnection conn) {
        List<Facet> facets = [];

        var matches = MentionRegex().Matches(text);

        foreach (var mobj in matches) {
            if (mobj is not Match m) continue;

            var handle = m.Value[1..];

            var did = await conn.GetDidAsync(handle);

            if (did is null) continue;

            var index = Encoding.UTF8.GetByteCount(text[..m.Index]);
            var end = Encoding.UTF8.GetByteCount(text[..(m.Index + m.Length)]);

            facets.Add(
                new (
                    new (index, end),
                    new FacetFeature(MentionType, null, null, did)
                )
            );
        }

        matches = HashtagRegex().Matches(text);

        foreach (var mobj in matches) {
            if (mobj is not Match m) continue;

            var tag = m.Value[1..];

            var index = Encoding.UTF8.GetByteCount(text[..m.Index]);
            var end = Encoding.UTF8.GetByteCount(text[..(m.Index + m.Length)]);

            facets.Add(new (
                new (index, end),
                new FacetFeature(HashtagType, null, tag, null)
            ));
        }

        var uris = new UrlDetector(text, UrlDetectorOptions.Default).Detect();

        foreach (var uri in uris) {
            var uriText = uri.GetOriginalUrl();
            var index = text.IndexOf(uriText);

            while (index != -1) {
                var byteIndex = Encoding.UTF8.GetByteCount(text[..index]);
                var byteEnd = Encoding.UTF8.GetByteCount(text[..(index + uriText.Length)]);

                facets.Add(new (
                    new (byteIndex, byteEnd),
                    new FacetFeature(LinkType, uri.GetFullUrl(), null, null)
                ));

                index = text.IndexOf(uriText, index + uriText.Length);
            }
        }

        return facets;
    }
}