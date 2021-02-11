using CaptacionesPlazo;
using CaptacionesVista;
using Cartera;
using Clientes;
using NegociosFinancieros;
using Organizaciones;
using Personas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using Utilidades;

namespace BLTChatBotWS
{
    /// <summary>
    /// Descripción breve de BLTChatBotWS
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
    // [System.Web.Script.Services.ScriptService]
    public class BLTChatBotWS : System.Web.Services.WebService
    {

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void ConfirmaIdentidadDelSocio(string numero_Cedula_Socio, DateTime fecha_Ultima_Transaccion, string agencia_Ultima_Transaccion)
        {
            JavaScriptSerializer json = new JavaScriptSerializer();
            string datoJson = string.Empty;
            string error = string.Empty;
            Oficina oficina = null;
            Cliente cliente = null;
            try
            {
                //Valida Si existe Socio
                var persona = PersonaActor.DameListaPorIdentificacion(numero_Cedula_Socio, true, 0).Where(x => x.TipoIdentificacion.NumeroMinimoRepresentantes == 0).FirstOrDefault();
                if (persona != null)
                {
                    //Busca Cliente
                    cliente = ClienteActor.DamePorSecuencialPersonaSinVerificarExistencia(persona.Secuencial);
                    if (cliente != null)
                    {
                        //Valida Oficina
                        var listaOficina = OficinaActor.DameListaPorEstaActiva(true, true, Oficina.CodigoOficinaControlCampo, 0);
                        oficina = listaOficina.Where(x => x.Ciudad.ToLower().Contains(agencia_Ultima_Transaccion.ToLower())
                            || x.Division.Nombre.ToLower().Contains(agencia_Ultima_Transaccion.ToLower())
                            || x.Siglas.ToLower().Contains(agencia_Ultima_Transaccion.ToLower())
                            || agencia_Ultima_Transaccion.ToLower().Contains(x.Siglas.ToLower())).FirstOrDefault();

                        if (oficina == null)
                            error = "No Hay Concidencias Para La Agencia Buscada";

                        //Valida Ultima Transaccion
                        int ultimoMovimiento = PrestamoMaestroActor.DevuelveUltimoMovimiento(cliente.Secuencial);

                        if (ultimoMovimiento > 0)
                        {
                            MovimientoDetalle movimiento = MovimientoDetalleActor.DameListaPorSecuencialMovimiento(ultimoMovimiento, 0).FirstOrDefault();
                            if (movimiento != null)
                            {
                                if (movimiento.Movimiento.Fecha != fecha_Ultima_Transaccion)
                                    error = "La Fecha De La Última Transacción No Coincide";

                                if (oficina != null)
                                {
                                    if (oficina.SecuencialDivision != movimiento.SecuencialOficinaAfectada)
                                        error = "La Oficina De La Última Transacción No Coincide";
                                }
                            }
                        }
                        else
                            error = "No Existen Movimientos Registrados";


                    }
                    else
                        error = "La persona Con Esa Identificación No Es Cliente";
                }
                else
                {
                    error = "Identificación No Encontrada";
                }

                if (error != string.Empty)
                {

                    var datosJson = new
                    {
                        success = false,
                        msg = error
                    };

                    HttpContext.Current.Response.ContentType = "application/json";
                    HttpContext.Current.Response.Write(json.Serialize(datosJson));
                    HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                    HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                }
                else
                {




                    var datosJson = new
                    {
                        success = true,
                        numero_Socio = cliente.NumeroCliente
                    };

                    HttpContext.Current.Response.ContentType = "application/json";
                    HttpContext.Current.Response.Write(json.Serialize(datosJson));
                    HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                    HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                }

            }
            catch (Exception ex)
            {
                var datosJson = new
                {
                    success = false,
                    msg = ex
                };

                HttpContext.Current.Response.ContentType = "application/json";
                HttpContext.Current.Response.Write(json.Serialize(datosJson));
                HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                HttpContext.Current.ApplicationInstance.CompleteRequest();

            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void ConsultaDePrestamos(string numero_Cedula_Socio, string numero_Socio)
        {
            JavaScriptSerializer json = new JavaScriptSerializer();
            string datoJson = string.Empty;
            List<Prestamos> lista = new List<Prestamos>();
            try
            {

                //Valida Si existe Socio
                var persona = PersonaActor.DamePorIdentificacionSinVerificarExistencia(numero_Cedula_Socio);
                var cliente = ClienteActor.DameListaPorNumeroCliente(FBSSettingsEmpresa.LeeConfiguracion().Secuencial,
                    int.Parse(numero_Socio), true, 0).FirstOrDefault();

                if (cliente != null)
                {
                    var listaPrestamos = PrestamoMaestroActor.DameListaPorSecuencialCliente(cliente.Secuencial).Where(x =>
                        x.CodigoEstadoPrestamo != FBSSettingsEstadoPrestamo.LeeConfiguracion().Cancelado);
                    foreach (var item in listaPrestamos)
                    {
                        lista.Add(new Prestamos(item.TipoPrestamo.Nombre,
                            item.DeudaInicial,
                            item.FechaProximoVencimiento.Day, 
                            item.ValorCuota,
                            item.SaldoActual));
                    }
                }

                var datosJson = new
                {
                    success = true,
                    nombre_Socio = persona.NombreUnido,
                    lista_De_Prestamos = lista
                };

                HttpContext.Current.Response.ContentType = "application/json";
                HttpContext.Current.Response.Write(json.Serialize(datosJson));
                HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                var datosJson = new
                {
                    success = false,
                    msg = ex
                };

                HttpContext.Current.Response.ContentType = "application/json";
                HttpContext.Current.Response.Write(json.Serialize(datosJson));
                HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                HttpContext.Current.ApplicationInstance.CompleteRequest();

            }
        }
       

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void ConsultaDeInversiones(string numero_Cedula_Socio, string numero_Socio)
        {
            JavaScriptSerializer json = new JavaScriptSerializer();
            string datoJson = string.Empty;
            List<Inversiones> lista = new List<Inversiones>();
            try
            {

                //Valida Si existe Socio
                var persona = PersonaActor.DamePorIdentificacionSinVerificarExistencia(numero_Cedula_Socio);
                var cliente = ClienteActor.DameListaPorNumeroCliente(FBSSettingsEmpresa.LeeConfiguracion().Secuencial,
                    int.Parse(numero_Socio), true, 0).FirstOrDefault();

                var estado = FBSSettingsEstadoDeposito.LeeConfiguracion();

                if (cliente != null)
                {
                    var listaInversiones = DepositoMaestroActor.DameListaPorSecuencialCliente(cliente.Secuencial).Where(x =>
                        x.CodigoEstadoDeposito == estado.Activo || x.CodigoEstadoDeposito == estado.Vencido
                     || x.CodigoEstadoDeposito == estado.Exigible);
                    foreach (var item in listaInversiones)
                    {

                        lista.Add(new Inversiones(item.Codigo,
                            item.Monto));
                    }
                }

                var datosJson = new
                {
                    success = true,
                    nombre_Socio = persona.NombreUnido,
                    lista_De_Inversiones = lista
                };

                HttpContext.Current.Response.ContentType = "application/json";
                HttpContext.Current.Response.Write(json.Serialize(datosJson));
                HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                var datosJson = new
                {
                    success = false,
                    msg = ex
                };

                HttpContext.Current.Response.ContentType = "application/json";
                HttpContext.Current.Response.Write(json.Serialize(datosJson));
                HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                HttpContext.Current.ApplicationInstance.CompleteRequest();

            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void ConsultaDeAhorrosSocio(string numero_Cedula_Socio, string numero_Socio)
        {
            JavaScriptSerializer json = new JavaScriptSerializer();
            string datoJson = string.Empty;
            List<Prestamos> lista = new List<Prestamos>();
            var estadoCuenta = FBSSettingsEstadoCuenta.LeeConfiguracion();
            try
            {

                //Valida Si existe Socio
                var persona = PersonaActor.DamePorIdentificacionSinVerificarExistencia(numero_Cedula_Socio);
                var cliente = ClienteActor.DameListaPorNumeroCliente(FBSSettingsEmpresa.LeeConfiguracion().Secuencial,
                    int.Parse(numero_Socio), true, 0).FirstOrDefault();

                if (cliente != null)
                {
                    CuentaMaestro cuentaAhorros = CuentaMaestroActor.DameListaPorSecuencialCliente(cliente.Secuencial).Where(x =>
                        (x.CodigoEstado == estadoCuenta.Activa || x.CodigoEstado == estadoCuenta.Inactiva)
                        && x.CodigoTipoCuenta == FBSSettingsTipoCuenta.LeeConfiguracion().Ahorro).OrderByDescending(x => x.Saldo).FirstOrDefault();

                    if (cuentaAhorros != null)
                    {
                        var datosJson = new
                        {
                            success = true,
                            nombre_Socio = persona.NombreUnido,
                            saldo_Cuenta_Ahorro_Socio = cuentaAhorros.Saldo
                        };

                        HttpContext.Current.Response.ContentType = "application/json";
                        HttpContext.Current.Response.Write(json.Serialize(datosJson));
                        HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                        HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }
                    else
                    {
                        var datosJson = new
                        {
                            success = false,
                            msg = "No se encontró ninguna cuenta de Ahorro Socio"
                        };

                        HttpContext.Current.Response.ContentType = "application/json";
                        HttpContext.Current.Response.Write(json.Serialize(datosJson));
                        HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                        HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }

                }
            }
            catch (Exception ex)
            {
                var datosJson = new
                {
                    success = false,
                    msg = ex
                };

                HttpContext.Current.Response.ContentType = "application/json";
                HttpContext.Current.Response.Write(json.Serialize(datosJson));
                HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                HttpContext.Current.ApplicationInstance.CompleteRequest();

            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void ConsultaDeAhorrosCoopMetas(string numero_Cedula_Socio, string numero_Socio)
        {
            JavaScriptSerializer json = new JavaScriptSerializer();
            string datoJson = string.Empty;
            List<Prestamos> lista = new List<Prestamos>();
            var estadoCuenta = FBSSettingsEstadoCuenta.LeeConfiguracion();
            try
            {

                //Valida Si existe Socio
                var persona = PersonaActor.DamePorIdentificacionSinVerificarExistencia(numero_Cedula_Socio);
                var cliente = ClienteActor.DameListaPorNumeroCliente(FBSSettingsEmpresa.LeeConfiguracion().Secuencial,
                    int.Parse(numero_Socio), true, 0).FirstOrDefault();

                if (cliente != null)
                {
                    CuentaMaestro cuentaAhorros = CuentaMaestroActor.DameListaPorSecuencialCliente(cliente.Secuencial).Where(x =>
                        (x.CodigoEstado == estadoCuenta.Activa || x.CodigoEstado == estadoCuenta.Inactiva)
                        && x.CodigoTipoCuenta == FBSSettingsTipoCuenta.LeeConfiguracion().AhorroProgramado).OrderByDescending(x => x.Saldo).FirstOrDefault();


                    if (cuentaAhorros != null)
                    {
                        CuentaMaestro_AhorroInversion ahorroInversion = CuentaMaestro_AhorroInversionActor.DamePorSecuencialCuentaMaestroSinVerificarExistencia(cuentaAhorros.Secuencial);
                        if (ahorroInversion != null)
                        {
                            var datosJson = new
                            {
                                success = true,
                                nombre_Socio = persona.NombreUnido,
                                saldo_Cuenta_Ahorro_CoopMetas = cuentaAhorros.Saldo,
                                fecha_Ahorro = ahorroInversion.FechaCancelacion.ToShortDateString(),
                                monto_Ahorro = ahorroInversion.ValorCobro
                            };

                            HttpContext.Current.Response.ContentType = "application/json";
                            HttpContext.Current.Response.Write(json.Serialize(datosJson));
                            HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                            HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                        }
                        else
                        {
                            var datosJson = new
                            {
                                success = false,
                                msg = "No se encontró ninguna cuenta de Ahorro Inversion"
                            };

                            HttpContext.Current.Response.ContentType = "application/json";
                            HttpContext.Current.Response.Write(json.Serialize(datosJson));
                            HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                            HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                        }


                    }
                    else
                    {
                        var datosJson = new
                        {
                            success = false,
                            msg = "No se encontró ninguna cuenta de Ahorros para el cliente"
                        };

                        HttpContext.Current.Response.ContentType = "application/json";
                        HttpContext.Current.Response.Write(json.Serialize(datosJson));
                        HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                        HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }


                }
            }
            catch (Exception ex)
            {
                var datosJson = new
                {
                    success = false,
                    msg = ex
                };

                HttpContext.Current.Response.ContentType = "application/json";
                HttpContext.Current.Response.Write(json.Serialize(datosJson));
                HttpContext.Current.Response.Flush(); // Sends all currently buffered output to the client.
                HttpContext.Current.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
                HttpContext.Current.ApplicationInstance.CompleteRequest();

            }
        }

        #region mensajeria

        public class Prestamos
        {
            private string _tipo_Prestamo;
            private decimal _monto_Credito;
            private int _dia_Pago;
            private decimal _valor_Cuota;
            private decimal _valor_Para_Cancelar;


            public string Tipo_Prestamo
            {
                get { return this._tipo_Prestamo; }
                set { this._tipo_Prestamo = value; }
            }

            public decimal Monto_Credito
            {
                get { return this._monto_Credito; }
                set { this._monto_Credito = value; }
            }

            public int Dia_Pago
            {
                get { return this._dia_Pago; }
                set { this._dia_Pago = value; }
            }

            public decimal Valor_Cuota
            {
                get { return this._valor_Cuota; }
                set { this._valor_Cuota = value; }
            }

            public decimal Valor_Para_Cancelar
            {
                get { return this._valor_Para_Cancelar; }
                set { this._valor_Para_Cancelar = value; }
            }

            public Prestamos() { }

            public Prestamos(
                string tipo_Prestamo,
                decimal monto_Credito,
                int dia_Pago,
                decimal valor_Cuota,
                decimal valor_Para_Cancelar)
            {
                this._tipo_Prestamo = tipo_Prestamo;
                this._monto_Credito = monto_Credito;
                this._dia_Pago = dia_Pago;
                this._valor_Cuota = valor_Cuota;
                this._valor_Para_Cancelar = valor_Para_Cancelar;
            }
        }

        public class Inversiones
        {
            private string _numero_Inversion;
            private decimal _monto_Inversion;


            public string NumeroInversion
            {
                get { return this._numero_Inversion; }
                set { this._numero_Inversion = value; }
            }

            public decimal Monto_Inversion
            {
                get { return this._monto_Inversion; }
                set { this._monto_Inversion = value; }
            }


            public Inversiones() { }

            public Inversiones(
                string numero_Inversion,
                decimal monto_Inversion)
            {
                this._numero_Inversion = numero_Inversion;
                this._monto_Inversion = monto_Inversion;
            }
        }

        #endregion
    }
}
