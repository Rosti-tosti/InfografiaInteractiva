using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Pathfinding
{
    /// <summary>Recorrido de IA Director, recreado según las seis pantallas de referencia.</summary>
    public class IADirectorFlow : MonoBehaviour
    {
        const float S = 2.147f; // Las referencias miden 678 x 380; el canvas del proyecto, 1456 x 819.
        const float EscalaFinalEnemigo = 2.2f;
        const int PenalizacionGolpeEnemigo = 20000;
        ArcadeGunInput control;
        Canvas canvas;
        Transform raiz;
        Image velo;
        GameObject[] pantallas;
        bool botonRecargaAnterior;
        bool transicionando;
        int actual;
        int ultimoFrameAtajoZ = -1;
        float inicioPantallaActual;
        RectTransform byteDescarga;
        RectTransform posterDescarga;
        RectTransform posterCentral;
        GameObject minijuegoTutorial, minijuegoPartida, juegoRecarga, juegoPanelMunicion;
        GameObject pantallaResultado;
        RectTransform botonGuardarResultado;
        TMPro.TMP_Text textoPuntajeResultado;
        TMPro.TMP_Text textoResultadoByte;
        readonly TMPro.TMP_Text[] textosUmbrales = new TMPro.TMP_Text[4];
        RectTransform juegoIconoEnemigo;
        Image juegoBarraRecarga, juegoOscuridad, juegoIconoAvanzar;
        RawImage juegoFondo, juegoCobertura, juegoIconoRecargar, juegoIconoRecargarDialogo;
        TMPro.TMP_Text juegoTextoDialogo, juegoTextoEnviando, juegoTextoUsando, juegoTextoEspacio,
            juegoTextoRecargaPrompt, juegoTextoRecargaFinal,
            juegoTextoRonda, juegoTextoBalas, juegoTextoRecarga, juegoTextoRecargaSufijo;
        readonly Image[] juegoEstadosRelleno = new Image[3];
        readonly Image[] juegoEstadosBorde = new Image[3];
        readonly TMPro.TMP_Text[] juegoEstadosTexto = new TMPro.TMP_Text[3];
        readonly System.Collections.Generic.List<RectTransform> juegoEnemigos = new System.Collections.Generic.List<RectTransform>();
        readonly System.Collections.Generic.List<Vector2> juegoPosicionesEnemigos = new System.Collections.Generic.List<Vector2>();
        readonly System.Collections.Generic.List<Vector2> juegoOrigenEnemigos = new System.Collections.Generic.List<Vector2>();
        readonly System.Collections.Generic.List<Vector2> juegoDestinoEnemigos = new System.Collections.Generic.List<Vector2>();
        readonly System.Collections.Generic.List<float> juegoProgresoEnemigos = new System.Collections.Generic.List<float>();
        readonly System.Collections.Generic.List<float> juegoDuracionAvanceEnemigos = new System.Collections.Generic.List<float>();
        int juegoFase, juegoRonda, juegoBalas, juegoPuntaje;
        float juegoProgresoRecarga;
        bool juegoRecargaCompleta;
        bool juegoRecargaIniciada;
        bool juegoSinBalasAvisado;

        public void Iniciar(ArcadeGunInput entrada)
        {
            control = entrada;
            Construir();
            Mostrar(0);
        }

        void Update()
        {
            if (pantallas == null) return;
            if (Input.GetKeyDown(KeyCode.Z) && Time.frameCount != ultimoFrameAtajoZ)
            {
                IrAlMinijuegoDirecto();
                return;
            }
            if (actual == 5)
            {
                bool disparoFinal = control != null && control.TomarClickPistola();
                if (!transicionando && (disparoFinal || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) ||
                    Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                {
                    juegoFase = 0;
                    Mostrar(6);
                }
                return;
            }
            if (actual == 6)
            {
                ActualizarMinijuego();
                return;
            }
            if (actual == 7)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    BorrarPuntuacionesGuardadas();
                    return;
                }
                bool disparoResultado = control != null && control.TomarClickPistola();
                bool clickGuardar = Input.GetMouseButtonDown(0) && botonGuardarResultado != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(botonGuardarResultado, Input.mousePosition, canvas.worldCamera);
                if (clickGuardar || disparoResultado || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    GuardarPuntuacionYReiniciar();
                return;
            }
            if (actual == 0 && Time.unscaledTime - inicioPantallaActual >= 3.8f)
            {
                Mostrar(1);
                return;
            }
            bool disparo = control != null && control.TomarClickPistola();
            if (transicionando) return;
            if (disparo || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (actual == 2) StartCoroutine(TransicionDescarga());
                else if (actual == 3) StartCoroutine(AnimacionDescargaPoster());
                else Mostrar(actual + 1);
            }
        }

        void Construir()
        {
            canvas = UIKit.CrearCanvas("Canvas_IADirector", 20);
            raiz = UIKit.CrearRaiz(canvas.transform, "Flujo_IADirector");
            var fondo = new GameObject("Fondo_Taller", typeof(RectTransform), typeof(RawImage));
            fondo.transform.SetParent(raiz, false);
            Estirar(fondo.GetComponent<RectTransform>());
            var imagenFondo = fondo.GetComponent<RawImage>();
            imagenFondo.texture = Resources.Load<Texture2D>("Fondos/Taller");
            imagenFondo.raycastTarget = false;
            velo = UIKit.Rellenar(raiz, 0, 0, UIKit.W, UIKit.H, new Color(0.025f, 0.045f, 0.04f, 0.54f));
            Estirar(velo.rectTransform);

            pantallas = new[]
            {
                NuevaPantalla("S00_Titulo_PathIntoTheAbyss"),
                NuevaPantalla("S01_Byte_Presentacion"),
                NuevaPantalla("S02_Descarga_PosterEnemigo"),
                NuevaPantalla("S03_Descarga_PosterCentral"),
                NuevaPantalla("S04_Explicacion_UnaIA"),
                NuevaPantalla("S05_Explicacion_IA_Director"),
                NuevaPantalla("S06_Minijuego_IA_Director")
            };
            PantallaTitulo(pantallas[0].transform);
            PantallaByte(pantallas[1].transform);
            PantallaPosterLateral(pantallas[2].transform);
            PantallaPosterCentral(pantallas[3].transform);
            PantallaExplicacionUnaIA(pantallas[4].transform);
            PantallaExplicacionDirector(pantallas[5].transform);
            PantallaMinijuego(pantallas[6].transform);
        }

        void PantallaTitulo(Transform p)
        {
            EncabezadoSintetika(p);
            var titulo = Texto(p, "PATH INTO THE\nABYSS", 35, 40, 510, 96, 40, Paleta.Crema,
                TextAnchor.UpperLeft, TMPro.FontStyles.Bold);
            titulo.lineSpacing = 8;
            Linea(p, 35, 183, 58, 2, Paleta.Naranja);
            Texto(p, "Caso Argentino de IA Director + IA enemiga //...", 35, 201, 545, 30, 15,
                Paleta.Gris, TextAnchor.MiddleLeft);
            Pie(p);
        }

        void PantallaByte(Transform p)
        {
            EncabezadoSintetika(p);
            Imagen(p, "Byte_Grande", "Byte/Byte_Intro", 253, 108, 110, 133);
            Dialogo(p, "ALOO!... coMO QuerIas aprender SobRE IA... Te trAJE una SorPREsa!");
            Pie(p);
        }

        void PantallaPosterLateral(Transform p)
        {
            EncabezadoSintetika(p);
            Panel(p, 175, 91, 295, 168, Paleta.VerdeBorde);
            byteDescarga = Imagen(p, "Byte_Alerta_Grande", "Byte/Byte_Alerta", 34, 106, 106, 133);
            posterDescarga = ImagenPoster(p, 534, 113, 84, 101);
            Dialogo(p, "UN JUEGO ARGENTINO!! ... //Descargando...");
            Pie(p);
        }

        void PantallaPosterCentral(Transform p)
        {
            EncabezadoSintetika(p);
            Panel(p, 175, 88, 295, 168, Paleta.VerdeBorde);
            posterCentral = ImagenPoster(p, 280, 99, 83, 100);
            // Flecha de descarga tal como aparece debajo del póster en la referencia.
            Texto(p, "↓", 307, 202, 31, 32, 25, Paleta.Naranja, TextAnchor.MiddleCenter);
            Linea(p, 307, 235, 29, 2, Paleta.Naranja);
            Linea(p, 307, 226, 2, 11, Paleta.Naranja);
            Linea(p, 334, 226, 2, 11, Paleta.Naranja);
            Dialogo(p, "UN JUEGO ARGENTINO!! ... //Descargando...");
            Pie(p);
        }

        void PantallaExplicacionUnaIA(Transform p)
        {
            EncabezadoPath(p);
            Panel(p, 153, 90, 478, 221, Paleta.VerdeBorde);
            ByteLateral(p);
            TituloCaso(p);
            Texto(p, "Path into the abyss no controla a sus enemigos con una sola IA", 165, 155, 430, 30,
                14, Paleta.Crema, TextAnchor.MiddleLeft);
            DiagramaUnaIA(p);
            Pie(p, true);
        }

        void PantallaExplicacionDirector(Transform p)
        {
            EncabezadoPath(p);
            Panel(p, 153, 90, 478, 221, Paleta.VerdeBorde);
            ByteLateral(p);
            TituloCaso(p);
            Texto(p, "El juego utiliza una IA “Director” para ayudar a las demás IA enemigos", 165, 158, 430, 30,
                14, Paleta.Crema, TextAnchor.MiddleLeft);
            DiagramaDirector(p);
            ImagenPoster(p, 167, 249, 40, 50);
            Pie(p, true);
        }

        void DiagramaUnaIA(Transform p)
        {
            Icono(p, "IA_Directora", "IADirector/IADirectora", 220, 205, 37, 37);
            Linea(p, 264, 218, 119, 2, Paleta.VerdeBorde);
            Linea(p, 291, 218, 2, 43, Paleta.VerdeBorde);
            Linea(p, 342, 218, 2, 30, Paleta.VerdeBorde);
            Linea(p, 382, 218, 2, 10, Paleta.VerdeBorde);
            Enemigo(p, 278, 260, 27, 30);
            Enemigo(p, 330, 247, 30, 34);
            Enemigo(p, 389, 212, 31, 34);
            Icono(p, "Negacion_Error", "IADirector/NegacionError", 450, 201, 37, 37);
            Icono(p, "Jugador", "IADirector/Jugador", 502, 266, 25, 29);
        }

        void DiagramaDirector(Transform p)
        {
            Icono(p, "IA_Directora", "IADirector/IADirectora", 219, 205, 37, 37);
            Icono(p, "Senal_Directora", "IADirector/RecibirSenal", 257, 203, 32, 37);
            Icono(p, "Senal_IA_1", "IADirector/RecibirSenal", 294, 257, 18, 22, 180);
            Icono(p, "Senal_IA_2", "IADirector/RecibirSenal", 346, 232, 18, 22, 180);
            Icono(p, "Senal_IA_3", "IADirector/RecibirSenal", 399, 205, 18, 22, 180);
            Enemigo(p, 314, 256, 25, 29);
            Enemigo(p, 368, 231, 29, 32);
            Enemigo(p, 421, 205, 31, 34);
            Icono(p, "Estado_Correcto", "IADirector/AceptarEstadoCorrecto", 545, 200, 37, 37);
            Icono(p, "Jugador", "IADirector/Jugador", 502, 266, 25, 29);
        }

        void Icono(Transform p, string nombre, string recurso, float x, float y, float w, float h, float rotacion = 0)
        {
            Imagen(p, nombre, recurso, x, y, w, h, null, true, rotacion);
        }

        void Enemigo(Transform p, float x, float y, float w, float h)
        {
            Icono(p, "IA_Enemiga", "IADirector/IAEnemiga", x, y, w, h);
        }

        void TituloCaso(Transform p)
        {
            Texto(p, "CASO PATH INTO THE ABYSS", 165, 105, 320, 16, 8, Paleta.Verde,
                TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            Texto(p, "IA DE IAS", 165, 126, 260, 28, 17, Paleta.Crema,
                TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
        }

        void ByteLateral(Transform p)
        {
            Imagen(p, "Byte_IADeIAs", "Byte/Byte_Intro", 46, 122, 88, 106);
        }

        void EncabezadoSintetika(Transform p)
        {
            UIKit.Encabezado(p, "SINTÉTIKA / IA EN PROGRAMACIÓN DE VIDEOJUEGOS", null);
        }

        void EncabezadoPath(Transform p)
        {
            UIKit.Encabezado(p, "PATH INTO THE ABYSS/", null);
        }

        void Dialogo(Transform p, string mensaje)
        {
            var caja = CrearPanel(p, 110 * S, 282 * S, 423 * S, 57 * S, Paleta.Panel, Paleta.Verde);
            Imagen(caja, "Byte_Retrato", "Byte/Byte_Sonriente", 5, 7, 40, 40);
            UIKit.RotuloDialogo(caja, "BYTE / TU GUÍA", 52 * S, 8 * S, 350 * S, 12 * S, Paleta.Verde);
            var texto = UIKit.CuerpoDialogo(caja, mensaje, 52 * S, 27 * S, 355 * S, 24 * S, Paleta.Crema);
            texto.lineSpacing = 0;
        }

        void Pie(Transform p, bool final = false)
        {
            Texto(p, "APUNTÁ PARA ELEGIR · DISPARÁ PARA AVANZAR", 16, 360, 300, 12, 7,
                Paleta.Gris, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            if (final)
                Texto(p, "EXPERIENCIA EDUCATIVA / VISTA ESTÁTICA", 502, 360, 160, 12, 6,
                    Paleta.Gris, TextAnchor.MiddleRight, TMPro.FontStyles.Bold);
        }

        RectTransform ImagenPoster(Transform p, float x, float y, float w, float h)
        {
            return Imagen(p, "Poster_PathIntoTheAbyss", "PathIntoTheAbyssPoster", x, y, w, h);
        }

        RectTransform Imagen(Transform p, string nombre, string recurso, float x, float y, float w, float h,
            Color? tint = null, bool conservarProporcion = true, float rotacion = 0)
        {
            Sprite sprite = Resources.Load<Sprite>(recurso);
            if (sprite == null) return null;
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(p, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((x + w * 0.5f) * S, -(y + h * 0.5f) * S);
            rt.sizeDelta = new Vector2(w * S, h * S);
            rt.localRotation = Quaternion.Euler(0, 0, rotacion);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = tint ?? Color.white;
            image.preserveAspect = conservarProporcion;
            image.raycastTarget = false;
            return rt;
        }

        IEnumerator TransicionDescarga()
        {
            transicionando = true;
            if (posterDescarga == null || byteDescarga == null)
            {
                Mostrar(3);
                transicionando = false;
                yield break;
            }
            Vector2 posterInicio = posterDescarga.anchoredPosition;
            Vector2 byteInicio = byteDescarga.anchoredPosition;
            Vector2 posterFin = new Vector2((280 + 83 * 0.5f) * S, -(99 + 100 * 0.5f) * S);
            Vector2 byteFin = new Vector2((-120 + 106 * 0.5f) * S, byteInicio.y);
            const float duracion = 0.65f;
            float tiempo = 0;
            while (tiempo < duracion)
            {
                tiempo += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(tiempo / duracion);
                t = t * t * (3f - 2f * t);
                posterDescarga.anchoredPosition = Vector2.LerpUnclamped(posterInicio, posterFin, t);
                byteDescarga.anchoredPosition = Vector2.LerpUnclamped(byteInicio, byteFin, t);
                yield return null;
            }
            Mostrar(3);
            transicionando = false;
        }

        IEnumerator AnimacionDescargaPoster()
        {
            transicionando = true;
            if (posterCentral == null)
            {
                Mostrar(4);
                transicionando = false;
                yield break;
            }
            Vector2 inicio = posterCentral.anchoredPosition;
            Vector2 fin = new Vector2(inicio.x, -(405 + 50) * S);
            const float duracion = 0.6f;
            float tiempo = 0;
            while (tiempo < duracion)
            {
                tiempo += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(tiempo / duracion);
                t = t * t * (3f - 2f * t);
                posterCentral.anchoredPosition = Vector2.LerpUnclamped(inicio, fin, t);
                yield return null;
            }
            Mostrar(4);
            transicionando = false;
        }

        TMPro.TMP_Text Texto(Transform p, string valor, float x, float y, float w, float h, int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft, TMPro.FontStyles estilo = TMPro.FontStyles.Normal)
        {
            return UIKit.Texto(p, valor, x * S, y * S, w * S, h * S, Mathf.RoundToInt(size * S), color, anchor, estilo);
        }

        void Linea(Transform p, float x, float y, float w, float h, Color color)
        {
            UIKit.Rellenar(p, x * S, y * S, Mathf.Max(1, w * S), Mathf.Max(1, h * S), color);
        }

        void Panel(Transform p, float x, float y, float w, float h, Color borde, Color? relleno = null)
        {
            CrearPanel(p, x * S, y * S, w * S, h * S,
                relleno ?? new Color(0.055f, 0.085f, 0.07f, 0.94f), borde);
        }

        RectTransform CrearPanel(Transform p, float x, float y, float w, float h, Color relleno, Color borde)
        {
            Sprite spriteRelleno = Resources.Load<Sprite>("UI/Panel_Fill");
            Sprite spriteBorde = Resources.Load<Sprite>("UI/Panel_Border");
            if (spriteRelleno == null || spriteBorde == null)
            {
                return UIKit.NuevaCaja(p, x, y, w, h, relleno, borde, 2).rt;
            }
            var rtRelleno = UIKit.Rect(p, "Panel_Relleno", x, y, w, h);
            var fill = rtRelleno.gameObject.AddComponent<Image>();
            fill.sprite = spriteRelleno; fill.type = Image.Type.Sliced;
            fill.color = relleno; fill.raycastTarget = false;
            var rtBorde = UIKit.Rect(p, "Panel_Borde", x, y, w, h);
            var outline = rtBorde.gameObject.AddComponent<Image>();
            outline.sprite = spriteBorde; outline.type = Image.Type.Sliced;
            outline.color = borde; outline.raycastTarget = false;
            return rtRelleno;
        }

        GameObject NuevaPantalla(string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(raiz, false);
            Estirar(go.GetComponent<RectTransform>());
            return go;
        }

        void Mostrar(int indice)
        {
            actual = Mathf.Clamp(indice, 0, pantallas.Length - 1);
            inicioPantallaActual = Time.unscaledTime;
            for (int i = 0; i < pantallas.Length; i++) pantallas[i].SetActive(i == actual);
            float oscuridad = actual == 6 ? 0.36f : actual >= 4 ? 0.90f : 0.72f;
            velo.color = new Color(0.025f, 0.045f, 0.04f, oscuridad);
        }


        void PantallaMinijuego(Transform p)
        {
            minijuegoTutorial = new GameObject("Tutorial_IA_Director", typeof(RectTransform));
            minijuegoTutorial.transform.SetParent(p, false);
            Estirar(minijuegoTutorial.GetComponent<RectTransform>());
            var fondoTutorialGO = new GameObject("Pasillo_Tutorial", typeof(RectTransform), typeof(RawImage));
            fondoTutorialGO.transform.SetParent(minijuegoTutorial.transform, false);
            Estirar(fondoTutorialGO.GetComponent<RectTransform>());
            var fondoTutorial = fondoTutorialGO.GetComponent<RawImage>();
            fondoTutorial.texture = Resources.Load<Texture2D>("Fondos/PasilloAlargado");
            fondoTutorial.raycastTarget = false;
            var sombra = UIKit.Rellenar(minijuegoTutorial.transform, 0, 0, UIKit.W, UIKit.H,
                new Color(0.008f, 0.014f, 0.012f, 0.72f));
            Estirar(sombra.rectTransform);
            var coberturaTutorialGO = new GameObject("Caja_de_Cobertura_Tutorial", typeof(RectTransform), typeof(RawImage));
            coberturaTutorialGO.transform.SetParent(minijuegoTutorial.transform, false);
            var coberturaTutorialRT = coberturaTutorialGO.GetComponent<RectTransform>();
            coberturaTutorialRT.anchorMin = coberturaTutorialRT.anchorMax = new Vector2(0, 1);
            coberturaTutorialRT.pivot = new Vector2(0, 1);
            coberturaTutorialRT.anchoredPosition = new Vector2(0, -330 * S);
            coberturaTutorialRT.sizeDelta = new Vector2(678 * S, 96 * S);
            var coberturaTutorial = coberturaTutorialGO.GetComponent<RawImage>();
            coberturaTutorial.texture = Resources.Load<Texture2D>("Juego/Cobertura_de_caja");
            coberturaTutorial.raycastTarget = false;
            var intro = CrearPanel(minijuegoTutorial.transform, 16 * S, 12 * S, 410 * S, 54 * S,
                new Color(0.045f, 0.07f, 0.052f, 0.96f), Paleta.VerdeBorde);
            UIKit.Byte(intro, 10 * S, 8 * S, 0.82f);
            UIKit.RotuloDialogo(intro, "BYTE / IA DIRECTOR", 52 * S, 6 * S, 300 * S, 12 * S, Paleta.Verde);
            UIKit.CuerpoDialogo(intro, "Ahora " + EnNaranja("YO") + " voy a ser la " + EnNaranja("IA DIRECTOR") + "!... Juguemos >:D",
                52 * S, 25 * S, 348 * S, 22 * S, Paleta.Crema);
            var seguir = CrearPanel(minijuegoTutorial.transform, 16 * S, 78 * S, 148 * S, 35 * S,
                new Color(0.05f, 0.07f, 0.05f, 0.98f), Paleta.Amarillo);
            UIKit.Texto(seguir, "[ + ]  Seguir", 0, 0, 148 * S, 35 * S, Mathf.RoundToInt(12 * S), Paleta.Amarillo, TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
            UIKit.Texto(minijuegoTutorial.transform, "APUNTÁ PARA ELEGIR · DISPARÁ PARA AVANZAR",
                18 * S, 360 * S, 300 * S, 12 * S, Mathf.RoundToInt(7 * S),Paleta.Gris, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);

            minijuegoPartida = new GameObject("Shooter_IA_Director", typeof(RectTransform));
            minijuegoPartida.transform.SetParent(p, false);
            Estirar(minijuegoPartida.GetComponent<RectTransform>());
            var juego = minijuegoPartida.transform;
            // La partida del prototipo ocurre dentro del pasillo, no sobre el fondo del taller.
            var fondoGO = new GameObject("Fondo_Pasillo", typeof(RectTransform), typeof(RawImage));
            fondoGO.transform.SetParent(juego, false);
            Estirar(fondoGO.GetComponent<RectTransform>());
            juegoFondo = fondoGO.GetComponent<RawImage>();
            juegoFondo.texture = Resources.Load<Texture2D>("Fondos/PasilloAlargado");
            juegoFondo.raycastTarget = false;
            var sombraGO = new GameObject("Oscuridad_Cobertura", typeof(RectTransform), typeof(Image));
            sombraGO.transform.SetParent(juego, false);
            Estirar(sombraGO.GetComponent<RectTransform>());
            juegoOscuridad = sombraGO.GetComponent<Image>();
            juegoOscuridad.color = new Color(0.005f, 0.008f, 0.006f, 0f);
            juegoOscuridad.raycastTarget = false;
            // La caja ilustrada reproduce el borde y la franja amarilla del original.
            var coverGO = new GameObject("Caja_de_Cobertura", typeof(RectTransform), typeof(RawImage));
            coverGO.transform.SetParent(juego, false);
            var coverRT = coverGO.GetComponent<RectTransform>();
            coverRT.anchorMin = coverRT.anchorMax = new Vector2(0, 1);
            coverRT.pivot = new Vector2(0, 1);
            coverRT.anchoredPosition = new Vector2(0, -330 * S);
            coverRT.sizeDelta = new Vector2(678 * S, 96 * S);
            juegoCobertura = coverGO.GetComponent<RawImage>();
            juegoCobertura.texture = Resources.Load<Texture2D>("Juego/Cobertura_de_caja");
            juegoCobertura.raycastTarget = false;
            var top = CrearPanel(juego, 16 * S, 12 * S, 410 * S, 54 * S,
                new Color(0.045f, 0.07f, 0.052f, 0.96f), Paleta.VerdeBorde);
            UIKit.Byte(top, 10 * S, 8 * S, 0.82f);
            UIKit.RotuloDialogo(top, "BYTE / IA DIRECTOR", 52 * S, 5 * S, 215 * S, 12 * S, Paleta.Verde);
            juegoIconoEnemigo = Imagen(top, "Icono_IA_Directora_Dialogo", "IADirector/IADirectora", 149, 20, 16, 24);
            juegoTextoDialogo = UIKit.CuerpoDialogo(top, "", 52 * S, 24 * S, 344 * S, 30 * S, Paleta.Crema);
            juegoTextoEnviando = UIKit.CuerpoDialogo(top, "", 52 * S, 24 * S, 95 * S, 30 * S, Paleta.Crema);
            juegoTextoUsando = UIKit.CuerpoDialogo(top, "", 172 * S, 24 * S, 228 * S, 30 * S, Paleta.Crema);
            juegoTextoEnviando.enableWordWrapping = false;
            juegoTextoUsando.enableWordWrapping = false;
            juegoTextoEnviando.gameObject.SetActive(false);
            juegoTextoUsando.gameObject.SetActive(false);
            juegoIconoEnemigo.gameObject.SetActive(false);
            juegoTextoEspacio = UIKit.CuerpoDialogo(top, "Presioná", 316 * S, 24 * S, 55 * S, 30 * S, Paleta.Crema);
            juegoTextoEspacio.gameObject.SetActive(false);
            juegoTextoRecargaPrompt = UIKit.CuerpoDialogo(top, "Presioná", 273 * S, 24 * S, 42 * S, 30 * S, Paleta.Crema);
            juegoTextoRecargaPrompt.gameObject.SetActive(false);
            juegoIconoRecargarDialogo = UIKit.Rect(top, "Icono_Recargar_Dialogo", 317 * S, 27 * S, 14 * S, 9 * S)
                .gameObject.AddComponent<RawImage>();
            juegoIconoRecargarDialogo.texture = Resources.Load<Texture2D>("Juego/BotonRecargar");
            juegoIconoRecargarDialogo.raycastTarget = false;
            juegoIconoRecargarDialogo.gameObject.SetActive(false);
            juegoTextoRecargaFinal = UIKit.CuerpoDialogo(top, "para " + EnNaranja("RECARGAR"), 333 * S, 24 * S, 75 * S, 30 * S, Paleta.Crema);
            juegoTextoRecargaFinal.gameObject.SetActive(false);
            juegoIconoAvanzar = UIKit.Rect(top, "Icono_Avanzar_Dialogo", 373 * S, 28 * S, 10 * S, 15 * S)
                .gameObject.AddComponent<Image>();
            juegoIconoAvanzar.sprite = Resources.Load<Sprite>("IconoAvanzar");
            juegoIconoAvanzar.preserveAspect = true;
            juegoIconoAvanzar.raycastTarget = false;
            juegoIconoAvanzar.gameObject.SetActive(false);

            var municion = CrearPanel(juego, 8 * S, 270 * S, 138 * S, 70 * S,
                new Color(0.045f, 0.07f, 0.052f, 0.97f), Paleta.VerdeBorde);
            juegoPanelMunicion = municion.gameObject;
            juegoTextoRonda = Texto(municion, "RONDA 1 / 3", 7, 5, 125, 15, 8, Paleta.Verde, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            juegoTextoBalas = Texto(municion, "01 / 03 BALAS", 7, 24, 128, 34, 16, Paleta.Naranja, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);

            var estado = CrearPanel(juego, 541 * S, 192 * S, 128 * S, 132 * S,
                new Color(0.045f, 0.07f, 0.052f, 0.97f), Paleta.VerdeBorde);
            UIKit.Texto(estado, "MÁQUINA DE ESTADOS:", 5 * S, 5 * S, 118 * S, 13 * S, Mathf.RoundToInt(7 * S),Paleta.Verde, TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
            string[] nombresEstadoJuego = { "DISPARANDO", "A CUBIERTO", "RECARGANDO" };
            float[] yEstadoJuego = { 22, 61, 97 };
            for (int i = 0; i < nombresEstadoJuego.Length; i++)
            {
                var cajaEstado = UIKit.NuevaCaja(estado, 7 * S, yEstadoJuego[i] * S, 114 * S, 22 * S,
                    new Color(0.08f, 0.10f, 0.08f, 0.96f), Paleta.VerdeBorde, 1.2f * S);
                juegoEstadosRelleno[i] = cajaEstado.fill;
                juegoEstadosBorde[i] = cajaEstado.borde;
                juegoEstadosTexto[i] = Texto(estado, nombresEstadoJuego[i], 7, yEstadoJuego[i], 114, 22, 8,
                    Paleta.Crema, TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
            }
            UIKit.Texto(estado, "↓ entrar en cobertura", 6 * S, 47 * S, 116 * S, 12 * S, Mathf.RoundToInt(6 * S),Paleta.Gris, TextAnchor.MiddleCenter);
            UIKit.Texto(estado, "↓ 00 balas + recargar", 6 * S, 83 * S, 116 * S, 12 * S, Mathf.RoundToInt(6 * S),Paleta.Gris, TextAnchor.MiddleCenter);
            UIKit.Texto(estado, "↓ recarga al 100 %", 6 * S, 119 * S, 116 * S, 12 * S, Mathf.RoundToInt(6 * S),Paleta.Gris, TextAnchor.MiddleCenter);
            EstablecerEstadoJuego("DISPARANDO");

            juegoRecarga = new GameObject("Progreso_Recarga", typeof(RectTransform));
            juegoRecarga.transform.SetParent(juego, false);
            Estirar(juegoRecarga.GetComponent<RectTransform>());
            var cajaRecarga = CrearPanel(juegoRecarga.transform, 10 * S, 270 * S, 280 * S, 60 * S,
                new Color(0.035f, 0.055f, 0.04f, 0.97f), Paleta.Amarillo);
            juegoTextoRecarga = Texto(cajaRecarga, "RECARGANDO // 0 %", 8, 4, 260, 12, 7, Paleta.Amarillo, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            juegoIconoRecargar = UIKit.Rect(cajaRecarga, "Icono_Boton_Recargar", 55 * S, 2 * S, 20 * S, 12 * S).gameObject.AddComponent<RawImage>();
            juegoIconoRecargar.texture = Resources.Load<Texture2D>("Juego/BotonRecargar");
            juegoIconoRecargar.raycastTarget = false;
            juegoIconoRecargar.gameObject.SetActive(false);
            juegoTextoRecargaSufijo = Texto(cajaRecarga, "PARA RECARGAR", 80, 4, 180, 12, 7, Paleta.Amarillo, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            juegoTextoRecargaSufijo.gameObject.SetActive(false);
            juegoBarraRecarga = UIKit.Rellenar(cajaRecarga, 8 * S, 27 * S, 260 * S, 7 * S, Paleta.Verde).GetComponent<Image>();
            juegoRecarga.SetActive(false);

            minijuegoTutorial.SetActive(true);
            minijuegoPartida.SetActive(false);
            CrearPantallaResultado(p);
        }

        void CrearPantallaResultado(Transform p)
        {
            pantallaResultado = new GameObject("Pantalla_Puntuacion_Final", typeof(RectTransform));
            pantallaResultado.transform.SetParent(p, false);
            var root = pantallaResultado.GetComponent<RectTransform>();
            Estirar(root);

            var fondoGO = new GameObject("Fondo_Puntuacion", typeof(RectTransform), typeof(RawImage));
            fondoGO.transform.SetParent(root, false);
            Estirar(fondoGO.GetComponent<RectTransform>());
            var fondo = fondoGO.GetComponent<RawImage>();
            fondo.texture = Resources.Load<Texture2D>("Fondos/PasilloAlargado");
            fondo.raycastTarget = false;
            var oscuro = UIKit.Rellenar(root, 0, 0, UIKit.W, UIKit.H, new Color(0.008f, 0.014f, 0.012f, 0.73f));
            Estirar(oscuro.rectTransform);

            var cabecera = CrearPanel(root, 18 * S, 12 * S, 642 * S, 27 * S,
                new Color(0.045f, 0.07f, 0.052f, 0.94f), Paleta.VerdeBorde);
            UIKit.RotuloDialogo(cabecera, "SINTÉTIKA / IA EN PROGRAMACIÓN DE VIDEOJUEGOS",
                10 * S, 6 * S, 620 * S, 14 * S, Paleta.Verde);

            var tarjeta = CrearPanel(root, 112 * S, 50 * S, 423 * S, 205 * S,
                new Color(0.045f, 0.07f, 0.052f, 0.97f), Paleta.VerdeBorde);
            textoPuntajeResultado = Texto(tarjeta, "0 Puntos", 16, 10, 390, 44, 30,
                Paleta.Crema, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            Linea(tarjeta, 16, 53, 44, 2, Paleta.Naranja);
            int[] umbrales = { 100000, 90000, 80000, 40000 };
            for (int i = 0; i < umbrales.Length; i++)
                textosUmbrales[i] = Texto(tarjeta, "", 18, 68 + i * 18, 340, 18, 12,
                    Paleta.Gris, TextAnchor.MiddleLeft);

            var guardar = CrearPanel(tarjeta, 263 * S, 158 * S, 148 * S, 34 * S,
                new Color(0.055f, 0.085f, 0.07f, 0.98f), Paleta.Amarillo);
            botonGuardarResultado = guardar;
            Imagen(guardar, "Icono_Guardar_Puntuacion", "IconoGuardar", 10, 7, 19, 19);
            UIKit.Texto(guardar, "Guardar", 34 * S, 0, 105 * S, 34 * S, Mathf.RoundToInt(14 * S),
                Paleta.Amarillo, TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);

            var bytePanel = CrearPanel(root, 112 * S, 270 * S, 423 * S, 58 * S,
                new Color(0.045f, 0.07f, 0.052f, 0.97f), Paleta.VerdeBorde);
            UIKit.Byte(bytePanel, 9 * S, 8 * S, 0.82f);
            UIKit.RotuloDialogo(bytePanel, "BYTE / IA DERROTADA", 56 * S, 7 * S, 342 * S, 12 * S, Paleta.Verde);
            textoResultadoByte = UIKit.CuerpoDialogo(bytePanel, "", 56 * S, 26 * S, 353 * S, 24 * S, Paleta.Crema);

            Texto(root, "P: BORRAR PUNTUACIONES GUARDADAS  ·  ESPACIO: GUARDAR",
                18, 359, 642, 12, 7, Paleta.Gris, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            pantallaResultado.SetActive(false);
        }

        void ActualizarPantallaResultado()
        {
            textoPuntajeResultado.text = FormatearPuntos(juegoPuntaje) + " Puntos";
            textoResultadoByte.text = juegoPuntaje >= 80000
                ? "¡GANASTE! Sabés sobre IA; guardemos tu puntuación."
                : "No alcanzaste el umbral esta vez. Guardemos tu puntuación.";
            int[] umbrales = { 100000, 90000, 80000, 40000 };
            for (int i = 0; i < umbrales.Length; i++)
            {
                int cantidad = ContarParticipantesUmbral(umbrales[i], true);
                textosUmbrales[i].text = FormatearPuntos(umbrales[i]) + " Puntos - " + cantidad + " Usuarios";
            }
        }

        int ClasificarUmbral(int puntaje)
        {
            if (puntaje >= 100000) return 100000;
            if (puntaje >= 90000) return 90000;
            if (puntaje >= 80000) return 80000;
            if (puntaje >= 40000) return 40000;
            return 0;
        }

        int ContarParticipantesUmbral(int umbral, bool incluirActual)
        {
            int total = PlayerPrefs.GetInt("IADirector_TotalParticipantes", 0);
            int cantidad = 0;
            for (int i = 1; i <= total; i++)
                if (ClasificarUmbral(PlayerPrefs.GetInt("IADirector_Participante_" + i, 0)) == umbral)
                    cantidad++;
            if (incluirActual && ClasificarUmbral(juegoPuntaje) == umbral) cantidad++;
            return cantidad;
        }

        void RecalcularConteosUmbral()
        {
            int[] umbrales = { 100000, 90000, 80000, 40000 };
            foreach (int umbral in umbrales)
                PlayerPrefs.SetInt("IADirector_Umbral_" + umbral, ContarParticipantesUmbral(umbral, false));
        }

        string FormatearPuntos(int puntos)
        {
            return puntos.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", ".");
        }

        void MostrarPantallaResultado()
        {
            juegoFase = 3;
            minijuegoTutorial.SetActive(false);
            minijuegoPartida.SetActive(false);
            pantallaResultado.SetActive(true);
            ActualizarPantallaResultado();
            actual = 7;
            velo.color = new Color(0.025f, 0.045f, 0.04f, 0.90f);
        }

        void GuardarPuntuacionYReiniciar()
        {
            int total = PlayerPrefs.GetInt("IADirector_TotalParticipantes", 0) + 1;
            PlayerPrefs.SetInt("IADirector_TotalParticipantes", total);
            PlayerPrefs.SetInt("IADirector_UltimaPuntuacion", juegoPuntaje);
            PlayerPrefs.SetInt("IADirector_Participante_" + total, juegoPuntaje);
            RecalcularConteosUmbral();
            PlayerPrefs.Save();
            SceneManager.LoadScene("01_Intro");
        }

        void BorrarPuntuacionesGuardadas()
        {
            int total = PlayerPrefs.GetInt("IADirector_TotalParticipantes", 0);
            for (int i = 1; i <= total; i++)
                PlayerPrefs.DeleteKey("IADirector_Participante_" + i);
            PlayerPrefs.DeleteKey("IADirector_TotalParticipantes");
            PlayerPrefs.DeleteKey("IADirector_UltimaPuntuacion");
            int[] umbrales = { 100000, 90000, 80000, 40000 };
            for (int i = 0; i < umbrales.Length; i++)
            {
                PlayerPrefs.DeleteKey("IADirector_Umbral_" + umbrales[i]);
                textosUmbrales[i].text = FormatearPuntos(umbrales[i]) + " Puntos - 0 Usuarios";
            }
            PlayerPrefs.Save();
        }

        void ActualizarMinijuego()
        {
            bool disparo = control != null && control.TomarClickPistola();
            bool botonRecargaActual = control != null && control.BotonRecarga;
            bool solicitarRecarga = botonRecargaActual && !botonRecargaAnterior;
            botonRecargaAnterior = botonRecargaActual;
            bool click = Input.GetMouseButtonDown(0);
            Vector2 mira = ObtenerMiraJuego();

            if (juegoFase == 0)
            {
                if (click || disparo || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    minijuegoTutorial.SetActive(false);
                    minijuegoPartida.SetActive(true);
                    juegoRonda = 0;
                    juegoPuntaje = 0;
                    IniciarRondaJuego();
                }
                return;
            }
            if (juegoFase == 1)
            {
                if (click || disparo) DispararJuego(mira);
                if (juegoFase == 1) ActualizarAvanceEnemigos();
                if (juegoEnemigos.Count == 0 && juegoRonda > 1) CompletarRondaJuego();
            }
            else if (juegoFase == 2)
            {
                if (!juegoRecargaIniciada)
                {
                    if (!Input.GetKeyDown(KeyCode.X) && !solicitarRecarga) return;
                    juegoRecargaIniciada = true;
                    EstablecerEstadoJuego("RECARGANDO");
                    juegoTextoRecarga.text = "RECARGANDO // 0 %";
                    juegoIconoRecargar.gameObject.SetActive(false);
                    juegoTextoRecargaSufijo.gameObject.SetActive(false);
                    MostrarDialogoJuego("Iniciaste la " + EnNaranja("RECARGA") + ".");
                }
                juegoProgresoRecarga = Mathf.Min(1f, juegoProgresoRecarga + Time.unscaledDeltaTime / 2f);
                juegoBarraRecarga.rectTransform.sizeDelta = new Vector2(260 * S * juegoProgresoRecarga, 7 * S);
                juegoTextoRecarga.text = "RECARGANDO // " + Mathf.RoundToInt(juegoProgresoRecarga * 100) + " %";
                if (juegoProgresoRecarga >= 1f && !juegoRecargaCompleta)
                {
                    juegoRecargaCompleta = true;
                    juegoBalas = 3;
                    juegoTextoBalas.text = "03 / 03 BALAS";
                    EstablecerEstadoJuego("A CUBIERTO");
                    juegoSinBalasAvisado = false;
                    MostrarDialogoAvance("Pasaste de “" + EnNaranja("RECARGAR") + "” a “" + EnNaranja("COBERTURA") + "”…");
                }
                if (juegoRecargaCompleta && Input.GetKeyDown(KeyCode.Space))
                    SalirCoberturaJuego();
            }
            else if (juegoFase == 3 && (Input.GetKeyDown(KeyCode.Return) || click || disparo))
            {
                juegoFase = 0;
                Mostrar(5);
            }
        }

        void IniciarRondaJuego()
        {
            juegoRonda++;
            juegoFase = 1;
            juegoSinBalasAvisado = false;
            juegoBalas = juegoRonda == 1 ? 1 : 3;
            juegoTextoRonda.text = "RONDA " + juegoRonda + " / 3";
            juegoTextoBalas.text = juegoBalas.ToString("00") + " / 03 BALAS";
            EstablecerEstadoJuego("DISPARANDO");
            juegoOscuridad.color = new Color(0.005f, 0.008f, 0.006f, 0f);
            juegoPanelMunicion.SetActive(true);
            juegoRecarga.SetActive(false);
            juegoIconoRecargar.gameObject.SetActive(false);
            juegoTextoRecargaSufijo.gameObject.SetActive(false);
            juegoIconoAvanzar.gameObject.SetActive(false);
            LimpiarEnemigosJuego();
            int cantidad = juegoRonda == 1 ? 1 : juegoRonda;
            for (int i = 0; i < cantidad; i++)
            {
                float w = juegoRonda == 1 ? 52 : 44;
                float h = juegoRonda == 1 ? 80 : 68;
                float x = 339 - w * 0.5f;
                float y = 190 - h * 0.5f;
                var enemigo = Imagen(minijuegoPartida.transform, "Enemigo_R" + juegoRonda + "_" + i,
                    "Enemigos/EnemigoStand", x, y, w, h);
                if (enemigo != null)
                    RegistrarEnemigoJuego(enemigo, i, cantidad);
            }
            string usando = juegoRonda == 1
                ? "Usando " + EnNaranja("TU pathfinding")
                : "Usando " + EnNaranja("A*") + " y " + EnNaranja("TUS enemigos");
            MostrarDialogoEnvio("x" + cantidad, usando);
        }

        public void IrAlMinijuegoDirecto()
        {
            ultimoFrameAtajoZ = Time.frameCount;
            Mostrar(6);
            minijuegoTutorial.SetActive(false);
            minijuegoPartida.SetActive(true);
            if (pantallaResultado != null) pantallaResultado.SetActive(false);
            juegoRonda = 0;
            juegoPuntaje = 0;
            IniciarRondaJuego();
        }

        void DispararJuego(Vector2 punto)
        {
            if (juegoBalas <= 0)
            {
                if (punto.y > 270)
                {
                    juegoFase = 2;
                    juegoProgresoRecarga = 0;
                    juegoRecargaCompleta = false;
                    juegoRecargaIniciada = false;
                    EstablecerEstadoJuego("A CUBIERTO");
                    juegoOscuridad.color = new Color(0.005f, 0.008f, 0.006f, 0.78f);
                    juegoPanelMunicion.SetActive(false);
                    juegoRecarga.SetActive(true);
                    juegoBarraRecarga.rectTransform.sizeDelta = new Vector2(1, 7 * S);
                    juegoTextoRecarga.text = "PRESIONÁ";
                    juegoIconoRecargar.gameObject.SetActive(true);
                    juegoTextoRecargaSufijo.gameObject.SetActive(true);
                    MostrarDialogoRecarga();
                }
                else if (!juegoSinBalasAvisado)
                {
                    MostrarDialogoJuego("Sin balas, " + EnNaranja("AGACHATE") + " para entrar en estado “" + EnNaranja("RECARGA") + "”.");
                    juegoSinBalasAvisado = true;
                }
                return;
            }

            juegoBalas--;
            juegoTextoBalas.text = juegoBalas.ToString("00") + " / 03 BALAS";
            int golpe = -1;
            for (int i = juegoPosicionesEnemigos.Count - 1; i >= 0; i--)
            {
                float radio = 34f * (juegoEnemigos[i] != null ? juegoEnemigos[i].localScale.x : 1f);
                if (Vector2.Distance(punto, juegoPosicionesEnemigos[i]) < radio) { golpe = i; break; }
            }
            if (golpe >= 0)
            {
                juegoPuntaje += 20000;
                Destroy(juegoEnemigos[golpe].gameObject);
                juegoEnemigos.RemoveAt(golpe);
                juegoPosicionesEnemigos.RemoveAt(golpe);
                juegoOrigenEnemigos.RemoveAt(golpe);
                juegoDestinoEnemigos.RemoveAt(golpe);
                juegoProgresoEnemigos.RemoveAt(golpe);
                juegoDuracionAvanceEnemigos.RemoveAt(golpe);
            }
            else juegoPuntaje = Mathf.Max(0, juegoPuntaje - 10000);
            if (juegoBalas == 0 && !juegoSinBalasAvisado)
            {
                MostrarDialogoJuego("Sin balas, " + EnNaranja("AGACHATE") + " para entrar en estado “" + EnNaranja("RECARGA") + "”.");
                juegoSinBalasAvisado = true;
            }
        }

        void SalirCoberturaJuego()
        {
            juegoPanelMunicion.SetActive(true);
            juegoRecarga.SetActive(false);
            juegoFase = 1;
            EstablecerEstadoJuego("DISPARANDO");
            juegoOscuridad.color = new Color(0.005f, 0.008f, 0.006f, 0f);
            juegoIconoAvanzar.gameObject.SetActive(false);
            if (juegoRonda == 1 && juegoEnemigos.Count == 0) IniciarRondaJuego();
            else if (juegoEnemigos.Count == 0) CompletarRondaJuego();
            else
            {
                int enemigos = juegoEnemigos.Count;
                string quedan = enemigos == 1 ? "Queda " + EnNaranja("1 enemigo") : "Quedan " + EnNaranja(enemigos + " enemigos");
                MostrarDialogoJuego(quedan + " en pie. Seguí disparando.");
            }
        }

        void CompletarRondaJuego()
        {
            if (juegoRonda >= 3)
            {
                EstablecerEstadoJuego("COMPLETADO");
                MostrarPantallaResultado();
                return;
            }
            juegoRonda++;
            juegoFase = 1;
            juegoBalas = 3;
            juegoTextoRonda.text = "RONDA " + juegoRonda + " / 3";
            juegoTextoBalas.text = "03 / 03 BALAS";
            EstablecerEstadoJuego("DISPARANDO");
            juegoOscuridad.color = new Color(0.005f, 0.008f, 0.006f, 0f);
            CrearEnemigosRondaActual();
            MostrarDialogoEnvio("x" + juegoRonda, "Usando " + EnNaranja("A*") + " y " + EnNaranja("TUS enemigos"));
        }

        void CrearEnemigosRondaActual()
        {
            LimpiarEnemigosJuego();
            int cantidad = juegoRonda;
            for (int i = 0; i < cantidad; i++)
            {
                var enemigo = Imagen(minijuegoPartida.transform, "Enemigo_R" + juegoRonda + "_" + i,
                    "Enemigos/EnemigoStand", 339 - 22, 190 - 34, 44, 68);
                if (enemigo != null)
                    RegistrarEnemigoJuego(enemigo, i, cantidad);
            }
        }

        void RegistrarEnemigoJuego(RectTransform enemigo, int indice, int cantidad)
        {
            var origen = new Vector2(339, 190);
            float xDestino = cantidad <= 1 ? 339 : Mathf.Lerp(170, 508, indice / (float)(cantidad - 1));
            juegoEnemigos.Add(enemigo);
            juegoPosicionesEnemigos.Add(origen);
            juegoOrigenEnemigos.Add(origen);
            juegoDestinoEnemigos.Add(new Vector2(xDestino, 270));
            juegoProgresoEnemigos.Add(0f);
            juegoDuracionAvanceEnemigos.Add(juegoRonda == 1 ? 4f : UnityEngine.Random.Range(2f, 4f));
        }

        void MostrarDialogoJuego(string mensaje, bool mostrarIconoAvanzar = false)
        {
            juegoTextoDialogo.text = mensaje;
            juegoTextoDialogo.gameObject.SetActive(true);
            juegoTextoDialogo.rectTransform.sizeDelta = new Vector2(344 * S, 30 * S);
            juegoTextoDialogo.enableWordWrapping = true;
            juegoTextoEnviando.gameObject.SetActive(false);
            juegoTextoUsando.gameObject.SetActive(false);
            juegoTextoEspacio.gameObject.SetActive(false);
            juegoTextoRecargaPrompt.gameObject.SetActive(false);
            juegoIconoRecargarDialogo.gameObject.SetActive(false);
            juegoTextoRecargaFinal.gameObject.SetActive(false);
            juegoIconoEnemigo.gameObject.SetActive(false);
            if (juegoIconoAvanzar != null) juegoIconoAvanzar.gameObject.SetActive(mostrarIconoAvanzar);
        }

        void MostrarDialogoRecarga()
        {
            MostrarDialogoJuego("Entraste en estado “" + EnNaranja("A CUBIERTO") + "”.");
            juegoTextoDialogo.enableWordWrapping = false;
            juegoTextoRecargaPrompt.gameObject.SetActive(true);
            juegoIconoRecargarDialogo.gameObject.SetActive(true);
            juegoTextoRecargaFinal.gameObject.SetActive(true);

            float x = 52 * S;
            x += MedirTextoInline(juegoTextoDialogo, x, 24 * S) + 6 * S;
            x += MedirTextoInline(juegoTextoRecargaPrompt, x, 24 * S) + 5 * S;
            juegoIconoRecargarDialogo.rectTransform.anchoredPosition = new Vector2(x, -27 * S);
            x += juegoIconoRecargarDialogo.rectTransform.sizeDelta.x + 5 * S;
            MedirTextoInline(juegoTextoRecargaFinal, x, 24 * S);
        }

        void MostrarDialogoAvance(string mensaje)
        {
            MostrarDialogoJuego(mensaje);
            juegoTextoDialogo.enableWordWrapping = false;
            juegoTextoEspacio.gameObject.SetActive(true);
            juegoIconoAvanzar.gameObject.SetActive(true);
            float x = 52 * S;
            x += MedirTextoInline(juegoTextoDialogo, x, 24 * S) + 6 * S;
            x += MedirTextoInline(juegoTextoEspacio, x, 24 * S) + 5 * S;
            juegoIconoAvanzar.rectTransform.anchoredPosition = new Vector2(x, -28 * S);
        }

        void MostrarDialogoEnvio(string cantidad, string mensaje)
        {
            juegoTextoDialogo.gameObject.SetActive(false);
            juegoTextoEnviando.text = "Enviando " + cantidad;
            juegoTextoUsando.text = mensaje;
            juegoTextoEnviando.enableWordWrapping = false;
            juegoTextoUsando.enableWordWrapping = false;
            juegoTextoEnviando.gameObject.SetActive(true);
            juegoTextoUsando.gameObject.SetActive(true);
            juegoTextoEspacio.gameObject.SetActive(false);
            juegoTextoRecargaPrompt.gameObject.SetActive(false);
            juegoIconoRecargarDialogo.gameObject.SetActive(false);
            juegoTextoRecargaFinal.gameObject.SetActive(false);
            juegoIconoEnemigo.gameObject.SetActive(true);
            juegoIconoAvanzar.gameObject.SetActive(false);
            float x = 52 * S;
            x += MedirTextoInline(juegoTextoEnviando, x, 24 * S) + 6 * S;
            juegoIconoEnemigo.anchoredPosition = new Vector2(x + juegoIconoEnemigo.sizeDelta.x * 0.5f, -32 * S);
            x += juegoIconoEnemigo.sizeDelta.x + 6 * S;
            MedirTextoInline(juegoTextoUsando, x, 24 * S);
        }

        float MedirTextoInline(TMPro.TMP_Text texto, float x, float y)
        {
            var rt = texto.rectTransform;
            float alto = rt.sizeDelta.y;
            texto.enableWordWrapping = false;
            rt.sizeDelta = new Vector2(1200 * S, alto);
            rt.anchoredPosition = new Vector2(x, -y);
            texto.ForceMeshUpdate();
            float ancho = Mathf.Ceil(texto.GetPreferredValues(texto.text, 1200 * S, alto).x) + 2 * S;
            rt.sizeDelta = new Vector2(ancho, alto);
            return ancho;
        }

        string EnNaranja(string texto)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(Paleta.Naranja) + ">" + texto + "</color>";
        }

        void EstablecerEstadoJuego(string nombre)
        {
            int activo = nombre == "DISPARANDO" ? 0 : nombre == "A CUBIERTO" ? 1 : nombre == "RECARGANDO" ? 2 : -1;
            for (int i = 0; i < juegoEstadosTexto.Length; i++)
            {
                bool resaltar = i == activo;
                if (juegoEstadosRelleno[i] != null)
                    juegoEstadosRelleno[i].color = resaltar ? Paleta.Amarillo : new Color(0.08f, 0.10f, 0.08f, 0.96f);
                if (juegoEstadosBorde[i] != null)
                    juegoEstadosBorde[i].color = resaltar ? Paleta.Amarillo : Paleta.VerdeBorde;
                if (juegoEstadosTexto[i] != null)
                    juegoEstadosTexto[i].color = resaltar ? Paleta.Oscuro : Paleta.Crema;
            }
        }

        void LimpiarEnemigosJuego()
        {
            foreach (var enemigo in juegoEnemigos)
                if (enemigo != null) Destroy(enemigo.gameObject);
            juegoEnemigos.Clear();
            juegoPosicionesEnemigos.Clear();
            juegoOrigenEnemigos.Clear();
            juegoDestinoEnemigos.Clear();
            juegoProgresoEnemigos.Clear();
            juegoDuracionAvanceEnemigos.Clear();
        }

        void ActualizarAvanceEnemigos()
        {
            for (int i = juegoEnemigos.Count - 1; i >= 0; i--)
            {
                RectTransform enemigo = juegoEnemigos[i];
                if (enemigo == null) continue;
                float progreso = Mathf.Min(1f, juegoProgresoEnemigos[i] + Time.unscaledDeltaTime / juegoDuracionAvanceEnemigos[i]);
                juegoProgresoEnemigos[i] = progreso;
                Vector2 centro = Vector2.Lerp(juegoOrigenEnemigos[i], juegoDestinoEnemigos[i], progreso);
                juegoPosicionesEnemigos[i] = centro;
                enemigo.anchoredPosition = new Vector2(centro.x * S, -centro.y * S);
                enemigo.localScale = Vector3.one * Mathf.Lerp(1f, EscalaFinalEnemigo, progreso);

                if (progreso >= 1f)
                {
                    juegoPuntaje = Mathf.Max(0, juegoPuntaje - PenalizacionGolpeEnemigo);
                    Destroy(enemigo.gameObject);
                    juegoEnemigos.RemoveAt(i);
                    juegoPosicionesEnemigos.RemoveAt(i);
                    juegoOrigenEnemigos.RemoveAt(i);
                    juegoDestinoEnemigos.RemoveAt(i);
                    juegoProgresoEnemigos.RemoveAt(i);
                    juegoDuracionAvanceEnemigos.RemoveAt(i);
                    MostrarDialogoJuego("¡Te golpearon! " + EnNaranja("-20.000 PUNTOS") + ".");
                }
            }
        }

        Vector2 ObtenerMiraJuego()
        {
            if (control != null && control.Conectado)
                return new Vector2(Mathf.Clamp(339 + control.Yaw * 6.78f, 0, 678),
                    Mathf.Clamp(190 - control.Pitch * 2.9f, 0, 380));
            return new Vector2(Input.mousePosition.x * 678f / Screen.width,
                (Screen.height - Input.mousePosition.y) * 380f / Screen.height);
        }

        void OnDestroy() { Cursor.visible = true; }
        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
