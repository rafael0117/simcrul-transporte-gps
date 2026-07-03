# Manual de Uso - SIMCRUL Mantenimiento

## Acceso al sistema

- Web: `http://localhost:5171`
- API Swagger: `http://localhost:5272/swagger`
- Script de arranque: `G:\PROYECTO\PROYECTO\PROYECTO\start-simcrul-local.ps1`

## Credenciales sembradas

- Administrador de Flota
  - Usuario: `admin.flota`
  - Clave: `Admin123!`

- Jefe de Mantenimiento
  - Usuario: `jefe.mantenimiento`
  - Clave: `Jefe123!`

- Tecnico de Mantenimiento
  - Usuario: `tecnico.mantenimiento`
  - Clave: `Tecnico123!`

- Conductor
  - Usuario: `conductor.bus01`
  - Clave: `Conductor123!`

## Flujo por actor

### 1. Administrador de Flota

- Inicia sesion con `admin.flota`.
- Ingresa a `Vehiculos`.
- Registra, edita o desactiva buses.
- Consulta `Historial` para revisar inspecciones, incidencias y ordenes.
- Revisa `Estadisticas` y exporta el reporte CSV de mantenimiento.

### 2. Jefe de Mantenimiento

- Inicia sesion con `jefe.mantenimiento`.
- En `Preventivos`, crea un plan de mantenimiento.
- Al guardar, el sistema genera automaticamente la orden de trabajo.
- En `Ordenes`, puede crear ordenes manuales y usar `Asignar` para designar tecnico.
- Revisa `Incidencias`, `Historial` y `Estadisticas`.

### 3. Tecnico de Mantenimiento

- Inicia sesion con `tecnico.mantenimiento`.
- Abre `Ordenes`.
- Selecciona `Ejecutar` sobre una orden asignada.
- Registra diagnostico, acciones realizadas, repuestos utilizados y nuevo estado operativo del bus.
- Al finalizar:
  - la orden queda en `FINALIZADA`
  - el vehiculo actualiza su estado operativo
  - la incidencia asociada se cierra si corresponde
  - el plan preventivo actualiza su siguiente fecha si la orden fue preventiva

### 4. Conductor

- Inicia sesion con `conductor.bus01`.
- En `Inspecciones`, registra la inspeccion diaria del bus.
- En `Incidencias`, reporta fallas o eventos detectados.
- Si la inspeccion es rechazada o la incidencia requiere parada, la unidad cambia de estado operativo.

## Datos demo sembrados

- Vehiculos demo:
  - `BUS-101 / ABC-101`
  - `BUS-102 / ABC-102`
  - `BUS-103 / ABC-103`

- Datos operativos demo:
  - 2 inspecciones diarias
  - 1 incidencia abierta
  - 1 plan preventivo
  - 3 ordenes de trabajo
  - 1 mantenimiento ejecutado con repuesto

## Casos de uso cubiertos

- `CU-01` Iniciar sesion
- `CU-02` Recuperar contrasena
- `CU-03` Mantener vehiculos
- `CU-04` Registrar inspeccion diaria del bus
- `CU-05` Registrar falla o incidencia
- `CU-06` Programar mantenimiento preventivo
- `CU-07` Generar orden de trabajo
- `CU-08` Asignar tecnico de mantenimiento
- `CU-09` Registrar mantenimiento correctivo
- `CU-10` Registrar repuestos utilizados
- `CU-11` Consultar historial de mantenimiento
- `CU-12` Cambiar estado operativo del bus
- `CU-13` Consultar estadisticas de mantenimiento
- `CU-14` Exportar reportes de mantenimiento

## Base de datos utilizada

- Servidor: `172.20.1.11`
- Base de datos: `SIMCRUL`

## Verificacion realizada

- Compilacion completa de la solucion: correcta
- Creacion de tablas de mantenimiento: correcta
- Insercion de usuarios actor: correcta
- Insercion de datos demo del flujo: correcta
