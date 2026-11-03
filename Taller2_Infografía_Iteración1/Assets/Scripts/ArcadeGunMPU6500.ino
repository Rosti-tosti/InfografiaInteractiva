#include <MPU6500-DMP.h>
#include <math.h>

#if defined(SAMD)
#define SerialPort SerialUSB
#else
#define SerialPort Serial
#endif

MPU6500_DMP imu;
const int pinFinalCarrera = 2; // Disparo
const int pinBotonRecarga = 3; // Recarga
float yawGrados = 0.0f;
float pitchGrados = 0.0f;
unsigned long ultimoMuestreo = 0;
const unsigned long intervaloMs = 10; // 100 Hz

void setup()
{
  SerialPort.begin(115200);
  pinMode(pinFinalCarrera, INPUT_PULLUP);
  pinMode(pinBotonRecarga, INPUT_PULLUP);
  if (imu.begin() != INV_SUCCESS) while (1) delay(1000);

  imu.setSensors(INV_XYZ_GYRO | INV_XYZ_ACCEL);
  imu.setGyroFSR(2000);
  imu.setAccelFSR(2);
  imu.setLPF(20);
  imu.setSampleRate(100);
  ultimoMuestreo = millis();
}

void loop()
{
  if (!imu.dataReady()) return;
  imu.update(UPDATE_ACCEL | UPDATE_GYRO);
  unsigned long ahora = millis();
  float dt = (ahora - ultimoMuestreo) / 1000.0f;
  if (dt < intervaloMs / 1000.0f) return;
  ultimoMuestreo = ahora;

  // Fusion complementaria para inclinaci?n. El yaw se integra desde el giroscopio.
  float ax = imu.calcAccel(imu.ax);
  float ay = imu.calcAccel(imu.ay);
  float az = imu.calcAccel(imu.az);
  float gyroX = imu.calcGyro(imu.gx);
  float gyroY = imu.calcGyro(imu.gy);
  float pitchAcelerometro = atan2f(ax, sqrtf(ay * ay + az * az)) * 57.29578f;
  pitchGrados = 0.98f * (pitchGrados + gyroX * dt) + 0.02f * pitchAcelerometro;
  yawGrados += gyroY * dt;

  int disparo = digitalRead(pinFinalCarrera) == LOW ? 1 : 0;
  int recarga = digitalRead(pinBotonRecarga) == LOW ? 1 : 0;
  // ArcadeGunInput.cs lee: AIM,yaw,pitch,disparo,recarga
  SerialPort.print("AIM,"); SerialPort.print(yawGrados, 2); SerialPort.print(',');
  SerialPort.print(pitchGrados, 2); SerialPort.print(','); SerialPort.print(disparo);
  SerialPort.print(','); SerialPort.println(recarga);
}
