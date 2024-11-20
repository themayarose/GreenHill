using System.Windows.Input;
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

    public void LoadEmbeddedPictures() {
        PicturesView.Items.Clear();

        foreach (var pic in ViewModel.EmbeddedPictures) {
            PicturesView.Items.Add(BuildImageItem(pic));
        }
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

                if (i == 0 && count == 3) {
                    item.Height = 2 * (GridRowMaxHeight + 4);
                    item.MaxHeight = 2 * (GridRowMaxHeight + 4);
                }
                else {
                    item.Height = GridRowMaxHeight;
                    item.MaxHeight = GridRowMaxHeight;
                }
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
            })
        );

    public FeedViewPost? Post {
        get => (FeedViewPost?) GetValue(PostProperty);
        set => SetValue(PostProperty, value);
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