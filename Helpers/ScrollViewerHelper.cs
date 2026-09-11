using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GastosCompartidos.Helpers;

/// <summary>
/// Ayudas de desplazamiento para las páginas alojadas en el <c>NavigationView</c>
/// de WPF-UI, cuyo Frame interno mide el contenido con altura infinita.
/// </summary>
public static class ScrollViewerHelper
{
    // ----------------------------------------------------------------------
    // BindHeightToHost: acota el alto del elemento (raíz de la página) al área
    // visible del contenedor, de modo que los ScrollViewer internos queden
    // acotados y puedan desplazarse.
    // ----------------------------------------------------------------------
    public static readonly DependencyProperty BindHeightToHostProperty =
        DependencyProperty.RegisterAttached(
            "BindHeightToHost", typeof(bool), typeof(ScrollViewerHelper),
            new PropertyMetadata(false, OnBindHeightToHostChanged));

    public static bool GetBindHeightToHost(DependencyObject obj) => (bool)obj.GetValue(BindHeightToHostProperty);
    public static void SetBindHeightToHost(DependencyObject obj, bool value) => obj.SetValue(BindHeightToHostProperty, value);

    private static void OnBindHeightToHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement fe && e.NewValue is true)
            fe.Loaded += OnElementLoaded;
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        var fe = (FrameworkElement)sender;

        if (FindAncestor<ScrollViewer>(fe) is { } host)
            fe.SetBinding(FrameworkElement.MaxHeightProperty,
                new Binding(nameof(ScrollViewer.ViewportHeight)) { Source = host });
        else if (FindAncestor<Frame>(fe) is { } frame)
            fe.SetBinding(FrameworkElement.MaxHeightProperty,
                new Binding(nameof(FrameworkElement.ActualHeight)) { Source = frame });
    }

    // ----------------------------------------------------------------------
    // FixMouseWheel: la rueda siempre desplaza el ScrollViewer (incluso sobre
    // gráficos u otros controles que se "comen" el evento) y lo hace de forma
    // suave (animada con easing) en lugar de saltar de golpe.
    // ----------------------------------------------------------------------
    private static readonly TimeSpan ScrollDuration = TimeSpan.FromMilliseconds(280);

    public static readonly DependencyProperty FixMouseWheelProperty =
        DependencyProperty.RegisterAttached(
            "FixMouseWheel", typeof(bool), typeof(ScrollViewerHelper),
            new PropertyMetadata(false, OnFixMouseWheelChanged));

    public static bool GetFixMouseWheel(DependencyObject obj) => (bool)obj.GetValue(FixMouseWheelProperty);
    public static void SetFixMouseWheel(DependencyObject obj, bool value) => obj.SetValue(FixMouseWheelProperty, value);

    private static void OnFixMouseWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer && e.NewValue is true)
        {
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    /// <summary>Proxy animado: cada cambio aplica el desplazamiento al ScrollViewer.</summary>
    private static readonly DependencyProperty AnimatedOffsetProperty =
        DependencyProperty.RegisterAttached(
            "AnimatedOffset", typeof(double), typeof(ScrollViewerHelper),
            new PropertyMetadata(0.0, OnAnimatedOffsetChanged));

    private static void OnAnimatedOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer)
        {
            double offset = (double)e.NewValue;
            scrollViewer.SetValue(LastAnimatedOffsetProperty, offset);
            scrollViewer.ScrollToVerticalOffset(offset);
        }
    }

    /// <summary>Destino acumulado de la animación (NaN = sin definir).</summary>
    private static readonly DependencyProperty TargetOffsetProperty =
        DependencyProperty.RegisterAttached(
            "TargetOffset", typeof(double), typeof(ScrollViewerHelper),
            new PropertyMetadata(double.NaN));

    /// <summary>Último desplazamiento pedido por la animación, para reconocer el scroll ajeno.</summary>
    private static readonly DependencyProperty LastAnimatedOffsetProperty =
        DependencyProperty.RegisterAttached(
            "LastAnimatedOffset", typeof(double), typeof(ScrollViewerHelper),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Si el desplazamiento no lo produjo la animación (arrastre de la barra, teclado),
    /// el destino acumulado deja de valer: se descarta para no saltar en la próxima muesca.
    /// </summary>
    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange == 0 || sender is not ScrollViewer scrollViewer) return;

        double last = (double)scrollViewer.GetValue(LastAnimatedOffsetProperty);
        if (double.IsNaN(last) || Math.Abs(last - e.VerticalOffset) > 0.5)
            scrollViewer.SetValue(TargetOffsetProperty, double.NaN);
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not ScrollViewer scrollViewer) return;

        // Sin nada que desplazar, o si el puntero está sobre un control con su propio
        // scroll (p. ej. el cuadro de Notas), el evento sigue su curso normal.
        if (scrollViewer.ScrollableHeight <= 0) return;
        if (InnerScrollViewerCanScroll(scrollViewer, e)) return;

        e.Handled = true;

        // Punto de partida del nuevo destino: el destino anterior (para acumular
        // varios "clics" de rueda seguidos), salvo que se haya desincronizado
        // del desplazamiento real (p. ej. al arrastrar la barra).
        double current = (double)scrollViewer.GetValue(TargetOffsetProperty);
        if (double.IsNaN(current))
            current = scrollViewer.VerticalOffset;

        double target = current - e.Delta;
        target = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, target));
        scrollViewer.SetValue(TargetOffsetProperty, target);

        var animation = new DoubleAnimation
        {
            From = scrollViewer.VerticalOffset,
            To = target,
            Duration = ScrollDuration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        scrollViewer.BeginAnimation(AnimatedOffsetProperty, animation);
    }

    /// <summary>¿El evento nació dentro de otro ScrollViewer que todavía puede desplazarse en esa dirección?</summary>
    private static bool InnerScrollViewerCanScroll(ScrollViewer outer, MouseWheelEventArgs e)
    {
        DependencyObject? current = e.OriginalSource as DependencyObject;
        while (current is not null && !ReferenceEquals(current, outer))
        {
            if (current is ScrollViewer inner && inner.ScrollableHeight > 0 &&
                (e.Delta < 0 ? inner.VerticalOffset < inner.ScrollableHeight : inner.VerticalOffset > 0))
                return true;

            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : (current as FrameworkContentElement)?.Parent;
        }
        return false;
    }

    // ----------------------------------------------------------------------
    private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
    {
        DependencyObject? current = VisualTreeHelper.GetParent(start);
        while (current is not null and not T)
            current = VisualTreeHelper.GetParent(current);
        return current as T;
    }
}
