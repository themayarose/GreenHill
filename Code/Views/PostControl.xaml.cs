using FishyFlip.Models;
using GreenHill.Helpers;
using GreenHill.Services;
using Microsoft.UI.Xaml.Controls;

namespace GreenHill.Views;

[ObservableObject]
public partial class PostControl : UserControl, IBaseView<PostViewModel> {
    public PostViewModel ViewModel { get; } = App.GetService<PostViewModel>();

    public PostControl() => InitializeComponent();

    [ObservableProperty] private GridLength gridRowHeight = new (.5, GridUnitType.Star);
    [ObservableProperty] private double gridMaxHeight = double.PositiveInfinity;
    [ObservableProperty] private double gridRowMaxHeight = double.PositiveInfinity;

    public void LoadEmbeddedVideo(object? _, RoutedEventArgs __) {
        if (!ViewModel.HasEmbeddedVideo) return;
        if (ViewModel.EmbeddedVideo?.AspectRatio is not AspectRatio ratio) return;

        var height = PostColumn.ActualWidth / ratio.Width * ratio.Height;

        MediaPlayer.Height = height;
    }

    public void LoadEmbeddedPictures() {
        PicturesView.Items.Clear();

        foreach (var pic in ViewModel.EmbeddedPictures) {
            PicturesView.Items.Add(BuildImageItem(pic));
        }
    }

    public void LoadOriginalPost() {
        OriginalPostContainer.Children.Clear();
        
        if (!ViewModel.IsReply || ViewModel.IsRepost || ViewModel.IsQuote || ViewModel.OriginalPost is null) return;

        PostControl post = new () { ShowThreadLine = true };

        post.MakeBinding(ViewModel, ConnectionProperty, nameof(ViewModel.Connection));
        post.MakeBinding(ViewModel, LinksCommandProperty, nameof(ViewModel.LinksCommand));
        post.MakeBinding(ViewModel, DisplayProfileCommandProperty, nameof(ViewModel.RequestDisplayProfileWithProfileCommand));
        post.MakeBinding(ViewModel, DisplayPostCommandProperty, nameof(ViewModel.RequestDisplayPostCommand));
        post.MakeBinding(ViewModel, DisplayUserListCommandProperty, nameof(ViewModel.RequestDisplayUserListCommand));
        post.MakeBinding(ViewModel, PostProperty, nameof(ViewModel.OriginalPost));

        OriginalPostContainer.Children.Add(post);
    }

    public void LoadMostRecentParent() {
        ParentReplyContainer.Children.Clear();

        if (!ViewModel.ShowParentReply || ViewModel.IsRepost || ViewModel.IsQuote) return;

        PostControl post = new () { ShowThreadLine = true, ShowReplyTo = ViewModel.ShowThreadExtensions };

        post.MakeBinding(ViewModel, ConnectionProperty, nameof(ViewModel.Connection));
        post.MakeBinding(ViewModel, LinksCommandProperty, nameof(ViewModel.LinksCommand));
        post.MakeBinding(ViewModel, DisplayProfileCommandProperty, nameof(ViewModel.RequestDisplayProfileWithProfileCommand));
        post.MakeBinding(ViewModel, DisplayPostCommandProperty, nameof(ViewModel.RequestDisplayPostCommand));
        post.MakeBinding(ViewModel, DisplayUserListCommandProperty, nameof(ViewModel.RequestDisplayUserListCommand));
        post.MakeBinding(ViewModel, PostProperty, nameof(ViewModel.ParentReply));

        ParentReplyContainer.Children.Add(post);
    }

    public void LoadEmbeddedQuote() {
        QuoteContainer.Children.Clear();

        if (!ViewModel.HasEmbeddedQuote || ViewModel.IsQuote) return;

        PostControl post = new () { IsQuote = true };

        post.MakeBinding(ViewModel, ConnectionProperty, nameof(ViewModel.Connection));
        post.MakeBinding(ViewModel, LinksCommandProperty, nameof(ViewModel.LinksCommand));
        post.MakeBinding(ViewModel, DisplayProfileCommandProperty, nameof(ViewModel.RequestDisplayProfileWithProfileCommand));
        post.MakeBinding(ViewModel, DisplayPostCommandProperty, nameof(ViewModel.RequestDisplayPostCommand));
        post.MakeBinding(ViewModel, DisplayUserListCommandProperty, nameof(ViewModel.RequestDisplayUserListCommand));
        post.MakeBinding(ViewModel, PostProperty, nameof(ViewModel.EmbeddedQuote));

        QuoteContainer.Children.Add(post);
    }

    public void ResizeGrid(object _, RoutedEventArgs __) {
        var width = PostColumn.ActualWidth;
        var count = ViewModel.EmbeddedPictures.Count;

        if (count == 1) {
            GridMaxHeight = 2 * width;
            GridRowMaxHeight = width;
        }
        else {
            GridRowHeight = count switch {
                2 => new (width / 2, GridUnitType.Pixel),
                3 => new (width / 4, GridUnitType.Pixel),
                _ => new (width / 3, GridUnitType.Pixel)
            };

            GridRowMaxHeight = GridRowHeight.Value;

            foreach (var (i, elem) in PicturesView.Items.WithIndex()) {
                if (elem is not GridViewItem item) continue;
                if (item.Content is not Microsoft.UI.Xaml.Controls.Image image) continue;

                var height = i == 0 && count == 3 ?
                    2 * (GridRowMaxHeight + 4) :
                    GridRowMaxHeight;

                item.Height = height;
                item.MaxHeight = height;
                image.Height = height;
                image.MaxHeight = height;
            }
        }
    }

