using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Pathfinding
{
    /// <summary>Muestra la tarjeta expandida del tema que está bajo el puntero.</summary>
    public class ByteTemaHover : MonoBehaviour
    {
        [SerializeField] float escalaInicio = 0.94f;
        [SerializeField] float velocidadExpansion = 12f;
        [SerializeField] float demoraAlSalir = 0.14f;
        [SerializeField] GameObject estadoMenu;
        [SerializeField] GameObject estadoPathfinding;
        [SerializeField] GameObject estadoMaquina;
        [SerializeField] Image byteCuerpo;
        [SerializeField] RectTransform tarjetaPathfindingExpandida;
        [SerializeField] RectTransform tarjetaMaquinaExpandida;
        [SerializeField] Sprite bytePathfinding;
        [SerializeField] Sprite byteMaquina;

        int temaSeleccionado = -1;
        public int TemaSeleccionado => temaSeleccionado;

        public void Configurar(GameObject menu, GameObject pathfinding, GameObject maquina, Image imagenByte,
            RectTransform tarjetaPath, RectTransform tarjetaMaquina, Sprite spritePath, Sprite spriteMaquina)
        {
            estadoMenu = menu;
            estadoPathfinding = pathfinding;
            estadoMaquina = maquina;
            byteCuerpo = imagenByte;
            tarjetaPathfindingExpandida = tarjetaPath;
            tarjetaMaquinaExpandida = tarjetaMaquina;
            bytePathfinding = spritePath;
            byteMaquina = spriteMaquina;
            temaSeleccionado = -1;
            MostrarTema();
        }

        public void HoverPathfinding() => SeleccionarTema(0);
        public void HoverMaquina() => SeleccionarTema(1);
        public void HoverPathfindingEvent(BaseEventData _) => HoverPathfinding();
        public void HoverMaquinaEvent(BaseEventData _) => HoverMaquina();
        public void SalirTarjetaEvent(BaseEventData _) => SalirTarjeta();
        public void SalirTarjeta()
        {
            CancelInvoke(nameof(LimpiarHover));
            Invoke(nameof(LimpiarHover), demoraAlSalir);
        }

        public void SeleccionarTema(int indice)
        {
            if (indice < 0 || indice > 1) return;
            CancelInvoke(nameof(LimpiarHover));
            bool cambio = temaSeleccionado != indice;
            temaSeleccionado = indice;
            if (cambio)
            {
                var tarjeta = indice == 0 ? tarjetaPathfindingExpandida : tarjetaMaquinaExpandida;
                if (tarjeta != null) tarjeta.localScale = Vector3.one * escalaInicio;
            }
            MostrarTema();
        }

        void LimpiarHover()
        {
            temaSeleccionado = -1;
            MostrarTema();
        }

        void MostrarTema()
        {
            if (estadoMenu != null) estadoMenu.SetActive(temaSeleccionado < 0);
            if (estadoPathfinding != null) estadoPathfinding.SetActive(temaSeleccionado == 0);
            if (estadoMaquina != null) estadoMaquina.SetActive(temaSeleccionado == 1);
            if (byteCuerpo != null)
                byteCuerpo.sprite = temaSeleccionado == 1 ? byteMaquina : bytePathfinding;
        }

        void Update()
        {
            Animar(tarjetaPathfindingExpandida, temaSeleccionado == 0);
            Animar(tarjetaMaquinaExpandida, temaSeleccionado == 1);
        }

        void Animar(RectTransform tarjeta, bool activa)
        {
            if (!activa || tarjeta == null) return;
            tarjeta.localScale = Vector3.Lerp(tarjeta.localScale, Vector3.one,
                Time.unscaledDeltaTime * velocidadExpansion);
        }
    }
}
