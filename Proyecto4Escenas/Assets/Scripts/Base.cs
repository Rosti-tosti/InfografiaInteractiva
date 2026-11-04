using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Pathfinding
{
    // ---------------------------------------------------------------
    //  Paleta (tomada de los PNG de la infografía)
    // ---------------------------------------------------------------
    public static class Paleta
    {
        public static readonly Color Fondo = new Color(0.055f, 0.078f, 0.067f);
        public static readonly Color Panel = new Color(0.075f, 0.118f, 0.098f, 0.96f);
        public static readonly Color Verde = new Color(0.60f, 0.74f, 0.53f);
        public static readonly Color VerdeBorde = new Color(0.43f, 0.57f, 0.40f);
        public static readonly Color Amarillo = new Color(0.91f, 0.76f, 0.42f);
        public static readonly Color Crema = new Color(0.96f, 0.93f, 0.85f);
        public static readonly Color Gris = new Color(0.72f, 0.77f, 0.68f);
        public static readonly Color Naranja = new Color(0.88f, 0.50f, 0.35f);
        public static readonly Color Muro = new Color(0.39f, 0.43f, 0.37f);
        public static readonly Color Piso = new Color(0.10f, 0.14f, 0.12f);
        public static readonly Color Oscuro = new Color(0.08f, 0.10f, 0.07f);
    }

    // ---------------------------------------------------------------
    //  Laberinto 7x7 (incluye el marco de muros). '#' muro, '.' libre
    //  Coordenadas: x = columna, y = fila (0 arriba).
    // ---------------------------------------------------------------
    public static class Laberinto
    {
        public const int N = 7;
        public static readonly string[] Mapa =
        {
            "#######",
            "#A..#.#",
            "###.#.#",
            "#.....#",
            "#####.#",
            "#####B#",
            "#######",
        };
        public static readonly Vector2Int A = new Vector2Int(1, 1);
        public static readonly Vector2Int B = new Vector2Int(5, 5);

        public static bool Muro(int x, int y)
        {
            if (x < 0 || y < 0 || x >= N || y >= N) return true;
            return Mapa[y][x] == '#';
        }
        public static bool Muro(Vector2Int c) { return Muro(c.x, c.y); }
    }

    // ---------------------------------------------------------------
    //  A* real sobre el laberinto (4 vecinos, heurística Manhattan)
    // ---------------------------------------------------------------
    public static class AEstrella
    {
        public static List<Vector2Int> Buscar(Vector2Int ini, Vector2Int meta)
        {
            var g = new Dictionary<Vector2Int, int>();
            var previo = new Dictionary<Vector2Int, Vector2Int>();
            var abiertos = new List<Vector2Int> { ini };
            var cerrados = new HashSet<Vector2Int>();
            g[ini] = 0;
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.down, Vector2Int.left, Vector2Int.up };

            while (abiertos.Count > 0)
            {
                int mejor = 0;
                for (int i = 1; i < abiertos.Count; i++)
                    if (F(abiertos[i], g, meta) < F(abiertos[mejor], g, meta)) mejor = i;
                var actual = abiertos[mejor];
                abiertos.RemoveAt(mejor);

                if (actual == meta)
                {
                    var ruta = new List<Vector2Int> { actual };
                    while (previo.ContainsKey(actual)) { actual = previo[actual]; ruta.Insert(0, actual); }
                    return ruta;
                }
                cerrados.Add(actual);
                foreach (var d in dirs)
                {
                    var v = actual + d;
                    if (Laberinto.Muro(v) || cerrados.Contains(v)) continue;
                    int ng = g[actual] + 1;
                    if (!g.ContainsKey(v) || ng < g[v])
                    {
                        g[v] = ng; previo[v] = actual;
                        if (!abiertos.Contains(v)) abiertos.Add(v);
                    }
                }
            }
            return new List<Vector2Int>();
        }

        static int F(Vector2Int n, Dictionary<Vector2Int, int> g, Vector2Int meta)
        {
            return g[n] + Mathf.Abs(n.x - meta.x) + Mathf.Abs(n.y - meta.y);
        }
    }

    // Datos que pasan de la escena 2 (laberinto) a la 3 (comparación)
    public static class DatosPartida
    {
        public static List<Vector2Int> Recorrido = new List<Vector2Int>();
        public static int Pasos { get { return Mathf.Max(0, Recorrido.Count - 1); } }
    }

    // ---------------------------------------------------------------
    //  Botones que se activan "disparando" con la mira (o con el mouse)
    // ---------------------------------------------------------------
    public class ShootButton : MonoBehaviour
    {
        public static readonly List<ShootButton> Todos = new List<ShootButton>();
        public Action onClick;
        public KeyCode tecla = KeyCode.None;
        public Image relleno;
        public Color normal, resaltado;
        public bool bajoMira;

        void OnEnable() { Todos.Add(this); }
        void OnDisable() { Todos.Remove(this); bajoMira = false; }

        public void SetResaltado(bool v)
        {
            bajoMira = v;
            if (relleno != null) relleno.color = v ? resaltado : normal;
        }

        // Llamar una vez por frame. Devuelve true si se activó un botón.
        public static bool Procesar()
        {
            Vector2 p = Cursor.lockState == CursorLockMode.Locked
                ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                : (Vector2)Input.mousePosition;

            var copia = Todos.ToArray();
            ShootButton top = null;
            foreach (var b in copia)
            {
                if (b == null || !b.gameObject.activeInHierarchy) continue;
                bool dentro = RectTransformUtility.RectangleContainsScreenPoint((RectTransform)b.transform, p, null);
                b.SetResaltado(dentro);
                if (dentro) top = b;
            }

            ArcadeGunInput control = ArcadeGunInput.Instancia;
            bool clickFisico = control != null && top != null && control.TomarClickPistola();
            if ((Input.GetMouseButtonDown(0) || clickFisico) && top != null && top.onClick != null)
            {
                top.onClick(); return true;
            }
            foreach (var b in copia)
            {
                if (b == null || !b.gameObject.activeInHierarchy) continue;
                if (b.tecla != KeyCode.None && Input.GetKeyDown(b.tecla) && b.onClick != null)
                {
                    b.onClick(); return true;
                }
            }
            return false;
        }
    }

    // ---------------------------------------------------------------
    //  Utilidades de UI (coordenadas en el espacio 1456 x 819 de los PNG)
    // ---------------------------------------------------------------
    public struct Caja { public RectTransform rt; public Image fill; public Image borde; }

    public static class UIKit
    {
        public const float W = 1456f, H = 819f;
        static TMP_FontAsset fuente;
        static TMP_FontAsset fuenteInter;
        static TMP_FontAsset fuenteMonoBold;

        public static TMP_FontAsset Fuente
        {
            get
            {
                if (fuente == null)
                    fuente = Resources.Load<TMP_FontAsset>("Fonts/SpaceMono-Regular SDF");
                return fuente;
            }
        }

        public static TMP_FontAsset FuenteInter
        {
            get
            {
                if (fuenteInter == null) fuenteInter = Resources.Load<TMP_FontAsset>("Fonts/Inter-Regular SDF");
                return fuenteInter != null ? fuenteInter : Fuente;
            }
        }

        public static TMP_FontAsset FuenteMonoBold
        {
            get
            {
                if (fuenteMonoBold == null) fuenteMonoBold = Resources.Load<TMP_FontAsset>("Fonts/SpaceMono-Bold SDF");
                return fuenteMonoBold != null ? fuenteMonoBold : Fuente;
            }
        }

        public static Canvas CrearCanvas(string nombre, int orden = 0)
        {
            var go = new GameObject(nombre);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = orden;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(W, H);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return c;
        }

        // Raíz de 1456x819 centrada en el canvas
        public static RectTransform CrearRaiz(Transform canvas, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(W, H);
            return rt;
        }

        public static RectTransform Rect(Transform p, string n, float x, float y, float w, float h)
        {
            var go = new GameObject(n, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(p, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image Rellenar(Transform p, float x, float y, float w, float h, Color c)
        {
            var rt = Rect(p, "Rect", x, y, w, h);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = c; im.raycastTarget = false;
            return im;
        }

        public static Caja NuevaCaja(Transform p, float x, float y, float w, float h, Color fill, Color? borde = null, float bw = 2f)
        {
            var rt = Rect(p, "Caja", x, y, w, h);
            var c = new Caja { rt = rt };
            if (borde.HasValue)
            {
                c.borde = rt.gameObject.AddComponent<Image>();
                c.borde.color = borde.Value; c.borde.raycastTarget = false;
                var inner = Rect(rt, "Relleno", bw, bw, w - 2 * bw, h - 2 * bw);
                c.fill = inner.gameObject.AddComponent<Image>();
            }
            else c.fill = rt.gameObject.AddComponent<Image>();
            c.fill.color = fill; c.fill.raycastTarget = false;
            return c;
        }

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

        public static TMP_Text Texto(Transform p, string s, float x, float y, float w, float h, int size, Color col,
            TextAnchor a = TextAnchor.UpperLeft, FontStyles st = FontStyles.Normal)
        {
            var rt = Rect(p, "Texto", x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Fuente; t.text = s; t.fontSize = size; t.color = col; t.alignment = Alineacion(a); t.fontStyle = st;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // Medidas del componente de diálogo de Figma (1070 x 184 en 1600 x 900).
        // El Canvas de juego usa 1456 x 819, por eso la tipografía equivale a 18/24 px al escalar.
        public const float DialogoAncho = 974f;
        public const float DialogoAlto = 150f;


        public static TMP_Text RotuloDialogo(Transform p, string s, float x, float y, float w, float h, Color col)
        {
            var t = Texto(p, s, x, y, w, h, 16, col, TextAnchor.UpperLeft, FontStyles.Bold);
            t.font = FuenteMonoBold;
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 0f;
            return t;
        }

        public static TMP_Text CuerpoDialogo(Transform p, string s, float x, float y, float w, float h, Color col)
        {
            var t = Texto(p, s, x, y, w, h, 22, col, TextAnchor.UpperLeft, FontStyles.Normal);
            t.font = FuenteInter;
            t.fontStyle = FontStyles.Normal;
            t.lineSpacing = 8f;
            t.characterSpacing = 0f;
            return t;
        }

        public static ShootButton Boton(Transform p, string prefijo, string etiqueta, float x, float y, float w, float h,
            Action onClick, KeyCode tecla = KeyCode.None, bool relleno = false)
        {
            Color normal = relleno ? Paleta.Amarillo : new Color(0.09f, 0.11f, 0.08f, 0.95f);
            var caja = NuevaCaja(p, x, y, w, h, normal, Paleta.Amarillo, 2f);
            var sb = caja.rt.gameObject.AddComponent<ShootButton>();
            sb.onClick = onClick; sb.tecla = tecla; sb.relleno = caja.fill;
            sb.normal = normal;
            sb.resaltado = relleno ? new Color(1f, 0.87f, 0.55f) : new Color(0.27f, 0.23f, 0.10f, 0.98f);

            if (relleno)
                Texto(caja.rt, etiqueta, 0, 0, w, h, 17, Paleta.Oscuro, TextAnchor.MiddleCenter, FontStyles.Bold);
            else
            {
                if (!string.IsNullOrEmpty(prefijo))
                    Texto(caja.rt, prefijo, 20, 0, 70, h, 16, new Color(0.7f, 0.58f, 0.32f), TextAnchor.MiddleLeft, FontStyles.Bold);
                Texto(caja.rt, etiqueta, string.IsNullOrEmpty(prefijo) ? 20 : 78, 0, w - 90, h, 22, Paleta.Amarillo, TextAnchor.MiddleLeft, FontStyles.Bold);
            }
            return sb;
        }

        public static ShootButton BotonContinuar(Transform dialogo, Action continuar,
            float ancho = DialogoAncho, float alto = DialogoAlto)
        {
            float w = Mathf.Min(136f, ancho * 0.3f);
            float h = Mathf.Min(56f, alto - 24f);
            return Boton(dialogo, "", "SIGUIENTE", ancho - w - 16f, (alto - h) * 0.5f,
                w, h, continuar, KeyCode.None, true);
        }

        public static void Encabezado(Transform p, string izq, string der)
        {
            var c = NuevaCaja(p, 37, 28, 1383, 58, new Color(0.07f, 0.11f, 0.09f, 0.93f), Paleta.VerdeBorde, 2f);
            Texto(c.rt, izq, 22, 0, 700, 58, 17, Paleta.Verde, TextAnchor.MiddleLeft, FontStyles.Bold);
            if (!string.IsNullOrEmpty(der))
                Texto(c.rt, der, 1383 - 722, 0, 700, 58, 17, Paleta.Crema, TextAnchor.MiddleRight, FontStyles.Bold);
        }

        public static void Pie(Transform p, string izq, string der)
        {
            Texto(p, izq, 37, 776, 800, 20, 13, Paleta.Gris, TextAnchor.MiddleLeft, FontStyles.Bold);
            if (!string.IsNullOrEmpty(der))
                Texto(p, der, 619, 776, 800, 20, 13, Paleta.Gris, TextAnchor.MiddleRight, FontStyles.Bold);
        }

        static Sprite byteCaraSonriente;

        static Sprite CaraByte
        {
            get
            {
                if (byteCaraSonriente == null) byteCaraSonriente = Resources.Load<Sprite>("Byte/Byte_Sonriente");
                return byteCaraSonriente;
            }
        }

        // Retrato de Byte para acompañar los diálogos.
        public static void Byte(Transform p, float x, float y, float s = 1f)
        {
            var sprite = CaraByte;
            if (sprite == null) return;
            var go = new GameObject("Byte_Retrato", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(p, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(106 * s, 104 * s);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        public static void Dialogo(Transform p, float x, float y, float w, float h, string nombre, string texto,
            Color? colorNombre = null, Action continuar = null)
        {
            var c = NuevaCaja(p, x, y, DialogoAncho, DialogoAlto, Paleta.Panel, Paleta.Verde, 2f);
            Byte(c.rt, 18, 18, 0.82f);
            float anchoTexto = continuar == null ? DialogoAncho - 148 : DialogoAncho - 294;
            RotuloDialogo(c.rt, nombre, 124, 30, anchoTexto, 27, colorNombre ?? Paleta.Verde);
            var cuerpo = CuerpoDialogo(c.rt, texto, 124, 65, anchoTexto, 82, Paleta.Crema);
            if (continuar != null)
            {
                cuerpo.fontSize = 18f;
                BotonContinuar(c.rt, continuar);
            }
        }

        public static void DialogoTema(Transform p, float x, float y, string nombre, string texto, Action continuar = null)
        {
            var c = NuevaCaja(p, x, y, DialogoAncho, DialogoAlto, Paleta.Panel, Paleta.Verde, 2f);
            Byte(c.rt, 18, 18, 0.82f);
            float anchoTexto = continuar == null ? DialogoAncho - 148 : DialogoAncho - 294;
            RotuloDialogo(c.rt, nombre, 124, 30, anchoTexto, 27, Paleta.Verde);
            var cuerpo = CuerpoDialogo(c.rt, texto, 124, 65, anchoTexto, 82, Paleta.Crema);
            if (continuar != null)
            {
                cuerpo.fontSize = 18f;
                BotonContinuar(c.rt, continuar);
            }
        }
        public static void Mira(Transform p, Color c)
        {
            float cx = W * 0.5f, cy = H * 0.5f, r = 16f, l = 8f, t = 2f;
            foreach (var sx in new[] { -1, 1 })
                foreach (var sy in new[] { -1, 1 })
                {
                    float hx = sx < 0 ? cx - r : cx + r - l;
                    float hy = sy < 0 ? cy - r : cy + r - t;
                    float vx = sx < 0 ? cx - r : cx + r - t;
                    float vy = sy < 0 ? cy - r : cy + r - l;
                    Rellenar(p, hx, hy, l, t, c);
                    Rellenar(p, vx, vy, t, l, c);
                }
            Rellenar(p, cx - 1, cy - 4, 2, 8, c);
            Rellenar(p, cx - 4, cy - 1, 8, 2, c);
        }
    }

    // ---------------------------------------------------------------
    //  Dibujo del mapa 7x7 en UI (minimapa / comparación / resumen)
    // ---------------------------------------------------------------
    public static class MapaUI
    {
        public static void Dibujar(Transform p, float x, float y, float lado, float gap,
            HashSet<Vector2Int> amarillo = null, HashSet<Vector2Int> verde = null, bool letras = true)
        {
            int n = Laberinto.N;
            float paso = lado / n, tam = paso - gap;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    var c = new Vector2Int(i, j);
                    Color col = Laberinto.Muro(c) ? Paleta.Muro : Paleta.Piso;
                    if (verde != null && verde.Contains(c)) col = Paleta.Verde;
                    if (amarillo != null && amarillo.Contains(c)) col = Paleta.Amarillo;
                    if (letras && c == Laberinto.A) col = Paleta.Amarillo;
                    if (letras && c == Laberinto.B) col = Paleta.Verde;
                    var im = UIKit.Rellenar(p, x + i * paso, y + j * paso, tam, tam, col);
                    if (letras && (c == Laberinto.A || c == Laberinto.B))
                    {
                        string l = c == Laberinto.A ? "A" : "B";
                        UIKit.Texto(im.transform, l, 0, 0, tam, tam, Mathf.RoundToInt(tam * 0.55f), Paleta.Oscuro, TextAnchor.MiddleCenter, FontStyles.Bold);
                    }
                }
        }

        // Línea de ruta (segmentos horizontales/verticales entre centros de celda)
        public static void Ruta(Transform p, float x, float y, float lado, List<Vector2Int> ruta, Color col, float grosor)
        {
            float paso = lado / Laberinto.N;
            for (int i = 0; i + 1 < ruta.Count; i++)
            {
                var a = ruta[i]; var b = ruta[i + 1];
                float ax = x + (a.x + 0.5f) * paso, ay = y + (a.y + 0.5f) * paso;
                float bx = x + (b.x + 0.5f) * paso, by = y + (b.y + 0.5f) * paso;
                float x0 = Mathf.Min(ax, bx) - grosor * 0.5f, y0 = Mathf.Min(ay, by) - grosor * 0.5f;
                float w = Mathf.Abs(ax - bx) + grosor, h = Mathf.Abs(ay - by) + grosor;
                UIKit.Rellenar(p, x0, y0, w, h, col);
            }
        }
    }

    // ---------------------------------------------------------------
    //  Base de las escenas 2D (1, 3 y 4)
    // ---------------------------------------------------------------
    public abstract class EscenaBase : MonoBehaviour
    {
        protected Canvas canvas;
        protected GameObject pantalla;
        Image velo;

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Image Capa(Transform padre, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            Estirar(go.GetComponent<RectTransform>());
            var im = go.AddComponent<Image>(); im.raycastTarget = false;
            return im;
        }

        // 0 = foto a pleno, 1 = negro
        protected void Oscurecer(float a) { if (velo != null) velo.color = new Color(0.04f, 0.07f, 0.06f, a); }

        protected virtual void Awake()
        {
            var entrada = GetComponent<ArcadeGunInput>();
            if (entrada == null) entrada = gameObject.AddComponent<ArcadeGunInput>();
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Paleta.Fondo;
            }
            canvas = UIKit.CrearCanvas("Canvas");
            var raizF = new GameObject("FondoRaiz", typeof(RectTransform));
            raizF.transform.SetParent(canvas.transform, false);
            Estirar(raizF.GetComponent<RectTransform>());
            var im = Capa(raizF.transform, "Fondo");
            im.color = Paleta.Fondo;
            var tex = Resources.Load<Texture2D>("Fondos/Taller");
            if (tex != null)
            {
                var go = new GameObject("Foto", typeof(RectTransform));
                go.transform.SetParent(raizF.transform, false);
                Estirar(go.GetComponent<RectTransform>());
                var raw = go.AddComponent<RawImage>(); raw.texture = tex; raw.raycastTarget = false;
            }
            velo = Capa(raizF.transform, "Velo");
            velo.color = new Color(0.04f, 0.07f, 0.06f, 0f);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        protected Transform NuevaPantalla()
        {
            if (pantalla != null) { pantalla.SetActive(false); Destroy(pantalla); }
            pantalla = UIKit.CrearRaiz(canvas.transform, "Pantalla").gameObject;
            return pantalla.transform;
        }

        protected void IrAEscena(string nombre) { SceneManager.LoadScene(nombre); }
    }
}

