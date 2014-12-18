﻿namespace Sporacid.Simplets.Webapp.Services
{
    using System;
    using System.Reflection;
    using Ninject;
    using Ninject.Web.Common;
    using Sporacid.Simplets.Webapp.Core.Security.Authorization;
    using Sporacid.Simplets.Webapp.Core.Security.Bootstrap;

    /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
    /// <version>1.9.0</version>
    public class SecurityConfig
    {
        private const Claims AllClaims = (Claims) 511;
        private const Claims ReadOnlyClaims = (Claims) 192;
        private const Claims ModifyClaims = (Claims) 207;
        private const Claims FullModifyClaims = (Claims) 255;

        private static readonly String[] AllModules =
        {
            "Anonyme", "Administration", "Enumerations", "Membre", "Subscription"
        };

        private static void BootstrapSecurityContext()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var kernel = new Bootstrapper().Kernel; // TODO try to get the singleton kernel.
            
            // Bootstrap the security database.
            kernel.Get<ISecurityDatabaseBootstrapper>()
                .Bootstrap(assembly, "Sporacid.Simplets.Webapp.Services.Services");
            
            // Bootstrap the user roles of the application.
            var roleBootstrapper = kernel.Get<IRoleBootstrapper>();
            roleBootstrapper
                .BindClaims(AllClaims)
                .ToModules(AllModules)
                .BootstrapTo(Role.Administrateur.ToString());
            roleBootstrapper
                .BindClaims(ReadOnlyClaims)
                .ToModules("Enumerations", "Anonyme")
                .BindClaims(AllClaims)
                .ToModules("Subscription", "Membre")
                .BindClaims(FullModifyClaims)
                .ToModules("Administration")
                .BootstrapTo(Role.Capitaine.ToString());
            roleBootstrapper
                .BindClaims(ReadOnlyClaims)
                .ToModules("Enumerations", "Membre", "Anonyme")
                .BootstrapTo(Role.Noob.ToString());
        }

        /// <authors>Simon Turcotte-Langevin, Patrick Lavallée, Jean Bernier-Vibert</authors>
        /// <version>1.9.0</version>
        public enum Role
        {
            Administrateur,
            Capitaine,
            Noob
        }
    }
}