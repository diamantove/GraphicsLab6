using PolyhedraLab.Transforms;

namespace PolyhedraLab.Models;

public sealed class Polyhedron
{
    public string Name { get; }
    public List<Point3D> Vertices { get; }
    public List<Face> Faces { get; }

    public Polyhedron(string name, IEnumerable<Point3D> vertices, IEnumerable<Face> faces)
    {
        Name = name;
        Vertices = vertices.ToList();
        Faces = faces.ToList();
    }

    // Среднее вершин совпадает с центром правильного многогранника.
    // Аффинное преобразование сохраняет это свойство, даже после отражения/масштаба.
    public Point3D Center => Point3D.Average(Vertices);

    public IEnumerable<(int A, int B)> GetEdges()
    {
        var edges = new HashSet<(int, int)>();
        foreach (var face in Faces)
        {
            var indices = face.VertexIndices;
            for (int i = 0; i < indices.Length; i++)
            {
                int a = indices[i], b = indices[(i + 1) % indices.Length];
                if (edges.Add((Math.Min(a, b), Math.Max(a, b))))
                    yield return (a, b);
            }
        }
    }

    public void Transform(Matrix4x4 matrix)
    {
        // Сначала проверяем весь результат: ошибка не должна менять модель наполовину.
        var transformed = Vertices.Select(matrix.TransformPoint).ToArray();
        if (transformed.Any(p => !p.IsFinite || Math.Max(Math.Abs(p.X), Math.Max(Math.Abs(p.Y), Math.Abs(p.Z))) > 1e9))
            throw new ArgumentException("Слишком большие координаты. Уменьшите параметры преобразования.");
        for (int i = 0; i < transformed.Length; i++)
            Vertices[i] = transformed[i];
    }
}
