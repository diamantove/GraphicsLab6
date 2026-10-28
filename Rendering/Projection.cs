using PolyhedraLab.Models;
using PolyhedraLab.Transforms;

namespace PolyhedraLab.Rendering;

public enum ProjectionMode { Axonometric, Perspective }

public sealed class Projection
{
    public const double CameraDistance = 8;
    public const double NearPlane = 0.2;
    public ProjectionMode Mode { get; }
    public Matrix4x4 View { get; }
    public Matrix4x4 ProjectionMatrix { get; }

    public Projection(ProjectionMode mode)
    {
        Mode = mode;
        // Изометрия — частный случай аксонометрии: одинаковое сокращение по трём осям.
        View = Matrix4x4.RotationX(35.264389682754654) * Matrix4x4.RotationY(-45);
        ProjectionMatrix = mode == ProjectionMode.Axonometric
            ? new Matrix4x4(new double[,] { {1,0,0,0}, {0,1,0,0}, {0,0,0,0}, {0,0,0,1} })
            : new Matrix4x4(new double[,] { {1,0,0,0}, {0,1,0,0}, {0,0,0,0}, {0,0,-1.0/CameraDistance,1} });
    }

    /// <summary>Отсекает грань в видовых координатах до перспективного деления.</summary>
    public List<Point3D> ClipFace(IReadOnlyList<Point3D> vertices)
    {
        if (Mode != ProjectionMode.Perspective) return vertices.ToList();
        var result = new List<Point3D>();
        double limit = CameraDistance - NearPlane;
        // Для каждого ребра сохраняем внутреннюю вершину и пересечение с плоскостью.
        // Это один шаг алгоритма Сазерленда — Ходжмана.
        for (int i = 0; i < vertices.Count; i++)
        {
            Point3D a = vertices[i], b = vertices[(i + 1) % vertices.Count];
            bool insideA = a.Z <= limit, insideB = b.Z <= limit;
            if (insideA) result.Add(a);
            if (insideA != insideB)
                result.Add(a + (b - a) * ((limit - a.Z) / (b.Z - a.Z)));
        }
        return result;
    }

    // Камера находится на положительной полуоси Z и смотрит в сторону -Z.
    // Отсекаем ребро ДО деления на W, иначе у камеры возникают бесконечные линии.
    public bool ProjectSegment(Point3D start, Point3D end, out Point3D a, out Point3D b)
    {
        a = View.TransformPoint(start);
        b = View.TransformPoint(end);
        if (Mode == ProjectionMode.Perspective)
        {
            double limit = CameraDistance - NearPlane;
            if (a.Z > limit && b.Z > limit) return false;
            if (a.Z > limit)
                a += (b - a) * ((limit - a.Z) / (b.Z - a.Z));
            else if (b.Z > limit)
                b += (a - b) * ((limit - b.Z) / (a.Z - b.Z));
        }
        a = ProjectionMatrix.TransformPoint(a);
        b = ProjectionMatrix.TransformPoint(b);
        return true;
    }
}
