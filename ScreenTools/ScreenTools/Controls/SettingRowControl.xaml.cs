using System.Windows;
using System.Windows.Controls;

namespace ScreenTools.Controls;

public partial class SettingRowControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SettingRowControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(SettingRowControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RightContentProperty =
        DependencyProperty.Register(
            nameof(RightContent),
            typeof(object),
            typeof(SettingRowControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty RightContentMarginProperty =
        DependencyProperty.Register(
            nameof(RightContentMargin),
            typeof(Thickness),
            typeof(SettingRowControl),
            new PropertyMetadata(new Thickness(0)));

    public SettingRowControl()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public Thickness RightContentMargin
    {
        get => (Thickness)GetValue(RightContentMarginProperty);
        set => SetValue(RightContentMarginProperty, value);
    }
}
