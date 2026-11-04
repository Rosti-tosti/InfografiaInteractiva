using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Pathfinding
{
    /// <summary>
    /// Laberinto 2D con vista en primera persona simulada por raycasting.
    /// La pistola orienta la vista y el pedal del piso confirma cada paso.
    /// </summary>
    public class Escena2Laberinto : EscenaBase
    {
        const int VistaAncho = 400, VistaAlto = 225;
        const float Fov = 66f;
        const float ConoVistaGrados = 45f;
        const float UmbralSeleccionBifurcacion = 8f;
        static readonly Vector2Int[] Direcciones = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        [Header("Pistola arcade")]
        [Tooltip("Activar cuando el Arduino esté enviando datos por USB serial.")]
        public bool usarArduino = true;
        public string puertoArduino = "COM3";
        public int baudArduino = 115200;
        [Header("Control OSC desde el celular")]
        public bool usarOSC;
        public int puertoOSC = 9000;
        public string ipTelefonoOSC = "";
        [Range(5f, 40f)] public float suavizadoMira = 22f;

        Texture2D textura;
        Texture2D texturaBoton;
        Texture2D texturaMeta;
        Color32[] pixelesMeta;
        Sprite spritePisada;
        Color32[] pixeles;
        float[] profundidad;
        RawImage vista;
        RawImage botonAvanzar;
        GameObject miraHUD;
        Image iconoPisada;
        ArcadeGunInput control;
        TMP_Text txtPasos, txtSeccion, txtPie, txtEleccion, txtAccion;
        Image opcionIzquierda, opcionDerecha;
        TMP_Text txtOpcionIzquierda, txtOpcionDerecha;
        RectTransform rtOpcionIzquierda, rtOpcionDerecha;
        bool seleccionarIzquierda, seleccionarDerecha;
        Transform dialogoRaiz;
        string dialogoActual = "";
        readonly List<Vector2Int> recorrido = new List<Vector2Int>();
        readonly HashSet<string> hechos = new HashSet<string>();
        readonly HashSet<Vector2Int> bifurcacionesResueltas = new HashSet<Vector2Int>();
        Vector2Int celdaActual;
        float yawOffset, yawJugador;
        bool bloqueado, moviendo, terminado, decisionActiva, girando;

        protected override void Awake()
        {
            base.Awake();
            var fondo = canvas.transform.Find("FondoRaiz");
            if (fondo != null) fondo.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Start()
        {
            textura = new Texture2D(VistaAncho, VistaAlto, TextureFormat.RGB24, false);
            textura.filterMode = FilterMode.Point;
            textura.wrapMode = TextureWrapMode.Clamp;
            var spriteMeta = Resources.Load<Sprite>("MetaBandera");
            texturaMeta = spriteMeta != null ? spriteMeta.texture : Resources.Load<Texture2D>("MetaBandera");
            if (texturaMeta != null) pixelesMeta = texturaMeta.GetPixels32();
            pixeles = new Color32[VistaAncho * VistaAlto];
            profundidad = new float[VistaAncho];

            var fondo = new GameObject("Vista 2D / Raycasting", typeof(RectTransform));
            fondo.transform.SetParent(canvas.transform, false);
            var rt = fondo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            vista = fondo.AddComponent<RawImage>();
            vista.texture = textura; vista.raycastTarget = false;

            control = GetComponent<ArcadeGunInput>();
            if (control == null) control = gameObject.AddComponent<ArcadeGunInput>();
            control.usarArduino = usarArduino;
            control.puerto = puertoArduino;
            control.baudRate = baudArduino;
            control.usarOSC = usarOSC;
            control.puertoOSC = puertoOSC;
            control.ipTelefonoOSC = ipTelefonoOSC;
            control.suavizadoMira = suavizadoMira;

            CrearHud();
            celdaActual = Laberinto.A;
            recorrido.Add(celdaActual);
            Dialogo("entrada");
        }

        void CrearHud()
        {
            var hud = UIKit.CrearRaiz(canvas.transform, "HUD");
            var c = UIKit.NuevaCaja(hud, 37, 28, 1383, 58, new Color(0.07f, 0.11f, 0.09f, 0.88f), Paleta.VerdeBorde, 2f);
            UIKit.Texto(c.rt, "SINTÉTIKA / IA EN JUEGO", 22, 0, 600, 58, 17, Paleta.Verde, TextAnchor.MiddleLeft, FontStyles.Bold);
            txtSeccion = UIKit.Texto(c.rt, "LABERINTO 2D / PRIMERA PERSONA", 660, 0, 700, 58, 17, Paleta.Crema, TextAnchor.MiddleRight, FontStyles.Bold);

            var cp = UIKit.NuevaCaja(hud, 1180, 98, 240, 44, new Color(0.07f, 0.11f, 0.09f, 0.88f), Paleta.Amarillo, 2f);
            txtPasos = UIKit.Texto(cp.rt, "PASOS  00", 0, 0, 240, 44, 20, Paleta.Amarillo, TextAnchor.MiddleCenter, FontStyles.Bold);
            var mira = new GameObject("Mira_HUD", typeof(RectTransform));
            mira.transform.SetParent(hud, false);
            var rtMira = mira.GetComponent<RectTransform>();
            rtMira.anchorMin = Vector2.zero; rtMira.anchorMax = Vector2.one;
            rtMira.offsetMin = Vector2.zero; rtMira.offsetMax = Vector2.zero;
            miraHUD = mira;
            UIKit.Mira(mira.transform, new Color(0.60f, 0.74f, 0.53f, 0.95f));
            txtEleccion = UIKit.Texto(hud, "", 380, 610, 696, 54, 22, Paleta.Amarillo, TextAnchor.MiddleCenter, FontStyles.Bold);
            var izquierda = UIKit.NuevaCaja(hud, 72, 346, 330, 96, new Color(0.06f, 0.09f, 0.07f, 0.94f), Paleta.VerdeBorde, 3f);
            opcionIzquierda = izquierda.fill;
            rtOpcionIzquierda = izquierda.rt;
            rtOpcionIzquierda.pivot = new Vector2(0.5f, 0.5f);
            rtOpcionIzquierda.anchoredPosition += new Vector2(165f, -48f);
            // Usar glifos ASCII evita que TMP muestre cuadros vacíos si la fuente no
            // incluye los triángulos Unicode de las versiones anteriores.
            txtOpcionIzquierda = UIKit.Texto(izquierda.rt, "<  IZQUIERDA", 8, 0, 310, 88, 22, Paleta.Gris, TextAnchor.MiddleCenter, FontStyles.Bold);
            var derecha = UIKit.NuevaCaja(hud, 1054, 346, 330, 96, new Color(0.06f, 0.09f, 0.07f, 0.94f), Paleta.VerdeBorde, 3f);
            opcionDerecha = derecha.fill;
            rtOpcionDerecha = derecha.rt;
            rtOpcionDerecha.pivot = new Vector2(0.5f, 0.5f);
            rtOpcionDerecha.anchoredPosition += new Vector2(165f, -48f);
            txtOpcionDerecha = UIKit.Texto(derecha.rt, "DERECHA  >", 8, 0, 310, 88, 22, Paleta.Gris, TextAnchor.MiddleCenter, FontStyles.Bold);
            izquierda.rt.gameObject.SetActive(false); derecha.rt.gameObject.SetActive(false);
            txtPie = UIKit.Texto(hud, "VISTA DENTRO DE 45° · PISÁ PARA AVANZAR", 37, 776, 960, 20, 13, Paleta.Gris, TextAnchor.MiddleLeft, FontStyles.Bold);
            CrearBotonAvanzar(hud);
            ActualizarPasos();
        }

        void CrearBotonAvanzar(Transform hud)
        {
            texturaBoton = CrearTexturaBoton();
            var rt = UIKit.Rect(hud, "Botón Avanzar", 1060, 735, 360, 64);
            botonAvanzar = rt.gameObject.AddComponent<RawImage>();
            botonAvanzar.texture = texturaBoton; botonAvanzar.raycastTarget = false;

            spritePisada = Resources.Load<Sprite>("IconoAvanzar");
            var pie = UIKit.Rect(hud, "Icono de avance", 1080, 749, 22, 35);
            iconoPisada = pie.gameObject.AddComponent<Image>();
            iconoPisada.sprite = spritePisada;
            iconoPisada.preserveAspect = true;
            iconoPisada.raycastTarget = false;

            txtAccion = UIKit.Texto(hud, "Avanzar", 1115, 735, 290, 64, 18, Paleta.Amarillo, TextAnchor.MiddleCenter, FontStyles.Bold);
            if (spritePisada == null)
            {
                iconoPisada.gameObject.SetActive(false);
                txtAccion.text = "PISÁ PARA AVANZAR";
            }
        }

        Texture2D CrearTexturaBoton()
        {
            const int w = 360, h = 64, corte = 12;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pix = new Color32[w * h];
            Color32 borde = Paleta.Amarillo, relleno = new Color(0.06f, 0.09f, 0.07f, 0.96f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int arriba = h - 1 - y, derecha = w - 1 - x;
                    bool fuera = (x < corte && y < corte && x + y < corte) ||
                                 (x < corte && arriba < corte && x + arriba < corte) ||
                                 (derecha < corte && y < corte && derecha + y < corte) ||
                                 (derecha < corte && arriba < corte && derecha + arriba < corte);
                    if (fuera) { pix[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                    bool diagonal = (x < corte && y < corte && x + y < corte + 2) ||
                                   (x < corte && arriba < corte && x + arriba < corte + 2) ||
                                   (derecha < corte && y < corte && derecha + y < corte + 2) ||
                                   (derecha < corte && arriba < corte && derecha + arriba < corte + 2);
                    bool marco = x < 2 || x >= w - 2 || y < 2 || y >= h - 2 || diagonal;
                    pix[y * w + x] = marco ? borde : relleno;
                }
            t.SetPixels32(pix); t.Apply(false, true); t.filterMode = FilterMode.Point;
            return t;
        }

        Vector2 DesviacionVista
        {
            get
            {
                float yawLibre = yawOffset + (control != null ? control.Yaw : 0f);
                float desviacionYaw = Mathf.DeltaAngle(yawJugador, yawLibre);
                float desviacionPitch = control != null ? control.Pitch : 0f;
                Vector2 desviacion = new Vector2(desviacionYaw, desviacionPitch);
                if (desviacion.sqrMagnitude > ConoVistaGrados * ConoVistaGrados)
                    desviacion = desviacion.normalized * ConoVistaGrados;
                return desviacion;
            }
        }
        float Yaw { get { return yawJugador + DesviacionVista.x; } }
        float Pitch { get { return DesviacionVista.y; } }
        float DesviacionMiraBifurcacion
        {
            get
            {
                float yawLibre = yawOffset + (control != null ? control.Yaw : 0f);
                return Mathf.DeltaAngle(yawJugador, yawLibre);
            }
        }

        void Update()
        {
            if (textura == null) return;
            DibujarVista();
            AnimarOpcionesBifurcacion();

            if (bloqueado && !girando && ShootButton.Procesar()) return;

            bool disparo = control != null && control.TomarClickPistola();

            if (terminado)
            {
                // El clic de mouse funciona como disparo de desarrollo; el botón serial
                // del Arduino permite avanzar con el gatillo de la pistola arcade.
                if (ShootButton.Procesar()) return;
                if (Input.GetMouseButtonDown(0) || disparo) IrAEscena("Escena3");
                return;
            }

            if (decisionActiva)
            {
                ActualizarIndicacion();
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || disparo) ConfirmarEleccion();
                return;
            }

            if (bloqueado)
            {
                if (!girando && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetMouseButtonDown(0) || disparo))
                {
                    if (terminado) return;
                    CerrarDialogo();
                }
                return;
            }
            if (moviendo || terminado) return;

            ActualizarIndicacion();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || disparo) AvanzarPaso();
        }

        void DibujarVista()
        {
            bool vistaCentrada = decisionActiva || bloqueado || moviendo;
            float pitchVista = vistaCentrada ? 0f : Pitch;
            float horizonte = Mathf.Clamp(VistaAlto * 0.5f + pitchVista * 0.55f, VistaAlto * 0.28f, VistaAlto * 0.72f);
            for (int y = 0; y < VistaAlto; y++)
            {
                float t = Mathf.Abs(y - horizonte) / VistaAlto;
                Color cielo = new Color(0.025f + t * 0.025f, 0.045f + t * 0.04f, 0.04f + t * 0.025f);
                for (int x = 0; x < VistaAncho; x++) pixeles[y * VistaAncho + x] = cielo;
            }

            // En pausas, diálogos y durante cada paso, la vista queda centrada.
            float a = (vistaCentrada ? yawJugador : Yaw) * Mathf.Deg2Rad;
            float dirX = Mathf.Cos(a), dirY = Mathf.Sin(a);
            float plano = Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
            Vector2 pos = new Vector2(celdaActual.x + 0.5f, celdaActual.y + 0.5f);

            for (int x = 0; x < VistaAncho; x++)
            {
                float camX = 2f * x / VistaAncho - 1f;
                float rayX = dirX - dirY * camX * plano;
                float rayY = dirY + dirX * camX * plano;
                int mapX = Mathf.FloorToInt(pos.x), mapY = Mathf.FloorToInt(pos.y);
                float deltaX = Mathf.Abs(1f / (Mathf.Abs(rayX) < 0.0001f ? 0.0001f : rayX));
                float deltaY = Mathf.Abs(1f / (Mathf.Abs(rayY) < 0.0001f ? 0.0001f : rayY));
                int stepX = rayX < 0 ? -1 : 1, stepY = rayY < 0 ? -1 : 1;
                float sideX = (rayX < 0 ? pos.x - mapX : mapX + 1f - pos.x) * deltaX;
                float sideY = (rayY < 0 ? pos.y - mapY : mapY + 1f - pos.y) * deltaY;
                int lado = 0, guardia = 0;
                while (guardia++ < 32 && !Laberinto.Muro(mapX, mapY))
                {
                    if (sideX < sideY) { sideX += deltaX; mapX += stepX; lado = 0; }
                    else { sideY += deltaY; mapY += stepY; lado = 1; }
                }
                float distancia = Mathf.Max(0.18f, lado == 0 ? sideX - deltaX : sideY - deltaY);
                profundidad[x] = distancia;
                int altoMuro = Mathf.Min(VistaAlto * 2, Mathf.RoundToInt(VistaAlto / distancia));
                int arriba = Mathf.Clamp(Mathf.RoundToInt(horizonte + altoMuro * 0.5f), 0, VistaAlto - 1);
                int abajo = Mathf.Clamp(Mathf.RoundToInt(horizonte - altoMuro * 0.5f), 0, VistaAlto - 1);
                float luz = Mathf.Clamp(1.15f / (1f + distancia * 0.16f), 0.22f, 0.88f) * (lado == 1 ? 0.78f : 1f);
                Color pared = new Color(0.30f * luz, 0.38f * luz, 0.30f * luz);
                for (int y = abajo; y <= arriba; y++)
                {
                    bool franja = ((x + mapX * 13) % 47) < 2;
                    Color color = franja ? new Color(0.44f, 0.34f, 0.19f) * luz : pared;
                    pixeles[y * VistaAncho + x] = color;
                }
            }
            DibujarMeta(pos, dirX, dirY, horizonte, plano);
            textura.SetPixels32(pixeles);
            textura.Apply(false, false);
        }

        void DibujarMeta(Vector2 pos, float dirX, float dirY, float horizonte, float plano)
        {
            if (texturaMeta == null || pixelesMeta == null || pixelesMeta.Length == 0) return;
            Vector2 haciaMeta = new Vector2(Laberinto.B.x + 0.5f - pos.x, Laberinto.B.y + 0.5f - pos.y);
            Vector2 adelante = new Vector2(dirX, dirY);
            Vector2 derecha = new Vector2(-dirY, dirX);
            float distancia = Vector2.Dot(haciaMeta, adelante);
            if (distancia <= 0.25f) return;
            float lateral = Vector2.Dot(haciaMeta, derecha);
            float normalizado = lateral / distancia;
            if (Mathf.Abs(normalizado) > plano) return;

            float centroX = VistaAncho * 0.5f + normalizado / (2f * plano) * VistaAncho;
            float alto = Mathf.Clamp(VistaAlto * 0.72f / distancia, 12f, VistaAlto * 1.4f);
            float ancho = alto * ((float)texturaMeta.width / texturaMeta.height);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(centroX - ancho * 0.5f));
            int x1 = Mathf.Min(VistaAncho - 1, Mathf.CeilToInt(centroX + ancho * 0.5f));
            int y0 = Mathf.Clamp(Mathf.RoundToInt(horizonte - 2f), 0, VistaAlto - 1);
            int y1 = Mathf.Min(VistaAlto - 1, y0 + Mathf.RoundToInt(alto));

            for (int x = x0; x <= x1; x++)
            {
                if (distancia >= profundidad[x]) continue;
                float u = Mathf.Clamp01((x - (centroX - ancho * 0.5f)) / Mathf.Max(1f, ancho));
                int tx = Mathf.Clamp(Mathf.FloorToInt(u * texturaMeta.width), 0, texturaMeta.width - 1);
                for (int y = y0; y <= y1; y++)
                {
                    float v = Mathf.Clamp01((float)(y - y0) / Mathf.Max(1, y1 - y0));
                    int ty = Mathf.Clamp(Mathf.FloorToInt(v * texturaMeta.height), 0, texturaMeta.height - 1);
                    Color32 c = pixelesMeta[ty * texturaMeta.width + tx];
                    if (c.a == 0) continue;
                    int indice = y * VistaAncho + x;
                    if (c.a == 255) pixeles[indice] = c;
                    else
                    {
                        Color fondo = pixeles[indice];
                        pixeles[indice] = Color.Lerp(fondo, c, c.a / 255f);
                    }
                }
            }
        }

        void ActualizarIndicacion()
        {
            if (decisionActiva)
            {
                // La elección usa el giro bruto de la mira; la vista dibujada sigue
                // centrada y respetando el cono de 45°.
                float diferencia = DesviacionMiraBifurcacion;
                seleccionarIzquierda = diferencia < -UmbralSeleccionBifurcacion;
                seleccionarDerecha = diferencia > UmbralSeleccionBifurcacion;
                opcionIzquierda.transform.parent.gameObject.SetActive(true);
                opcionDerecha.transform.parent.gameObject.SetActive(true);
                opcionIzquierda.color = seleccionarIzquierda ? Paleta.Amarillo : new Color(0.06f, 0.09f, 0.07f, 0.94f);
                opcionDerecha.color = seleccionarDerecha ? Paleta.Amarillo : new Color(0.06f, 0.09f, 0.07f, 0.94f);
                txtOpcionIzquierda.color = seleccionarIzquierda ? Paleta.Oscuro : Paleta.Gris;
                txtOpcionDerecha.color = seleccionarDerecha ? Paleta.Oscuro : Paleta.Gris;
                txtEleccion.text = seleccionarIzquierda ? "IZQUIERDA SELECCIONADA · PISÁ LA PLACA" : seleccionarDerecha ? "DERECHA SELECCIONADA · PISÁ LA PLACA" : "APUNTÁ A IZQUIERDA O DERECHA";
            }
            else
            {
                seleccionarIzquierda = seleccionarDerecha = false;
                txtEleccion.text = "";
                opcionIzquierda.transform.parent.gameObject.SetActive(false);
                opcionDerecha.transform.parent.gameObject.SetActive(false);
            }
        }

        void AnimarOpcionesBifurcacion()
        {
            float escalaIzquierda = decisionActiva ? (seleccionarIzquierda ? 1.08f : seleccionarDerecha ? 0.94f : 1f) : 1f;
            float escalaDerecha = decisionActiva ? (seleccionarDerecha ? 1.08f : seleccionarIzquierda ? 0.94f : 1f) : 1f;
            float velocidad = Time.unscaledDeltaTime * 12f;
            if (rtOpcionIzquierda != null)
                rtOpcionIzquierda.localScale = Vector3.Lerp(rtOpcionIzquierda.localScale, Vector3.one * escalaIzquierda, velocidad);
            if (rtOpcionDerecha != null)
                rtOpcionDerecha.localScale = Vector3.Lerp(rtOpcionDerecha.localScale, Vector3.one * escalaDerecha, velocidad);
        }

        Vector2Int DireccionApuntada()
        {
            int cuarto = Mathf.RoundToInt(Yaw / 90f);
            switch (((cuarto % 4) + 4) % 4)
            {
                case 0: return Vector2Int.right;
                case 1: return Vector2Int.up;
                case 2: return Vector2Int.left;
                default: return Vector2Int.down;
            }
        }

        void AvanzarPaso()
        {
            if (decisionActiva || bloqueado || moviendo || terminado) return;
            Vector2Int delta = DireccionApuntada();
            Vector2Int siguiente = celdaActual + delta;
            if (Laberinto.Muro(siguiente))
            {
                txtPie.text = "NO HAY PASO EN ESA DIRECCIÓN · GIRÁ LA PISTOLA";
                return;
            }
            txtPie.text = "CENTRANDO VISTA...";
            StartCoroutine(MoverACelda(siguiente, delta));
        }

        System.Collections.IEnumerator MoverACelda(Vector2Int destino, Vector2Int delta, bool animarGiroInicial = false)
        {
            moviendo = true;
            Vector2Int anterior = celdaActual;
            float anguloDeAvance = AnguloDe(delta);
            if (animarGiroInicial)
                yield return StartCoroutine(GirarGradual(anguloDeAvance, DuracionGiro(anguloDeAvance)));
            // Un paso físico corresponde a una casilla. Se anima en pantalla al confirmar.
            float duracion = 0.18f, transcurrido = 0f;
            while (transcurrido < duracion)
            {
                transcurrido += Time.deltaTime;
                yield return null;
            }
            celdaActual = destino;
            recorrido.Add(celdaActual);
            // La orientación de la pistola queda relativa a la nueva dirección del jugador.
            Vector2Int rumboCentrado = delta;
            Vector2Int giroForzado;
            if (EncontrarSalidaUnica(anterior, celdaActual, out giroForzado))
                rumboCentrado = giroForzado;
            float anguloNuevo = AnguloDe(rumboCentrado);
            if (Mathf.Abs(Mathf.DeltaAngle(yawJugador, anguloNuevo)) > 1f)
                yield return StartCoroutine(GirarGradual(anguloNuevo, DuracionGiro(anguloNuevo)));
            else
                CentrarOrientacion(anguloNuevo);
            ActualizarPasos();
            moviendo = false;
            txtPie.text = "VISTA CENTRADA · PISÁ PARA AVANZAR";
            AlEntrar(anterior, celdaActual);
        }

        bool EncontrarSalidaUnica(Vector2Int celdaAnterior, Vector2Int actual, out Vector2Int salida)
        {
            salida = Vector2Int.zero;
            int cantidad = 0;
            for (int i = 0; i < Direcciones.Length; i++)
            {
                Vector2Int candidata = actual + Direcciones[i];
                if (candidata == celdaAnterior || Laberinto.Muro(candidata)) continue;
                salida = Direcciones[i];
                cantidad++;
            }
            return cantidad == 1;
        }

        static float AnguloDe(Vector2Int direccion)
        {
            if (direccion == Vector2Int.right) return 0f;
            if (direccion == Vector2Int.up) return 90f;
            if (direccion == Vector2Int.left) return 180f;
            return 270f;
        }

        float DuracionGiro(float anguloObjetivo)
        {
            // La rotación se reparte en tres tramos suaves; una media vuelta tarda más.
            float grados = Mathf.Abs(Mathf.DeltaAngle(yawJugador, anguloObjetivo));
            return Mathf.Lerp(1.2f, 2f, Mathf.Clamp01(grados / 180f));
        }

        System.Collections.IEnumerator GirarGradual(float anguloObjetivo, float duracion)
        {
            girando = true;
            if (control != null) control.CentrarMira();
            float inicio = yawJugador;
            float diferencia = Mathf.DeltaAngle(inicio, anguloObjetivo);
            float transcurrido = 0f;
            while (transcurrido < duracion)
            {
                transcurrido += Time.deltaTime;
                float progreso = Mathf.Clamp01(transcurrido / duracion);
                // Recorrido suave de inicio a fin, distribuido en tres fases visuales.
                float progresoSuave = Mathf.SmoothStep(0f, 1f, progreso);
                CentrarOrientacion(inicio + diferencia * progresoSuave);
                yield return null;
            }
            CentrarOrientacion(anguloObjetivo);
            girando = false;
        }

        void CentrarOrientacion(float angulo)
        {
            yawJugador = Mathf.Repeat(angulo, 360f);
            yawOffset = yawJugador - (control != null ? control.Yaw : 0f);
        }

        void ActualizarPasos() { if (txtPasos != null) txtPasos.text = "PASOS  " + Mathf.Max(0, recorrido.Count - 1).ToString("00"); }

        void AlEntrar(Vector2Int anterior, Vector2Int actual)
        {
            bool bifurcacion = actual == new Vector2Int(3, 3) || actual == new Vector2Int(5, 3);
            if (!bifurcacion)
            {
                decisionActiva = false;
                txtEleccion.text = "";
                opcionIzquierda.transform.parent.gameObject.SetActive(false);
                opcionDerecha.transform.parent.gameObject.SetActive(false);
            }
            if (bifurcacion)
            {
                // La elección es obligatoria la primera vez; al volver, el jugador puede
                // orientar la pistola y seguir por el camino que ya conoce.
                if (!bifurcacionesResueltas.Contains(actual)) IniciarEleccion();
                return;
            }
            if (actual == new Vector2Int(4, 3) && hechos.Add("rival")) { Dialogo("rival"); return; }
            if (EsCallejonSinSalida(actual) && hechos.Add("callejon" + actual)) { Dialogo("callejon"); return; }
            if (actual == Laberinto.B) FinalizarApartado();
        }

        bool EsCallejonSinSalida(Vector2Int celda)
        {
            if (celda == Laberinto.A || celda == Laberinto.B) return false;
            int salidas = 0;
            for (int i = 0; i < Direcciones.Length; i++)
                if (!Laberinto.Muro(celda + Direcciones[i])) salidas++;
            return salidas == 1;
        }

        void IniciarEleccion()
        {
            // La elección solo existe mientras el jugador ocupa una de estas dos casillas.
            decisionActiva = true;
            bloqueado = false;
            txtSeccion.text = "LABERINTO 2D / ELEGÍ TU CAMINO";
            txtPie.text = "APUNTÁ A IZQUIERDA O DERECHA";
            txtAccion.text = "Avanzar";
            txtEleccion.text = "APUNTÁ A IZQUIERDA O DERECHA";
            opcionIzquierda.transform.parent.gameObject.SetActive(true);
            opcionDerecha.transform.parent.gameObject.SetActive(true);
            ActualizarIndicacion();
        }

        void ConfirmarEleccion()
        {
            float diferencia = DesviacionMiraBifurcacion;
            float giro;
            if (diferencia < -UmbralSeleccionBifurcacion) giro = -90f;
            else if (diferencia > UmbralSeleccionBifurcacion) giro = 90f;
            else
            {
                txtEleccion.text = "APUNTÁ A IZQUIERDA O DERECHA ANTES DE PISAR";
                return;
            }

            float yawElegido = yawJugador + giro;
            int cuarto = Mathf.RoundToInt(yawElegido / 90f);
            Vector2Int delta;
            switch (((cuarto % 4) + 4) % 4)
            {
                case 0: delta = Vector2Int.right; break;
                case 1: delta = Vector2Int.up; break;
                case 2: delta = Vector2Int.left; break;
                default: delta = Vector2Int.down; break;
            }
            Vector2Int siguiente = celdaActual + delta;
            if (Laberinto.Muro(siguiente))
            {
                txtEleccion.text = "ESE LADO ESTÁ BLOQUEADO · ELEGÍ EL OTRO";
                return;
            }

            bifurcacionesResueltas.Add(celdaActual);
            decisionActiva = false;
            opcionIzquierda.transform.parent.gameObject.SetActive(false);
            opcionDerecha.transform.parent.gameObject.SetActive(false);
            txtEleccion.text = "";
            StartCoroutine(MoverACelda(siguiente, delta, true));
        }

        void Dialogo(string id)
        {
            if (dialogoRaiz != null) Destroy(dialogoRaiz.gameObject);
            dialogoRaiz = UIKit.CrearRaiz(canvas.transform, "Dialogo_" + id);
            bloqueado = true;
            dialogoActual = id;
            if (id == "callejon")
            {
                // El jugador ve la media vuelta gradual hacia el camino de regreso.
                StartCoroutine(GirarGradual(yawJugador + 180f, DuracionGiro(yawJugador + 180f)));
            }
            string titulo = "BYTE / IA CORRUPTA", texto;
            switch (id)
            {
                case "entrada": texto = "El minimapa se apagó. Apuntá con la pistola para orientarte y pisá la placa del piso para confirmar cada paso."; break;
                case "bif1": texto = "Primera bifurcación. Elegí el lado apuntando con la pistola; la placa confirma tu decisión."; break;
                case "bif2": texto = "Otra bifurcación. La dirección de la mira marca el camino que vas a tomar."; break;
                case "rival": texto = "Parece que vas bien. Yo ya calculé los caminos posibles. Seguí avanzando."; break;
                case "callejon": texto = "Elegiste un camino sin salida. La cámara está girando hacia el regreso; cuando termine, pisá para volver."; break;
                default: texto = "Pisá la placa para avanzar."; break;
            }
            txtSeccion.text = id.StartsWith("bif") ? "LABERINTO 2D / ELEGÍ TU CAMINO" : "LABERINTO 2D / PRIMERA PERSONA";
            txtPie.text = "PISÁ LA PLACA DEL PISO PARA AVANZAR";
            txtAccion.text = "Avanzar";
            UIKit.Dialogo(dialogoRaiz, 55, 592, 890, 166, titulo, texto, continuar: CerrarDialogo);
        }

        void CerrarDialogo()
        {
            if (dialogoRaiz != null) { Destroy(dialogoRaiz.gameObject); dialogoRaiz = null; }
            bloqueado = false;
            if (dialogoActual == "callejon")
            {
                txtPie.text = "VISTA CENTRADA · PISÁ PARA VOLVER";
            }
            else txtPie.text = "VISTA CENTRADA · PISÁ PARA AVANZAR";
            dialogoActual = "";
            txtAccion.text = "Avanzar";
        }

        void FinalizarApartado()
        {
            terminado = true; bloqueado = true;
            DatosPartida.Recorrido = new List<Vector2Int>(recorrido);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            txtSeccion.text = "FIN DEL APARTADO / LABERINTO";
            txtEleccion.text = "";
            botonAvanzar.gameObject.SetActive(false);
            iconoPisada.gameObject.SetActive(false);
            txtAccion.gameObject.SetActive(false);
            if (dialogoRaiz != null) Destroy(dialogoRaiz.gameObject);
            dialogoRaiz = UIKit.CrearRaiz(canvas.transform, "FinLaberinto");
            var panel = UIKit.NuevaCaja(dialogoRaiz, 263, 244, 930, 330, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Byte(panel.rt, 34, 74, 0.9f);
            UIKit.Texto(panel.rt, "APARTADO COMPLETADO", 160, 42, 720, 34, 24, Paleta.Verde, TextAnchor.MiddleLeft, FontStyles.Bold);
            UIKit.Texto(panel.rt, "Llegaste al punto B.", 160, 98, 720, 50, 32, Paleta.Crema, TextAnchor.MiddleLeft, FontStyles.Bold);
            UIKit.Texto(panel.rt, "Avanzaste " + Mathf.Max(0, recorrido.Count - 1) + " pasos usando la pistola y la placa del piso.", 52, 183, 820, 42, 20, Paleta.Gris, TextAnchor.MiddleLeft);
            UIKit.Texto(panel.rt, "FIN DEL APARTADO", 52, 250, 820, 32, 18, Paleta.Amarillo, TextAnchor.MiddleCenter, FontStyles.Bold);
            if (miraHUD != null) miraHUD.SetActive(false);
            UIKit.Boton(dialogoRaiz, "", "Ver comparación", 522, 620, 412, 64, () => IrAEscena("Escena3"));
            txtPie.text = "LABERINTO 2D / PRIMERA PERSONA";
        }

        void OnDestroy()
        {
            if (textura != null) Destroy(textura);
            if (texturaBoton != null) Destroy(texturaBoton);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}



