namespace Sporacid.Simplets.Webapp.Services
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Web;
    using System.Web.Http.Filters;
    using Microsoft.Web.Infrastructure.DynamicModuleHelper;
    using Ninject;
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
    using Sporacid.Simplets.Webapp.Services.Services.Administration;
    using Sporacid.Simplets.Webapp.Services.Services.Administration.Impl;
    using Sporacid.Simplets.Webapp.Services.Services.Clubs;
    using Sporacid.Simplets.Webapp.Services.Services.Clubs.Impl;
    using Sporacid.Simplets.Webapp.Services.Services.Public;
    using Sporacid.Simplets.Webapp.Services.Services.Public.Impl;
    using Sporacid.Simplets.Webapp.Services.Services.Userspace;
    using Sporacid.Simplets.Webapp.Services.Services.Userspace.Impl;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.ExceptionHandling;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Localization;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Security;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Security.Credentials;
    using Sporacid.Simplets.Webapp.Services.WebApi2.Filters.Security.Credentials.Impl;
    using Sporacid.Simplets.Webapp.Tools.Collections.Caches;
    using Sporacid.Simplets.Webapp.Tools.Collections.Caches.Policies;
    using Sporacid.Simplets.Webapp.Tools.Collections.Caches.Policies.Invalidation;
    using Sporacid.Simplets.Webapp.Tools.Collections.Caches.Policies.Locking;
    using Sporacid.Simplets.Webapp.Tools.Collections.Pooling;
    using Sporacid.Simplets.Webapp.Tools.Factories;
    using Sporacid.Simplets.Webapp.Tools.Threading.Pooling;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavall�e, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    public static class NinjectWebCommon
    {
        private static readonly Bootstrapper Bootstrapper = new Bootstrapper();

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
                .InRequestScope();
            // Bind repositories used in the bootstrapping of security database in thread scope,
            // because it's not running as a web request.
            kernel.Bind(typeof (IRepository<,>)).To(typeof (GenericRepository<,>))
                .WhenAnyAncestorMatches(ctx =>
                    typeof (ISecurityDatabaseBootstrapper).IsAssignableFrom((ctx.Binding).Service) ||
                    typeof (IRoleBootstrapper).IsAssignableFrom((ctx.Binding).Service))
                .InThreadScope();

            // Data context stores configuration
            kernel.Bind<IDataContextStore>().To<DataContextStore>()
                .When(request => request.ParentRequest.Service.GetGenericArguments()[1].Namespace == "Sporacid.Simplets.Webapp.Core.Security.Database")
                .InRequestScope()
                .WithConstructorArgument(typeof (SecurityDataContext))
                .OnDeactivation(store => ((IDisposable) store).Dispose());
            kernel.Bind<IDataContextStore>().To<DataContextStore>()
                .When(request => request.ParentRequest.Service.GetGenericArguments()[1].Namespace == "Sporacid.Simplets.Webapp.Services.Database")
                .InRequestScope()
                .WithConstructorArgument(typeof (DatabaseDataContext))
                .OnDeactivation(store => ((IDisposable) store).Dispose());

            // Security database boostrap configuration.
            kernel.Bind<ISecurityDatabaseBootstrapper>().To<SecurityDatabaseBootstrapper>();
            kernel.Bind<IRoleBootstrapper>().To<RoleBootstrapper>();
            // kernel.Bind<IDatabaseCreator>().To<DatabaseCreator>();

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
                .WithConstructorArgument(typeof (IEnumerable<IAuthenticationObservable>), ctx => new IAuthenticationObservable[]
                {
                    kernel.Get<KerberosAuthenticationModule>()
                });
            kernel.Bind<AuthorizationModule>().ToSelf()
                .InRequestScope();
            // 3. bind responbility chains enumerables. Those will be used when 
            // a class needs all supported security modules.
            kernel.Bind<IEnumerable<IAuthenticationModule>>()
                .ToMethod(ctx => new IAuthenticationModule[]
                {
                    kernel.Get<KerberosAuthenticationModule>(),
                    kernel.Get<TokenAuthenticationModule>()
                })
                .InRequestScope();
            kernel.Bind<IEnumerable<IAuthorizationModule>>()
                .ToMethod(ctx => new IAuthorizationModule[]
                {
                    kernel.Get<AuthorizationModule>()
                })
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
        /// Register all bindings for the tools project.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterToolProject(IKernel kernel)
        {
            // Cache configurations.
            kernel.Bind(typeof (ICache<,>)).To(typeof (ConfigurableCache<,>))
                .InSingletonScope();
            kernel.Bind(typeof (ICachePolicy<,>)).To(typeof (ExclusiveLockingPolicy<,>));
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
            // Services configuration.
            // Administration services.
            kernel.Bind<IContextAdministrationService>().To<ContextAdministrationService>().InRequestScope();
            kernel.Bind<IUserspaceAdministrationService>().To<UserspaceAdministrationService>().InRequestScope();
            kernel.Bind<IPrincipalAdministrationService>().To<PrincipalAdministrationService>().InRequestScope();
            kernel.Bind<ISystemAdministrationService>().To<SystemAdministrationService>().InRequestScope();
            // Club services.
            kernel.Bind<IInscriptionService>().To<InscriptionService>().InRequestScope();
            // Public services.
            kernel.Bind<IAnonymousService>().To<AnonymousService>().InRequestScope();
            kernel.Bind<IEnumerationService>().To<EnumerationService>().InRequestScope();
            // Userspace services.
            kernel.Bind<IProfilService>().To<ProfilService>().InRequestScope();

            // Data contexts configuration.
            kernel.Bind<SecurityDataContext>().ToSelf()
                .InRequestScope()
                .WithConstructorArgument(ConfigurationManager.ConnectionStrings["SIMPLETSConnectionString"].ConnectionString);
            kernel.Bind<DatabaseDataContext>().ToSelf()
                .InRequestScope()
                .WithConstructorArgument(ConfigurationManager.ConnectionStrings["SIMPLETSConnectionString"].ConnectionString);

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
                .WhenControllerHas<RequiresAuthenticatedPrincipalAttribute>()
                .InRequestScope();

            // Bind the authentication filter on services that have the RequiresAuthorizedPrincipal attribute
            kernel.BindHttpFilter<AuthorizationFilter>(FilterScope.Controller)
                .WhenControllerHas<RequiresAuthorizedPrincipalAttribute>()
                .InRequestScope()
                .WithConstructorArgument("endpointsNamespaces", ctx => new[]
                {
                    "Sporacid.Simplets.Webapp.Services.Services.Administration",
                    "Sporacid.Simplets.Webapp.Services.Services.Clubs",
                    "Sporacid.Simplets.Webapp.Services.Services.Public",
                    "Sporacid.Simplets.Webapp.Services.Services.Userspace"
                });

            // Bind the exception handling filter on services that have the HandlesException attribute
            kernel.BindHttpFilter<ExceptionHandlingFilter>(FilterScope.Controller)
                .WhenControllerHas<HandlesExceptionAttribute>()
                .InRequestScope();

            // Bind the localization filter.
            kernel.BindHttpFilter<LocalizationFilter>(FilterScope.Global);

            // Bind the anti-forgery validation filter.
            // kernel.BindHttpFilter<ValidateAntiForgeryTokenFilter>(FilterScope.Global);
        }
    }
}