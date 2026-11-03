using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Pathfinding
{
    // Escena 3: comparación entre tu recorrido (amarillo) y la ruta A* (verde).
    public class Escena3Comparacion : EscenaBase
    {
        void Start()
        {
            var recorrido = new List<Vector2Int>(DatosPartida.Recorrido);
            if (recorrido.Count < 2) recorrido = DemoRecorrido();   // por si se abre la escena sola

            var optima = AEstrella.Buscar(Laberinto.A, Laberinto.B);
            int pasosJugador = recorrido.Count - 1;
            int pasosA = optima.Count - 1;
            int efectivo = RutaEfectiva(recorrido).Count - 1;

            Oscurecer(0.78f);
            var p = NuevaPantalla();
            UIKit.Encabezado(p, "Pathfinding/Sintetika", null);

            var amarillo = new HashSet<Vector2Int>(recorrido);
            var verde = new HashSet<Vector2Int>(optima);
            // La ruta amarilla se dibuja encima de la verde, igual que en el diseño
            MapaUI.Dibujar(p, 87, 123, 638, 5, amarillo, verde, true);

            var panel = UIKit.NuevaCaja(p, 808, 123, 612, 635, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Byte(panel.rt, 26, 30, 1f);
            UIKit.RotuloDialogo(panel.rt, "BYTE / IA CORRUPTA", 150, 30, 430, 27, Paleta.Verde);
            UIKit.CuerpoDialogo(panel.rt, "Soy imbatible en esto.", 150, 65, 430, 56, Paleta.Crema);

            Tarjeta(panel.rt, 154, "TU RECORRIDO / AMARILLO", "Costo mínimo: " + efectivo + " pasos.", pasosJugador.ToString(), Paleta.Amarillo);
            Tarjeta(panel.rt, 258, "RUTA A* / VERDE", "Costo mínimo: " + pasosA + " pasos.", pasosA.ToString(), Paleta.Verde);

            int menos = Mathf.Max(0, pasosJugador - pasosA);
            UIKit.Texto(panel.rt, menos + " PASOS MENOS · MISMO PUNTO B", 33, 376, 540, 24, 14, Paleta.Amarillo, TextAnchor.UpperLeft, FontStyles.Bold);

            UIKit.Boton(p, "", "¿Cómo funciona?", 1003, 678, 222, 47, () => IrAEscena("Escena4"), KeyCode.Return, true);
        }

        void Tarjeta(Transform panel, float y, string titulo, string sub, string numero, Color c)
        {
            UIKit.Rellenar(panel, 33, y, 4, 77, c);
            UIKit.Texto(panel, titulo, 60, y + 12, 400, 18, 13, c, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Texto(panel, sub, 60, y + 40, 420, 28, 20, Paleta.Gris);
            UIKit.Texto(panel, numero, 450, y + 12, 120, 60, 46, c, TextAnchor.MiddleRight, FontStyles.Bold);
        }

        // Quita los desvíos (idas y vueltas) del recorrido para obtener la ruta efectiva
        static List<Vector2Int> RutaEfectiva(List<Vector2Int> r)
        {
            var res = new List<Vector2Int>();
            foreach (var c in r)
            {
                int i = res.IndexOf(c);
                if (i >= 0) res.RemoveRange(i + 1, res.Count - i - 1);
                else res.Add(c);
            }
            return res;
        }

        static List<Vector2Int> DemoRecorrido()
        {
            int[,] pts = { {1,1},{2,1},{3,1},{3,2},{3,3},{2,3},{1,3},{2,3},{3,3},{4,3},{5,3},{5,2},{5,1},{5,2},{5,3},{5,4},{5,5} };
            var l = new List<Vector2Int>();
            for (int i = 0; i < pts.GetLength(0); i++) l.Add(new Vector2Int(pts[i, 0], pts[i, 1]));
            return l;
        }

        void Update() { ShootButton.Procesar(); }
    }
}



