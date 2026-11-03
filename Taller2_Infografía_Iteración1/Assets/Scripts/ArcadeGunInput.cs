using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>Reads aim and button input from Arduino serial or OSC sensor streams.</summary>
    public class ArcadeGunInput : MonoBehaviour
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [StructLayout(LayoutKind.Sequential)]
        struct PuntoCursorWindows { public int X; public int Y; }

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out PuntoCursorWindows punto);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
#endif

        [Header("Arduino serial (optional)")]
        public bool usarArduino;
        public string puerto = "COM3";
        public int baudRate = 115200;

        [Header("OSC desde Sensor2OSC")]
        public bool usarOSC = true;
        public int puertoOSC = 9000;
        [Tooltip("IP del celular para aceptar solo sus paquetes. Vacío acepta cualquier emisor de la red local.")]
        public string ipTelefonoOSC = "";
        public bool giroscopioOSCEnRadianes = true;
        [Range(0.1f, 10f)] public float sensibilidad = 1f;
        [Range(0f, 15f)] public float zonaMuerta = 1.5f;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public bool BotonPistola { get; private set; }
        bool clickPistolaPendiente;
        public bool BotonRecarga { get; private set; }
        public bool Conectado { get; private set; }
        public bool TieneMuestra { get; private set; }

        // Conserva el flanco de pulsación hasta que el flujo activo lo consuma.
        // Así no se pierde si Arduino envía PRESIONADO y LIBERADO en la misma lectura.
        public bool TomarClickPistola()
        {
            if (!clickPistolaPendiente) return false;
            clickPistolaPendiente = false;
            return true;
        }

        void ActualizarBotonPistola(bool presionado)
        {
            if (presionado && !BotonPistola) clickPistolaPendiente = true;
            BotonPistola = presionado;
        }

        object serial;
        Type serialType;
        PropertyInfo isOpen;
        MethodInfo readExisting, open, close;
        readonly StringBuilder buffer = new StringBuilder();
        bool primerAngulo;
        float ultimoYaw;
        float ultimoPitchArduino, referenciaPitchArduino;
        bool referenciaPitchArduinoLista;
        bool hayAcelerometro;
        bool referenciaIMU;
        bool primerGiro;
        bool registroPrimeraTrama;
        float inicioLecturaSerial;
        float proximoLogMuestra;
        bool avisoSinDatos;
        UdpClient receptorOSC;
        float ultimoPaqueteOSC = -10f;
        bool serialActivo;
        bool entradasIniciadas;
        bool registroPrimerPaqueteOSC;
        float accelX, accelY, accelZ, ultimoTiempoIMU, yawGiroscopio;
        float referenciaRoll, referenciaPitch;
        bool origenVentanaCursorCalculado;
        Vector2 origenVentanaCursor;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Texture2D mira = Resources.Load<Texture2D>("Juego/Mira");
            if (mira != null)
                Cursor.SetCursor(mira, new Vector2(mira.width * 0.5f, mira.height * 0.5f), CursorMode.Auto);
        }

        void Update()
        {
            if (!entradasIniciadas)
            {
                entradasIniciadas = true;
                if (usarArduino) AbrirPuerto();
                if (usarOSC) AbrirOSC();
            }
            if (usarArduino && serialActivo) LeerSerial();
            if (usarOSC) LeerOSC();

            bool oscActivo = receptorOSC != null && Time.realtimeSinceStartup - ultimoPaqueteOSC < 1.5f;
            Conectado = serialActivo || oscActivo;
            if (Conectado)
            {
                if (Input.GetKeyDown(KeyCode.T) && TieneMuestra) CentrarMira();
                if (TieneMuestra)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    float xReferencia = Mathf.Clamp(339f + Yaw * 6.78f, 0f, 678f);
                    float yReferencia = Mathf.Clamp(190f - Pitch * 2.9f, 0f, 380f);
                    MoverCursorWindows(new Vector2(xReferencia / 678f * Screen.width,
                        yReferencia / 380f * Screen.height), new Vector2(Screen.width, Screen.height));
                }
            }
            else LeerMouseComoMira();
        }

        void LeerMouseComoMira()
        {
            Yaw += Input.GetAxis("Mouse X") * 2.4f;
            Pitch = Mathf.Clamp(Pitch - Input.GetAxis("Mouse Y") * 2.4f, -65f, 65f);
            if (Input.GetKey(KeyCode.LeftArrow)) Yaw -= 120f * Time.deltaTime;
            if (Input.GetKey(KeyCode.RightArrow)) Yaw += 120f * Time.deltaTime;
            if (Input.GetKey(KeyCode.UpArrow)) Pitch = Mathf.Clamp(Pitch + 120f * Time.deltaTime, -65f, 65f);
            if (Input.GetKey(KeyCode.DownArrow)) Pitch = Mathf.Clamp(Pitch - 120f * Time.deltaTime, -65f, 65f);
        }

        public void MoverCursorWindows(Vector2 posicion, Vector2 referencia)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (Screen.width <= 0 || Screen.height <= 0 || referencia.x <= 0f || referencia.y <= 0f) return;

            // Calcula una sola vez el origen del área de juego en el escritorio. Así
            // funciona también cuando el juego está dentro de la vista Game de Unity.
            if (!origenVentanaCursorCalculado)
            {
                PuntoCursorWindows cursorActual;
                if (!GetCursorPos(out cursorActual)) return;
                Vector3 posicionUnity = Input.mousePosition;
                origenVentanaCursor = new Vector2(
                    cursorActual.X - posicionUnity.x,
                    cursorActual.Y - (Screen.height - posicionUnity.y));
                origenVentanaCursorCalculado = true;
            }

            float x = Mathf.Clamp01(posicion.x / referencia.x) * Screen.width;
            float y = (1f - Mathf.Clamp01(posicion.y / referencia.y)) * Screen.height;
            SetCursorPos(
                Mathf.RoundToInt(origenVentanaCursor.x + x),
                Mathf.RoundToInt(origenVentanaCursor.y + y));
