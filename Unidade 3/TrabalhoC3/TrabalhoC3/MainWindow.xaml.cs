using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace TrabalhoC3;

/// <summary>
/// Janela principal: cria o deserto, instancia quatro camelos a partir de um único
/// modelo hierárquico e os faz caminhar em círculo.
/// </summary>
public partial class MainWindow : Window
{
    private const double RaioCirculo = 7;       // raio do círculo percorrido pelos camelos
    private const int QuantidadeCamelos = 4;
    private const double PeriodoPassada = 1.6;  // duração de um ciclo de passos (s)
    private const double AmplitudeQuadril = 16; // abertura máxima das pernas (graus)

    public MainWindow()
    {
        InitializeComponent();

        // Deserto de 400 x 400 com ladrilhos de 12 x 12 (textura de areia repetida ~34 x 34 vezes),
        // grande o suficiente para preencher toda a visão da câmera.
        deserto.Geometry = Malhas.CriarPlanoLadrilhado(400, 12);

        // Um único modelo hierárquico de camelo, com as sete juntas animadas...
        var modelo = new ModeloCamelo();
        modelo.IniciarAnimacao(PeriodoPassada, AmplitudeQuadril);

        // ...instanciado quatro vezes. Cada instância posiciona o camelo na borda do
        // círculo (z = R), virado para +X (direção tangente ao círculo), e depois o gira
        // em torno do eixo Y para distribuir os camelos a cada 360°/4 = 90°.
        var rotacaoCaravana = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
        var grupoCaravana = new Model3DGroup { Transform = new RotateTransform3D(rotacaoCaravana) };

        for (int i = 0; i < QuantidadeCamelos; i++)
        {
            var transformacaoInstancia = new Transform3DGroup();
            transformacaoInstancia.Children.Add(new TranslateTransform3D(0, 0, RaioCirculo));
            transformacaoInstancia.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 1, 0), i * 360.0 / QuantidadeCamelos)));

            var instancia = new Model3DGroup { Transform = transformacaoInstancia };
            instancia.Children.Add(modelo.Camelo); // mesmo modelo compartilhado pelas 4 instâncias
            grupoCaravana.Children.Add(instancia);
        }

        caravana.Content = grupoCaravana;

        // Deslocamento em círculo: o grupo inteiro gira em torno do eixo Y. Girando no
        // sentido positivo, o camelo em z = R anda para +X, ou seja, para a frente.
        //
        // A velocidade é calculada a partir da passada para as patas não "escorregarem":
        // durante o apoio (meio ciclo) a pata, com a perna esticada de comprimento L,
        // vai de +a até -a e recua 2·L·sen(a) em relação ao corpo.
        double velocidade = 2 * ModeloCamelo.AlturaQuadril * Math.Sin(AmplitudeQuadril * Math.PI / 180)
                            / (PeriodoPassada / 2);
        double tempoVolta = 2 * Math.PI * RaioCirculo / velocidade;

        rotacaoCaravana.BeginAnimation(AxisAngleRotation3D.AngleProperty,
            new DoubleAnimation(0, 360, TimeSpan.FromSeconds(tempoVolta)) { RepeatBehavior = RepeatBehavior.Forever });
    }
}
