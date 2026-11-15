using System.Windows;
using System.Windows.Media.Media3D;

namespace TrabalhoC2;

/// <summary>
/// Janela principal: monta as malhas (MeshGeometry3D) dos modelos declarados em
/// MainWindow.xaml. Câmera, luzes, materiais e animações estão no XAML.
/// </summary>
public partial class MainWindow : Window
{
    // ---- Medidas da casa (unidades ~ metros) ----
    private const double AlturaParede = 3;   // pé-direito dos cômodos
    private const double AlturaCumeeira = 6; // ponto mais alto do telhado
    private const double Beiral = 0.5;       // quanto o telhado avança além das paredes

    public MainWindow()
    {
        InitializeComponent();

        // Planta baixa (vista de cima). A frente da casa está voltada para +Z,
        // que é o lado onde a câmera começa o movimento.
        //
        //     x = -7 ......... 0 ......... +7
        //  z=-4  +-------------+------------+
        //        |             |  COZINHA   |
        //        |    SALA     +------------+ z = 0
        //        |             |   QUARTO   |
        //  z=+4  +-------------+------------+
        sala.Geometry = Malhas.CriarCaixa(new Point3D(-7, 0, -4), new Point3D(0, AlturaParede, 4));
        cozinha.Geometry = Malhas.CriarCaixa(new Point3D(0, 0, -4), new Point3D(7, AlturaParede, 0));
        quarto.Geometry = Malhas.CriarCaixa(new Point3D(0, 0, 0), new Point3D(7, AlturaParede, 4));

        // Telhado triangular cobrindo os três cômodos
        telhado.Geometry = Malhas.CriarTelhado(-7 - Beiral, 7 + Beiral, -4 - Beiral, 4 + Beiral,
                                               AlturaParede, AlturaCumeeira);

        // Porta na frente da sala e janelas espalhadas pelas paredes externas
        porta.Geometry = Malhas.CriarCaixa(new Point3D(-4.6, 0, 4), new Point3D(-3.4, 2.2, 4.06));
        janelas.Geometry = CriarJanelas();

        // Chão de 1000 x 1000 com ladrilhos de 10 x 10 => granito repetido 100 x 100 vezes.
        // É grande o bastante para preencher a visão da câmera até perto do horizonte.
        chao.Geometry = Malhas.CriarPlanoLadrilhado(1000, 10);
    }

    /// <summary>
    /// Janelas: placas finas (0,06 de espessura) encostadas do lado de fora das paredes.
    /// </summary>
    private static MeshGeometry3D CriarJanelas()
    {
        const double e = 0.06;
        var malha = new MeshGeometry3D();

        // Frente (z = +4): sala e quarto
        Malhas.AdicionarCaixa(malha, new Point3D(-2.2, 1.2, 4), new Point3D(-0.8, 2.2, 4 + e));
        Malhas.AdicionarCaixa(malha, new Point3D(2.8, 1.2, 4), new Point3D(4.2, 2.2, 4 + e));

        // Fundos (z = -4): sala e cozinha
        Malhas.AdicionarCaixa(malha, new Point3D(-4.2, 1.2, -4 - e), new Point3D(-2.8, 2.2, -4));
        Malhas.AdicionarCaixa(malha, new Point3D(2.8, 1.2, -4 - e), new Point3D(4.2, 2.2, -4));

        // Lateral direita (x = +7): cozinha e quarto
        Malhas.AdicionarCaixa(malha, new Point3D(7, 1.2, -2.7), new Point3D(7 + e, 2.2, -1.3));
        Malhas.AdicionarCaixa(malha, new Point3D(7, 1.2, 1.3), new Point3D(7 + e, 2.2, 2.7));

        // Lateral esquerda (x = -7): sala
        Malhas.AdicionarCaixa(malha, new Point3D(-7 - e, 1.2, -0.7), new Point3D(-7, 2.2, 0.7));

        return malha;
    }
}
