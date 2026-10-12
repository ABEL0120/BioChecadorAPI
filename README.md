# BioChecador API 🕒🔐

API principal del sistema **Reloj Checador Biométrico**, diseñada para la gestión integral de asistencia de personal, autenticación sin contraseña mediante WebAuthn/FIDO2, control de geolocalización, administración de incidencias/vacaciones y orquestación de servicios empresariales.

---

## 🚀 Características Principales

- **Autenticación Biométrica WebAuthn (FIDO2):** Enrolamiento y validación de huella/FaceID sin almacenar datos biométricos sensibles en el servidor.
- **Validación de Asistencia y Geolocalización:** Verificación en tiempo real de tolerancia por radio de metros respecto a la sucursal o centro de trabajo.
- **Control de Turnos y Tolerancias:** Evaluación de retardos, descansos, salidas a comida y horarios laborales personalizados.
- **Módulo de Vacaciones:** Cálculo automático de periodos, días hábiles, fechas restrictivas y generación de solicitudes para visto bueno de supervisores.
- **Notificaciones por Correo:** Envío automatizado de avisos para solicitudes y aprobaciones vía SMTP.
- **Consumo de Microservicios:** Conexión con `NominaDocApi` para la consulta de recibos de nómina.

---

## 🛠️ Stack Tecnológico

- **Framework:** ASP.NET Core 8.0 Web API
- **Lenguaje:** C# 12
- **Base de Datos:** Microsoft SQL Server (`2020_NOM_WEB`)
- **Autenticación FIDO2:** `Fido2.NetLib`
- **Documentación:** Swagger / OpenAPI

---

## ⚙️ Configuración del Entorno

### 1. Clonar el Repositorio
```bash
git clone https://github.com/TU_USUARIO/BioChecadorApi.git
cd BioChecadorApi
```

### 2. Archivo de Configuración Local
Copia la plantilla de ejemplo para generar tu archivo de configuración:
```bash
cp appsettings.example.json appsettings.json
```

Edita `appsettings.json` con los valores de tu entorno:
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=TU_SERVIDOR;Database=2020_NOM_WEB;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=False;"
  },
  "Microservicios": {
    "NominaDocApiUrl": "http://localhost:5054"
  },
  "Mail": {
    "Email": "tu_correo@gmail.com",
    "PasswordApp": "tu_password_de_aplicacion"
  },
  "Fido2": {
    "ServerDomain": "localhost",
    "ServerName": "BioChecador PWA",
    "Origins": [
      "http://localhost:5173",
      "http://localhost:8100",
      "https://localhost:5173",
      "https://localhost:8100"
    ],
    "TimestampCounterTolerance": 0
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 📡 Endpoints Clave

| Método | Ruta | Descripción |
| :--- | :--- | :--- |
| `POST` | `/api/checador/verificar-rfc` | Consulta el estado, turno y biometría del empleado por RFC. |
| `POST` | `/api/biometria/desafio` | Genera el desafío criptográfico FIDO2 (Enrolar o Marcar). |
| `POST` | `/api/biometria/enrolar` | Registra la clave pública biométrica del dispositivo. |
| `POST` | `/api/checador/marcar` | Registra la checada validando coordenadas GPS y biometría. |
| `POST` | `/api/checador/historico` | Consulta el historial de marcajes de asistencia. |
| `GET`  | `/api/vacaciones/inicial/{compania}/{m103}` | Obtiene saldo de periodos y fechas restrictivas. |
| `POST` | `/api/vacaciones/calcular-fechas` | Calcula fecha de fin y retorno según días hábiles. |
| `POST` | `/api/vacaciones/solicitud` | Registra una nueva solicitud de vacaciones. |
| `POST` | `/api/solicitudes/reinicio` | Solicita autorización para registrar un nuevo dispositivo. |

---

## 🏃‍♂️ Ejecución y Pruebas

```bash
# Restaurar dependencias
dotnet restore

# Compilar proyecto
dotnet build

# Ejecutar API
dotnet run
```