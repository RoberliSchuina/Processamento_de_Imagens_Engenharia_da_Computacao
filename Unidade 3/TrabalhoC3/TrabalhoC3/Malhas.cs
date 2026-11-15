using System.Windows;
using System.Windows.Media.Media3D;

namespace TrabalhoC3;

/// <summary>
/// Funções auxiliares para montar malhas triangulares (MeshGeometry3D).
///
/// Convenção do WPF: a face visível de um triângulo é aquela em que os vértices
/// aparecem em sentido ANTI-HORÁRIO para quem olha. As normais não são informadas:
/// o WPF as calcula a partir dessa ordem. Como cada face recebe seus próprios
/// vértices, as arestas ficam "vivas" e cada face é iluminada de forma uniforme.
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

    /// <summary>
    /// Adiciona um hexaedro (caixa, possivelmente afunilada/inclinada) a partir das
    /// 4 quinas de baixo e das 4 de cima, ambas na ordem (-x,-z), (+x,-z), (+x,+z), (-x,+z).
    /// </summary>
    public static void AdicionarHexaedro(MeshGeometry3D malha, Point3D[] baixo, Point3D[] cima)
    {
        AdicionarQuad(malha, baixo[3], baixo[2], cima[2], cima[3]); // frente   (+Z)
        AdicionarQuad(malha, baixo[1], baixo[0], cima[0], cima[1]); // fundos   (-Z)
        AdicionarQuad(malha, baixo[2], baixo[1], cima[1], cima[2]); // direita  (+X)
        AdicionarQuad(malha, baixo[0], baixo[3], cima[3], cima[0]); // esquerda (-X)
        AdicionarQuad(malha, cima[3], cima[2], cima[1], cima[0]);   // topo     (+Y)
        AdicionarQuad(malha, baixo[0], baixo[1], baixo[2], baixo[3]); // base   (-Y)
    }

    /// <summary>Adiciona uma caixa alinhada aos eixos, do canto mínimo ao canto máximo.</summary>
    public static void AdicionarCaixa(MeshGeometry3D malha, Point3D min, Point3D max)
    {
        AdicionarHexaedro(malha,
            Retangulo(min.X, max.X, min.Y, min.Z, max.Z),
            Retangulo(min.X, max.X, max.Y, min.Z, max.Z));
    }

    /// <summary>
    /// Adiciona um tronco de pirâmide de base retangular: um retângulo horizontal centrado em
    /// <paramref name="centroBaixo"/> ligado a outro centrado em <paramref name="centroCima"/>.
    /// Serve para membros afunilados e para o pescoço inclinado.
    /// </summary>
    public static void AdicionarTronco(MeshGeometry3D malha,
                                       Point3D centroBaixo, double larguraXBaixo, double larguraZBaixo,
                                       Point3D centroCima, double larguraXCima, double larguraZCima)
    {
        AdicionarHexaedro(malha,
            Retangulo(centroBaixo.X - larguraXBaixo / 2, centroBaixo.X + larguraXBaixo / 2, centroBaixo.Y,
                      centroBaixo.Z - larguraZBaixo / 2, centroBaixo.Z + larguraZBaixo / 2),
            Retangulo(centroCima.X - larguraXCima / 2, centroCima.X + larguraXCima / 2, centroCima.Y,
                      centroCima.Z - larguraZCima / 2, centroCima.Z + larguraZCima / 2));
    }

    /// <summary>As 4 quinas de um retângulo horizontal (altura y) na ordem usada por AdicionarHexaedro.</summary>
    private static Point3D[] Retangulo(double x0, double x1, double y, double z0, double z1) =>
    [
        new Point3D(x0, y, z0), new Point3D(x1, y, z0), new Point3D(x1, y, z1), new Point3D(x0, y, z1)
    ];

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
