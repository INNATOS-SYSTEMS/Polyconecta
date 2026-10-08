using System;
using System.Collections.Generic;
using Contpaq.Bridge.Infrastructure.Sdk;
using Contpaq.Bridge.Tests.Soporte;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Contpaq.Bridge.Tests.Sdk
{
    /// <summary>
    /// Sesión permanente del SDK (L1-T001, R-07): el SDK se inicia una vez por proceso y la empresa se
    /// abre por lote. Con un <see cref="ISdkNativo"/> falso; la corrida contra CONTPAQi es de L1-T005 (VPS).
    /// </summary>
    public class SesionPermanenteTests
    {
        private readonly SdkNativoFalso _nativo = new();
        private readonly RelojFalso _reloj = new(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));

        private ContpaqiSdkGateway Gateway(bool conUsuarios = true, int inactividadSegundos = 5)
        {
            var valores = new Dictionary<string, string?> { ["BridgeConfig:IdleSessionTimeoutSeconds"] = inactividadSegundos.ToString() };
            if (conUsuarios)
            {
                valores["BridgeConfig:Sesion:ComercialUsuario"] = "SUPERVISOR";
                valores["BridgeConfig:Sesion:ContpaqiUsuario"] = "admin";
            }
            var config = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
            return new ContpaqiSdkGateway(config, NullLogger<ContpaqiSdkGateway>.Instance, _nativo, _reloj);
        }

        [Fact]
        public void El_inicio_es_Comercial_luego_el_nombre_del_paquete_luego_CONTPAQi_y_despues_la_empresa()
        {
            var gateway = Gateway();

            Assert.True(gateway.AsegurarSesion());

            Assert.Equal(
                ["Preparar", "InicioSesionSdk", "SetNombrePaq", "InicioSesionSdkContpaqi", "AbreEmpresa"],
                _nativo.Llamadas);
            Assert.True(gateway.SdkIniciado);
            Assert.True(gateway.SesionActiva);
        }

        [Fact]
        public void Veinte_lotes_seguidos_no_repiten_el_inicio_de_sesion()
        {
            var gateway = Gateway();

            for (var lote = 0; lote < 20; lote++)
            {
                Assert.True(gateway.AsegurarSesion());
                _reloj.Avanzar(TimeSpan.FromSeconds(10)); // la cola se vacía y pasa la inactividad
                gateway.CerrarSiInactiva();
            }

            Assert.Equal(1, _nativo.Veces("Preparar"));
            Assert.Equal(1, _nativo.Veces("InicioSesionSdk"));
            Assert.Equal(1, _nativo.Veces("SetNombrePaq"));
            Assert.Equal(1, _nativo.Veces("InicioSesionSdkContpaqi"));
            Assert.Equal(20, _nativo.Veces("AbreEmpresa"));
            Assert.Equal(20, _nativo.Veces("CierraEmpresa"));
            Assert.Equal(0, _nativo.Veces("TerminaSdk"));
        }

        [Fact]
        public void Dentro_del_mismo_lote_la_empresa_no_se_vuelve_a_abrir()
        {
            var gateway = Gateway();
            for (var i = 0; i < 5; i++) Assert.True(gateway.AsegurarSesion());
            Assert.Equal(1, _nativo.Veces("AbreEmpresa"));
        }

        [Fact]
        public void La_inactividad_cierra_la_empresa_pero_no_el_SDK()
        {
            var gateway = Gateway(inactividadSegundos: 5);
            gateway.AsegurarSesion();

            _reloj.Avanzar(TimeSpan.FromSeconds(4));
            gateway.CerrarSiInactiva();
            Assert.True(gateway.SesionActiva, "todavía no pasa el tiempo de inactividad");

            _reloj.Avanzar(TimeSpan.FromSeconds(2));
            gateway.CerrarSiInactiva();

            Assert.False(gateway.SesionActiva);
            Assert.True(gateway.SdkIniciado);
            Assert.Equal(1, _nativo.Veces("CierraEmpresa"));
            Assert.Equal(0, _nativo.Veces("TerminaSdk"));
        }

        [Fact]
        public void Al_apagar_se_cierra_la_empresa_y_fTerminaSDK_se_llama_una_sola_vez()
        {
            var gateway = Gateway();
            gateway.AsegurarSesion();

            gateway.Apagar();
            gateway.Apagar();

            Assert.Equal(1, _nativo.Veces("CierraEmpresa"));
            Assert.Equal(1, _nativo.Veces("TerminaSdk"));
            Assert.False(gateway.SdkIniciado);
            Assert.Equal(["CierraEmpresa", "TerminaSdk"], System.Linq.Enumerable.TakeLast(_nativo.Llamadas, 2));
        }

        [Fact]
        public void Sin_usuarios_configurados_se_usa_fInicializaSDK()
        {
            var gateway = Gateway(conUsuarios: false);

            Assert.True(gateway.AsegurarSesion());

            Assert.Equal(["Preparar", "InicializaSdk", "AbreEmpresa"], _nativo.Llamadas);
        }

        [Fact]
        public void Si_el_inicio_falla_no_se_abre_la_empresa_y_el_siguiente_lote_lo_reintenta()
        {
            var gateway = Gateway();
            _nativo.ResultadoSetNombrePaq = 7;

            Assert.False(gateway.AsegurarSesion());
            Assert.False(gateway.SdkIniciado);
            Assert.Equal(0, _nativo.Veces("AbreEmpresa"));

            _nativo.ResultadoSetNombrePaq = 0;
            Assert.True(gateway.AsegurarSesion());
            Assert.True(gateway.SdkIniciado);
        }

        [Fact]
        public void Si_el_entorno_no_sirve_para_el_SDK_no_se_llama_nada_nativo()
        {
            _nativo.MotivoPreparar = "El proceso es de 64 bits";
            var gateway = Gateway();

            Assert.False(gateway.AsegurarSesion());

            Assert.Equal(["Preparar"], _nativo.Llamadas);
        }

        [Fact]
        public void Empresa_ya_abierta_en_Comercial_126209_se_toma_como_abierta()
        {
            _nativo.ResultadoAbreEmpresa = 126209;
            var gateway = Gateway();

            Assert.True(gateway.AsegurarSesion());
            Assert.True(gateway.SesionActiva);
        }

        [Fact]
        public void IniciarSdk_al_arrancar_deja_el_SDK_listo_sin_abrir_la_empresa()
        {
            var gateway = Gateway();

            Assert.True(gateway.IniciarSdk());
            Assert.True(gateway.IniciarSdk());

            Assert.True(gateway.SdkIniciado);
            Assert.False(gateway.SesionActiva);
            Assert.Equal(0, _nativo.Veces("AbreEmpresa"));
            Assert.Equal(1, _nativo.Veces("InicioSesionSdk"));
        }
    }
}
