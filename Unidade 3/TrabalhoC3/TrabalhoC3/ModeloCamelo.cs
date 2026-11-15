using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace TrabalhoC3;

/// <summary>
/// Modelo hierárquico do camelo, seguindo o grafo do enunciado
/// ((J) = transformação de junta, animada):
///
///   Camel ─┬─ (J) pescoço ─────────── Head
///          ├─ Torso
///          ├─ (J) quadril dianteiro esq. ─┐
///          ├─ (J) quadril traseiro esq. ──┴─ Left leg ─┬─ Thigh
///          │                                           └─ (J) joelho esq. ─┐
///          ├─ (J) quadril dianteiro dir. ─┐                                ├─ Lower leg ─┬─ Foot
///          └─ (J) quadril traseiro dir. ──┴─ Right leg ─┬─ Thigh           │             └─ Shin
///                                                       └─ (J) joelho dir. ┘
///
/// Cada aresta do grafo vira uma INSTÂNCIA: um Model3DGroup "invólucro" que guarda a
/// transformação (de instância e/ou de junta) e contém o modelo filho COMPARTILHADO.
/// Assim, o mesmo Left leg aparece duas vezes (quadris dianteiro e traseiro), o mesmo
/// Lower leg é usado pelas duas pernas, o mesmo Thigh pelas duas pernas, etc.
///
/// Sistema local do camelo: frente em +X, para cima em +Y, lado esquerdo em +Z;
/// as patas tocam o chão em y = 0.
/// </summary>
public class ModeloCamelo
{
    // ---- Dimensões (unidades da cena) ----
    private const double ComprimentoCoxa = 1.2;
    private const double ComprimentoCanela = 1.15;
    private const double AlturaPe = 0.15;

    /// <summary>Altura das articulações do quadril = coxa + canela + pé (pata apoiada no chão).</summary>
    public const double AlturaQuadril = ComprimentoCoxa + ComprimentoCanela + AlturaPe;

    private const double QuadrilX = 1.15;  // distância dos quadris ao centro do corpo (frente / trás)
    private const double QuadrilZ = 0.42;  // afastamento lateral das pernas

    // ---- As sete transformações de junta ----
    // Todas giram em torno do eixo Z local. Pela regra da mão direita, ângulo positivo
    // leva a ponta da peça para a frente (+X): perna avança, pescoço inclina para trás.
    public AxisAngleRotation3D JuntaPescoco { get; } = NovaJunta();
    public AxisAngleRotation3D QuadrilDianteiroEsquerdo { get; } = NovaJunta();
    public AxisAngleRotation3D QuadrilTraseiroEsquerdo { get; } = NovaJunta();
    public AxisAngleRotation3D QuadrilDianteiroDireito { get; } = NovaJunta();
    public AxisAngleRotation3D QuadrilTraseiroDireito { get; } = NovaJunta();
    public AxisAngleRotation3D JoelhoEsquerdo { get; } = NovaJunta();
    public AxisAngleRotation3D JoelhoDireito { get; } = NovaJunta();

    /// <summary>Raiz da hierarquia: o elemento agrupador final "Camel".</summary>
    public Model3DGroup Camelo { get; }

