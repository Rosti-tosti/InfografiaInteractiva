using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Pathfinding
{
    // Escena 1: cargas, misión, "Vos contra la IA" y minimapa.
    public class Escena1Intro : EscenaBase
    {
        int idx = -1;
        static bool iniciarEnTituloPathfinding;
        float t;
        TMP_Text subtitulo;
        string subBase;
        const float DuracionCarga = 3.2f;

        void Start()
        {
            if (iniciarEnTituloPathfinding)
            {
                iniciarEnTituloPathfinding = false;
                idx = -1;
            }
            Siguiente();
        }

        public static void AbrirInicioPathfinding()
        {
            const string escenaPathfinding = "Escena1";
            if (!Application.CanStreamedLevelBeLoaded(escenaPathfinding))
            {
                Debug.LogWarning("No se encontró la escena de inicio de Pathfinding: " + escenaPathfinding);
                return;
            }
            iniciarEnTituloPathfinding = true;
            SceneManager.LoadScene(escenaPathfinding);
        }

        void Siguiente()
        {
            idx++;
            var p = NuevaPantalla();
            t = 0;
            Oscurecer(idx <= 1 ? 0.8f : 0.12f);
            switch (idx)
            {
                case 0: Carga(p, "Buscando la ruta más optima", "VAMOS! NOs vemos aL oTRo laDO"); break;
                case 1: Carga(p, "Estableciendo casilleros", "Tu miSióN es llEgAr al pUntO B, veAmOs qUiEn es máS ráPiDo."); break;
                case 2: Mision(p); break;
                case 3: Rival(p); break;
                case 4: Mapa(p); break;
                default: IrAEscena("Escena2"); break;
            }
        }

        void Update()
        {
            ShootButton.Procesar();
            if (idx >= 0 && idx <= 1)
            {
                t += Time.deltaTime;
                int n = Mathf.Clamp(Mathf.FloorToInt(t / DuracionCarga * 8f), 0, 8);
                if (subtitulo != null) subtitulo.text = subBase + " //" + new string('.', n) + "//";
                if (t >= DuracionCarga || Input.GetMouseButtonDown(0)) Siguiente();
            }
        }

        void Carga(Transform p, string sub, string byteTxt)
        {
            UIKit.Encabezado(p, "Pathfinding/Sintetika", null);
            UIKit.Texto(p, "PATHFINDING", 68, 150, 900, 110, 84, Paleta.Crema, TextAnchor.MiddleLeft, FontStyles.Bold);
            UIKit.Rellenar(p, 74, 284, 123, 4, Paleta.Naranja);
            subBase = sub;
            subtitulo = UIKit.Texto(p, sub + " //........//", 74, 322, 900, 40, 24, Paleta.Gris);
            var c = UIKit.NuevaCaja(p, 237, 583, UIKit.DialogoAncho, UIKit.DialogoAlto, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Byte(c.rt, 18, 18, 0.82f);
            UIKit.RotuloDialogo(c.rt, "BYTE / Tu gUiA", 124, 30, UIKit.DialogoAncho - 148, 27, Paleta.Verde);
            UIKit.CuerpoDialogo(c.rt, byteTxt, 124, 65, UIKit.DialogoAncho - 148, 82, Paleta.Crema);
            UIKit.Pie(p, "APUNTÁ CON LA PISTOLA · PISÁ LA PLACA PARA AVANZAR", null);
        }

        void FondoJuego(Transform p, string der)
        {
            UIKit.Encabezado(p, "SINTÉTIKA / IA EN JUEGO", der);
            UIKit.Pie(p, "APUNTÁ CON LA PISTOLA · PISÁ LA PLACA PARA AVANZAR", "LABERINTO 2D / PRIMERA PERSONA");
        }

        void Mision(Transform p)
        {
            FondoJuego(p, "INTRODUCCIÓN / MISIÓN");
            UIKit.Texto(p, "Tu misión", 58, 124, 700, 50, 36, Paleta.Crema, TextAnchor.MiddleLeft, FontStyles.Bold);
            var c = UIKit.NuevaCaja(p, 58, 220, 428, 220, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Texto(c.rt, "OBJETIVO", 16, 16, 300, 18, 12, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Texto(c.rt, "Apuntá con la pistola, elegí un camino y avanzá pisando la placa.", 16, 46, 396, 80, 20, Paleta.Crema);
            UIKit.Texto(c.rt, "Salís en el punto A y tenés que llegar al punto B.", 16, 132, 396, 70, 17, Paleta.Gris);
            UIKit.DialogoTema(p, 55, 592, "BYTE / IA CORRUPTA", "Bienvenido a mi laberinto. Tu trabajo es simple: llegar del punto A al punto B.");
            UIKit.Boton(p, "[ + ]", "Siguiente", 1047, 639, 353, 73, Siguiente, KeyCode.Return);
        }

        void Rival(Transform p)
        {
            FondoJuego(p, "INTRODUCCIÓN / EL RIVAL");
            UIKit.Texto(p, "Vos contra la IA", 58, 124, 700, 50, 36, Paleta.Crema, TextAnchor.MiddleLeft, FontStyles.Bold);
            var a = UIKit.NuevaCaja(p, 236, 220, 426, 220, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Texto(a.rt, "VOS", 16, 16, 300, 18, 12, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Texto(a.rt, "Apuntás izquierda o derecha con la pistola.", 16, 46, 394, 80, 20, Paleta.Crema);
            UIKit.Texto(a.rt, "Pisás la placa para confirmar y avanzar.", 16, 132, 394, 70, 17, Paleta.Gris);
            UIKit.Texto(p, "VS", 662, 270, 132, 80, 50, Paleta.Naranja, TextAnchor.MiddleCenter, FontStyles.Bold);
            var b = UIKit.NuevaCaja(p, 794, 220, 424, 220, new Color(0.14f, 0.10f, 0.08f, 0.96f), Paleta.Naranja, 2f);
            UIKit.Texto(b.rt, "BYTE / IA", 16, 16, 300, 18, 12, Paleta.Naranja, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Texto(b.rt, "Recorre el mismo laberinto que vos.", 16, 46, 392, 80, 20, Paleta.Crema);
            UIKit.Texto(b.rt, "Calcula el costo de cada camino antes de moverse.", 16, 132, 392, 70, 17, Paleta.Gris);
            UIKit.DialogoTema(p, 55, 592, "BYTE / IA CORRUPTA", "Apuntá al camino que elegís. La placa del piso confirma cada paso.");
            UIKit.Boton(p, "[ + ]", "Siguiente", 1047, 639, 353, 73, Siguiente, KeyCode.Return);
        }

        void Mapa(Transform p)
        {
            FondoJuego(p, "INTRODUCCIÓN / EL MAPA");
            var c = UIKit.NuevaCaja(p, 428, 97, 517, 484, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Texto(c.rt, "MINIMAPA / A Y B", 38, 40, 440, 30, 22, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            MapaUI.Dibujar(p, 428 + 78.5f, 186, 360, 4);
            UIKit.DialogoTema(p, 55, 592, "BYTE / IA CORRUPTA", "Una vez adentro, el minimapa se apaga. Apuntá con la pistola y pisá la placa del piso para confirmar cada avance.");
            UIKit.Boton(p, "[ + ]", "Empezar", 1047, 639, 353, 73, Siguiente, KeyCode.Return);
        }
    }
}



