# Ferretería ERP

## 1. Descripción del sistema

**Ferretería ERP** es un sistema de gestión empresarial orientado a la administración integral de una ferretería con múltiples sucursales. Su objetivo es centralizar y optimizar los procesos de ventas, inventario, proveedores, logística, recursos humanos y gestión de créditos y cobranzas.

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

El proyecto contempla los siguientes requisitos:

| Categoría