    /// <summary>
    /// Construtor: cria os primitivos e os encaixa no grafo hierárquico
    /// (primitivos → Lower leg → Left/Right leg → Camel).
    /// </summary>
    public ModeloCamelo()
    {
        // ================= 1) PRIMITIVOS =================
        // Cada um é definido no próprio sistema local. Nos membros a origem fica
        // na articulação de cima e a peça cresce para baixo (-Y), de modo que a
        // rotação da junta acontece exatamente na articulação.
        GeometryModel3D coxa = Primitivo(CriarCoxa(), "#B08355");    // Thigh
        GeometryModel3D canela = Primitivo(CriarCanela(), "#C49A6C"); // Shin
        GeometryModel3D pe = Primitivo(CriarPe(), "#5C4330");         // Foot
        GeometryModel3D torso = Primitivo(CriarTorso(), "#C8A06E");   // Torso
        GeometryModel3D cabeca = Primitivo(CriarCabeca(), "#D2AE7E"); // Head

        // ================= 2) AGRUPADORES =================

        // Lower leg = Shin + Foot (o pé é instanciado na ponta de baixo da canela)
        Model3DGroup pernaInferior = Grupo(
            Instancia(canela, Transform3D.Identity),
            Instancia(pe, new TranslateTransform3D(0, -ComprimentoCanela, 0)));

        // Left leg / Right leg = Thigh + Lower leg, articulado pelo joelho na ponta da coxa
        Model3DGroup pernaEsquerda = Grupo(
            Instancia(coxa, Transform3D.Identity),
            Instancia(pernaInferior, Junta(JoelhoEsquerdo, 0, -ComprimentoCoxa, 0)));

        Model3DGroup pernaDireita = Grupo(
            Instancia(coxa, Transform3D.Identity),
            Instancia(pernaInferior, Junta(JoelhoDireito, 0, -ComprimentoCoxa, 0)));

        // Camel = Head + Torso + quatro pernas articuladas nos quadris
        Camelo = Grupo(
            Instancia(cabeca, Junta(JuntaPescoco, 1.35, 3.05, 0)),
            Instancia(torso, new TranslateTransform3D(0, 2.9, 0)),
            Instancia(pernaEsquerda, Junta(QuadrilDianteiroEsquerdo, QuadrilX, AlturaQuadril, QuadrilZ)),
            Instancia(pernaEsquerda, Junta(QuadrilTraseiroEsquerdo, -QuadrilX, AlturaQuadril, QuadrilZ)),
            Instancia(pernaDireita, Junta(QuadrilDianteiroDireito, QuadrilX, AlturaQuadril, -QuadrilZ)),
            Instancia(pernaDireita, Junta(QuadrilTraseiroDireito, -QuadrilX, AlturaQuadril, -QuadrilZ)));
    }

    /// <summary>
    /// Anima as sete juntas com o andar típico do camelo (andadura / "pace"): as duas
    /// pernas do MESMO lado avançam juntas, alternando com o outro lado. Isso combina com
    /// o modelo hierárquico, em que um único joelho esquerdo serve às duas pernas esquerdas.
    /// </summary>
    /// <param name="periodo">Duração de um ciclo completo (passo esquerdo + passo direito), em segundos.</param>
    /// <param name="amplitudeQuadril">Ângulo máximo, em graus, que a perna avança ou recua.</param>
    public void IniciarAnimacao(double periodo, double amplitudeQuadril)
    {
        // Duração de um ciclo e amplitude (nome curto) usadas em todos os quadros-chave
        var ciclo = TimeSpan.FromSeconds(periodo);
        double a = amplitudeQuadril;

        // Quadris. Cada quadro-chave: (fração do ciclo, ângulo, suave?).
        //  - Balanço (perna no ar indo para a frente): movimento suavizado.
        //  - Apoio (pata no chão empurrando o corpo): velocidade constante, para a pata
        //    não "escorregar" no chão.
        // Lado esquerdo balança na 1ª metade do ciclo; o direito, na 2ª metade.
        var quadrilEsquerdo = QuadrosChave(ciclo, (0, -a, false), (0.5, a, true), (1, -a, false));
        var quadrilDireito = QuadrosChave(ciclo, (0, a, false), (0.5, -a, false), (1, a, true));

        // Andadura: dianteira e traseira do MESMO lado recebem a mesma animação
        QuadrilDianteiroEsquerdo.BeginAnimation(AxisAngleRotation3D.AngleProperty, quadrilEsquerdo);
        QuadrilTraseiroEsquerdo.BeginAnimation(AxisAngleRotation3D.AngleProperty, quadrilEsquerdo);
        QuadrilDianteiroDireito.BeginAnimation(AxisAngleRotation3D.AngleProperty, quadrilDireito);
        QuadrilTraseiroDireito.BeginAnimation(AxisAngleRotation3D.AngleProperty, quadrilDireito);

        // Joelhos: dobram (canela recolhe para trás = ângulo negativo) no meio do balanço,
        // erguendo a pata do chão, e ficam esticados durante o apoio.
        JoelhoEsquerdo.BeginAnimation(AxisAngleRotation3D.AngleProperty,
            QuadrosChave(ciclo, (0, 0, true), (0.25, -50, true), (0.5, 0, true), (1, 0, true)));
        JoelhoDireito.BeginAnimation(AxisAngleRotation3D.AngleProperty,
            QuadrosChave(ciclo, (0, 0, true), (0.5, 0, true), (0.75, -50, true), (1, 0, true)));

        // Pescoço: balança para a frente e para trás uma vez a cada passo (2x por ciclo)
        JuntaPescoco.BeginAnimation(AxisAngleRotation3D.AngleProperty,
            QuadrosChave(ciclo, (0, 4, true), (0.25, -6, true), (0.5, 4, true), (0.75, -6, true), (1, 4, true)));
    }

