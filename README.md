# Reparaciones Flash
## Sistema Web de Gestión de Reparaciones Técnicas

### Descripción del Proyecto

**Reparaciones Flash** es un sistema web diseñado para optimizar la gestión de un taller de reparación de equipos electrónicos, como laptops, computadoras de escritorio y consolas de videojuegos.

El sistema permite administrar clientes, registrar equipos, crear órdenes de trabajo, realizar diagnósticos técnicos, gestionar repuestos, consultar estados de reparación, controlar pagos y generar comprobantes digitales.

Además, proporciona un panel administrativo con reportes semanales y mensuales para facilitar la toma de decisiones.

### Objetivo General

Desarrollar un sistema web que permita digitalizar y automatizar los procesos de recepción, diagnóstico, reparación y entrega de equipos electrónicos, mejorando la eficiencia operativa, la seguridad de la información y la atención al cliente.

---

## 1. Actores del Sistema

### 1.1. Recepcionista
- Registra clientes.
- Abre órdenes de trabajo.
- Registra los datos de los equipos y las fallas reportadas.
- Registra los pagos.
- Genera recibos digitales.
- Cierra las órdenes de trabajo.

### 1.2. Técnico
- Diagnostica los equipos asignados.
- Actualiza el estado de las órdenes.
- Registra notas técnicas.
- Lista los repuestos necesarios y sus costos.

### 1.3. Cliente
- Consulta el estado de su reparación mediante su número de carnet o código de ticket.
- Visualiza el presupuesto estimado.
- Acepta o rechaza presupuestos en línea.

### 1.4. Dueño / Administrador
- Accede al panel de control.
- Visualiza reportes semanales y mensuales.
- Consulta equipos atendidos, productividad de técnicos, ingresos totales y repuestos más solicitados.
- Administra el acceso al sistema.

---

## 2. Requisitos Funcionales (RF)

Los requisitos funcionales describen las acciones que debe realizar el sistema.

| Código | Requisito Funcional |
|---|---|
| RF-01 | El sistema debe permitir a la recepcionista registrar clientes y abrir órdenes de trabajo, ingresando marca, modelo, número de serie, accesorios y falla reportada. |
| RF-02 | El sistema debe permitir al técnico cambiar el estado de una orden a "En diagnóstico", "Esperando repuestos", "Reparado" o "Entregado". |
| RF-03 | El sistema debe permitir al técnico registrar notas técnicas detalladas sobre el diagnóstico y la reparación. |
| RF-04 | El sistema debe permitir al técnico listar los repuestos necesarios junto con sus respectivos costos para calcular el presupuesto total. |
| RF-05 | El sistema debe permitir al cliente consultar el estado de su reparación mediante su número de carnet o código de ticket. |
| RF-06 | El sistema debe mostrar al cliente el presupuesto estimado de la reparación durante la consulta en línea. |
| RF-07 | El sistema debe permitir al cliente aceptar o rechazar el presupuesto en línea cuando el costo supere lo previsto. |
| RF-08 | El sistema debe permitir a la recepcionista registrar el método de pago (efectivo, tarjeta o transferencia) al momento de la entrega. |
| RF-09 | El sistema debe generar una nota de venta o recibo digital en formato PDF al cerrar la orden. |
| RF-10 | El sistema debe permitir a la recepcionista cerrar la orden de trabajo una vez completado el pago. |
| RF-11 | El sistema debe registrar automáticamente la fecha y hora de finalización al cerrar la orden. |
| RF-12 | El sistema debe calcular automáticamente una garantía de 90 días sobre el servicio realizado a partir de la fecha de cierre. |
| RF-13 | El sistema debe permitir al administrador visualizar reportes semanales y mensuales de equipos atendidos, técnicos productivos, ingresos totales y repuestos más solicitados. |
| RF-14 | El sistema debe solicitar nombre de usuario y contraseña para que cada actor inicie sesión. |
| RF-15 | El sistema debe validar que únicamente los usuarios con rol de Administrador puedan acceder al panel de reportes. |

---

## 3. Requisitos No Funcionales (RNF)

