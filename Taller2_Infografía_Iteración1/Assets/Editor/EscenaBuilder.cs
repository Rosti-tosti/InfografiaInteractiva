#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using Pathfinding;

// Construye 01_Intro.unity con TODOS los elementos como GameObjects reales (visibles en Hierarchy).
// Coordenadas en pixeles del frame de Figma (1600 x 900), origen arriba-izquierda.
// Menu: Tools > Pathfinding > Construir escena 01_Intro
[InitializeOnLoad]
public static class EscenaBuilder
{
    const string RutaEscena = "Assets/Scenes/01_Intro.unity";
    static TMP_FontAsset FBold, FReg, FInter;
    static Sprite SpPanelFill, SpPanelBorder, SpCirculo, SpTaller;
    static Sprite SpByteIntro, SpByteAlerta, SpByteSonriente, SpByteConfundido, SpByteEntusiasmado, SpByteReflexivo;
    static Texture2D TexAxel;

    static EscenaBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RutaEscena)) Construir();
        };
    }

    [MenuItem("Tools/Pathfinding/Construir escena 01_Intro")]
    public static void Construir()
    {
        ConfigurarImportadores();
        CargarRecursos();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camara 2D
        var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = PaletaFigma.Velo;
        camGO.transform.position = new Vector3(0, 0, -10);
        camGO.AddComponent<AudioListener>();

        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>();

        // Canvas 1600x900
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1600, 900);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGO.AddComponent<GraphicRaycaster>();

        var fondoNegro = Img(canvasGO.transform, "FondoNegro", 0, 0, 1600, 900, PaletaFigma.Velo);
        Stretch(fondoNegro.rectTransform);

        var raiz = Rt(canvasGO.transform, "Pantallas", 0, 0, 1600, 900);
        raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 0.5f); raiz.pivot = new Vector2(0.5f, 0.5f);
        raiz.anchoredPosition = Vector2.zero;

        var flowGO = new GameObject("IntroFlow");
        var flow = flowGO.AddComponent<IntroFlow>();

        // ---------------- S00 Bienvenida ----------------
        var s0 = Pantalla(raiz, "S00_Bienvenida", 0.72f);
        Header(s0); Pie(s0);
        Texto(s0, "Titulo_Linea1", "IA EN PROGRAMACIÓN", 76, 235, 1300, FBold, 94, PaletaFigma.Crema);
        Texto(s0, "Titulo_Linea2", "DE VIDEOJUEGOS", 76, 333, 1300, FBold, 94, PaletaFigma.Crema);
        Img(s0, "LineaNaranja", 81, 404, 136, 6, PaletaFigma.Naranja);
        Texto(s0, "Subtitulo_Linea1", "Rutas, decisiones y estados: explorá la lógica", 81, 453, 900, FInter, 30, PaletaFigma.Gris);
        Texto(s0, "Subtitulo_Linea2", "que hace posible un mundo de juego.", 81, 497, 900, FInter, 30, PaletaFigma.Gris);
        flow.btnComenzar = BotonAmarillo(s0, "Boton_Comenzar", 66, 690, 860, 82, "Apretá el gatillo para comenzar");

        // ---------------- S01 Asignando guia ----------------
        var s1 = Pantalla(raiz, "S01_AsignandoGuia", 0.77f);
        Header(s1); Pie(s1);
        Texto(s1, "Titulo", "ASIGNANDO GUÍA...", 76, 236, 1300, FBold, 92, PaletaFigma.Crema);
        Img(s1, "LineaNaranja", 81, 404, 136, 6, PaletaFigma.Naranja);
        Texto(s1, "Linea1", "Protocolo 30A47...", 81, 453, 900, FInter, 30, PaletaFigma.Gris);
        Texto(s1, "Linea2", "IA Byte asignado para guiar la experiencia", 81, 516, 900, FInter, 30, PaletaFigma.Gris);
        Texto(s1, "Linea3", "Lo acepta como guía?...//", 81, 582, 900, FInter, 30, PaletaFigma.Gris);
        flow.btnEntendido = BotonAmarillo(s1, "Boton_Entendido", 66, 690, 400, 82, "Entendido");

        // ---------------- S02 Cargando presentacion ----------------
        var s2 = Pantalla(raiz, "S02_CargandoPresentacion", 0.93f);
        Header(s2); Pie(s2);
        Dialogo(s2, "Dialogo", "BYTE / TU GUÍA", "Cargando presentación...", SpByteSonriente);

        // ---------------- S03 Byte aparece ----------------
        var s3 = Pantalla(raiz, "S03_ByteHola", 0.93f);
        Header(s3); Pie(s3);
        Spr(s3, "Byte_Cuerpo", SpByteIntro, 610, 175, 380, 430);
        Dialogo(s3, "Dialogo", "BYTE / TU GUÍA", "Hola! soy BYTE, tu asistente en esta experiencia...", SpByteSonriente);

        // ---------------- S04 Byte pide un especialista ----------------
        var s4 = Pantalla(raiz, "S04_ByteEspecialista", 0.93f);
        Header(s4); Pie(s4);
        Spr(s4, "Byte_Cuerpo", SpByteAlerta, 610, 175, 380, 430);
        Dialogo(s4, "Dialogo", "BYTE / TU GUÍA", "Descarguemos a un especialista en Programación!", SpByteSonriente);

        // ---------------- S05 Axel ----------------
        var s5 = Pantalla(raiz, "S05_Axel", 0.78f);
        Header(s5); Pie(s5);
        DialogoAxel(s5);

        // ---------------- S06 Menu (2 tarjetas) ----------------
        var s6 = Pantalla(raiz, "S06_Menu", 0.93f);
        Header(s6); Pie(s6);
        var byteMenu = Spr(s6, "Byte_CuerpoCompleto", SpByteIntro, 40, 240, 190, 225);
        var estadoMenu = CrearGrupoUI(s6, "Estado_Menu");
        var cardPathCompacta = CardPathfinding(estadoMenu.transform, "Card_Pathfinding", 284, 190, 567, 360, 1f);
        var cardMaquinaCompacta = CardMaquina(estadoMenu.transform, "Card_MaquinaDeEstados", 871, 190, 628, 360, 1f);
        Dialogo(estadoMenu.transform, "Dialogo", "BYTE / TU GUÍA", "Ahora que estamos juntos... ¿qué hacemos?", SpByteSonriente);

        var estadoPathfinding = CrearGrupoUI(s6, "Estado_PathfindingExpandido");
        var miniMaquina = CardMaquina(estadoPathfinding.transform, "CardMini_MaquinaDeEstados", 1047, 190, 628, 360, 0.70f);
        Atenuar(miniMaquina.gameObject, 0.5f);
        var cardPathExpandida = CardPathfindingGrande(estadoPathfinding.transform);
        Dialogo(estadoPathfinding.transform, "Dialogo", "BYTE / TU GUÍA", "Pathfinding! ¿Cómo nos movemos las IA?", SpByteSonriente);
        estadoPathfinding.SetActive(false);

        var estadoMaquina = CrearGrupoUI(s6, "Estado_MaquinaExpandido");
        var miniPathfinding = CardPathfinding(estadoMaquina.transform, "CardMini_Pathfinding", 284, 190, 567, 360, 0.70f);
        Atenuar(miniPathfinding.gameObject, 0.5f);
        var cardMaquinaExpandida = CardMaquinaGrande(estadoMaquina.transform);
        Dialogo(estadoMaquina.transform, "Dialogo", "BYTE / TU GUÍA",
            "No puedo hacer A si estoy haciendo B... ¡Contradictorio!", SpByteConfundido);
        estadoMaquina.SetActive(false);

        var hoverTemas = s6.gameObject.AddComponent<ByteTemaHover>();
        flow.hoverTemas = hoverTemas;
        hoverTemas.Configurar(estadoMenu, estadoPathfinding, estadoMaquina, byteMenu,
            cardPathExpandida.transform.parent as RectTransform, cardMaquinaExpandida.transform.parent as RectTransform,
            SpByteIntro, SpByteReflexivo);
        flow.btnElegirPathfinding = cardPathExpandida;
        flow.btnElegirMaquina = cardMaquinaExpandida;
        AgregarEventoHover(cardPathCompacta.gameObject, EventTriggerType.PointerEnter, hoverTemas.HoverPathfindingEvent);
        AgregarEventoHover(cardPathCompacta.gameObject, EventTriggerType.PointerExit, hoverTemas.SalirTarjetaEvent);
        AgregarEventoHover(cardMaquinaCompacta.gameObject, EventTriggerType.PointerEnter, hoverTemas.HoverMaquinaEvent);
        AgregarEventoHover(cardMaquinaCompacta.gameObject, EventTriggerType.PointerExit, hoverTemas.SalirTarjetaEvent);
        AgregarEventosHoverExpandido(cardPathExpandida.transform.parent.gameObject, hoverTemas.HoverPathfindingEvent, hoverTemas.SalirTarjetaEvent);
        AgregarEventosHoverExpandido(miniMaquina.gameObject, hoverTemas.HoverMaquinaEvent, hoverTemas.SalirTarjetaEvent);
        AgregarEventosHoverExpandido(cardMaquinaExpandida.transform.parent.gameObject, hoverTemas.HoverMaquinaEvent, hoverTemas.SalirTarjetaEvent);
        AgregarEventosHoverExpandido(miniPathfinding.gameObject, hoverTemas.HoverPathfindingEvent, hoverTemas.SalirTarjetaEvent);

        flow.s00Bienvenida = s0.gameObject; flow.s01AsignandoGuia = s1.gameObject; flow.s02Cargando = s2.gameObject;
        flow.s03ByteHola = s3.gameObject; flow.s04ByteEspecialista = s4.gameObject; flow.s05Axel = s5.gameObject;
        flow.s06Menu = s6.gameObject;
        // Solo S00 activa al arrancar (el resto lo maneja IntroFlow, pero se dejan visibles en el editor)
        s0.gameObject.SetActive(true);
        foreach (var s in new[] { s1, s2, s3, s4, s5, s6 }) s.gameObject.SetActive(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, RutaEscena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true), new EditorBuildSettingsScene("Assets/Scenes/Escena1.unity", true), new EditorBuildSettingsScene("Assets/Scenes/Escena2.unity", true), new EditorBuildSettingsScene("Assets/Scenes/Escena3.unity", true), new EditorBuildSettingsScene("Assets/Scenes/Escena4.unity", true) };
        Debug.Log("01_Intro construida. Tip: activa/desactiva las pantallas S00..S06 en Hierarchy para verlas.");
    }

    // ===================================================================
    //  Componentes de UI
    // ===================================================================
    static void Header(Transform p)
    {
        var g = new GameObject("Header", typeof(RectTransform)); g.transform.SetParent(p, false);
        var rt = g.GetComponent<RectTransform>(); Place(rt, 40, 31, 1520, 64);
        var b = Img(g.transform, "Borde", 0, 0, 1520, 64, new Color(105 / 255f, 116 / 255f, 86 / 255f, 1f));
        Img(g.transform, "Fondo", 1, 1, 1518, 62, new Color(19 / 255f, 27 / 255f, 22 / 255f, 0.95f));
        Texto(g.transform, "Texto", "SINTÉTIKA / IA EN PROGRAMACIÓN DE VIDEOJUEGOS", 25, 32, 1400, FBold, 18, PaletaFigma.Verde);
    }

    static void Pie(Transform p)
    {
        Texto(p, "Pie", "APUNTÁ PARA ELEGIR · DISPARÁ PARA AVANZAR", 40, 861, 900, FBold, 16, PaletaFigma.Gris);
    }

    static Button BotonAmarillo(Transform p, string nombre, float x, float y, float w, float h, string label)
    {
        var g = Panel(p, nombre, x, y, w, h, PaletaFigma.Panel, PaletaFigma.Amarillo);
        var fill = g.transform.Find("Fondo").GetComponent<Image>();
        var btn = g.AddComponent<Button>(); btn.targetGraphic = fill; Tinte(btn, PaletaFigma.Panel, new Color(0.20f, 0.19f, 0.10f, 0.97f));
        Img(g.transform, "Acento", 3, 30, 4, 40, PaletaFigma.Amarillo);
        Texto(g.transform, "Prefijo", "[ + ]", 31, 46, 120, FReg, 26, PaletaFigma.AmarilloTenue);
        Texto(g.transform, "Etiqueta", label, 121, 46, w - 130, FBold, 26, PaletaFigma.Amarillo);
        return btn;
    }

    static void Dialogo(Transform p, string nombre, string quien, string msg, Sprite retrato = null)
    {
        var g = Panel(p, nombre, 260, 641, 1070, 200, PaletaFigma.Panel, PaletaFigma.Verde);
        Img(g.transform, "Acento", 0, 19, 4, 31, PaletaFigma.Verde);
        Spr(g.transform, "Retrato_Byte", retrato != null ? retrato : SpByteSonriente, 24, 34, 117, 115);
        Texto(g.transform, "Nombre", quien, 160, 44, 850, FBold, 18, PaletaFigma.Verde);
        var m = Texto(g.transform, "Mensaje", msg, 160, 98, 850, FInter, 24, PaletaFigma.Crema);
        m.lineSpacing = 8.4f;
        m.enableWordWrapping = true;
        ((RectTransform)m.transform).sizeDelta = new Vector2(850, 112);
        m.richText = true;
    }

    static void DialogoAxel(Transform p)
    {
        var turquesa = new Color(0.28f, 0.79f, 0.76f, 1f);
        var g = Panel(p, "Dialogo_Axel", 260, 641, 1070, 200, PaletaFigma.Panel, turquesa);
        Img(g.transform, "Acento", 0, 19, 4, 31, turquesa);
        var rt = Rt(g.transform, "Retrato_Axel", 24, 34, 117, 115);
        var imagen = rt.gameObject.AddComponent<RawImage>();
        imagen.texture = TexAxel;
        imagen.raycastTarget = false;
        Texto(g.transform, "Nombre", "AXEL MUÑOZ / PROGRAMADOR DEL ESTUDIO PAMPEANO ‘ATANOR’",
            160, 44, 850, FBold, 18, turquesa);
        var msg = Texto(g.transform, "Mensaje", "Hola! Soy Axel... Programador hace más de 10 años\nen el estudio pampeano ‘Atanor’.",
            160, 98, 850, FInter, 24, PaletaFigma.Crema);
        msg.lineSpacing = 8.4f;
        msg.enableWordWrapping = true;
        ((RectTransform)msg.transform).sizeDelta = new Vector2(850, 112);
        msg.alignment = TextAlignmentOptions.MidlineLeft;
    }

    // Tarjeta Pathfinding (menu). escala != 1 -> version chica (tarjeta secundaria)
    static Button CardPathfinding(Transform p, string nombre, float x, float y, float w, float h, float esc)
    {
        var g = Panel(p, nombre, x, y, w, h, PaletaFigma.Panel, PaletaFigma.Verde);
        var btn = g.AddComponent<Button>(); btn.targetGraphic = g.transform.Find("Fondo").GetComponent<Image>();
        Tinte(btn, PaletaFigma.Panel, new Color(0.10f, 0.16f, 0.12f, 0.97f));
        Img(g.transform, "Acento", 0, 19, 4, 31, PaletaFigma.Verde);
        Texto(g.transform, "Etiqueta", "NÚCLEO / NAVEGACIÓN", 29, 42, 500, FBold, 18, PaletaFigma.Verde);
        var pts = new[] { P(43, 76), P(93, 76), P(93, 141), P(218, 141), P(218, 76), P(341, 76), P(341, 160), P(464, 160), P(464, 98) };
        Camino(g.transform, pts, 4, PaletaFigma.Verde, PaletaFigma.Verde, PaletaFigma.Rosa);
        Texto(g.transform, "Titulo", "Pathfinding", 29, 203, 500, FBold, 34, PaletaFigma.Crema);
        Texto(g.transform, "Descripcion_L1", "Explorá un laberinto y compará tu ruta con la", 29, 260, 540, FInter, 25, PaletaFigma.Gris);
        Texto(g.transform, "Descripcion_L2", "de un algoritmo.", 29, 296, 540, FInter, 25, PaletaFigma.Gris);
        EscalarTL(g.GetComponent<RectTransform>(), esc);
        return btn;
    }

    static Button CardMaquina(Transform p, string nombre, float x, float y, float w, float h, float esc)
    {
        var g = Panel(p, nombre, x, y, w, h, PaletaFigma.Panel, PaletaFigma.Verde);
        var btn = g.AddComponent<Button>(); btn.targetGraphic = g.transform.Find("Fondo").GetComponent<Image>();
        Tinte(btn, PaletaFigma.Panel, new Color(0.10f, 0.16f, 0.12f, 0.97f));
        Img(g.transform, "Acento", 0, 19, 4, 31, PaletaFigma.Verde);
        Texto(g.transform, "Etiqueta", "NÚCLEO / NAVEGACIÓN", 28, 42, 500, FBold, 18, PaletaFigma.Verde);
        DiagramaABC(g.transform, 29, 71, 1f);
        Texto(g.transform, "Titulo", "Máquina de Estados", 28, 203, 580, FBold, 34, PaletaFigma.Crema);
        Texto(g.transform, "Descripcion_L1", "Descubrí cómo las condiciones cambian lo que", 28, 260, 590, FInter, 25, PaletaFigma.Gris);
        Texto(g.transform, "Descripcion_L2", "podés hacer.", 28, 296, 590, FInter, 25, PaletaFigma.Gris);
        EscalarTL(g.GetComponent<RectTransform>(), esc);
        return btn;
    }

    static void AgregarEventosHoverExpandido(GameObject objetivo,
        UnityAction<BaseEventData> alEntrar, UnityAction<BaseEventData> alSalir)
    {
        AgregarEventoHover(objetivo, EventTriggerType.PointerEnter, alEntrar);
        AgregarEventoHover(objetivo, EventTriggerType.PointerExit, alSalir);
    }

    static void AgregarEventoHover(GameObject objetivo, EventTriggerType tipo, UnityAction<BaseEventData> accion)
    {
        var trigger = objetivo.GetComponent<EventTrigger>();
        if (trigger == null) trigger = objetivo.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
        var entrada = new EventTrigger.Entry { eventID = tipo };
        UnityEventTools.AddPersistentListener(entrada.callback, accion);
        trigger.triggers.Add(entrada);
        EditorUtility.SetDirty(trigger);
    }

    static GameObject CrearGrupoUI(Transform padre, string nombre)
    {
        var grupo = new GameObject(nombre, typeof(RectTransform));
        grupo.transform.SetParent(padre, false);
        var rt = grupo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return grupo;
    }

    // Tarjeta grande de Pathfinding con boton "Elegir" (devuelve ese boton)
    static Button CardPathfindingGrande(Transform p)
    {
        var g = Panel(p, "Card_Pathfinding_Grande", 284, 190, 690, 438, PaletaFigma.Panel, PaletaFigma.Verde);
        Img(g.transform, "Acento", 0, 19, 4, 31, PaletaFigma.Verde);
        Texto(g.transform, "Etiqueta", "NÚCLEO / NAVEGACIÓN", 34, 50, 600, FBold, 20, PaletaFigma.Verde);
        var pts = new[] { P(53, 92), P(115, 92), P(115, 173), P(265, 173), P(265, 92), P(415, 92), P(415, 197), P(565, 197), P(565, 120) };
        Camino(g.transform, pts, 4, PaletaFigma.Verde, PaletaFigma.Verde, PaletaFigma.Rosa);
        Texto(g.transform, "Titulo", "Pathfinding", 34, 251, 600, FBold, 38, PaletaFigma.Crema);
        return BotonAmarillo(g.transform, "Boton_Elegir", 33, 322, 350, 82, "Elegir");
    }

    static Button CardMaquinaGrande(Transform p)
    {
        var g = Panel(p, "Card_MaquinaDeEstados_Grande", 871, 192, 690, 438, PaletaFigma.Panel, PaletaFigma.Verde);
        Img(g.transform, "Acento", 0, 19, 4, 31, PaletaFigma.Verde);
        Texto(g.transform, "Etiqueta", "NÚCLEO / NAVEGACIÓN", 34, 50, 600, FBold, 20, PaletaFigma.Verde);
        DiagramaABC(g.transform, 31, 87, 1.1f);
        Texto(g.transform, "Titulo", "Máquina de Estados", 34, 251, 640, FBold, 38, PaletaFigma.Crema);
        return BotonAmarillo(g.transform, "Boton_Elegir", 33, 322, 350, 82, "Elegir");
    }

    // A -> B -> C con retorno inferior. (ox, oy) = esquina sup-izq dentro del padre (coordenadas de 1x)
    static void DiagramaABC(Transform p, float ox, float oy, float s)
    {
        var g = new GameObject("Diagrama_ABC", typeof(RectTransform)); g.transform.SetParent(p, false);
        Place(g.GetComponent<RectTransform>(), ox, oy, 580 * s, 90 * s);
        float[] bx = { 0, 224, 452 }; string[] ls = { "A", "B", "C" };
        for (int i = 0; i < 3; i++)
        {
            Contorno(g.transform, "Caja_" + ls[i], bx[i] * s, 0, 126 * s, 63 * s, 4, PaletaFigma.Verde);
            Texto(g.transform, "Letra_" + ls[i], ls[i], (bx[i] + 63) * s - 20, 32 * s, 40, FBold, Mathf.RoundToInt(30 * s), PaletaFigma.Crema, TextAnchor.MiddleCenter);
        }
        Img(g.transform, "Union_AB", 126 * s, 30 * s, 98 * s, 4, PaletaFigma.Verde);
        Img(g.transform, "Union_BC", 353 * s, 30 * s, 99 * s, 4, PaletaFigma.Verde);
        Img(g.transform, "Retorno_V1", 62 * s, 63 * s, 4, 25 * s, PaletaFigma.Verde);
        Img(g.transform, "Retorno_H", 62 * s, 87 * s, 455 * s, 4, PaletaFigma.Verde);
        Img(g.transform, "Retorno_V2", 513 * s, 63 * s, 4, 25 * s, PaletaFigma.Verde);
    }

    static void Camino(Transform p, Vector2[] pts, float t, Color linea, Color ini, Color fin)
    {
        var g = new GameObject("Camino", typeof(RectTransform)); g.transform.SetParent(p, false);
        Place(g.GetComponent<RectTransform>(), 0, 0, 1, 1);
        for (int i = 0; i + 1 < pts.Length; i++)
        {
            Vector2 a = pts[i], b = pts[i + 1];
            float x0 = Mathf.Min(a.x, b.x) - t / 2, y0 = Mathf.Min(a.y, b.y) - t / 2;
            Img(g.transform, "Tramo_" + (i + 1), x0, y0, Mathf.Abs(a.x - b.x) + t, Mathf.Abs(a.y - b.y) + t, linea);
        }
        Spr(g.transform, "Punto_Inicio", SpCirculo, pts[0].x - 8, pts[0].y - 8, 16, 16, ini);
        Spr(g.transform, "Punto_Fin", SpCirculo, pts[pts.Length - 1].x - 8, pts[pts.Length - 1].y - 8, 16, 16, fin);
    }

    // ===================================================================
    //  Primitivas
    // ===================================================================
    static Vector2 P(float x, float y) { return new Vector2(x, y); }

    static RectTransform Rt(Transform p, string n, float x, float y, float w, float h)
    {
        var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false);
        var rt = g.GetComponent<RectTransform>(); Place(rt, x, y, w, h); return rt;
    }

    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void EscalarTL(RectTransform rt, float s) { if (!Mathf.Approximately(s, 1f)) rt.localScale = new Vector3(s, s, 1); }

    static Image Img(Transform p, string n, float x, float y, float w, float h, Color c)
    {
        var rt = Rt(p, n, x, y, w, h); var im = rt.gameObject.AddComponent<Image>();
        im.color = c; im.raycastTarget = false; return im;
    }

    static Image Spr(Transform p, string n, Sprite sp, float x, float y, float w, float h, Color? c = null)
    {
        var im = Img(p, n, x, y, w, h, c ?? Color.white); im.sprite = sp; im.preserveAspect = false; return im;
    }

    static void Contorno(Transform p, string n, float x, float y, float w, float h, float t, Color c)
    {
        var g = Rt(p, n, x, y, w, h);
        Img(g, "Arriba", 0, 0, w, t, c); Img(g, "Abajo", 0, h - t, w, t, c);
        Img(g, "Izq", 0, 0, t, h, c); Img(g, "Der", w - t, 0, t, h, c);
    }

    // cy = centro vertical del texto (asi coincide con lo medido en Figma)
    static TextAlignmentOptions Alineacion(TextAnchor a)
    {
        switch (a)
        {
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Midline;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.TopLeft;
        }
    }

    static TMP_Text Texto(Transform p, string n, string s, float x, float cy, float w, TMP_FontAsset f, int size, Color c, TextAnchor a = TextAnchor.MiddleLeft)
    {
        float h = size * 2f;
        var rt = Rt(p, n, x, cy - h / 2f, w, h);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = f; t.text = s; t.fontSize = size; t.color = c; t.alignment = Alineacion(a);
        t.enableWordWrapping = false; t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false; t.richText = false;
        return t;
    }

    // Panel con esquinas cortadas (sup-izq e inf-der): objeto padre + Fondo + Borde
    static GameObject Panel(Transform p, string n, float x, float y, float w, float h, Color fill, Color borde)
    {
        var rt = Rt(p, n, x, y, w, h);
        var f = Img(rt, "Fondo", 0, 0, w, h, fill); f.sprite = SpPanelFill; f.type = Image.Type.Sliced; f.raycastTarget = true;
        var b = Img(rt, "Borde", 0, 0, w, h, borde); b.sprite = SpPanelBorder; b.type = Image.Type.Sliced;
        return rt.gameObject;
    }

    static void Tinte(Button b, Color normal, Color resaltado)
    {
        var fill = b.targetGraphic as Image; if (fill != null) fill.color = Color.white;
        b.transition = Selectable.Transition.ColorTint;
        var cb = b.colors; cb.normalColor = normal; cb.highlightedColor = resaltado;
        cb.pressedColor = new Color(resaltado.r * 0.8f, resaltado.g * 0.8f, resaltado.b * 0.8f, resaltado.a);
        cb.selectedColor = normal; cb.disabledColor = normal; cb.colorMultiplier = 1f; cb.fadeDuration = 0.08f;
        b.colors = cb;
    }

    static void Atenuar(GameObject g, float a) { var cg = g.AddComponent<CanvasGroup>(); cg.alpha = a; }

    static Transform Pantalla(Transform raiz, string nombre, float velo)
    {
        var g = Rt(raiz, nombre, 0, 0, 1600, 900); g.anchorMin = Vector2.zero; g.anchorMax = Vector2.one; g.offsetMin = g.offsetMax = Vector2.zero;
        var fondo = Img(g, "Fondo_Taller", 0, 0, 1600, 900, Color.white); fondo.sprite = SpTaller; Stretch(fondo.rectTransform);
        var v = Img(g, "Velo", 0, 0, 1600, 900, new Color(PaletaFigma.Velo.r, PaletaFigma.Velo.g, PaletaFigma.Velo.b, velo)); Stretch(v.rectTransform);
        return g;
    }

    // ===================================================================
    //  Importadores y recursos
    // ===================================================================
    static void CargarRecursos()
    {
        FBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/SpaceMono-Bold SDF.asset");
        FReg = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/SpaceMono-Regular SDF.asset");
        FInter = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/Inter-Regular SDF.asset");
        SpPanelFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Panel_Fill.png");
        SpPanelBorder = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Panel_Border.png");
        SpCirculo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Circulo.png");
        SpTaller = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Fondos/Taller.jpg");
        SpByteIntro = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Byte/Byte_Intro.png");
        SpByteAlerta = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Byte/Byte_Alerta.png");
        SpByteSonriente = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Byte/Byte_Sonriente.png");
        SpByteConfundido = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Byte/Byte_Confundido.png");
        SpByteEntusiasmado = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Byte/Byte_Entusiasmado.png");
        SpByteReflexivo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Byte/Byte_Reflexivo.png");
        TexAxel = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/FotoAxel.png");
    }

    [MenuItem("Tools/Pathfinding/Reimportar sprites")]
    public static void ConfigurarImportadores()
    {
        AssetDatabase.Refresh();
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites", "Assets/Resources/Fondos", "Assets/Resources/Byte" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.spritePixelsPerUnit = 100;
            bool pixel = path.Contains("/Byte/") || path.Contains("/Enemigos/") || path.Contains("/HUD/");
            ti.filterMode = pixel ? FilterMode.Point : FilterMode.Bilinear;
            ti.textureCompression = pixel ? TextureImporterCompression.Uncompressed : ti.textureCompression;
            ti.maxTextureSize = 4096;
            if (path.Contains("/UI/Panel_")) { ti.spriteBorder = new Vector4(16, 16, 16, 16); }
            var ts = new TextureImporterSettings(); ti.ReadTextureSettings(ts);
            ts.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(ts);
            ti.SaveAndReimport();
        }
    }
}
#endif

