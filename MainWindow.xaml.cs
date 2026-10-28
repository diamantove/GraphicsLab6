using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PolyhedraLab.Geometry;
using PolyhedraLab.Models;
using PolyhedraLab.Rendering;
using PolyhedraLab.Transforms;

namespace PolyhedraLab;

public partial class MainWindow : Window
{
    private Polyhedron model = PolyhedronFactory.Create(1);
    private SceneRenderer? renderer;
    private (Point3D A, Point3D B)? rotationLine;

    public MainWindow()
    {
        InitializeComponent();
        // На экране с увеличенным DPI окно не должно уходить за рабочую область.
        Width = Math.Min(Width, SystemParameters.WorkArea.Width);
        Height = Math.Min(Height, SystemParameters.WorkArea.Height);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        renderer = new SceneRenderer(DrawingCanvas);
        Draw();
        Status.Text = "Готово. Выберите преобразование слева. Грани освещены для передачи объёма.";
    }

    private void Draw()
    {
        if (renderer is null) return;
        renderer.Draw(model, (ProjectionMode)ProjectionChoice.SelectedIndex, Zoom.Value,
            AxesCheck.IsChecked == true, rotationLine);
        var c = model.Center;
        ModelInfo.Text = $"{model.Name}: вершин {model.Vertices.Count}, рёбер {model.GetEdges().Count()}, граней {model.Faces.Count}. " +
            $"Центр: ({c.X:F3}; {c.Y:F3}; {c.Z:F3}).";
    }

    private static double Number(TextBox box)
    {
        string text = box.Text.Trim().Replace(',', '.');
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            || !double.IsFinite(value) || Math.Abs(value) > 1e6)
        {
            box.Focus();
            box.SelectAll();
            throw new ArgumentException("Введите конечное число от −1000000 до 1000000. Дробный разделитель — точка или запятая.");
        }
        return value;
    }

    private void Apply(Func<Matrix4x4> createMatrix, string message)
    {
        try
        {
            model.Transform(createMatrix());
            Draw();
            Status.Text = message;
        }
        catch (ArgumentException ex)
        {
            Status.Text = ex.Message;
        }
    }

    private Matrix4x4 AxisRotation() => AxisChoice.SelectedIndex switch
    {
        0 => Matrix4x4.RotationX(Number(Angle)),
        1 => Matrix4x4.RotationY(Number(Angle)),
        _ => Matrix4x4.RotationZ(Number(Angle))
    };

    private Matrix4x4 ScaleMatrix() => Matrix4x4.Scaling(Number(Sx), Number(Sy), Number(Sz));
    private void Translate_Click(object sender, RoutedEventArgs e) =>
        Apply(() => Matrix4x4.Translation(Number(Dx), Number(Dy), Number(Dz)), "Выполнено смещение T(dx,dy,dz).");
    private void RotateOrigin_Click(object sender, RoutedEventArgs e) =>
        Apply(AxisRotation, "Выполнен поворот вокруг выбранной координатной оси.");
    private void RotateCenter_Click(object sender, RoutedEventArgs e) =>
        Apply(() => Matrix4x4.AroundPoint(model.Center, AxisRotation()), "Поворот через центр: T(C) · R · T(−C).");
    private void ScaleOrigin_Click(object sender, RoutedEventArgs e) =>
        Apply(ScaleMatrix, "Масштабирование относительно начала координат. Нулевой коэффициент допустим: фигура вырождается.");
    private void ScaleCenter_Click(object sender, RoutedEventArgs e) =>
        Apply(() => Matrix4x4.AroundPoint(model.Center, ScaleMatrix()), "Масштабирование через центр: T(C) · S · T(−C).");
    private void Reflect_Click(object sender, RoutedEventArgs e) =>
        Apply(() => Matrix4x4.Reflection(PlaneChoice.SelectedIndex), "Выполнено отражение относительно выбранной плоскости.");

    private void RotateLine_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var a = new Point3D(Number(Ax), Number(Ay), Number(Az));
            var b = new Point3D(Number(Bx), Number(By), Number(Bz));
            var matrix = Matrix4x4.RotationAroundLine(a, b, Number(LineAngle));
            model.Transform(matrix);
            rotationLine = (a, b);
            Draw();
            Status.Text = "Поворот вокруг A → B: T(A) · R(направление B−A, угол) · T(−A).";
        }
        catch (ArgumentException ex) { Status.Text = ex.Message; }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        model = PolyhedronFactory.Create(SolidChoice.SelectedIndex);
        rotationLine = null;
        Draw();
        Status.Text = "Исходная фигура восстановлена. Параметры вида сохранены.";
    }

    private void SolidChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (renderer is not null) Reset_Click(sender, e);
    }
    private void View_Changed(object sender, RoutedEventArgs e) => Draw();
    private void Zoom_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => Draw();
    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e) => Draw();
}
