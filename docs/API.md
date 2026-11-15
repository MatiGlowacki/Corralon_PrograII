# Contrato de la API – Corralón

Base URL en desarrollo: `http://localhost:5160` (o `https://localhost:7010`).
Documentación interactiva: `/swagger`.

- Todos los endpoints (salvo `POST /api/auth/login`) requieren el header `Authorization: Bearer <token>`.
- Los JSON usan **camelCase**. Las fechas van en ISO (`2026-11-20T10:30:00`); las de reparto son solo fecha (`2026-11-20`).
- Los errores devuelven `{ "status": 400, "title": "...", "detail": "mensaje para mostrar" }`.
  - `400` regla de negocio o datos inválidos · `401` sin token o credenciales incorrectas · `403` sin permisos · `404` no existe · `409` conflicto en la base.
- Roles: **Administrador** (todo) y **Vendedor** (facturas, repartos, clientes; solo lectura en el resto).

## Seguridad
| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/auth/login` | Body `{ nombreUsuario, password }` → `{ token, expira, idUsuario, nombreUsuario, nombreCompleto, rol }` |
| POST | `/api/auth/registrar` | (Admin) Body `{ nombreUsuario, password, nombreCompleto, email, idRol }` |
| GET | `/api/auth/yo` | Datos del usuario logueado |

Usuarios de prueba (`database/02_seed_usuarios.sql`): `admin / Admin123!` y `vendedor / Vendedor123!`.

## Soporte (ABMC) – lectura: cualquier usuario · escritura: Administrador
Mismo patrón para `/api/formaspago`, `/api/categorias`, `/api/proveedores`, `/api/vehiculos`, `/api/choferes`:

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/formaspago?soloActivos=true` | Lista |
| GET | `/api/formaspago/{id}` | Uno |
| POST | `/api/formaspago` | Alta (devuelve 201 con el registro) |
| PUT | `/api/formaspago/{id}` | Modificación |
| DELETE | `/api/formaspago/{id}` | Baja lógica (queda `activo: false`) |

Campos:
- **formaspago**: `idFormaPago, nombre, descripcion, activo`
- **categorias**: `idCategoria, nombre, descripcion, activo`
- **proveedores**: `idProveedor, razonSocial, cuit, telefono, email, domicilio, idLocalidad, localidad*, activo`
- **vehiculos**: `idVehiculo, patente, marca, modelo, capacidadKg, activo`
- **choferes**: `idChofer, nombreCompleto, dni, telefono, activo`

(* campo solo de lectura)

## Clientes – cualquier usuario puede leer y escribir
`/api/clientes` (mismas rutas que arriba). Campos: `idCliente, nombreCompleto, tipoDocumento (DNI|CUIT|CUIL), nroDocumento, idCondicionIva, condicionIva*, telefono, email, domicilio, idLocalidad, localidad*, activo`.

## Artículos y stock – lectura: todos · escritura: Administrador
`/api/articulos` (ABMC igual que arriba). Campos: `idArticulo, codigo, nombre, descripcion, idCategoria, categoria*, idUnidadMedida, unidadMedida*, permiteDecimales*, idProveedor, proveedor*, precioUnitario, stockActual, stockMinimo, activo, stockBajo*`.
- `stockActual` en el alta es el stock inicial; en la modificación se ignora (el stock cambia por ventas y ajustes).

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/articulos/stock-bajo` | Activos con stock ≤ mínimo |
| GET | `/api/articulos/{id}/movimientos` | Historial de stock |
| POST | `/api/articulos/{id}/ajustar-stock` | (Admin) Body `{ tipo: "Entrada"\|"Salida", motivo: "Compra"\|"Ajuste"\|"Devolucion", cantidad, observaciones }` |

## Catálogos (solo lectura, para combos)
`GET /api/catalogos/provincias` · `/localidades?idProvincia=` · `/condiciones-iva` · `/unidades-medida` · `/roles`

## Facturas (transacción)
| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/facturas?desde=&hasta=&idCliente=&estado=` | Listado (estado: `Emitida` o `Anulada`) |
| GET | `/api/facturas/{id}` | Una factura con sus detalles |
| POST | `/api/facturas` | Alta |
| PUT | `/api/facturas/{id}` | Modificación (solo emitidas) |
| DELETE | `/api/facturas/{id}?motivo=...` | (Admin) Baja = anulación; devuelve el stock |