#endif
        }

        void AbrirPuerto()
        {
            try
            {
                // Reflection avoids a compile-time dependency on System.IO.Ports.
                serialType = Type.GetType("System.IO.Ports.SerialPort, System") ?? Type.GetType("System.IO.Ports.SerialPort, System.IO.Ports");
                if (serialType == null) { Debug.LogWarning("System.IO.Ports no está disponible; se usarán mouse y flechas para apuntar."); return; }
                serial = Activator.CreateInstance(serialType, puerto, baudRate);
                isOpen = serialType.GetProperty("IsOpen");
                readExisting = serialType.GetMethod("ReadExisting");
                open = serialType.GetMethod("Open"); close = serialType.GetMethod("Close");
                var dtr = serialType.GetProperty("DtrEnable");
                if (dtr != null && dtr.CanWrite) dtr.SetValue(serial, true, null);
                open.Invoke(serial, null);
                serialActivo = true;
                inicioLecturaSerial = Time.realtimeSinceStartup;
                Debug.Log("Pistola arcade conectada en " + puerto);
            }
            catch (Exception e) { Debug.LogWarning("No se pudo abrir " + puerto + ": " + e.GetBaseException().Message); }
        }

        void AbrirOSC()
        {
            try
            {
                receptorOSC = new UdpClient(puertoOSC);
                receptorOSC.Client.Blocking = false;
                Debug.Log("Receptor OSC escuchando en UDP " + puertoOSC +
                    (string.IsNullOrWhiteSpace(ipTelefonoOSC) ? " (cualquier IP)" : "; celular " + ipTelefonoOSC));
            }
            catch (Exception e)
            {
                Debug.LogWarning("No se pudo abrir el receptor OSC en el puerto " + puertoOSC + ": " + e.Message);
                CerrarOSC();
            }
        }

        void LeerOSC()
        {
            if (receptorOSC == null) return;
            try
            {
                while (receptorOSC.Available > 0)
                {
                    IPEndPoint origen = new IPEndPoint(IPAddress.Any, 0);
                    byte[] paquete = receptorOSC.Receive(ref origen);
                    if (!string.IsNullOrWhiteSpace(ipTelefonoOSC) && origen.Address.ToString() != ipTelefonoOSC.Trim())
                        continue;
                    ProcesarPaqueteOSC(paquete);
                    if (!registroPrimerPaqueteOSC)
                    {
                        registroPrimerPaqueteOSC = true;
                        Debug.Log("Datagrama OSC recibido desde " + origen.Address + ". Revisá la ruta del sensor si la mira no responde.");
                    }
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                    Debug.LogWarning("Error recibiendo OSC: " + e.Message);
            }
            catch (Exception e) { Debug.LogWarning("Paquete OSC inválido: " + e.Message); }
        }

        void ProcesarPaqueteOSC(byte[] datos)
        {
            if (datos == null || datos.Length < 8) return;
            int indice = 0;
            string ruta = LeerCadenaOSC(datos, ref indice);
            if (ruta == "#bundle") return; // Sensor2OSC envía mensajes individuales, no bundles.
            string tipos = LeerCadenaOSC(datos, ref indice);
            if (string.IsNullOrEmpty(ruta) || string.IsNullOrEmpty(tipos) || tipos[0] != ',') return;

            float[] valores = new float[tipos.Length - 1];
            for (int i = 1; i < tipos.Length; i++)
            {
                switch (tipos[i])
                {
                    case 'f':
                        if (indice + 4 > datos.Length) return;
                        byte[] floatBytes = new byte[4];
                        Buffer.BlockCopy(datos, indice, floatBytes, 0, 4);
                        if (BitConverter.IsLittleEndian) Array.Reverse(floatBytes);
                        valores[i - 1] = BitConverter.ToSingle(floatBytes, 0);
                        indice += 4;
                        break;
                    case 'i':
                        if (indice + 4 > datos.Length) return;
                        byte[] intBytes = new byte[4];
                        Buffer.BlockCopy(datos, indice, intBytes, 0, 4);
                        if (BitConverter.IsLittleEndian) Array.Reverse(intBytes);
                        valores[i - 1] = BitConverter.ToInt32(intBytes, 0);
                        indice += 4;
                        break;
                    case 'T': valores[i - 1] = 1f; break;
                    case 'F': valores[i - 1] = 0f; break;
                    default: return;
                }
            }

            string normalizada = ruta.ToLowerInvariant().TrimEnd('/');
            if ((normalizada.EndsWith("/accelerometer") || normalizada.EndsWith("/acceleration") ||
                 normalizada.EndsWith("/acc")) && valores.Length >= 3)
            {
                ultimoPaqueteOSC = Time.realtimeSinceStartup;
                accelX = valores[0]; accelY = valores[1]; accelZ = valores[2];
                hayAcelerometro = true;
                ActualizarMiraDesdeAcelerometro();
            }
            else if ((normalizada.EndsWith("/gyroscope") || normalizada.EndsWith("/gyro")) && valores.Length >= 3)
            {
                ultimoPaqueteOSC = Time.realtimeSinceStartup;
                ProcesarGiroscopioValores(valores[0], valores[1], valores[2], giroscopioOSCEnRadianes);
            }
            else if ((normalizada.EndsWith("/shoot") || normalizada.EndsWith("/trigger") || normalizada.EndsWith("/disparo")) && valores.Length > 0)
            {
                ultimoPaqueteOSC = Time.realtimeSinceStartup;
                ActualizarBotonPistola(valores[0] != 0f);
            }
            else if ((normalizada.EndsWith("/reload") || normalizada.EndsWith("/recarga")) && valores.Length > 0)
            {
                ultimoPaqueteOSC = Time.realtimeSinceStartup;
                BotonRecarga = valores[0] != 0f;
            }
        }

        string LeerCadenaOSC(byte[] datos, ref int indice)
        {
            int inicio = indice;
            while (indice < datos.Length && datos[indice] != 0) indice++;
            if (indice >= datos.Length) return "";
            string resultado = Encoding.UTF8.GetString(datos, inicio, indice - inicio);
            indice = (indice + 4) & ~3;
            return resultado;
        }

        void LeerSerial()
        {
            try
            {
                if (serial == null || !(bool)isOpen.GetValue(serial)) return;
                // ReadExisting ya devuelve solo lo disponible; evitar BytesToRead acá
                // elimina una condición de carrera del buffer reflejado en Unity.
                string recibido = (string)readExisting.Invoke(serial, null);
                if (string.IsNullOrEmpty(recibido))
                {
                    if (!avisoSinDatos && Time.realtimeSinceStartup - inicioLecturaSerial > 3f)
                    {
                        avisoSinDatos = true;
                        Debug.LogWarning("COM3 está abierto, pero no llegan datos del Arduino. Cerrá el Monitor Serie y verificá que el sketch esté ejecutándose a 115200 baudios.");
                    }
                    return;
                }
                if (!registroPrimeraTrama)
                {
                    registroPrimeraTrama = true;
                    string muestra = recibido.Replace("\r", "\\r").Replace("\n", "\\n");
                    if (muestra.Length > 160) muestra = muestra.Substring(0, 160);
                    Debug.Log("Arduino COM3 recibió: " + muestra);
                }
                buffer.Append(recibido);
                while (true)
                {
                    string s = buffer.ToString(); int end = s.IndexOf('\n'); if (end < 0) break;
                    string line = s.Substring(0, end).Trim(); buffer.Remove(0, end + 1);
                    if (line.StartsWith("BOTON:")) { BotonRecarga = line.Contains("PRESIONADO"); continue; }
                    if (line.StartsWith("FINAL DE CARRERA:")) { ActualizarBotonPistola(line.Contains("ACTIVADO")); continue; }
                    if (line.StartsWith("Acelerometro:")) { hayAcelerometro = ParsearVector(line, out accelX, out accelY, out accelZ); continue; }
                    if (line.StartsWith("Giroscopio:")) { ProcesarGiroscopio(line); continue; }
                    string[] v = line.Split(','); if ((v.Length < 3 || v.Length > 5) || v[0] != "AIM") continue;
                    float y, p; int disparo = 0, recarga = 0;
                    if (!float.TryParse(v[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y) ||
                        !float.TryParse(v[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out p)) continue;
                    if (v.Length == 4) int.TryParse(v[3], out disparo); // protocolo anterior
                    if (v.Length == 5) { int.TryParse(v[3], out disparo); int.TryParse(v[4], out recarga); }
                    if (!primerAngulo) { ultimoYaw = y; primerAngulo = true; }
                    else { Yaw -= Mathf.Abs(Mathf.DeltaAngle(ultimoYaw, y)) < zonaMuerta ? 0f : Mathf.DeltaAngle(ultimoYaw, y) * sensibilidad; ultimoYaw = y; }
                    ultimoPitchArduino = p;
                    if (!referenciaPitchArduinoLista)
                    {
                        referenciaPitchArduino = p;
                        referenciaPitchArduinoLista = true;
                    }
                    float pitchRelativo = p - referenciaPitchArduino;
                    Pitch = Mathf.Clamp(Mathf.Abs(pitchRelativo) < zonaMuerta ? 0f : pitchRelativo * sensibilidad, -65f, 65f);
                    ActualizarBotonPistola(disparo != 0);
                    BotonRecarga = recarga != 0;
                    TieneMuestra = true;
                    if (Time.unscaledTime >= proximoLogMuestra)
                    {
                        proximoLogMuestra = Time.unscaledTime + 1f;
                        Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "Arduino AIM | yaw {0:F1}°, pitch {1:F1}° | mira {2:F1}, {3:F1}",
                            Yaw, Pitch, 339f + Yaw * 6.78f, 190f - Pitch * 2.9f));
                    }
                }
                if (buffer.Length > 1024) buffer.Clear();
            }
            catch (Exception e) { Debug.LogWarning("Se perdió la conexión con la pistola: " + e.GetBaseException().Message); CerrarPuerto(); }
        }

        bool ParsearVector(string linea, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            int dosPuntos = linea.IndexOf(':');
            if (dosPuntos < 0) return false;
            string datos = linea.Substring(dosPuntos + 1).Trim();
            int unidad = datos.LastIndexOf(' ');
            if (unidad > 0 && (datos.EndsWith("g") || datos.EndsWith("dps")))
                datos = datos.Substring(0, unidad).Trim();
            string[] valores = datos.Split(',');
            return valores.Length == 3 &&
                float.TryParse(valores[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) &&
                float.TryParse(valores[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y) &&
                float.TryParse(valores[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z);
        }

        void ProcesarGiroscopio(string linea)
        {
            if (!hayAcelerometro) return;
            float gx, gy, gz;
            if (!ParsearVector(linea, out gx, out gy, out gz)) return;
            ProcesarGiroscopioValores(gx, gy, gz, false);
        }

        void ProcesarGiroscopioValores(float gx, float gy, float gz, bool enRadianes)
        {
            if (!hayAcelerometro) return;
            if (enRadianes) gy *= Mathf.Rad2Deg;
            float ahora = Time.realtimeSinceStartup;
            float dt = primerGiro ? Mathf.Clamp(ahora - ultimoTiempoIMU, 0f, 0.1f) : 0f;
            yawGiroscopio += gy * dt * sensibilidad;
            primerGiro = true;
            ultimoTiempoIMU = ahora;
            ActualizarMiraDesdeAcelerometro();
        }

        void ActualizarMiraDesdeAcelerometro()
        {
            if (!hayAcelerometro) return;
            float pitchAcelerometro = Mathf.Atan2(accelX, Mathf.Sqrt(accelY * accelY + accelZ * accelZ)) * Mathf.Rad2Deg;
            float rollAcelerometro = Mathf.Atan2(accelY, accelZ) * Mathf.Rad2Deg;
            if (!referenciaIMU)
            {
                referenciaRoll = rollAcelerometro;
                referenciaPitch = pitchAcelerometro;
                referenciaIMU = true;
            }

            float deltaYaw = Mathf.DeltaAngle(referenciaRoll, rollAcelerometro);
            float deltaPitch = pitchAcelerometro - referenciaPitch;
            Yaw = Mathf.Clamp(-((Mathf.Abs(deltaYaw) < zonaMuerta ? 0f : deltaYaw * sensibilidad * 2.2f) + yawGiroscopio), -50f, 50f);
            Pitch = Mathf.Clamp(Mathf.Abs(deltaPitch) < zonaMuerta ? 0f : deltaPitch * sensibilidad * 2.2f, -65f, 65f);
            TieneMuestra = true;
            if (Time.unscaledTime >= proximoLogMuestra)
            {
                proximoLogMuestra = Time.unscaledTime + 1f;
                Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Arduino MPU6500 | accel {0:F2}, {1:F2}, {2:F2} g | yaw {3:F1}°, pitch {4:F1}° | mira {5:F1}, {6:F1}",
                    accelX, accelY, accelZ, Yaw, Pitch, 339f + Yaw * 6.78f, 190f - Pitch * 2.9f));
            }
        }

        void CentrarMira()
        {
            if (hayAcelerometro)
            {
                referenciaRoll = Mathf.Atan2(accelY, accelZ) * Mathf.Rad2Deg;
                referenciaPitch = Mathf.Atan2(accelX, Mathf.Sqrt(accelY * accelY + accelZ * accelZ)) * Mathf.Rad2Deg;
                referenciaIMU = true;
            }
            if (referenciaPitchArduinoLista)
                referenciaPitchArduino = ultimoPitchArduino;

            yawGiroscopio = 0f;
            Yaw = 0f;
            Pitch = 0f;
            ultimoTiempoIMU = Time.realtimeSinceStartup;
            Debug.Log("Centro de la mira recalibrado con T");
        }

        void CerrarPuerto()
        {
            try { if (serial != null && isOpen != null && (bool)isOpen.GetValue(serial)) close.Invoke(serial, null); } catch { }
            serialActivo = false;
        }

        void CerrarOSC()
        {
            try { if (receptorOSC != null) receptorOSC.Close(); } catch { }
            receptorOSC = null;
        }

        void OnDestroy() { CerrarPuerto(); CerrarOSC(); }
    }
}
