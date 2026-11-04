using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Pathfinding
{
    // Controla la secuencia de inicio, la presentación de Axel y la elección del capítulo.
    // Todas las pantallas son GameObjects reales en Canvas/Pantallas; aca solo se activan / desactivan.
    public class IntroFlow : MonoBehaviour
    {
        static bool iniciarDirectoEnMaquinaEstados;
        static bool iniciarDirectoEnIADirector;

        [Header("Pantallas (en orden)")]
        public GameObject s00Bienvenida;
        public GameObject s01AsignandoGuia;
        public GameObject s02Cargando;
        public GameObject s03ByteHola;
        public GameObject s04ByteEspecialista;
        public GameObject s05Axel;
        public GameObject s06Menu;

        [Header("Botones")]
        public Button btnComenzar;
        public Button btnEntendido;
        public Button btnElegirPathfinding;
        public Button btnElegirMaquina;
        public ByteTemaHover hoverTemas;

        [Header("Siguiente escena (Pathfinding)")]
        public string escenaPathfinding = "Escena1";
        public float segundosCargando = 2f;
        public bool usarArduino = true;
        public string puertoArduino = "COM3";
        public int baudArduino = 115200;
        [Header("Control OSC desde el celular")]
        public bool usarOSC;
        public int puertoOSC = 9000;
        public string ipTelefonoOSC = "";
        [Range(5f, 40f)] public float suavizadoMira = 22f;

        GameObject[] todas;
        ArcadeGunInput control;
        int actual = -1;

        void Start()
        {
            todas = new[] { s00Bienvenida, s01AsignandoGuia, s02Cargando, s03ByteHola, s04ByteEspecialista, s05Axel, s06Menu };
            control = GetComponent<ArcadeGunInput>();
            if (control == null) control = gameObject.AddComponent<ArcadeGunInput>();
            control.usarArduino = usarArduino;
            control.puerto = puertoArduino;
            control.baudRate = baudArduino;
            control.usarOSC = usarOSC;
            control.puertoOSC = puertoOSC;
            control.ipTelefonoOSC = ipTelefonoOSC;
            control.suavizadoMira = suavizadoMira;
            Click(btnComenzar, () => Ir(1));
            Click(btnEntendido, () => Ir(2));
            Click(btnElegirPathfinding, ElegirPathfinding);
            Click(btnElegirMaquina, EntrarMaquina);
            AgregarContinuarDialogo(s03ByteHola, "Dialogo", 4);
            AgregarContinuarDialogo(s04ByteEspecialista, "Dialogo", 5);
            AgregarContinuarDialogo(s05Axel, "Dialogo_Axel", 6);
            if (iniciarDirectoEnIADirector)
            {
                iniciarDirectoEnIADirector = false;
                EntrarIADirector();
            }
            else if (iniciarDirectoEnMaquinaEstados)
            {
                iniciarDirectoEnMaquinaEstados = false;
                EntrarMaquina();
            }
            else Ir(0);
        }

        public static void AbrirMaquinaEstadosDesdePathfinding()
        {
            const string escenaInicio = "01_Intro";
            iniciarDirectoEnMaquinaEstados = true;
            if (Application.CanStreamedLevelBeLoaded(escenaInicio))
                SceneManager.LoadScene(escenaInicio);
            else
            {
                iniciarDirectoEnMaquinaEstados = false;
                Debug.LogError("No se puede iniciar Máquina de Estados: falta la escena '01_Intro' en Build Settings.");
            }
        }

        public static void AbrirIADirector()
        {
            const string escenaInicio = "01_Intro";
            iniciarDirectoEnIADirector = true;
            if (Application.CanStreamedLevelBeLoaded(escenaInicio))
                SceneManager.LoadScene(escenaInicio);
            else
            {
                iniciarDirectoEnIADirector = false;
                Debug.LogError("No se puede iniciar IA Director: falta la escena '01_Intro' en Build Settings.");
            }
        }

        static void Click(Button b, UnityEngine.Events.UnityAction a) { if (b != null) b.onClick.AddListener(a); }

        void AgregarContinuarDialogo(GameObject pantalla, string nombreDialogo, int siguiente)
        {
            if (pantalla == null) return;
            var dialogo = pantalla.transform.Find(nombreDialogo);
            if (dialogo == null) return;
            var mensaje = dialogo.Find("Mensaje") as RectTransform;
            if (mensaje != null)
            {
                var tam = mensaje.sizeDelta;
                tam.x = 680f;
                mensaje.sizeDelta = tam;
            }
            UIKit.BotonContinuar(dialogo, () => Ir(siguiente), 1001f, 133f);
        }

        void Ir(int i)
        {
            StopAllCoroutines();
            actual = i;
            for (int k = 0; k < todas.Length; k++) if (todas[k] != null) todas[k].SetActive(k == i);
            if (i == 2) StartCoroutine(Esperar(segundosCargando, () => Ir(3)));
        }

        IEnumerator Esperar(float s, System.Action fin) { yield return new WaitForSeconds(s); fin(); }

        void ElegirPathfinding()
        {
            if (Application.CanStreamedLevelBeLoaded(escenaPathfinding)) SceneManager.LoadScene(escenaPathfinding);
            else Debug.Log("Escena '" + escenaPathfinding + "' todavia no existe (se arma en la siguiente entrega).");
        }

        void EntrarMaquina()
        {
            // La secuencia se monta sobre esta escena para reutilizar la entrada arcade,
            // sin alterar las pantallas existentes ni iniciar Pathfinding.
            foreach (var pantalla in todas) if (pantalla != null) pantalla.SetActive(false);
            actual = 7;
            enabled = false;
            var flujo = GetComponent<MaquinaEstadosFlow>();
            if (flujo == null) flujo = gameObject.AddComponent<MaquinaEstadosFlow>();
            flujo.Iniciar(control);
        }

        void EntrarIADirector()
        {
            foreach (var pantalla in todas) if (pantalla != null) pantalla.SetActive(false);
            actual = 8;
            enabled = false;
            var flujo = GetComponent<IADirectorFlow>();
            if (flujo == null) flujo = gameObject.AddComponent<IADirectorFlow>();
            flujo.Iniciar(control);
        }

        void Update()
        {
            if (ShootButton.Procesar()) return;
            if (Input.GetKeyDown(KeyCode.Z))
            {
                EntrarIADirector();
                var flujoPrueba = GetComponent<IADirectorFlow>();
                if (flujoPrueba != null) flujoPrueba.IrAlMinijuegoDirecto();
                return;
            }
            bool avanzar = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            bool disparo = control != null && control.TomarClickPistola();
            if (actual >= 3 && actual <= 5 && (avanzar || Input.GetMouseButtonDown(0) || disparo))
            {
                Ir(actual + 1);
                return;
            }

            bool izq = Input.GetKeyDown(KeyCode.LeftArrow), der = Input.GetKeyDown(KeyCode.RightArrow);
            switch (actual)
            {
                case 0: if (avanzar || Input.GetMouseButtonDown(0) || disparo) Ir(1); break;
                case 1: if (avanzar || Input.GetMouseButtonDown(0) || disparo) Ir(2); break;
                case 2: if (avanzar || Input.GetMouseButtonDown(0) || disparo) Ir(3); break;
                case 6:
                    if (izq) hoverTemas?.SeleccionarTema(0);
                    else if (der) hoverTemas?.SeleccionarTema(1);
                    else if ((avanzar || disparo) && hoverTemas != null)
                    {
                        if (hoverTemas.TemaSeleccionado == 0) ElegirPathfinding();
                        else if (hoverTemas.TemaSeleccionado == 1) EntrarMaquina();
                    }
                    break;
            }
        }
    }
}