Body de alta/modificación:
```json
{
  "idCliente": 2,
  "idFormaPago": 1,
  "observaciones": "Entrega en obra",
  "detalles": [
    { "idArticulo": 1, "cantidad": 10 },
    { "idArticulo": 1, "cantidad": 5 },
    { "idArticulo": 3, "cantidad": 2.5 }
  ]
}
```
- Si un artículo se repite, se **suma la cantidad** en un solo detalle (en el ejemplo, artículo 1 → 15).
- Se valida stock suficiente, artículo/cliente/forma de pago activos y que la cantidad sea entera si la unidad no admite decimales.
- Respuesta: `{ idFactura, nroFactura, fecha, idCliente, cliente, idFormaPago, formaPago, idUsuario, vendedor, observaciones, estado, fechaAnulacion, motivoAnulacion, total, detalles: [{ idDetalle, idArticulo, codigo, articulo, unidadMedida, cantidad, precioUnitario, subtotal }] }`.
- `total` y `subtotal` se calculan al responder (no se guardan en la base).

## Repartos
| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/repartos?desde=&hasta=&estado=` | Listado (fechas `yyyy-MM-dd`) |
| GET | `/api/repartos/{id}` | Uno, con sus entregas |
| POST | `/api/repartos` | Alta (queda `Programado`) |
| PUT | `/api/repartos/{id}` | Modificación (solo `Programado`) |
| POST | `/api/repartos/{id}/iniciar` | Programado → EnCurso |
| PUT | `/api/repartos/{id}/entregas/{idDetalleReparto}` | Resultado de una entrega |
| POST | `/api/repartos/{id}/finalizar` | EnCurso → Finalizado |
| POST | `/api/repartos/{id}/cancelar?motivo=...` | Cancela (motivo opcional) |

Body de alta/modificación (el orden de `entregas` es el orden de las paradas):
```json
{
  "fechaProgramada": "2026-11-20",
  "idVehiculo": 1,
  "idChofer": 1,
  "observaciones": "Salida a las 8",
  "entregas": [
    { "idFactura": 5, "domicilioEntrega": "Ruta 9 km 12", "idLocalidad": 2, "telefonoContacto": "353-4000000" }
  ]
}
```
Body del resultado de una entrega: `{ "estado": "Entregada" }` o `{ "estado": "NoEntregada", "observaciones": "No había nadie" }`.

Reglas: la fecha no puede ser pasada; el vehículo y el chofer no pueden estar en otro reparto el mismo día; solo se asignan facturas `Emitida` que no estén en otro reparto vigente; no se finaliza con entregas pendientes.

## Dashboard
`GET /api/dashboard` →
```json
{
  "facturasEmitidasMes": 0, "facturasAnuladasMes": 0, "totalVendidoMes": 0, "ticketPromedio": 0,
  "repartosPendientes": 0, "entregasPendientes": 0,
  "ventasPorFormaPago": [{ "formaPago": "", "cantidadFacturas": 0, "total": 0 }],
  "articulosMasVendidos": [{ "idArticulo": 0, "articulo": "", "cantidadVendida": 0, "total": 0 }],
  "articulosStockBajo": [{ "idArticulo": 0, "codigo": "", "articulo": "", "unidadMedida": "", "stockActual": 0, "stockMinimo": 0 }],
  "ventasUltimos30Dias": [{ "fecha": "2026-11-01T00:00:00", "total": 0 }]
}
```