    private GridViewItem BuildImageItem(EmbeddedPicture picture) {
        var image = new Microsoft.UI.Xaml.Controls.Image() {
            Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Source = picture.Thumb,
        };

        if (picture.View?.Alt is not null && picture.View.Alt != string.Empty) {
            image.SetValue(ToolTipService.ToolTipProperty, picture.View.Alt);
        }

        var item = new GridViewItem {
            Content = image,
            CornerRadius = new (8)
        };

        item.SetValue(Grid.RowProperty, picture.Row);
        item.SetValue(Grid.RowSpanProperty, picture.RowSpan);
        item.SetValue(Grid.ColumnProperty, picture.Column);
        item.SetValue(Grid.ColumnSpanProperty, picture.ColumnSpan);

        return item;
    }
    public static readonly DependencyProperty ConnectionProperty =
        DependencyProperty.Register(
            "Connection",
            typeof(SkyConnection),
            typeof(PostControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not PostControl self) return;

                self.ViewModel.Connection = (SkyConnection?) a.NewValue;
            })
        );

    public SkyConnection? Connection {
        get => (SkyConnection?) GetValue(ConnectionProperty);
        set => SetValue(ConnectionProperty, value);
    }

    public static readonly DependencyProperty PostProperty =
        DependencyProperty.Register(
            "Post",
            typeof(FeedViewPost),
            typeof(PostControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not PostControl self) return;

                self.ViewModel.Post = (FeedViewPost?) a.NewValue;

                self.LoadEmbeddedPictures();
                self.LoadEmbeddedQuote();
                self.LoadOriginalPost();
                self.LoadMostRecentParent();

                if ((self.ViewModel.IsQuote || self.ViewModel.IsRepost) && self.ViewModel.Post?.Reply is not null) {
                    self.ShowReplyTo = true;
                }
            })
        );

    public FeedViewPost? Post {
        get => (FeedViewPost?) GetValue(PostProperty);
        set => SetValue(PostProperty, value);
    }

    public static readonly DependencyProperty IsQuoteProperty =
        DependencyProperty.Register(
            "IsQuote",
            typeof(bool),
            typeof(PostControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not PostControl self) return;

                self.ViewModel.IsQuote = (bool) a.NewValue;
            })
        );

    public bool IsQuote {
        get => (bool) GetValue(IsQuoteProperty);
        set => SetValue(IsQuoteProperty, value);
    }

    public static readonly DependencyProperty ShowThreadLineProperty =
        DependencyProperty.Register(
            "ShowThreadLine",
            typeof(bool),
            typeof(PostControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not PostControl self) return;

                self.ViewModel.ShowThreadLine = (bool) a.NewValue;
            })
        );
    
    public bool ShowThreadLine {
        get => (bool) GetValue(ShowThreadLineProperty);
        set => SetValue(ShowThreadLineProperty, value);
    }

    public static readonly DependencyProperty ShowReplyToProperty =
        DependencyProperty.Register(
            "ShowReplyTo",
            typeof(bool),
            typeof(PostControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not PostControl self) return;

                self.ViewModel.ShowReplyTo = (bool) a.NewValue;
            })
        );
    
    public bool ShowReplyTo {
        get => (bool) GetValue(ShowReplyToProperty);
        set => SetValue(ShowReplyToProperty, value);
    }

    public static readonly DependencyProperty DisplayProfileCommandProperty =
        DependencyProperty.Register(
            "DisplayProfileCommand",
            typeof(IRelayCommand),
            typeof(PostControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not PostControl self) return;

                self.ViewModel.DisplayProfileCommand = (IRelayCommand?) a.NewValue;
            })
        );

    public IRelayCommand? DisplayProfileCommand {
        get => (IRelayCommand?) GetValue(DisplayProfileCommandProperty);
        set => SetValue(DisplayProfileCommandProperty, value);
    }

    public static readonly DependencyProperty DisplayPostCommandProperty =
        DependencyProperty.Register(
            "DisplayPostCommand",
            typeof(IRelayCommand),
            typeof(PostControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not PostControl self) return;

                self.ViewModel.DisplayPostCommand = (IRelayCommand?) a.NewValue;
            })
        );

    public IRelayCommand? DisplayPostCommand {
        get => (IRelayCommand?) GetValue(DisplayPostCommandProperty);
        set => SetValue(DisplayPostCommandProperty, value);
    }

    public static readonly DependencyProperty DisplayUserListCommandProperty =
        DependencyProperty.Register(
            "DisplayUserListCommand",
            typeof(IRelayCommand),
            typeof(PostControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not PostControl self) return;

                self.ViewModel.DisplayUserListCommand = (IRelayCommand?) a.NewValue;
            })
        );

    public IRelayCommand? DisplayUserListCommand {
        get => (IRelayCommand?) GetValue(DisplayUserListCommandProperty);
        set => SetValue(DisplayUserListCommandProperty, value);
    }

    public static readonly DependencyProperty LinksCommandProperty = 
        DependencyProperty.Register(
            "LinksCommand",
            typeof(IRelayCommand),
            typeof(PostControl),
            new (defaultValue: null, propertyChangedCallback: (s, a) => {
                if (s is not PostControl self) return;

                self.ViewModel.LinksCommand = (IRelayCommand?) a.NewValue;
            })
        );

    public IRelayCommand? LinksCommand {
        get => (IRelayCommand?) GetValue(LinksCommandProperty);
        set => SetValue(LinksCommandProperty, value);
    }

}