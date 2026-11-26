# Ferretería DCS

Sistema de gestión empresarial de escritorio para una ferretería con múltiples sucursales: ventas, inventario, proveedores, logística, recursos humanos y créditos y cobranzas.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)
![Avalonia](https://img.shields.io/badge/UI-Avalonia%2012-8B44AC)
![PostgreSQL](https://img.shields.io/badge/DB-PostgreSQL-336791)
![EF Core](https://img.shields.io/badge/ORM-EF%20Core%209-512BD4)

## Contenido

1. [Descripción del sistema](#1-descripción-del-sistema)
2. [Actores del sistema](#2-actores-del-sistema)
3. [Requisitos funcionales (RF)](#3-requisitos-funcionales-rf)
4. [Requisitos no funcionales (RNF)](#4-requisitos-no-funcionales-rnf)
5. [Resumen general de requisitos](#5-resumen-general-de-requisitos)
6. [Tecnologías](#6-tecnologías)
7. [Arquitectura](#7-arquitectura)
8. [Instalación y puesta en marcha](#8-instalación-y-puesta-en-marcha)
9. [Configuración Central / Sucursal](#9-configuración-central--sucursal)
10. [Base de datos y migraciones](#10-base-de-datos-y-migraciones)
11. [Pruebas](#11-pruebas)
12. [Seguridad](#12-seguridad)
13. [Funcionalidades adicionales](#13-funcionalidades-adicionales)

---

## 1. Descripción del sistema

**Ferretería DCS** es un sistema de gestión empresarial orientado a la administración integral de una ferretería con múltiples sucursales. Su objetivo es centralizar y optimizar los procesos de ventas, inventario, proveedores, logística, recursos humanos y gestión de créditos y cobranzas.

El sistema permitirá controlar las operaciones comerciales, gestionar el movimiento de mercadería, realizar el seguimiento de las cuentas por cobrar y generar información estadística para apoyar la toma de decisiones.

---

## 2. Actores del sistema

Los actores representan a las personas o entidades que interactúan directa o indirectamente con las funcionalidades del sistema.

### 2.1. Actores internos

| Actor | Descripción y responsabilidades |
|---|---|
| **Super Administrador / Gerente Central** | Tiene acceso total al sistema. Puede visualizar información de todas las sucursales, configurar banners de promociones y consultar la clasificación (Tier List) del rendimiento de los empleados. |
| **Cajero / Vendedor** | Opera el módulo de Punto de Venta (POS), gestiona los carritos de compras mediante lector de códigos de barras o catálogo visual, registra métodos de pago, procesa ventas a crédito y participa en el sistema de comisiones. |
| **Encargado de Almacén / Logística** | Gestiona el catálogo de productos, actualiza el inventario, controla las cargas entrantes (recepción de flotas y camiones) y coordina las órdenes de despacho de mercadería hacia las obras. |

### 2.2. Actores externos

| Actor | Descripción y responsabilidades |
|---|---|
| **Cliente** | Entidad que realiza compras al contado o a crédito. Interactúa con el sistema a través del cajero o vendedor y recibe recordatorios de pago mediante WhatsApp. |
| **Proveedor** | Persona o entidad jurídica que suministra mercadería. Tiene asociadas políticas de garantía, condiciones de flete, documentación contractual y recibe solicitudes de reabastecimiento mediante WhatsApp. |
| **Transportista / Chofer (Externo)** | Responsable del traslado de mercadería, por ejemplo, mediante sindicatos de camioneros o empresas de transporte. Sus datos se registran para calcular los fletes en destino y los costos de envío hacia las obras. |

---

## 3. Requisitos funcionales (RF)

Los requisitos funcionales describen las operaciones y servicios que el sistema debe proporcionar para satisfacer las necesidades del negocio.

### 3.1. Módulo de Inicio (Dashboard)

| Código | Descripción |
|---|---|
| **RF01** | El sistema debe calcular y mostrar los ingresos diarios y mensuales, considerando la suma de las ventas al contado y los abonos correspondientes a créditos. |
| **RF02** | El sistema debe mostrar tarjetas de alerta que permitan identificar las cargas entrantes pendientes y los despachos a obras que se encuentren activos. |
| **RF03** | El sistema debe generar un Top 7 de los productos más vendidos durante el mes actual. |
| **RF04** | El sistema debe generar alertas sobre los créditos vencidos o próximos a vencer, considerando un plazo igual o inferior a tres (3) días. |
| **RF05** | El sistema debe permitir configurar y visualizar un banner promocional con las promociones vigentes, incluyendo sus respectivas fechas de inicio y finalización. |

### 3.2. Módulo de Punto de Venta (POS)

| Código | Descripción |
|---|---|
| **RF06** | El sistema debe permitir procesar ventas mediante la búsqueda de productos utilizando un lector de códigos de barras, con incorporación automática del producto al presionar Enter, o mediante un catálogo visual interactivo. |
| **RF07** | El sistema debe exigir la selección de un empleado con rol de vendedor antes de finalizar una venta, con el propósito de registrar correctamente las comisiones correspondientes. |
| **RF08** | El sistema debe permitir realizar cobros mediante los siguientes métodos de pago: efectivo, QR o crédito. |
| **RF09** | Al registrar una venta a crédito, el sistema debe permitir establecer una fecha límite de pago personalizada. |

### 3.3. Módulo de Inventario

| Código | Descripción |
|---|---|
| **RF10** | El sistema debe permitir registrar productos incluyendo código de barras, nombre, categorías, precios y una fotografía seleccionada desde los archivos locales del equipo. |
| **RF11** | El sistema debe enviar un mensaje predeterminado de solicitud de reabastecimiento al proveedor mediante WhatsApp cuando un producto alcance su nivel mínimo de stock. |
| **RF12** | El sistema debe permitir la importación masiva de productos mediante archivos en formato CSV. |

### 3.4. Módulo de Proveedores

| Código | Descripción |
|---|---|
| **RF13** | El sistema debe almacenar información fiscal y comercial detallada de los proveedores, incluyendo NIT, nombre comercial, formas de pago aceptadas y monedas de facturación. |
| **RF14** | El sistema debe permitir registrar las políticas operativas de cada proveedor, incluyendo tiempos de entrega, condiciones de flete y políticas de garantía y devolución. |
| **RF15** | El sistema debe permitir adjuntar, almacenar y visualizar documentos locales asociados a los proveedores, en formatos PDF y DOCX. |

### 3.5. Módulo de Logística y Cargas

| Código | Descripción |
|---|---|
| **RF16** | El sistema debe permitir registrar cargas entrantes, especificando la guía de remisión, la empresa de transporte y los costos correspondientes al flete en destino. |
| **RF17** | El sistema debe actualizar automáticamente las existencias del inventario únicamente cuando una carga entrante cambie al estado **Recibido**. |
| **RF18** | El sistema debe permitir generar órdenes de despacho para entregas de mercadería a obras, contemplando la posibilidad de registrar entregas parciales y sus respectivos costos de transporte. |

### 3.6. Módulo de Recursos Humanos

| Código | Descripción |
|---|---|
| **RF19** | El sistema debe permitir registrar los datos personales y de contacto de cada empleado, su sucursal asignada y su respectiva fotografía. |
| **RF20** | El sistema debe implementar un mecanismo de borrado lógico para los empleados que renuncien o dejen de trabajar en la empresa, inhabilitando sus cuentas y trasladándolos a una lista de ex-trabajadores, con el fin de preservar el historial de ventas. |
| **RF21** | El sistema debe generar automáticamente una clasificación (Tier List o ranking) de los empleados activos, tomando como criterios el volumen y el monto total de sus ventas. |

### 3.7. Módulo de Créditos y Cobranzas

| Código | Descripción |
|---|---|
| **RF22** | El sistema debe gestionar las cuentas por cobrar, permitiendo registrar abonos parciales o realizar la liquidación total de una deuda. |
| **RF23** | El sistema debe recalcular y mostrar automáticamente el saldo pendiente de cada crédito después de registrar un abono. |
| **RF24** | El sistema debe permitir redirigir al usuario hacia un chat de WhatsApp del cliente para facilitar las gestiones de cobranza. |

---

## 4. Requisitos no funcionales (RNF)

Los requisitos no funcionales establecen las condiciones técnicas, arquitectónicas, de rendimiento, seguridad y usabilidad que debe cumplir el sistema.

### 4.1. Arquitectura y tecnología

| Código | Descripción |
|---|---|
| **RNF01** | El sistema debe desarrollarse como una aplicación de escritorio multiplataforma, utilizando el lenguaje de programación C# y el framework .NET 9. |
| **RNF02** | La interfaz gráfica de usuario (GUI) debe implementarse utilizando Avalonia UI. |
| **RNF03** | La arquitectura del proyecto debe respetar el patrón de desarrollo N-Capas, organizando la solución en los componentes Core, Infrastructure y UI.Desktop. Asimismo, debe implementar el patrón de diseño MVVM (Model-View-ViewModel) para separar la lógica de presentación de la interfaz gráfica. |

### 4.2. Base de datos y almacenamiento

| Código | Descripción |
|---|---|
| **RNF04** | El sistema debe utilizar PostgreSQL como motor de base de datos relacional, gestionado mediante Entity Framework Core y aplicando el enfoque de desarrollo Code-First. |
| **RNF05** | Los archivos multimedia, como fotografías de productos y empleados, así como los documentos adjuntos de proveedores, deben almacenarse y gestionarse localmente de forma segura en el directorio `%APPDATA%\FerreteriaERP\`. |

### 4.3. Usabilidad e interfaz de usuario (UI/UX)

| Código | Descripción |
|---|---|
| **RNF06** | La interfaz de usuario debe presentar un diseño visual moderno y minimalista en modo claro (Light Mode), basado en tarjetas corporativas con esquinas redondeadas y sombras suaves. |
| **RNF07** | El sistema debe implementar componentes visuales nativos y evitar librerías de terceros que puedan generar conflictos durante la renderización de gráficos estadísticos, especialmente dependencias cruzadas con SkiaSharp. |
| **RNF08** | El sistema debe proporcionar retroalimentación visual al usuario mediante notificaciones emergentes no intrusivas (estilo Toast o SweetAlert). |
| **RNF09** | Las pantallas que presenten información densa deben organizar su contenido mediante cuadrículas adaptativas (UniformGrid o proporciones fijas en columnas), evitando el desbordamiento de los elementos visuales. |

### 4.4. Rendimiento y seguridad

| Código | Descripción |
|---|---|
| **RNF10** | Las consultas a la base de datos destinadas a generar los reportes del Dashboard deben ejecutarse de manera asíncrona y materializar sus resultados en memoria, evitando bloqueos del hilo principal de la interfaz de usuario. |
| **RNF11** | El sistema debe implementar un mecanismo de aislamiento y filtrado lógico de datos transaccionales según la sucursal asignada al usuario autenticado. Esta restricción debe aplicarse a todos los usuarios, excepto al administrador de la Sucursal Central, quien tendrá acceso a la información consolidada de todas las sucursales. |

---

## 5. Resumen general de requisitos

El sistema define **24 requisitos funcionales** y **11 requisitos no funcionales**.

### 5.1. Requisitos funcionales por módulo

| Módulo | Códigos | Cantidad | Actor principal |
|---|---|:---:|---|
| Inicio (Dashboard) | RF01 – RF05 | 5 | Super Administrador / Gerente Central |
| Punto de Venta (POS) | RF06 – RF09 | 4 | Cajero / Vendedor |
| Inventario | RF10 – RF12 | 3 | Encargado de Almacén / Logística |
| Proveedores | RF13 – RF15 | 3 | Encargado de Almacén / Logística |
| Logística y Cargas | RF16 – RF18 | 3 | Encargado de Almacén / Logística |
| Recursos Humanos | RF19 – RF21 | 3 | Super Administrador / Gerente Central |
| Créditos y Cobranzas | RF22 – RF24 | 3 | Cajero / Vendedor |
| **Total** | **RF01 – RF24** | **24** | |

### 5.2. Requisitos no funcionales por categoría

| Categoría | Códigos | Cantidad |
|---|---|:---:|
| Arquitectura y tecnología | RNF01 – RNF03 | 3 |
| Base de datos y almacenamiento | RNF04 – RNF05 | 2 |
| Usabilidad e interfaz (UI/UX) | RNF06 – RNF09 | 4 |
| Rendimiento y seguridad | RNF10 – RNF11 | 2 |
| **Total** | **RNF01 – RNF11** | **11** |

### 5.3. Integraciones con sistemas externos

| Integración | Requisitos | Uso |
|---|---|---|
| WhatsApp (`wa.me`) | RF11, RF24 | Solicitudes de reabastecimiento a proveedores y gestión de cobranza a clientes |
| Archivos CSV | RF12 | Importación masiva de productos |
| Sistema de archivos local | RF10, RF15, RF19, RNF05 | Fotografías y documentos en `%APPDATA%\FerreteriaERP\` |

---

## 6. Tecnologías

| Componente | Tecnología |
|---|---|
| Lenguaje y plataforma | C# 13 · .NET 9 |
| Interfaz gráfica | Avalonia UI 12 (Fluent, DataGrid) |
| Patrón de presentación | MVVM con CommunityToolkit.Mvvm |
| Acceso a datos | Entity Framework Core 9 (Code-First) · Npgsql |
| Base de datos | PostgreSQL |
| Inyección de dependencias | Microsoft.Extensions.DependencyInjection |
| Gráficos | LiveCharts (SkiaSharp) |
| Documentos PDF | QuestPDF (notas de venta y órdenes de compra) |
| Importación | CsvHelper |
| Pruebas | xUnit · EF Core InMemory · coverlet |

---

## 7. Arquitectura

La solución sigue una arquitectura **N-Capas** con dependencias en un solo sentido (la UI depende de Infrastructure y de Core; Infrastructure depende de Core; Core no depende de ninguna).

```text
FerreteriaERP.sln
├── FerreteriaERP.Core            Dominio: entidades, interfaces, constantes, excepciones y reglas puras
│   ├── Configuration/            Modelo de configuración de conexión
│   ├── Constants/                Estados y métodos de pago
│   ├── Entities/                 Venta, Credito, Producto, Empleado, OrdenDespacho...
│   ├── Enums/                    ModoOperacion (Central / Sucursal)
│   ├── Exceptions/               StockInsuficienteException
│   ├── Interfaces/               Contratos de repositorios y servicios
│   ├── Security/                 Hashing de contraseñas (PBKDF2-SHA256)
│   └── Services/                 NumeroALetras, SesionActual
│
├── FerreteriaERP.Infrastructure  Acceso a datos y servicios técnicos
│   ├── Data/                     FerreteriaDbContext, fábricas de contexto y semilla inicial
│   ├── Migrations/               Migraciones de EF Core
│   ├── Repositories/             Implementación de los repositorios
│   ├── Services/                 PDF, respaldo, WhatsApp, importación CSV, conexión y sesión
│   └── DependencyInjection.cs    Registro de servicios: services.AddInfrastructure()
│
├── FerreteriaERP.UI.Desktop      Presentación (Avalonia + MVVM)
│   ├── ViewModels/               Un ViewModel por pantalla
│   ├── Views/                    Vistas .axaml
│   ├── Converters/ · Models/     Utilidades de la interfaz
│   └── Program.cs                Raíz de composición (contenedor de DI)
│
└── FerreteriaERP.Tests           Pruebas automatizadas
```

### Decisiones de diseño relevantes

- **Contexto de datos por operación.** Los repositorios no guardan estado: cada operación crea su propio `FerreteriaDbContext` mediante una fábrica que lee la configuración de conexión vigente.
- **Cambio de servidor en caliente.** Al cambiar entre modo Central y Sucursal en la pantalla de configuración, la siguiente operación ya usa la nueva conexión, sin reiniciar la aplicación.
- **Registro de ventas transaccional.** `VentaRepository.RegistrarVentaAsync` recalcula los montos en el servidor, descuenta el stock de la sucursal con una actualización atómica y registra la venta, el crédito (si aplica) y el contador del cliente en una sola transacción. Si falta stock se lanza `StockInsuficienteException` y no se guarda nada.
- **Abonos atómicos.** `CreditoRepository.RegistrarAbonoAsync` descuenta el saldo solo si el crédito está pendiente y el saldo alcanza; al llegar a cero el crédito pasa a *Pagado*.

---

## 8. Instalación y puesta en marcha

### Requisitos previos

- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL (con el servicio en ejecución)
- `pg_dump` accesible (viene con PostgreSQL) para la función de respaldo
- Herramienta de migraciones de EF Core, en versión 9:

```powershell
dotnet tool install --global dotnet-ef --version 9.0.0
```

### Pasos

```powershell
# 1. Clonar o descomprimir el proyecto y entrar a la carpeta de la solución
cd "Programa Ferreteria"

# 2. Restaurar y compilar
dotnet restore
dotnet build

# 3. Crear la base de datos y aplicar las migraciones (ver sección 10)
dotnet ef database update --project FerreteriaERP.Infrastructure --startup-project FerreteriaERP.UI.Desktop

# 4. Ejecutar la aplicación
dotnet run --project FerreteriaERP.UI.Desktop
```

> `dotnet run` debe indicar el proyecto con `--project`, porque en la carpeta de la solución no hay un `.csproj`.

### Primer inicio de sesión

Al iniciar por primera vez sobre una base vacía, el sistema crea una sucursal central y un usuario administrador inicial:

| Campo | Valor |
|---|---|
| Usuario (CI) | `admin` |
| Contraseña | `admin123` |

> **Importante:** cambia esta contraseña en cuanto ingreses. Es una credencial de demostración.

---

## 9. Configuración Central / Sucursal

Cada instalación se configura desde la pantalla de configuración inicial:

| Modo | Descripción |
|---|---|
| **Central** | La Sucursal Central aloja la base de datos; se conecta a `localhost`. |
| **Sucursal** | Las sucursales secundarias se conectan a la IP (VPN, por ejemplo Tailscale o ZeroTier) del equipo Central. |

Parámetros: servidor, puerto (por defecto `5432`), base de datos (por defecto `FerreteriaDSS`), usuario y contraseña. Incluye un botón **Probar conexión**.

La configuración se guarda **localmente en cada equipo**, no en la base de datos:

```text
%APPDATA%\FerreteriaERP\conexion.json
```

> Si la base de datos aloja información real, protege ese archivo: la contraseña se guarda en texto plano (ver sección 12).

---

## 10. Base de datos y migraciones

El esquema se gestiona con **Code-First**. Las migraciones están en `FerreteriaERP.Infrastructure/Migrations`.

```powershell
# Aplicar las migraciones existentes
dotnet ef database update --project FerreteriaERP.Infrastructure --startup-project FerreteriaERP.UI.Desktop

# Crear una migración nueva después de cambiar una entidad
dotnet ef migrations add NombreDeLaMigracion --project FerreteriaERP.Infrastructure --startup-project FerreteriaERP.UI.Desktop
```

> **Conexión de las migraciones.** `dotnet ef` no usa `conexion.json`: usa `FerreteriaDbContextFactory`, que apunta a `localhost:5432`, base `FerreteriaDSS`, usuario `postgres`. Ajusta esa cadena si tu PostgreSQL tiene otras credenciales.

> **Nombres de tablas.** Las tablas principales (`Usuarios`, `Ventas`, `Productos`, `Creditos`...) conservan mayúsculas y las compuestas usan `snake_case` (`stock_sucursales`, `detalle_ventas`, `abonos_credito`...). En PostgreSQL, las primeras deben consultarse entre comillas dobles:
>
> ```sql
> SELECT * FROM "Usuarios";
> ```

### Respaldo

La aplicación genera copias de seguridad completas (`.backup`, formato personalizado de `pg_dump`) desde la pantalla de configuración.

---

## 11. Pruebas

```powershell
dotnet test
```

Estado actual: el proyecto `FerreteriaERP.Tests` cubre el inicio de sesión, el hashing de contraseñas y la migración automática de contraseñas en texto plano.

Siguientes módulos recomendados para ampliar la cobertura:

| Prioridad | Módulo | Tipo de prueba |
|:---:|---|---|
| 1 | `NumeroALetras`, propiedades calculadas de las entidades | Unitaria, sin dependencias |
| 2 | `VentaRepository.RegistrarVentaAsync`, `CreditoRepository.RegistrarAbonoAsync` | Integración con SQLite en memoria o PostgreSQL (el proveedor *InMemory* de EF no admite transacciones ni `ExecuteUpdate`) |
| 3 | ViewModels (POS, Créditos) | Unitaria, con las interfaces simuladas |

---

## 12. Seguridad

- **Contraseñas de usuarios:** PBKDF2-SHA256 con salt aleatorio de 16 bytes y 100 000 iteraciones, comparación en tiempo constante. Los hashes BCrypt antiguos se siguen aceptando y se migran de forma transparente al iniciar sesión.
- **Control de acceso por rol (RBAC):** los módulos administrativos (Proveedores, Empleados, Gerencia) solo están disponibles para roles administrativos.
- **Aislamiento por sucursal:** las consultas transaccionales se filtran por la sucursal del usuario autenticado.
- **Montos calculados en el servidor:** el registro de ventas no confía en los totales enviados desde la interfaz.

### Limitaciones conocidas

- La contraseña de PostgreSQL se guarda en texto plano en `conexion.json`. Se recomienda cifrarla (por ejemplo con DPAPI en Windows).
- `FerreteriaDbContextFactory` contiene una cadena de conexión de desarrollo con contraseña. No debe usarse con datos reales ni subirse a un repositorio público.
- El usuario `admin` / `admin123` es de demostración y debe cambiarse.
- No existe bloqueo de cuenta por intentos fallidos de inicio de sesión.

---

## 13. Funcionalidades adicionales

Además de los requisitos listados, el sistema incluye:

| Módulo | Descripción |
|---|---|
| Cotizaciones | Proformas a clientes sin afectar stock ni caja |
| Órdenes de compra | Emisión de órdenes a proveedores con generación de PDF |
| Reabastecimiento | Pantalla de productos bajo el stock mínimo |
| Gerencia | Panel de inteligencia gerencial con gráficos y gastos operativos |
| Promociones | Reglas por porcentaje, precio fijo o volumen, por producto, categoría o sucursal |
| Configuración del negocio | Nombre, NIT, logo, datos de contacto, pie de recibo y QR de pago |
| Notas de venta en PDF | Recibos con el monto en letras (`NumeroALetras`) |

## 14. Enlace al Plan de Trabajo

[Planificacion en JIRA](https://omarbd.atlassian.net/jira/software/projects/SFDCS/boards/3/backlog?epics=visible&jql=parent+%3D+SFDCS-2&atlOrigin=eyJpIjoiNzllOTZlOWM1Yzg3NDZmMGFhOWFmMTNmZjE4ZjRhMjUiLCJwIjoiaiJ9)
