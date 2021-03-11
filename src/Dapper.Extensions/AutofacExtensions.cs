﻿using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.AttributeFilters;
using Dapper.Extensions.MasterSlave;
using Dapper.Extensions.Monitor;
using Dapper.Extensions.SQL;
using Microsoft.Extensions.Hosting;

namespace Dapper.Extensions
{
    public static class AutofacExtensions
    {
        public static ContainerBuilder AddDapper<TDbProvider>(this ContainerBuilder container, string connectionName = "DefaultConnection", string serviceKey = null, bool enableMasterSlave = false, bool enableMonitor = false) where TDbProvider : IDapper
        {
            container.RegisterType<ResolveContext>().As<IResolveContext>().IfNotRegistered(typeof(IResolveContext)).InstancePerLifetimeScope();
            container.RegisterType<ResolveKeyed>().As<IResolveKeyed>().IfNotRegistered(typeof(IResolveKeyed)).InstancePerLifetimeScope();
            container.RegisterType<DefaultConnectionStringProvider>().As<IConnectionStringProvider>().SingleInstance();
            container.RegisterType<WeightedPolling>().As<ILoadBalancing>().SingleInstance();

            if (string.IsNullOrWhiteSpace(serviceKey))
            {
                if (enableMasterSlave)
                {
                    container.RegisterType<TDbProvider>().As<IDapper>().WithParameters(new[] { new NamedParameter("connectionName", connectionName), new NamedParameter("enableMasterSlave", true) }).InstancePerLifetimeScope();
                    container.RegisterType<TDbProvider>().Keyed<IDapper>("_slave").WithParameters(new[] { new NamedParameter("connectionName", connectionName), new NamedParameter("enableMasterSlave", true) }).InstancePerLifetimeScope();
                }
                else
                {
                    if (enableMonitor)
                    {
                        container.RegisterType<TDbProvider>().WithParameter("connectionName", connectionName).InstancePerLifetimeScope();
                        container.Register<IDapper>(ctx => new DapperProxy(ctx.Resolve<TDbProvider>())).InstancePerLifetimeScope();
                    }
                    else
                        container.RegisterType<TDbProvider>().As<IDapper>().WithParameter("connectionName", connectionName)
                            .InstancePerLifetimeScope();
                }
            }
            else
            {
                if (enableMasterSlave)
                {
                    container.RegisterType<TDbProvider>().Keyed<IDapper>(serviceKey).WithParameters(new[] { new NamedParameter("connectionName", connectionName), new NamedParameter("enableMasterSlave", true) }).InstancePerLifetimeScope();
                    container.RegisterType<TDbProvider>().Keyed<IDapper>($"{serviceKey}_slave").WithParameters(new[] { new NamedParameter("connectionName", connectionName), new NamedParameter("enableMasterSlave", true) }).InstancePerLifetimeScope();
                }
                else
                    container.RegisterType<TDbProvider>().Keyed<IDapper>(serviceKey).WithParameter("connectionName", connectionName).InstancePerLifetimeScope();
            }


            return container;
        }

        public static ContainerBuilder AddDapperConnectionStringProvider<TConnectionStringProvider>(this ContainerBuilder container) where TConnectionStringProvider : IConnectionStringProvider
        {
            container.RegisterType<TConnectionStringProvider>().As<IConnectionStringProvider>().SingleInstance();
            return container;
        }

        public static ContainerBuilder AddAllControllers(this ContainerBuilder container)
        {
            container.RegisterAssemblyTypes(Assembly.GetEntryAssembly())
                .Where(t => t.Name.EndsWith("Controller"))
                .PropertiesAutowired().WithAttributeFiltering().InstancePerLifetimeScope();
            return container;
        }

        public static IHostBuilder UseAutofac(this IHostBuilder builder)
        {
            return builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        }

        /// <summary>
        /// Enable SQL separation
        /// </summary>
        /// <param name="services"></param>
        /// <param name="xmlRootDir">The root directory of the xml file</param>
        /// <returns></returns>
        public static ContainerBuilder AddSQLSeparationForDapper(this ContainerBuilder services, string xmlRootDir)
        {
            services.RegisterInstance(new SQLSeparateConfigure
            {
                RootDir = xmlRootDir
            });
            services.RegisterType<SQLManager>().As<ISQLManager>().SingleInstance();
            return services;
        }
    }
}
