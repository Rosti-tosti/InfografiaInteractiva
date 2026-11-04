#include <MPU6500-DMP.h>
#include <math.h>

#if defined(SAMD)
#define SerialPort SerialUSB
#else
#define SerialPort Serial
#endif

// Sensor y conexiones del control existente.
MPU6500_DMP imu;
const int pinFinalCarrera = 2; // Disparo: interruptor entre pin 2 y GND
const int pinBoton = 3;        // Recarga: botón entre pin 3 y GND

// El MPU6500 entrega datos a 100 Hz. Se usa micros() para integrar el giro
// con el tiempo real entre muestras, aunque alguna lectura llegue tarde.
const unsigned long intervaloMuestraUs = 10000UL;
unsigned long ultimaMuestraUs = 0;

// Antirrebote para los dos interruptores mecánicos.
const unsigned long antirreboteMs = 15;
int finalCrudoAnterior = HIGH;
int finalEstable = HIGH;
unsigned long cambioFinalMs = 0;
int botonCrudoAnterior = HIGH;
int botonEstable = HIGH;
unsigned long cambioBotonMs = 0;

// Filtrado de inclinación y deriva pequeña del giroscopio.
const float correccionAcelerometro = 0.015f;
const float zonaMuertaGiroscopioDps = 0.35f;
float pitchGrados = 0.0f;
float yawGrados = 0.0f;
bool primeraMuestra = true;

void actualizarBotones(unsigned long ahoraMs)
{
  int finalCrudo = digitalRead(pinFinalCarrera);
  if (finalCrudo != finalCrudoAnterior)
  {
    finalCrudoAnterior = finalCrudo;
    cambioFinalMs = ahoraMs;
  }
  if (ahoraMs - cambioFinalMs >= antirreboteMs)
    finalEstable = finalCrudoAnterior;

  int botonCrudo = digitalRead(pinBoton);
  if (botonCrudo != botonCrudoAnterior)
  {
    botonCrudoAnterior = botonCrudo;
    cambioBotonMs = ahoraMs;
  }
  if (ahoraMs - cambioBotonMs >= antirreboteMs)
    botonEstable = botonCrudoAnterior;
}

void setup()
{
  SerialPort.begin(115200);

  pinMode(pinFinalCarrera, INPUT_PULLUP);
  pinMode(pinBoton, INPUT_PULLUP);

  if (imu.begin() != INV_SUCCESS)
  {
    SerialPort.println("ERROR: MPU6500 no detectado");
    while (1) delay(1000);
  }

  imu.setSensors(INV_XYZ_GYRO | INV_XYZ_ACCEL);
  imu.setGyroFSR(2000); // Conserva el rango del sketch funcional original.
  imu.setAccelFSR(2);
  imu.setLPF(10);       // Reduce vibración con menos demora que el filtro de 5 Hz.
  imu.setSampleRate(100);

  finalCrudoAnterior = finalEstable = digitalRead(pinFinalCarrera);
  botonCrudoAnterior = botonEstable = digitalRead(pinBoton);
  ultimaMuestraUs = micros();
}

void loop()
{
  // Se leen los botones en cada vuelta, no solo cuando el sensor actualiza.
  actualizarBotones(millis());

  if (!imu.dataReady()) return;
  imu.update(UPDATE_ACCEL | UPDATE_GYRO);

  unsigned long ahoraUs = micros();
  unsigned long transcurridoUs = ahoraUs - ultimaMuestraUs;
  if (transcurridoUs < intervaloMuestraUs) return;

  float dt = transcurridoUs / 1000000.0f;
  ultimaMuestraUs = ahoraUs;

  float accelX = imu.calcAccel(imu.ax);
  float accelY = imu.calcAccel(imu.ay);
  float accelZ = imu.calcAccel(imu.az);
  float gyroX = imu.calcGyro(imu.gx);
  float gyroY = imu.calcGyro(imu.gy);

  if (fabsf(gyroX) < zonaMuertaGiroscopioDps) gyroX = 0.0f;
  if (fabsf(gyroY) < zonaMuertaGiroscopioDps) gyroY = 0.0f;

  float pitchAcelerometro =
    atan2f(accelX, sqrtf(accelY * accelY + accelZ * accelZ)) * 57.29578f;

  if (primeraMuestra)
  {
    pitchGrados = pitchAcelerometro;
    primeraMuestra = false;
  }
  else
  {
    // El giroscopio sigue los movimientos rápidos; el acelerómetro corrige
    // lentamente la inclinación para reducir el temblor acumulado.
    pitchGrados = (1.0f - correccionAcelerometro) * (pitchGrados + gyroX * dt)
                + correccionAcelerometro * pitchAcelerometro;
  }

  // El MPU6500 no tiene magnetómetro: yaw se integra desde el giroscopio.
  yawGrados += gyroY * dt;
  if (yawGrados > 180.0f) yawGrados -= 360.0f;
  if (yawGrados < -180.0f) yawGrados += 360.0f;

  int disparo = finalEstable == LOW ? 1 : 0;
  int recarga = botonEstable == LOW ? 1 : 0;

  // Protocolo que Unity lee a 115200 baudios:
  // AIM,yaw,pitch,disparo,recarga
  SerialPort.print("AIM,");
  SerialPort.print(yawGrados, 2);
  SerialPort.print(',');
  SerialPort.print(pitchGrados, 2);
  SerialPort.print(',');
  SerialPort.print(disparo);
  SerialPort.print(',');
  SerialPort.println(recarga);
}
