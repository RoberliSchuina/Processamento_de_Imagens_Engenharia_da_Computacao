using System.Windows;
using System.Windows.Media.Media3D;

namespace TrabalhoC2;

/// <summary>
/// Funções auxiliares para montar malhas triangulares (MeshGeometry3D).
///
/// Convenção do WPF: a face visível de um triângulo é aquela em que os vértices
/// aparecem em sentido ANTI-HORÁRIO para quem olha. As normais não são informadas:
/// o WPF as calcula a partir dessa ordem. Como cada face recebe seus próprios
/// vértices (não compartilhados com as faces vizinhas), as arestas ficam "vivas"
/// e cada face é iluminada de forma uniforme.
/// </summary>
public static class Malhas
{
    /// <summary>
    /// Adiciona o quadrilátero a-b-c-d (anti-horário visto de fora) como dois triângulos.
    /// Coordenadas de textura: a=(0,1) b=(1,1) c=(1,0) d=(0,0), isto é, a imagem inteira na face.
    /// </summary>
    public static void AdicionarQuad(MeshGeometry3D malha, Point3D a, Point3D b, Point3D c, Point3D d)
    {
        int i = malha.Positions.Count;

        malha.Positions.Add(a);
        malha.Positions.Add(b);
        malha.Positions.Add(c);
        malha.Positions.Add(d);

        malha.TextureCoordinates.Add(new Point(0, 1));
        malha.TextureCoordinates.Add(new Point(1, 1));
        malha.TextureCoordinates.Add(new Point(1, 0));
        malha.TextureCoordinates.Add(new Point(0, 0));

        // Triângulos (a, b, c) e (a, c, d)
        malha.TriangleIndices.Add(i);
        malha.TriangleIndices.Add(i + 1);
        malha.TriangleIndices.Add(i + 2);
        malha.TriangleIndices.Add(i);
        malha.TriangleIndices.Add(i + 2);
        malha.TriangleIndices.Add(i + 3);
    }

    /// <summary>Adiciona o triângulo a-b-c (anti-horário visto de fora).</summary>
    public static void AdicionarTriangulo(MeshGeometry3D malha, Point3D a, Point3D b, Point3D c)
    {
        int i = malha.Positions.Count;

        malha.Positions.Add(a);
        malha.Positions.Add(b);
        malha.Positions.Add(c);

        malha.TextureCoordinates.Add(new Point(0, 1));
        malha.TextureCoordinates.Add(new Point(1, 1));
        malha.TextureCoordinates.Add(new Point(0.5, 0));

        malha.TriangleIndices.Add(i);
        malha.TriangleIndices.Add(i + 1);
        malha.TriangleIndices.Add(i + 2);
    }

    /// <summary>
    /// Adiciona uma caixa alinhada aos eixos, do canto mínimo ao canto máximo (6 faces, 12 triângulos).
    /// </summary>
    public static void AdicionarCaixa(MeshGeometry3D malha, Point3D min, Point3D max)
    {
        // Os 8 vértices da caixa
        var p000 = new Point3D(min.X, min.Y, min.Z);
        var p100 = new Point3D(max.X, min.Y, min.Z);
        var p101 = new Point3D(max.X, min.Y, max.Z);
        var p001 = new Point3D(min.X, min.Y, max.Z);
        var p010 = new Point3D(min.X, max.Y, min.Z);
        var p110 = new Point3D(max.X, max.Y, min.Z);
        var p111 = new Point3D(max.X, max.Y, max.Z);
        var p011 = new Point3D(min.X, max.Y, max.Z);

        AdicionarQuad(malha, p001, p101, p111, p011); // frente   (+Z)
        AdicionarQuad(malha, p100, p000, p010, p110); // fundos   (-Z)
        AdicionarQuad(malha, p101, p100, p110, p111); // direita  (+X)
        AdicionarQuad(malha, p000, p001, p011, p010); // esquerda (-X)
        AdicionarQuad(malha, p011, p111, p110, p010); // topo     (+Y)
        AdicionarQuad(malha, p000, p100, p101, p001); // base     (-Y)
    }

    /// <summary>Cria uma malha contendo uma única caixa.</summary>
    public static MeshGeometry3D CriarCaixa(Point3D min, Point3D max)
    {
        var malha = new MeshGeometry3D();
        AdicionarCaixa(malha, min, max);
        return malha;
    }

    /// <summary>
    /// Telhado de duas águas (prisma de seção triangular) com a cumeeira paralela ao
    /// eixo X, no meio da profundidade (z). Formado por 2 retângulos inclinados (águas),
    /// 2 triângulos (oitões) e o forro (face de baixo).
    /// </summary>
    public static MeshGeometry3D CriarTelhado(double xMin, double xMax, double zMin, double zMax,
                                              double yBase, double yCumeeira)
    {
        double zMeio = (zMin + zMax) / 2;

        // Beirais (base do telhado) e extremidades da cumeeira
        var frenteEsq = new Point3D(xMin, yBase, zMax);
        var frenteDir = new Point3D(xMax, yBase, zMax);
        var fundoEsq = new Point3D(xMin, yBase, zMin);
        var fundoDir = new Point3D(xMax, yBase, zMin);
        var cumeEsq = new Point3D(xMin, yCumeeira, zMeio);
        var cumeDir = new Point3D(xMax, yCumeeira, zMeio);

        var malha = new MeshGeometry3D();
        AdicionarQuad(malha, frenteEsq, frenteDir, cumeDir, cumeEsq);  // água da frente
        AdicionarQuad(malha, fundoDir, fundoEsq, cumeEsq, cumeDir);    // água dos fundos
        AdicionarTriangulo(malha, frenteDir, fundoDir, cumeDir);       // oitão direito  (+X)
        AdicionarTriangulo(malha, fundoEsq, frenteEsq, cumeEsq);       // oitão esquerdo (-X)
        AdicionarQuad(malha, fundoEsq, fundoDir, frenteDir, frenteEsq); // forro (-Y)
        return malha;
    }

    /// <summary>
    /// Plano horizontal (y = 0), quadrado de lado <paramref name="tamanho"/> e centrado na
    /// origem, dividido em ladrilhos de lado <paramref name="ladrilho"/>. Cada ladrilho recebe
    /// as coordenadas de textura (0,0)-(1,1), ou seja, a imagem inteira: a textura aparece
    /// repetida N x N vezes.
    /// Obs.: no WPF a imagem é esticada sobre o retângulo que envolve TODAS as coordenadas de
    /// textura da malha; por isso usar valores maiores que 1 não repete a imagem — é preciso
    /// subdividir o plano como feito aqui.
    /// </summary>
    public static MeshGeometry3D CriarPlanoLadrilhado(double tamanho, double ladrilho)
    {
        var malha = new MeshGeometry3D();
        int n = (int)Math.Ceiling(tamanho / ladrilho);
        double inicio = -n * ladrilho / 2;

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                double x0 = inicio + i * ladrilho, x1 = x0 + ladrilho;
                double z0 = inicio + j * ladrilho, z1 = z0 + ladrilho;

                // Face voltada para cima (+Y)
                AdicionarQuad(malha,
                    new Point3D(x0, 0, z1), new Point3D(x1, 0, z1),
                    new Point3D(x1, 0, z0), new Point3D(x0, 0, z0));
            }
        }

        malha.Freeze(); // malha estática: congelar evita cópias e melhora o desempenho
        return malha;
    }
}
