namespace Sporacid.Simplets.Webapp.Services
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Linq;
    using System.Web;
    using System.Web.Http.Filters;
    using log4net;
    using log4net.Core;
    using Microsoft.Web.Infrastructure.DynamicModuleHelper;
    using Ninject;
    using Ninject.Syntax;
    using Ninject.Web.Common;
    using Ninject.Web.WebApi.FilterBindingSyntax;
    using Sporacid.Simplets.Webapp.Core.Repositories;
    using Sporacid.Simplets.Webapp.Core.Repositories.Impl;
    using Sporacid.Simplets.Webapp.Core.Security.Authentication;
    using Sporacid.Simplets.Webapp.Core.Security.Authentication.Impl;
    using Sporacid.Simplets.Webapp.Core.Security.Authentication.Tokens.Factories;
    using Sporacid.Simplets.Webapp.Core.Security.Authentication.Tokens.Factories.Impl;
    using Sporacid.Simplets.Webapp.Core.Security.Authorization;
    using Sporacid.Simplets.Webapp.Core.Security.Authorization.Impl;
    using Sporacid.Simplets.Webapp.Core.Security.Bootstrap;
    using Sporacid.Simplets.Webapp.Core.Security.Bootstrap.Impl;
    using Sporacid.Simplets.Webapp.Core.Security.Database;
    using Sporacid.Simplets.Webapp.Core.Security.Ldap;
    using Sporacid.Simplets.Webapp.Core.Security.Ldap.Impl;
    using Sporacid.Simplets.Webapp.Services.Database;
    using Sporacid.Simplets.Webapp.Services.Services.Clubs;
    using Sporacid.Simplets.Webapp.Services.Services.Clubs.Administration;
    using Sporacid.Simplets.Webapp.Services.Services.Clubs.Administration.Impl;
    using Sporacid.Simplets.Webapp.Services.Services.Clubs.Impl;
    using Sporacid.Simplets.Webapp.Services.Services.Public;
    using Sporacid.Simplets.Webapp.Services.Services.Public.Impl;
    using Sporacid.Simplets.Webapp.Services.Services.Security.Administration;
    using Sporacid.Simplets.Webapp.Services.Services.Security.Administration.Impl;
    using Sporacid.Simplets.Webapp.Services.Services.Userspace;
    using Sporacid.Simplets.Webapp.Services.Services.Userspace.Administration;
    using Sporacid.Simplets.Webapp.Services.Services.Userspace.Administration.Impl;
    using Sporacid.Simplets.Webapp.Services.Services.Userspace.Impl;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Exception;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Security;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Security.Credentials;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Security.Credentials.Impl;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Validation;
    using Sporacid.Simplets.Webapp.Tools.Collections.Caches;
    using Sporacid.Simplets.Webapp.Tools.Collections.Caches.Policies;
    using Sporacid.Simplets.Webapp.Tools.Collections.Caches.Policies.Invalidation;
    using Sporacid.Simplets.Webapp.Tools.Collections.Caches.Policies.Locking;
    using Sporacid.Simplets.Webapp.Tools.Collections.Dictionary;
    using Sporacid.Simplets.Webapp.Tools.Collections.Pooling;
    using Sporacid.Simplets.Webapp.Tools.Factories;
    using Sporacid.Simplets.Webapp.Tools.Threading.Pooling;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavall�e, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    public static class NinjectWebCommon
    {
        public static readonly Bootstrapper Bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof (OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof (NinjectHttpModule));
            Bootstrapper.Initialize(CreateKernel);
        }

        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            Bootstrapper.ShutDown();
        }

        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                RegisterServices(kernel);
                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            RegisterDataContext(kernel);
            RegisterToolProject(kernel);
            RegisterCoreProject(kernel);
            RegisterServiceProject(kernel);
            RegisterServiceProjectFilters(kernel);
        }

        /// <summary>
        /// Register all bindings for the core project.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterCoreProject(IKernel kernel)
        {
            // Repositories configuration.
            // Bind all (beside exceptions) repositories in request scope, because linq to sql data
            // contexts should live for a single unit of work.
            kernel.Bind(typeof (IRepository<,>)).To(typeof (GenericRepository<,>))
                .WhenAnyAncestorMatches(ctx =>
                    !typeof (ISecurityDatabaseBootstrapper).IsAssignableFrom((ctx.Binding).Service) &&
                    !typeof (IRoleBootstrapper).IsAssignableFrom((ctx.Binding).Service))
                .InRequestScope()
                .OnDeactivation(ctx => ((IDisposable) ctx).Dispose());
            // Bind repositories used in the bootstrapping of security database in thread scope,
            // because it's not running as a web request.
            kernel.Bind(typeof (IRepository<,>)).To(typeof (GenericRepository<,>))
                .WhenAnyAncestorMatches(ctx =>
                    typeof (ISecurityDatabaseBootstrapper).IsAssignableFrom((ctx.Binding).Service) ||
                    typeof (IRoleBootstrapper).IsAssignableFrom((ctx.Binding).Service))
                .InThreadScope()
                .OnDeactivation(ctx => ((IDisposable) ctx).Dispose());

            // Security database boostrap configuration.
            kernel.Bind<ISecurityDatabaseBootstrapper>().To<SecurityDatabaseBootstrapper>();
            kernel.Bind<IRoleBootstrapper>().To<RoleBootstrapper>();

            // Security modules configuration.
            // 1. bind security module defaults.
            kernel.Bind<IAuthenticationModule>().To<KerberosAuthenticationModule>()
                .InRequestScope()
                .WithConstructorArgument(ConfigurationManager.AppSettings["ActiveDirectoryDomainName"]);
            kernel.Bind<IAuthorizationModule>().To<AuthorizationModule>()
                .InRequestScope();
            // 2. bind implementation to themselves. 
            // There is cases where we specifically need a given module.
            kernel.Bind<KerberosAuthenticationModule>().ToSelf()
                .InRequestScope()
                .WithConstructorArgument(ConfigurationManager.AppSettings["ActiveDirectoryDomainName"]);
            kernel.Bind<TokenAuthenticationModule>().ToSelf()
                .InRequestScope()
                // Bind observables like this, because it doesn't work another way.
                .WithConstructorArgument(typeof (IEnumerable<IAuthenticationObservable>), ctx => new IAuthenticationObservable[] {kernel.Get<KerberosAuthenticationModule>()});
            kernel.Bind<AuthorizationModule>().ToSelf()
                .InRequestScope();
            // 3. bind responbility chains enumerables. Those will be used when 
            // a class needs all supported security modules.
            kernel.Bind<IEnumerable<IAuthenticationModule>>()
                .ToMethod(ctx => new IAuthenticationModule[] {kernel.Get<KerberosAuthenticationModule>(), kernel.Get<TokenAuthenticationModule>()})
                .InRequestScope();
            kernel.Bind<IEnumerable<IAuthorizationModule>>()
                .ToMethod(ctx => new IAuthorizationModule[] {kernel.Get<AuthorizationModule>()})
                .InRequestScope();

            // Token factory configuration.
            kernel.Bind<ITokenFactory>().To<AuthenticationTokenFactory>()
                .WithConstructorArgument(TimeSpan.FromHours(6))
                .WithConstructorArgument((uint) 64);

            // Ldap configuration.
            kernel.Bind<ILdapSearcher>().To<ActiveDirectorySearcher>()
                .WithConstructorArgument(ConfigurationManager.AppSettings["ActiveDirectoryDomainName"]);
        }

        /// <summary>
        /// Register all linq to sql data contexts.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterDataContext(IKernel kernel)
        {
            kernel.Bind<LinqToSqlLog4netAdapter>().To<LinqToSqlLog4netAdapter>()
                .InSingletonScope()
                .WithConstructorArgument(Level.Info)
                .WithConstructorArgument(LogManager.GetLogger("Queries.QueriesLogger"));

            // Security data context configuration.
            kernel.Bind<DataContext>().To<SecurityDataContext>()
                .When(request => request.ParentRequest.Service.GetGenericArguments()[1].Namespace == "Sporacid.Simplets.Webapp.Core.Security.Database")
                .WithConstructorArgument(ConfigurationManager.ConnectionStrings["SIMPLETSConnectionString"].ConnectionString)
                .OnActivation(dc => dc.Log = kernel.Get<LinqToSqlLog4netAdapter>());
            // .OnActivation(dc => dc.ObjectTrackingEnabled = false);

            // Database data context configuration.
            kernel.Bind<DataContext>().To<DatabaseDataContext>()
                .When(request => request.ParentRequest.Service.GetGenericArguments()[1].Namespace == "Sporacid.Simplets.Webapp.Services.Database")
                .WithConstructorArgument(ConfigurationManager.ConnectionStrings["SIMPLETSConnectionString"].ConnectionString)
                .OnActivation(dc => dc.Log = kernel.Get<LinqToSqlLog4netAdapter>());
        }

        /// <summary>
        /// Register all bindings for the tools project.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterToolProject(IKernel kernel)
        {
            // Cache configurations.
            kernel.Bind(typeof (IDictionary<,>)).To(typeof (Dictionary<,>));
            kernel.Bind(typeof (IDictionary<,>)).To(typeof (LinkedDictionary<,>))
                .When(request =>
                    request.ParentRequest.Service.GetGenericArguments()[1].Namespace == "Sporacid.Simplets.Webapp.Core.Security.Database" ||
                    request.ParentRequest.Service.GetGenericArguments()[1].Namespace == "Sporacid.Simplets.Webapp.Services.Database");
            kernel.Bind(typeof (ICache<,>)).To(typeof (ConfigurableCache<,>))
                .InSingletonScope();
            kernel.Bind(typeof (ICachePolicy<,>)).To(typeof (ReaderWriterLockingPolicy<,>));
            kernel.Bind(typeof (ICachePolicy<,>)).To(typeof (TimeBasedInvalidationPolicy<,>))
                .WithConstructorArgument(TimeSpan.FromHours(6));

            // General configuration.
            kernel.Bind<IThreadPool>().To<ThreadPool>();
            kernel.Bind(typeof (IFactory<>)).To(typeof (Factory<>));
            kernel.Bind(typeof (IObjectPool<>)).To(typeof (ObjectPool<>));
        }

        /// <summary>
        /// Register all bindings for the services project.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServiceProject(IKernel kernel)
        {
            // Services configuration. TODO Rebind the shits.
            // Security services.
            kernel.Bind<IContextAdministrationService>().To<ContextAdministrationService>().InRequestScope();
            kernel.Bind<IPrincipalAdministrationService>().To<PrincipalAdministrationService>().InRequestScope();
            // Club services.
            kernel.Bind<IClubAdministrationService>().To<ClubAdministrationService>().InRequestScope();
            kernel.Bind<IInscriptionService>().To<InscriptionService>().InRequestScope();
            kernel.Bind<ICommanditaireService>().To<CommanditaireService>().InRequestScope();
            kernel.Bind<ICommanditeService>().To<CommanditeService>().InRequestScope();
            kernel.Bind<IFournisseurService>().To<FournisseurService>().InRequestScope();
            kernel.Bind<IInventaireService>().To<InventaireService>().InRequestScope();
            kernel.Bind<IMeetingService>().To<MeetingService>().InRequestScope();
            kernel.Bind<IGroupeService>().To<GroupeService>().InRequestScope();
            kernel.Bind<IMembreService>().To<MembreService>().InRequestScope();
            // Public services.
            kernel.Bind<IAnonymousService>().To<AnonymousService>().InRequestScope();
            kernel.Bind<IEnumerationService>().To<EnumerationService>().InRequestScope();
            kernel.Bind<IDescriptionService>().To<DescriptionService>().InRequestScope();
            // Userspace services.
            kernel.Bind<IProfilAdministrationService>().To<ProfilAdministrationService>().InRequestScope();
            kernel.Bind<IProfilService>().To<ProfilService>().InRequestScope();
            
            // Credential extractor configuration.
            // See security module section to know why we bind like thid.
            kernel.Bind<ICredentialsExtractor>().To<KerberosCredentialsExtractor>();
            kernel.Bind<KerberosCredentialsExtractor>().ToSelf();
            kernel.Bind<TokenCredentialsExtractor>().ToSelf();
            kernel.Bind<IEnumerable<ICredentialsExtractor>>()
                .ToMethod(ctx => new ICredentialsExtractor[]
                {
                    kernel.Get<KerberosCredentialsExtractor>(),
                    kernel.Get<TokenCredentialsExtractor>()
                });
        }

        /// <summary>
        /// Register all web api filters for the services project.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServiceProjectFilters(IKernel kernel)
        {
            // Bind the authentication filter on services that have the RequiresAuthenticatedPrincipal attribute
            kernel.BindHttpFilter<AuthenticationFilter>(FilterScope.Controller)
                .WhenControllerHas<RequiresAuthenticatedPrincipalAttribute>();
            // .InRequestScope();

            // Bind the authentication filter on services that have the RequiresAuthorizedPrincipal attribute
            kernel.BindHttpFilter<AuthorizationFilter>(FilterScope.Controller)
                .WhenControllerHas<RequiresAuthorizedPrincipalAttribute>()
                // .InRequestScope()
                .WithConstructorArgument("endpointsNamespaces", ctx => new[]
                {
                    "Sporacid.Simplets.Webapp.Services.Services.Security",
                    "Sporacid.Simplets.Webapp.Services.Services.Security.Administration",
                    "Sporacid.Simplets.Webapp.Services.Services.Clubs",
                    "Sporacid.Simplets.Webapp.Services.Services.Clubs.Administration",
                    "Sporacid.Simplets.Webapp.Services.Services.Public",
                    "Sporacid.Simplets.Webapp.Services.Services.Public.Administration",
                    "Sporacid.Simplets.Webapp.Services.Services.Userspace",
                    "Sporacid.Simplets.Webapp.Services.Services.Userspace.Administration"
                });

            // Bind the exception handling filter on services that have the HandlesException attribute
            kernel.BindHttpFilter<ExceptionHandlingFilter>(FilterScope.Controller)
                .WhenControllerHas<HandlesExceptionAttribute>();
            // .InRequestScope();

            // Bind the model validation filter.
            kernel.BindHttpFilter<ValidationFilter>(FilterScope.Global);

            // Bind the anti-forgery validation filter.
            // kernel.BindHttpFilter<ValidateAntiForgeryTokenFilter>(FilterScope.Global);
        }
    }
}
