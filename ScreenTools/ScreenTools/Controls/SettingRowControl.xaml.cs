using System.Windows;
using System.Windows.Controls;

namespace ScreenTools.Controls;

/// <summary>
/// Provides a reusable settings row with a title, description, and trailing content.
/// </summary>
public partial class SettingRowControl : UserControl
{
    /// <summary>
    /// Provides the TitleProperty value exposed by this component.
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SettingRowControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Provides the DescriptionProperty value exposed by this component.
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(SettingRowControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Provides the RightContentProperty value exposed by this component.
    /// </summary>
    public static readonly DependencyProperty RightContentProperty =
        DependencyProperty.Register(
            nameof(RightContent),
            typeof(object),
            typeof(SettingRowControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Provides the RightContentMarginProperty value exposed by this component.
    /// </summary>
    public static readonly DependencyProperty RightContentMarginProperty =
        DependencyProperty.Register(
            nameof(RightContentMargin),
            typeof(Thickness),
            typeof(SettingRowControl),
            new PropertyMetadata(new Thickness(0)));

    /// <summary>
    /// Initializes the reusable settings-row control and its dependency-property bindings.
    /// </summary>
    public SettingRowControl()
    {
        InitializeComponent();
    }

    private void RowLayout_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Keep the label and its control side-by-side when there is room. On
        // narrow cards, give each its own row instead of allowing them to collide.
        var useStackedLayout = e.NewSize.Width < 380;
        var isStacked = RowLayout.RowDefinitions.Count == 2;
        if (useStackedLayout == isStacked)
        {
            return;
        }

        RowLayout.ColumnDefinitions.Clear();
        RowLayout.RowDefinitions.Clear();

        if (useStackedLayout)
        {
            RowLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            RowLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetColumn(TextContent, 0);
            Grid.SetRow(TextContent, 0);
            Grid.SetColumn(RightContentPresenter, 0);
            Grid.SetRow(RightContentPresenter, 1);
            RightContentPresenter.HorizontalAlignment = HorizontalAlignment.Left;
            RightContentPresenter.VerticalAlignment = VerticalAlignment.Center;
            RightContentPresenter.Margin = new Thickness(0, 10, 0, 0);
        }
        else
        {
            RowLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            RowLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(TextContent, 0);
            Grid.SetRow(TextContent, 0);
            Grid.SetColumn(RightContentPresenter, 1);
            Grid.SetRow(RightContentPresenter, 0);
            RightContentPresenter.HorizontalAlignment = HorizontalAlignment.Right;
            RightContentPresenter.Margin = RightContentMargin;
        }
    }

    /// <summary>
    /// Initializes the reusable settings-row control and its dependency-property bindings.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Initializes the reusable settings-row control and its dependency-property bindings.
    /// </summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>
    /// Initializes the reusable settings-row control and its dependency-property bindings.
    /// </summary>
    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    /// <summary>
    /// Initializes the reusable settings-row control and its dependency-property bindings.
    /// </summary>
    public Thickness RightContentMargin
    {
        get => (Thickness)GetValue(RightContentMarginProperty);
        set => SetValue(RightContentMarginProperty, value);
    }
}
