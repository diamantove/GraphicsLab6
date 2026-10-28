using PolyhedraLab.Models;

namespace PolyhedraLab.Transforms;

/// <summary>Однородные координаты: столбец (x,y,z,1). В A*B сначала действует B.</summary>
public sealed class Matrix4x4
{
    private readonly double[,] values;
    public double this[int row, int column] => values[row, column];

    public Matrix4x4(double[,] values)
    {
        if (values.GetLength(0) != 4 || values.GetLength(1) != 4)
            throw new ArgumentException("Матрица должна иметь размер 4×4.");
        this.values = (double[,])values.Clone();
    }

    public static Matrix4x4 Identity() => new(new double[,] {
        {1,0,0,0}, {0,1,0,0}, {0,0,1,0}, {0,0,0,1} });

    public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
    {
        var result = new double[4, 4];
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
                for (int k = 0; k < 4; k++)
                    result[row, col] += a[row, k] * b[k, col];
        return new Matrix4x4(result);
    }

    public Point3D TransformPoint(Point3D p)
    {
        // Обычное умножение матрицы на столбец (x, y, z, 1).
        double[] input = [p.X, p.Y, p.Z, 1];
        var result = new double[4];
        for (int row = 0; row < 4; row++)
            for (int k = 0; k < 4; k++)
                result[row] += values[row, k] * input[k];
        double w = result[3];
        if (Math.Abs(w) < 1e-12)
            throw new ArgumentException("Невозможно выполнить деление на нулевую однородную координату.");
        // У аффинных преобразований w = 1; у перспективы нужно деление на w.
        return new(result[0] / w, result[1] / w, result[2] / w);
    }

    public static Matrix4x4 Translation(double x, double y, double z) => new(new double[,] {
        {1,0,0,x}, {0,1,0,y}, {0,0,1,z}, {0,0,0,1} });

    public static Matrix4x4 Scaling(double x, double y, double z) => new(new double[,] {
        {x,0,0,0}, {0,y,0,0}, {0,0,z,0}, {0,0,0,1} });

    public static Matrix4x4 RotationX(double degrees)
    {
        double a = degrees * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
        return new(new double[,] { {1,0,0,0}, {0,c,-s,0}, {0,s,c,0}, {0,0,0,1} });
    }

    public static Matrix4x4 RotationY(double degrees)
    {
        double a = degrees * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
        return new(new double[,] { {c,0,s,0}, {0,1,0,0}, {-s,0,c,0}, {0,0,0,1} });
    }

    public static Matrix4x4 RotationZ(double degrees)
    {
        double a = degrees * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
        return new(new double[,] { {c,-s,0,0}, {s,c,0,0}, {0,0,1,0}, {0,0,0,1} });
    }

    public static Matrix4x4 AroundPoint(Point3D center, Matrix4x4 operation) =>
        Translation(center.X, center.Y, center.Z) * operation * Translation(-center.X, -center.Y, -center.Z);

    public static Matrix4x4 Reflection(int plane) => plane switch
    {
        0 => Scaling(1, 1, -1), // XY: меняется знак Z
        1 => Scaling(1, -1, 1), // XZ: меняется знак Y
        2 => Scaling(-1, 1, 1), // YZ: меняется знак X
        _ => throw new ArgumentException("Выберите координатную плоскость.")
    };

    public static Matrix4x4 RotationAroundLine(Point3D a, Point3D b, double degrees)
    {
        Point3D direction = b - a;
        if (direction.Length < 1e-9)
            throw new ArgumentException("Точки A и B должны различаться: они задают ось вращения.");
        var u = direction * (1 / direction.Length);
        double angle = degrees * Math.PI / 180;
        double c = Math.Cos(angle), s = Math.Sin(angle), t = 1 - c;
        double x = u.X, y = u.Y, z = u.Z;
        // Формула Родрига: R = cos(a)I + (1-cos(a))uuᵀ + sin(a)[u]×.
        var rotation = new Matrix4x4(new double[,] {
            {c+x*x*t, x*y*t-z*s, x*z*t+y*s, 0},
            {y*x*t+z*s, c+y*y*t, y*z*t-x*s, 0},
            {z*x*t-y*s, z*y*t+x*s, c+z*z*t, 0},
            {0,0,0,1} });
        // Переносим A в начало координат, вращаем, возвращаем обратно.
        return AroundPoint(a, rotation);
    }
}