    // ===================== Auxiliares da hierarquia =====================

    /// <summary>Nova junta: rotação em torno do eixo Z local, começando em 0°.</summary>
    private static AxisAngleRotation3D NovaJunta() => new(new Vector3D(0, 0, 1), 0);

    /// <summary>Primitivo = geometria (MeshGeometry3D) + material de cor sólida.</summary>
    private static GeometryModel3D Primitivo(MeshGeometry3D malha, string cor)
    {
        // Material difuso com a cor informada em hexadecimal (ex.: "#B08355")
        var material = new DiffuseMaterial(new SolidColorBrush((Color)ColorConverter.ConvertFromString(cor)));

        // A malha não muda mais: congelá-la evita cópias ao compartilhá-la entre instâncias
        malha.Freeze();
        return new GeometryModel3D(malha, material);
    }

    /// <summary>Agrupador: junta vários nós (instâncias) num único modelo.</summary>
    private static Model3DGroup Grupo(params Model3D[] filhos)
    {
        // Grupo vazio que recebe cada filho na ordem informada
        var grupo = new Model3DGroup();
        foreach (Model3D filho in filhos)
            grupo.Children.Add(filho);
        return grupo;
    }

    /// <summary>
    /// Instância: invólucro que aplica uma transformação a um modelo compartilhado.
    /// O mesmo modelo pode ser instanciado várias vezes com transformações diferentes.
    /// </summary>
    private static Model3DGroup Instancia(Model3D modelo, Transform3D transformacao)
    {
        // O invólucro guarda a transformação; o filho é o modelo compartilhado (não é copiado)
        var instancia = new Model3DGroup { Transform = transformacao };
        instancia.Children.Add(modelo);
        return instancia;
    }

    /// <summary>
    /// Transformação de junta + transformação de instância. A ordem importa:
    /// 1º gira a peça em torno da sua origem (a articulação) e
    /// 2º leva essa articulação até o ponto de encaixe no pai (x, y, z).
    /// </summary>
    private static Transform3DGroup Junta(AxisAngleRotation3D rotacao, double x, double y, double z)
    {
        // No Transform3DGroup os filhos são aplicados em sequência: rotação e depois translação
        var grupo = new Transform3DGroup();
        grupo.Children.Add(new RotateTransform3D(rotacao));
        grupo.Children.Add(new TranslateTransform3D(x, y, z));
        return grupo;
    }

    /// <summary>Animação por quadros-chave repetida para sempre ao longo de um ciclo.</summary>
    private static DoubleAnimationUsingKeyFrames QuadrosChave(TimeSpan ciclo,
        params (double fracao, double angulo, bool suave)[] quadros)
    {
        // Animação com a duração de um ciclo, repetida indefinidamente
        var animacao = new DoubleAnimationUsingKeyFrames { Duration = ciclo, RepeatBehavior = RepeatBehavior.Forever };

        // Cada quadro: instante = fração do ciclo; "suave" usa curva seno, senão interpolação linear
        foreach (var (fracao, angulo, suave) in quadros)
        {
            KeyTime instante = KeyTime.FromPercent(fracao);
            animacao.KeyFrames.Add(suave
                ? new EasingDoubleKeyFrame(angulo, instante, new SineEase { EasingMode = EasingMode.EaseInOut })
                : new LinearDoubleKeyFrame(angulo, instante));
        }
        return animacao;
    }

