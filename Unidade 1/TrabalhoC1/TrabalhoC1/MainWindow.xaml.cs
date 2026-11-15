using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TrabalhoC1;

/// <summary>
/// Janela principal: cria as animações da locomotiva.
///  - Storyboard (rotação das rodas, biela de acoplamento e translação do conjunto);
///  - Cálculo quadro a quadro da biela principal e da cruzeta (CompositionTarget.Rendering).
/// </summary>
public partial class MainWindow : Window
{
    // ---- Medidas (em pixels) do desenho feito em MainWindow.xaml ----
    private const double LarguraLocomotiva = 500;  // largura do Canvas "locomotiva"
    private const double RaioRodaMotriz = 40;      // raio das rodas motrizes
    private const double RaioRodaPequena = 20;     // raio da roda-guia dianteira
    private const double RaioManivela = 24;        // distância do pino da biela ao centro da roda
    private const double ComprimentoBiela = 140;   // distância entre o pino e a cruzeta
    private const double CentroRodaMotriz2X = 240; // roda onde a biela principal está presa
    private const double EixoY = 190;              // altura dos eixos motrizes e da linha do pistão

    // Velocidade de deslocamento da locomotiva (pixels por segundo)
    private const double Velocidade = 140;

    // Caminhos (PropertyPath) até as propriedades animadas dentro dos TransformGroups
    private const string CaminhoTranslacaoX = "(UIElement.RenderTransform).(TranslateTransform.X)";
    private const string CaminhoRotacaoRoda = "(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.Angle)";
    private const string CaminhoAcoplamentoAntiGiro = "(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)";
    private const string CaminhoAcoplamentoGiro = "(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)";

    public MainWindow()
    {
        InitializeComponent();

        // A distância percorrida depende da largura da janela: (re)inicia a
        // animação sempre que a largura da "pista" mudar (inclusive na abertura).
        pista.SizeChanged += (_, e) =>
        {
            if (e.WidthChanged)
                IniciarAnimacao();
        };

        // Evento disparado uma vez por quadro, logo após o WPF atualizar as
        // animações; usado para posicionar a biela principal e a cruzeta.
        CompositionTarget.Rendering += AtualizarBielaPrincipal;
        Closed += (_, _) => CompositionTarget.Rendering -= AtualizarBielaPrincipal;
    }

    /// <summary>
    /// Monta e inicia a Storyboard. Todas as animações têm a mesma duração e a
    /// mesma suavização; com AutoReverse a locomotiva vai até a borda direita,
    /// volta até a esquerda (com as rodas girando ao contrário) e repete para sempre.
    /// </summary>
    private void IniciarAnimacao()
    {
        // Espaço livre para a locomotiva andar sem sair da janela
        double distancia = pista.ActualWidth - LarguraLocomotiva;
        if (distancia < 1)
            return;

        var duracao = new Duration(TimeSpan.FromSeconds(distancia / Velocidade));

        // Rolamento sem deslizar: ao percorrer "d" pixels, uma roda de raio "r"
        // gira d / r radianos. Rodas menores giram mais rápido.
        double giroMotriz = Graus(distancia / RaioRodaMotriz);
        double giroPequena = Graus(distancia / RaioRodaPequena);

        var storyboard = new Storyboard
        {
            AutoReverse = true,                    // volta pelo mesmo caminho
            RepeatBehavior = RepeatBehavior.Forever // ida e volta indefinidamente
        };

        // Translação do Canvas com toda a locomotiva
        AdicionarAnimacao(storyboard, locomotiva, CaminhoTranslacaoX, distancia, duracao);

        // Rotação de cada instância da roda
        foreach (Control roda in new[] { rodaTraseira, rodaMotriz1, rodaMotriz2, rodaMotriz3 })
            AdicionarAnimacao(storyboard, roda, CaminhoRotacaoRoda, giroMotriz, duracao);
        AdicionarAnimacao(storyboard, rodaGuia, CaminhoRotacaoRoda, giroPequena, duracao);

        // Biela de acoplamento: gira +θ junto com as rodas motrizes e -θ para
        // cancelar a própria rotação, resultando em translação circular.
        AdicionarAnimacao(storyboard, bielaAcoplamento, CaminhoAcoplamentoAntiGiro, -giroMotriz, duracao);
        AdicionarAnimacao(storyboard, bielaAcoplamento, CaminhoAcoplamentoGiro, giroMotriz, duracao);

        // Begin substitui (SnapshotAndReplace) uma Storyboard anterior, caso a janela seja redimensionada
        storyboard.Begin();
    }

    /// <summary>
    /// Cria uma DoubleAnimation de 0 até <paramref name="valorFinal"/> e a adiciona à Storyboard.
    /// A suavização (SineEase) faz a locomotiva acelerar na partida e frear antes de inverter
    /// o sentido; como todas as animações usam a mesma curva, rodas e deslocamento continuam
    /// sincronizados.
    /// </summary>
    private static void AdicionarAnimacao(Storyboard storyboard, DependencyObject alvo, string caminho,
                                          double valorFinal, Duration duracao)
    {
        var animacao = new DoubleAnimation(0, valorFinal, duracao)
        {
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(animacao, alvo);
        Storyboard.SetTargetProperty(animacao, new PropertyPath(caminho));
        storyboard.Children.Add(animacao);
    }

    /// <summary>
    /// Executado a cada quadro: posiciona a biela principal e a cruzeta a partir do
    /// ângulo atual da 2ª roda motriz (mecanismo biela-manivela).
    /// </summary>
    private void AtualizarBielaPrincipal(object? sender, EventArgs e)
    {
        // Ângulo atual (já animado pela Storyboard) da roda onde a biela está presa
        var rotacaoRoda = (RotateTransform)((TransformGroup)rodaMotriz2.RenderTransform).Children[1];
        double teta = Radianos(rotacaoRoda.Angle);

        // Pino da manivela: gira com a roda, a RaioManivela px do centro
        double pinoX = CentroRodaMotriz2X + RaioManivela * Math.Cos(teta);
        double pinoY = EixoY + RaioManivela * Math.Sin(teta);

        // A cruzeta só desliza sobre a reta y = EixoY. Como a biela é rígida
        // (comprimento fixo), pelo teorema de Pitágoras:
        //   dx² + dy² = ComprimentoBiela²  =>  dx = √(L² - dy²)
        double dy = EixoY - pinoY;
        double dx = Math.Sqrt(ComprimentoBiela * ComprimentoBiela - dy * dy);

        // Biela: translada até o pino e gira até apontar para a cruzeta
        transBiela.X = pinoX;
        transBiela.Y = pinoY;
        rotBiela.Angle = Graus(Math.Atan2(dy, dx));

        // Cruzeta (e haste do pistão) acompanham a ponta da biela
        transCruzeta.X = pinoX + dx;
    }

    private static double Graus(double radianos) => radianos * 180.0 / Math.PI;

    private static double Radianos(double graus) => graus * Math.PI / 180.0;
}
