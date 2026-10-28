namespace PolyhedraLab.Models;

/// <summary>Грань — многоугольник с циклически упорядоченными индексами общих вершин.</summary>
public sealed class Face
{
    public int[] VertexIndices { get; }

    public Face(IEnumerable<int> indices)
    {
        VertexIndices = indices.ToArray();
        if (VertexIndices.Length < 3)
            throw new ArgumentException("У грани должно быть хотя бы три вершины.");
    }
}
