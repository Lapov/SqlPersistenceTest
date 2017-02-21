using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using RabbitMQ.Client.Framing;
using Topshelf;

namespace TestPersistance
{
    public class WorkerHost : ServiceControl
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostControl"></param>
        /// <returns></returns>
        public bool Start(HostControl hostControl)
        {
            var builder = new ContainerBuilder();
            var container = builder.Build();
            var scopeName = "newLife";
            var sqlConnection = @"";
            var rabbitConnection = "host=localhost;username=guest;password=guest";

            var newScope = container.BeginLifetimeScope(scopeName, child =>
            {
                child.Register(scope =>
                    {
                        var endpointConfiguration = new EndpointConfiguration("worker");
                        endpointConfiguration.EnableInstallers();
                        endpointConfiguration.SendFailedMessagesTo("worker.error");
                        endpointConfiguration.UsePersistence<InMemoryPersistence>();

                        var persistence = endpointConfiguration.UsePersistence<SqlPersistence, StorageType.Sagas>();
                        persistence.SqlVariant(SqlVariant.MsSqlServer);
                        persistence.ConnectionBuilder(() => new SqlConnection(sqlConnection));
                        persistence.DisableInstaller();

                        endpointConfiguration.UseTransport<RabbitMQTransport>()
                            .ConnectionString(rabbitConnection);

                        endpointConfiguration.UseContainer<AutofacBuilder>(
                            customizations: customizations =>
                            {
                                customizations.ExistingLifetimeScope(scope.Resolve<ILifetimeScope>());

                            });

                        return Endpoint.Start(endpointConfiguration).ConfigureAwait(false).GetAwaiter().GetResult();
                    }).As<IEndpointInstance>()
                    .InstancePerMatchingLifetimeScope(scopeName)
                    .OnRelease(x => x.Stop());
            });
            var endpointInstance = newScope.Resolve<IEndpointInstance>();
            
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            return true;
        }
    }
}
