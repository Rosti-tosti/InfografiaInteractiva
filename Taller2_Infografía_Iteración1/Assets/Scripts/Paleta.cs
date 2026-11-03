using UnityEngine;

namespace Pathfinding
{
    // Colores medidos pixel a pixel de los frames de Figma (1600x900).
    public static class PaletaFigma
    {
        public static readonly Color Crema    = Hex(241, 235, 216); // titulos / mensajes
        public static readonly Color Gris     = Hex(178, 187, 163); // textos secundarios / pie
        public static readonly Color Verde    = Hex(148, 185, 133); // bordes y etiquetas
        public static readonly Color VerdeTenue = new Color(148 / 255f, 185 / 255f, 133 / 255f, 0.45f);
        public static readonly Color Amarillo = Hex(229, 191, 105); // botones
        public static readonly Color AmarilloTenue = Hex(160, 134, 74);
        public static readonly Color Naranja  = Hex(215, 131, 89);  // linea decorativa / antena
        public static readonly Color Rosa     = Hex(185, 133, 133); // punto B del camino
        public static readonly Color Panel    = new Color(20 / 255f, 27 / 255f, 22 / 255f, 0.94f);
        public static readonly Color Velo     = Hex(10, 17, 13);

        static Color Hex(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f, 1f); }
    }
}
