using System.Windows;
using System.Windows.Controls;

namespace ScreenTools.Controls;

public partial class ToolbarItemControl : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(ToolbarItemControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconContentProperty =
        DependencyProperty.Register(
            nameof(IconContent),
            typeof(object),
            typeof(ToolbarItemControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(ToolbarItemControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(ToolbarItemControl),
            new PropertyMetadata(44d));

    public static readonly DependencyProperty ItemPaddingProperty =
        DependencyProperty.Register(
            nameof(ItemPadding),
            typeof(Thickness),
            typeof(ToolbarItemControl),
            new PropertyMetadata(new Thickness(0)));

    public ToolbarItemControl()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public object? IconContent
    {
        get => GetValue(IconContentProperty);
        set => SetValue(IconContentProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public Thickness ItemPadding
    {
        get => (Thickness)GetValue(ItemPaddingProperty);
        set => SetValue(ItemPaddingProperty, value);
    }
}