Los requisitos no funcionales establecen las condiciones de calidad, seguridad, rendimiento y funcionamiento del sistema.

| Código | Requisito No Funcional |
|---|---|
| RNF-01 | El sistema debe soportar al menos 30 usuarios concurrentes consultando sus tickets simultáneamente sin degradación del servicio ni caídas. |
| RNF-02 | Las búsquedas de estado de tickets y la generación de reportes deben responder en menos de 2 segundos. |
| RNF-03 | El sistema debe almacenar las contraseñas de los usuarios cifradas utilizando un algoritmo robusto, como bcrypt o SHA-256, según lo establecido en el documento. |
| RNF-04 | El sistema debe implementar control de acceso basado en roles (RBAC), permitiendo que cada usuario acceda únicamente a las funcionalidades autorizadas. |
| RNF-05 | El sistema debe garantizar el tratamiento confidencial y seguro de los datos personales de los clientes, considerando la normativa indicada en el documento. |
| RNF-06 | La plataforma debe ser responsiva y visualizarse correctamente en navegadores de escritorio y dispositivos móviles iOS y Android. |
| RNF-07 | Los recibos digitales deben generarse en formato PDF compatible con el estándar PDF/A. |
| RNF-08 | El sistema debe mantener una disponibilidad del 99.5% durante el horario laboral de 08:00 a 20:00. |
| RNF-09 | El sistema debe realizar copias de seguridad automáticas de la base de datos al menos una vez al día. |
| RNF-10 | El sistema debe registrar un log de auditoría inmutable de las modificaciones de las órdenes, identificando quién realizó el cambio, cuándo y qué modificó. |
| RNF-11 | La recepcionista debe poder completar el registro de una nueva orden de trabajo en un máximo de 3 minutos. |
| RNF-12 | El recibo digital debe reflejar claramente los términos y las fechas de la garantía de 90 días. |
| RNF-13 | El código fuente debe seguir estándares de codificación y estar debidamente documentado para facilitar su mantenimiento. |
| RNF-14 | El sistema debe utilizar protocolos de comunicación seguros HTTPS/TLS 1.2 o superior para proteger los datos transmitidos. |
| RNF-15 | El sistema debe contar con una arquitectura escalable que permita aumentar la cantidad de usuarios concurrentes y el volumen de datos sin una reestructuración total. |

---

## 4. Módulos Principales

- **Gestión de Clientes:** Registro y administración de información de clientes.
- **Gestión de Órdenes de Trabajo:** Registro, seguimiento y cierre de reparaciones.
- **Módulo Técnico:** Diagnósticos, notas técnicas, estados y repuestos.
- **Consulta de Reparaciones:** Seguimiento en línea mediante carnet o código de ticket.
- **Gestión de Presupuestos:** Visualización y aceptación o rechazo de presupuestos.
- **Gestión de Pagos:** Registro de pagos y métodos de pago.
- **Generación de Comprobantes:** Emisión de recibos digitales PDF.
- **Gestión de Garantías:** Cálculo automático de garantía de 90 días.
- **Panel Administrativo:** Reportes de gestión, productividad e ingresos.
- **Seguridad y Usuarios:** Autenticación y control de acceso por roles.

---

## 5. Alcance del Sistema

El sistema contempla el ciclo completo de reparación de equipos electrónicos, desde su recepción en el taller hasta su entrega al cliente.

Se busca reemplazar los registros manuales en cuadernos y notas adhesivas por una plataforma digital centralizada, segura, accesible y eficiente.

---

## 6. Requisitos de Calidad

El sistema debe cumplir con las siguientes características:

- Seguridad de la información.
- Rendimiento y rapidez de respuesta.
- Disponibilidad y confiabilidad.
- Usabilidad y diseño responsivo.
- Mantenibilidad del código.
- Escalabilidad.
- Trazabilidad de las operaciones.

---

## 7. Proyecto Académico

**Caso de estudio:** Reparaciones Flash  
**Materia:** Desarrollo de Sistemas II  
**Universidad:** Universidad Privada Domingo Savio (UPDS)  
**Ubicación:** Tarija, Bolivia  
**Gestión:** Noviembre 2026
