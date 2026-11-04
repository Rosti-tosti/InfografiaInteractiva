using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Pathfinding
{
    /// <summary>Recorrido interactivo de Máquina de Estados y su minijuego.</summary>
    public class MaquinaEstadosFlow : MonoBehaviour
    {
        const float duracionAnuncioInicial = 3.8f;
        const float duracionPantallaCarga = 2.8f;
        readonly Color turquesa = new Color(0.34f, 0.82f, 0.78f);
        ArcadeGunInput control;
        Canvas canvas;
        Transform raiz;
        Image veloFondo;
        GameObject pantallaByteCarga, pantallaCarga, pantallaAxel, pantallaDecision;
        GameObject pantallaHistorial, pantallaSecuencia, pantallaPrograma, pantallaProgramaFinal;
        Image[] bordes = new Image[3];
        CanvasGroup[] gruposTarjeta = new CanvasGroup[3];
        RectTransform[] tarjetas = new RectTransform[3];
        RectTransform enemigoHistorial;
        RectTransform panelEnemigoHistorial;
        Image imagenEnemigoHistorial;
        Image imagenEnemigoPrograma;
        Vector2 posicionInicialEnemigoHistorial;
        float elevacionObjetivoEnemigoHistorial;
        RectTransform historialDiagrama;
        RectTransform[] cartasObjetivo = new RectTransform[8];
        Image[] bordesObjetivo = new Image[8];
        Sprite[] animacionEstadoCentral;
        float proximoCuadroAnimacion;
        int cuadroAnimacion;
        Sprite[] cuadrosPantallaPrograma;
        float proximoCuadroPrograma;
        int cuadroPantallaPrograma;
        Image[] imagenesReversoCarta = new Image[8];
        Image[] imagenesFrenteCarta = new Image[8];
        int[] estadoCarta = new int[8];
        bool[] cartaDeCara = new bool[8];
        bool[] cartaCorrecta = new bool[8];
        bool[] estadoNecesarioDeCara = new bool[3];
        TMPro.TMP_Text cuerpoHistorial;
        TMPro.TMP_Text cuerpoHistorialByte, cuerpoDialogoSecuencia, textoDialogoDecision, indicadorObjetivoHistorial;
        Image bordeAvisoHistorial, fondoAvisoHistorial;
        RectTransform botonCrearPrograma;
        RectTransform botonPathfindingFinal, botonIADirectorFinal;
        RectTransform cajaDialogoSecuencia;
        RectTransform cajaDialogoHistorialByte;
        int seleccion = -1, ultimaSeleccionDecision = -1, seleccionObjetivo = -1, opcionElegida = 1;
        int disparosHistorial, estadosCorrectosDeCara;
        bool iniciado, decisionConfirmada;
        float inicioCarga, finAvisoFalloHistorial;
        bool avisoFalloHistorial;
        // Assets que se utilizan en el diagrama y en la secuencia del minijuego.
        readonly string[] recursosEstado = { "Enemigos/EnemigoStand", "Enemigos/EnemigoIdle_1", "Enemigos/EnemigoStand_2" };
        readonly string[] nombresEstado = { "QUIETO", "ESTADO", "ATACAR" };
        readonly string[] nombresAccion = { "ESQUIVAR", "SALTAR", "AGACHARSE" };

        public void Iniciar(ArcadeGunInput entrada)
        {
            if (iniciado) return;
            iniciado = true;
            control = entrada;
            ConstruirUI();
            Mostrar(pantallaByteCarga);
            inicioCarga = Time.unscaledTime;
        }

        void Update()
        {
            if (!iniciado) return;
            ActualizarAnimacionMuestra();
            ActualizarAnimacionPrograma();
            ActualizarAscensoEnemigo();
            ActualizarAvisoFalloHistorial();
            if (pantallaByteCarga.activeSelf)
            {
                if (ShootButton.Procesar()) return;
                if (Time.unscaledTime - inicioCarga > duracionAnuncioInicial)
                {
                    Mostrar(pantallaCarga);
                    inicioCarga = Time.unscaledTime;
                }
                return;
            }
            if (pantallaCarga.activeSelf)
            {
                if (Time.unscaledTime - inicioCarga > duracionPantallaCarga) Mostrar(pantallaAxel);
                return;
            }

            if ((pantallaAxel.activeSelf || pantallaDecision.activeSelf || pantallaPrograma.activeSelf) &&
                ShootButton.Procesar()) return;
            bool disparo = control != null && control.TomarClickPistola();

            if (pantallaPrograma.activeSelf)
            {
                if (AvanceSimple(disparo)) Mostrar(pantallaProgramaFinal);
                return;
            }

            if (pantallaProgramaFinal.activeSelf)
            {
                Vector2 punto = PuntoMira();
                if (disparo && Dentro(botonPathfindingFinal, punto)) { IrAInicioPathfinding(); return; }
                if (disparo && Dentro(botonIADirectorFinal, punto)) { IrAIADirector(); return; }
                ShootButton.Procesar();
                return;
            }

            if (pantallaAxel.activeSelf)
            {
                if (AvanceSimple(disparo)) Mostrar(pantallaDecision);
                return;
            }

            if (pantallaDecision.activeSelf)
            {
                ActualizarSeleccionDecision();
                AnimarTarjetas();
                if (!decisionConfirmada && seleccion >= 0 && (disparo || Input.GetKeyDown(KeyCode.Space) ||
                    Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
                {
                    opcionElegida = seleccion;
                    decisionConfirmada = true;
                    IniciarHistorial();
                }
                return;
            }

            if (pantallaHistorial.activeSelf)
            {
                bool apuntandoAlRobot = Dentro(enemigoHistorial, PuntoMira());
                if (disparo || Input.GetMouseButtonDown(0))
                {
                    if (apuntandoAlRobot) RegistrarDisparo();
                    else MostrarFalloDisparoHistorial();
                }
                return;
            }

            if (pantallaSecuencia.activeSelf)
            {
                ActualizarSeleccionObjetivo();
                bool activarCarta = seleccionObjetivo >= 0 && (disparo || Input.GetKeyDown(KeyCode.Space) ||
                    Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0));
                if (activarCarta) VoltearCarta(seleccionObjetivo);
                if (!activarCarta && MinijuegoResuelto() && botonCrearPrograma != null &&
                    ActivarBoton(botonCrearPrograma, disparo)) Mostrar(pantallaPrograma);
                return;
            }

        }

        bool AvanceSimple(bool disparo)
        {
            return disparo || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
        }

        void ConstruirUI()
        {
            canvas = UIKit.CrearCanvas("Canvas_MaquinaEstados", 10);
            raiz = UIKit.CrearRaiz(canvas.transform, "Flujo_MaquinaEstados");

            var fondo = new GameObject("Fondo_Taller", typeof(RectTransform), typeof(RawImage));
            fondo.transform.SetParent(raiz, false);
            Estirar(fondo.GetComponent<RectTransform>());
            var raw = fondo.GetComponent<RawImage>();
            var textura = Resources.Load<Texture2D>("Fondos/Taller");
            if (textura != null) raw.texture = textura;
            raw.raycastTarget = false;
            veloFondo = UIKit.Rellenar(raiz, 0, 0, UIKit.W, UIKit.H, new Color(0.025f, 0.045f, 0.04f, 0.67f));
            Estirar(veloFondo.rectTransform);

            pantallaByteCarga = NuevaPantalla("S00_Byte_AnunciaDescarga");
            Encabezado(pantallaByteCarga.transform);
            TituloCarga(pantallaByteCarga.transform);
            DialogoByte(pantallaByteCarga.transform);
            Pie(pantallaByteCarga.transform);

            pantallaCarga = NuevaPantalla("S01_ProgresoDescarga");
            Encabezado(pantallaCarga.transform);
            TituloCarga(pantallaCarga.transform);
            var cajaCarga = UIKit.NuevaCaja(pantallaCarga.transform, 250, 650, 960, 78, Paleta.Panel, turquesa, 2);
            UIKit.Texto(cajaCarga.rt, "//000000000000000000000000000000001111111111111111110101//", 26, 0,
                900, 78, 18, Paleta.Crema, TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
            Pie(pantallaCarga.transform);

            pantallaAxel = NuevaPantalla("S02_Axel_MaquinaDeEstados");
            Encabezado(pantallaAxel.transform);
            UIKit.Texto(pantallaAxel.transform, "MAQUINA DE ESTADOS", 74, 165, 1100, 75, 58,
                Paleta.Crema, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            UIKit.Rellenar(pantallaAxel.transform, 80, 245, 140, 5, turquesa);
            DialogoAxel(pantallaAxel.transform,
                "El Bicho me prometió la libertad si te ayudaba con la “Máquina de Estados”. Empecemos con algo simple...", 583,
                () => Mostrar(pantallaDecision));
            Pie(pantallaAxel.transform);

            ConstruirDecision();
            ConstruirHistorial();
            ConstruirSecuencia();
            ConstruirPrograma();
            ConstruirProgramaFinal();
            ActualizarBordes();
        }

        void ConstruirDecision()
        {
            pantallaDecision = NuevaPantalla("S03_PrimeraDecision_Axel");
            Encabezado(pantallaDecision.transform);
            UIKit.Texto(pantallaDecision.transform, "MAQUINA DE ESTADOS / ELEGÍ UNA ACCIÓN", 48, 92, 1100, 32, 18,
                Paleta.Verde, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            CrearOpcion(pantallaDecision.transform, 0, "//Esquivar", recursosEstado[0], 296);
            CrearOpcion(pantallaDecision.transform, 1, "//Saltar", recursosEstado[1], 594);
            CrearOpcion(pantallaDecision.transform, 2, "//Agacharse", recursosEstado[2], 892);
            DialogoAxel(pantallaDecision.transform,
                "Bien... voy a programar un enemigo. ¿Qué acción querés que realice?", 583,
                ConfirmarDecisionConBoton);
            Pie(pantallaDecision.transform);
        }

        void ConstruirHistorial()
        {
            pantallaHistorial = NuevaPantalla("S04_HistorialDeComportamiento");
            Encabezado(pantallaHistorial.transform);
            CajaPanel(pantallaHistorial.transform, 65, 117, 650, 388, new Color(0.39f, 0.58f, 0.68f));
            UIKit.Texto(pantallaHistorial.transform, "//Historial de comportamiento :", 108, 140, 570, 32, 21,
                Paleta.Crema, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            historialDiagrama = UIKit.Rect(pantallaHistorial.transform, "Historial_Diagrama", 0, 0, UIKit.W, UIKit.H);
            CrearDiagramaHistorial(historialDiagrama, 105, 186, 0);

            panelEnemigoHistorial = CajaPanel(pantallaHistorial.transform, 820, 117, 602, 612, Paleta.VerdeBorde);
            var bordeObjetivo = panelEnemigoHistorial.GetComponent<Image>();
            if (bordeObjetivo != null) bordeObjetivo.color = Paleta.Amarillo;
            UIKit.Rellenar(panelEnemigoHistorial, 0, 439, 602, 2, Paleta.VerdeBorde);
            enemigoHistorial = CrearImagenEstado(panelEnemigoHistorial, recursosEstado[opcionElegida], 212, 236, 178, 236);
            imagenEnemigoHistorial = enemigoHistorial != null ? enemigoHistorial.GetComponent<Image>() : null;
            if (enemigoHistorial != null) posicionInicialEnemigoHistorial = enemigoHistorial.anchoredPosition;
            CrearMarcoObjetivoEnemigo(enemigoHistorial);
            CrearIndicacionContinuar(panelEnemigoHistorial);
            animacionEstadoCentral = new Sprite[] {
                Resources.Load<Sprite>("Enemigos/SaltarAnim_00"),
                Resources.Load<Sprite>("Enemigos/SaltarAnim_01"),
                Resources.Load<Sprite>("Enemigos/SaltarAnim_02")
            };
            cuadroAnimacion = 0;
            proximoCuadroAnimacion = Time.unscaledTime + 0.5f;
            if (imagenEnemigoHistorial != null && animacionEstadoCentral[0] != null)
                imagenEnemigoHistorial.sprite = animacionEstadoCentral[0];
            cuerpoHistorial = TextoDialogo(pantallaHistorial.transform,
                "Dispará al enemigo del recuadro derecho para continuar.", false,
                43, 562, 700, 167, 65, 90);
            cajaDialogoHistorialByte = CajaPanel(pantallaHistorial.transform, 43, 530, 727, 121, Paleta.VerdeBorde);
            CajaByteDialogo(cajaDialogoHistorialByte, "¡MÁS DISPAROS!!!", out cuerpoHistorialByte);
            cajaDialogoHistorialByte.gameObject.SetActive(false);
            Pie(pantallaHistorial.transform);
        }

        void CrearMarcoObjetivoEnemigo(RectTransform objetivo)
        {
            if (objetivo == null) return;
            const float margen = 12f, largo = 26f, grosor = 4f;
            float ancho = objetivo.sizeDelta.x + margen * 2f;
            float alto = objetivo.sizeDelta.y + margen * 2f;
            var go = new GameObject("Marco_Objetivo_Enemigo", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(objetivo, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(ancho, alto);

            Color color = Paleta.Amarillo;
            UIKit.Rellenar(rt, 0, 0, largo, grosor, color);
            UIKit.Rellenar(rt, 0, 0, grosor, largo, color);
            UIKit.Rellenar(rt, ancho - largo, 0, largo, grosor, color);
            UIKit.Rellenar(rt, ancho - grosor, 0, grosor, largo, color);
            UIKit.Rellenar(rt, 0, alto - grosor, largo, grosor, color);
            UIKit.Rellenar(rt, 0, alto - largo, grosor, largo, color);
            UIKit.Rellenar(rt, ancho - largo, alto - grosor, largo, grosor, color);
            UIKit.Rellenar(rt, ancho - grosor, alto - largo, grosor, largo, color);
        }

        void CrearIndicacionContinuar(Transform panel)
        {
            // Cartel destacado: informa el objetivo y se transforma en aviso de fallo al errar.
            var caja = UIKit.NuevaCaja(panel, 38, 520, 526, 58,
                new Color(0.055f, 0.085f, 0.07f, 0.96f), Paleta.Amarillo, 2f);
            bordeAvisoHistorial = caja.borde;
            fondoAvisoHistorial = caja.fill;
            indicadorObjetivoHistorial = UIKit.Texto(caja.rt, "DISPARÁ AL ROBOT PARA CONTINUAR", 10, 0, 502, 58, 17,
                Paleta.Amarillo, TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
        }

        void MostrarFalloDisparoHistorial()
        {
            if (indicadorObjetivoHistorial == null) return;
            avisoFalloHistorial = true;
            finAvisoFalloHistorial = Time.unscaledTime + 1.6f;
            indicadorObjetivoHistorial.text = "¡FALLO! APUNTÁ AL ROBOT";
            indicadorObjetivoHistorial.color = Paleta.Naranja;
            if (bordeAvisoHistorial != null) bordeAvisoHistorial.color = Paleta.Naranja;
            if (fondoAvisoHistorial != null) fondoAvisoHistorial.color = new Color(0.16f, 0.075f, 0.055f, 0.98f);
        }

        void ActualizarAvisoFalloHistorial()
        {
            if (!avisoFalloHistorial || Time.unscaledTime < finAvisoFalloHistorial) return;
            avisoFalloHistorial = false;
            if (indicadorObjetivoHistorial == null) return;
            indicadorObjetivoHistorial.text = "DISPARÁ AL ROBOT PARA CONTINUAR";
            indicadorObjetivoHistorial.color = Paleta.Amarillo;
            if (bordeAvisoHistorial != null) bordeAvisoHistorial.color = Paleta.Amarillo;
            if (fondoAvisoHistorial != null) fondoAvisoHistorial.color = new Color(0.055f, 0.085f, 0.07f, 0.96f);
        }

        void ConstruirSecuencia()
        {
            pantallaSecuencia = NuevaPantalla("S05_Minijuego_SecuenciaCorrecta");
            Encabezado(pantallaSecuencia.transform);
            var izquierda = CajaPanel(pantallaSecuencia.transform, 72, 115, 617, 486, new Color(0.39f, 0.58f, 0.68f));
            UIKit.Texto(izquierda, "Secuencia Correcta:", 40, 34, 560, 42, 29, Paleta.Naranja,
                TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            CrearDiagramaEstados(izquierda, 67, 188, new[] { 0, 1, 2 }, 434, 140, 3, Paleta.Naranja);

            var derecha = CajaPanel(pantallaSecuencia.transform, 727, 115, 617, 486, new Color(0.39f, 0.58f, 0.68f));
            CrearCartasObjetivo(derecha);
            cajaDialogoSecuencia = CrearDialogoByte(pantallaSecuencia.transform,
                "Dispará para dar vuelta las cartas. Dejá de cara la secuencia correcta.", 386, 626, 727, 127,
                out cuerpoDialogoSecuencia);
            botonCrearPrograma = CrearBotonAccion(pantallaSecuencia.transform, "CREAR PROGRAMA", 588, 657, 395, 74);
            botonCrearPrograma.gameObject.SetActive(false);
            Pie(pantallaSecuencia.transform);
        }

        void ConstruirPrograma()
        {
            pantallaPrograma = NuevaPantalla("S06_ProgramaDeEstados");
            Encabezado(pantallaPrograma.transform);
            cuadrosPantallaPrograma = new Sprite[] {
                Resources.Load<Sprite>("Enemigos/ProgramaFinalAnim_00"),
                Resources.Load<Sprite>("Enemigos/ProgramaFinalAnim_01"),
                Resources.Load<Sprite>("Enemigos/ProgramaFinalAnim_02")
            };
            var go = new GameObject("Comportamiento_y_Ciclo_Animados", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(pantallaPrograma.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(100, -145);
            rt.sizeDelta = new Vector2(1200, 619);
            imagenEnemigoPrograma = go.GetComponent<Image>();
            imagenEnemigoPrograma.sprite = cuadrosPantallaPrograma[0];
            imagenEnemigoPrograma.preserveAspect = true;
            imagenEnemigoPrograma.raycastTarget = false;
            cuadroPantallaPrograma = 0;
            proximoCuadroPrograma = Time.unscaledTime + 0.5f;
            CrearIconoGuardar(pantallaPrograma.transform, 1015, 255, 42);
            TextoDialogo(pantallaPrograma.transform,
                "Bien! ahora creamos un programa para darle los estados que debe cumplir... o sea una Máquina de Estados!",
                false, 43, 520, 700, 167, 70, 95, () => Mostrar(pantallaProgramaFinal));
            Pie(pantallaPrograma.transform);
        }

        void ConstruirProgramaFinal()
        {
            pantallaProgramaFinal = NuevaPantalla("S07_CicloCompleto_Axel");
            Encabezado(pantallaProgramaFinal.transform);

            var spriteCiclo = Resources.Load<Sprite>("Enemigos/ProgramaFinal_Estatico");
            if (spriteCiclo != null)
            {
                var grafico = UIKit.Rect(pantallaProgramaFinal.transform, "Diagrama_Ciclo_Completo", 145, 144, 900, 239);
                var imagen = grafico.gameObject.AddComponent<Image>();
                imagen.sprite = spriteCiclo;
                imagen.preserveAspect = true;
                imagen.raycastTarget = false;
            }
            else Debug.LogWarning("No se encontró el gráfico estático del ciclo de Máquina de Estados.");

            DialogoAxel(pantallaProgramaFinal.transform,
                "A comparación de antes, el comportamiento no puede repetirse sin completar el ciclo.", 583);
            botonPathfindingFinal = UIKit.Boton(pantallaProgramaFinal.transform, "", "PATHFINDING", 1202, 320, 220, 48,
                IrAInicioPathfinding, KeyCode.None, true).GetComponent<RectTransform>();
            botonIADirectorFinal = UIKit.Boton(pantallaProgramaFinal.transform, "", "IA DIRECTOR", 1202, 385, 220, 48,
                IrAIADirector, KeyCode.None, true).GetComponent<RectTransform>();
            Pie(pantallaProgramaFinal.transform);
        }

        void IrAInicioPathfinding()
        {
            Escena1Intro.AbrirInicioPathfinding();
        }

        void IrAIADirector()
        {
            IntroFlow.AbrirIADirector();
        }

        GameObject NuevaPantalla(string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(raiz, false);
            Estirar(go.GetComponent<RectTransform>());
            return go;
        }

        void CrearOpcion(Transform p, int i, string etiqueta, string recurso, float x)
        {
            var caja = UIKit.NuevaCaja(p, x, 188, 272, 310, new Color(0.055f, 0.085f, 0.07f, 0.94f), turquesa, 2);
            tarjetas[i] = caja.rt;
            gruposTarjeta[i] = caja.rt.gameObject.AddComponent<CanvasGroup>();
            bordes[i] = caja.borde;
            caja.rt.pivot = new Vector2(0.5f, 0.5f);
            caja.rt.anchoredPosition = new Vector2(x + 136, -(188 + 155));
            var sprite = Resources.Load<Sprite>(recurso);
            if (sprite != null)
            {
                var go = new GameObject("Enemigo_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(caja.rt, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -24);
                rt.sizeDelta = new Vector2(112, 150);
                var img = go.GetComponent<Image>(); img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
            }
            UIKit.Texto(caja.rt, etiqueta, 12, 224, 248, 56, 26, Paleta.Crema,
                TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
        }

        void CrearDiagramaHistorial(Transform p, float x, float y, int adicionales)
        {
            int[] estados = { 0, 1, 2, 1, 1, 1, 1, 1 };
            int total = adicionales <= 0 ? 3 : adicionales == 1 ? 4 : 8;
            bool esHistorial = p == historialDiagrama;
            float w = 104, h = 140;
            for (int i = 0; i < total; i++)
            {
                int fila = i / 4;
                int columna = i % 4;
                float paso = esHistorial ? (total == 8 ? 150f : 165f) : 138f;
                float xx = x + columna * paso;
                float yy = y + fila * (h + 24);
                var card = CrearTarjetaEstado(p, recursosEstado[estados[i]], "", xx, yy, w, h,
                    new Color(0.39f, 0.58f, 0.68f));
                card.name = "Tarjeta_Historial";
                if (columna < 3 && i + 1 < total)
                {
                    var flecha = UIKit.Texto(p, "→", xx + w, yy + 49, paso - w, 40, 26, Paleta.Verde,
                        TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
                    flecha.name = "Flecha_Historial";
                }
            }
        }

        void CrearDiagramaHistorialError(Transform p, float x, float y)
        {
            int[] estados = { 0, 1, 1 };
            for (int i = 0; i < estados.Length; i++)
            {
                float xx = x + i * 138f;
                var card = CrearTarjetaEstado(p, recursosEstado[estados[i]], "", xx, y, 104, 140,
                    new Color(0.39f, 0.58f, 0.68f));
                if (i < estados.Length - 1)
                    UIKit.Texto(p, "→", xx + 104, y + 49, 34, 40, 25, Paleta.Verde,
                        TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
                if (i == 2)
                    UIKit.Texto(p, "×", xx + 77, y - 15, 40, 40, 36, Paleta.Naranja,
                        TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
            }
        }

        void CrearCartasObjetivo(Transform p)
        {
            for (int i = 0; i < cartasObjetivo.Length; i++)
            {
                float x = 35 + (i % 4) * 150;
                float y = 65 + (i / 4) * 200;
                var c = UIKit.NuevaCaja(p, x, y, 104, 140,
                    new Color(0.055f, 0.085f, 0.07f, 0.94f), new Color(0.39f, 0.58f, 0.68f), 2);
                cartasObjetivo[i] = c.rt;
                bordesObjetivo[i] = c.borde;
                imagenesReversoCarta[i] = CrearIconoCarta(c.rt, "Diana", SpriteBlanco, 12, 13, 80, 110);
                imagenesFrenteCarta[i] = CrearIconoCarta(c.rt, "Estado", null, 12, 13, 80, 110);
                imagenesFrenteCarta[i].gameObject.SetActive(false);
            }
        }

        Image[] CrearDiagramaEstados(Transform p, float x, float y, int[] estados, float w, float h, int cantidad, Color borde)
        {
            var resultados = new Image[cantidad];
            float cardW = Mathf.Min(104f, (w - 2 * 58f) / cantidad);
            float gap = (w - cantidad * cardW) / (cantidad - 1);
            for (int i = 0; i < cantidad; i++)
            {
                float xx = x + i * (cardW + gap);
                int estado = estados[i];
                var rt = CrearTarjetaEstado(p, recursosEstado[estado], "", xx, y, cardW, h, borde);
                resultados[i] = rt.Find("Imagen_Estado")?.GetComponent<Image>();
                if (i + 1 < cantidad)
                {
                    float ax = xx + cardW;
                    UIKit.Texto(p, "→", ax, y + h * 0.37f, gap, 46, 30,
                        borde, TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
                }
            }
            return resultados;
        }

        void ActualizarAnimacionMuestra()
        {
            if (pantallaHistorial == null || !pantallaHistorial.activeSelf || imagenEnemigoHistorial == null ||
                animacionEstadoCentral == null || animacionEstadoCentral.Length == 0) return;
            if (Time.unscaledTime < proximoCuadroAnimacion) return;
            cuadroAnimacion = (cuadroAnimacion + 1) % animacionEstadoCentral.Length;
            if (animacionEstadoCentral[cuadroAnimacion] != null)
                imagenEnemigoHistorial.sprite = animacionEstadoCentral[cuadroAnimacion];
            proximoCuadroAnimacion = Time.unscaledTime + 0.5f;
        }

        void ActualizarAnimacionPrograma()
        {
            if (pantallaPrograma == null || !pantallaPrograma.activeSelf || imagenEnemigoPrograma == null ||
                cuadrosPantallaPrograma == null || cuadrosPantallaPrograma.Length == 0 ||
                Time.unscaledTime < proximoCuadroPrograma) return;
            cuadroPantallaPrograma = (cuadroPantallaPrograma + 1) % cuadrosPantallaPrograma.Length;
            if (cuadrosPantallaPrograma[cuadroPantallaPrograma] != null)
                imagenEnemigoPrograma.sprite = cuadrosPantallaPrograma[cuadroPantallaPrograma];
            proximoCuadroPrograma = Time.unscaledTime + 0.5f;
        }

        void ActualizarAscensoEnemigo()
        {
            if (enemigoHistorial == null || pantallaHistorial == null || !pantallaHistorial.activeSelf) return;
            Vector2 destino = posicionInicialEnemigoHistorial + new Vector2(0, elevacionObjetivoEnemigoHistorial);
            enemigoHistorial.anchoredPosition = Vector2.MoveTowards(enemigoHistorial.anchoredPosition, destino,
                480f * Time.unscaledDeltaTime);
        }

        Image CrearIconoCarta(Transform p, string nombre, Sprite sprite, float x, float y, float w, float h)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(p, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -y - h * 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            var im = go.GetComponent<Image>();
            im.sprite = sprite;
            im.preserveAspect = true;
            im.raycastTarget = false;
            return im;
        }

        RectTransform CrearTarjetaEstado(Transform p, string recurso, string etiqueta, float x, float y,
            float w, float h, Color borde)
        {
            var c = CajaPanel(p, x, y, w, h, borde);
            CrearImagenEstado(c, recurso, 13, 8, w - 26, h - 50);
            if (!string.IsNullOrEmpty(etiqueta))
                UIKit.Texto(c, etiqueta, 5, h - 37, w - 10, 27, 12,
                    Paleta.Crema, TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
            return c.GetComponent<RectTransform>();
        }

        RectTransform CrearImagenEstado(Transform p, string recurso, float x, float y, float w, float h)
        {
            var sprite = Resources.Load<Sprite>(recurso);
            if (sprite == null) return null;
            var go = new GameObject("Imagen_Estado", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(p, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -y - h * 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            var im = go.GetComponent<Image>(); im.sprite = sprite; im.preserveAspect = true; im.raycastTarget = false;
            return rt;
        }

        static Sprite spriteBlanco;
        static Sprite SpriteBlanco
        {
            get
            {
                if (spriteBlanco != null) return spriteBlanco;
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                var clear = new Color(0, 0, 0, 0);
                var pixels = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = x - (n - 1) * 0.5f, dy = y - (n - 1) * 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        bool cruz = (Mathf.Abs(dx) < 2 && d < 59) || (Mathf.Abs(dy) < 2 && d < 59);
                        bool aro = Mathf.Abs(d - 54) < 4 || Mathf.Abs(d - 39) < 4 || Mathf.Abs(d - 24) < 4 || d < 10;
                        pixels[y * n + x] = (aro || cruz) ? (d < 11 ? Paleta.Amarillo : Paleta.Naranja) : clear;
                    }
                tex.SetPixels(pixels); tex.Apply();
                spriteBlanco = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                return spriteBlanco;
            }
        }

        RectTransform CajaPanel(Transform p, float x, float y, float w, float h, Color borde)
        {
            var caja = UIKit.NuevaCaja(p, x, y, w, h, new Color(0.055f, 0.085f, 0.07f, 0.94f), borde, 2);
            return caja.rt;
        }

        RectTransform CrearBotonAccion(Transform p, string texto, float x, float y, float w, float h)
        {
            var caja = CajaPanel(p, x, y, w, h, Paleta.Amarillo);
            UIKit.Rellenar(caja, 2, 2, w - 4, h - 4, new Color(0.17f, 0.22f, 0.16f, 0.97f));
            CrearIconoGuardar(caja, 45, h * 0.5f, 42);
            UIKit.Texto(caja, texto, 78, 0, w - 92, h, 17, Paleta.Amarillo,
                TextAnchor.MiddleCenter, TMPro.FontStyles.Bold);
            return caja;
        }

        void CrearIconoGuardar(Transform p, float x, float y, float tam)
        {
            var sprite = Resources.Load<Sprite>("IconoGuardar");
            if (sprite == null) return;
            var go = new GameObject("Icono_Guardar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(p, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(tam, tam);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        RectTransform CrearDialogoByte(Transform p, string mensaje, float x, float y, float w, float h,
            out TMPro.TMP_Text cuerpo)
        {
            var caja = CajaPanel(p, x, y, w, h, Paleta.VerdeBorde);
            CajaByteDialogo(caja, mensaje, out cuerpo);
            return caja;
        }

        void CajaByteDialogo(Transform caja, string mensaje, out TMPro.TMP_Text cuerpo)
        {
            UIKit.Byte(caja, 17, 12, 0.82f);
            UIKit.RotuloDialogo(caja, "BYTE / IA CORRUPTA", 120, 10, 570, 25, Paleta.Verde);
            cuerpo = UIKit.CuerpoDialogo(caja, mensaje, 120, 38, 570, 67, Paleta.Crema);
        }

        TMPro.TMP_Text TextoDialogo(Transform p, string mensaje, bool byteHabla, float x = 55, float y = 608,
            float w = 1346, float h = 143, float cuerpoY = 50, float cuerpoH = 73,
            System.Action continuar = null)
        {
            var c = CajaPanel(p, x, y, w, h, byteHabla ? Paleta.Verde : turquesa);
            if (byteHabla) UIKit.Byte(c, 20, 17, 0.86f);
            else
            {
                var foto = Resources.Load<Texture2D>("FotoAxel");
                if (foto != null)
                {
                    var go = new GameObject("Retrato_Axel", typeof(RectTransform), typeof(RawImage));
                    go.transform.SetParent(c, false);
                    var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(18, -13); rt.sizeDelta = new Vector2(106, 112);
                    var image = go.GetComponent<RawImage>(); image.texture = foto; image.raycastTarget = false;
                }
            }
            string autor = byteHabla ? "BYTE / IA CORRUPTA" : "AXEL MUÑOZ / PROGRAMADOR DEL ESTUDIO PAMPEANO ‘ATANOR’";
            float contenidoX = byteHabla ? 145 : 145;
            float anchoRotulo = Mathf.Max(100, w - contenidoX - 24f);
            float anchoCuerpo = Mathf.Max(100, w - contenidoX - (continuar != null ? 160f : 24f));
            UIKit.RotuloDialogo(c, autor, contenidoX, 18, anchoRotulo, 25, byteHabla ? Paleta.Verde : turquesa);
            var cuerpo = UIKit.CuerpoDialogo(c, mensaje, contenidoX, cuerpoY, anchoCuerpo, cuerpoH, Paleta.Crema);
            if (continuar != null)
            {
                cuerpo.fontSize = 18f;
                UIKit.BotonContinuar(c, continuar, w, h);
            }
            return cuerpo;
        }

        void DialogoAxel(Transform p, string mensaje, float y, System.Action continuar = null)
        {
            var caja = UIKit.NuevaCaja(p, 237, y, UIKit.DialogoAncho, UIKit.DialogoAlto, Paleta.Panel, turquesa, 2);
            var foto = Resources.Load<Texture2D>("FotoAxel");
            if (foto != null)
            {
                var go = new GameObject("Retrato_Axel", typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(caja.rt, false);
                var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(18, -18); rt.sizeDelta = new Vector2(87, 85);
                var image = go.GetComponent<RawImage>(); image.texture = foto; image.raycastTarget = false;
            }
            float anchoTexto = continuar == null ? UIKit.DialogoAncho - 148 : UIKit.DialogoAncho - 294;
            UIKit.RotuloDialogo(caja.rt, "AXEL MUÑOZ / PROGRAMADOR DEL ESTUDIO PAMPEANO ‘ATANOR’", 124, 30,
                anchoTexto, 27, turquesa);
            var cuerpo = UIKit.CuerpoDialogo(caja.rt, mensaje, 124, 65, anchoTexto, 82, Paleta.Crema);
            if (pantallaDecision != null && p == pantallaDecision.transform)
                textoDialogoDecision = cuerpo;
            if (continuar != null)
            {
                cuerpo.fontSize = 18f;
                UIKit.BotonContinuar(caja.rt, continuar);
            }
        }

        void ConfirmarDecisionConBoton()
        {
            int opcion = seleccion >= 0 ? seleccion : ultimaSeleccionDecision;
            if (opcion < 0)
            {
                if (textoDialogoDecision != null)
                    textoDialogoDecision.text = "Elegí una acción arriba y después presioná SIGUIENTE.";
                return;
            }
            if (decisionConfirmada) return;
            opcionElegida = opcion;
            decisionConfirmada = true;
            IniciarHistorial();
        }

        void TituloCarga(Transform p)
        {
            UIKit.Texto(p, "MAQUINA DE ESTADOS", 74, 165, 1100, 75, 58, Paleta.Crema,
                TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
            UIKit.Rellenar(p, 80, 245, 140, 5, Paleta.Naranja);
            UIKit.Texto(p, "Descargando Base de Datos de Internet //..........//", 80, 283, 1080, 44,
                28, Paleta.Gris, TextAnchor.MiddleLeft);
        }

        void DialogoByte(Transform p)
        {
            var caja = UIKit.NuevaCaja(p, 237, 583, UIKit.DialogoAncho, UIKit.DialogoAlto, Paleta.Panel, Paleta.Verde, 2);
            var cara = Resources.Load<Sprite>("Byte/Byte_Confundido");
            if (cara != null)
            {
                var go = new GameObject("Retrato_Byte", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(caja.rt, false);
                var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(18, -18); rt.sizeDelta = new Vector2(87, 85);
                var img = go.GetComponent<Image>(); img.sprite = cara; img.preserveAspect = true; img.raycastTarget = false;
            }
            UIKit.RotuloDialogo(caja.rt, "BYTE / Tu guía", 124, 30, UIKit.DialogoAncho - 294, 27, Paleta.Verde);
            var cuerpo = UIKit.CuerpoDialogo(caja.rt, "MAQUINA DE ESTADOS!... Secuestr... TraIGAmos AL EXPErto!", 124, 65,
                UIKit.DialogoAncho - 294, 82, Paleta.Crema);
            cuerpo.fontSize = 18f;
            UIKit.BotonContinuar(caja.rt, () => Mostrar(pantallaCarga));
        }

        void Encabezado(Transform p)
        {
            UIKit.NuevaCaja(p, 37, 28, 1383, 58, new Color(0.07f, 0.11f, 0.09f, 0.94f), Paleta.VerdeBorde, 2);
            UIKit.Texto(p, "SINTÉTIKA / IA EN PROGRAMACIÓN DE VIDEOJUEGOS", 57, 28, 1320, 58,
                15, Paleta.Verde, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
        }

        void Pie(Transform p)
        {
            UIKit.Texto(p, "APUNTÁ PARA ELEGIR · DISPARÁ PARA AVANZAR", 37, 776, 900, 24,
                13, Paleta.Gris, TextAnchor.MiddleLeft, TMPro.FontStyles.Bold);
        }

        Vector2 PuntoMira()
        {
            if (control != null && control.Conectado)
            {
                float x = UIKit.W * 0.5f + control.Yaw * 12f;
                float y = UIKit.H * 0.5f - control.Pitch * 8f;
                return new Vector2(x * Screen.width / UIKit.W, Screen.height - y * Screen.height / UIKit.H);
            }
            return Input.mousePosition;
        }

        static bool Dentro(RectTransform rt, Vector2 punto)
        {
            return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, punto, null);
        }

        void ActualizarSeleccionDecision()
        {
            int nueva = -1;
            Vector2 punto = PuntoMira();
            if (control == null || !control.Conectado)
            {
                if (!Input.mousePresent) return;
            }
            for (int i = 0; i < tarjetas.Length; i++)
                if (Dentro(tarjetas[i], punto)) { nueva = i; break; }
            if (nueva != seleccion)
            {
                seleccion = nueva;
                if (nueva >= 0) ultimaSeleccionDecision = nueva;
                ActualizarBordes();
            }
        }

        void ActualizarSeleccionObjetivo()
        {
            int nueva = -1;
            Vector2 punto = PuntoMira();
            if (control == null || !control.Conectado)
            {
                if (!Input.mousePresent) return;
            }
            for (int i = 0; i < cartasObjetivo.Length; i++)
                if (Dentro(cartasObjetivo[i], punto)) { nueva = i; break; }
            if (nueva != seleccionObjetivo)
            {
                seleccionObjetivo = nueva;
                ActualizarBordesObjetivo();
            }
        }

        void ActualizarBordesObjetivo()
        {
            for (int i = 0; i < bordesObjetivo.Length; i++)
            {
                if (bordesObjetivo[i] == null) continue;
                bordesObjetivo[i].color = cartaCorrecta[i] ? Paleta.Naranja :
                    i == seleccionObjetivo ? Paleta.Amarillo : new Color(0.39f, 0.58f, 0.68f);
            }
        }

        void ActualizarBordes()
        {
            for (int i = 0; i < bordes.Length; i++)
                if (bordes[i] != null) bordes[i].color = i == seleccion ? Paleta.Amarillo : new Color(turquesa.r, turquesa.g, turquesa.b, 0.72f);
        }

        void AnimarTarjetas()
        {
            for (int i = 0; i < tarjetas.Length; i++)
            {
                if (tarjetas[i] == null) continue;
                float escalaObjetivo = seleccion < 0 ? 1f : (seleccion == i ? 1.08f : 0.94f);
                tarjetas[i].localScale = Vector3.Lerp(tarjetas[i].localScale, Vector3.one * escalaObjetivo,
                    Time.unscaledDeltaTime * 12f);
                if (gruposTarjeta[i] != null)
                    gruposTarjeta[i].alpha = Mathf.Lerp(gruposTarjeta[i].alpha,
                        seleccion < 0 || seleccion == i ? 1f : 0.68f, Time.unscaledDeltaTime * 12f);
            }
        }

        void IniciarHistorial()
        {
            disparosHistorial = 0;
            cuerpoHistorial.text = "Dispará al enemigo del recuadro derecho para continuar.";
            cajaDialogoHistorialByte.gameObject.SetActive(false);
            if (imagenEnemigoHistorial != null)
                imagenEnemigoHistorial.sprite = animacionEstadoCentral[0];
            if (enemigoHistorial != null) enemigoHistorial.anchoredPosition = posicionInicialEnemigoHistorial;
            elevacionObjetivoEnemigoHistorial = 0;
            cuadroAnimacion = 0;
            proximoCuadroAnimacion = Time.unscaledTime + 0.5f;
            LimpiarDiagramaHistorial();
            CrearDiagramaHistorial(historialDiagrama, 105, 186, 0);
            Mostrar(pantallaHistorial);
        }

        void RegistrarDisparo()
        {
            avisoFalloHistorial = false;
            if (indicadorObjetivoHistorial != null)
            {
                indicadorObjetivoHistorial.text = "DISPARÁ AL ROBOT PARA CONTINUAR";
                indicadorObjetivoHistorial.color = Paleta.Amarillo;
            }
            if (bordeAvisoHistorial != null) bordeAvisoHistorial.color = Paleta.Amarillo;
            if (fondoAvisoHistorial != null) fondoAvisoHistorial.color = new Color(0.055f, 0.085f, 0.07f, 0.96f);
            disparosHistorial++;
            // El robot sube un poco con cada impacto, pero permanece dentro del panel
            // para que los tres disparos necesarios puedan apuntarse a su sprite.
            elevacionObjetivoEnemigoHistorial += 100f;
            if (disparosHistorial >= 3)
            {
                PrepararMinijuego();
                Mostrar(pantallaSecuencia);
                return;
            }
            LimpiarDiagramaHistorial();
            CrearDiagramaHistorial(historialDiagrama, 105, 186, disparosHistorial);
            cajaDialogoHistorialByte.gameObject.SetActive(true);
            cuerpoHistorialByte.text = disparosHistorial == 1 ? "¡MÁS DISPAROS!!!" : "oh...";
            cuerpoHistorial.text = "¡Funciona! El programa va registrando cada cambio de comportamiento...";
        }

        void LimpiarDiagramaHistorial()
        {
            var encontrados = historialDiagrama.GetComponentsInChildren<Transform>(true);
            foreach (var t in encontrados)
                if (t != historialDiagrama && (t.name == "Tarjeta_Historial" || t.name == "Flecha_Historial"))
                    Destroy(t.gameObject);
        }

        void PrepararMinijuego()
        {
            int[] mezcla = new int[cartasObjetivo.Length];
            mezcla[0] = 0; mezcla[1] = 1; mezcla[2] = 2;
            for (int i = 3; i < mezcla.Length; i++) mezcla[i] = Random.Range(0, 3);
            for (int i = mezcla.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = mezcla[i]; mezcla[i] = mezcla[j]; mezcla[j] = temp;
            }
            estadosCorrectosDeCara = 0;
            for (int i = 0; i < cartasObjetivo.Length; i++)
            {
                estadoCarta[i] = mezcla[i];
                cartaDeCara[i] = false;
                cartaCorrecta[i] = false;
                cartasObjetivo[i].gameObject.SetActive(true);
                imagenesFrenteCarta[i].sprite = Resources.Load<Sprite>(recursosEstado[estadoCarta[i]]);
                imagenesFrenteCarta[i].gameObject.SetActive(false);
                imagenesReversoCarta[i].gameObject.SetActive(true);
            }
            for (int i = 0; i < estadoNecesarioDeCara.Length; i++) estadoNecesarioDeCara[i] = false;
            seleccionObjetivo = -1;
            botonCrearPrograma.gameObject.SetActive(false);
            cajaDialogoSecuencia.gameObject.SetActive(true);
            cuerpoDialogoSecuencia.text = "Dispará para dar vuelta las cartas. Dejá de cara la secuencia correcta.";
            ActualizarBordesObjetivo();
        }

        void VoltearCarta(int i)
        {
            cartaDeCara[i] = !cartaDeCara[i];
            if (cartaDeCara[i])
            {
                imagenesReversoCarta[i].gameObject.SetActive(false);
                imagenesFrenteCarta[i].gameObject.SetActive(true);
                int estado = estadoCarta[i];
                cartaCorrecta[i] = !estadoNecesarioDeCara[estado];
                if (cartaCorrecta[i])
                {
                    estadoNecesarioDeCara[estado] = true;
                    estadosCorrectosDeCara++;
                    cuerpoDialogoSecuencia.text = "¡Bien! Encontraste un estado de la secuencia. Buscá los que faltan.";
                }
                else cuerpoDialogoSecuencia.text = "Ese estado ya está de cara. Volvé a ocultar esta carta con otro disparo.";
            }
            else
            {
                if (cartaCorrecta[i])
                {
                    estadoNecesarioDeCara[estadoCarta[i]] = false;
                    estadosCorrectosDeCara--;
                }
                cartaCorrecta[i] = false;
                imagenesFrenteCarta[i].gameObject.SetActive(false);
                imagenesReversoCarta[i].gameObject.SetActive(true);
                cuerpoDialogoSecuencia.text = "La carta volvió a quedar oculta. Encontrá un ejemplar de cada estado.";
            }

            if (MinijuegoResuelto())
            {
                cuerpoDialogoSecuencia.text = "¡Secuencia correcta! Ya podés crear el programa.";
                cajaDialogoSecuencia.gameObject.SetActive(false);
                botonCrearPrograma.gameObject.SetActive(true);
                botonCrearPrograma.SetAsLastSibling();
            }
            else
            {
                cajaDialogoSecuencia.gameObject.SetActive(true);
                botonCrearPrograma.gameObject.SetActive(false);
            }
            ActualizarBordesObjetivo();
        }

        bool MinijuegoResuelto()
        {
            if (estadosCorrectosDeCara != 3) return false;
            for (int i = 0; i < cartaDeCara.Length; i++)
                if (cartaDeCara[i] && !cartaCorrecta[i]) return false;
            return true;
        }

        string NombreEstado(int estado)
        {
            return estado == 1 ? nombresAccion[opcionElegida] : nombresEstado[estado];
        }

        bool ActivarBoton(RectTransform rt, bool disparo)
        {
            if (rt == null) return false;
            bool mouse = Input.GetMouseButtonDown(0) && Dentro(rt, PuntoMira());
            bool teclado = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            bool pistolaSobreBoton = disparo && Dentro(rt, PuntoMira());
            return mouse || teclado || pistolaSobreBoton;
        }

        void Mostrar(GameObject activo)
        {
            if (veloFondo != null)
            {
                float exposicion = activo == pantallaAxel ? 0.53f : 0.67f;
                veloFondo.color = new Color(0.025f, 0.045f, 0.04f, exposicion);
            }
            pantallaByteCarga.SetActive(activo == pantallaByteCarga);
            pantallaCarga.SetActive(activo == pantallaCarga);
            pantallaAxel.SetActive(activo == pantallaAxel);
            pantallaDecision.SetActive(activo == pantallaDecision);
            pantallaHistorial.SetActive(activo == pantallaHistorial);
            pantallaSecuencia.SetActive(activo == pantallaSecuencia);
            pantallaPrograma.SetActive(activo == pantallaPrograma);
            pantallaProgramaFinal.SetActive(activo == pantallaProgramaFinal);
        }

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

    }
}