    // ===================== Geometria dos primitivos =====================
    // Podem ser formados por mais de uma caixa na mesma malha; continuam sendo
    // um único primitivo (um único GeometryModel3D).

    /// <summary>Thigh: afunila do quadril (em cima) até o joelho (em baixo).</summary>
    private static MeshGeometry3D CriarCoxa()
    {
        // Tronco: base estreita no joelho (y = -ComprimentoCoxa), topo largo um pouco acima do quadril
        var m = new MeshGeometry3D();
        Malhas.AdicionarTronco(m, new Point3D(0, -ComprimentoCoxa, 0), 0.26, 0.26,
                                  new Point3D(0, 0.15, 0), 0.46, 0.36);
        return m;
    }

    /// <summary>Shin: do joelho até o tornozelo, com uma pequena "rótula" no topo.</summary>
    private static MeshGeometry3D CriarCanela()
    {
        // Canela afunilada: do joelho (y = 0) até o tornozelo (y = -ComprimentoCanela)
        var m = new MeshGeometry3D();
        Malhas.AdicionarTronco(m, new Point3D(0, -ComprimentoCanela, 0), 0.17, 0.17,
                                  new Point3D(0, 0, 0), 0.24, 0.24);

        // Rótula: cubo pequeno em volta da articulação, disfarçando a emenda com a coxa
        Malhas.AdicionarCaixa(m, new Point3D(-0.15, -0.15, -0.15), new Point3D(0.15, 0.1, 0.15));
        return m;
    }

    /// <summary>Foot: almofada larga e achatada, um pouco projetada para a frente.</summary>
    private static MeshGeometry3D CriarPe()
    {
        // Caixa achatada abaixo do tornozelo, avançando mais para +X (frente) que para trás
        var m = new MeshGeometry3D();
        Malhas.AdicionarCaixa(m, new Point3D(-0.2, -AlturaPe, -0.2), new Point3D(0.32, 0, 0.2));
        return m;
    }

    /// <summary>Torso: corpo + corcova + cauda (origem no centro do corpo).</summary>
    private static MeshGeometry3D CriarTorso()
    {
        // Três peças na mesma malha (um único primitivo)
        var m = new MeshGeometry3D();
        Malhas.AdicionarCaixa(m, new Point3D(-1.6, -0.55, -0.6), new Point3D(1.6, 0.55, 0.6));      // corpo
        Malhas.AdicionarTronco(m, new Point3D(-0.1, 0.55, 0), 1.6, 1.0,
                                  new Point3D(-0.1, 1.25, 0), 0.6, 0.5);                           // corcova
        Malhas.AdicionarCaixa(m, new Point3D(-1.72, -0.65, -0.06), new Point3D(-1.6, 0.35, 0.06)); // cauda
        return m;
    }

    /// <summary>Head: pescoço inclinado para a frente + cabeça + focinho + orelhas (origem na base do pescoço).</summary>
    private static MeshGeometry3D CriarCabeca()
    {
        // Cinco peças na mesma malha; a junta do pescoço gira todas juntas
        var m = new MeshGeometry3D();
        Malhas.AdicionarTronco(m, new Point3D(0, 0, 0), 0.55, 0.4,
                                  new Point3D(0.75, 1.35, 0), 0.32, 0.28);                        // pescoço
        Malhas.AdicionarCaixa(m, new Point3D(0.5, 1.2, -0.22), new Point3D(1.45, 1.62, 0.22));     // cabeça
        Malhas.AdicionarCaixa(m, new Point3D(1.35, 1.2, -0.17), new Point3D(1.62, 1.5, 0.17));     // focinho
        Malhas.AdicionarCaixa(m, new Point3D(0.6, 1.62, 0.1), new Point3D(0.72, 1.8, 0.2));        // orelha esq.
        Malhas.AdicionarCaixa(m, new Point3D(0.6, 1.62, -0.2), new Point3D(0.72, 1.8, -0.1));      // orelha dir.
        return m;
    }
}
