using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PolyhedraLab.Models;

namespace PolyhedraLab.Rendering;

public sealed class SceneRenderer
{
    private readonly Canvas canvas;

    public SceneRenderer(Canvas canvas)
    {
        this.canvas = canvas;
    }

    public void Draw(Polyhedron model, ProjectionMode mode, double zoom, bool showAxes,
        (Point3D A, Point3D B)? rotationLine)
    {
        canvas.Children.Clear();
        var projection = new Projection(mode);
        double scale = Math.Min(canvas.ActualWidth, canvas.ActualHeight) / 6 * zoom;
        if (scale <= 0) return;

        void Segment(Point3D a, Point3D b, Brush color, double thickness, bool dashed = false, string? label = null)
        {
            if (!projection.ProjectSegment(a, b, out var pa, out var pb)) return;
            Point s = ToScreen(pa, scale), e = ToScreen(pb, scale);
            var line = new Line { X1 = s.X, Y1 = s.Y, X2 = e.X, Y2 = e.Y,
                Stroke = color, StrokeThickness = thickness };
            if (dashed) line.StrokeDashArray = new DoubleCollection { 4, 3 };
            canvas.Children.Add(line);
            if (label is not null)
            {
                var text = new TextBlock { Text = label, Foreground = color, FontWeight = FontWeights.Bold };
                Canvas.SetLeft(text, e.X + 4);
                Canvas.SetTop(text, e.Y + 4);
                canvas.Children.Add(text);
            }
        }

        if (showAxes)
        {
            Segment(new(), new(2.5,0,0), Brushes.Firebrick, 1.2, label: "X");
            Segment(new(), new(0,2.5,0), Brushes.ForestGreen, 1.2, label: "Y");
            Segment(new(), new(0,0,2.5), Brushes.RoyalBlue, 1.2, label: "Z");
        }
        if (rotationLine is { } axis)
            Segment(axis.A, axis.B, Brushes.DarkOrange, 2, true);
        DrawFaces(model, projection, scale);
        // Полностью сжатая в точку фигура тоже должна оставаться видимой.
        Point3D center = model.Center;
        if (model.Vertices.All(p => (p - center).Length < 1e-10)
            && projection.ProjectSegment(center, center, out var projected, out _))
        {
            Point point = ToScreen(projected, scale);
            var dot = new Ellipse { Width = 5, Height = 5, Fill = Brushes.MidnightBlue };
            Canvas.SetLeft(dot, point.X - 2.5);
            Canvas.SetTop(dot, point.Y - 2.5);
            canvas.Children.Add(dot);
        }
    }

    // Y меняет знак только при выводе на Canvas.
    private Point ToScreen(Point3D p, double scale) => new(
        canvas.ActualWidth / 2 + p.X * scale,
        canvas.ActualHeight / 2 - p.Y * scale);

    private void DrawFaces(Polyhedron model, Projection projection, double scale)
    {
        Point3D center = projection.View.TransformPoint(model.Center);
        Point3D light = new(-3, 5, 8);
        light *= 1 / light.Length;
        var faces = model.Faces.Select(face => face.VertexIndices
            .Select(i => projection.View.TransformPoint(model.Vertices[i])).ToArray());

        // Камера со стороны +Z: сначала рисуем дальние грани (меньший Z).
        // Для наших выпуклых тел достаточно алгоритма художника.
        foreach (var vertices in faces.OrderBy(points => points.Average(p => p.Z)))
        {
            Point3D faceCenter = Point3D.Average(vertices);
            // У наших правильных граней первые три вершины не лежат на одной прямой.
            // Нормаль перпендикулярна двум соседним сторонам.
            Point3D normal = Point3D.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
            double brightness = 0.55;
            if (normal.Length > 1e-10)
            {
                normal *= 1 / normal.Length;
                // Отрицательный масштаб/отражение меняет направление обхода.
                // Возвращаем нормаль наружу, не полагаясь на исходный порядок индексов.
                double outward = Point3D.Dot(normal, faceCenter - center);
                if (outward < -1e-10 || (Math.Abs(outward) <= 1e-10 && normal.Z < 0)) normal *= -1;
                brightness = 0.25 + 0.75 * Math.Max(0, Point3D.Dot(normal, light));
            }
            var clipped = projection.ClipFace(vertices);
            if (clipped.Count < 3) continue;
            var color = Color.FromRgb((byte)(85 * brightness), (byte)(160 * brightness), (byte)(235 * brightness));
            var polygon = new Polygon
            {
                Points = new PointCollection(clipped.Select(p => ToScreen(projection.ProjectionMatrix.TransformPoint(p), scale))),
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.MidnightBlue,
                StrokeThickness = 1.2,
                StrokeLineJoin = PenLineJoin.Round
            };
            canvas.Children.Add(polygon);
        }
    }
}
