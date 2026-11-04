using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Pathfinding
{
    // Escena 4: explicación del algoritmo A* y resumen.
    public class Escena4Explicacion : EscenaBase
    {
        void Start() { Oscurecer(0.78f); Casilleros(); }
        void Update() { ShootButton.Procesar(); }

        Transform Base()
        {
            var p = NuevaPantalla();
            UIKit.Encabezado(p, "Pathfinding/Sintetika", null);
            return p;
        }

        void Casilleros()
        {
            var p = Base();
            var panel = UIKit.NuevaCaja(p, 37, 125, 613, 634, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Byte(panel.rt, 26, 30, 1f);
            UIKit.RotuloDialogo(panel.rt, "BYTE / IA CORRUPTA", 150, 30, 430, 27, Paleta.Verde);
            UIKit.CuerpoDialogo(panel.rt, "Voy a compartirte mi secreto", 150, 65, 430, 56, Paleta.Crema);
            UIKit.Texto(panel.rt, "ALGORITMO A*", 60, 219, 400, 18, 13, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Texto(panel.rt, "Para empezar dividimos el area a recorrer en zonas.", 60, 247, 500, 60, 19, Paleta.Gris);
            UIKit.Rellenar(panel.rt, 33, 297, 4, 90, Paleta.VerdeBorde);
            UIKit.Texto(panel.rt, "Cada uno de estos “casilleros” tiene una etiqueta para distinguirlos.", 60, 305, 500, 70, 19, Paleta.Gris);
            UIKit.Texto(panel.rt, "Luego en función de que distancia exista desde el casillero actual hasta la meta, cada uno de los mismos adquiere valores especificos.", 60, 390, 500, 110, 19, Paleta.Gris);
            UIKit.Boton(p, "", "¿Y qué más?", 411, 696, 223, 47, Letras, KeyCode.Return, true);

            string[] letras = { "A","B","C","D","E","F","G","H","I","J","K","L","M","N","Ñ","O","P","Q","R","S","T","U","V","W","X" };
            float[] xs = { 851, 952, 1056, 1157, 1258 };
            float[] ys = { 216, 315, 414, 512, 610 };
            for (int i = 0; i < 25; i++)
            {
                bool esp = i == 0 || i == 24;
                var c = UIKit.NuevaCaja(p, xs[i % 5], ys[i / 5], 90, 90, new Color(0.07f, 0.11f, 0.09f), esp ? Paleta.Amarillo : Paleta.VerdeBorde, 6f);
                UIKit.Texto(c.rt, letras[i], 8, 6, 70, 50, 34, esp ? Paleta.Amarillo : Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            }
            UIKit.Texto(p, "Casillero actual", 777, 134, 300, 34, 24, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Texto(p, "↓", 876, 168, 30, 44, 34, Paleta.Verde, TextAnchor.UpperCenter);
            UIKit.Texto(p, "↑", 1289, 696, 30, 44, 34, Paleta.Verde, TextAnchor.UpperCenter);
            UIKit.Texto(p, "Meta", 1250, 736, 110, 34, 24, Paleta.Verde, TextAnchor.UpperCenter, FontStyles.Bold);
        }

        void Letras()
        {
            var p = Base();
            string[] L = { "A","B","C","F","G","H" };
            int[] f = { 88, 74, 68, 88, 74, 68 };
            int[] g = { 28, 24, 28, 28, 24, 28 };
            int[] h = { 60, 50, 40, 60, 50, 40 };
            float[] xs = { 112, 292, 472 };
            float[] ys = { 286, 457 };
            for (int i = 0; i < 6; i++)
            {
                var c = UIKit.NuevaCaja(p, xs[i % 3], ys[i / 3], 155, 155, new Color(0.07f, 0.11f, 0.09f), Paleta.VerdeBorde, 6f);
                UIKit.Texto(c.rt, L[i], 12, 6, 100, 80, 64, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
                UIKit.Texto(c.rt, f[i].ToString(), 85, 14, 56, 24, 17, Paleta.Verde, TextAnchor.UpperRight, FontStyles.Bold);
                UIKit.Texto(c.rt, g[i].ToString(), 14, 118, 56, 24, 17, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
                UIKit.Texto(c.rt, h[i].ToString(), 85, 118, 56, 24, 17, Paleta.Verde, TextAnchor.UpperRight, FontStyles.Bold);
            }
            UIKit.Texto(p, "Letra del casillero", 38, 152, 420, 40, 30, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Rellenar(p, 428, 174, 72, 3, Paleta.VerdeBorde);
            UIKit.Rellenar(p, 497, 174, 3, 64, Paleta.VerdeBorde);
            UIKit.Texto(p, "▼", 486, 232, 30, 30, 22, Paleta.VerdeBorde, TextAnchor.UpperCenter);
            UIKit.Texto(p, "↑", 106, 618, 36, 50, 40, Paleta.Verde, TextAnchor.UpperCenter);
            UIKit.Texto(p, "h(estimación del costo hasta la meta)", 112, 696, 700, 40, 25, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);

            var panel = UIKit.NuevaCaja(p, 808, 125, 612, 634, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Byte(panel.rt, 26, 30, 1f);
            UIKit.RotuloDialogo(panel.rt, "BYTE / IA CORRUPTA", 150, 30, 430, 27, Paleta.Verde);
            UIKit.CuerpoDialogo(panel.rt, "Voy a compartirte mi secreto", 150, 65, 430, 56, Paleta.Crema);
            UIKit.Texto(panel.rt, "ALGORITMO A*", 60, 290, 400, 18, 13, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Rellenar(panel.rt, 33, 297, 4, 92, Paleta.VerdeBorde);
            UIKit.Texto(panel.rt, "Además de los pasos necesarios para llegar al casillero actual, el algoritmo también calcula constantemente cuanto faltan para llegar a la meta.", 60, 320, 510, 120, 19, Paleta.Gris);
            UIKit.Boton(p, "", "¿Y el cálculo?", 1182, 696, 222, 47, Resumen, KeyCode.Return, true);
        }

        void Resumen()
        {
            var p = Base();
            var panel = UIKit.NuevaCaja(p, 37, 125, 681, 634, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Byte(panel.rt, 26, 30, 1f);
            UIKit.RotuloDialogo(panel.rt, "BYTE / IA CORRUPTA", 150, 30, 510, 27, Paleta.Verde);
            UIKit.CuerpoDialogo(panel.rt, "Voy a compartirte mi secreto.", 150, 65, 510, 56, Paleta.Crema);
            UIKit.Texto(panel.rt, "A* / BÚSQUEDA DE RUTA", 26, 198, 500, 18, 13, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Texto(panel.rt, "A* compara los próximos pasos para encontrar una ruta de menor costo entre A y B.", 26, 238, 620, 80, 24, Paleta.Crema);
            UIKit.Texto(panel.rt, "f(n) = g(n) + h(n)", 26, 315, 620, 60, 42, Paleta.Verde, TextAnchor.UpperLeft, FontStyles.Bold);
            UIKit.Texto(panel.rt, "g: costo recorrido hasta ese punto.", 26, 395, 620, 26, 17, Paleta.Gris);
            UIKit.Texto(panel.rt, "h: estimación del costo hasta la meta.", 26, 424, 620, 26, 17, Paleta.Gris);
            UIKit.Texto(panel.rt, "CADA PASO QUE DA ES EL MÁS OPTIMO DISPONIBLE", 26, 468, 620, 22, 14, Paleta.Amarillo, TextAnchor.UpperLeft, FontStyles.Bold);

            float lado = 342, gx = 958, gy = 125;
            MapaUI.Dibujar(p, gx, gy, lado, 4, null, null, true);
            MapaUI.Ruta(p, gx, gy, lado, AEstrella.Buscar(Laberinto.A, Laberinto.B), Paleta.Verde, 4);
            // Letras A y B quedan debajo de la línea; se redibujan arriba
            UIKit.Texto(p, "A", gx + 1.5f * lado / 7 - 20, gy + 1.5f * lado / 7 - 20, 40, 40, 20, Paleta.Oscuro, TextAnchor.MiddleCenter, FontStyles.Bold);
            UIKit.Texto(p, "B", gx + 5.5f * lado / 7 - 20, gy + 5.5f * lado / 7 - 20, 40, 40, 20, Paleta.Oscuro, TextAnchor.MiddleCenter, FontStyles.Bold);

            var q = UIKit.NuevaCaja(p, 600, 470, 820, 150, Paleta.Panel, Paleta.Verde, 2f);
            UIKit.Byte(q.rt, 24, 22, 1f);
            UIKit.RotuloDialogo(q.rt, "BYTE / IA CORRUPTA", 148, 22, 470, 27, Paleta.Verde);
            UIKit.CuerpoDialogo(q.rt, "¿Que exploramos ahora?", 148, 57, 470, 56, Paleta.Crema);
            UIKit.Boton(p, "", "MAQUINA DE ESTADOS", 1017, 640, 223, 48,
                IntroFlow.AbrirMaquinaEstadosDesdePathfinding, KeyCode.None, true);
            UIKit.Boton(p, "", "IA DIRECTOR", 1017, 700, 223, 48, IntroFlow.AbrirIADirector, KeyCode.None, true);
        }
    }
}



